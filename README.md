# Walking Into Night（走入夜境）

2D 克苏鲁式跑团游戏，基于团结引擎 / Unity **2022.3.62t2**（URP 2D）。

当前版本：**v0.1.0-alpha.1 · Internal Vertical Slice Alpha**。这是可运行、可保存并可走到结局的内部试玩基线，不是公开 Steam Demo；完整人物线、调查笔记、最终结局改写、剩余美术与音频仍在开发中。

当前产品与内容规划：

- [单人单机与 Steam 路线图](SINGLE_PLAYER_STEAM_ROADMAP.md)
- [叙事与美术圣经](NARRATIVE_BIBLE.md)
- [Scenario_01《雨停之前》剧本草案](SCENARIO_01_SCRIPT_DRAFT.md)
- [Scenario_01 完整节点流与失败回路](SCENARIO_01_FLOW.md)
- [第三方许可与 AI 美术来源记录](THIRD_PARTY_NOTICES.md)
- [版本更新记录](CHANGELOG.md)
- [High 可执行工程 TODO](TODO.md)

## 功能概览

- **角色创建**：3d6×5 滚点、技能与 HP/SAN/MP
- **叙事引擎**：JSON 节点（对话、检定、旗标、物品、理智、战斗、时间推进、结局）
- **时间系统**：第 N 天 + 上午/下午/傍晚/夜间；玩家可等待下一时段或下一天
- **NPC 日程**：按天与时段出现在指定地点
- **NPC 关系**：关系数据与解锁旗标，可门槛选项
- **第一个剧本**：`Scenario_01` —《咖啡馆关店后的失踪》
- **背包 / NPC / 地点**：地点列表 + NPC 交谈（非自由地图）
- **战斗**：简化回合（影鼠、狂热侍从）
- **存档**：本地 JSON，槽位 1（主菜单「继续」）
- **正式身份**：`Seeunever / Walking Into Night / com.seeunever.walkingintonight`
- **剧情编辑器**：Unity 菜单 `WalkingIntoNight/剧情编辑器`
- **Steam**：见 [STEAM_BUILD.md](STEAM_BUILD.md)

## 运行

1. 用 2022.3.62t2 打开项目根目录（文件夹名可与产品名不同）
2. 打开场景 `Assets/Scenes/MainMenu.scene`
3. **Play** — UI 由代码自动生成

## 目录

| 路径 | 说明 |
|------|------|
| `Assets/Scripts/` | 游戏逻辑（`WalkingIntoNight.TRPG` 命名空间） |
| `Assets/Scripts/Editor/StoryEditor/` | 可视化剧情编辑器 |
| `Assets/Resources/Data/` | 剧本、物品、NPC、关系 JSON |
| `Assets/Resources/Art/Characters/` | 当前正式头像：小梅、老陈、店猫（3 张） |
| `Assets/Scenes/` | MainMenu / CharacterCreate / Gameplay |

## 剧情编辑器

菜单：**WalkingIntoNight → 剧情编辑器**

| 区域 | 功能 |
|------|------|
| 左侧实体面板 | 创建/编辑 NPC、地点、物品、关系、NPC 日程 |
| 中央节点图 | 可视化编辑剧本节点，拖拽连线表示跳转 |
| 右侧 Inspector | 编辑选中节点的文本、检定、选项、时间条件等 |

**保存** 会写入以下文件（游戏运行时直接读取）：

- `Assets/Resources/Data/Scenarios/{剧本ID}/nodes.json` — 剧本节点
- `Assets/Resources/Data/Scenarios/{剧本ID}/nodes.editor.json` — 编辑器布局（仅 Editor 用）
- `Assets/Resources/Data/NPCs/npcs.json`
- `Assets/Resources/Data/NPCs/locations.json`
- `Assets/Resources/Data/NPCs/relationships.json`
- `Assets/Resources/Data/Items/items.json`

### 节点类型

| type | 说明 |
|------|------|
| `dialogue` | 对话；可带 `choices` 分支 |
| `check` | 技能检定；连 Success / Failure 出边 |
| `setflag` | 设置剧情旗标 |
| `giveitem` | 给予物品 |
| `location` | 切换地点 hub |
| `advancetime` | 自动推进 `advancePeriods` / `advanceDays` |
| `combat` / `end` | 战斗 / 结局 |

### NPC 日程

在「日程」标签为 NPC 添加条目：`day`（0=每天）、`period`（morning/afternoon/evening/night/any）、`locationId`。

有日程的 NPC 仅在匹配时段出现在对应地点；无日程则使用 `locationIds` 静态绑定。

## 手动修改剧本

也可直接编辑 `Assets/Resources/Data/Scenarios/Scenario_01/nodes.json`。

## 角色立绘（当前共 3 张）

当前项目实际包含：

- `Assets/Resources/Art/Characters/`：小梅、老陈和店猫 3 张正式运行时头像。

旧的 bear / fox / rabbit / swan 水印占位图不再放入 `Resources`，不会进入本版本构建。仓库历史中的旧开发素材不代表可用于发行；发布边界见 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)。

黑衣女人和老板影子的正式头像仍待完成；当前对话会使用无头像回退，不影响剧情推进。

README 过去写过“500 张头像”，那是批量生成脚本的目标数量，不是已经导入的资源数量。当前 Demo 不需要先生成 500 张。

### 推荐：AI 高质量批量生成（精度 > 星露谷 64px 头像）

风格参考见 [`tools/style_reference.png`](tools/style_reference.png)（1920s  pulp 像素立绘）。

**需要 NVIDIA GPU（约 8GB 显存）或耐心使用 CPU：**

```bash
py -m pip install torch torchvision --index-url https://download.pytorch.org/whl/cu124
py -m pip install diffusers transformers accelerate safetensors pillow
py tools/generate_portraits_ai.py              # 可选：按脚本配置批量生成
py tools/generate_portraits_ai.py --count 10     # 先试 10 张
py tools/generate_portraits_ai.py --start 10 --count 20  # 断点续跑
```

- **输出**：320×320 PNG，命名 `职业-性格-性别-年龄-人种.png`
- **索引**：`portraits_manifest.json`
- **游戏中引用**：`portraitId` = 文件名（无扩展名），`PortraitDatabase.Get(id)`

Unity 导入：**WalkingIntoNight → Art → Apply Import Settings**

### 备选：程序化像素画（离线、无需 GPU，细节有限）

```bash
py tools/generate_portraits.py
```

使用 160px 像素画引擎（2× 输出 320px），带描边与多层 shading，但仍不如手绘/AI 精度。

- **命名**：`职业-性格-性别-年龄-人种.png`（英文）
- **索引**：`portraits_manifest.json` 含全部元数据

## Git

```bash
git pull
git add -A && git commit -m "说明" && git push
```

远程：https://github.com/Seeunever/WalkingIntoNight
