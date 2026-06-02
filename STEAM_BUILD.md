# Steam 构建说明

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

## 首版可不做

- 成就、排行榜、云存档（使用本地 JSON 存档即可）

## 团结引擎注意

若 Steam 构建异常，请用同版本 **Unity 2022.3 LTS** 打开项目对比测试。
