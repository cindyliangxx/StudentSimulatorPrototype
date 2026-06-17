# Reigns 式卡牌配表说明

本文档定义 `StudentSimulatorPrototype` 的卡牌配表字段、填写规则、抽卡解释、数值规范和导入映射。当前项目仍以 Unity `EventCardAsset` ScriptableObject 作为运行时数据源，Excel 配表是策划与设计源文件；如后续需要自动导入，可按本文的字段映射编写 importer。

## 1. 配表文件

- Excel 模板：`Docs/CardConfig/ReignsStyleCardConfigTemplate.xlsx`
- 主表 Sheet：`Cards`
- 字段说明 Sheet：`Field Guide`
- 效果语法 Sheet：`Effect Grammar`
- 命名规则 Sheet：`Naming Rules`
- 导入映射 Sheet：`Import Notes`

主表每一行对应一张 `EventCardAsset`。建议把 Excel 当作源数据维护，再按行创建或更新 Unity 里的 `.asset` 文件。

## 2. 当前项目字段映射

### 2.1 卡牌基础字段

| 配表列 | Unity 字段 | 说明 |
|---|---|---|
| `cardId` | `EventCardAsset.cardId` | 稳定 ID，强制跳转 `nextCardId` 会引用它。不要随意改。 |
| `assetFileName` | `.asset` 文件名 | 建议使用英文、数字、下划线，不要用中文文件名。 |
| `title` | `title` | 卡牌标题，可中文。 |
| `description` | `description` | 卡牌正文，可中文。 |
| `leftChoiceText` | `leftChoiceText` | 左划/左按钮文案。 |
| `rightChoiceText` | `rightChoiceText` | 右划/右按钮文案。 |
| `debugNote` | `debugNote` | 仅供策划/调试阅读，不进正式 UI。 |
| `debugConditionNote` | `debugConditionNote` | 条件解释，便于检查卡池。 |

### 2.1.1 卡牌图片批量对应

当前代码已支持 `EventCardAsset.cardImage`。推荐不要逐张手动拖图，而是使用编辑器菜单批量绑定：

```text
Tools > Card Config > Auto Bind Card Images
```

批量绑定规则：

1. 图片放在 `Assets/Art/Cards` 或它的子目录中。
2. 图片导入工具会自动设置为 `Sprite (2D and UI)`。
3. 图片名按 `cardId` 转下划线匹配。
4. 工具会把匹配到的 Sprite 写入对应卡牌的 `cardImage` 字段。

推荐命名：

```text
cardId: daily.001.morning_lecture
image:  spr_card_daily_001_morning_lecture_main.png
```

匹配时会兼容这些形式：

- `spr_card_daily_001_morning_lecture_main.png`
- `spr_card_daily_001_morning_lecture.png`
- `daily_001_morning_lecture.png`

对于当前测试卡这种数字 ID，也可以使用：

```text
cardId: 1
image:  spr_card_1_main.png
```

### 2.2 抽卡控制字段

| 配表列 | Unity 字段 | 填写规则 |
|---|---|---|
| `isSpecialEvent` | `isSpecialEvent` | `TRUE/FALSE`。只影响 UI 和调试显示，不再固定每 5 张出现。 |
| `isHiddenCard` | `isHiddenCard` | `TRUE/FALSE`。用于隐藏剧情卡、特殊链路卡标记。 |
| `weight` | `weight` | 普通加权随机权重。默认 `1`。`0` 表示不进普通随机池。 |
| `useMaxPriority` | `useMaxPriority` | `TRUE` 时等价于 Reigns 的 `weight=max`，符合条件时优先抽。 |
| `cooldownTurns` | `cooldownTurns` | 卡牌被解决后冷却的后续抽卡次数。默认 `0`。 |
| `minResolvedEvents` | `minResolvedEvents` | 至少解决多少张卡后才可出现。默认 `0`。 |
| `maxResolvedEvents` | `maxResolvedEvents` | 最晚出现窗口。`-1` 表示无上限。 |

抽卡优先级固定为：

1. 若上一张选择写了 `nextCardId`，优先尝试强制下一张。
2. 强制卡仍要通过基础条件和事件窗口；但忽略权重和冷却。
3. 没有强制卡时，合并普通卡和特殊卡列表。
4. 过滤条件不满足、事件窗口不满足、冷却未结束、`weight <= 0` 且非 `useMaxPriority` 的卡。
5. 如果存在 `useMaxPriority = TRUE` 的候选卡，从这些卡中随机一张。
6. 否则按 `weight` 做加权随机。
7. 如果没有候选卡，使用代码里的 fallback 测试卡。

### 2.3 条件字段

| 配表列 | Unity 字段 | 说明 |
|---|---|---|
| `requiredFlags` | `requiredFlags` | 需要已拥有的 flag。多个用英文分号 `;` 分隔。 |
| `blockedFlags` | `blockedFlags` | 拥有任一 flag 时阻止出现。多个用英文分号 `;` 分隔。 |
| `statConditions` | `statConditions` | 属性条件。格式：`Academic>=60;Health<=30`。 |

支持的属性：

- `Health`
- `Academic`
- `Social`
- `Money`

支持的比较符：

- `>=`
- `<=`

示例：

```text
requiredFlags: HelpedFriend;ChoseInternship
blockedFlags: QuestionedLoop
statConditions: Academic>=60;Money<=20
```

## 3. 左右选项效果

每张卡有左、右两个 `ChoiceEffect`。主表把四项数值拆成独立列，便于筛选和做平衡检查。

### 3.1 数值变化

| 配表列 | 说明 |
|---|---|
| `leftHealth` / `rightHealth` | 健康变化。 |
| `leftAcademic` / `rightAcademic` | 学业变化。 |
| `leftSocial` / `rightSocial` | 社交变化。 |
| `leftMoney` / `rightMoney` | 金钱变化。 |

填写规则：

- 留空等价于 `0`。
- 正数表示增加，负数表示减少。
- 当前项目属性范围是 `0~100`，初始值是 `50`。
- 任一属性降到 `0` 或以下会触发失败结局。
- 达到 `100` 当前不会失败，只会被 clamp 到 `100`。

### 3.2 UI 圆点提示

拖动预览时，UI 会先计算“有效变化值”：

```text
有效变化值 = clamp(当前值 + 配表变化值, 0, 100) - 当前值
```

圆点显示规则：

| 有效变化值 | UI 表现 |
|---|---|
| `0` | 不显示圆点 |
| `1~14` 或 `-1~-14` | 小圆点 |
| `>=15` 或 `<=-15` | 大圆点 |

示例：

- 当前 `Money = 50`，配表 `rightMoney = -20`，有效变化 `-20`，显示大红点。
- 当前 `Money = 95`，配表 `rightMoney = +20`，有效变化 `+5`，显示小绿点。
- 当前 `Health = 3`，配表 `leftHealth = -10`，有效变化 `-3`，显示小红点。

### 3.3 Flag 和强制跳转

| 配表列 | Unity 字段 | 说明 |
|---|---|---|
| `leftSetFlags` / `rightSetFlags` | `setFlags` | 选择后添加 flag。多个用 `;` 分隔。 |
| `leftClearFlags` / `rightClearFlags` | `clearFlags` | 选择后移除 flag。多个用 `;` 分隔。 |
| `leftNextCardId` / `rightNextCardId` | `nextCardId` | 选择后强制下一张卡的 `cardId`。 |

Flag 命名建议使用 PascalCase：

```text
HelpedFriend
ChoseInternship
QuestionedLoop
FailedScholarshipInterview
```

不要在 flag 里使用空格、中文、标点符号或临时编号。

## 4. 加权随机说明

如果候选池中没有 `useMaxPriority = TRUE` 的卡，系统会使用普通权重池。

示例候选：

| cardId | weight |
|---|---:|
| campus.club.poster | 1 |
| library.night.study | 3 |
| internship.offer.normal | 6 |

总权重为 `1 + 3 + 6 = 10`。系统随机一个 `[0, 10)` 的整数或区间值，然后按累计权重命中：

- `campus.club.poster`：约 `10%`
- `library.night.study`：约 `30%`
- `internship.offer.normal`：约 `60%`

填写建议：

- 常规卡：`weight = 1~5`
- 希望更常见的日常卡：`weight = 6~10`
- 只通过剧情链出现的卡：`weight = 0`，并由 `nextCardId` 或 `useMaxPriority` 触发
- 紧急事件或剧情关键卡：使用 `useMaxPriority = TRUE`，同时配合条件限制

## 5. 冷却和事件窗口

`cooldownTurns` 是“这张卡被解决后，后续多少次抽卡内不能再次随机出现”。

当前代码逻辑：

```text
cooldownUntil = TotalResolvedEvents + cooldownTurns
当 TotalResolvedEvents < cooldownUntil 时，该卡不可进普通候选池
```

示例：

- 第 5 张卡解决后，该卡 `cooldownTurns = 2`
- 系统记录 `cooldownUntil = 7`
- 第 6 张时不能出现
- 第 7 张开始可以再次出现

事件窗口：

- `minResolvedEvents = 0`：开局即可出现。
- `minResolvedEvents = 5`：至少解决 5 张卡后出现。
- `maxResolvedEvents = -1`：无上限。
- `maxResolvedEvents = 10`：解决事件数大于 10 后不再出现。

## 6. 平衡建议

### 6.1 单次选择数值档位

建议按 Reigns 式 UI 圆点阈值组织数值：

| 档位 | 建议数值 | UI |
|---|---|---|
| 微小 | `±1~4` | 小圆点 |
| 小 | `±5~9` | 小圆点 |
| 中 | `±10~14` | 小圆点 |
| 大 | `±15~24` | 大圆点 |
| 极大 | `±25+` | 大圆点，慎用 |

### 6.2 一张卡的整体强度

常规日常卡建议：

- 单边总变化绝对值控制在 `10~25`。
- 一次选择影响 `2~3` 个属性最容易读懂。
- 不要让两个选项都是纯收益；最好有取舍。

剧情关键卡可以更重：

- 单项 `±15` 以上会明确显示大圆点。
- 若选择可能导致失败，应在文本中给玩家足够暗示。

## 7. 导入到 Unity 的人工流程

当前没有自动 importer，人工创建/更新建议如下：

1. 在 Excel 主表新增或修改一行。
2. 按 `assetFileName` 在 `Assets/Data/EventCards` 下创建或找到 `.asset`。
3. 把基础字段填入 `EventCardAsset`。
4. 把四项数值填入左右 `ChoiceEffect.Changes`。
5. 把 flag 列按 `;` 拆成 `setFlags` / `clearFlags` 列表。
6. 把 `nextCardId` 填入对应选择的 `nextCardId`。
7. 把 `requiredFlags`、`blockedFlags`、`statConditions` 填入条件。
8. 进入 Play Mode，观察 Console 中 `[CardDeck]` 过滤日志和抽卡日志。

## 8. 常见错误

- `cardId` 改名后忘记同步其他卡的 `nextCardId`。
- 把展示标题写进 `cardId`，导致 ID 含中文或空格。
- `weight = 0` 但没有任何 `nextCardId` 或 `useMaxPriority` 触发，导致卡永远不出现。
- `useMaxPriority = TRUE` 但条件太宽，导致它长期压制普通卡池。
- `requiredFlags` 和 `blockedFlags` 同时包含同一个 flag，导致卡永远不出现。
- 数值列填了文本，例如 `+10` 可以读，但建议只填 `10`；不要填 `10点`。
- `statConditions` 使用了不支持的比较符，例如 `>`、`<`、`==`。当前只支持 `>=` 和 `<=`。

## 9. 后续可扩展

如果要把 Excel 作为正式数据源，建议后续新增 Unity Editor importer：

- 读取 `Cards` sheet。
- 按 `cardId` 查找或创建 `EventCardAsset`。
- 自动填充基础字段、抽卡字段、条件、左右效果。
- 对 `nextCardId`、flag 命名、空标题、重复 ID 做校验。
- 生成导入报告，列出成功、警告、错误。
