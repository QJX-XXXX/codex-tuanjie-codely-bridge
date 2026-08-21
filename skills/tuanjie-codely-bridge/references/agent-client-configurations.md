# Agent 客户端配置路由

本 Skill 可以使用 EditorWindow 配置用户级全局或当前项目 MCP。用户级全局适合一次只使用一个项目；多项目并行时使用当前项目范围。公共命令是：

```text
codely.cmd serve unity-mcp --stdio --unity-project-path <ProjectRoot>
```

Windows 下 JSON 客户端推荐将 `command` 设为 `cmd.exe`，将 `/c`、`codely.cmd` 绝对路径和上述参数放入 `args`。不要记录 token、端口或 descriptor。

| Agent | 用户级全局 | 当前项目 | 注册/状态检查 |
|---|---|---|---|
| Codex | `~/.codex/config.toml` | `.codex/config.toml` | `codex mcp list` |
| Claude Code | `~/.claude.json` user | `~/.claude.json` local 项目节点 | `claude mcp list`、`claude mcp get tuanjie`、会话 `/mcp` |
| Qoder | `~/.qoder/settings.json` | `.qoder/settings.local.json` | 设置页连接图标和工具列表 |
| Cursor | `~/.cursor/mcp.json` | `.cursor/mcp.json` | MCP 设置页；存在 CLI 时才用 `cursor-agent mcp list` |
| WorkBuddy | `~/.workbuddy/mcp.json` | `.workbuddy/mcp.json` | Plugins → MCP servers 的配置页和绿色状态 |

EditorWindow 固定支持上述五个客户端。已有条目只替换 `--unity-project-path` 后的路径；目标不唯一或结构异常时拒绝写入。不要用完整 JSON/TOML 反序列化覆盖用户文件。

所有客户端都必须在实际只读调用后比较 MCP 报告的项目根和工作区绝对路径。连接图标、列表出现或文件存在都不能单独证明 Editor 实际可读。同一团结 Editor 同时只允许一个 Agent 执行写入。
