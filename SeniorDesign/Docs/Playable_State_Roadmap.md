# Playable State Roadmap (MVP)

This checklist tracks what must be finished to reach a clearly playable game loop.

## Core Loop (Must-Have)
- [x] Main Menu -> Level load works for intended playable scene(s).
- [x] Build phase lets player place flags reliably.
- [x] Simulation phase starts from UI and runs actor behavior.
- [x] Win condition triggers clear end-of-run state.
- [x] Fail condition triggers clear end-of-run state.
- [x] Player can restart run and return to menu.

## Gameplay Systems
- [x] Implement/finish win flow in `WinZone.OnWin()` and route through game state.
- [x] Define fail condition (timeout/no completion) and show result in UI.
- [x] Ensure phase controls exist in UI: Start Sim, Restart, Back to Build, Back to Menu.
- [x] Complete scene navigation methods in `PlayMenu` (remove stubs).

## Scene/Inspector Wiring
- [X] Complete all checks in `Docs/Scene_Inspector_Checklist.md`.
- [X] Confirm actor prefab has required ML + movement components and refs.
- [X] Confirm tags/layers are correct (`Agent`, `Ground`, `WinZone`, `MainCamera`).

## Balance/Playtest
- [X] Run 5-10 internal playtests.
- [X] Tune movement speed, jump force, flag strengths, episode length, win zone size.
- [X] Verify no blocker console errors during full loop tests.

## ML (Non-Blocking for MVP)
- [X] Keep scripted path playable even if training is not running.
- [X] Keep trainer + TensorBoard setup documented and reproducible.
- [X] Run smoke training periodically to validate integration.

## Definition of Done (Playable)
- [X] New user can launch from menu, place flags, run sim, see win/lose, restart, and return to menu without manual setup.
- [X] No critical runtime errors in Unity Console during a full session.
