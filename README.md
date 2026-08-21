# Tuanjie + Codely Bridge Agent Integration

这个仓库的目的，是让使用 Codex、Claude Code、Qoder、Cursor、WorkBuddy 等 MCP Agent 的团队不依赖 TuanjieAI，直接通过 CodelyCLI 的 MCP stdio 入口和 Codely Bridge 连接团结 Editor。它提供可复用的团结 Editor 工作流，包含 PowerShell 配置脚本、EditorWindow UPM 包、Tuanjie + Codely Skill 套件、项目级 Agent 配置模板和验收指南。

## 项目定位

这是“多 Agent + 团结 Editor”的本地连接方案，作为 TuanjieAI 之外的工作路径；它不替换团结 Editor 或 Codely Bridge，也不适用于 Unity 官方 Editor 项目。

## 适用边界

- 目标是团结 Editor 项目，不是 Unity 官方 Editor 项目。
- Skill 可以全局安装并复用，但 MCP 的项目路径不能静态地“一次配置覆盖所有项目”。
- 当前版本只声明 Windows + PowerShell 验证，不宣称 macOS/Linux 兼容。
- 不自动安装 Node.js、任意 Agent、CodelyCLI 或 Codely Bridge；设置阶段不启动长期驻留的 MCP 服务，运行时由当前 Agent 按项目配置按需拉起 stdio 子进程，Bridge 则随团结 Editor 自动加载。
- 同一个团结 Editor 同时只允许一个 Agent 执行写入操作；切换客户端前先结束前一会话的写入并确认 Editor 稳定。

## 链路

任意支持本地 STDIO 的 Agent → CodelyCLI MCP/stdio → Codely Bridge → Tuanjie Editor

详见 [架构说明](docs/architecture.md)、[安装与设置](docs/setup-guide.md)、[多 Agent 配置](docs/agent-configurations.md) 和 [排错指南](docs/troubleshooting.md)。

## 推荐的项目级配置

| 客户端 | 项目级入口 | 推荐方式 |
|---|---|---|
| Codex | `.codex/config.toml` | 单项目使用 EditorWindow；PowerShell 仅用于批量/脚本化/CI |
| Claude Code | `.mcp.json` | `claude mcp add --scope project` 或手工 JSON |
| Qoder | MCP 设置页 | 添加本地 STDIO server，粘贴通用 JSON |
| Cursor | `.cursor/mcp.json` | 使用项目配置文件后刷新 MCP 设置 |
| WorkBuddy | `.workbuddy/mcp.json` | 使用项目配置文件后在 MCP 页面刷新 |

所有配置都必须把 `--unity-project-path` 指向当前项目的规范化绝对路径。五个客户端都要安装 Skills，所有团结项目都要安装 EditorWindow；各客户端的完整示例、状态判断和安全边界见 [多 Agent 配置](docs/agent-configurations.md)。

## Skills 套件

Codex、Claude Code、Cursor、Qoder 和 WorkBuddy/CodeBuddy 都安装全部五个 Skill，让入口能够按任务自动分流：

| Skill | 负责内容 |
|---|---|
| `tuanjie-workflows` | 判断团结/Unity 边界并路由专项工作流 |
| `tuanjie-codely-bridge` | CodelyCLI、Bridge、MCP 配置和连接诊断 |
| `tuanjie-editor-automation` | Scene、Prefab、GameObject、组件、资源和脚本编译闭环 |
| `tuanjie-package-management` | 团结包查询、安装、升级、移除和解析版本验收 |
| `tuanjie-codely-custom-tools` | Bridge 自定义工具 API 核对、注册、发现和调用 |

Skill 分别安装到客户端的用户级目录：Codex 使用 `~/.codex/skills/`，Claude Code 使用 `~/.claude/skills/`，Cursor 使用 `~/.cursor/skills/`，Qoder 使用 `~/.qoder/skills/`，WorkBuddy/CodeBuddy 使用 `~/.codebuddy/skills/`。安装地址和通用 Agent 接入提示见 [安装与设置](docs/setup-guide.md)。

典型任务会自动路由：

- “连接不上 Bridge” → `tuanjie-codely-bridge`
- “给 Scene 对象加组件并保存” → `tuanjie-editor-automation`
- “升级 Tuanjie 包并确认实际版本” → `tuanjie-package-management`
- “创建并验证 Bridge 自定义工具” → `tuanjie-codely-custom-tools`

## 运行方式

安装并配置完成后，打开团结项目即可让 Codely Bridge 随 Editor 自动加载和初始化；当前 Agent 首次使用 `tuanjie` MCP 时，会按对应的项目配置自动启动 CodelyCLI 的 stdio 服务，不需要手动运行长期驻留的 MCP 服务。

## 前置条件

1. 安装与项目版本匹配的团结 Editor。
2. 按[官方 Codely Bridge 安装流程](https://codely-docs.tuanjie.cn/en/using-codely/codely-bridge-installation-guide/)安装与 Editor 版本匹配的 Codely Bridge，并打开目标团结项目。
3. 安装 Node.js LTS，并通过 npm 全局安装 CodelyCLI；确保 `codely.cmd` 可从 `PATH` 找到。接入时由 Agent 自动解析其绝对路径，只有自动解析失败时才需要用户提供。
4. 安装至少一个支持本地 MCP STDIO 的 Agent，并在其中打开并信任当前项目目录。

完整安装、通用 Agent 接入提示、EditorWindow、PowerShell、各客户端配置和验证步骤见[安装与设置](docs/setup-guide.md)。普通用户只需按 GitHub 子路径安装 Skill 套件和 EditorWindow UPM 包，不需要克隆或下载整个仓库。

## Agent 规则

任务路由、连接诊断、Editor 写入闭环和自定义工具验收已整合到五个 Skill；项目代理补充规则见[团结规则片段](templates/AGENTS.tuanjie-snippet.md)。

## 许可证

MIT，见 [LICENSE](LICENSE)。
