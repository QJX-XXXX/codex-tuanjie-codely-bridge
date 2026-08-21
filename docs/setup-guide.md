# 安装与设置

## 1. 准备团结项目

确认项目根包含 Assets、Packages 和 ProjectSettings，ProjectVersion.txt 同时有团结版本字段，并在 Package Manager 安装 cn.tuanjie.codely.bridge。打开该项目的团结 Editor，确认 Bridge 已连接。

Unity 官方 Editor 项目不要使用本仓库的 Codely Bridge Skill 或 tuanjie MCP。

## 2. 准备 CodelyCLI

安装 CodelyCLI 后记下 codely.cmd 的绝对路径。先在 PowerShell 中运行：

    & "C:\Tools\CodelyCLI\codely.cmd" --version

然后按 CodelyCLI 实际帮助确认 serve unity-mcp --stdio 支持的参数。项目路径必须使用目标团结项目的规范化绝对路径。

## 3. 生成项目配置

推荐使用仓库脚本：

    .\scripts\setup-project.ps1 -ProjectPath "D:\TuanjieProjects\YourGame" -CodelyCliPath "C:\Tools\CodelyCLI\codely.cmd"

如果 .codex/config.toml 已存在且需要更新，增加 -Force。脚本会写入 config.toml.bak；不加 Force 时不会覆盖已有差异配置。

也可以手动复制 [config.toml.example](../templates/config.toml.example)，修改两处绝对路径。不要把 token、端口、descriptor 或真实用户凭据提交到仓库。

## 4. 安装 EditorWindow

在团结 Package Manager 中选择 Add package from git URL，使用：

    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package

也可以将 editor-package 作为本地包引用。打开 Window/Tuanjie Codex Setup，先点击刷新状态和预览配置；确认目标路径后再点击生成/更新项目配置。

## 5. 安装全局 Skill

将 skills/tuanjie-codely-bridge 复制到用户级 Codex Skill 目录（通常为 ~/.codex/skills/tuanjie-codely-bridge），然后新开 Codex 对话。若使用其他 Skill 根目录，遵循当前 Codex 的 Skill 安装规则。

## 6. 连接 Codex

在项目根打开并信任 Codex。使用 codex mcp list 确认项目配置中的 tuanjie server 已加载；首次对象操作先发送 [只读连接检查](../prompts/readonly-connection-check.md)，确认根路径一致后再发送 [写入冒烟测试](../prompts/write-smoke-test.md)。

## 7. 多项目使用

Skill 可以全局复用；config.toml 不应静态指向一个项目后再假装适用于所有项目。为每个团结项目写入自己的 .codex/config.toml，或在切换项目时显式生成/审核该项目配置。
