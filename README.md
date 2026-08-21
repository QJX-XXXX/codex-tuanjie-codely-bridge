# Codex + Tuanjie + Codely Bridge

这个仓库提供一套可复用的团结 Editor 工作流：Codex 通过 CodelyCLI 的 MCP stdio 入口，经 Codely Bridge 连接当前团结项目。它包含 PowerShell 配置脚本、EditorWindow UPM 包、Codex Skill 和 Agent 提示。

## 适用边界

- 目标是团结 Editor 项目，不是 Unity 官方 Editor 项目。
- Skill 可以全局安装并复用，但 MCP 的项目路径不能静态地“一次配置覆盖所有项目”。
- 当前版本只声明 Windows + PowerShell 验证，不宣称 macOS/Linux 兼容。
- 不自动安装 Node.js、Codex、CodelyCLI 或 Codely Bridge，也不自动启动 MCP 服务。

## 链路

Codex → CodelyCLI MCP/stdio → Codely Bridge → Tuanjie Editor

详见 [架构说明](docs/architecture.md)、[安装与设置](docs/setup-guide.md) 和 [排错指南](docs/troubleshooting.md)。

## 前置条件

1. 安装与项目版本匹配的团结 Editor。
2. 在团结 Package Manager 安装 Codely Bridge，并确认项目能连接。
3. 安装 CodelyCLI，并知道 codely.cmd 的绝对路径。
4. 在 Codex 中打开并信任当前项目目录。

## 快速设置（首次使用）

### EditorWindow（推荐）

首次在一个团结项目中设置时，直接使用 EditorWindow，不需要先运行 PowerShell：

1. 将 editor-package 作为本地 UPM 包或 Git 包加入团结项目。
2. 打开 Window/Tuanjie Codex Setup。
3. 点击刷新状态，确认团结 Editor、Bridge、CodelyCLI 和项目根路径均正确。
4. 点击预览配置，确认目标 .codex/config.toml 内容。
5. 点击生成/更新项目配置，按确认对话完成写入。

窗口只读显示 Editor、Bridge、descriptor、CLI、项目 config 和全局 Skill 状态；预览不会写文件。窗口不会自动安装 Bridge，也不会自动运行 codely serve unity-mcp。

### PowerShell（批量/脚本化/CI）

只有在需要批量配置、脚本化或 CI 时，才使用 PowerShell 入口：

    .\scripts\setup-project.ps1 -ProjectPath "D:\TuanjieProjects\YourGame" -CodelyCliPath "C:\Tools\CodelyCLI\codely.cmd" -Force

脚本会识别团结项目、只更新 mcp_servers.tuanjie table，并在覆盖已有配置前生成 config.toml.bak。首次创建不需要 Force。EditorWindow 和 PowerShell 作用于同一个项目配置，首次设置时二选一，不需要连续执行。

## config.toml 模板

将项目路径和 CodelyCLI 路径替换为实际绝对路径。Windows TOML 字符串中的反斜杠需要写成两个反斜杠。

    [mcp_servers.tuanjie]
    command = "cmd.exe"
    args = [
        "/c",
        "C:\\Tools\\CodelyCLI\\codely.cmd",
        "serve",
        "unity-mcp",
        "--stdio",
        "--unity-project-path",
        "D:\\TuanjieProjects\\YourGame"
    ]
    startup_timeout_sec = 30
    tool_timeout_sec = 120
    enabled = true

完整模板位于 [templates/config.toml.example](templates/config.toml.example)。Codex 可以读取用户级 ~/.codex/config.toml，但该静态 MCP 参数仍然只能指向一个项目；要覆盖多个团结项目，应让每个项目拥有自己的 .codex/config.toml。全局 Skill 则可以复用，不包含某个项目的固定路径。

## Skill

将 [skills/tuanjie-codely-bridge](skills/tuanjie-codely-bridge) 安装到用户级 Codex Skill 目录（通常是 ~/.codex/skills/），然后在新对话中使用 $tuanjie-codely-bridge。Skill 会先核对团结 Editor、Bridge、MCP 根路径和实际 schema，再执行最小动作并重读验证；Unity 官方 Editor 不使用它。

## Agent 提示

- [只读连接检查](prompts/readonly-connection-check.md)
- [写入冒烟测试](prompts/write-smoke-test.md)
- [连接诊断](prompts/diagnose-connection.md)
- [Agent 自动设置本地连接](docs/agent-setup-guide.md)
- [团结规则片段](templates/AGENTS.tuanjie-snippet.md)

## 验证

完成安装后，在当前团结 Editor 中打开 Window/Tuanjie Codex Setup，点击刷新状态、预览配置并确认项目根路径；然后使用 codex mcp list 检查 tuanjie server 是否加载。

Skill 结构检查：

    python -X utf8 $env:USERPROFILE\.codex\skills\.system\skill-creator\scripts\quick_validate.py .\skills\tuanjie-codely-bridge

PowerShell 和 EditorWindow 都会在写入前检查项目身份、Bridge、CodelyCLI 和配置目标；写入后应重新读取配置并按需检查 MCP 状态。

## 许可证

MIT，见 [LICENSE](LICENSE)。
