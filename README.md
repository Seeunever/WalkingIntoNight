# AnimalCafe — 克苏鲁式跑团（2D）

单人、重剧情的 2D 跑团游戏原型，基于团结引擎 / Unity **2022.3.62t2**（URP 2D）。

## 功能概览

- **角色创建**：3d6×5 滚点、技能与 HP/SAN/MP
- **叙事引擎**：JSON 节点（对话、检定、旗标、物品、理智、战斗、结局）
- **第一个剧本**：`Scenario_01` —《咖啡馆关店后的失踪》
- **背包 / NPC / 地点**：地点列表 + NPC 交谈（非自由地图）
- **战斗**：简化回合（影鼠、狂热侍从）
- **存档**：本地 JSON，槽位 1（主菜单「继续」）
- **Steam**：见 [STEAM_BUILD.md](STEAM_BUILD.md)（Steamworks.NET 占位）

## 运行

1. 用 2022.3.62t2 打开项目根目录
2. 打开场景 `Assets/Scenes/MainMenu.scene`（或任意已加入 Build 的场景）
3. **Play** — UI 由代码自动生成，无需手绑 Canvas

## 目录

| 路径 | 说明 |
|------|------|
| `Assets/Scripts/` | 游戏逻辑（Core / Character / Dice / Narrative / UI 等） |
| `Assets/Resources/Data/` | 剧本、物品、NPC JSON |
| `Assets/Scenes/` | MainMenu / CharacterCreate / Gameplay |

## 修改剧本

编辑 `Assets/Resources/Data/Scenarios/Scenario_01/nodes.json`，节点类型见 `StoryNodeData`。

## Git

```bash
git pull
# 编辑后
git add -A && git commit -m "说明" && git push
```

远程：https://github.com/Seeunever/AnimalCafe
