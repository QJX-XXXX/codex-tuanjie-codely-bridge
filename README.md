# Codex + Tuanjie + Codely Bridge

这个仓库提供一套可复用的团结 Editor 工作流：Codex 通过 CodelyCLI 的 MCP stdio 入口，经 Codely Bridge 连接当前团结项目。它包含 PowerShell 配置脚本、EditorWindow UPM 包、Codex Skill、Agent 提示和独立测试工程。

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

## 快速设置

### PowerShell

在仓库根目录执行：

    .\scripts\setup-project.ps1 -ProjectPath "D:\TuanjieProjects\YourGame" -CodelyCliPath "C:\Tools\CodelyCLI\codely.cmd" -Force

脚本会识别团结项目、只更新 [mcp_servers.tuanjie] table，并在覆盖已有配置前生成 config.toml.bak。首次创建不需要 Force。

### EditorWindow

将 editor-package 作为本地 UPM 包或 Git 包加入团结项目，然后打开 Window/Tuanjie Codex Setup。窗口只读显示 Editor、Bridge、descriptor、CLI、项目 config 和全局 Skill 状态；预览不会写文件，生成/更新前会显示精确目标和备份行为。

窗口不会自动安装 Bridge，也不会自动运行 codely serve unity-mcp。

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
- [团结规则片段](templates/AGENTS.tuanjie-snippet.md)

## 验证

PowerShell 配置测试：

    Invoke-Pester -Script .\tests\scripts\CodexTuanjieConfig.Tests.ps1 -PassThru

文档测试：

    Invoke-Pester -Script .\tests\docs\RepositoryDocs.Tests.ps1 -PassThru

Skill 结构检查：

    python -X utf8 $env:USERPROFILE\.codex\skills\.system\skill-creator\scripts\quick_validate.py .\skills\tuanjie-codely-bridge

独立团结测试工程位于 tests/TuanjieTestProject；如果当前已有团结 Editor 单实例占用，批处理测试可能只启动导入进程而不产生结果 XML，此时应保留日志并明确标注 Editor 验证未完成。

## 许可证

MIT，见 [LICENSE](LICENSE)。
