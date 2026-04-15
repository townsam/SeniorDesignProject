## G.A.S.P.U.M.L — Test Plan

This document describes how we test the current Unity project implementation to verify **core gameplay**, **UI/UX**, **persistence/progression**, and (optionally) **ML-Agents training/inference workflows**.

---

## Scope
### In scope (what we actively test)
- **Core gameplay loop**: Build → Simulate → Win/Lose → Retry/Menu/Next
- **UI flows**: main menu, level select, in-level overlays, pause
- **Tool system**: flag placement, limits, supply budget, delete/refund (where enabled)
- **Level progression**: unlocks persist across restarts (PlayerPrefs)
- **Scene correctness**: required objects wired; levels load and are completable
- **Optional ML workflows**: trainer connection smoke test, TensorBoard visibility, inference toggle

### Out of scope (not required to demonstrate progress)
- Automated unit tests for every script (Unity Test Framework is available, but not the primary proof-of-progress)
- “Only solvable with intended solution” constraints (levels may have multiple valid solutions)
- Long-horizon ML performance claims (training quality is iterative and hardware/time dependent)

---

## Test environments
- **Primary**: Unity Editor `6000.2.7f2` on macOS (see `../SeniorDesign/ProjectSettings/ProjectVersion.txt`)
- **Secondary** (optional): Windows editor run-through (to validate parity)
- **Optional trainer**: Python 3.10.x + project ML venv (see `../SeniorDesign/README_MAC_SETUP.md`)

---

## Test data / artifacts
- Scenes:
  - `../SeniorDesign/Assets/Scenes/MainMenu.unity`
  - `../SeniorDesign/Assets/Scenes/Level01.unity`
  - `../SeniorDesign/Assets/Scenes/Level02.unity`
  - `../SeniorDesign/Assets/Scenes/Level03.unity`
- Demo checklist: `../SeniorDesign/Docs/DEMO_CHECKLIST.md`
- Scene wiring checklist: `../SeniorDesign/Docs/Scene_Inspector_Checklist.md`

---

## Pass/Fail criteria (high level)
A build is considered **demo-ready** when:
- All listed levels can be started from the menu and run without exceptions.
- Build/Simulate loop functions and produces a win/lose state with correct UI.
- Tools obey **supplies** and **use limits**; placement cannot softlock the UI.
- Level completion updates progression and remains after restarting Play mode (or restarting the app in a build).
- Optional ML smoke run can connect (if run) without crashing; inference toggle works when a model is present.

---

## Test cases

### Test 1 — Main menu + level select UI
- **Purpose**: Verify menu navigation and level loading.
- **Steps**:
  - Open `MainMenu` and press Play.
  - Navigate to level select.
  - Select each level and confirm it loads.
- **Expected**:
  - Buttons respond correctly.
  - Correct scene loads; no missing-reference spam.
- **Case**: Normal
- **Box**: Black box
- **Type**: Functional
- **Scope**: Integration

### Test 2 — Build phase toolbar + hover/help text
- **Purpose**: Verify build UI renders correctly and communicates tool info.
- **Steps**:
  - Enter a level and remain in Build phase.
  - Hover tools; verify name swaps/hover hints appear as designed.
  - Confirm supplies/cost/uses display makes sense for that level.
- **Expected**:
  - Toolbar is visible, not overlapping critical HUD.
  - Hover feedback works; no layout break at common resolutions.
- **Case**: Normal
- **Box**: Black box
- **Type**: Functional (UI)
- **Scope**: Integration

### Test 3 — Tool placement rules + selection workflow
- **Purpose**: Verify tools can be selected and placed only where allowed.
- **Steps**:
  - Select each available tool.
  - Attempt placement on valid surfaces and invalid areas (off-map, blocked regions, etc.).
  - Confirm selection state updates (selected tool highlight/indicator).
- **Expected**:
  - Tools place where intended; invalid placement is prevented gracefully.
  - No “stuck selected tool” state; player can switch tools or cancel.
- **Case**: Normal
- **Box**: Black box
- **Type**: Functional
- **Scope**: Integration

### Test 4 — Supplies economy (spend / refund / bounds)
- **Purpose**: Verify the supplies budget and refund logic.
- **Steps**:
  - Note starting supplies.
  - Place tools until supplies are insufficient.
  - Attempt to place one more tool.
  - Delete a placed tool (if supported) and verify refund.
- **Expected**:
  - Supplies decrease on placement, never go negative.
  - Placement is blocked when supplies are insufficient.
  - Refund restores supplies correctly and does not exceed maximum budget rules (if any).
- **Case**: Boundary (min/0 supplies)
- **Box**: White box (inspect counters/state if needed)
- **Type**: Functional
- **Scope**: Integration

### Test 5 — Tool usage limits (per-tool caps)
- **Purpose**: Ensure limited-use tools stop being placeable once exhausted.
- **Steps**:
  - Place a tool until its allowed uses are consumed.
  - Try placing it again.
  - Verify other tools still work.
- **Expected**:
  - Tool is disabled/blocked when out of uses.
  - UI communicates that the tool is unavailable.
- **Case**: Boundary
- **Box**: Black box
- **Type**: Functional
- **Scope**: Integration

### Test 6 — Actor spawning + simulation stepping
- **Purpose**: Verify actors spawn and simulation progresses.
- **Steps**:
  - Start simulation in each level.
  - Observe that actors spawn and begin moving.
- **Expected**:
  - No missing-target / missing-manager hard failures.
  - Actors move (including planned-movement fallback when ML control isn’t active).
- **Case**: Normal
- **Box**: White box (validate required objects exist)
- **Type**: Functional
- **Scope**: Integration

### Test 7 — Win/Lose overlays + phase transitions
- **Purpose**: Validate end-state UI and state changes.
- **Steps**:
  - Win a level (reach WinZone) and confirm win overlay.
  - Trigger a loss (time-out) and confirm lose overlay.
  - Use Retry/Menu/Next buttons and verify behavior.
- **Expected**:
  - Correct overlay appears.
  - Buttons route correctly; no duplicate scene loads; state resets on retry.
- **Case**: Normal
- **Box**: Black box
- **Type**: Functional
- **Scope**: Integration

### Test 8 — Pause behavior
- **Purpose**: Verify pause does not break the loop or UI.
- **Steps**:
  - During build and during simulation, press Esc to pause/unpause.
  - Interact with UI where permitted.
- **Expected**:
  - Pause state toggles reliably and does not softlock input.
  - Returning from pause restores expected controls.
- **Case**: Normal
- **Box**: Black box
- **Type**: Functional
- **Scope**: Integration

### Test 9 — Level progression persistence
- **Purpose**: Verify unlock/progression persists across restarts.
- **Steps**:
  - Complete a level.
  - Exit play mode and re-enter play mode (and/or restart a built app if applicable).
  - Confirm the next level is unlocked as expected.
  - Use “reset unlocks” (if present) and verify locks re-apply.
- **Expected**:
  - Unlock state persists and correctly updates menu buttons/labels.
  - Reset restores baseline state.
- **Case**: Normal
- **Box**: White box (persistence layer)
- **Type**: Functional
- **Scope**: Integration

### Test 10 (Optional) — ML trainer connection smoke test
- **Purpose**: Ensure ML-Agents training can connect and run without crashes.
- **Steps**:
  - Follow `../SeniorDesign/README_MAC_SETUP.md` to set up the venv and start trainer + TensorBoard.
  - Press Play in Unity, start simulation, confirm trainer connection.
- **Expected**:
  - Trainer connects to Unity environment.
  - No action-buffer exceptions; TensorBoard shows a run folder updating eventually.
- **Case**: Normal
- **Box**: White box (logs + trainer output)
- **Type**: Functional (workflow)
- **Scope**: Integration

### Test 11 (Optional) — Inference mode toggle
- **Purpose**: Validate switching agents to inference-only mode works when a model is present.
- **Steps**:
  - Ensure a recent `.onnx` exists under `results/best/` (or equivalent workflow).
  - Use the menu inference toggle to import/copy model to `../SeniorDesign/Assets/ML-Agents/Models/`.
  - Verify agents switch to inference-only and still step in simulation.
- **Expected**:
  - No import errors; agents’ behavior type updates correctly.
  - Simulation runs without trainer attached.
- **Case**: Normal
- **Box**: White box (asset import + behavior settings)
- **Type**: Functional (workflow)
- **Scope**: Integration

---

## Test matrix (summary)

| Test | Title | Case | Box | Type | Scope |
|---:|---|---|---|---|---|
| 1 | Main menu + level select UI | Normal | Black box | Functional | Integration |
| 2 | Build toolbar + hover/help | Normal | Black box | Functional | Integration |
| 3 | Tool placement rules + selection | Normal | Black box | Functional | Integration |
| 4 | Supplies economy (spend/refund) | Boundary | White box | Functional | Integration |
| 5 | Tool usage limits | Boundary | Black box | Functional | Integration |
| 6 | Actor spawning + simulation stepping | Normal | White box | Functional | Integration |
| 7 | Win/Lose overlays + transitions | Normal | Black box | Functional | Integration |
| 8 | Pause behavior | Normal | Black box | Functional | Integration |
| 9 | Progression persistence | Normal | White box | Functional | Integration |
| 10 | ML trainer smoke (optional) | Normal | White box | Functional | Integration |
| 11 | Inference toggle (optional) | Normal | White box | Functional | Integration |

---

## Reporting
When executing this plan, record:
- Unity version + OS
- Scene tested
- Pass/Fail per test case
- Any errors from Console (copy the first relevant exception + stack trace)
- Screenshots for UI issues (overlap/layout), if needed

