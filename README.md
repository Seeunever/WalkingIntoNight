# AnimalCafe

2D Unity（团结引擎）项目：动物咖啡馆主题，当前包含基础对话系统原型。

## 环境

- 团结引擎 / Unity **2022.3.62t2**（见 `ProjectSettings/ProjectVersion.txt`）
- URP 2D 模板

## 打开项目

1. 克隆本仓库
2. 用对应版本的编辑器打开项目根目录
3. 首次打开会生成 `Library/`（已在 `.gitignore` 中忽略）

## 主要目录

| 路径 | 说明 |
|------|------|
| `Assets/Scripts/` | 对话相关 C#（`DialogueManager`、`DialogueLine`、`DialogueTest`） |
| `Assets/Scenes/` | 场景（`SampleScene.scene`） |
| `Assets/Images/` | 图片资源 |

## 对话系统（简要）

- `DialogueLine`：单条对话数据（角色名、文本、可选立绘）
- `DialogueManager`：队列播放，需绑定 TMP 文本、按钮、立绘 Image
- `DialogueTest`：启动时注入测试对话

场景内需自行搭建 Canvas/UI 并挂载上述组件后才能在 Play 时看到效果。

## GitHub 仓库

- 远程地址：https://github.com/Seeunever/AnimalCafe
- 默认分支：`main`

首次在本机推送（若尚未上传）：

```bash
cd E:\AnimalCafe
git push -u origin main
```

在另一台电脑克隆：

```bash
git clone https://github.com/Seeunever/AnimalCafe.git
```

## 两地开发

```bash
git pull    # 开始工作前
# ... 编辑并保存场景 ...
git add -A
git commit -m "描述你的修改"
git push    # 离开前
```

请勿提交 `Library/`、`Logs/`、`UserSettings/`（已由 `.gitignore` 排除）。
