# Demo checklist (build + ML-Agents)

Use this before a presentation, expo, or submission build.

## Quick play (no trainer)

1. Open `MainMenu` in the Editor (or run a standalone build; the first scene in Build Settings should be `MainMenu`).
2. Press **Play**, choose **Play**, then **Tutorial** (unlocks first; others unlock after wins).
3. Use the **tool strip** (boxed slots): **Select** is free; Slow/Boost show **supply cost** and **uses left**. Placing spends supplies; **Delete** on a selected flag **refunds** it. **Start** simulation when ready.
4. Goal: get **every** agent into the **green win zone** before **Time left** hits zero.
5. **Esc** pauses during simulation. Win/lose panels offer **Retry**, **Menu**, and **Next level** (when applicable).

## Progression and settings

- **Level unlocks** persist in `PlayerPrefs` (`level_max_unlocked_index`). Use **Settings → Reset level unlocks** on the main menu to return to Tutorial-only.
- **Master volume** is in **Settings** (slider is created at runtime under `SettingsMenu`).
- Optional **SFX**: assign clips on the `GameSfx` component (same object as `GameManager` in gameplay scenes) for win / lose / UI click.

## Heuristic vs trained policy

- For a **keyboard demo**, set the actor prefab’s **Behavior Parameters** to **Heuristic Only** and use **WASD + Space** (see `ActorAgent.Heuristic`).
- For **inference**, use **Inference Only** and assign your trained **Model** (`.onnx`). If no communicator and no model, the project falls back to **planned movement** when appropriate (see `ActorAgent` logs on play).

## ML training smoke (optional)

- Python env: run **`Setup-MLAgentsVenv.ps1`** once (or **`Setup-MLAgentsVenv.cmd`**) to create `.venv-mlagents` from **`requirements-mlagents.txt`** (keeps **PyTorch below 2.9** so ONNX checkpoints work with ml-agents’ pinned `onnx` / `protobuf`). Re-run after cloning or if `pip install -U torch` breaks checkpoint export.
- **If `.ps1` scripts won’t run** (execution policy): double-click **`Start-MLSmoke.cmd`** / **`Stop-MLSmoke.cmd`**, or from **Command Prompt** run:
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File Start-MLSmoke.ps1`
- Smoke launcher **defaults to `--force`** so re-runs don’t fail on existing `results\actor-smoke`. Use **`-Resume`** to continue the same run, **`-NoOverwrite`** if you want a hard error when the folder exists, or **`-RunId myname`** for a separate folder.
- Otherwise: `Start-MLSmoke.ps1` and `Stop-MLSmoke.ps1` from PowerShell. Confirm **Behavior Parameters** use **3 continuous actions** (see `Docs/Scene_Inspector_Checklist.md`).

**Where “training” shows up**

1. A **second PowerShell window** (trainer): look for a connection to Unity and **step** / **environment** messages — not the small `cmd` window that only launched it.
2. **TensorBoard** in the browser: scalars need some steps first (`summary_freq` in `actor_ppo.yaml` is **500** for quick feedback). Pick run **`actor-smoke`** (or your `--run-id`) in the left sidebar if the page looks empty; click **Refresh**.
3. **Unity**: **Play** + **Start** (simulation). With **Heuristic Only**, the trainer does not learn — use **Default** on **Behavior Parameters** while training.

## Build Settings sanity

Scenes should include, in order: `MainMenu`, `Level01`, `SampleScene`, `Level03` (or your replacements). The first scene in the list is the startup scene for builds.
