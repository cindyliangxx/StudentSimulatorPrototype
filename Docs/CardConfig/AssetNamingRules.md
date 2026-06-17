# 素材命名规则

本文档定义 `StudentSimulatorPrototype` 后续卡牌、角色、UI、音频和 Prefab 的命名规则。目标是让策划配表、Unity 资源、代码引用和版本管理能长期保持一致。

## 1. 总原则

- 文件名使用英文小写、数字和下划线：`snake_case`。
- 不在文件名里使用中文、空格、括号、特殊符号。
- 展示给玩家的中文只写在配表字段，例如 `title`、`description`、选项文案。
- `cardId` 是逻辑 ID，文件名是资源路径 ID，二者要稳定，不随显示文案改变。
- 临时素材加 `_tmp`，确认进入项目后移除 `_tmp`。
- 版本号只用于源文件或外部迭代稿，例如 PSD、Aseprite、Figma 导出记录；Unity 正式资源不要用 `_final_final`。

## 2. 卡牌 ID 与卡牌资产

### 2.1 cardId

格式：

```text
<arc>.<seq>.<slug>
```

示例：

```text
daily.001.morning_lecture
daily.002.cafeteria_line
internship.010.normal_offer
internship.011.premium_offer
hidden.900.wake_up_call
```

规则：

- `arc`：剧情线或卡池，例如 `daily`、`exam`、`club`、`internship`、`hidden`。
- `seq`：三位数字，便于排序。
- `slug`：英文语义短名，使用下划线。
- `cardId` 一旦被 `nextCardId` 或存档引用，不要改名。

### 2.2 EventCardAsset 文件

目录：

```text
Assets/Data/EventCards/<arc>/
```

格式：

```text
ec_<arc>_<seq>_<slug>.asset
```

示例：

```text
Assets/Data/EventCards/daily/ec_daily_001_morning_lecture.asset
Assets/Data/EventCards/internship/ec_internship_010_normal_offer.asset
Assets/Data/EventCards/hidden/ec_hidden_900_wake_up_call.asset
```

当前项目已有测试资产可以保留，但新增正式卡建议使用上述格式。

## 3. 角色与卡面素材

### 3.1 角色头像或立绘

目录：

```text
Assets/Art/Characters/<character>/
```

格式：

```text
spr_char_<character>_<pose>_<emotion>.png
```

示例：

```text
spr_char_roommate_idle_neutral.png
spr_char_professor_talk_serious.png
spr_char_friend_idle_happy.png
```

字段说明：

- `character`：角色名英文短名，例如 `roommate`、`professor`、`friend`。
- `pose`：姿态，例如 `idle`、`talk`、`think`。
- `emotion`：情绪，例如 `neutral`、`happy`、`angry`、`worried`。

### 3.2 卡牌插图

目录：

```text
Assets/Art/Cards/<arc>/
```

格式：

```text
spr_card_<arc>_<seq>_<slug>_<variant>.png
```

示例：

```text
spr_card_daily_001_morning_lecture_main.png
spr_card_club_004_poster_main.png
spr_card_internship_010_normal_offer_alt01.png
```

如果一张卡没有专属插图，可以先使用通用背景或角色图，不要复制一份改名相同内容。

### 3.3 场景背景

目录：

```text
Assets/Art/Backgrounds/
```

格式：

```text
spr_bg_<location>_<time>_<variant>.png
```

示例：

```text
spr_bg_library_night_main.png
spr_bg_classroom_morning_main.png
spr_bg_dorm_evening_rain.png
```

## 4. UI 素材

### 4.1 状态图标

目录：

```text
Assets/ResourceLibrary/MainGameUI/Resources/UI/Stats/
```

格式：

```text
spr_stat_<stat>.png
```

示例：

```text
spr_stat_health.png
spr_stat_academic.png
spr_stat_social.png
spr_stat_money.png
```

### 4.2 通用 UI 图

目录：

```text
Assets/ResourceLibrary/MainGameUI/
```

格式：

```text
spr_ui_<component>_<state>.png
```

示例：

```text
spr_ui_card_frame_normal.png
spr_ui_card_frame_highlight.png
spr_ui_button_primary_normal.png
spr_ui_button_primary_pressed.png
spr_ui_dot_positive_small.png
spr_ui_dot_negative_large.png
```

当前原型里的圆点是程序化绘制，不需要这些图片；如果后续替换为美术素材，应使用以上命名。

## 5. Prefab、材质和动画

### 5.1 Prefab

目录：

```text
Assets/Prefabs/
```

格式：

```text
pfb_<category>_<name>.prefab
```

示例：

```text
pfb_ui_card_view.prefab
pfb_ui_stat_bar.prefab
pfb_vfx_card_swipe.prefab
```

### 5.2 材质

目录：

```text
Assets/Materials/
```

格式：

```text
mat_<target>_<style>.mat
```

示例：

```text
mat_card_paper_warm.mat
mat_ui_panel_dark.mat
```

### 5.3 动画

目录：

```text
Assets/Animations/
```

格式：

```text
anim_<target>_<action>.anim
```

示例：

```text
anim_card_enter.anim
anim_card_swipe_left.anim
anim_card_swipe_right.anim
```

## 6. 音频

### 6.1 音效

目录：

```text
Assets/Audio/SFX/
```

格式：

```text
sfx_<category>_<action>_<variant>.wav
```

示例：

```text
sfx_ui_card_drag_01.wav
sfx_ui_card_release_01.wav
sfx_ui_stat_warning_01.wav
```

### 6.2 背景音乐

目录：

```text
Assets/Audio/BGM/
```

格式：

```text
bgm_<scene>_<mood>_loop.wav
```

示例：

```text
bgm_campus_calm_loop.wav
bgm_exam_tense_loop.wav
```

## 7. 版本与临时文件

允许用于源文件：

```text
spr_card_daily_001_morning_lecture_main_v01.psd
spr_card_daily_001_morning_lecture_main_v02.psd
```

不建议用于 Unity 正式导入资源：

```text
card_final.png
card_final_new.png
card_final_new2.png
新建画布 1.png
```

临时占位素材：

```text
spr_card_daily_001_morning_lecture_tmp.png
```

临时素材进入正式提交前，应改为正式命名或删除。

## 8. 与配表的对应关系

建议在卡牌配表中保持：

| 配表字段 | 对应资源 |
|---|---|
| `cardId` | 逻辑 ID，例如 `daily.001.morning_lecture` |
| `assetFileName` | `ec_daily_001_morning_lecture.asset` |
| `cardIllustration` / `cardImage` | `spr_card_daily_001_morning_lecture_main.png` |
| `speakerPortrait`（后续可加） | `spr_char_professor_talk_serious.png` |
| `background`（后续可加） | `spr_bg_classroom_morning_main.png` |

当前代码已支持 `EventCardAsset.cardImage`。批量绑定流程：

1. 把卡牌图片放到 `Assets/Art/Cards` 或其子目录。
2. 图片命名使用 `spr_card_<cardId转下划线>_main.png`。
3. 在 Unity 菜单点击 `Tools > Card Config > Auto Bind Card Images`。
4. 工具会把图片导入为 `Sprite (2D and UI)`，并按命名自动写入对应卡牌的 `cardImage`。

示例：

| cardId | 推荐图片名 |
|---|---|
| `daily.001.morning_lecture` | `spr_card_daily_001_morning_lecture_main.png` |
| `internship.010.normal_offer` | `spr_card_internship_010_normal_offer_main.png` |
| `hidden.900.wake_up_call` | `spr_card_hidden_900_wake_up_call_main.png` |

`speakerPortrait`、`background` 字段目前仍未接入代码。若后续增加角色头像或背景展示，可以按上表继续扩展 `EventCardAsset`。
