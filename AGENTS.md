# AGENTS.md

## Project

This is a Unity prototype for a choice-based text strategy game named "Student Simulator Prototype".

The player sees event cards and makes left/right choices. Each choice changes five stats:

* Body
* Mental
* Academic
* Social
* Money

All stats start at 50, range from 0 to 100. If any stat reaches 0 or below, trigger a failure ending.

## Current Development Goal

Build only the core gameplay prototype.

Do not implement:

* final UI art
* complex animation
* audio
* save/load
* achievements
* collection gallery
* formal start menu
* music player
* chat system

## Unity Rules

* Use Unity built-in UI only.
* Use C#.
* Do not install external packages unless explicitly approved.
* Do not edit Library, Temp, Obj, Logs, or UserSettings folders.
* Do not change ProjectSettings unless explicitly needed and explained first.
* Keep scripts organized under Assets/Scripts.

## Code Architecture Rules

* Keep data, game logic, UI, and input separated.
* Do not put all logic into one MonoBehaviour.
* Prefer clear class names.
* Add short comments for important prototype systems.
* Make event card data easy to replace later.

## Workflow

Before making large changes:

1. Inspect the project.
2. Explain planned files and changes.
3. Ask before changing ProjectSettings or installing packages.

After making changes:

1. List created or modified files.
2. Explain how to test in Unity.
3. Mention possible risks or unfinished parts.
