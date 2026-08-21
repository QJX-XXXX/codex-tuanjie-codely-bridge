# 多 Agent 的项目级 MCP 配置

本仓库的连接链路与 Agent 无关：每个客户端都通过本地 stdio 启动 CodelyCLI，再由 Codely Bridge 把请求转给当前团结 Editor。

```text
Agent 客户端
  → codely.cmd serve unity-mcp --stdio --unity-project-path <ProjectRoot>
  → Codely Bridge
  → 团结 Editor
```

`<ProjectRoot>` 必须替换为当前项目的规范化绝对路径。Windows 下推荐用 `cmd.exe /c` 启动 `.cmd` 文件；不要把真实用户路径、token、端口或 descriptor 提交到仓库。每个项目单独配置，不能用一个静态用户级配置自动切换所有项目。

## 所有客户端的必装内容

无论使用哪个客户端，接入流程都包含两项共同安装：

1. 将仓库中的五个 Skill 分别安装到当前客户端的用户级 Skill 根目录。
2. 在团结项目中安装 EditorWindow UPM 包 `cn.qjx.codex-codely-setup`。

| 客户端 | 用户级 Skill 根目录 |
|---|---|
| Codex | `$CODEX_HOME/skills/`，未设置时通常为 `~/.codex/skills/` |
| Claude Code | `~/.claude/skills/` |
| Cursor | `~/.cursor/skills/` |
| Qoder | `~/.qoder/skills/` |
| WorkBuddy/CodeBuddy | `~/.codebuddy/skills/` |

WorkBuddy/CodeBuddy 的官方 Skill 目录使用 `.codebuddy/skills/`，MCP 项目配置仍使用下文的 `.workbuddy/mcp.json`。Skill 安装后要按客户端支持的 reload/重启方式确认五个 Skill 已被发现。EditorWindow 对所有团结项目必装，用于统一检查项目、Bridge 和 CodelyCLI；其中“生成/更新项目配置”当前只写 Codex 的 `.codex/config.toml`，其他客户端仍使用下文各自的 MCP 入口。

EditorWindow UPM 来源：

```text
https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package
```

## 通用 JSON 片段

下面的 `mcpServers` 片段可粘贴到支持 JSON MCP 配置的客户端。将两个占位路径替换为实际绝对路径：

```json
{
  "mcpServers": {
    "tuanjie": {
      "command": "cmd.exe",
      "args": [
        "/c",
        "C:\\Tools\\CodelyCLI\\codely.cmd",
        "serve",
        "unity-mcp",
        "--stdio",
        "--unity-project-path",
        "D:\\TuanjieProjects\\YourGame"
      ]
    }
  }
}
```

可直接复制的无凭据模板见 [templates/mcp.json.example](../templates/mcp.json.example)、[templates/cursor-mcp.json.example](../templates/cursor-mcp.json.example) 和 [templates/workbuddy-mcp.json.example](../templates/workbuddy-mcp.json.example)。

## Claude Code

推荐使用项目范围的 `.mcp.json`，这样配置跟随当前项目而不是某个用户目录。也可以在项目根执行：

```powershell
claude mcp add --transport stdio --scope project tuanjie -- cmd.exe /c "C:\Tools\CodelyCLI\codely.cmd" serve unity-mcp --stdio --unity-project-path "D:\TuanjieProjects\YourGame"
```

检查：

```powershell
claude mcp list
claude mcp get tuanjie
```

在 Claude Code 会话内再使用 `/mcp` 查看连接和工具。首次信任项目或 `.mcp.json` 发生变化时，按客户端提示批准项目配置；不要因为列表出现就跳过实际只读检查。

## Qoder

推荐在 Qoder 的 **Settings → MCP → My Servers → Add** 中添加一个本地 STDIO server，粘贴上面的 JSON 片段或分别填写 command/args。Qoder 支持 STDIO、SSE 和 Streamable HTTP；本方案只使用 STDIO。

保存后观察服务器条目的连接图标，并展开工具列表确认 `tuanjie` 工具可见。首次调用仍须核对 MCP 报告的项目根；连接图标只能证明客户端完成了连接尝试。

## Cursor

项目配置放在项目根的 `.cursor/mcp.json`；仅在确实要对所有项目复用同一静态路径时才考虑用户级 `~/.cursor/mcp.json`。推荐使用项目级文件：

```text
<ProjectRoot>/.cursor/mcp.json
```

将通用 JSON 片段写入该文件后，在 Cursor 的 MCP 设置页刷新并检查 `tuanjie` 的工具列表。若本机安装了 Cursor Agent CLI，可用 `cursor-agent mcp list` 辅助查看注册状态；命令不存在时以 Cursor UI 为准，不要猜测替代命令。

## WorkBuddy

推荐使用项目级配置：

```text
<ProjectRoot>/.workbuddy/mcp.json
```

用户级配置位于 `~/.workbuddy/mcp.json`，只适合明确的固定项目，不适合作为多项目自动切换方案。将通用 JSON 片段放入项目配置后，在 **Plugins → MCP servers → Configure MCP** 中刷新并确认条目显示绿色可用状态；红色状态时按配置路径、CLI 版本和项目根顺序排查。

## Codex（现有入口）

Codex 使用项目级 `.codex/config.toml`，普通单项目通过 EditorWindow 生成；本仓库的 PowerShell 脚本只用于批量项目、脚本化或 CI。模板见 [templates/config.toml.example](../templates/config.toml.example)。Window/Tuanjie Codely Agent Setup 当前只生成 Codex 配置，不会替其他 Agent 写入 JSON 文件。

## 统一验收与并发边界

按以下顺序验收，不要把注册状态当成实际 Editor 连接：

1. 五个 Skill 已安装到当前客户端的正确用户级目录并被发现。
2. EditorWindow 包已加入当前团结项目并完成导入，团结 Editor 已打开，Codely Bridge 按官方流程随 Editor 加载和初始化。
3. 客户端已加载对应项目级配置，且 `codely.cmd --version` 能执行。
4. 运行客户端自己的 MCP 列表/状态检查，确认 `tuanjie` 已注册或可用。
5. 仅在当前会话真实暴露只读 MCP 工具时执行只读连接检查，并将 MCP 报告的项目根与工作区绝对路径比较；不一致时停止写入。

客户端会按需启动 stdio 子进程，不要求用户手动运行长期驻留的 MCP 服务。Bridge 的生命周期仍由团结 Editor 管理。

同一个团结 Editor 同时只允许一个 Agent 执行写入操作。切换 Agent 前先结束前一会话的写入、确认 Editor 不在导入/编译/Domain Reload/保存状态，再让下一个客户端连接。

官方客户端说明：[Claude Code Skills](https://code.claude.com/docs/en/slash-commands)、[Claude Code MCP](https://code.claude.com/docs/en/mcp)、[Cursor Skills](https://prod.cursor.com/docs/skills)、[Cursor MCP](https://docs.cursor.com/context/model-context-protocol)、[Qoder Skills](https://docs.qoder.com/extensions/skills)、[Qoder MCP](https://docs.qoder.com/zh/user-guide/chat/model-context-protocol)、[WorkBuddy/CodeBuddy Skills](https://www.workbuddy.ai/docs/zh/cli/skills)、[WorkBuddy MCP](https://www.workbuddy.ai/docs/zh/workbuddy/From-Beginner-to-Expert-Guide/Function-Description/MCP-Guide)。
