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

- **`SteeringModule.cs`** — static library of steering algorithms: Seek, Flee, Arrive, Wander, Orbit, Pursue, Evade, AvoidObstacles (5-ray SphereCast fan), Cohesion, Separation, Alignment, Containment, Formation, Patrol, AttackRun.
- **`SteeringAgent.cs`** — MonoBehaviour that blends behaviors each frame. Reads physics params from `ShipSteeringProfile` and role weights/params from `ShipSteeringBehaviorProfile`. Holds per-engagement scene refs (targets, containment center, formation leader, patrol waypoints) and engagement-specific scalars (orbitRadius, containmentRadius). Outputs `desiredDirection` and `desiredSpeed`.
- **`FlightController.cs`** — reads `SteeringAgent` output and `ShipFlightProfile`; handles rotation (Slerp with blended up-vector to avoid gimbal lock), banking roll, drift damping, and position update.
- **`ShipAI.cs`** — per-ship tactical brain. Scans for enemies via `ShipSensorProfile`, assigns the nearest to its turrets (`Targeting[]`) and guns (`Shooting[]`), and swaps the `SteeringAgent`'s behavior profile between `attackProfile` and `patrolProfile`. Guns are enabled only when the target is within `GunProfile.effectiveRange`. `FactionManager` can override the self-scanned target via `AssignTarget()`.
- **`FactionManager.cs`** — per-faction strategic coordinator. Holds a list of `ShipAI` ships and an enemy target list (populated by a scenario/GameManager). `DistributeTargets()` round-robins enemies across ships; `OrderAttack()` focuses all ships on one target; `OrderClearTargets()` releases assignments so ships resume self-scanning.
- **`ShipFlightProfile.cs`** — SO defining flight physics for a ship type: `maxSpeed`, `acceleration`, `turnRate`, `driftDamping`, `driftDampingFactor`, `bankAmount`, `bankSmooth`.
- **`ShipSteeringProfile.cs`** — SO defining steering physics for a ship type: avoidance distances, obstacle mask, ship radius, separation distance, max steering angle, wander params, waypoint reach radius.
- **`ShipSteeringBehaviorProfile.cs`** — SO defining an AI role: all behavior weights plus role-specific numeric params (`slowRadius`, `arriveRadius`, `attackRange`, `breakOffRange`). Swap this SO at runtime to change a ship's assignment.
- **`ShipSensorProfile.cs`** — SO defining sensor characteristics for a ship type: `detectionRange` (OverlapSphere radius) and `enemyMask` (LayerMask). Referenced by `ShipAI` for target scanning.

**Four-SO rule**: `ShipFlightProfile`, `ShipSteeringProfile`, and `ShipSensorProfile` hold ship-type physical constants (what kind of ship this is). `ShipSteeringBehaviorProfile` holds tactical parameters that change with the ship's assignment. Scene references and per-engagement scalars stay on the MonoBehaviour.

### 2. Turrets & Firing (`Assets/Content/Guns/`)

- **`Targeting.cs`** — dual-axis turret aiming. Yaw on `rotatorYMain`, pitch on `rotatorX`, per-barrel yaw on `rotatorBarrelLeft`/`rotatorBarrelRight`. Supports predictive aiming via target `Rigidbody.linearVelocity`.
- **`Shooting.cs`** — Constant and Burst firing modes driven by a `GunProfile` SO. Spawns projectiles via `ProjectileManager`.
- **`GunProfile.cs`** — SO (in `ScriptableObjects/` subfolder) replacing the old inline `FiringPattern` struct. Fields: `FiringMode`, cooldown/burst params, `projectileSpeed`, `projectileLifetime`, `effectiveRange`, `projectileColor`, `shieldDamage`, `hullDamage`. Also defines the `FiringMode` enum.

### 3. Projectile & VFX Pipeline (`Assets/Content/Guns/`)

High-performance, no-Rigidbody design:

- **`ProjectileManager.cs`** — singleton. Moves projectiles via `Physics.Raycast` each `FixedUpdate`. On hit: calls `IDamageable.TakeDamage()` on the hit collider's parent, then notifies `TurretVFXManager` to kill the particle and spawn an impact. Lifetime-expired projectiles are removed silently.
- **`TurretVFXManager.cs`** — singleton. Batches all tracer spawns and impacts into `GraphicsBuffer` uploads once per `LateUpdate`, then fires a single VFX Graph event. The `HitBuffer` (uint[1024]) signals the GPU when a particle should die — avoids per-particle CPU events.
- `ProjectileData` struct carries position, direction, speed, lifetime, color, `shieldDamage`, and `hullDamage`.

### 4. Health & Shields (`Assets/Content/Shared/`, `Assets/Content/Ships/ScriptableObjects/`)

- **`IDamageable.cs`** — interface: `TakeDamage(float shieldDamage, float hullDamage)`.
- **`HealthComponent.cs`** — implements `IDamageable`. Shields absorb `shieldDamage` first; `hullDamage` only applies once shields are at 0. Shield recharge: delayed by `shieldRechargeDelay` seconds after last hit, then regenerates at `shieldRechargeRate` per second. `OnShieldsDown` and `OnDestroyed` UnityEvents for hooking destruction logic.
- **`HealthProfile.cs`** — SO with `maxHull`, `maxShields`, `shieldRechargeDelay`, `shieldRechargeRate`.

---

## Design Decisions

- **All spacecraft have shields** (fighters and capital ships alike). `HealthProfile.maxShields = 0` effectively disables shields for any variant that doesn't need them.
- **Health granularity**: fighters → one `HealthComponent` per ship. Capital ships → one `HealthComponent` per turret + one for the hull. Separate instances, no subsystem complexity.
- **Weapon differentiation**: guns have separate `shieldDamage` and `hullDamage`. Energy weapons strip shields fast; kinetic weapons punch hull once shields are down.
- **Friendly fire** is allowed — no faction/IFF check in `ProjectileManager`.
- **ScriptableObjects for everything** — ship flight, steering physics, sensor config, behavior profiles, gun configs, and health profiles are all SOs for easy per-prefab tuning without code changes. Ship-type constants live in `ShipFlightProfile`/`ShipSteeringProfile`/`ShipSensorProfile`; role/mission tactics live in `ShipSteeringBehaviorProfile`; engagement-specific scene refs stay on the MonoBehaviour.
- **No Rigidbody physics** on ships or projectiles — all movement is custom, raycast-based collision.

---

## What's Not Yet Built

- VR player turret control (Meta Quest 3 controller input → turret transforms)
- Scenario / game state logic (wave management, reinforcement trigger) — `FactionManager` API is in place, needs a `GameManager` to drive it
- Ship destruction (visual + removal on `OnDestroyed`)
- Enemy capital ship with its own turrets and health
- AI behavior profile SO assets for the different roles (attack fighter, patrol, evade) — infrastructure is in place, just need Unity asset instances
- `ShipSensorProfile` and `ShipAI` wired up on prefabs in the scene
- Shield/hull HUD indicators

---

## Key File Locations

| Purpose | Path |
|---------|------|
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
