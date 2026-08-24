# Scenario_01 节点流 ·《咖啡馆关店后的失踪》

> H8 调查闭环基线；SP-2B-1 已补入雨夜开场与三条人物线入口。节点源文件：`Assets/Resources/Data/Scenarios/Scenario_01/nodes.json`。

## 完整节点流

```mermaid
flowchart TD
    intro_01["intro_01<br/>匿名邮件与午夜之约"]:::entry --> intro_motive{"intro_motive<br/>为何继续往前？"}:::choice
    intro_motive -->|完成委托| set_motive_duty["set_motive_duty<br/>旗标：motive_duty"]:::flag
    intro_motive -->|一段记忆| set_motive_memory["set_motive_memory<br/>旗标：motive_memory"]:::flag
    intro_motive -->|不忽略求救| set_motive_kindness["set_motive_kindness<br/>旗标：motive_kindness"]:::flag
    set_motive_duty --> intro_02["intro_02<br/>三只杯子与最后一盏灯"]
    set_motive_memory --> intro_02
    set_motive_kindness --> intro_02
    intro_02 --> intro_cat_mirror["intro_cat_mirror<br/>店猫避开镜面"]
    intro_cat_mirror --> set_cat_mirror_intro["set_cat_mirror_intro<br/>旗标：cat_mirror"]:::flag
    set_cat_mirror_intro --> intro_03{"intro_03<br/>小梅拦下访客"}:::choice
    intro_03 -->|出示邮件| intro_email_shown["intro_email_shown<br/>确认老板失踪"]
    intro_03 -->|追问第三只杯子| intro_third_cup["intro_third_cup<br/>昨晚少了一个人"]
    intro_03 -->|观察大厅| check_spot_main{"check_spot_main<br/>侦查检定"}:::check
    intro_03 -->|先自己看看| intro_hall_threads{"intro_hall_threads<br/>三条人物线入口"}:::choice
    intro_email_shown --> intro_hall_threads
    intro_third_cup --> intro_hall_threads

    check_spot_main -->|成功| spot_success["spot_success<br/>获得：银币"]:::item
    check_spot_main -->|失败| spot_fail["spot_fail<br/>暂时看不清异常"]
    spot_success --> set_found_coin["set_found_coin<br/>旗标：found_coin"]:::flag
    set_found_coin --> intro_hall_threads
    spot_fail -->|仍可询问三位证人| intro_hall_threads

    intro_hall_threads -->|小梅的自责| npc_mei_talk
    intro_hall_threads -->|老陈的等待| npc_chen_talk
    intro_hall_threads -->|店猫与倒影| npc_cat_talk
    intro_hall_threads -->|自由调查| hub_explore

    hub_explore{{"hub_explore<br/>自由调查中心"}}:::hub
    hub_explore -->|检查储藏室| goto_storage["goto_storage<br/>进入储藏室"]
    goto_storage --> storage_check{"storage_check<br/>聆听检定"}:::check
    storage_check -->|成功| storage_key["storage_key<br/>获得：生锈的钥匙"]:::item
    storage_check -->|失败：可重试或换路线| hub_explore
    storage_key --> hub_explore

    hub_explore -->|需生锈的钥匙| goto_basement_check["goto_basement_check<br/>SAN -2"]
    goto_basement_check --> basement_enter["basement_enter<br/>进入地下室"]
    basement_enter --> basement_choice{"basement_choice<br/>地下室行动"}:::choice
    basement_choice -->|调查墙面| check_occult_wall{"check_occult_wall<br/>神秘学检定"}:::check
    check_occult_wall -->|成功| give_symbol["give_symbol<br/>获得：符号拓片"]:::item
    give_symbol --> set_know_ritual["set_know_ritual<br/>旗标：know_ritual"]:::flag
    set_know_ritual --> hub_explore
    check_occult_wall -->|失败| san_loss_fail["san_loss_fail<br/>SAN -5"]
    san_loss_fail -->|回到调查，可重试或换路线| hub_explore
    basement_choice -->|强行前进| combat_shadow_rat[["combat_shadow_rat<br/>战斗：影鼠"]]:::combat
    combat_shadow_rat -->|胜利| after_rat_win["after_rat_win"]
    after_rat_win --> hub_explore
    combat_shadow_rat -->|逃跑| hub_explore
    combat_shadow_rat -->|失败| end_bad

    hub_explore -. 大厅 NPC .-> npc_mei_talk["npc_mei_talk<br/>店员小梅"]
    npc_mei_talk -->|安抚| check_psych_mei{"check_psych_mei<br/>心理学检定"}:::check
    npc_mei_talk -->|返回| hub_explore
    check_psych_mei -->|成功| mei_reveal["mei_reveal<br/>旗标：mei_trust<br/>关系：小梅 ↔ 老陈"]:::flag
    check_psych_mei -->|失败：可重试或换路线| hub_explore
    mei_reveal --> hub_explore

    hub_explore -. 大厅 NPC .-> npc_chen_talk["npc_chen_talk<br/>常客老陈"]
    npc_chen_talk --> chen_library{"chen_library<br/>图书馆使用检定"}:::check
    chen_library -->|成功| give_diary_hint["give_diary_hint<br/>日记藏匿线索"]
    chen_library -->|失败：可重试或换路线| hub_explore
    give_diary_hint --> bar_diary_search{"bar_diary_search<br/>侦查检定"}:::check
    bar_diary_search -->|成功| give_diary["give_diary<br/>获得：老板日记"]:::item
    bar_diary_search -->|失败：可重试或换路线| hub_explore
    give_diary --> hub_explore
    hub_explore -->|需老板日记| read_diary["read_diary<br/>SAN -3"]
    read_diary --> hub_explore

    hub_explore -. 大厅 NPC .-> npc_cat_talk["npc_cat_talk<br/>店猫避开镜面并引路"]
    npc_cat_talk --> set_cat_guided["set_cat_guided<br/>旗标：cat_guided"]:::flag
    set_cat_guided --> hub_explore
    basement_choice -->|交谈| npc_woman_talk["npc_woman_talk<br/>黑衣女人"]
    npc_woman_talk -->|说服| check_persuade_woman{"check_persuade_woman<br/>说服检定"}:::check
    npc_woman_talk -->|撤退| hub_explore
    check_persuade_woman -->|成功| woman_pass["woman_pass<br/>旗标：woman_pass"]:::flag
    check_persuade_woman -->|失败：转入可胜/可逃战斗| combat_cultist
    woman_pass --> midnight_event
    hub_explore -. 地下室 NPC .-> npc_owner_talk["npc_owner_talk<br/>老板的影子"]
    npc_owner_talk --> hub_explore

    hub_explore -->|需 found_coin| midnight_event["midnight_event<br/>午夜钟声"]
    midnight_event --> midnight_san_check{"midnight_san_check<br/>理智替代检定"}:::check
    midnight_san_check -->|成功| final_choice{"final_choice<br/>终局选择"}:::choice
    midnight_san_check -->|失败但保留选择权| midnight_failure_choice{"midnight_failure_choice<br/>抵抗或回应歌声"}:::choice
    midnight_failure_choice -->|承受 SAN -5| midnight_failure_cost["midnight_failure_cost<br/>恢复清醒"]
    midnight_failure_cost --> final_choice
    midnight_failure_choice -->|主动回应| end_madness

    final_choice -->|需 know_ritual| end_good(["end_good<br/>生还者"]):::ending
    final_choice -->|需 found_coin| end_neutral(["end_neutral<br/>无声收束"]):::ending
    final_choice -->|鲁莽射击| combat_cultist[["combat_cultist<br/>战斗：狂热侍从"]]:::combat
    combat_cultist -->|胜利| after_cult_win["after_cult_win"]
    after_cult_win --> final_choice
    combat_cultist -->|逃跑| hub_explore
    combat_cultist -->|失败| end_bad(["end_bad<br/>失踪"]):::ending
    end_madness(["end_madness<br/>永恒顾客"]):::ending

    classDef entry fill:#27405c,stroke:#8fc7ff,color:#fff;
    classDef hub fill:#44345c,stroke:#ceb1ff,color:#fff;
    classDef choice fill:#3d4655,stroke:#d7dce5,color:#fff;
    classDef check fill:#5b4828,stroke:#ffd27d,color:#fff;
    classDef item fill:#28513c,stroke:#8de0ad,color:#fff;
    classDef flag fill:#2d4f57,stroke:#8edbe5,color:#fff;
    classDef combat fill:#5c2d35,stroke:#ff9ba7,color:#fff;
    classDef ending fill:#49305c,stroke:#e6b3ff,color:#fff;
```

## 当前可试玩闭环

主闭环已经形成两种明确路径：

1. `匿名邮件 → 选择来访动机 → 三只杯子与异常倒影 → 大厅三线入口 → 侦查成功 → 获得银币 → 解锁午夜钟声 → 终局 → 中立结局`。
2. `调查储藏室 → 获得钥匙 → 进入地下室 → 读懂符号 → 解锁仪式选项 → 终局 → 好结局`。

NPC 和日记分支负责补充氛围、关系与风险提示；狂热侍从战斗是缺少关键线索时仍可尝试的高风险出口。

## 失败回路约定

- 大厅首次侦查失败会回到 `intro_hall_threads`，仍可改问小梅、老陈或跟随店猫；聆听、神秘学、心理学和图书馆检定失败后会回到 `hub_explore`，玩家可以换路线或稍后重试。
- 说服黑衣女人失败会进入可胜利、可逃跑的战斗，不会直接锁死剧情。
- 午夜检定失败不再由一次骰子直接判定疯狂结局；玩家可付出 `SAN -5` 继续终局，也可主动回应歌声进入 `end_madness`。
- 影鼠与狂热侍从战斗都保留胜、负、逃出口。

自动测试 `InvestigationLoopTests.Scenario01_AllNonEndNodesHaveKnownExit_AndFourEndingsRemainReachable` 会检查所有非 End 节点至少有一个已存在的出口，并从 `startNodeId` 遍历确认四个结局都仍可抵达。
