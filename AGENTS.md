# AGENTS.md

## Project

This is a Unity 2022 prototype for a choice-based Reigns-like text strategy game named "Student Simulator Prototype" / "大学生模拟器".

The current project is based on the imported "Kings - Card Swiping Decision Game v1.55" Unity package. Use the package as an implementation and UI reference, but follow the Student Simulator design direction in `Docs/DesignMemory.md`.

## Core Design

The player sees event cards and chooses left or right. Each choice changes four internal stats:

* 身心
* 学业
* 人际
* 经济

All stats start at 50 and range from 0 to 100. If any stat reaches 0 or below, or 100 or above, trigger the corresponding imbalance ending.

Do not show exact stat numbers or exact plus/minus values to the player. Use fuzzy UI feedback such as icons, bars, color, danger states, or text frequency.

## Current Development Goal

Build only the core 2D gameplay prototype:

1. Card swipe loop.
2. Four-stat internal value changes.
3. Data-driven event cards.
4. Conditional weighted card pool with cooldowns.
5. Ending checks for high and low imbalance.
6. Restart flow after an ending.

Do not implement:

* final UI art
* complex animation
* audio systems
* save/load
* achievements
* collection gallery
* formal start menu
* music player
* chat system
* 3D gameplay

Imported Kings systems that are already compatible with Unity 2022 may stay in the project as reference or reusable infrastructure, but they are not current feature goals.

## Strong Boundary

Stop and ask the user before continuing when any of these happen:

* The user request conflicts with `Docs/DesignMemory.md`, this file, or earlier explicit instructions.
* A requirement is ambiguous enough that implementation would choose a product direction.
* A migration or compatibility issue could require removing a usable system, adding a new external package, or changing `ProjectSettings`.
* A planned change would expand scope beyond the core V1 prototype.
* Unity 2019-to-2022 differences create behavior that cannot be verified locally.

When the issue is a discoverable technical fact, inspect the project first. Ask only after reasonable inspection cannot resolve it.

## Unity Rules

* Use Unity built-in UI only unless explicitly approved.
* Use C#.
* Keep development 2D/UI-focused; do not introduce 3D systems for gameplay.
* Do not install new external packages unless explicitly approved.
* Do not edit `Library`, `Temp`, `Obj`, `Logs`, or `UserSettings`.
* Do not change `ProjectSettings` unless explicitly needed and explained first, or unless the user has explicitly approved a migration plan that includes it.
* Preserve Unity `.meta` files and GUIDs when moving imported assets.
* Put new Student Simulator gameplay scripts under `Assets/Scripts` unless a change is specifically adapting imported Kings code under `Assets/Kings`.

## Code Architecture Rules

* Keep data, game logic, UI, and input separated.
* Do not put all logic into one MonoBehaviour.
* Prefer clear class names.
* Add short comments for important prototype systems.
* Make event card data easy to replace later.
* Prefer data-driven events over hard-coded cards.

## Event Data Direction

Future event data should support:

* event ID
* event type
* title and description
* left and right option text
* left and right feedback text
* four-stat effects
* hidden variable effects
* trigger conditions
* draw weight
* cooldown
* unique/once-only flag
* tags
* art resource path

Hidden variables should follow the design memory: 自我选择, 家庭期待, 回避倾向, 真实连接, 内卷倾向, 消费压力.

## Workflow

Before making large changes:

1. Inspect the project.
2. Explain planned files and changes.
3. Ask before changing `ProjectSettings`, installing packages, or crossing the strong boundary above.

After making changes:

1. List created or modified files.
2. Explain how to test in Unity.
3. Mention possible risks or unfinished parts.
