using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Patrol, SensorContact, EnemyWarpIn, Battle, Reinforcements, Victory }

    [Header("Factions")]
    [SerializeField] private FactionManager factionBlue;
    [SerializeField] private FactionManager factionRed;

    [Header("Player Ship")]
    [SerializeField] private ShipAI          playerCapitalShip;
    [SerializeField] private HealthComponent playerCapitalShipHealth;

    [Header("Battle Positions")]
    [SerializeField] private Transform friendlyBattlePosition;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform enemyBattlePosition;
    [SerializeField] private Transform reinforcementEntryPoint;

    [Header("Spawn Roots (children = individual spawn slots)")]
    [SerializeField] private Transform redFighterSpawnRoot;
    [SerializeField] private Transform blueFighterSpawnRoot;
    [SerializeField] private Transform reinforcementSpawnRoot;

    [Header("Prefabs")]
    [SerializeField] private GameObject prefabEnemyCruiser;
    [SerializeField] private GameObject prefabFighterRed;
    [SerializeField] private GameObject prefabDroneRed;
    [SerializeField] private GameObject prefabFighterBlue;
    [SerializeField] private GameObject prefabReinforcementCruiser;
    [SerializeField] private GameObject prefabReinforcementFighter;

    [Header("Cameras")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private GameObject  gameCamera;

    [Header("Radio Chatter")]
    [SerializeField] private UIManager   uiManager;
    [SerializeField] private AudioSource radioSource;
    [SerializeField] private AudioClip   chatterSensorContact;
    [SerializeField] private AudioClip   chatterEnemyDetected;
    [SerializeField] private AudioClip   chatterReinforcements;
    [SerializeField] private AudioClip   chatterReinforcementsArrived;
    [SerializeField] private string      messageSensorContact         = "Unidentified contact on sensors.";
    [SerializeField] private string      messageEnemyDetected         = "Enemy capital ship confirmed. All hands to battle stations.";
    [SerializeField] private string      messageReinforcements        = "Requesting immediate reinforcements.";
    [SerializeField] private string      messageReinforcementsArrived = "Reinforcements inbound. Hold the line.";

    [System.Serializable]
    struct ContactEntry { public RadioSender sender; public RadioContact contact; }
    [SerializeField] private ContactEntry[] radioContacts;

    [Header("Timing (seconds)")]
    [SerializeField] private float patrolDuration        = 30f;
    [SerializeField] private float sensorContactDuration = 8f;
    [SerializeField] private float battleDuration        = 300f;
    [SerializeField] private float enemyRespawnInterval  = 25f;

    [Header("Force Counts")]
    [SerializeField] private int initialRedFighters = 4;
    [SerializeField] private int initialRedDrones   = 2;
    [SerializeField] private int initialBlueFighters = 3;
    [SerializeField] private int maxRedFighters     = 6;
    [SerializeField] private int maxRedDrones       = 3;
    [SerializeField] private int reinforcementFighters = 4;

    [Header("Health Trigger")]
    [SerializeField] [Range(0f, 1f)] private float healthTriggerThreshold = 0.3f;

    // Runtime state
    private GameState _state;
    private Coroutine _stateRoutine;

    // Spawned ship tracking
    private ShipAI       _enemyCapitalShip;
    private bool         _enemyCruiserDestroyed;
    private List<ShipAI> _liveRedFighters = new();
    private List<ShipAI> _liveRedDrones   = new();

    // Round-robin spawn index per root
    private int _redSpawnIdx;
    private int _blueSpawnIdx;
    private int _reinforcementSpawnIdx;

    // Statistics
    private int   _enemiesDestroyed;
    private float _battleStartTime;

    // Battle-phase cancellation flag
    private bool _reinforcementsTriggered;

    public GameState CurrentState => _state;

    void Start()
    {
        TransitionTo(GameState.Menu);
    }

    // -------------------------------------------------------------------------
    // State machine core
    // -------------------------------------------------------------------------

    void TransitionTo(GameState next)
    {
        if (_stateRoutine != null) StopCoroutine(_stateRoutine);
        _state = next;
        _stateRoutine = StartCoroutine(RunState(next));
    }

    IEnumerator RunState(GameState s)
    {
        return s switch
        {
            GameState.Menu           => MenuState(),
            GameState.Patrol         => PatrolState(),
            GameState.SensorContact  => SensorContactState(),
            GameState.EnemyWarpIn    => EnemyWarpInState(),
            GameState.Battle         => BattleState(),
            GameState.Reinforcements => ReinforcementsState(),
            GameState.Victory        => VictoryState(),
            _                        => throw new System.ArgumentOutOfRangeException()
        };
    }

    // -------------------------------------------------------------------------
    // States
    // -------------------------------------------------------------------------

    IEnumerator MenuState()
    {
        SetCamera(menuActive: true);
        bool ready = false;
        void OnReady() => ready = true;
        if (menuManager != null)
        {
            menuManager.OnStartRequested += OnReady;
            menuManager.Activate();
            yield return new WaitUntil(() => ready);
            menuManager.OnStartRequested -= OnReady;
            menuManager.Deactivate();
        }
        else
        {
            yield return new WaitUntil(() => Input.anyKeyDown);
        }
        TransitionTo(GameState.Patrol);
    }

    IEnumerator PatrolState()
    {
        SetCamera(menuActive: false);
        yield return new WaitForSeconds(patrolDuration);
        TransitionTo(GameState.SensorContact);
    }

    IEnumerator SensorContactState()
    {
        PlayChatter(RadioSender.BridgeOfficer, chatterSensorContact, messageSensorContact);
        yield return new WaitForSeconds(sensorContactDuration);
        TransitionTo(GameState.EnemyWarpIn);
    }

    IEnumerator EnemyWarpInState()
    {
        // Spawn enemy capital ship
        if (prefabEnemyCruiser != null && enemySpawnPoint != null)
        {
            GameObject go = Instantiate(prefabEnemyCruiser, enemySpawnPoint.position, enemySpawnPoint.rotation);
            _enemyCapitalShip = go.GetComponent<ShipAI>();
            factionRed.RegisterShip(_enemyCapitalShip);
            factionBlue.RegisterEnemy(go.transform, ShipClass.CapitalShip);

            HealthComponent hp = go.GetComponent<HealthComponent>();
            if (hp != null)
                hp.OnDestroyed.AddListener(OnEnemyCruiserDestroyed);

            if (enemyBattlePosition != null)
                factionRed.OrderMoveTo(enemyBattlePosition);
        }

        // Scramble Red fighters and drones
        for (int i = 0; i < initialRedFighters; i++)
            SpawnShip(prefabFighterRed, redFighterSpawnRoot, ref _redSpawnIdx,
                      factionRed, factionBlue, ShipClass.Fighter, _liveRedFighters);
        for (int i = 0; i < initialRedDrones; i++)
            SpawnShip(prefabDroneRed, redFighterSpawnRoot, ref _redSpawnIdx,
                      factionRed, factionBlue, ShipClass.Fighter, _liveRedDrones);

        // Scramble Blue fighters
        for (int i = 0; i < initialBlueFighters; i++)
            SpawnShip(prefabFighterBlue, blueFighterSpawnRoot, ref _blueSpawnIdx,
                      factionBlue, factionRed, ShipClass.Fighter, null);

        // Order Blue capital ship to battle position
        if (friendlyBattlePosition != null)
            factionBlue.OrderMoveTo(friendlyBattlePosition);

        // Assign combat targets
        factionRed.DistributeTargets();
        factionBlue.DistributeTargets();

        PlayChatter(RadioSender.BridgeOfficer, chatterEnemyDetected, messageEnemyDetected);
        float chatterDelay = chatterEnemyDetected != null ? chatterEnemyDetected.length + 1f : 3f;
        yield return new WaitForSeconds(chatterDelay);

        PlayChatter(RadioSender.WingCommander, chatterReinforcements, messageReinforcements);

        _battleStartTime = Time.time;
        TransitionTo(GameState.Battle);
    }

    IEnumerator BattleState()
    {
        _reinforcementsTriggered = false;
        Coroutine respawn  = StartCoroutine(RespawnLoop());
        Coroutine watchdog = StartCoroutine(HealthWatchdog());

        yield return new WaitForSeconds(battleDuration);

        StopCoroutine(respawn);
        StopCoroutine(watchdog);

        TransitionTo(GameState.Reinforcements);
    }

    IEnumerator ReinforcementsState()
    {
        // Spawn reinforcement capital ship
        if (prefabReinforcementCruiser != null && reinforcementEntryPoint != null)
        {
            GameObject go = Instantiate(prefabReinforcementCruiser,
                                        reinforcementEntryPoint.position,
                                        reinforcementEntryPoint.rotation);
            ShipAI ai = go.GetComponent<ShipAI>();
            factionBlue.RegisterShip(ai);
            factionRed.RegisterEnemy(go.transform, ShipClass.CapitalShip);

            if (enemyBattlePosition != null)
                factionBlue.OrderMoveTo(enemyBattlePosition);
        }

        // Spawn reinforcement fighters
        for (int i = 0; i < reinforcementFighters; i++)
            SpawnShip(prefabReinforcementFighter, reinforcementSpawnRoot, ref _reinforcementSpawnIdx,
                      factionBlue, factionRed, ShipClass.Fighter, null);

        factionBlue.DistributeTargets();
        factionRed.DistributeTargets();

        PlayChatter(RadioSender.ReinforcementLeader, chatterReinforcementsArrived, messageReinforcementsArrived);

        yield return new WaitUntil(() => _enemyCruiserDestroyed);
        TransitionTo(GameState.Victory);
    }

    IEnumerator VictoryState()
    {
        float elapsed = Time.time - _battleStartTime;
        Debug.Log($"[GameManager] Victory! Enemies destroyed: {_enemiesDestroyed} | Battle time: {elapsed:F0}s");
        // TODO: show end screen / statistics UI
        yield break;
    }

    // -------------------------------------------------------------------------
    // Battle sub-coroutines
    // -------------------------------------------------------------------------

    IEnumerator RespawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(enemyRespawnInterval);

            CleanDeadEntries(_liveRedFighters);
            CleanDeadEntries(_liveRedDrones);

            if (_liveRedFighters.Count < maxRedFighters)
                SpawnShip(prefabFighterRed, redFighterSpawnRoot, ref _redSpawnIdx,
                          factionRed, factionBlue, ShipClass.Fighter, _liveRedFighters);

            if (_liveRedDrones.Count < maxRedDrones)
                SpawnShip(prefabDroneRed, redFighterSpawnRoot, ref _redSpawnIdx,
                          factionRed, factionBlue, ShipClass.Fighter, _liveRedDrones);

            factionRed.DistributeTargets();
        }
    }

    IEnumerator HealthWatchdog()
    {
        while (true)
        {
            yield return null;
            if (playerCapitalShipHealth != null &&
                playerCapitalShipHealth.HullPercent <= healthTriggerThreshold &&
                !_reinforcementsTriggered)
            {
                _reinforcementsTriggered = true;
                // Cancel BattleState's WaitForSeconds by transitioning immediately
                TransitionTo(GameState.Reinforcements);
                yield break;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    ShipAI SpawnShip(GameObject prefab, Transform spawnRoot, ref int spawnIndex,
                     FactionManager ownFaction, FactionManager enemyFaction,
                     ShipClass cls, List<ShipAI> trackingList)
    {
        if (prefab == null || spawnRoot == null || spawnRoot.childCount == 0) return null;

        Transform slot = spawnRoot.GetChild(spawnIndex % spawnRoot.childCount);
        spawnIndex++;

        GameObject go = Instantiate(prefab, slot.position, slot.rotation);
        ShipAI ai = go.GetComponent<ShipAI>();

        if (ai != null) ownFaction.RegisterShip(ai);
        enemyFaction.RegisterEnemy(go.transform, cls);
        trackingList?.Add(ai);

        HealthComponent hp = go.GetComponent<HealthComponent>();
        if (hp != null)
        {
            hp.OnDestroyed.AddListener(() => OnShipDestroyed(go, ai, enemyFaction, trackingList));
        }

        return ai;
    }

    void OnShipDestroyed(GameObject ship, ShipAI ai, FactionManager enemyFaction, List<ShipAI> trackingList)
    {
        enemyFaction.UnregisterEnemy(ship.transform);
        trackingList?.Remove(ai);
        _enemiesDestroyed++;
    }

    void OnEnemyCruiserDestroyed()
    {
        _enemyCruiserDestroyed = true;
        _enemiesDestroyed++;
        if (_enemyCapitalShip != null)
            factionBlue.UnregisterEnemy(_enemyCapitalShip.transform);
    }

    void PlayChatter(RadioSender sender, AudioClip clip, string message)
    {
        if (radioSource != null && clip != null)
        {
            radioSource.Stop();
            radioSource.clip = clip;
            radioSource.Play();
        }
        uiManager?.DisplayRadioMessage(GetContact(sender), message,
                                       clip != null ? clip.length : 3f);
    }

    RadioContact GetContact(RadioSender sender)
    {
        foreach (var e in radioContacts)
            if (e.sender == sender) return e.contact;
        return null;
    }

    void SetCamera(bool menuActive)
    {
        if (gameCamera != null) gameCamera.SetActive(!menuActive);
    }

    static void CleanDeadEntries(List<ShipAI> list)
    {
        list.RemoveAll(ai => ai == null);
    }
}
