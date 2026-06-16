# CODEX_TASK.md

## Game Concept

The game is a choice-based text strategy game inspired by university life.

Core loop:

1. Show an event card.
2. Player chooses left or right.
3. Choice changes five core stats.
4. Normal cards appear most of the time.
5. After several normal cards, a special story card appears.
6. Endings are determined by stats and story flags.

## Core Stats

* Body
* Mental
* Academic
* Social
* Money

Initial value: 50
Minimum: 0
Maximum: 100

If any stat reaches 0 or below, stop gameplay and trigger a matching failure ending.

## First Demo Scope

Implement only:

1. Core stat system
2. Event card data structure
3. Normal card deck
4. Special card deck
5. Basic card progression
6. Choice effects
7. Failure ending check
8. Minimal UI for testing
9. Mouse drag left/right card selection

Do not implement final art, save/load, achievements, collection UI, music player, or formal menus yet.

## Suggested Script Structure

Assets/Scripts/Core/

* StatType.cs
* PlayerStats.cs
* ChoiceEffect.cs
* EventCardData.cs
* CardDeckManager.cs
* GameManager.cs
* EndingManager.cs

Assets/Scripts/Prototype/

* PrototypeBootstrap.cs
* PrototypeHUD.cs
* CardDragController.cs

## Prototype Event Rule

* Start with 8 normal test cards.
* Start with 2 special test cards.
* After 5 normal cards, force the next card to be a special card.

## Development Order

Stage 1:
Core gameplay logic and minimal UI with two buttons.

Stage 2:
Mouse drag card interaction.

Stage 3:
Basic story flags and ending structure.
