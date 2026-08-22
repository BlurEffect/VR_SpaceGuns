# VR SpaceGuns — Project Context

## Vision

A VR showcase built for Meta Quest 3. The player rides a capital ship under attack, manning a turret to defend it. The scenario: en route to a destination, an enemy capital ship appears and scrambles fighters while opening fire with its own turrets. The player's ship responds in kind. The player calls for reinforcements, which eventually arrive and tip the battle. Not a fully-fledged game — a polished interactive showcase of space combat.

## Scenario Beat

1. Player capital ship in transit.
2. Enemy capital ship detected on sensors, warps in.
3. Enemy scrambles fighters, opens fire with capital turrets.
4. Friendly fighters scramble in response.
5. Player mans a turret and engages.
6. Player calls reinforcements.
7. Reinforcements arrive, scales tip, attackers destroyed.

---

## Architecture

### 1. Spacecraft AI & Flight (`Assets/Content/Ships/`)

Weighted steering behavior system — no Rigidbody physics, all custom.

- **`ShipClass.cs`** — enum: `Fighter`, `CapitalShip`, `CapitalShipSubsystem`. Used by `ShipAI` and `FactionManager` to classify ships and match them to appropriate enemy targets.
- **`SteeringModule.cs`** — static library of steering algorithms: Seek, Flee, Arrive, Wander, Orbit, Pursue, Evade, AvoidObstacles (5-ray SphereCast fan), Cohesion, Separation, Alignment, Containment, Formation, Patrol, AttackRun.
- **`SteeringAgent.cs`** — MonoBehaviour that blends behaviors each frame. Reads physics params from `ShipSteeringProfile` and role weights/params from `ShipSteeringBehaviorProfile` (set at runtime by `ShipAI` — `[HideInInspector]` in Inspector). Holds per-engagement scene refs (targets, containment center, formation leader, patrol waypoints) and engagement-specific scalars (orbitRadius, containmentRadius). Outputs `desiredDirection` and `desiredSpeed`.
- **`FlightController.cs`** — reads `SteeringAgent` output and `ShipFlightProfile`; handles rotation (Slerp with blended up-vector to avoid gimbal lock), banking roll, drift damping, and position update.
- **`ShipAI.cs`** — per-ship tactical brain. Fields: `shipClass` (Fighter/CapitalShip), `selfScanForTargets` (false on capital ships to prevent chasing fighters autonomously). Three-tier steering priority in `UpdateSteering()`: `MovementTarget` (explicit fly-to) → `AssignedTarget` (combat, also drives turrets) → patrol waypoints. Turrets split into `TurretMount[] primaryMounts` (tracks `AssignedTarget`) and `TurretMount[] pointDefenseMounts` (autonomously tracks nearest scanned enemy per mount). Both use `ReadyToFire` gate before enabling `Shooting`. `FactionManager` drives targets via `AssignTarget()` / `AssignMovementTarget()`.
- **`FactionManager.cs`** — per-faction strategic coordinator. Enemies registered as `EnemyEntry { Transform target, ShipClass shipClass }` via `RegisterEnemy(Transform, ShipClass)`. `DistributeTargets()` round-robins enemies to ships by matching `ShipClass`; fighters fall back to any enemy if no matching class found; capital ships get `ClearTarget()` if no match. `initialEnemies` list for scene testing without a GameManager — populated at `Start()` and `DistributeTargets()` fires automatically. Key methods: `OrderAttack()`, `OrderClearTargets()`, `OrderMoveTo(Transform)` (hull navigation, turrets unaffected), `OrderClearMovement()`.
- **`ShipFlightProfile.cs`** — SO defining flight physics for a ship type: `maxSpeed`, `acceleration`, `turnRate`, `driftDamping`, `driftDampingFactor`, `bankAmount`, `bankSmooth`.
- **`ShipSteeringProfile.cs`** — SO defining steering physics for a ship type: avoidance distances, obstacle mask, ship radius, separation distance, max steering angle, wander params, waypoint reach radius.
- **`ShipSteeringBehaviorProfile.cs`** — SO defining an AI role: all behavior weights plus role-specific numeric params (`slowRadius`, `arriveRadius`, `attackRange`, `breakOffRange`). Swapped at runtime by `ShipAI` between `attackProfile` and `patrolProfile`.
- **`ShipSensorProfile.cs`** — SO defining sensor characteristics for a ship type: `detectionRange` (OverlapSphere radius) and `enemyMask` (LayerMask). Referenced by `ShipAI` for target scanning.

**Four-SO rule**: `ShipFlightProfile`, `ShipSteeringProfile`, and `ShipSensorProfile` hold ship-type physical constants (what kind of ship this is). `ShipSteeringBehaviorProfile` holds tactical parameters that change with the ship's assignment. Scene references and per-engagement scalars stay on the MonoBehaviour.

**Capital ship movement**: hull navigation (`MovementTarget` / patrol waypoints) is separate from combat targeting (`AssignedTarget`). The GameManager sets `OrderMoveTo(battlePosition)` for hull movement; `DistributeTargets()` assigns the enemy capital ship for turret engagement independently. Capital ships use Arrive behavior (slow `FlightProfile`: maxSpeed ~8, turnRate ~0.5) and hold position at their strategic location.

**Prefab variants**: faction-specific differences (gun profiles with projectile color, layer masks) are baked into Blue/Red prefab variants rather than a runtime faction system. One neutral base prefab per ship type; Blue and Red variants override faction-specific fields.

### 2. Turrets & Firing (`Assets/Content/Guns/`)

- **`Targeting.cs`** — dual-axis turret aiming. Yaw on `rotatorYMain` (projects onto parent's local up-plane via `Vector3.ProjectOnPlane` — correct for any ship orientation), pitch on `rotatorX`, optional per-barrel yaw on `rotatorBarrelLeft`/`rotatorBarrelRight` (null-safe). Supports predictive aiming via target `Rigidbody.linearVelocity`. Exposes `IsAimed` (angle between muzzle forward and predicted target within `aimThresholdDegrees`) and `ReadyToFire` (`IsAimed` + `lineOfFireBlockers` raycast clear). `ShipAI` gates `Shooting.enabled` on `ReadyToFire`.
- **`Shooting.cs`** — Constant and Burst firing modes driven by a `GunProfile` SO. Spawns projectiles via `ProjectileManager`.
- **`GunProfile.cs`** — SO (in `ScriptableObjects/` subfolder). Fields: `FiringMode`, cooldown/burst params, `projectileSpeed`, `projectileLifetime`, `effectiveRange`, `projectileColor`, `shieldDamage`, `hullDamage`. Also defines the `FiringMode` enum.
- **`TurretMount`** — serializable struct pairing `Targeting` and `Shooting` explicitly, used for both `primaryMounts` and `pointDefenseMounts` on `ShipAI`. Eliminates fragile index-pairing between separate arrays.

### 3. Projectile & VFX Pipeline (`Assets/Content/Guns/`)

High-performance, no-Rigidbody design:

- **`ProjectileManager.cs`** — singleton. Moves projectiles via `Physics.Raycast` each `FixedUpdate`. On hit: calls `IDamageable.TakeDamage()` on the hit collider's parent, then notifies `TurretVFXManager` to kill the particle and spawn an impact. Lifetime-expired projectiles are removed silently.
- **`TurretVFXManager.cs`** — singleton. Batches all tracer spawns and impacts into `GraphicsBuffer` uploads once per `LateUpdate`, then fires a single VFX Graph event. The `HitBuffer` (uint[1024]) signals the GPU when a particle should die — avoids per-particle CPU events.
- `ProjectileData` struct carries position, direction, speed, lifetime, color, `shieldDamage`, and `hullDamage`.

### 4. Health & Shields (`Assets/Content/Shared/`, `Assets/Content/Ships/ScriptableObjects/`)

- **`IDamageable.cs`** — interface: `TakeDamage(float shieldDamage, float hullDamage)`.
- **`HealthComponent.cs`** — implements `IDamageable`. Shields absorb `shieldDamage` first; `hullDamage` only applies once shields are at 0. Shield recharge: delayed by `shieldRechargeDelay` seconds after last hit, then regenerates at `shieldRechargeRate` per second. `OnShieldsDown` and `OnDestroyed` UnityEvents for hooking destruction logic. `HullPercent` property (0–1) used by `GameManager` health watchdog.
- **`HealthProfile.cs`** — SO with `maxHull`, `maxShields`, `shieldRechargeDelay`, `shieldRechargeRate`.

### 5. Scenario Orchestration (`Assets/Content/Scenario/`)

- **`GameManager.cs`** — singleton MonoBehaviour driving the full showcase via a coroutine-based state machine. States: `Menu → Patrol → SensorContact → EnemyWarpIn → Battle → Reinforcements → Victory`. Spawns enemy cruiser, fighters, and drones at `EnemyWarpIn`; calls `FactionManager.RegisterShip/RegisterEnemy/DistributeTargets` to wire AI; runs a `RespawnLoop` coroutine during `Battle` to top up Red fighter/drone counts; triggers `Reinforcements` either on `battleDuration` timer or when player capital ship `HullPercent` drops below `healthTriggerThreshold`; transitions to `Victory` when the enemy capital ship `OnDestroyed` fires. `PlayChatter(RadioSender, AudioClip, string)` drives both audio and `UIManager` at each narrative beat. All timing and force counts are Inspector-tweakable. Statistics (enemies destroyed, elapsed time) logged to console in `VictoryState` — proper UI is future work.
- **`MenuManager.cs`** — active during the `Menu` state. Cycles through a `CinemachineCamera[]` array on a fixed interval. Subscribes to `InputSystem.onAnyButtonPress` as an `IDisposable` (fire-once pattern). Exposes `Activate()` / `Deactivate()` called by GameManager; fires `OnStartRequested` event to trigger the `Patrol` transition.
- **`RadioContact.cs`** — SO (`Assets/Content/Scenario/`) holding `contactName` (string) and `portrait` (Sprite) for a radio sender. Also defines the `RadioSender` enum (`BridgeOfficer`, `WingCommander`, `ReinforcementLeader`). GameManager holds a `ContactEntry[]` array (enum → SO) and looks up the right contact at each chatter beat.

**Drone distinction**: drones use `ShipClass.Fighter` — no separate class. Visual and flight-model difference only (separate prefab, different `ShipFlightProfile`). Keeps `DistributeTargets()` logic unchanged.

**Scene wiring required**: empty GameObjects for battle positions and spawn roots; `MenuManager` + `gameCamera` wired on GameManager (`MenuManager` handles camera cycling during menu, `gameCamera` is enabled for gameplay); `playerCapitalShip` and `playerCapitalShipHealth` wired to the Blue cruiser.

### 6. UI (`Assets/Content/UI/`)

- **`UIManager.cs`** — displays radio message callouts. `DisplayRadioMessage(RadioContact, string, float)` sets the contact portrait and name then shows the message text; auto-clears after the given duration (matched to audio clip length by `GameManager`). A string-only overload is kept for fallback callers. Wired to GameManager via Inspector.

---

## Design Decisions

- **All spacecraft have shields** (fighters and capital ships alike). `HealthProfile.maxShields = 0` effectively disables shields for any variant that doesn't need them.
- **Health granularity**: fighters → one `HealthComponent` per ship. Capital ships → one `HealthComponent` per turret + one for the hull. Separate instances, no subsystem complexity.
- **Weapon differentiation**: guns have separate `shieldDamage` and `hullDamage`. Energy weapons strip shields fast; kinetic weapons punch hull once shields are down.
- **Friendly fire** is allowed — no faction/IFF check in `ProjectileManager`.
- **ScriptableObjects for everything** — ship flight, steering physics, sensor config, behavior profiles, gun configs, and health profiles are all SOs for easy per-prefab tuning without code changes. Ship-type constants live in `ShipFlightProfile`/`ShipSteeringProfile`/`ShipSensorProfile`; role/mission tactics live in `ShipSteeringBehaviorProfile`; engagement-specific scene refs stay on the MonoBehaviour.
- **No Rigidbody physics** on ships or projectiles — all movement is custom, raycast-based collision.
- **Capital ships do not self-scan** (`selfScanForTargets = false`) — hull movement and turret targets are always assigned externally by `FactionManager` / `GameManager`. Without this flag, capital ships would auto-acquire and chase the nearest enemy (a fighter), causing unwanted hull rotation.
- **Class-based target distribution** — `FactionManager.DistributeTargets()` matches ships to enemies by `ShipClass`. Fighters fall back to any enemy if no matching class available. Capital ships receive `ClearTarget()` if no matching enemy exists, keeping them in patrol/hold position.
- **Movement vs combat targeting are separate** — `MovementTarget` drives hull navigation (arrive at battle position); `AssignedTarget` drives turret engagement. The GameManager sets both independently.
- **Faction prefab variants over a runtime faction system** — gun profile colors and layer masks are set in Blue/Red prefab variants. Avoided a `FactionData` + `FactionMember` system as the complexity outweighs the benefit for a 2-faction showcase.

---

## What's Not Yet Built

- VR player turret control (Meta Quest 3 controller input → turret transforms) — `PlayerTurretController.cs` exists but XR integration (cursor lock, locomotion-provider disable) is deferred
- Ship destruction (visual + removal on `OnDestroyed`)
- Enemy capital ship prefab wired up in the scene with turrets, health, ShipAI, and FactionManager
- Drone prefab (placeholder model + fast/swarmy `ShipFlightProfile`)
- Scene wiring for `GameManager` (battle position transforms, spawn roots, prefab/audio refs, RadioContact assets)
- Shield/hull HUD indicators
- Victory screen / statistics UI (currently console-log stub)

---

## Key File Locations

| Purpose | Path |
|---------|------|
| Ship class enum | `Assets/Content/Ships/Scripts/ShipClass.cs` |
| Steering behaviors | `Assets/Content/Ships/Scripts/SteeringModule.cs` |
| Steering agent (AI blend) | `Assets/Content/Ships/Scripts/SteeringAgent.cs` |
| Ship movement | `Assets/Content/Ships/Scripts/FlightController.cs` |
| Flight physics SO | `Assets/Content/Ships/ScriptableObjects/ShipFlightProfile.cs` |
| Steering physics SO | `Assets/Content/Ships/ScriptableObjects/ShipSteeringProfile.cs` |
| AI role / behavior weights SO | `Assets/Content/Ships/ScriptableObjects/ShipSteeringBehaviorProfile.cs` |
| Sensor config SO | `Assets/Content/Ships/ScriptableObjects/ShipSensorProfile.cs` |
| Per-ship combat brain | `Assets/Content/Ships/Scripts/ShipAI.cs` |
| Per-faction coordinator | `Assets/Content/Ships/Scripts/FactionManager.cs` |
| Turret aiming | `Assets/Content/Guns/Targeting.cs` |
| Turret firing | `Assets/Content/Guns/Shooting.cs` |
| Gun config SO | `Assets/Content/Guns/ScriptableObjects/GunProfile.cs` |
| Projectile physics | `Assets/Content/Guns/ProjectileManager.cs` |
| VFX batching | `Assets/Content/Guns/TurretVFXManager.cs` |
| Damage interface | `Assets/Content/Shared/IDamageable.cs` |
| Health/shield logic | `Assets/Content/Shared/HealthComponent.cs` |
| Health config SO | `Assets/Content/Ships/ScriptableObjects/HealthProfile.cs` |
| Scenario state machine | `Assets/Content/Scenario/GameManager.cs` |
| Menu camera cycling + input | `Assets/Content/Scenario/MenuManager.cs` |
| Radio contact SO + sender enum | `Assets/Content/Scenario/RadioContact.cs` |
| Radio message UI | `Assets/Content/UI/UIManager.cs` |
