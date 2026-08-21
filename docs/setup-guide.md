# 安装与设置

本流程用于让 Codex 直接连接团结 Editor，不要求使用 TuanjieAI。它只覆盖团结 Editor + Codely Bridge；Unity 官方 Editor 项目请使用对应的 Unity MCP 工作流。

## 1. 准备团结项目

确认项目根包含 Assets、Packages 和 ProjectSettings，ProjectVersion.txt 同时有团结版本字段，并在 Package Manager 安装与 Editor 版本匹配的 cn.tuanjie.codely.bridge；按[官方 Codely Bridge 安装指南](https://codely-docs.tuanjie.cn/en/using-codely/codely-bridge-installation-guide/)打开 Bridge 自带的状态窗口，确认状态为 `Connected/Ready`。

Unity 官方 Editor 项目不要使用本仓库的 Codely Bridge Skill 或 tuanjie MCP。

## 2. 准备 CodelyCLI

先安装 Node.js LTS（自带 npm），在 PowerShell 中运行以下命令安装 CodelyCLI：

    npm install -g @unity-china/codely-cli

安装完成后，用下面的命令找到 codely.cmd 的绝对路径并验证版本：

    $cli = (Get-Command codely.cmd -ErrorAction Stop).Source
    $cli
    & $cli --version

记录 `$cli` 输出的绝对路径，供 EditorWindow 或 PowerShell 配置入口使用。也可以参考 [Codely CLI 安装说明](https://codely-docs.tuanjie.cn/learn/ai-programming-environment-setup-guide/)。

然后按 CodelyCLI 实际帮助确认 serve unity-mcp --stdio 支持的参数。项目路径必须使用目标团结项目的规范化绝对路径。

## 3. 首次设置：EditorWindow（推荐）

在团结 Package Manager 中选择 Add package from git URL，使用：

    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package

也可以将 editor-package 作为本地包引用。打开 Window/Tuanjie Codex Setup，然后：

1. 点击刷新状态，确认 Editor、Bridge、CodelyCLI 和 MCP 项目根路径。
2. 点击预览配置，检查将要写入的 .codex/config.toml。
3. 点击生成/更新项目配置，确认目标路径和备份行为后写入。

首次手动设置不需要再运行 PowerShell；EditorWindow 已经完成同一项项目配置工作。

## 4. 批量/脚本化/CI：PowerShell

当需要批量处理多个团结项目、脚本化或 CI 时，使用 PowerShell 入口：

    .\scripts\setup-project.ps1 -ProjectPath "D:\TuanjieProjects\YourGame" -CodelyCliPath "C:\Tools\CodelyCLI\codely.cmd"

如果 .codex/config.toml 已存在且需要更新，增加 -Force。脚本会写入 config.toml.bak；不加 Force 时不会覆盖已有差异配置。EditorWindow 和 PowerShell 二选一，不需要连续执行。

也可以手动复制 [config.toml.example](../templates/config.toml.example)，修改两处绝对路径。不要把 token、端口、descriptor 或真实用户凭据提交到仓库。

## 5. 安装全局 Skill

将 skills/tuanjie-codely-bridge 复制到用户级 Codex Skill 目录（通常为 ~/.codex/skills/tuanjie-codely-bridge），然后新开 Codex 对话。若使用其他 Skill 根目录，遵循当前 Codex 的 Skill 安装规则。

## 6. 让 Agent 自动完成本地连接配置

安装 Skill 后，可以直接使用 [Agent 自动设置指南](agent-setup-guide.md) 中的提示，让 Agent 在当前项目根完成只针对该项目的 .codex/config.toml 配置和验证。Agent 不会把配置静态写入所有项目，也不会在 Bridge 缺失时擅自修改 manifest 或安装包。

## 7. 连接 Codex

在项目根打开并信任 Codex。使用 codex mcp list 确认项目配置中的 tuanjie server 已加载；首次对象操作先发送 [只读连接检查](../prompts/readonly-connection-check.md)，确认根路径一致后再发送 [写入冒烟测试](../prompts/write-smoke-test.md)。

## 8. 多项目使用

Skill 可以全局复用；config.toml 不应静态指向一个项目后再假装适用于所有项目。为每个团结项目写入自己的 .codex/config.toml，或在切换项目时显式生成/审核该项目配置。
