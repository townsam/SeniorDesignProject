# SeniorDesign — Handover Log

This file is an append-only engineering handover log.

## How to Use This Log
- Add updates under the current date section.
- Keep entries in chronological order using incrementing entry numbers.
- Keep each entry short and actionable.
- Include changed files and immediate next steps.
- Fast path in Unity: `Tools > Handover > Append Entry Template` (auto-adds date section, increments entry number, inserts template).

## Entry Template
Copy this block for each new handover message:

```md
### Entry NNN — HH:MM
**Summary**
- ...

**Changes**
- ...

**Files Updated**
- path/to/file

**Validation**
- ...

**Next Steps**
- ...

**Blockers / Risks**
- None
```

---

## 2026-03-16

### Entry 001 — ML-Agents Foundation + Modular Flag Effects
**Summary**
- Added a first-pass ML-Agents integration for actor behavior.
- Introduced a modular flag effect system so new flag types can be added without changing core agent logic.

**Changes**
- Added `ActorAgent` with observations, actions, reward shaping, heuristics, and episode reset.
- Added flag influence abstraction, provider contract, provider registry, and radius-based sample flag effect.
- Updated actor behavior to support toggling planned movement for ML-controlled actors.
- Updated actor spawner to reset both scripted actors and ML agents.

**Files Updated**
- `Assets/Scripts/ActorAgent.cs`
- `Assets/Scripts/FlagInfluence.cs`
- `Assets/Scripts/FlagEffectProvider.cs`
- `Assets/Scripts/FlagEffectRegistry.cs`
- `Assets/Scripts/ActorBehavior.cs`
- `Assets/Scripts/ActorSpawner.cs`

**Validation**
- C# error scan run for `Assets/Scripts`; no errors reported.

**Next Steps**
- Add trainer config (`ppo`) and run initial learning loop.
- Configure actor prefab with `Behavior Parameters` + `Decision Requester` + `ActorAgent`.
- Add new concrete flag providers as needed (e.g., directional buffs, one-shot effects, team-specific effects).

**Blockers / Risks**
- Reward tuning and reset timing may need iteration once training starts.

### Entry 002 — Concrete Flag Types + UI Button Helper
**Summary**
- Added two ready-made flag provider scripts (`SlowZoneFlag`, `RewardBoostFlag`) for immediate placement.
- Added UI helper script to auto-generate placement buttons in the Build phase.

**Changes**
- Created `SlowZoneFlag`: reduces actor move speed within radius zone.
- Created `RewardBoostFlag`: multiplies agent rewards and adds continuous bonus within radius zone.
- Created `FlagTypeButtons`: auto-generates UI buttons for both flag types; buttons trigger placement workflow via `PlacementManager`.

**Files Updated**
- `Assets/Scripts/SlowZoneFlag.cs`
- `Assets/Scripts/RewardBoostFlag.cs`
- `Assets/Scripts/FlagTypeButtons.cs`
- `README_HANDOVER.md`

**Validation**
- C# error scan for all three new scripts; no errors reported.

**Next Steps**
- Attach `FlagTypeButtons` to Build UI (assign button container in Inspector).
- Test placement workflow: click buttons → place flags in scene → verify Gizmo radius visualization.
- Extend with more flag types as needed (follow same `FlagEffectProvider` pattern).

**Blockers / Risks**
- None.

## 2026-03-18

### Entry 003 — Project Progress Review (Strengths, Risks, Next Steps)
**Summary**
- Reviewed current project implementation and handover progress to identify what is working well, what is weak/risky, and what should happen next.

**Changes**
- Assessed code architecture and integration across ML agent control, flag effect providers/registry, and build/simulation flow.
- Confirmed strong areas: modular flag system (`FlagEffectProvider` + registry), baseline `ActorAgent` observation/action loop, and clear phase separation in game flow.
- Identified risk areas: no training config present yet, runtime null-risk hotspots (`GameManager.Instance`, `FindObjectOfType<PlacementManager>()`, `groundCheck` assumptions), and unfinished scene-loading stubs in `PlayMenu`.
- Produced a prioritized execution sequence for next implementation steps.

**Files Updated**
- `README_HANDOVER.md`

**Validation**
- C# error scan for `Assets/Scripts` reports no errors.
- Review findings aligned with current script state in `Assets/Scripts` and package setup in `Packages/manifest.json`.

**Next Steps**
- Add trainer configuration (PPO YAML) and run a short end-to-end ML-Agents training smoke test.
- Complete and verify inspector/scene wiring: `Behavior Parameters`, `Decision Requester`, `ActorAgent`, `groundCheck`, tags, and layer masks.
- Add defensive null guards for singleton/manager access paths and improve runtime warnings for missing references.
- Run full gameplay validation pass: Build placement flow → Simulation start/reset loop → Win condition checks.

**Blockers / Risks**
- Main blocker is operational readiness for training loop (missing trainer config + scene wiring).
- Reward shaping/reset tuning likely needs iteration after first training runs.

### Entry 004 — HH:MM
**Summary**
- Started executing the prioritized next steps by hardening runtime safety paths and adding a starter PPO trainer configuration.

**Changes**
- Added defensive singleton handling in `GameManager` to prevent duplicate instance issues.
- Added null-safe guards in UI and placement flow (`UIManager`, `PlacementManager`) for missing manager/camera/UI references.
- Added safe manager/prefab checks in flag selection handlers (`FlagTypeButtons`, `FlagButton`).
- Added safe fallback for missing `groundCheck` in `ActorBehavior.IsGrounded()`.
- Added initial ML-Agents trainer config file at `Assets/ML-Agents/actor_ppo.yaml` with baseline PPO hyperparameters.

**Files Updated**
- `Assets/Scripts/GameManager.cs`
- `Assets/Scripts/UIManager.cs`
- `Assets/Scripts/PlacementManager.cs`
- `Assets/Scripts/FlagTypeButtons.cs`
- `Assets/Scripts/FlagButton.cs`
- `Assets/Scripts/ActorBehavior.cs`
- `Assets/ML-Agents/actor_ppo.yaml`
- `README_HANDOVER.md`

**Validation**
- C# error scan for `Assets/Scripts`; no errors reported after changes.

**Next Steps**
- Match `Behavior Parameters` behavior name in Unity to the YAML behavior key (`ActorBehavior`) or update YAML key to match scene config.
- Run a short training smoke test with `mlagents-learn Assets/ML-Agents/actor_ppo.yaml --run-id actor-smoke --time-scale 10`.
- Complete inspector wiring checklist (`Decision Requester`, `ActorAgent`, `groundCheck`, tags/layers) and verify one full build→simulation loop.

**Blockers / Risks**
- Training execution still depends on local Python ML-Agents environment setup and scene behavior-name alignment.

### Entry 005 — HH:MM
**Summary**
- Continued next-step execution with a script-by-script scene wiring pass and created a practical inspector checklist for Build/Simulation/ML validation.

**Changes**
- Added additional runtime safety guards in two remaining risk points:
	- `ActorSpawner.Update()` now exits safely if `GameManager.Instance` is missing.
	- `WinZone.CheckWinCondition()` now warns/returns if `actorSpawner` is unassigned.
- Added a new checklist document for manual verification and smoke-test flow:
	- `Docs/Scene_Inspector_Checklist.md`
	- Covers scene object wiring, placement flow, actor prefab setup, behavior-name alignment, simulation loop checks, win condition checks, and first training command.

**Files Updated**
- `Assets/Scripts/ActorSpawner.cs`
- `Assets/Scripts/WinZone.cs`
- `Docs/Scene_Inspector_Checklist.md`
- `README_HANDOVER.md`

**Validation**
- C# error scan for `Assets/Scripts`; no errors reported after changes.

**Next Steps**
- Execute the checklist in `SampleScene` and resolve any missing inspector references.
- Ensure `Behavior Parameters > Behavior Name` matches YAML key (`ActorBehavior`).
- Run first training smoke test and capture first observations/checkpoint output.

**Blockers / Risks**
- Remaining blocker is environment readiness for `mlagents-learn` plus any unresolved inspector references found during checklist run.

### Entry 006 — HH:MM
**Summary**
- Set up a working ML-Agents Python training environment in this project and validated trainer startup.

**Changes**
- Detected compatibility issues with the default Python 3.12 venv and created a dedicated Python 3.10 environment at `.venv-mlagents`.
- Installed and validated compatible trainer stack:
	- `mlagents==1.1.0`
	- `protobuf==3.20.3`
	- `setuptools==80.9.0` (required because ML-Agents currently imports `pkg_resources`).
- Verified trainer command works:
	- `.venv-mlagents\\Scripts\\python.exe -m mlagents.trainers.learn --help`
- Added reproducible package pin file: `requirements-mlagents.txt`.
- Updated smoke-test command in checklist docs to use project-local venv interpreter.

**Files Updated**
- `requirements-mlagents.txt`
- `Docs/Scene_Inspector_Checklist.md`
- `README_HANDOVER.md`

**Validation**
- Trainer CLI help command executes successfully in `.venv-mlagents`.

**Next Steps**
- Run smoke test from repo root:
	- `.venv-mlagents\\Scripts\\python.exe -m mlagents.trainers.learn Assets/ML-Agents/actor_ppo.yaml --run-id actor-smoke --time-scale 10`
- Press Play in Unity and confirm environment connects and starts stepping.

**Blockers / Risks**
- None for environment setup; remaining risk is scene wiring/behavior-name mismatch during first run.

### Entry 007 — HH:MM
**Summary**
- Ran the first Unity-connected smoke test, identified behavior-name mismatch, and fixed trainer config so training initializes correctly.

**Changes**
- Started trainer and confirmed Unity Editor connection (`package version 2.0.2`, communication API `1.5.0`).
- Observed runtime trainer failure:
	- `TrainerConfigError: The behavior name My Behavior has not been specified in the trainer configuration.`
- Updated trainer config to include Unity default behavior key by adding a `"My Behavior"` PPO block in `Assets/ML-Agents/actor_ppo.yaml` (kept existing `ActorBehavior` block).
- Relaunched trainer with `--force` and confirmed successful behavior registration and hyperparameter load for `My Behavior`.

**Files Updated**
- `Assets/ML-Agents/actor_ppo.yaml`
- `README_HANDOVER.md`

**Validation**
- Trainer now reaches:
	- `Connected to Unity environment`
	- `Connected new brain: My Behavior?team=0`
	- Hyperparameter printout for `My Behavior`

**Next Steps**
- Continue runtime smoke test and verify first summaries/checkpoints are emitted (current `summary_freq` may delay visible logs).
- Optionally standardize Unity `Behavior Parameters` name to `ActorBehavior` and remove dual-name config later.

**Blockers / Risks**
- No blocker for connection/startup; only observability delay due to summary/checkpoint intervals.

### Entry 008 — HH:MM
**Summary**
- Set up live training observability via TensorBoard and confirmed dashboard availability.

**Changes**
- Started TensorBoard from project ML environment:
	- `.venv-mlagents\\Scripts\\python.exe -m tensorboard.main --logdir results --port 6006`
- Confirmed local dashboard endpoint:
	- `http://localhost:6006/`

**Files Updated**
- `README_HANDOVER.md`

**Validation**
- TensorBoard server reports:
	- `TensorBoard 2.20.0 at http://localhost:6006/`

**Next Steps**
- Keep Unity Play mode + trainer active and monitor scalar charts in TensorBoard.
- If chart updates are too slow for smoke testing, reduce `summary_freq` and `checkpoint_interval` in `Assets/ML-Agents/actor_ppo.yaml`.

**Blockers / Risks**
- None. Non-blocking warnings observed (`pkg_resources` deprecation, reduced feature set without TensorFlow).

### Entry 009 — HH:MM
**Summary**
- Consolidated “what to do next to reach playable state” into a dedicated, visible roadmap checklist.

**Changes**
- Added `Docs/Playable_State_Roadmap.md` as a single source of truth for MVP readiness work.
- Included explicit sections for:
	- Core playable loop requirements
	- Gameplay system completion tasks
	- Scene/Inspector wiring checks
	- Balance/playtest pass
	- ML as non-blocking scope for MVP
	- Playable definition-of-done criteria

**Files Updated**
- `Docs/Playable_State_Roadmap.md`
- `README_HANDOVER.md`

**Validation**
- Roadmap file added and aligned with prior recommendations/checklists.

**Next Steps**
- Use `Docs/Playable_State_Roadmap.md` as the active task board for implementation.
- Mark items complete as each gameplay flow component is finished and verified.

**Blockers / Risks**
- None.

## 2026-03-19

### Entry 010 — HH:MM
**Summary**
- Removed fragile prefab-level scene target dependency by auto-assigning `ActorAgent.target` at runtime from `ActorSpawner`.
- Updated the scene checklist to reflect the new target wiring workflow.

**Changes**
- Added `ActorSpawner.defaultTarget`.
- Added startup fallback in `ActorSpawner` to automatically use `WinZone` transform when `defaultTarget` is not assigned.
- Updated actor spawn flow so each spawned `ActorAgent` receives `agent.target = defaultTarget`.
- Updated checklist item from prefab `ActorAgent.target` assignment to `ActorSpawner.defaultTarget` / `WinZone` fallback verification.
- Added checklist notes clarifying that prefab scene reference assignment for `ActorAgent.target` is no longer required.

**Files Updated**
- `Assets/Scripts/ActorSpawner.cs`
- `Docs/Scene_Inspector_Checklist.md`
- `README_HANDOVER.md`

**Validation**
- C# error scan for `Assets/Scripts/ActorSpawner.cs`; no errors reported.

**Next Steps**
- In Unity, optionally assign an explicit `ActorSpawner.defaultTarget` (e.g., `GoalTarget`) for clarity; otherwise rely on `WinZone` fallback.
- Run Play mode smoke check to confirm spawned agents move toward target and training episodes progress.

**Blockers / Risks**
- If scene does not contain `WinZone` and `defaultTarget` is unassigned, agents will spawn with no target (warning is logged).

### Entry 011 — HH:MM
**Summary**
- Fixed non-moving actors during Simulation by restoring movement when ML control is unavailable.
- Confirmed actors now move in Simulation after the fallback update.

**Changes**
- Updated `ActorAgent.Initialize()` to conditionally enable planned movement fallback instead of always disabling it.
- Added `ShouldUsePlannedMovementFallback()` in `ActorAgent`:
	- Keeps ML control when trainer is connected.
	- Keeps ML/heuristic control when inference model or heuristic-only behavior is configured.
	- Falls back to planned movement when neither trainer nor local ML control is available.
- Added checklist note documenting this expected fallback behavior.

**Files Updated**
- `Assets/Scripts/ActorAgent.cs`
- `Docs/Scene_Inspector_Checklist.md`
- `README_HANDOVER.md`

**Validation**
- C# error scan for `Assets/Scripts/ActorAgent.cs`; no errors reported.
- Runtime verification: actors move during Simulation phase.

**Next Steps**
- Continue win-condition checks (WinZone trigger + "Level Complete!" verification).
- Run/continue trainer smoke test and monitor step progression in TensorBoard.

**Blockers / Risks**
- None for actor movement fallback; remaining risks are checklist items still open in win-condition validation.

### Entry 012 — HH:MM
**Summary**
- Fixed ML-Agents action-buffer crash during smoke testing and improved training process control scripts.
- Added explicit checklist coverage for required action-space configuration.

**Changes**
- Patched `ActorAgent.OnActionReceived()` to safely handle missing continuous action indices instead of throwing `IndexOutOfRangeException`.
- Added one-time warning log in `ActorAgent` when fewer than 3 continuous actions are received.
- Hardened `ActorAgent.Heuristic()` with bounds checks so heuristic writes are safe for mismatched action sizes.
- Added one-command launcher script `Start-MLSmoke.ps1` to start trainer + TensorBoard together.
- Added companion `Stop-MLSmoke.ps1` and improved it to stop TensorBoard reliably by matching listening port owner process in addition to command-line patterns.
- Updated checklist with required `Behavior Parameters` action-space settings (`Continuous`, size `3`).

**Files Updated**
- `Assets/Scripts/ActorAgent.cs`
- `Docs/Scene_Inspector_Checklist.md`
- `Start-MLSmoke.ps1`
- `Stop-MLSmoke.ps1`
- `README_HANDOVER.md`

**Validation**
- C# error scan for `Assets/Scripts/ActorAgent.cs`; no errors reported.
- Runtime validation: prior `IndexOutOfRangeException` path removed; actors move during Simulation.
- Process validation: `Stop-MLSmoke.ps1` successfully stopped active trainer/TensorBoard processes and closed listening port after targeting active TensorBoard port.

**Next Steps**
- In Unity, set actor `Behavior Parameters` to `Space Type = Continuous`, `Continuous Actions = 3`.
- Re-run smoke training with launcher scripts and confirm trainer connection + step progression in TensorBoard.

**Blockers / Risks**
- If action-space settings drift from script expectations, agent control quality may degrade (now warns and fails safe instead of crashing).

---

## 2026-04-06

### Entry 013 — Game loop, progression, Level03, demo docs
**Summary**
- Implemented a full player loop: mission timer, win/lose overlays (Retry / Menu / Next), pause (Esc), build-phase hint line, and flag-button hover tips.
- Added level catalog + PlayerPrefs unlock progression; main menu level buttons respect locks and show display names.
- Added `Level03` (two agents), runtime settings (volume + reset unlocks + credits text), optional `GameSfx` hooks, and demo documentation.

**Changes**
- `LevelFlowController` + `GameSfx` auto-attached from `GameManager`; `WinZone` raises `LevelCompleted` instead of auto-loading the menu.
- `ActorSpawner`: `StopEpisodeLoop()` for end states; horizontal spawn offsets when `actorCount` > 1.
- `PlayMenu` / `MainMenu` / `FlagTypeButtons` updated; new `GameProgression.cs`, `SettingsStore.cs`; `Docs/DEMO_CHECKLIST.md`.

**Files Updated**
- `Assets/Scripts/GameManager.cs`, `WinZone.cs`, `ActorSpawner.cs`, `PlayMenu.cs`, `MainMenu.cs`, `FlagTypeButtons.cs`
- `Assets/Scripts/GameProgression.cs`, `SettingsStore.cs`, `GameSfx.cs`, `LevelFlowController.cs` (+ `.meta`)
- `Assets/Scenes/Level03.unity` (+ `.meta`), `ProjectSettings/EditorBuildSettings.asset`
- `Docs/DEMO_CHECKLIST.md`, `README_HANDOVER.md`

**Validation**
- IDE/linter pass on edited scripts; no new diagnostics reported.

**Next Steps**
- Assign optional audio clips on `GameSfx`; customize `MainMenu` credits string in Inspector.
- Playtest all three levels with heuristic and inference modes; tune `missionTimeLimitSeconds` per scene in `LevelFlowController` if needed.

**Blockers / Risks**
- `GameObject.Find` for menu level buttons only succeeds while those objects are active; unlock labels refresh when the level-select panel enables or after **Reset level unlocks**.

### Entry 014 — Build UI: supplies economy, vertical toolbar, layout polish
**Summary**
- Added a **supply budget** on `PlacementManager` (per-level total, per-flag costs, refund on Delete) with `EconomyChanged` / `SelectedPrefabChanged` events.
- Rebuilt **flag toolbar** as a **right-docked vertical strip**: square **72×72** tool buttons with centered **glyph** (+ / S / R), cost/count on the tile, and **hover name** to the right of the square plus bottom hint text.
- Fixed **delegate mismatch**: `EconomyChanged` uses `RefreshToolbar()`; `SelectedPrefabChanged` uses `OnSelectedPrefabChanged`.
- **LevelFlow** build hint moved **upper-left** (avoids overlap with flag hints); win/lose button spacing fixed; **lose** panel uses two centered buttons when there is no **Next**.
- New **`ToolbarUiSprites`** (sliced tile for icon backgrounds). **`PlacementManager`** / **scenes** tuned with `toolBudgetTotal`, costs, and caps per level.

**Changes**
- `PlacementManager`: `toolBudgetTotal`, `slowPlacementCost`, `boostPlacementCost`; spend/refund; `SelectObject` only fires `SelectedPrefabChanged` when prefab actually changes.
- `FlagTypeButtons`: `ApplyRightDockLayout`, runtime rows + `WireRowHoverAndHint`; optional `dockToRightEdge` / width / margin fields.
- `LevelFlowController`: build hint copy; result button layout; `FlagTypeButtons` hint line inset adjusted earlier for toolbar width.

**Files Updated**
- `Assets/Scripts/PlacementManager.cs`, `FlagTypeButtons.cs`, `LevelFlowController.cs`
- `Assets/Scripts/ToolbarUiSprites.cs` (+ `.meta`)
- `Assets/Scenes/SampleScene.unity`, `Level01.unity`, `Level03.unity` (PlacementManager economy fields)
- `Docs/DEMO_CHECKLIST.md` (supplies / toolbar wording)

**Validation**
- Script compile: `Action` vs `Action<GameObject>` subscription split on `FlagTypeButtons`.
- Linter pass on touched scripts.

**Next Steps**
- If the right toolbar overlaps HUD (`LevelFlowCanvas`), raise `edgeInset` / lower `toolbarWidth` on `FlagTypeButtons` or shift HUD.
- Optional: assign real sprites for tool icons instead of letter glyphs.

**Blockers / Risks**
- `dockToRightEdge` rewrites `buttonContainer` `RectTransform` anchors; disable or adjust if a scene relied on the old bottom-only layout.

### Entry 015 — Flag toolbar layout (no overlap)
**Summary**
- Rebuilt the build-phase toolbar from **fixed-size vertical cards**: each tool is a **column stack** (title row → **64×64** square → **stats line**). Cost and uses were **moved out of the square** so glyphs no longer fight badge text.
- **Narrow dock** (`columnWidth` ~112px default) replaces the wide strip; **no horizontal spacers** or side-by-side hover labels that caused overlap.
- **Hover**: title line swaps **short → long** name (`Slow` → `Slow Zone`); bottom hint still shows the full tip.

**Files Updated**
- `Assets/Scripts/FlagTypeButtons.cs`
- `README_HANDOVER.md`

**Next Steps**
- Tweak `columnWidth`, `squareSize`, `slotSpacing` in the Inspector if a skin or resolution needs more room.

**Blockers / Risks**
- Renamed inspector field from `toolbarWidth` to **`columnWidth`**; reopen scenes and confirm values if anything looked off after upgrade.

### Entry 016 — ML smoke launchers without script policy friction
**Summary**
- Added **`Start-MLSmoke.cmd`** and **`Stop-MLSmoke.cmd`** that call the existing `.ps1` files with **`-ExecutionPolicy Bypass`**, so training can be started from Explorer or **cmd** when direct `.ps1` execution is blocked.
- Documented the same in **`Docs/DEMO_CHECKLIST.md`** and **`Docs/Scene_Inspector_Checklist.md`**.

**Files Updated**
- `Start-MLSmoke.cmd`, `Stop-MLSmoke.cmd`
- `Docs/DEMO_CHECKLIST.md`, `Docs/Scene_Inspector_Checklist.md`, `README_HANDOVER.md`

### Entry 017 — Visible training feedback (TensorBoard + launcher hints)
**Summary**
- Reduced **`summary_freq`** to **500** and **`checkpoint_interval`** to **5000** in `actor_ppo.yaml` so TensorBoard and checkpoints appear soon during smoke runs (tune back up for long production runs if desired).
- **`Start-MLSmoke.ps1`** now prints where to look: **trainer PowerShell window**, TensorBoard delay/run folder, and Unity **Play + Simulation** + **Behavior Type = Default**.

**Files Updated**
- `Assets/ML-Agents/actor_ppo.yaml`, `Start-MLSmoke.ps1`, `Docs/DEMO_CHECKLIST.md`, `README_HANDOVER.md`

### Entry 018 — Start-MLSmoke default `--force` (run ID collision)
**Summary**
- `Start-MLSmoke.ps1` now appends **`--force`** by default so repeat smoke runs don’t hit `UnityTrainerException` when `results\<run-id>` already exists.
- **` -Resume`** continues training; **`-NoOverwrite`** skips both flags (old strict behavior).

**Files Updated**
- `Start-MLSmoke.ps1`, `Docs/DEMO_CHECKLIST.md`, `README_HANDOVER.md`

---

## 2026-04-07

### Entry 019 — Stable ML trainer venv (torch cap, setup script)
**Summary**
- **Long-term fix** for PyTorch 2.9+ ONNX export (`dynamo=True` → `onnxscript`) conflicting with ml-agents **onnx==1.15** / **protobuf&lt;3.21**: pin **`torch>=2.1.1,<2.9`** in `requirements-mlagents.txt` (resolves to e.g. **2.8.x**).
- **Reverted** in-place edit of `site-packages/mlagents/.../model_serialization.py` so installs are defined by requirements, not a forked venv.
- Added **`Setup-MLAgentsVenv.ps1`** + **`Setup-MLAgentsVenv.cmd`** (create venv + `pip install -r requirements-mlagents.txt`).
- **`tools/patch_mlagents_onnx_dynamo_false.py`** kept only for teams that **must** use PyTorch 2.9+ (adds `dynamo=False` to ml-agents export).
- **`Start-MLSmoke.ps1`** error text points to setup script when venv is missing; **`Docs/DEMO_CHECKLIST.md`** documents setup.

**Files Updated**
- `requirements-mlagents.txt`, `Setup-MLAgentsVenv.ps1`, `Setup-MLAgentsVenv.cmd`, `tools/patch_mlagents_onnx_dynamo_false.py`, `Start-MLSmoke.ps1`, `Docs/DEMO_CHECKLIST.md`, `README_HANDOVER.md`

**Validation**
- `pip install -r requirements-mlagents.txt` in `.venv-mlagents`: **torch 2.8.0** installed; protobuf/onnx pins satisfied.

**Next Steps**
- After any `pip install -U mlagents` or torch upgrade, re-run **`Setup-MLAgentsVenv.ps1`** or `pip install -r requirements-mlagents.txt` to restore caps.

**Blockers / Risks**
- **torch&lt;2.9** may lag newest PyTorch features; upgrade path is Unity/ml-agents releases or the optional patch script + newer onnx/protobuf when officially supported.

### Entry 020 — Unity Version Control: ignore venv + PackageCache noise
**Summary**
- Extended **`ignore.conf`** so UVCS/Plastic stops offering huge trees: **`.venv-mlagents` / `.venv`** (dot-folders need name + trailing-slash rule), **`results/`**, **`__pycache__`**, **`*.pyc`**, explicit **`Library/PackageCache`**, and **`packages-lock.json.private.*` / `manifest.json.private.*`** merge sidecars.

**Files Updated**
- `ignore.conf`, `README_HANDOVER.md`

**Next Steps**
- If **Library** or **.venv-mlagents** was already **checked in**, ignore rules only affect *new* detection: in Unity Version Control / Plastic, **remove those paths from source control** (delete from repo / undo add) without deleting local files, then refresh pending changes.

**Blockers / Risks**
- If any `**` pattern is rejected by an older Plastic client, delete that line and rely on the `.venv-mlagents` / `.venv` / `results` entries.

### Entry 021 — UX polish + ML workflow hardening
**Summary**
- Camera controls: added **R to reset view** and **WASD panning**; documented controls in build hint text.
- Phase HUD: fixed overlap with the in-scene Simulate button by moving phase info and introducing a unified **top-center** action button that switches between **Simulate** (build) and **Back to build** (simulation).
- Levels/menu: added `Level02` to build settings; fixed scene name mapping; added a toggle to **unlock all levels** for quick testing.
- ML training: fixed action/observation frame mismatch (local vs world), improved reward shaping for **closing on WinZone**, ensured decisions are requested during training, and added training-time episode step cap for more consistent summaries.
- Results management: added scripts to keep only “best + current”, added TensorBoard `-TensorBoardLogDir` option, and added in-menu run delete + prune controls.

**Changes**
- **Camera**
	- `CameraController`: capture baseline on start; `R` resets orbit target/angles/distance; **WASD** pans camera target on ground plane (`keyboardPanSpeed`).
- **HUD / Level flow**
	- `LevelFlowController`: runtime **PhaseActionButton** (top-center), disables scene `BuildUI/Start`, phase label sits below action; build hint updated (WASD/R + Simulate location).
	- Phase strip no longer overlaps scene UI.
- **Levels**
	- `ProjectSettings/EditorBuildSettings.asset`: added `Assets/Scenes/Level02.unity` to scenes-in-build.
	- `GameProgression (LevelCatalog)`: updated scene/display names; `LevelProgress.UnlockAllLevels()` helper.
	- `MainMenu`: `unlockAllLevelsForNow` toggle (default true) to unlock for testing; refreshes `PlayMenu` buttons.
- **Gameplay**
	- `ActorSpawner`: spawn spacing now configurable (`spawnSlotSpacing`, default 0 = one spot); added explicit WinZone target resolution diagnostics (warns on multiple WinZones; logs chosen target).
- **ML**
	- `ActorAgent`: use **unscaled clock** for episode timing during training; training-only `MaxStep` cap (`maxTrainingDecisionSteps`); auto-add `DecisionRequester` during trainer connection; fixed movement actions to be **local**; added progress-based shaping (`progressRewardScale`, `stepPenalty`); added warning if no flag providers are registered during training.
	- `FlagEffectRegistry`: added `GetActiveProviderCount()` for diagnostics.
	- Added `MlTrainingFlagSeeder`: when trainer connected, can **activate pre-placed flags** or **spawn flag prefabs along spawn→goal** path so flags actually influence observations/reward.
- **ML results / TensorBoard**
	- `Start-MLSmoke.ps1`: added `-TensorBoardLogDir` to point TensorBoard at a specific run folder; clarified output/hints.
	- `Assets/ML-Agents/actor_ppo.yaml`: reduced `keep_checkpoints` to **2** per run folder.
	- Added `Promote-MlRun.ps1` (copy results\<run> → results\best) and `Prune-MlResults.ps1` (delete all runs except best/current).
	- `TrainingRunsBrowser`: added per-run **Delete** with confirm dialog and **Prune runs** (keeps best/current). Also includes a safety toggle to prevent deleting best/current by default.
- **Warnings cleanup**
	- Replaced deprecated `FindObjectOfType<T>()` usages with `FindFirstObjectByType<T>()`.
	- Made `PlacementManager.ClearHighlight()` public to support UI and phase action flow.

**Files Updated**
- `Assets/Scripts/CameraController.cs`
- `Assets/Scripts/LevelFlowController.cs`
- `Assets/Scripts/ActorSpawner.cs`
- `Assets/Scripts/ActorAgent.cs`
- `Assets/Scripts/FlagEffectRegistry.cs`
- `Assets/Scripts/MlTrainingFlagSeeder.cs`
- `Assets/Scripts/TrainingRunsBrowser.cs`
- `Assets/Scripts/PlacementManager.cs`
- `Assets/Scripts/FlagButton.cs`
- `Assets/Scripts/MainMenu.cs`
- `Assets/Scripts/GameProgression.cs`
- `Assets/ML-Agents/actor_ppo.yaml`
- `ProjectSettings/EditorBuildSettings.asset`
- `Start-MLSmoke.ps1`
- `Promote-MlRun.ps1`, `Prune-MlResults.ps1`
- `README_HANDOVER.md`

**Validation**
- C# error scan via editor build showed no compile errors after changes; lints reported clean for touched scripts during edits.

**Next Steps**
- Add `MlTrainingFlagSeeder` to whichever level scene(s) you train in and assign flag prefabs (`Assets/Prefabs/*Flag_Prefab.prefab`) to ensure training sees non-neutral influences.
- Once demo-ready, turn off `MainMenu.unlockAllLevelsForNow` to restore progression.
- If desired, add a “Promote current → best” button in the TrainingRunsBrowser to match the prune workflow.

### Entry 022 — Reduce ONNX export frequency
**Summary**
- Increased ML-Agents `checkpoint_interval` from **5000 → 50000** so `.onnx` exports happen 10× less often (reduces training slowdowns from frequent model serialization).

**Files Updated**
- `Assets/ML-Agents/actor_ppo.yaml`

### Entry 023 — WinZone auto-wiring + final ML stabilizers
**Summary**
- `WinZone` now auto-finds `ActorSpawner` when the inspector reference is missing (removes “actorSpawner reference is not assigned” warnings).
- Additional ML stabilizers: progress shaping tuned, training-time diagnostics for near-zero progress, and planned-movement fallback stays synced even if the trainer connects after scene start.

**Files Updated**
- `Assets/Scripts/WinZone.cs`
- `Assets/Scripts/ActorAgent.cs`

### Entry 024 — Inference mode toggle (Editor)
**Summary**
- Added an Editor-friendly **inference mode** you can trigger from the Main Menu: it copies the newest `.onnx` under `results\best\` into `Assets/ML-Agents/Models/`, imports it as an `NNModel`, and switches all agents with `BehaviorName="ActorBehavior"` to **Inference Only**.
- Added a companion button to switch agents back to **Default** behavior (trainer / heuristic).

**Files Updated**
- `Assets/Scripts/InferenceModeController.cs`
- `Assets/Scripts/MainMenu.cs`
- `README_HANDOVER.md`