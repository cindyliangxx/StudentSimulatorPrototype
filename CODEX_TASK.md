# CODEX_TASK.md

## Game Concept

The game is a Reigns-like choice-based text strategy game about university life.

Core loop:

1. Show an event card.
2. Player chooses left or right by swiping.
3. Choice changes four internal stats.
4. Choice may update hidden variables, tags, cooldowns, and card pool state.
5. Next card is drawn from a conditional weighted card pool.
6. Endings are determined by stat imbalance and hidden variables.

## Core Stats

* 身心
* 学业
* 人际
* 经济

Initial value: 50
Minimum: 0
Maximum: 100

If any stat reaches 0 or below, or 100 or above, stop gameplay and trigger the matching imbalance ending.

Exact stat numbers should stay hidden from the player. The UI should communicate fuzzy state, trend, and danger.

## First Demo Scope

Implement only:

1. Core four-stat system.
2. Data-driven event card structure.
3. Conditional weighted card pool.
4. Card cooldown and unique-card support.
5. Basic card progression.
6. Choice effects.
7. Hidden variable hooks.
8. High and low imbalance ending checks.
9. Restart flow after an ending.
10. Minimal 2D UI for testing.
11. Mouse/touch drag left/right card selection.

Do not implement final art, save/load, achievements, collection UI, music player, chat systems, formal menus, or 3D gameplay yet.

## Imported Base

The project now uses the imported `Assets/Kings` package as the base resource and reference implementation.

Keep compatible Kings systems when they compile and load in Unity 2022, but do not treat old Kings-specific features such as kingdom values, ads, achievements, inventory, quests, or music as current Student Simulator requirements.

## Data Direction

Event card records should support:

* event ID
* event type: normal, special, sub-pool, ending
* title and description
* left and right option text
* left and right feedback text
* stat changes for 身心, 学业, 人际, 经济
* hidden variable changes
* trigger conditions
* draw weight
* cooldown
* unique/once-only flag
* tags
* art resource path

## Hidden Variables

Use these long-term hidden variables unless the design changes:

* 自我选择
* 家庭期待
* 回避倾向
* 真实连接
* 内卷倾向
* 消费压力

## Development Order

Stage 1:
Confirm the imported Kings scene and scripts remain stable in Unity 2022.

Stage 2:
Map the Student Simulator four-stat model onto the imported card-swipe loop.

Stage 3:
Replace hard-coded or Kings-themed content with data-driven university event content.

Stage 4:
Implement conditional weighted pools, cooldowns, hidden variables, endings, and restart.
