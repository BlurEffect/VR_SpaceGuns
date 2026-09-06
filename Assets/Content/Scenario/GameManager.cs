using System.Collections;
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
    [SerializeField] private Transform reinforcementBattlePosition;

    [Header("Hangar Bays")]
    [SerializeField] private HangarBay playerHangarBay;
    [SerializeField] private Transform dogfightContainmentAnchor;

    [Header("Prefabs")]
    [SerializeField] private GameObject prefabEnemyCruiser;
    [SerializeField] private GameObject prefabReinforcementCruiser;

    [Header("Cameras")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private GameObject  gameCamera;
    [SerializeField] private GameObject  gameCameraAddons;

    [Header("Radio Chatter")]
    [SerializeField] private UIManager      uiManager;
    [SerializeField] private AudioSource    radioSource;
    [SerializeField] private ChatterLibrary chatterLibrary;

    [Header("Timing (seconds)")]
    [SerializeField] private float patrolDuration        = 30f;
    [SerializeField] private float sensorContactDuration = 8f;
    [SerializeField] private float battleDuration        = 300f;

    [Header("Force Counts")]
    [SerializeField] private int initialRedFighters = 4;
    [SerializeField] private int initialRedDrones   = 2;
    [SerializeField] private int initialBlueFighters = 3;
    [SerializeField] private int initialBlueDrones   = 2;
    [SerializeField] private int reinforcementFighters = 4;
    [SerializeField] private int reinforcementDrones   = 2;

    [Header("Health Trigger")]
    [SerializeField] [Range(0f, 1f)] private float healthTriggerThreshold = 0.3f;

    // Runtime state
    private GameState _state;
    private Coroutine _stateRoutine;

    // Spawned ship tracking
    private ShipAI    _enemyCapitalShip;
    private bool      _enemyCruiserDestroyed;
    private HangarBay _enemyHangarBay;
    private HangarBay _reinforcementHangarBay;

    // Statistics
    private int   _enemiesDestroyed;
    private float _battleStartTime;

    // Battle-phase cancellation flag
    private bool _reinforcementsTriggered;

    public GameState CurrentState => _state;

    void Start()
    {
        playerHangarBay?.SetFactions(factionBlue, factionRed);
        playerHangarBay?.SetContainmentAnchor(dogfightContainmentAnchor);
        TransitionTo(GameState.Menu);
    }

    // -------------------------------------------------------------------------
    // State machine core
    // -------------------------------------------------------------------------

    void TransitionTo(GameState next)
    {
        Debug.Log($"[GameManager] State transition: {_state} -> {next}");
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
        PlayChatter(ChatterKey.SensorContact);
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
            _enemyHangarBay   = go.GetComponent<HangarBay>();
            factionRed.RegisterShip(_enemyCapitalShip);
            factionBlue.RegisterEnemy(go.transform, ShipClass.CapitalShip);
            _enemyHangarBay?.SetFactions(factionRed, factionBlue);
            _enemyHangarBay?.SetContainmentAnchor(dogfightContainmentAnchor);

            HealthComponent hp = go.GetComponent<HealthComponent>();
            if (hp != null)
                hp.OnDestroyed.AddListener(OnEnemyCruiserDestroyed);

            if (enemyBattlePosition != null)
                _enemyCapitalShip.AssignMovementTarget(enemyBattlePosition);
        }

        // Scramble Red fighters and drones, and Blue fighters, from their carriers' own hangars
        _enemyHangarBay?.LaunchFighters(initialRedFighters);
        _enemyHangarBay?.LaunchDrones(initialRedDrones);
        playerHangarBay?.LaunchFighters(initialBlueFighters);
        playerHangarBay?.LaunchDrones(initialBlueDrones);

        // Order Blue capital ship to battle position
        if (friendlyBattlePosition != null)
            playerCapitalShip.AssignMovementTarget(friendlyBattlePosition);

        // Assign combat targets
        factionRed.DistributeTargets();
        factionBlue.DistributeTargets();

        float enemyDetectedLength = 3f;
        if (chatterLibrary != null &&
            chatterLibrary.TryGet(ChatterKey.EnemyDetected, out var detected) &&
            detected.clip != null)
        {
            enemyDetectedLength = detected.clip.length + 1f;
        }
        PlayChatter(ChatterKey.EnemyDetected);
        yield return new WaitForSeconds(enemyDetectedLength);

        PlayChatter(ChatterKey.ReinforcementsRequested);

        _battleStartTime = Time.time;
        TransitionTo(GameState.Battle);
    }

    IEnumerator BattleState()
    {
        _reinforcementsTriggered = false;
        _enemyHangarBay?.StartAutoRespawn();
        Coroutine watchdog = StartCoroutine(HealthWatchdog());

        yield return new WaitForSeconds(battleDuration);

        _enemyHangarBay?.StopAutoRespawn();
        StopCoroutine(watchdog);

        Debug.Log("[GameManager] Reinforcements triggered by battle duration timer.");
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
            _reinforcementHangarBay = go.GetComponent<HangarBay>();
            factionBlue.RegisterShip(ai);
            factionRed.RegisterEnemy(go.transform, ShipClass.CapitalShip);
            _reinforcementHangarBay?.SetFactions(factionBlue, factionRed);
            _reinforcementHangarBay?.SetContainmentAnchor(dogfightContainmentAnchor);

            if (reinforcementBattlePosition != null)
                ai.AssignMovementTarget(reinforcementBattlePosition);
        }

        // Scramble reinforcement fighters and drones from the reinforcement carrier's own hangar
        _reinforcementHangarBay?.LaunchFighters(reinforcementFighters);
        _reinforcementHangarBay?.LaunchDrones(reinforcementDrones);

        factionBlue.DistributeTargets();
        factionRed.DistributeTargets();

        PlayChatter(ChatterKey.ReinforcementsArrived);

        yield return new WaitUntil(() => _enemyCruiserDestroyed);
        TransitionTo(GameState.Victory);
    }

    IEnumerator VictoryState()
    {
        float elapsed = Time.time - _battleStartTime;
        int totalLosses = _enemiesDestroyed
            + (_enemyHangarBay         != null ? _enemyHangarBay.DestroyedCount         : 0)
            + (playerHangarBay         != null ? playerHangarBay.DestroyedCount         : 0)
            + (_reinforcementHangarBay != null ? _reinforcementHangarBay.DestroyedCount : 0);
        Debug.Log($"[GameManager] Victory! Enemies destroyed: {totalLosses} | Battle time: {elapsed:F0}s");
        // TODO: show end screen / statistics UI
        yield break;
    }

    // -------------------------------------------------------------------------
    // Battle sub-coroutines
    // -------------------------------------------------------------------------

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
                Debug.Log($"[GameManager] Reinforcements triggered early by hull health watchdog (HullPercent={playerCapitalShipHealth.HullPercent:F2} <= {healthTriggerThreshold:F2}).");
                // Cancel BattleState's WaitForSeconds by transitioning immediately
                TransitionTo(GameState.Reinforcements);
                yield break;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    void OnEnemyCruiserDestroyed()
    {
        _enemyCruiserDestroyed = true;
        _enemiesDestroyed++;
        if (_enemyCapitalShip != null)
            factionBlue.UnregisterEnemy(_enemyCapitalShip.transform);
    }

    void PlayChatter(ChatterKey key)
    {
        if (chatterLibrary == null || !chatterLibrary.TryGet(key, out var entry)) return;

        if (radioSource != null && entry.clip != null)
        {
            radioSource.Stop();
            radioSource.clip = entry.clip;
            radioSource.Play();
        }
        uiManager?.DisplayRadioMessage(entry.contact, entry.message,
                                       entry.clip != null ? entry.clip.length : 3f);
    }

    void SetCamera(bool menuActive)
    {
        if (gameCamera != null)
        {
            gameCamera.SetActive(!menuActive);
            gameCameraAddons.SetActive(!menuActive);
        }
    }
}
