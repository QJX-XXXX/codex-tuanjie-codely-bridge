# Codex + Tuanjie + Codely Bridge

这个仓库的目的，是让选择 Codex 的团队可以不依赖 TuanjieAI，直接通过 CodelyCLI 的 MCP stdio 入口和 Codely Bridge 连接团结 Editor。它提供一套可复用的团结 Editor 工作流，包含 PowerShell 配置脚本、EditorWindow UPM 包、Codex Skill 和 Agent 提示。

## 项目定位

这是“Codex + 团结 Editor”的本地连接方案，作为 TuanjieAI 之外的工作路径；它不替换团结 Editor 或 Codely Bridge，也不适用于 Unity 官方 Editor 项目。

## 适用边界

- 目标是团结 Editor 项目，不是 Unity 官方 Editor 项目。
- Skill 可以全局安装并复用，但 MCP 的项目路径不能静态地“一次配置覆盖所有项目”。
- 当前版本只声明 Windows + PowerShell 验证，不宣称 macOS/Linux 兼容。
- 不自动安装 Node.js、Codex、CodelyCLI 或 Codely Bridge；设置阶段不启动 CodelyCLI MCP 服务，运行时由 Codex 按项目配置按需拉起，Bridge 则随团结 Editor 自动加载。

## 链路

Codex → CodelyCLI MCP/stdio → Codely Bridge → Tuanjie Editor

详见 [架构说明](docs/architecture.md)、[安装与设置](docs/setup-guide.md) 和 [排错指南](docs/troubleshooting.md)。

## 运行方式

安装并配置完成后，打开团结项目即可让 Codely Bridge 随 Editor 自动加载和初始化；Codex 首次调用 `tuanjie` MCP 时，会按项目 `.codex/config.toml` 自动启动 CodelyCLI 的 stdio 服务，不需要每次点击连接，也不需要手动运行长期驻留的 MCP 服务。

## 前置条件

1. 安装与项目版本匹配的团结 Editor。
2. 按[官方 Codely Bridge 安装流程](https://codely-docs.tuanjie.cn/en/using-codely/codely-bridge-installation-guide/)安装与 Editor 版本匹配的 Codely Bridge，并打开目标团结项目。
3. 安装 Node.js LTS、CodelyCLI，并准备好 `codely.cmd` 的绝对路径。
4. 安装 Codex，在其中打开并信任当前项目目录。

完整安装、Agent 主导接入、EditorWindow、PowerShell、`config.toml` 和验证步骤统一见[安装与设置](docs/setup-guide.md)，不要把下面的入口连续执行一遍。

## Agent 提示

- [只读连接检查](prompts/readonly-connection-check.md)
- [写入冒烟测试](prompts/write-smoke-test.md)
- [连接诊断](prompts/diagnose-connection.md)
- [团结规则片段](templates/AGENTS.tuanjie-snippet.md)

## 许可证

MIT，见 [LICENSE](LICENSE)。
