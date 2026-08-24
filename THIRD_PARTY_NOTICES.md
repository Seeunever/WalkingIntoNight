# 第三方许可与素材来源记录

本文件用于 Windows/Steam 发布前的素材审计。它记录仓库中当前可确认的第三方组件和仍需保留的来源信息，不构成法律意见。

仓库当前没有顶层 `LICENSE`；本记录不会替项目源码授予开源许可。开始接收外部贡献或准备公开发行前，应由项目所有者另行选择代码许可或明确保留权利。

## 字体

### Noto Sans SC

- 文件：`Assets/Resources/Fonts/NotoSansSC-VF.ttf`
- 许可：SIL Open Font License 1.1
- 随附许可全文：`Assets/Resources/Fonts/NotoSansSC-LICENSE.txt`
- 发布要求：构建或发行包中应保留可读取的 OFL 文本；不得单独出售字体文件。

### Liberation Sans

- 用途：TextMesh Pro 随附默认/回退字体资源。
- 版权记录：Google Corporation（2010）与 Red Hat, Inc.（2012），详见随附文件。
- 许可：SIL Open Font License 1.1
- 随附许可全文：`Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`

## AI 辅助生成的项目美术

以下 PNG 是为本项目制作的 AI 辅助美术，不是从素材站下载的第三方成品。文件内的 C2PA 内容凭证标识 `OpenAI Media Service`、`gpt-image` 与 trained algorithmic media：

- `Assets/Resources/Art/Characters/mei_barista_v1.png`：小梅，生成日期 2026-08-21；
- `Assets/Resources/Art/Characters/chen_regular_v2.png`：老陈，生成日期 2026-08-21；
- `Assets/Resources/Art/Characters/shop_cat_v1.png`：店猫，生成日期 2026-08-21；
- `Assets/Resources/Art/Backgrounds/cafe_main_v1.png`：咖啡馆大厅，生成日期 2026-08-21；
- `tools/style_reference.png`：开发用风格参考，生成日期 2026-06-07，不进入游戏构建。

2026-08-21 的既有图片没有在仓库中保存完整提示词。Steam 发布前仍应把本文件、C2PA 凭证、生成服务条款和可用的账户 / 任务记录一起归档。不能确认来源的新图片不得直接加入发行包。

## 不进入发行构建的旧占位图

较早 Git 历史中的 `Assets/Images/Character/` 曾包含 bear / fox / rabbit / swan 开发占位图，画面带“通义万相”水印，无法从仓库确认生成账户、日期和适用条款。它们已从 `v0.1.0-alpha.1` 当前树与运行时资源中移除；旧提交历史只用于必要时恢复和审计，不应作为发行授权证据。

## 引擎与包

- 项目使用团结引擎 / Unity 2022.3.62t2 及 Package Manager 依赖；其许可分别由引擎安装与各 package 自带文件管理。
- TextMesh Pro 的未使用文档与 EmojiOne 示例资源未纳入仓库；当前 TMP 默认 Sprite Asset 为空，游戏不分发 EmojiOne 图集。
- `tools/generate_portraits_ai.py` 是未接入运行时的可选开发脚本，不捆绑模型权重。脚本默认指向的第三方模型及任何生成输出，在用于发行前必须另行核对模型许可、生成服务条款与输出来源。
- Steamworks.NET 当前尚未导入，`STEAMWORKS` 也未启用，因此本仓库目前没有随发行包分发 Steamworks.NET 二进制。

## 发布前检查

- [ ] Noto Sans SC 与 Liberation Sans 的 OFL 文本随发行归档保留。
- [ ] 新增美术逐张记录来源、作者/生成工具、日期和适用条款。
- [ ] 新增音效、音乐、商店图、图标和宣传视频分别记录许可。
- [ ] 导入 Steamworks.NET 后补记版本、来源和 MIT 许可文件。
