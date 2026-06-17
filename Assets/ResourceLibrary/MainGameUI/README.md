# Main Game UI Resource Library

这个目录用于集中存放《大学生模拟器》主游戏界面新增的 UI 资源。当前只收纳主游戏 HUD 相关资源，不放卡牌数据、玩法脚本或最终系统功能。

## 目录结构

```text
Assets/ResourceLibrary/MainGameUI/
├── Backgrounds/
│   └── spr_ui_game_bg_warm_main.png
└── Resources/
    └── UI/
        └── Stats/
            ├── icon_body_mind_transparent_512.png
            ├── icon_body_mind_fill_mask_512.png
            ├── icon_money_transparent_512.png
            ├── icon_money_fill_mask_512.png
            ├── icon_social_transparent_512.png
            ├── icon_social_fill_mask_512.png
            ├── icon_study_transparent_512.png
            └── icon_study_fill_mask_512.png
```

## 资源说明

- `Backgrounds/spr_ui_game_bg_warm_main.png`
  - 主游戏背景图。
  - 规格：1920 x 1080 px。
  - 用途：横屏 16:9 主游戏 UI 背景。
  - 视觉：浅米白、低饱和、轻微纸感或暖灰过渡。

- `Resources/UI/Stats/*_transparent_512.png`
  - 顶部状态图标原图。
  - 规格：512 x 512 px，透明背景，正方形 PNG。
  - 对应关系：
    - `icon_body_mind_transparent_512.png`：身心
    - `icon_money_transparent_512.png`：经济
    - `icon_social_transparent_512.png`：人际
    - `icon_study_transparent_512.png`：学业

- `Resources/UI/Stats/*_fill_mask_512.png`
  - 顶部状态图标填充遮罩。
  - 规格：512 x 512 px，透明背景，正方形 PNG。
  - 用途：配合 UI 图片的纵向填充，表现当前数值。
  - 替换图标时需要同步更新对应 mask，否则填充形状会不匹配。

## 使用规则

- `Resources` 目录必须保留在 `Assets/ResourceLibrary/MainGameUI/Resources` 下，因为 `PrototypeHUD` 通过 `Resources.Load("UI/Stats/...")` 读取状态图标。
- 除背景外，所有图片素材必须是正方形 PNG。可用透明区域补足正方形。
- 如果替换顶部状态图标，请保持现有文件名，或同步修改 `PrototypeHUD.GetStatIconResourcePath`。
- 如果替换背景图，请保持现有文件名，或同步修改 `PrototypeHUD.LoadEditorBackgroundSprite`。
- 这个资源库只用于主游戏 UI 初版资源归档。正式卡牌插画、角色图、结局图等后续应单独建立子库或目录。

## 相关文档

- 主 UI 视觉与素材规格：`Docs/MainGameUISpec.md`
