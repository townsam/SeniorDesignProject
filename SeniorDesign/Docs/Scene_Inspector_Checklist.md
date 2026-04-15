# Scene & Inspector Checklist (Build + Simulation + ML)

Use this as a quick verification pass in `SampleScene` before running training.

## 1) Core Scene Objects
- [X] `GameManager` exists in scene with `GameManager` component.
- [X] `UIManager` exists and references both:
  - [X] `buildUI`
  - [X] `simulationUI`
- [X] `PlacementManager` exists in scene.
- [X] Main camera is tagged `MainCamera`.

## 2) Build-Phase Placement Wiring
- [X] Build UI contains `FlagTypeButtons`.
- [X] `FlagTypeButtons.buttonContainer` assigned.
- [X] `FlagTypeButtons.buttonPrefab` assigned (or intentionally left null for generated fallback buttons).
- [X] `FlagTypeButtons.slowZonePrefab` assigned.
- [X] `FlagTypeButtons.rewardBoostPrefab` assigned.
- [X] Ground objects are on `Ground` layer.
- [X] Win zone object is on `WinZone` layer (so placement raycasts ignore it).

## 3) Actor Prefab Wiring (Spawner + Agent)
- [X] `ActorSpawner.actorPrefab` assigned.
- [X] Actor prefab has:
  - [X] `Rigidbody`
  - [X] `ActorBehavior`
  - [X] `ActorAgent`
  - [X] `Behavior Parameters`
  - [X] `Decision Requester`
- [X] `ActorBehavior.groundCheck` assigned to a child transform near feet.
- [X] `ActorBehavior.groundLayer` includes the same `Ground` layer used by terrain/floor.
- [X] Actor prefab tagged `Agent` (required for `WinZone` trigger counting).
- [X] `ActorSpawner.defaultTarget` assigned (or `WinZone` exists for auto-fallback target assignment).

Notes:
- `ActorAgent.target` is now assigned automatically at runtime by `ActorSpawner` for each spawned actor.
- Directly assigning a scene target on the prefab is not required.
- `ActorAgent` now uses planned-movement fallback when no trainer/model is available, so actors still move during Simulation.
- When trainer is connected (or inference/heuristic is configured), ML control remains active.

## 4) ML-Agents Behavior Name Alignment
- [X] In Unity Actor Prefab `Behavior Parameters > Behavior Name` matches YAML behavior key.
- [X] Current YAML file exists: `Assets/ML-Agents/actor_ppo.yaml`.
- [X] Current YAML behavior key is: `ActorBehavior`.
- [X] Actor `Behavior Parameters` action space matches `ActorAgent` expectations:
  - [X] `Space Type = Continuous`
  - [X] `Continuous Actions = 3` (moveX, moveZ, jump)

If Unity behavior name differs, update one side so they match exactly.

## 5) Simulation Loop Validation
- [X] Start in Build phase and confirm actors are hidden.
- [X] Place at least one `SlowZone` and one `RewardBoost` flag.
- [X] Enter Simulation phase and confirm actors appear and move.
- [X] Verify periodic environment reset occurs at `ActorSpawner.episodeLength`.
- [X] Verify no `NullReferenceException` appears in Console.

## 6) Win Condition Validation
- [X] `WinZone.actorSpawner` assigned in Inspector.
- [X] Win zone collider is trigger-enabled.
- [X] Confirm "Level Complete!" logs when all active actors enter zone.

## 7) First Training Smoke Test
From project root (`c:/Users/Andrew/SeniorDesign/SeniorDesign`):

- **Easiest (no script policy issues):** run **`Start-MLSmoke.cmd`** (or `Stop-MLSmoke.cmd` to stop).
- **PowerShell blocked?** `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Start-MLSmoke.ps1`
- **Manual Python:**

```bash
.venv-mlagents\\Scripts\\python.exe -m mlagents.trainers.learn Assets/ML-Agents/actor_ppo.yaml --run-id actor-smoke --time-scale 10
```

Then press Play in Unity and verify step count increases and summaries/checkpoints are produced.

If setup is needed on another machine, use pinned packages in `requirements-mlagents.txt`.

## Script-by-Script Review Notes (Current)
- `GameManager`: duplicate singleton guard present.
- `UIManager`: guarded against missing manager/UI refs.
- `PlacementManager`: guarded against missing manager/main camera/null prefabs.
- `FlagTypeButtons` + `FlagButton`: guarded against missing `PlacementManager`/prefabs.
- `ActorBehavior`: grounded check safe when `groundCheck` is unset.
- `ActorSpawner`: now guarded against missing `GameManager`.
- `WinZone`: now guarded against missing `actorSpawner`.
