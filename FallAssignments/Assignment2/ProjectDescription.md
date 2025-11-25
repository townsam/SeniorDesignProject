# G.A.S.P.U.M.L — Game About Solving Puzzles Using Machine Learning

## Overview

G.A.S.P.U.M.L is a puzzle game prototype built in Unity that places machine-learning-inspired mechanics at the core of its puzzles. Each level begins with a population of actors exhibiting randomized movement and simple physics-driven behaviors. The player's objective is to influence those behaviors so the actors reach a specified goal.

## Gameplay

- Levels start with multiple autonomous actors that move according to randomized properties.
- The player observes actors and identifies behaviors that move actors closer to the goal.
- Between simulation batches, the player places a limited number of reinforcement tools to bias or copy desirable behaviors across the population.
- After the player places tools, the game runs another batch of simulations; the process repeats until all actors reach the target goal.

## Mechanics

- Actor Properties: Each actor has tunable properties (for example movement speed, turning bias, jump strength) that affect its behavior.
- Randomization: Levels initialize actors with randomized properties to create varied behavior and emergent outcomes.
- Reinforcement Tools: A set of in-level tools the player can place to influence actor properties or reward particular behaviors.
- Batch Simulation: The game advances in discrete simulation batches—players observe results, place tools, then run the next batch.
- Win Condition: A level is completed when all actors reach the level's target goal during a simulation run.

## Implementation Notes

- Engine: Prototype developed in Unity (C#) using simple physics and data-driven actor definitions.
- Systems: Key systems include UI input, Game Logic (replication & reinforcement rules), Actor Simulation, and Goal Checking.

## Progression

Players progress by completing levels of increasing complexity that require more careful observation, tool placement, and strategy to reliably replicate successful behaviors across the actor population.

