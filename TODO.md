# Walking Into Night · High 可执行工程 TODO

> 更新日期：2026-08-23
> 项目引擎：团结引擎 / Unity `2022.3.62t2`，Tuanjie Editor `1.8.0`
> 用途：由 Max 做拆解和风险判断，High 按批次实施。不要把本文当愿望清单；每个批次都包含范围、实施要求和验收证据。

> **2026-08-23 进度更新：** `H0–H9` 技术基线、`SP-1D` 产品身份和 `SP-2B-1` 雨夜开场批已经完成。正式身份为 `Seeunever / Walking Into Night / com.seeunever.walkingintonight`；下一步用 Sol High 做 `SP-2B-2` 小梅完整人物线，再依次处理老陈、店猫、推理与终局，不急着进入调查笔记或 Steam SDK。

## 0. High 开工协议

### 0.1 每次只领取一个批次

- 一次任务只实现一个标有 `Batch` 的批次。
- 开工前先阅读项目根目录的 `AGENTS.md`（若存在）和本文。
- 先用 `git status --short` 查看现场；当前工作区已有大量未提交修改，不得假设它们可以丢弃。
- 不执行 `git reset --hard`、`git checkout --`、批量删除、重写历史或覆盖来源不明的文件。
- 未经妹妹明确要求，不提交、不推送、不发布、不安装新的生产依赖。
- 优先复用现有运行时代码 UI、数据结构和命名空间，不把任务顺手扩大成架构重写。
- 安全、局部、可回退的小实现可以直接做；涉及新增第三方库、替换 UI 技术栈、删除资源或改变剧情设计时先询问。

### 0.2 每个批次的完成定义

一个批次只有同时满足下列条件才可勾选：

- [ ] 目标问题已通过代码或数据修改解决。
- [ ] Unity 完成资源刷新与脚本编译。
- [ ] Unity Console 为 `0 Error`；新增 Warning 必须解释并处理或记录。
- [ ] 按批次列出的 Play Mode 路径实际点击验证，不只做静态阅读。
- [ ] 验证后退出 Play Mode，让编辑器停在安全状态。
- [ ] 运行 `git diff --check`，没有新增空白错误。
- [ ] 最终汇报包含：改了什么、验证了什么、还有什么未覆盖。

### 0.3 High 不应擅自做的事

- 不删除或重建现有场景。
- 不把代码生成 UI 改成 Prefab/UI Builder/UIToolkit 全量重写。
- 不为了“整洁”重命名全部节点 ID、NPC ID、物品 ID 或资源目录。
- 不更改现有存档槽位约定：当前 UI 使用槽位 `1`。
- 不添加 DOTween、Spine、Live2D 等依赖；动画第一阶段使用 Unity 自带能力。
- 不生成、替换或删除大量美术资源，除非任务明确要求。
- 不修改与当前批次无关的用户改动。

## 1. 当前项目基线

### 1.1 已完成并已在 Play Mode 验证

- [x] 修复运行时 UI 无法点击：运行时自动创建 `EventSystem`。
- [x] 导入 TextMesh Pro Essential Resources。
- [x] 接入 Noto Sans SC，中文能够正常显示。
- [x] 修复角色名输入框占位文字被当成真实姓名。
- [x] 修复地点切换后顶部地点标题不刷新。
- [x] 修复 `hub_explore` 把玩家强制送回大厅。
- [x] 修复日志面板遮挡侧栏 NPC 按钮。
- [x] 侧栏显示调查员 HP/SAN/MP、随身物品、地点和当前 NPC。
- [x] NPC 对话可根据 `portraitId` 显示头像。
- [x] 没有有效剧情选项时有兜底按钮，不再直接软锁。
- [x] 跑通：`主菜单 → 新游戏 → 角色创建 → 开始调查 → 储藏室 → 店猫对话`。
- [x] 最近一次验证时 Unity Console 为 `0 Warning / 0 Error`，另有一条 Steam 未启用的普通信息日志。

### 1.2 关键入口与文件地图

- 场景入口：
  - `Assets/Scenes/MainMenu.scene`
  - `Assets/Scenes/CharacterCreate.scene`
  - `Assets/Scenes/Gameplay.scene`
- 运行时初始化：
  - `Assets/Scripts/Core/TRPGRuntimeInit.cs`
  - `Assets/Scripts/Core/AutoSceneBootstrap.cs`
- 游戏状态与存档：
  - `Assets/Scripts/Core/GameStateManager.cs`
  - `Assets/Scripts/Core/GameSaveData.cs`
  - `Assets/Scripts/Core/SaveSystem.cs`
- 剧情执行：
  - `Assets/Scripts/Narrative/ScenarioRunner.cs`
  - `Assets/Scripts/Narrative/StoryNodeData.cs`
  - `Assets/Scripts/Narrative/ConditionEvaluator.cs`
- 运行时 UI：
  - `Assets/Scripts/UI/MainMenuUI.cs`
  - `Assets/Scripts/UI/CharacterCreateUI.cs`
  - `Assets/Scripts/UI/GameplayUI.cs`
  - `Assets/Scripts/UI/UIBuilder.cs`
- 战斗：
  - `Assets/Scripts/Combat/CombatManager.cs`
  - `Assets/Scripts/Combat/CombatState.cs`
  - `Assets/Scripts/Combat/CombatEncounterDatabase.cs`
- 剧情和实体数据：
  - `Assets/Resources/Data/Scenarios/Scenario_01/nodes.json`
  - `Assets/Resources/Data/NPCs/*.json`
  - `Assets/Resources/Data/Items/items.json`
- 剧情编辑器与验证器：
  - `Assets/Scripts/Editor/StoryEditor/StoryEditorWindow.cs`
  - `Assets/Scripts/Editor/StoryEditor/StoryValidator.cs`
- 头像：
  - `Assets/Resources/Portraits/Cthulhu1920s/`
  - 当前实际接入 `bear / fox / rabbit / swan` 四张静态 PNG。
  - 当前没有 `.anim`、Animator Controller 或逐帧动画素材。

### 1.3 已确认的高风险点

1. `SaveSystem.Load` 没有异常与坏 JSON 防护；“继续”只判断文件是否存在。
2. 读档通过 `ScenarioRunner.StartFrom(CurrentNodeId)` 重新执行节点；若存档停在 `giveitem`、`changesan`、`advancetime` 等有副作用的展示节点，可能重复获得物品、重复扣 SAN 或重复推进时间。
3. 剧情、NPC 对话和战斗期间，左侧地点/NPC及“下一时段/下一天”仍可能被点击，能够绕过正常剧情。
4. 侧栏始终列出全部地点；玩家可能不经钥匙或剧情条件直接进入地下室。
5. 战斗节点配置错误或调查员为空时，当前代码可能进入没有按钮的战斗 UI。
6. “闪避”日志声称提升防御，但当前敌方回合并未真正读取闪避状态。
7. README 声称存在 500 张头像，但当前 Resources 下实际只有四张；文档与资源现场不一致。

---

# Batch H0 · 建立可重复验证基线

## 目标

不改变玩法，先让后续修复有一致的验证入口和最小自动测试基础。

## 实施任务

- [x] H0-1：记录当前基线
  - 打开 `MainMenu.scene`，刷新资源并确认编译状态。
  - Play Mode 跑一遍已知主链路。
  - 记录 Console 中现有信息、Warning 和 Error 数量。

- [x] H0-2：创建最小 EditMode 测试目录
  - 建议目录：`Assets/Tests/EditMode/`。
  - 建立只引用 `WalkingIntoNight.Runtime` 和 Unity Test Framework 的测试 asmdef。
  - 不修改 `Packages/manifest.json`；项目已包含 `com.unity.test-framework 1.1.33`。

- [x] H0-3：添加纯逻辑测试
  - `GameTime`：上午→下午→傍晚→夜间→次日上午，天数只在夜间跨越时增加。
  - `GameTime.ParsePeriod`：空值和未知值安全回退上午。
  - `StoryValidator`：能识别重复节点、缺失跳转目标和不存在的 startNode。
  - 测试不得读取或覆盖真实玩家存档。

## 验收

- [x] EditMode 测试全部通过。
- [x] 主链路仍可运行。
- [x] 没有因为建立测试而修改运行时数据和玩家存档。

---

# Batch H1 · 存档/读档可靠性与副作用安全

## 目标

槽位 1 能可靠保存和继续；坏存档不会让游戏崩溃；读档不会重复结算当前节点。

## 涉及文件

- `Assets/Scripts/Core/GameSaveData.cs`
- `Assets/Scripts/Core/GameStateManager.cs`
- `Assets/Scripts/Core/SaveSystem.cs`
- `Assets/Scripts/Narrative/ScenarioRunner.cs`
- `Assets/Scripts/UI/MainMenuUI.cs`
- `Assets/Scripts/UI/GameplayUI.cs`
- 新增测试文件时放入 `Assets/Tests/EditMode/`

## 实施任务

- [x] H1-1：定义存档版本
  - 在 `GameSaveData` 增加整数版本字段，当前版本从 `1` 开始。
  - 版本 `0` 视为当前字段出现前的旧存档，允许按明确默认值兼容。
  - 未知的未来版本不可静默读取；返回可显示的错误。

- [x] H1-2：统一槽位边界
  - 明确合法槽位为 `1..SlotCountMax`，保持现有 UI 槽位 1 不变。
  - `Save / Load / HasSave / Delete` 的槽位校验规则一致。
  - 本批次不调用 `Delete`，也不删除妹妹现有存档。

- [x] H1-3：提供不抛到 UI 的安全 API
  - 建议新增 `TrySave(slot, data, out error)`、`TryLoad(slot, out data, out error)` 或等价接口。
  - 捕获文件读取、权限、JSON 反序列化和空数据错误。
  - 加载失败不得改写或删除原存档。
  - 保存失败不得显示“已存档”。
  - 如采用临时文件写入，确保异常时旧存档仍保留；不要引入平台相关的脆弱替换逻辑。

- [x] H1-4：验证读入数据
  - `scenarioId` 必须能在 `ScenarioRegistry` 找到。
  - 对应场景 JSON 必须能加载。
  - `nodeId` 必须存在于该场景。
  - `locationId` 必须是已知地点。
  - `investigator` 必须存在且关键数值可恢复。
  - `flags`、`inventoryItemIds` 为空时使用空集合，不能 NullReference。
  - 日/时段为空或非法时按已说明的兼容规则处理。
  - 不要把严重损坏的存档静默重置成新游戏；应让玩家知道“继续失败”。

- [x] H1-5：修复“读档重新执行副作用节点”
  - 不再对已有 `CurrentNodeId` 无条件调用普通 `StartFrom`。
  - 为读档建立明确的恢复入口，例如 `ResumeFromSave`，名称可按项目风格调整。
  - 恢复时：
    - `dialogue / location / end` 可以重新呈现，但不能重复改变状态。
    - `giveitem` 节点只重新显示已发生的文本，不再次添加物品。
    - `changesan` 节点只重新显示，不再次修改 SAN。
    - `advancetime` 节点只重新显示，不再次推进时间。
    - 无文本的自动节点正常存档时不应成为停留点；遇到旧存档时安全推进到可展示节点，并防止无限递归。
    - 战斗中仍不允许创建存档，本批次不实现战斗状态序列化。
  - 新游戏仍使用普通起始执行入口，不能跳过初始化节点。

- [x] H1-6：主菜单反馈
  - “继续”按钮只在槽位 1 存在可尝试读取的数据时启用。
  - 点击后读取失败时留在主菜单并显示简短错误，不抛异常、不进入空 Gameplay。
  - 不因为一次加载失败自动删除存档。

- [x] H1-7：游戏内保存反馈
  - 成功才显示“已存档（槽位 1）”。
  - 失败显示可理解的原因。
  - 保留战斗中禁止存档的规则。

- [x] H1-8：添加测试
  - 存档 DTO 序列化/反序列化保留角色、物品、旗标、节点、地点和时间。
  - 空列表和旧版本字段得到安全默认值。
  - 坏 JSON 返回失败而不是抛到调用者。
  - 非法槽位返回失败。
  - 测试使用独立临时路径或可注入路径，不碰真实 `Application.persistentDataPath` 存档。

## Play Mode 验收矩阵

- [x] 新游戏进入 Gameplay，在大厅存档；回主菜单后“继续”可用。
- [x] 切换到储藏室、推进到夜间、修改物品与旗标后存档；继续游戏恢复同一地点、时间、角色状态、物品和节点。
- [x] 停在获得 `rusty_key` 的文字页面存档并读档，钥匙数量仍为 1。
- [x] 停在扣 SAN 或时间推进的文字页面存档并读档，SAN 和时间不发生第二次变化。
- [x] 临时使用一份故意损坏的测试存档验证错误提示；不要覆盖妹妹真实存档。
- [x] 读档失败后仍能点击“新游戏”。

## 本批次停止条件

- 如果无法在不删除现有存档的情况下完成测试，停止并说明需要妹妹确认的具体文件路径。
- 如果需要设计多槽位 UI，停止；当前批次只保证槽位 1，不扩大范围。

---

# Batch H2 · 游戏交互状态门控

## 目标

只有处于“自由调查 Hub”时才能切换地点、找 NPC 或等待；对话、检定展示、战斗和结局期间不能绕过当前流程。

## 实施任务

- [x] H2-1：定义明确的交互模式
  - 建议建立 `Narrative / Exploration / Combat / End` 等最小枚举，或提供等价的单一状态来源。
  - 不要让 UI 通过 `node.id == "hub_explore"` 到处散落硬编码判断。
  - 可在 `StoryNodeData` 增加向后兼容的 `allowExploration` 布尔字段，并只在真正 Hub 节点设为 true；也可提出同等清晰的实现。

- [x] H2-2：Runner 成为权限最终守门人
  - `TravelToLocation`、`TalkToNpc`、`WaitNextPeriod`、`WaitNextDay` 在非 Exploration 状态直接拒绝。
  - 战斗状态优先级最高。
  - 即使 UI 忘了禁用按钮，也不能从 Runner 绕过流程。

- [x] H2-3：UI 同步禁用
  - 保存时间按钮与侧栏交互容器引用。
  - 非 Exploration 状态时禁用地点、NPC、下一时段和下一天。
  - 禁用应有视觉差异；不要只让按钮悄悄失效。
  - 存档按钮按 H1 规则处理：非战斗可保存，战斗不可保存。

- [x] H2-4：模式切换事件
  - Runner 状态变化时通知 GameplayUI，一处统一刷新按钮状态。
  - 场景重新加载和读档后也必须刷新，不能依赖上一次静态字段值。

- [x] H2-5：防连续点击
  - 同一“继续”或选项连续快速点击不会推进两次。
  - 切换节点后旧按钮不可继续触发。

## 验收

- [x] 开场三个旁白页面：地点、NPC、等待按钮不可用。
- [x] 到 `hub_explore`：地点、NPC、等待按钮恢复可用。
- [x] 点击小梅进入对话：其他 NPC 和地点暂时不可用；返回 Hub 后恢复。
- [x] 战斗中：地点、NPC、等待、存档均不可绕过战斗。
- [x] 结局页面：只保留返回主菜单。
- [x] 快速双击继续与选项，不跳过两个节点。

---

# Batch H3 · 战斗闭环与错误恢复

## 目标

影鼠与狂热侍从战斗能完整经历开始、玩家行动、敌方行动、胜利/失败/逃跑和剧情返回；坏配置不产生软锁。

## 涉及文件

- `Assets/Scripts/Combat/CombatManager.cs`
- `Assets/Scripts/Combat/CombatState.cs`
- `Assets/Scripts/Combat/CombatEncounterDatabase.cs`
- `Assets/Scripts/Narrative/ScenarioRunner.cs`
- `Assets/Scripts/UI/GameplayUI.cs`
- `Assets/Resources/Data/Scenarios/Scenario_01/nodes.json`

## 实施任务

- [x] H3-1：安全启动战斗
  - `StartEncounter` 返回明确成功/失败结果。
  - encounter 不存在、调查员为空或敌人数据为空时，不进入战斗 UI。
  - 错误写入日志并回到安全剧情节点或结束场景；不得留下空按钮页面。

- [x] H3-2：统一战斗结束顺序
  - 先同步玩家 HP 到 Investigator。
  - 再清理战斗状态与 UI 模式。
  - 最后跳转 win/lose/flee 节点。
  - `HasPendingCombatReturn`、`PostCombatNodeId` 在所有出口正确清理。
  - 缺失 win/lose/flee 目标时走明确兜底并记录错误。

- [x] H3-3：修复闪避语义
  - 当前日志说“防御提升”，但实现没有效果。
  - 实现一个最小、可测试的闪避效果，例如令下一次敌方攻击带惩罚或进行对抗；或者调整设计与文本，但按钮行为必须和反馈一致。
  - 闪避状态在一轮敌方行动后重置。

- [x] H3-4：战斗 UI
  - 进入战斗时隐藏或弱化旧 NPC 头像。
  - 显示玩家 HP、敌人名称和 HP、当前回合。
  - 玩家行动期间只生成一套按钮；回合更新不能重复堆叠。
  - 结算后旧战斗按钮不可再点击。
  - 战斗日志只写入一次，不重复订阅同一事件。

- [x] H3-5：最小测试
  - 无效 encounter ID 启动失败且不进入 active 状态。
  - 胜利时 `playerWon=true`，敌人 HP 不小于 0。
  - 失败时玩家 HP 被夹在 0..MaxHP 并同步到 Investigator。
  - 逃跑成功与失败均有确定的状态转换。
  - 如随机数难以稳定测试，优先增加可注入/可控的掷骰入口；不要写依赖运气的测试。

## Play Mode 验收矩阵

- [x] 进入 `combat_shadow_rat`，至少完成攻击、闪避和逃跑各一次验证。
- [x] 战斗胜利进入 `after_rat_win`，继续后回到 Hub。
- [x] 战斗逃跑回到 `hub_explore`。
- [x] 狂热侍从胜利进入 `after_cult_win`，失败进入 `end_bad`。
- [x] 战斗结束后 HP、按钮、侧栏和剧情模式一致。

---

# Batch H4 · 剧情数据验证与第一章完整通关

## 目标

让 Scenario_01 的结构错误能在编辑器中提前发现，并实际跑通至少一个完整结局。

## 实施任务

- [x] H4-1：修复验证器输出格式
  - `FormatIssues` 当前把前缀和正文分到错误行；每个问题输出为完整单行。

- [x] H4-2：扩展节点验证
  - 不支持的 `type` 报错。
  - `dialogue/location/advanceTime` 等需要后继时，缺失出口给出警告或错误。
  - `check` 必须有 skill、success、failure。
  - `combat` 必须有合法 combatId、win、lose、flee。
  - `giveitem` 和 choice.requiredItemId 必须引用已知物品。
  - locationId 必须引用已知地点。
  - `end` 不应要求普通 nextNode。

- [x] H4-3：扩展实体验证
  - NPC defaultNodeId 存在。
  - NPC locationIds 和 schedule.locationId 存在。
  - Location.npcIds 引用已知 NPC。
  - relationship 两端 NPC 存在。
  - portraitId 对应 Resources 中的 Sprite；缺图给警告，不阻止保存。

- [x] H4-4：验证器自动测试
  - 为每类新增规则准备一个最小坏数据测试。
  - 当前 Scenario_01 达到 0 Error；如保留 Warning，逐条写明原因。

- [x] H4-5：第一章通关走查
  - 跑通至少一个好/中立结局和一个战斗相关出口。
  - 记录每个检定失败后的去向，确保失败仍能继续。
  - 结局后返回主菜单；重新新游戏时旧旗标、物品和节点不会残留。

## 注意

- 剧情编辑器“保存”会重写多个 JSON 文件。只为验证时不要随手点击保存。
- 若必须保存，先检查 `git diff`，确认没有无关实体数据被机械重排或覆盖。

---

# Batch H5 · 地点解锁与调查规则

## 目标

侧栏地点不能绕过剧情条件；等待行为只在自由调查时使用；玩家能理解为什么某条路暂时不可走。

## 实施任务

- [x] H5-1：建立地点访问条件
  - 为 LocationDefinition 增加最小、数据驱动的访问条件，或建立等价规则层。
  - `cafe_main`：始终可访问。
  - `cafe_storage`：按当前设计保持可访问。
  - `cafe_basement`：至少需要 `rusty_key`；如创作决定还需要旗标，应先让妹妹确认。
  - 不要把地点 ID 和条件硬编码进 GameplayUI。

- [x] H5-2：侧栏反馈
  - 未解锁地点可以隐藏，或显示禁用按钮与简短原因；选择一种一致规则。
  - 如果显示禁用项，应区分“缺物品”和“时间不对”。

- [x] H5-3：时间行为
  - 等待只能在 Exploration 模式调用。
  - “下一天”明确到达次日上午。
  - NPC 日程变化后侧栏立即刷新。
  - 黑衣女人仅在地下室夜间出现；其他时段不可见。

- [x] H5-4：回归
  - 没钥匙时不能从侧栏直接进地下室。
  - 获得钥匙后可进入。
  - 傍晚→夜间和夜间→次日上午切换正确。
  - 时间变化不会重复执行当前剧情节点副作用。

---

# Batch H6 · 第一阶段动画：静态头像的演出系统

## 目标

在没有逐帧素材的前提下，先做稳定、克制的 UI 演出：头像入场、切换、说话强调和退场。不要伪装成完整角色动画系统。

## 技术判断

- 当前只有四张静态 PNG，没有动画帧、骨骼或 Animator Controller。
- 第一阶段使用 `CanvasGroup + RectTransform + Coroutine` 完成 UI 动画。
- 不引入 DOTween，不建立一大批无法使用的 Animator 状态机。
- 所有动画必须允许静态回退；低配或快速推进时不能阻塞剧情。

## 建议新增文件

- `Assets/Scripts/UI/PortraitPresenter.cs`
- 如需要：`Assets/Scripts/UI/DialogueTextPresenter.cs`
- 测试放在 `Assets/Tests/EditMode/` 或 `Assets/Tests/PlayMode/`。

## 实施任务

- [x] H6-1：封装 PortraitPresenter
  - 组件自行持有 Image、CanvasGroup 和 RectTransform 引用。
  - 最小 API：`Show(sprite)`、`Hide()`、`SetTalking(bool)`、`PlayEmotion(...)`，可按项目命名调整。
  - 新动画开始前取消旧协程，避免快速切换角色时多个动画争夺透明度和位置。
  - 使用 `Time.unscaledDeltaTime`，避免以后暂停时卡住 UI。
  - Image 保持比例且不拦截射线。

- [x] H6-2：定义第一版视觉参数
  - 入场：约 0.2 秒淡入并横向移动 12–24 px。
  - 同一角色连续说话：不重复完整入场。
  - 更换角色：旧头像短淡出，再显示新头像。
  - 旁白：隐藏头像。
  - Talking：轻微亮度或缩放强调；避免持续大幅呼吸导致视觉疲劳。
  - Emotion：先只支持一个短促强调动作；无配置时安全忽略。

- [x] H6-3：接入 GameplayUI
  - GameplayUI 不再直接操作 `s_portraitImage.sprite` 和 frame active。
  - `ShowPortrait(node)` 只负责解析 portraitId，具体过渡交给 Presenter。
  - NPC 没图时隐藏头像并保留文本，不报 NullReference。
  - 进入战斗与结局时明确切换头像状态。

- [x] H6-4：动画测试入口
  - 优先建立独立的最小 AnimationLab 场景或开发用测试组件。
  - 测试入口至少能切换 bear/fox/rabbit/swan、旁白隐藏、快速连续切换。
  - 如果新增场景，需要明确命名；不要修改现有三个正式场景内容。

- [x] H6-5：验证
  - 快速连续点击十次，不出现头像卡在半透明或错误位置。
  - 同一 NPC 连续两句不反复闪烁。
  - NPC→旁白→NPC 切换正确。
  - 4:3、16:9 和较小 Game View 下头像不越界。

## 本批次明确不做

- 不做逐帧走路动画。
- 不做 Spine/Live2D。
- 不为四张静态图硬造 Animator Controller。
- 不批量生成 500 张头像。

---

# Batch H7 · 第二阶段对话表现

## 依赖

H2 交互门控和 H6 PortraitPresenter 完成。

## 实施任务

- [x] H7-1：逐字显示
  - 使用 TMP `maxVisibleCharacters` 或等价方式，不反复拼接字符串制造 GC。
  - 第一击“继续”只立即显示完整文本；第二击才推进节点。
  - 选项在正文显示完成后出现，或明确选择“同步出现”；全项目保持一致。

- [x] H7-2：跳过与双击安全
  - 快速双击不会跨过两段文本。
  - 切换节点会取消旧的逐字协程。
  - 返回主菜单/切场景后不残留静态事件引用。

- [x] H7-3：数据字段
  - 如需要表情或动作字段，在 StoryNodeData 增加可选字符串/枚举映射。
  - 旧 JSON 不填字段时表现与当前一致。
  - StoryValidator 检查未知动作名并给 Warning。

- [x] H7-4：可访问性
  - 提供关闭逐字或把速度设为立即显示的入口；第一版可先集中成常量/设置对象，不必立即制作完整设置菜单。

---

# Batch H8 · 调查玩法闭环与内容补强

## 实施任务

- [x] H8-1：画出 Scenario_01 节点流
  - 标记入口、检定、失败回路、物品、旗标、战斗和四个结局。
  - 确认每个非 End 节点至少存在一个合法出口。

- [x] H8-2：完成一条清晰闭环
  - `调查 → 获得线索/物品 → 解锁新选择 → 进入终局 → 结局`。
  - 失败检定保留替代路线，不让一次坏骰子永久堵死。

- [x] H8-3：条件反馈
  - 当前选项只隐藏不满足条件的项；评估改为“禁用 + 原因”。
  - 至少让玩家知道地下室需要钥匙、仪式选项需要什么线索。

- [x] H8-4：物品与关系
  - 获得物品时有明确反馈。
  - 消耗品行为和描述一致。
  - 关系解锁先做日志或轻提示，不做复杂关系面板。

---

# Batch H9 · 美术、UI、体积与构建

## 实施任务

- [x] H9-1：头像资源整理
  - 复核后确认店猫已经使用正式灰猫图，不再按过期清单误覆盖为新素材。
  - 核对五名 NPC 的头像映射。
  - 统一文件名、portraitId 和导入设置。
  - 修正 README 中“500 张头像”与实际资源数量不一致的问题。

- [x] H9-2：滚动与分辨率
  - 长文本、侧栏和日志使用可靠的 ScrollRect/裁剪。
  - 检查 1920×1080、1600×900、1280×720、16:10 和较小窗口。
  - 所有按钮仍可点击，文本不越界。

- [x] H9-3：资源体积
  - 当前四张 PNG 约 4–7 MB/张，Noto 字体约 17.8 MB。
  - 优先调整 Unity 纹理压缩与 Max Size，不直接破坏源图。
  - 字体子集化必须先验证所有中文字符覆盖；缺字比体积大更糟。

- [x] H9-4：第三方许可
  - 保留 Noto Sans SC 的 OFL 文件。
  - 整理其他外部素材来源、作者和许可证。

- [x] H9-5：Windows 开发构建
  - Development Build 成功。
  - 在项目外的干净目录启动。
  - 验证新游戏、存档、退出、继续、一个结局。
  - 检查构建后的日志无新增异常。

---

## 2. 推荐执行顺序

1. `H0` 可重复验证基线
2. `H1` 存档/读档安全
3. `H2` 交互状态门控
4. `H3` 战斗闭环
5. `H4` 验证器与完整结局
6. `H5` 地点与时间规则
7. `H6` 静态头像演出
8. `H7` 对话逐字与表情指令
9. `H8` 调查内容闭环
10. `H9` 美术、UI、体积和构建

不要先做 H6 动画再回头修 H1–H3。当前最危险的是状态正确性，不是头像还不够会动。

## 3. 给 High 的任务模板

妹妹之后可以把下面这段直接发给 High，并把批次号替换掉：

```text
阅读项目根目录 AGENTS.md 和 TODO.md。只执行 Batch H1，不开始后续批次。

先检查现有工作区和相关代码，保留所有用户未提交修改。按 TODO 中的范围、实施任务和验收矩阵完成修改；在 Unity 中实际 Play 验证，退出 Play Mode 后再汇报。

未经允许不要提交、推送、删除资源、安装依赖或扩大重构。完成后说明：
1. 修改的文件与行为；
2. 自动测试和 Play Mode 路径；
3. Console 状态；
4. 未覆盖风险。
```

## 4. 执行记录

High 每完成一个批次，在这里追加一条，不重写历史：

- [x] H0 — 完成（2026-08-20，EditMode 7/7 通过）
- [x] H1 — 完成（2026-08-20，EditMode 21/21 通过）
- [x] H2 — 完成（2026-08-21，EditMode 26/26 通过）
- [x] H3 — 完成（2026-08-22，EditMode 45/45、PlayMode 1/1 通过）
- [x] H4 — 完成（2026-08-22，EditMode 57/57、PlayMode 2/2 通过）
- [x] H5 — 完成（2026-08-22，EditMode 62/62、PlayMode 3/3 通过）
- [x] H6 — 完成（2026-08-22，EditMode 66/66、PlayMode 5/5 通过）
- [x] H7 — 完成（2026-08-22，EditMode 68/68、PlayMode 7/7 通过）
- [x] H8 — 完成（2026-08-23，EditMode 76/76、PlayMode 8/8 通过）
- [x] H9 — 完成（2026-08-23，EditMode 85/85、PlayMode 9/9、Windows 构建冒烟通过）
- [x] SP-1D — 完成（2026-08-23，EditMode 106/106、PlayMode 9/9、正式身份、烟测隔离保护与两阶段迁移冒烟通过）
- [x] SP-2B-1 — 完成（2026-08-23，EditMode 110/110、PlayMode 9/9、雨夜开场与三条人物线入口落地）

记录格式：

```text
YYYY-MM-DD · Batch Hx
- 状态：完成 / 部分完成 / 阻塞
- 主要改动：
- 验证证据：
- 遗留风险：
- 下一批次前必须知道的事：
```

2026-08-20 · Batch H0
- 状态：完成
- 主要改动：新增独立 EditMode 测试程序集；覆盖 GameTime 时段推进、ParsePeriod 安全回退，以及 StoryValidator 对重复节点、缺失跳转目标和不存在 startNode 的识别。
- 验证证据：Tuanjie Editor 1.8.0 完成资源刷新与脚本编译；EditMode Test Runner 7/7 通过、0 失败、0 跳过；Play Mode 跑通“主菜单 → 新游戏 → 角色创建 → 开始调查 → 储藏室 → 店猫对话”，验证后已退出 Play Mode；Console 为 1 条既有 Steam 未启用信息、0 Warning、0 Error；`git diff --check` 退出码 0。
- 遗留风险：H0 仅覆盖最小纯逻辑基线，存档、交互门控和战斗风险留给后续批次；现有 Steam 信息日志不影响运行。
- 下一批次前必须知道的事：测试程序集为直接测试真实 StoryValidator，除 Runtime 与 Test Framework 外还引用现有 WalkingIntoNight.Editor；测试不调用 SaveSystem，也未读取或覆盖玩家存档。

2026-08-20 · Batch H1
- 状态：完成
- 主要改动：为存档加入版本和统一槽位校验，提供失败不抛到 UI 的安全读写与临时文件替换；读入前验证场景、节点、地点、角色和时间；新增读档专用恢复入口，避免 `giveitem`、`changesan`、`advancetime` 与 `setflag` 重复结算；完善主菜单继续失败和游戏内保存反馈；新增隔离路径的存档与副作用恢复测试。
- 验证证据：Tuanjie Editor 1.8.0 刷新编译完成；EditMode Test Runner 全部 21/21 通过、0 失败、0 跳过。Play Mode 使用本次生成的独立临时存档根完成槽位 1 全矩阵：大厅保存与继续、储藏室/night/角色/节点/地点/背包恢复；`rusty_key` 读档后再保存仍恰好 1；SAN 45→43 后读档再保存仍为 43；`mei_trust` 读档后仍恰好 1；损坏 JSON 留在主菜单显示继续失败，随后新游戏仍进入角色创建。验证后已退出 Play Mode；最终 Console 为 1 条既有 Steam 未启用信息、0 Warning、0 Error。
- 遗留风险：战斗中仍按既有规则禁止存档，未实现战斗状态序列化；真实玩家存档未作为测试材料，因此未覆盖真实历史文件的所有未知畸形组合；当前主菜单会附带底层 JSON 解析文本，功能正确但文案可在后续单独收敛。
- 下一批次前必须知道的事：UI 仍只使用槽位 1；存档单元测试与 Play 验收应使用可注入或带专用哨兵的环境变量隔离路径，自动化测试缺少隔离配置时必须失败关闭；读档恢复会重现有文本副作用节点的页面，但不会再次改变状态。

2026-08-21 · Batch H2
- 状态：完成
- 主要改动：新增 Narrative / Exploration / Combat / End 单一交互模式与模式变化事件；仅在 `hub_explore` 数据节点允许自由探索；Runner 对地点、NPC、等待提供最终权限守门；GameplayUI 统一同步地点、NPC、等待和存档按钮状态；节点展示加入版本令牌与 0.75 秒输入冷却，拦截旧按钮及连续点击。
- 验证证据：Tuanjie Editor 1.8.0 编译完成；EditMode Test Runner 全部 26/26 通过、0 失败、0 跳过，新增测试覆盖非探索状态拒绝且不改变状态、Hub 模式事件、End 门控、连续继续与过期选项。Play Mode 实走“序章双击继续 → Hub 等待 → 小梅对话 → 储藏室取得钥匙 → 地下室影鼠战斗 → 失败结局”：序章双击只前进一页；Hub 可推进上午到下午；对话中地点/NPC/等待失效，返回后恢复；战斗中地点/NPC/等待/存档失效；结局只保留返回主菜单。验证后已退出 Play Mode；清空旧记录后的最终 Console 为 1 条既有 Steam 未启用信息、0 Warning、0 Error。
- 遗留风险：H2 只负责交互门控；战斗坏配置恢复、结束顺序和完整胜负/逃跑矩阵仍留给 H3。0.75 秒冷却是当前 UI 防跨节点双击的保守值，后续若加入逐字动画可根据实际手感统一调整。
- 下一批次前必须知道的事：GameplayUI 不再自行猜测是否处于 Hub，而是订阅 Runner 模式；即使将来 UI 漏禁按钮，Runner 仍会拒绝非 Exploration 的探索命令。现场曾出现 4 条 Missing Script 旧记录，但场景中 2 个脚本 GUID 均能解析为 URP AdditionalCameraData/Light2D，全项目脚本引用核对为 0 个未解析；清空并重新编译、测试、Play 后未复现，因此未删除任何场景组件。

2026-08-22 · Batch H3
- 状态：完成
- 主要改动：战斗启动改为可失败且不进入空 UI；统一胜利、失败与逃跑的 HP 同步、状态清理和剧情跳转顺序，并为缺失出口增加安全回退；闪避会令本轮所有敌方攻击承受 1 个惩罚骰；战斗界面显示轮次及双方 HP、隐藏旧头像，并以回合版本阻止过期按钮重复行动；掷骰与伤害随机入口可注入，测试不再依赖运气。
- 验证证据：Tuanjie Editor 1.8.0 无界面完成脚本编译；EditMode Test Runner 45/45 通过、0 失败、0 跳过，覆盖坏配置、胜负、逃跑、闪避、状态清理、缺失出口和狂热侍从实际剧情出口；PlayMode 1/1 通过，程序化点击“闪避 → 攻击 → 继续 → 逃走”，验证旧按钮销毁、影鼠胜利回 Hub 与逃跑回 Hub。除 1 条既有未使用字段编译警告及无图形/联网授权环境诊断外，未发现本批游戏代码错误。
- 遗留风险：本批没有控制鼠标或打开可见 Unity 窗口，因此未做人工视觉手感检查；按钮布局、文字换行和节奏仍需后续在可见编辑器中由人眼确认。战斗规则仍是最小原型，不含道具、状态效果或敌人差异化 AI。
- 下一批次前必须知道的事：战斗按钮携带创建时的回合号，回合刷新后的旧按钮即使收到迟到点击也会被 CombatManager 拒绝；后续 H4 可直接围绕剧情图验证和第一章完整通关推进，不必再次改写战斗结算。

2026-08-22 · Batch H4
- 状态：完成
- 主要改动：修正验证结果的单行格式；验证器新增节点类型、必要出口、检定字段、战斗配置、物品与地点引用，以及 NPC 默认节点/地点/日程、地点 NPC、关系端点和头像资源规则；头像缺失只产生 Warning，其余可能造成软锁或坏引用的问题产生 Error；ScenarioRunner 的剧情检定入口改为可注入，以便通关测试不依赖随机数。
- 验证证据：Tuanjie Editor 1.8.0 无界面完成编译；EditMode 57/57 通过、0 失败、0 跳过，当前 Scenario_01 验证为 0 Error、0 Warning。PlayMode 2/2 通过：保留 H3 的战斗 UI 回归，并实际点击完成“雨夜开场 → 发现银币 → 午夜事件 → 中立结局 → 返回主菜单 → 新游戏 → 角色创建”，确认旧旗标、物品、节点、调查员和战斗返回状态均已清空。
- 遗留风险：本批验证结构与引用，不判断文案质量、数值平衡或所有条件组合是否都有可见选项；仍未使用可见 Unity 窗口做人工排版与节奏检查。无界面日志保留 1 条既有未使用字段编译警告，以及无图形/联网授权环境诊断，不属于本批游戏逻辑错误。
- 下一批次前必须知道的事：8 个检定失败出口均由测试记录；其中午夜理智检定已在 H8 从“随机失败直接进入 `end_madness`”改为 `midnight_failure_choice`，玩家可以付出 SAN 继续或主动回应歌声。地点权限已在 H5 收口，侧栏不能绕过钥匙直接进入地下室。

2026-08-22 · Batch H5
- 状态：完成
- 主要改动：为 LocationDefinition 增加物品、旗标与时段访问条件，并由 LocationAccessEvaluator 统一返回允许状态、阻塞类型和玩家可读原因；地下室数据要求 `rusty_key`，大厅与储藏室保持开放；ScenarioRunner 在最终旅行入口再次守门，GameplayUI 以禁用按钮显示“需要物品”或“仅在某时段可进入”；剧情编辑器可编辑新字段，验证器会阻止未知物品和非法时段条件。
- 验证证据：Tuanjie Editor 1.8.0 无界面完成编译；EditMode 62/62 通过、0 失败、0 跳过，覆盖无钥匙拒绝、有钥匙放行、物品/时间阻塞原因、傍晚→夜间、夜间→次日上午、黑衣女人日程，以及等待不重复当前节点副作用。PlayMode 3/3 通过：侧栏实际显示并禁用“地下室（需要「生锈的钥匙」）”，取得钥匙后立即解锁并进入地下室；黑衣女人在傍晚不可见、夜间出现、第二天上午再次隐藏。H3 战斗和 H4 第一章通关同时回归通过。
- 遗留风险：当前只有地下室使用真实访问条件，时间门控仅由通用规则和测试覆盖，尚未给现有地点配置时段限制；旗标阻塞统一显示“尚未解锁”，以后若需要更具体的剧情提示，可再加入数据化文案字段。仍未打开可见 Unity 窗口做人工布局检查。
- 下一批次前必须知道的事：地点权限不能只靠侧栏按钮，任何新旅行入口都应调用 ScenarioRunner.TravelToLocation 或 LocationAccessEvaluator；本批按计划只要求地下室钥匙，没有擅自追加剧情旗标。下一批 H6 可开始静态头像演出，不需要再改地点规则。

2026-08-22 · Batch H6
- 状态：完成
- 主要改动：新增 PortraitPresenter，以单一可取消协程管理约 0.2 秒淡入与 18 px 滑动、角色更换淡出/淡入、旁白退场、Talking 轻微缩放/亮度和一次短促 Emphasis；动画使用 `Time.unscaledDeltaTime`，可整体关闭并立即落到稳定静态状态。GameplayUI 不再直接操作头像 Image，改由 Presenter 处理；头像容器加入裁切，缺图、战斗和结局均安全隐藏。新增 PortraitAnimationLab 作为开发测试入口，可预览 bear/fox/rabbit/swan 与旁白状态。
- 验证证据：Tuanjie Editor 1.8.0 无界面完成编译；EditMode 66/66 通过、0 失败、0 跳过，覆盖关闭动画的静态回退、Image 保持比例/不拦截射线，以及 1024×768、1920×1080、800×600 三种画面尺寸不越出头像框。PlayMode 5/5 通过：在 `Time.timeScale=0` 下连续快速切换十次并完成动画；同一角色不重新入场；Emphasis 后恢复稳定；NPC→旁白→NPC、缺图/结局隐藏与原有战斗、通关、地点回归全部通过。
- 遗留风险：无界面测试能验证状态、裁切和布局边界，但不能代替人眼判断 0.2 秒节奏是否最舒服；当前 Talking 是静态轻强调，不做持续呼吸，Emotion 只有 Emphasis 一种。项目仍只有静态头像，本批没有伪造逐帧、Spine 或 Live2D。
- 下一批次前必须知道的事：任何新头像表现应调用 PortraitPresenter，不要重新直接改 Image；同一 Sprite 的 Show 会保留现状而不重启动画，新请求会先取消旧协程。下一批 H7 可在此基础上做逐字文本，并需要确保切节点时同时取消旧文本协程。

2026-08-22 · Batch H7
- 状态：完成
- 主要改动：新增 DialogueTextPresenter，使用 TMP `maxVisibleCharacters` 和 `Time.unscaledDeltaTime` 实现逐字显示，不反复拼接字符串；显示期间“继续”只补全文字，补全后设置 0.2 秒防误触窗口，随后再次点击才推进；有选项的节点在正文完成后才生成选项。GameplayUI 为每次展示绑定版本令牌，旧按钮不能补全或推进新节点；切节点、进入战斗和销毁 UI 时会取消旧文本协程与回调。DialoguePresentationSettings 集中提供逐字开关、速度和防误触时间，关闭后立即显示全文。
- 验证证据：Tuanjie Editor 1.8.0 无界面完成编译；EditMode 68/68 通过、0 失败、0 跳过，覆盖静态回退与集中关闭逐字。PlayMode 7/7 通过：`Time.timeScale=0` 时逐字仍自然完成；新文本取消旧回调；第一击补全、同按钮连续触发两次不推进、等待后第二击才前进；旧节点按钮不能补全新文本；正文显示期间只出现“继续”，补全后才出现剧情选项。此前战斗、完整通关、地点与头像演出全部回归通过。
- 遗留风险：当前默认速度为每秒 42 个可见字符，0.2 秒防误触是工程默认值，仍需要后续用可见窗口由人眼确认中文阅读节奏；第一版只有集中设置对象，还没有玩家设置菜单。文本完成后选项才出现是本项目当前统一规则。
- 下一批次前必须知道的事：本批判断暂时不新增表情/动作数据字段，因为现有剧情没有节点需要驱动动作名，避免为未使用功能改写 JSON；若 H8 内容补强确实加入动作字段，应同时给 StoryValidator 添加未知动作 Warning。继续按钮逻辑必须经过 DialogueTextPresenter.CanAdvance 与 Runner 展示版本，不能另写绕过入口。

2026-08-23 · Batch H8
- 状态：完成
- 主要改动：新增 `SCENARIO_01_FLOW.md`，标注入口、检定、失败回路、物品、旗标、战斗与四个结局；选项条件由隐藏改为“保留并禁用”，显示数据化未解锁原因，地下室钥匙与终局仪式线索均有明确反馈；获得物品、新线索和关系解锁使用玩家可读日志，小梅与老陈的关系改为剧情实际支持的“互相照应”；急救包与安神茶统一由 ScenarioRunner 使用，严格按描述恢复数值、只消耗一份，满状态不会浪费；午夜检定失败改为玩家可付出 SAN 继续或主动选择疯狂结局。
- 验证证据：Tuanjie Editor 1.8.0 无界面完成编译；EditMode 76/76 通过、0 失败、0 跳过，覆盖锁定原因、解锁、物品数值/消耗、满状态保护、关系日志、银币完整闭环、失败重试、全节点合法出口与四结局；PlayMode 8/8 通过，实际验证禁用选项及原因、钥匙即时解锁、点击急救包后 HP +3/物品与按钮消失、仪式原因，并回归战斗、通关、地点、头像与逐字演出；`git diff --check` 退出码 0。
- 遗留风险：本批未控制鼠标或打开可见 Unity 窗口；动态增加的禁用原因和消耗品按钮在较小窗口可能使侧栏变长，滚动与多分辨率排版统一留给 H9-2。流程图是工程与测试基线，不代替后续试玩对节奏、难度和文案的人工判断。
- 下一批次前必须知道的事：所有剧情选项都会由 Runner 发布，GameplayUI 再根据 ConditionEvaluator 显示可用或禁用状态；新条件应填写 `unavailableReason`，新旗标可填写 `flagNotice`，剧情编辑器已支持两项字段。物品使用必须走 ScenarioRunner.UseItem，避免 UI 自行改 HP/SAN 或漏发背包刷新事件。下一批进入 H9，优先处理侧栏/长文本滚动和多分辨率，再整理资源与 Windows 开发构建。

2026-08-23 · Batch H9
- 状态：完成
- 主要改动：正文、选项、侧栏与日志改为带裁剪的纵向滚动区域，长按钮按换行自动增高，窗口允许调整大小；新增五档分辨率 PlayMode 回归。复核美术后确认 `shop_cat_v1` 已是正式灰猫图，因此保留现有素材而不做无依据覆盖；统一检查正式头像、兼容头像、背景和字体导入边界，README 修正实际资源数量，并新增 `THIRD_PARTY_NOTICES.md`。新增 Win64 Development Build 脚本和只在开发构建参数启用的隔离存档冒烟流程。
- 验证证据：Tuanjie Editor 1.8.0 无界面完成编译；EditMode 85/85、PlayMode 9/9 通过，覆盖五名 NPC 资源映射、纹理导入约束、字体许可文件，以及 1920×1080、1600×900、1280×720、1280×800、1024×640 下的溢出、滚轮和末项点击。Win64 Development Build 成功生成于项目外目录，约 157.78 MiB；隐藏启动实际 exe 后自动完成新游戏、剧情检定、取得银币、保存、重置、继续和中立结局，进程返回码 0 并记录 `WALKING_INTO_NIGHT_BUILD_SMOKE_PASS`，隔离测试存档已自行删除。
- 遗留风险：自动冒烟使用 `-nographics`，其 Null Graphics Device 会报告预期的 Shader 不支持日志，因此它验证逻辑与构建可启动性，不等于人眼画面验收；仍需在正常显卡模式人工检查滚动手感、中文节奏和实际视觉效果。三张正式 AI 辅助美术缺少可追溯的逐图模型、日期与提示词原始记录，已在第三方记录中如实标注，发布前必须补齐来源档案或替换素材。Steamworks.NET 尚未导入。
- 下一阶段前必须知道的事：`H0–H9` 工程基线已经闭环；下一个高价值工作不是继续堆底层功能，而是按产品路线完成第一章重写、最小调查笔记和陌生玩家试玩。日常实施继续使用 Sol High 即可；只有重定叙事结构、跨系统架构或连续排错失败时再开 Max。

2026-08-23 · SP-1D 产品身份基线
- 状态：完成
- 主要改动：正式锁定 `companyName=Seeunever`、`productName=Walking Into Night`、中文显示名“走入夜境”和 Standalone Identifier `com.seeunever.walkingintonight`；保留 `WalkingIntoNight` 作为 namespace、程序集、仓库和编辑器菜单等内部工程标识。所有 Editor 构建均由公开 API 守门，项目内开发构建另把身份写入 `BUILD_INFO.txt`。新增槽位 1 一次性旧路径迁移：只接受格式与剧情引用均有效的旧档，优先保护有效/未来版本/无法证明更旧的新档；替换前创建唯一回退副本，源文件不删除，原子完成标记阻止二次覆盖和旧档复活。两种开发烟测与命令行 Editor 测试都要求专用绝对路径和各自哨兵，并拒绝 `LocalLow`，不会回落或误指玩家真实存档。
- 验证证据：Tuanjie Editor 1.8.0 无界面完成编译；EditMode 106/106、PlayMode 9/9 通过，覆盖正式身份序列化、全局构建守门、普通烟测目录保护、旧路径推导、旧/新/损坏/未来版本档、时间戳缺失、备份重名、语义损坏重试、完成标记、槽位隔离和重复启动。项目外 Win64 Development Build 成功；实际 exe 在缺少普通烟测哨兵时返回码 1 且未生成存档，加入哨兵后完成新游戏、剧情检定、物品、保存/重载和结局并返回码 0。另以带专用哨兵的项目外沙盒启动同一 exe 两次：首次旧档与新档 SHA-256 相同且“继续”可用；第二次把旧档改为更晚内容后，新档与 marker 哈希均不变，两个进程分别记录 `WALKING_INTO_NIGHT_IDENTITY_MIGRATION_SMOKE_PASS:first/second`。
- 隔离审计：早期一次未配置测试根的 PlayMode 启动曾在真实新目录误写迁移 marker；终审确认旧目录为空、新目录仅有该测试 marker 后，已精确删除 marker，没有删除目录或存档。随后增加命令行 Unity 测试的失败关闭保护，并在带 `.walking_into_night_editor_tests` 哨兵的隔离根重新跑过 EditMode 106/106 与 PlayMode 9/9；隔离根只留下哨兵，真实新目录保持为空。第一次人工试玩时仍应确认旧开发存档能在新标题下继续。本次 Tuanjie Win64 构建实测 `Application.identifier` 为空，Identifier 由构建前 API 与序列化测试锁定；真正决定 Windows 存档目录的 Company 与 Product 已在实际 Player 中验证。未控制鼠标或打开可见 Unity 窗口。
- 下一阶段前必须知道的事：发布身份以后不要随意更改 Company 或 Product；若必须改名，应新增迁移版本而不是覆盖当前 marker。下一批进入 `SP-2B` 第一章重写，日常使用 Sol High 即可；涉及整章分支重排或调查笔记跨系统设计时再开 Max。

2026-08-23 · SP-2B-1 雨夜开场与三线入口
- 状态：完成
- 主要改动：把开场改为匿名邮件与午夜约定，让玩家以委托、记忆或善意选择来访动机并记录互斥 flag；进入咖啡馆后以三只杯子、最后一盏灯和慢半拍的店猫倒影建立悬念；邮件、第三只杯子、侦查成功与侦查失败都会汇合到大厅三线入口，玩家可直接选择小梅、老陈、店猫或自由调查。小梅、老陈与店猫入口对白完成第一轮改写；跟随店猫记录 `cat_guided` 后安全回到 Hub。旧节点 ID 与类型全部保留，未扩展运行时 schema。
- 验证证据：`nodes.json` 可解析，共 55 个节点、0 重复 ID、0 悬空引用；新增测试从真实动机选择断言三个动机 flag 恰好一个生效，覆盖邮件与第三只杯子汇合、三个人物入口、店猫引路、失败回路，并从 `startNodeId` 遍历确认四个既有结局仍可抵达。Tuanjie Editor 1.8.0 隔离运行 EditMode 110/110、PlayMode 9/9，0 失败、0 跳过；PlayMode 实际点击新开场并回归中立结局、新游戏清理和逐字显示交互。
- 遗留风险：本批没有人工计时，仍需试玩判断开场能否在约 90 秒内建立目标又不过密；三个动机目前只记录风味，尚未在后续对白回响；三条人物线只完成入口，完整内容仍沿用原型。旧开发存档不会因节点缺失失效，但保留的旧 ID 已有少量语义更新，首次人工试玩仍应留意恢复页面是否自然。
- 下一批次前必须知道的事：使用 Sol High 执行 `SP-2B-2`，范围只含“小梅——害怕不是背叛”。保留 `npc_mei_talk`、`check_psych_mei`、`mei_reveal` 与 `hub_explore`，落地 `mei_asked / mei_trust / mei_last_order / mei_story_complete`，让检定失败和不检定的温和行动都能前进；不要同时改老陈、店猫、推理、结局、调查笔记或 Steam SDK。

2026-08-24 · v0.1.0-alpha.1 内部纵切片检查点
- 状态：完成
- 主要改动：把产品版本锁定为 `0.1.0-alpha.1`，并由 `ProductIdentity`、Unity PlayerSettings、构建前守门、测试与 `BUILD_INFO.txt` 共同防漂移；新增 Changelog 和里程碑边界。发布审计排除了个人 `.vscode`、Python 缓存、TMP 未用文档 / Emoji 示例，以及会进入构建的通义万相水印头像副本；小梅、老陈和店猫保留正式运行时头像，黑衣女人与老板影子在正式图完成前使用无头像回退。快速头像切换测试另抓到“切回当前角色后旧换人协程仍覆盖画面”的竞态，已通过取消过期切换修复。
- 验证证据：最终工作树由 Tuanjie 2022.3.62t2 隔离运行 EditMode 110/110、PlayMode 9/9，0 失败、0 跳过；项目外 Win64 Development Build 成功，`BUILD_INFO.txt` 记录 `Version=0.1.0-alpha.1`，总目录约 156.46 MiB；真实 exe 以专用哨兵目录完成新游戏、剧情检定、物品、保存 / 重载与中立结局，退出码 0 并记录 `WALKING_INTO_NIGHT_BUILD_SMOKE_PASS`。烟测后隔离根只剩哨兵，真实 `LocalLow/Seeunever/Walking Into Night` 保持为空。
- 构建位置：`E:\WalkingIntoNight-v0.1.0-alpha.1-Win64-20260824\Walking Into Night.exe`。这是内部 Development Build，不是 Steam Demo RC。
- 遗留风险：尚未做正常显卡窗口的人眼验收；黑衣女人与老板影子正式头像、剩余背景和音频仍缺；2026-08-21 AI 美术的完整提示词档案、旧提交历史中的水印素材审计、外部试玩与 Steamworks 接入仍是发布前事项。下一批继续 `SP-2B-2`，使用 Sol High。
