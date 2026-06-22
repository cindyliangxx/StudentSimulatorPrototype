# Student Simulator Design Memory

Source: `C:/Users/24276/Downloads/大学生模拟器_正式策划案_v0.2.docx`, version V0.2, dated 2026-06-15.

## Core Definition

`大学生模拟器` is a Reigns-like card-swiping narrative strategy game about university life. The core experience is using very small left/right choices to drive state balance, probabilistic narrative, and long-term self-definition.

The design question is not "how to become an excellent student." The design question is: are you living your own university life, or completing the university life expected by others?

## Player Experience

The game should not feel like a math optimization puzzle. Choices should feel ordinary but sharp. A single choice looks light; accumulated choices change the player's life focus.

Writing should be short, concrete, suggestive, and non-preachy. Each card shows a local moment and lets the player infer the larger cause and emotion.

## Core Loop

1. Event appears.
2. Player swipes left or right.
3. Stats and hidden variables change.
4. Conditions, cooldowns, and card pool state update.
5. Ending checks run.
6. Next event is drawn from the available weighted card pool.

Do not build a traditional branching story tree for V1. Use a state-driven card pool instead.

## Core Stats

There are four internal stats:

* 身心: how the player treats body and emotion.
* 学业: how the player handles grades, courses, and future pressure.
* 人际: how the player builds relationships while keeping selfhood.
* 经济: how the player faces resources, consumption, and reality pressure.

Internal range: 0-100.

Initial value: 50.

Both ends are dangerous. If a stat reaches 0 or below, or 100 or above, trigger the corresponding ending.

Exact numbers and exact value deltas should not be shown to the player. Use fuzzy status displays, danger feedback, icons, bars, color, text frequency, or similar indirect cues.

## Imbalance Endings

Base ending directions:

* 身心过低: 透支崩溃
* 身心过高: 自我封闭
* 学业过低: 学业崩盘
* 学业过高: 绩点机器
* 人际过低: 社交孤岛
* 人际过高: 所有人的好人
* 经济过低: 生活失控
* 经济过高: 被金钱吞没
* 多项平衡: 普通毕业
* 隐藏变量达成: 自己的课表

Endings should express a life pattern, not simply say "you lost."

## Event Card Data

Event data should be replaceable and data-driven. Required direction:

* event ID
* event type: normal, special, sub-pool, ending
* title and description
* left option and right option
* left feedback and right feedback
* four-stat changes
* hidden variable changes
* trigger conditions
* draw weight
* cooldown
* unique/once-only flag
* tags
* art resource path

Selection text should be short, like a thought in the player's head. Both choices should have costs; avoid obvious correct answers.

## Card Pools

Use conditional weighted pools instead of a full story tree.

Supported pool ideas:

* Main normal card pool.
* Special story cards.
* Temporary sub-pools lasting about 3-5 cards.

Candidate sub-pools:

* 考试周
* 小组作业
* 兼职
* 家庭压力
* 生病

Each card can be filtered by stats, time, tags, hidden variables, cooldown, and uniqueness, then drawn by weight.

## Hidden Variables

Use hidden variables for long-term narrative direction:

* 自我选择
* 家庭期待
* 回避倾向
* 真实连接
* 内卷倾向
* 消费压力

These variables should affect later card availability, special pools, and endings.

## UI Direction

The event card is the visual center.

Recommended layout:

* Top: four fuzzy status indicators for 身心, 学业, 人际, 经济.
* Middle: event card with art, title, and description.
* Sides or bottom: left/right option hints while swiping.
* Feedback layer: one short response after a choice.

Do not show precise stat numbers in normal play.

## V1 Demo Scope

Must implement:

* stable left/right swipe
* four internal stats
* hidden value presentation
* conditional weighted card pool
* data-driven event import from JSON or table-like data
* endings, including high and low imbalance directions
* restart after ending
* at least 10-15 unified-style event cards for demo

Do not implement for V1:

* complex character affection
* map or free movement
* item system
* long branching story tree
* exact stat display
* large animation scope

## Strong Boundary Reminder

If implementation choices conflict with this document, `AGENTS.md`, or the user's latest instruction, stop and ask before continuing.

If a Unity migration issue requires removing a usable system, adding a new dependency, changing `ProjectSettings`, or changing the game's design model, stop and ask before continuing.
