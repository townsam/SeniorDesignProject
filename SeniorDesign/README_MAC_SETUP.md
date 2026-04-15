# Mac setup (Unity + ML-Agents)

## 1) Install Unity (required)
- Install **Unity Hub**.
- In Unity Hub, install **Unity Editor `6000.2.7f2`** (this project’s required version from `ProjectSettings/ProjectVersion.txt`).
- In Unity Hub, click **Open** and select this project folder (`SeniorDesign/`).
- Let Unity import packages; first open can take a while.

## 2) Run the game (no ML trainer)
- Open the **startup scene** (`MainMenu`) and press **Play**.
- Demo flow details are in `Docs/DEMO_CHECKLIST.md`.

## 3) ML-Agents trainer (optional)

### Install Python
You need **Python 3.10.12** (recommended) because this repo pins **`mlagents==1.1.0`**, which only publishes wheels for **Python 3.10.1–3.10.12**.

Common options:
- **pyenv (recommended)**: `pyenv install 3.10.12` then `pyenv local 3.10.12`
- Homebrew Python: `brew install python@3.10` (note: Homebrew may install 3.10.13+ which is **too new** for `mlagents==1.1.0`)

### Create the venv + install pinned packages
From repo root:

```bash
chmod +x Setup-MLAgentsVenv.sh Start-MLSmoke.sh Stop-MLSmoke.sh
PYTHON=python3.10 ./Setup-MLAgentsVenv.sh
```

### Start a smoke training run + TensorBoard
```bash
./Start-MLSmoke.sh
open "http://localhost:6006/"
```

Then in Unity:
- Press **Play**
- Press **Start/Simulate**
- Ensure the agent prefab `Behavior Parameters` is **Behavior Type = Default** while training

To stop:

```bash
./Stop-MLSmoke.sh
```

## 4) Common “it doesn’t connect” checklist
- Unity package `com.unity.ml-agents` is present (see `Packages/manifest.json`).
- Trainer command is running (check `Logs/ml-trainer-actor-smoke.log`).
- In Unity, you actually entered Simulation/Start (agents must step for training to progress).
- Behavior name matches trainer config key (see `Docs/Scene_Inspector_Checklist.md`).
