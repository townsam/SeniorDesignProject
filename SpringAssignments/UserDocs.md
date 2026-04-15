## G.A.S.P.U.M.L — User Guide & Manual

G.A.S.P.U.M.L is a puzzle/simulation game where you **don’t directly control** the actors. Instead, you influence outcomes by placing tools (flags) during a build phase and then running a simulation to see what happens.

The project includes optional Unity ML-Agents training, but the game is designed to be approachable and playable **without any machine learning background**.

---

## What’s the goal?
Each level’s objective is to get the actors to the **WinZone** (goal) before time runs out.

You repeat this loop:
- **Build**: spend limited supplies to place tools (flags)
- **Simulate**: run the level and observe results
- **Adjust**: refine tool placement until you can consistently win

---

## Who this is for
- **Puzzle / strategy players** who enjoy experimentation
- **Students** who want an intuitive introduction to reinforcement-style ideas
- **Reviewers / graders** who need clear run steps and a quick way to see progress

---

## Getting started (Unity Editor)
### Requirements
- Unity Editor **6000.2.7f2** (required by this project)

### Run steps
1. Open the project folder in Unity Hub / Unity Editor.
2. Open the start scene: `../SeniorDesign/Assets/Scenes/MainMenu.unity`
3. Press **Play** in the Unity Editor.

For the “demo-ready” flow, use: `../SeniorDesign/Docs/DEMO_CHECKLIST.md`

---

## Starting a level
From the Main Menu:
1. Select a level.
2. You begin in **Build** mode.
3. Place tools (flags) while watching your **supplies**.
4. Press **Simulate** to run the level.

---

## How the game works (Build ↔ Simulate)
### Build phase
- Place tools from the toolbar.
- Each tool typically has:
  - **cost** (spends supplies)
  - **limited uses** (varies by level)
- Some levels allow deleting placed tools for a **refund** (depending on settings).

### Simulation phase
- Actors spawn and begin moving.
- Your placed tools influence what happens.
- The level ends when:
  - **Win**: actors reach the WinZone (goal), or
  - **Lose**: time expires (or the level’s lose condition triggers)

After win/lose, you can **Retry**, return to **Menu**, or go **Next** (when available).

---

## Controls
### Camera
- **W/A/S/D**: pan the camera
- **R**: reset the camera view

### Pause
- **Esc**: pause / unpause

---

## Tools (Flags)
Exact tools vary by level, but examples include:
- **Slow Zone**: reduces actor speed inside its radius
- **Reward Boost**: increases rewards / encourages behavior inside its radius (especially relevant for training demos)

Strategy tip: use a few flags at **key decision points** (tight corners, chokepoints, just before the goal) instead of trying to “paint a whole path.”

---

## Optional (advanced): ML-Agents training
If you want to run training, follow:
- `../SeniorDesign/README_MAC_SETUP.md` (trainer setup on macOS)
- `../SeniorDesign/Docs/Scene_Inspector_Checklist.md` (scene wiring + common mistakes)

---

## FAQ / Troubleshooting
### Do I need machine learning knowledge to play?
No. You can play by observing, experimenting, and iterating on tool placement.

### Why do the actors sometimes look random?
Variation is part of the puzzle. Your job is to set up the level so that the population reliably reaches the goal.

### Is there only one correct solution per level?
No. Multiple tool layouts can work; the fun is in discovering stable solutions.

### I pressed Play, but nothing seems to happen.
- Make sure you’re in a level (not just the menu).
- Start **Simulate** from the build phase button.

### The project won’t open / takes a long time to import.
First open can take a while because Unity generates caches. If it’s stuck, confirm you’re using the correct Unity version (**6000.2.7f2**).