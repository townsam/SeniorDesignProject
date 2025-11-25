# Design Diagram Description

## Diagram Conventions

- **Boxes / Figures**: Represent components, modules, or actors within the game system (for example: the Player, the Game Engine, or subsystems such as the UI).
- **Lines with Arrows**: Represent the flow of data, actions, or information between components. The arrow indicates the direction of flow (for example, from an input device to the Game Engine).

## Design D0 — High-Level View

**Purpose:** Provides the highest-level view of the system, showing the core interaction between the Player and the Replicate Game System.

- **Inputs:** Player inputs such as mouse clicks and keyboard commands.
- **Outputs:** Visual feedback on-screen showing actors' behavior and the overall game state.
- **Diagram Description:** The player interacts with the game system. The system receives the player's inputs and responds by updating and rendering the game state as visual output.

## Design D1 — Elaboration of Subsystems

**Purpose:** Breaks down the Replicate Game System into its major subsystems: User Interface (UI), Game Logic, and Actor Simulation.

### Subsystems

- **User Interface (UI):** Handles player input, selection, and displays information.
- **Game Logic:** Processes game rules, tracks puzzle/state, and manages the replication mechanic.
- **Actor Simulation:** Manages individual actors, randomized behavior, movement, and physics.

**Data Flow / Diagram Description:** The player's input (for example, selecting an actor) is received by the UI. The UI sends commands to the Game Logic, which updates the Actor Simulation. The Actor Simulation produces actor positions and behaviors that are fed back to the UI for display.

## Design D2 — Detailed System View

**Purpose:** Shows a detailed view of data and processes within each subsystem, including how randomization, replication, physics, and goal checks interact.

- **Inputs:** Mouse Click (Select), Button Press (Replicate), Keyboard Input.
- **Outputs:** Visual feedback, actor movement, level status, and sound effects.

### Data Flow

- Actor properties (for example `moveSpeed`, `jumpStrength`) are randomized during level setup.
- When the player selects an actor (Mouse Click), the UI signals the Game Logic to identify that actor.
- When the player triggers Replicate (Button Press), the Replicate command takes the selected actor's properties and applies them to other actors.
- The Physics Engine uses the updated properties to compute actor movement and interactions.
- The Goal Check module monitors whether all actors have met the Level Goal and reports status to the Game Logic and UI.

**Diagram Description:** Player interactions with the UI trigger events handled by the Game Logic. Replication updates actor properties, which the Physics Engine uses to simulate movement. The Goal Check continually evaluates completion criteria; results are rendered to the player as visual and audio feedback.

---