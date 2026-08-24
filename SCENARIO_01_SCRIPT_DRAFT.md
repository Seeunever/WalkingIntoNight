# Scenario_01 剧本草案 ·《雨停之前》

> 状态：可执行草案；第 5 节开场与三条人物线入口已由 SP-2B-1 写入 `nodes.json`，第 6–12 节仍待落地
> 目标：45–70 分钟首次通关，完整调查不超过约 90 分钟
> 目标规模：70–90 个节点、约 8,000–12,000 个中文字符
> 依赖：[NARRATIVE_BIBLE.md](NARRATIVE_BIBLE.md) 与 [SINGLE_PLAYER_STEAM_ROADMAP.md](SINGLE_PLAYER_STEAM_ROADMAP.md)

## 1. 改写原则

- 保留现有地点、NPC、物品 ID 和主节点 ID，尽量只增补节点，降低旧代码与测试受影响范围。
- 现有存档尚未公开发布，但改写节点前仍要决定内部旧档是迁移、提示不兼容，还是仅在开发期清理；不能静默读到错误分支。
- 本文样稿只使用当前 `StoryNodeData` 已支持的字段：`dialogue/check/setflag/giveitem/changesan/location/advancetime/combat/end`、单个 `requiredFlag`、单个 `requiredItemId` 和 `blockedByFlag`。
- 多线索条件通过连续推理节点合成 `best_route_ready`，第一版不强迫扩展条件表达式。
- 每个关键检定只出现一次。失败后进入新状态和替代行动，不能原地重掷。
- 午夜精神检定失败只造成代价，不直接结束章节。
- 战斗失败从午夜前安全点重试；战斗是危险路线，不是最好结局门票。

## 2. 现有节点必须修正的问题

1. `spot_fail` 后仍需提供银币的备用来源，不能让 `found_coin` 永久缺失并卡住午夜。
2. `midnight_event` 不再只以 `found_coin` 为入口，也不能在上午触发“午夜”。
3. `storage_key`、`give_diary`、`read_diary` 需要一次性 flag；不得重复给物品或反复扣 SAN。
4. `rusty_key` 的描述改成“地下室旧门钥匙”，与实际用途一致。
5. `mei_trust`、老陈历史、店猫倒影线必须在终局汇合。
6. `midnight_san_check` 失败改为 `changesan → deduction_start`，不直接 `end_madness`。
7. 最终选择不能出现“没有仪式 / 银币时只剩反复战斗”的循环。
8. 黑衣女人从单纯挡路者改为五十年前的失踪者与守门人。

## 3. 剧情节拍

| 时长 | 段落 | 玩家目标 | 必须发生 |
|---:|---|---|---|
| 0–5 分钟 | 匿名邮件与进店 | 明白案件与自己的来意 | 三只杯子、关店、失踪老板、午夜期限 |
| 5–25 分钟 | 大厅三线调查 | 认识小梅、老陈、店猫 | 至少得到一条人的线索和一条物理线索 |
| 25–40 分钟 | 储藏室与地下室 | 找到日记、钥匙、符号 | 明白老板主动进镜、黑衣女人并非普通敌人 |
| 40–55 分钟 | 午夜推理 | 组合三类证据 | 人的锚点、循环真相、仪式方法逐层核对 |
| 55–70 分钟 | 选择与清晨 | 接受选择的代价 | 三个正式结局之一；失败可从安全点重试 |

## 4. 状态表

### 保留并赋予用途

| 状态 | 用途 |
|---|---|
| `found_coin` | 已确认银币及五十年前日期；开放银币结局与老陈追加对白 |
| `mei_trust` | 小梅愿意参与终局，不再只是一次奖励文字 |
| `know_ritual` | 理解拓片 / 镜面规则；开放封门结局与最佳推理 |
| `woman_pass` | 黑衣女人允许玩家靠近镜面，免去一次危险冲突 |

### 建议新增 flags

| flag | 设置时机 | 作用 |
|---|---|---|
| `motive_duty` / `motive_memory` / `motive_kindness` | 开场动机 | 少量风味文本与结局回响 |
| `mei_asked` | 第一次尝试追问 | 防止重复心理学检定 |
| `mei_last_order` | 得知三只杯子的意义 | 最佳推理第一层 |
| `mei_story_complete` | 小梅线完成 | 解锁关店后生活对白 |
| `chen_history` | 得知五十年前循环 | 推理与黑衣女人身份 |
| `chen_truth` | 老陈承认失踪者是姐姐 | 最佳结局的叫名演出 |
| `cat_guided` | 跟随店猫调查 | 记录无骰线索路线 |
| `cat_mirror` | 发现倒影慢半拍 | 解释异常依附镜面 |
| `diary_read` | 日记首次阅读 | 防重复 SAN 与文本 |
| `owner_entered_willingly` | 对照日记后 | 终局问题答案 |
| `black_woman_named` | 老陈 / 银币确认身份 | 黑衣女人完整结局演出 |
| `midnight_started` | 确认进入终局 | 防重复午夜与安全存档点 |
| `best_route_ready` | 连续推理完成 | 最佳结局条件 |

### 物品

继续使用现有 `silver_coin`、`rusty_key`、`owner_diary`、`strange_symbol`。第一版不再创建一堆只在背包里占行的线索物品；其余知识进入调查笔记 / flag。

## 5. 场景一：邮件、动机与三只杯子

### 演出目标

- 90 秒内出现“匿名求救、午夜、亮灯的关店咖啡馆”。
- 玩家用一个选择定义自己为何来，而不是先刷一页属性。
- 三只杯子成为贯穿全章的视觉母题。

### 节点样稿

```json
[
  {
    "id": "intro_01",
    "type": "dialogue",
    "speaker": "旁白",
    "text": "邮件没有署名，只有一句话：如果你看见它，说明我已经错过了打烊。请在午夜前来夜航咖啡馆，别让里面只剩一盏灯。",
    "nextNodeId": "intro_motive"
  },
  {
    "id": "intro_motive",
    "type": "dialogue",
    "speaker": "旁白",
    "text": "雨落在车窗上。你仍然可以掉头。你为什么继续往前？",
    "choices": [
      { "text": "这是委托。我来把事情做完。", "nextNodeId": "set_motive_duty" },
      { "text": "这扇门让我想起一个等过人的地方。", "nextNodeId": "set_motive_memory" },
      { "text": "有人求救，我不想假装没看见。", "nextNodeId": "set_motive_kindness" }
    ]
  },
  {
    "id": "set_motive_duty",
    "type": "setflag",
    "flag": "motive_duty",
    "flagValue": true,
    "text": "你把邮件归进委托记录，推门下车。",
    "nextNodeId": "intro_02"
  },
  {
    "id": "set_motive_memory",
    "type": "setflag",
    "flag": "motive_memory",
    "flagValue": true,
    "text": "你没有继续追究那个念头。至少今晚没有。",
    "nextNodeId": "intro_02"
  },
  {
    "id": "set_motive_kindness",
    "type": "setflag",
    "flag": "motive_kindness",
    "flagValue": true,
    "text": "这理由不够专业，但足够让你在雨里撑开伞。",
    "nextNodeId": "intro_02"
  },
  {
    "id": "intro_02",
    "type": "dialogue",
    "speaker": "旁白",
    "text": "门牌已经翻到「休息」，里面却亮着最后一盏暖黄的灯。吧台摆着三只杯子：两杯咖啡已经冷透，第三只没有倒咖啡，杯柄朝门，瓷壁还留着刚被热水烫过的薄雾。",
    "locationId": "cafe_main",
    "nextNodeId": "intro_cat_mirror"
  },
  {
    "id": "intro_cat_mirror",
    "type": "dialogue",
    "speaker": "旁白",
    "text": "风铃只响了一声。店猫从你脚边绕开，宁可贴着桌腿，也不肯经过墙上的大镜子。你抬头时，它的倒影迟了半拍才转过脸。",
    "nextNodeId": "set_cat_mirror_intro"
  },
  {
    "id": "set_cat_mirror_intro",
    "type": "setflag",
    "flag": "cat_mirror",
    "flagValue": true,
    "flagNotice": "店猫的倒影比它慢了半拍。",
    "nextNodeId": "intro_03"
  },
  {
    "id": "intro_03",
    "type": "dialogue",
    "speaker": "店员小梅",
    "text": "“我们已经打烊了。”柜台后的小梅看了一眼没有落锁的门，又改口，“应该说，我们本来打算永远打烊。”",
    "choices": [
      { "text": "出示匿名邮件", "nextNodeId": "intro_email_shown" },
      { "text": "先问第三只杯子给谁", "nextNodeId": "intro_third_cup" },
      { "text": "观察大厅环境", "nextNodeId": "check_spot_main" },
      { "text": "先自己看看", "nextNodeId": "intro_hall_threads" }
    ]
  }
]
```

落地版中，邮件、第三只杯子、侦查成功与侦查失败都会汇合到 `intro_hall_threads`，再明确提供小梅、老陈、店猫和自由调查四个入口。完整出口以 [SCENARIO_01_FLOW.md](SCENARIO_01_FLOW.md) 与 `nodes.json` 为准。

## 6. 场景二：小梅——害怕不是背叛

### 人物目标

小梅以为自己昨晚先离开，等于抛下老板。玩家必须先让她停止为“害怕”道歉，才能得到三只杯子的完整含义。

### 节点样稿

```json
[
  {
    "id": "npc_mei_talk",
    "type": "dialogue",
    "speaker": "店员小梅",
    "text": "“老板不是会失约的人。他哪怕发烧，也会等我走过街口再关最后一盏灯。昨晚灯没有灭，我却先走了。”",
    "choices": [
      {
        "text": "心理学：请她把昨晚慢慢说一遍",
        "nextNodeId": "mei_mark_asked",
        "blockedByFlag": "mei_asked"
      },
      {
        "text": "不问案子，先陪她倒掉冷咖啡",
        "nextNodeId": "mei_warm_open",
        "blockedByFlag": "mei_story_complete"
      },
      {
        "text": "问她关店以后打算做什么",
        "nextNodeId": "mei_aftercare",
        "requiredFlag": "mei_story_complete"
      },
      { "text": "暂时离开", "nextNodeId": "hub_explore" }
    ]
  },
  {
    "id": "mei_mark_asked",
    "type": "setflag",
    "flag": "mei_asked",
    "flagValue": true,
    "nextNodeId": "check_psych_mei"
  },
  {
    "id": "check_psych_mei",
    "type": "check",
    "speaker": "心理学",
    "text": "你没有追问地下室，只问昨晚是谁替她关了灯。",
    "skillId": "psychology",
    "difficulty": 0,
    "successNodeId": "mei_set_trust",
    "failureNodeId": "mei_check_fail"
  },
  {
    "id": "mei_check_fail",
    "type": "dialogue",
    "speaker": "店员小梅",
    "text": "小梅把纸杯捏出一道折痕：“你们问问题的人，怎么都只关心他去了哪里，不问他为什么要一个人下去？”",
    "nextNodeId": "hub_explore"
  },
  {
    "id": "mei_warm_open",
    "type": "dialogue",
    "speaker": "旁白",
    "text": "你没有再问。热水流过杯底，把凝住的糖浆一点点冲开。小梅看着那道旋涡，终于松开攥紧的手。",
    "nextNodeId": "mei_warm_time"
  },
  {
    "id": "mei_warm_time",
    "type": "advancetime",
    "speaker": "旁白",
    "text": "你们花了一段时间收拾吧台。窗外的天色又暗了一层。",
    "advancePeriods": 1,
    "nextNodeId": "mei_set_trust"
  },
  {
    "id": "mei_set_trust",
    "type": "setflag",
    "flag": "mei_trust",
    "flagValue": true,
    "speaker": "店员小梅",
    "text": "“他最后煮了三杯。给我加奶，给老陈不加糖，第三杯空着。他说，等人到齐，就叫我的名字。”",
    "nextNodeId": "mei_set_last_order"
  },
  {
    "id": "mei_set_last_order",
    "type": "setflag",
    "flag": "mei_last_order",
    "flagValue": true,
    "nextNodeId": "mei_story_complete"
  },
  {
    "id": "mei_story_complete",
    "type": "setflag",
    "flag": "mei_story_complete",
    "flagValue": true,
    "nextNodeId": "hub_explore"
  },
  {
    "id": "mei_aftercare",
    "type": "dialogue",
    "speaker": "店员小梅",
    "text": "“还不知道。也许先睡一天，再找一家早上开门的店。”她想了想，“夜班太会骗人了，让人以为天永远不会亮。”",
    "nextNodeId": "hub_explore"
  }
]
```

这里成功与失败的差异是：成功直接取得信任；失败后玩家仍可通过陪伴行动取得相同关键线索，但付出一段时间。故事没有删，代价成立。

## 7. 场景三：老陈——第二杯不是习惯

### 人物目标

老陈一直声称五十年前的失踪只是旧闻。事实上，失踪者是他的姐姐。他每年同一晚点两杯咖啡，假装第二杯只是老板多做的。

### 关键对白

```text
老陈：报纸上的事，隔得久了，看起来都像发生在别人身上。

玩家：可你把日期圈了五十年。

老陈：（把报纸折好）年轻人，活得久不代表擅长告别。很多时候，只是擅长把同一件事拖到明天。
```

### 建议节点流

`npc_chen_talk → chen_library → [success: chen_clipping_full / failure: chen_clipping_half]`

- 成功：旧剪报完整写出日期、失踪者姓陈、镜面留下盐水。
- 失败：只辨认出日期与半个姓氏；仍设置 `chen_history`，不会堵主线。
- 若持有 `silver_coin`，开放“把银币放在报纸日期旁”：“陈月”两个磨损的字终于对上，设置 `chen_truth` 与 `black_woman_named`。
- 日记提示仍来自第三块松动木板；如果没有赢得老陈信任，店猫也会抓挠同一位置提供备用来源。

建议对白：

```text
老陈：她叫陈月。那年她十八，我十二。后来每个人都说，别再叫了，人回不来。

老陈：可一个人要是连名字都没人叫，才是真的回不来。
```

## 8. 场景四：店猫——不会说话的可靠证人

### 人物目标

店猫是所有坏骰路线的安全网，但不能像万能提示箭头。它只对气味、声音和倒影异常作出一致反应。

### 三次出现

1. **大厅**：拒绝靠近大镜子；倒影慢半拍。
2. **储藏室**：抓门框，暴露地下室旧门钥匙；检定失败时它打翻杯碟，也会让钥匙掉出来，但推进时间。
3. **吧台**：抓第三块松木板，提供日记备用路线；如果日记已取得，它改为把爪子按在日记的水渍上，指向镜面裂缝。

建议短文本：

```text
旁白：猫从你的手边绕开，没有看钥匙，只盯着不锈钢水壶。壶身里的它仍蹲在原地。

旁白：下一秒，倒影里的猫先一步弓起了背。
```

设置 `cat_guided` 与 `cat_mirror`。最佳结局不要求玩家完成一场“和猫对话”的检定，而是奖励认真观察它的行为。

## 9. 场景五：黑衣女人与地下室

### 演出目标

- 玩家先误判她在阻止救人，随后发现她在阻止门吞下更多人。
- 有 `black_woman_named` 时，玩家可称她“陈月”；否则她始终只是“黑衣女人”。
- 知晓老陈历史或猫的倒影线可避免战斗；强行靠近才触发危险遭遇。

### 关键对白

```text
黑衣女人：别叫他老板。

玩家：那该叫他什么？

黑衣女人：名字。镜子喜欢职务，因为职务不会想家。
```

持有银币并知道老陈真相时：

```text
玩家：陈月。

黑衣女人：（很久没有回答）他还在点第二杯吗？

玩家：每一年。

黑衣女人：那就叫他别点了。咖啡放五十年，难喝得很。
```

这句之后她才侧身，让出通向镜子的路，设置 `woman_pass`。

## 10. 场景六：午夜精神冲击与连续推理

### 精神检定失败也前进

```json
[
  {
    "id": "midnight_san_check",
    "type": "check",
    "speaker": "心理学",
    "text": "歌声借用了你最害怕被忘记的那部分声音。你试着分清，哪些念头真正属于自己。",
    "skillId": "psychology",
    "difficulty": 1,
    "successNodeId": "deduction_start",
    "failureNodeId": "midnight_stagger"
  },
  {
    "id": "midnight_stagger",
    "type": "changesan",
    "speaker": "旁白",
    "text": "你几乎答应留在那间永不打烊的店里。小梅碰倒一只杯子，碎裂声把你拉了回来。（理智 -5）",
    "sanDelta": -5,
    "nextNodeId": "deduction_start"
  }
]
```

### 用当前 schema 合成最佳路线

```json
[
  {
    "id": "deduction_start",
    "type": "dialogue",
    "speaker": "调查笔记",
    "text": "镜中的老板已经忘了回来的理由。你必须先证明，现实里仍有人记得他在等什么。",
    "choices": [
      {
        "text": "三只杯子是给仍会回来的人留的位置",
        "nextNodeId": "deduction_diary",
        "requiredFlag": "mei_last_order"
      },
      {
        "text": "线索不足，用现有办法直接面对镜子",
        "nextNodeId": "final_choice"
      }
    ]
  },
  {
    "id": "deduction_diary",
    "type": "dialogue",
    "speaker": "调查笔记",
    "text": "但老板为什么知道今晚会发生什么？",
    "choices": [
      {
        "text": "日记证明他主动进入镜中拖延时间",
        "nextNodeId": "deduction_ritual",
        "requiredItemId": "owner_diary"
      },
      {
        "text": "到此为止，用现有办法面对镜子",
        "nextNodeId": "final_choice"
      }
    ]
  },
  {
    "id": "deduction_ritual",
    "type": "dialogue",
    "speaker": "调查笔记",
    "text": "最后还缺一件事：怎样让镜子分清活人的名字？",
    "choices": [
      {
        "text": "拓片是把名字送回正确一侧的路标",
        "nextNodeId": "set_best_route_ready",
        "requiredFlag": "know_ritual"
      },
      {
        "text": "放弃完整推理，用现有办法面对镜子",
        "nextNodeId": "final_choice"
      }
    ]
  },
  {
    "id": "set_best_route_ready",
    "type": "setflag",
    "flag": "best_route_ready",
    "flagValue": true,
    "nextNodeId": "final_choice"
  }
]
```

## 11. 最终选择

```json
{
  "id": "final_choice",
  "type": "dialogue",
  "speaker": "黑衣女人",
  "text": "“五十年前，没有人叫我的名字。今晚，你们还有机会叫回他的。”镜中的老板抬起头，像一个在深水里听见敲门声的人。",
  "choices": [
    {
      "text": "摆好三只杯子，让所有人叫他回来",
      "nextNodeId": "end_best",
      "requiredFlag": "best_route_ready"
    },
    {
      "text": "用拓片封住镜面，先让活人安全",
      "nextNodeId": "end_good",
      "requiredFlag": "know_ritual"
    },
    {
      "text": "用银币买下这一夜的安静",
      "nextNodeId": "end_neutral",
      "requiredItemId": "silver_coin"
    },
    {
      "text": "挡在其他人前面，直接砸碎镜面",
      "nextNodeId": "combat_cultist"
    }
  ]
}
```

任何玩家至少拥有最后一项，因此不会出现无按钮页面；但战斗失败应回到午夜安全存档，而不是重复播放四十分钟。

## 12. 结局文本初稿

### 灯还亮着（最佳）

```json
{
  "id": "end_best",
  "type": "end",
  "speaker": "结局",
  "endTitle": "灯还亮着",
  "text": "你把三只杯子推回各自的位置。小梅叫出周柏安的名字，老陈把欠了五十年的银币压在杯碟下。你最后说：‘有人在等你。’镜面向内塌陷，老板摔在地上，第一句话是：‘咖啡是不是糊了？’天亮后，咖啡馆仍按原计划关门。所有人在卷帘门落下前吃完最后一顿早餐，猫偷走了半片吐司。你走进刚停雨的街道，觉得今天也许值得过完。"
}
```

### 把门关好（苦甜）

```text
拓片贴上镜面时，歌声像一口终于合拢的箱子。老板的手停在玻璃另一侧，没有再拍。

黑衣女人转向老陈。她的脸只清楚了一瞬，足够让他叫出那个练习了五十年的名字。

清晨，小梅把招牌收进仓库，又塞给你一杯咖啡。

“带走吧。”她说，“别空着手回去。”
```

### 无声收束（中立）

```text
银币在杯碟下轻响一声。镜面恢复成普通玻璃，日记里的字迹却一行行褪去。

老板没有回来。没有人再记得歌声的旋律，只记得这一夜很冷。

三天后，小梅发来一张白天的咖啡馆照片：

“我们决定先把这个月开完。猫不同意关门。”
```

### 失败页

标题可保留“永恒顾客”，但按钮必须是：

- 从午夜前重试；
- 返回主菜单。

失败页不把玩家数十分钟的选择清空，也不将精神冲击写成羞辱。

## 13. 文本验收标准

- 每个对话节点尽量为 25–90 个中文字符；长信息拆成有节奏的两到三步。
- 选项写玩家的**意图与行动**，不直接标“好人选项 / 坏人选项”。
- 每名主要 NPC 至少有一段与案件无关的未来生活对白。
- 三条人物线在最终十分钟内都有一次明确回响。
- 每个关键事实至少有两个来源；完整真相只奖励认真调查，不阻止普通通关。
- 所有非 `end` 节点至少一个合法出口。
- 当前选项条件不满足时，UI 最终应显示禁用原因；在该功能完成前，剧本不得依赖玩家猜隐藏条件。
- 完成后由剧情验证器检查引用，再实走最佳、苦甜、中立、战斗失败重试四条路径。
