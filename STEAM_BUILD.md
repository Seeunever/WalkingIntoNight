# Steam 构建说明

## 已锁定的产品身份

- Company：`Seeunever`
- 英文产品名：`Walking Into Night`
- 中文显示名：`走入夜境`
- 当前内部版本：`0.1.0-alpha.1`
- Standalone Identifier：`com.seeunever.walkingintonight`

产品版本的代码事实源是 `ProductIdentity.ProductVersion`，Unity `PlayerSettings.bundleVersion` 必须与它一致；构建前守门和自动测试会阻止二者漂移。Git 发布标签在版本前加 `v`，例如 `v0.1.0-alpha.1`。存档里的整数 `version` 是独立的存档格式版本，不能随产品版本一起修改。

Windows 本地存档的新目录为 `%USERPROFILE%\AppData\LocalLow\Seeunever\Walking Into Night`。首次启动会检查旧目录 `%USERPROFILE%\AppData\LocalLow\DefaultCompany\WalkingIntoNight` 的槽位 1：只迁移通过完整校验的存档，旧文件不删除；存在有效新档时优先保护新档，需要替换损坏或较旧新档时先创建 `.before_identity_migration.bak` 回退副本。完成标记为 `.identity_migration_defaultcompany_v1.done`，防止旧档以后再次覆盖或复活。

## 前置

1. 在 [Steamworks 伙伴后台](https://partner.steamgames.com/) 创建应用，获取 **App ID**。
2. 将 [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) 导入 `Assets/Plugins`（或 UPM/git URL）。
3. **Edit → Project Settings → Player → Scripting Define Symbols** 添加：`STEAMWORKS`
4. 修改 [Assets/Scripts/Steam/SteamBootstrap.cs](Assets/Scripts/Steam/SteamBootstrap.cs) 中的 `steamAppId` 为你的 AppID。
5. 开发阶段可在构建输出目录放置 `steam_appid.txt`（内容一行 AppID），并从 Steam 客户端启动测试。

## 构建 Win64

1. **File → Build Settings**
2. Platform: **Windows**, Architecture: **x86_64**
3. Scenes（顺序）：
   - `Assets/Scenes/MainMenu.scene`
   - `Assets/Scenes/CharacterCreate.scene`
   - `Assets/Scenes/Gameplay.scene`
4. Build；在 Steam 后台上传 Depot（SteamPipe / SDK tools）。

也可以使用项目内的开发构建入口：

- 菜单：`WalkingIntoNight/Build/Windows Development`
- 命令行执行方法：`WalkingIntoNight.TRPG.Editor.WindowsDevelopmentBuild.Build`
- 用环境变量 `WALKING_INTO_NIGHT_BUILD_EXE` 指定项目外的 exe 绝对路径；未指定时输出到 `Builds/WindowsDevelopment/Walking Into Night.exe`。
- 所有 Editor 构建在开始前都会校验 Company、Product、Version 和 Standalone Identifier；任一字段漂移就拒绝出包。项目内开发构建还会把版本与三项身份写入 `BUILD_INFO.txt`。

开发版支持内部冒烟参数 `-winSmokeTest`。它会自动验证新游戏、剧情检定、物品、保存/重载和一个结局，并在日志写入 `WALKING_INTO_NIGHT_BUILD_SMOKE_PASS` 后退出。使用时必须把 `WALKING_INTO_NIGHT_SMOKE_SAVE_ROOT` 设为专用绝对路径（建议放在项目外），并预先在该目录创建 `.walking_into_night_gameplay_smoke` 专用哨兵；程序会拒绝任何位于真实 `LocalLow` 内的目录，防止误删玩家槽位。不要把该参数用于正式玩家启动方式。

身份迁移另有内部参数 `-winIdentityMigrationSmokeTest`，只接受带 `.walking_into_night_identity_smoke` 哨兵的专用绝对路径（建议放在项目外），并拒绝真实 `LocalLow` 内的目录。环境变量 `WALKING_INTO_NIGHT_IDENTITY_SMOKE_ROOT` 指定沙盒，`WALKING_INTO_NIGHT_IDENTITY_SMOKE_PHASE` 依次使用 `first`、`second`，用于验证首次迁移、主菜单继续按钮和二次启动幂等性。该流程不会读写玩家真实存档。

团结引擎 2022.3.62t2 的本次 Win64 实际构建中，`Application.identifier` 运行时返回空值；本地 API 文档只说明了 Apple、Android 和 UWP 的运行时含义，并未承诺普通 Windows Standalone 的值。因此 Win64 Identifier 由构建前 Editor API 和自动测试锁定，运行时另外核对 Company、Product 与实际 `persistentDataPath`。

命令行运行 EditMode / PlayMode 测试时，必须把 `WALKING_INTO_NIGHT_TEST_SAVE_ROOT` 指向带 `.walking_into_night_editor_tests` 哨兵的专用绝对路径；缺失或落在真实 `LocalLow` 内都会失败关闭。存档单元测试自身使用代码注入的临时目录。

## 首版可不做

- 成就、排行榜、云存档（使用本地 JSON 存档即可）

## 团结引擎注意

若 Steam 构建异常，请用同版本 **Unity 2022.3 LTS** 打开项目对比测试。
