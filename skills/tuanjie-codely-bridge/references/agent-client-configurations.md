# Agent 客户端配置路由

本 Skill 只写当前项目的 MCP 配置，不把一个项目的 `--unity-project-path` 写入用户级全局配置。公共命令是：

```text
codely.cmd serve unity-mcp --stdio --unity-project-path <ProjectRoot>
```

Windows 下 JSON 客户端推荐将 `command` 设为 `cmd.exe`，将 `/c`、`codely.cmd` 绝对路径和上述参数放入 `args`。不要记录 token、端口或 descriptor。

| Agent | 项目级入口 | 注册/状态检查 |
|---|---|---|
| Codex | `.codex/config.toml` | `codex mcp list` |
| Claude Code | `.mcp.json` | `claude mcp list`、`claude mcp get tuanjie`、会话 `/mcp` |
| Qoder | Settings → MCP → My Servers | 设置页连接图标和工具列表 |
| Cursor | `.cursor/mcp.json` | MCP 设置页；存在 CLI 时才用 `cursor-agent mcp list` |
| WorkBuddy | `.workbuddy/mcp.json` | Plugins → MCP servers 的配置页和绿色状态 |

Claude Code 与 Qoder 可以复用同一份 `mcpServers` JSON 结构，但 Qoder 推荐通过设置页保存；Cursor 和 WorkBuddy 使用相同的 JSON 形状，但文件路径不同。Codex 的 EditorWindow 只生成 `.codex/config.toml`，不会替其他客户端写 JSON。

所有客户端都必须在实际只读调用后比较 MCP 报告的项目根和工作区绝对路径。连接图标、列表出现或文件存在都不能单独证明 Editor 实际可读。同一团结 Editor 同时只允许一个 Agent 执行写入。
