# 多 Agent MCP 配置

本仓库的连接链路与 Agent 无关：每个客户端都通过本地 stdio 启动 CodelyCLI，再由 Codely Bridge 把请求转给当前团结 Editor。

```text
Agent 客户端
  → codely.cmd serve unity-mcp --stdio --unity-project-path <ProjectRoot>
  → Codely Bridge
  → 团结 Editor
```

`<ProjectRoot>` 必须替换为当前项目的规范化绝对路径。Windows 下使用 `cmd.exe /c` 启动 `.cmd` 文件；不要把真实用户路径、token、端口或 descriptor 提交到仓库。EditorWindow 默认配置用户级全局入口，适合一次只使用一个团结项目；这个静态路径不会自动跟随工作区，多项目并行时选择当前项目范围。

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
| WorkBuddy | `~/.codebuddy/skills/` |

WorkBuddy 的官方兼容 Skill 目录使用 `.codebuddy/skills/`，MCP 配置仍使用 `.workbuddy/mcp.json`。在 EditorWindow 中点击“安装/更新 Skills”会按当前客户端写入对应用户级目录；安装后仍要按客户端支持的 reload/重启方式确认五个 Skill 已被发现。EditorWindow 对所有团结项目必装，并支持为五个客户端选择用户级全局或当前项目范围。

EditorWindow UPM 来源：

```text
https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package
```

## 打开项目与工作区信任

Agent 可以帮助定位项目根、启动客户端或填写项目路径。首次出现信任或访问授权弹窗时，由用户在客户端界面确认；如果当前 Agent 无法操作客户端 UI，就按下面的方法手动打开：

| 客户端 | 手动打开/信任方法 |
|---|---|
| Codex | Codex Desktop 新建会话时选择当前本地项目/文件夹并授予访问权限；使用 Codex CLI 时先在项目根启动 `codex`。 |
| Claude Code | 在项目根启动 `claude`；首次出现 workspace trust 对话框时选择接受。涉及外部文件导入时，按提示批准导入。 |
| Cursor | 使用 **File → Open Folder** 选择项目根；出现 Workspace Trust 提示时选择 **Trust this Workspace**。没有提示时检查 Workspace Trust 是否被策略禁用。 |
| Qoder | 从项目根启动 Qoder/Qoder CLI；首次进入目录时在 Directory Trust 提示中选择本次或记住信任。CLI 可使用 `qodercli -w "<ProjectRoot>"`，额外目录使用 `/add-dir`，不要用额外目录代替主项目根。 |
| WorkBuddy | 创建任务时选择当前项目根作为 workspace；出现目录信任或权限确认时由用户确认。需要长期信任时使用用户级 `trustedDirectories`，不要把本机授权写入团队共享配置。 |

信任成功只说明客户端允许读取项目或加载项目配置，不等于 MCP 已连接。后续仍要完成 Skill 发现、EditorWindow 导入、MCP 注册和实际只读项目根检查。客户端官方参考：[Codex 本地文件访问](https://help.openai.com/en/articles/20001275/)、[Claude Code 工作区](https://code.claude.com/docs/en/worktrees)、[Cursor Workspace Trust](https://www.cursor.com/en/security)、[Qoder Directory Trust](https://docs.qoder.com/cli/permissions)、[WorkBuddy 权限](https://www.workbuddy.ai/docs/zh/cli/settings)。

## EditorWindow 的五客户端目标

| 客户端 | 用户级全局（默认，单项目） | 当前项目（多项目并行推荐） |
|---|---|---|
| Codex | `$CODEX_HOME/config.toml`；未设置时 `~/.codex/config.toml` | `<ProjectRoot>/.codex/config.toml` |
| Claude Code | `~/.claude.json` 顶层 `mcpServers.tuanjie` | `~/.claude.json` 的 `projects[<ProjectRoot>].mcpServers.tuanjie`（local scope） |
| Cursor | `~/.cursor/mcp.json` | `<ProjectRoot>/.cursor/mcp.json` |
| Qoder | `~/.qoder/settings.json` | `<ProjectRoot>/.qoder/settings.local.json` |
| WorkBuddy | `~/.workbuddy/mcp.json` | `<ProjectRoot>/.workbuddy/mcp.json` |

已有 `tuanjie` 时，窗口只替换 `args` 内 `--unity-project-path` 后面的一个字符串，不更新 command、CodelyCLI 路径或其他字段；缺少 `tuanjie` 时才插入最小 server。目标重复、参数缺失、结构异常或预览后文件变化时拒绝写入。写入现有文件前创建同目录 `.bak`，写入后验证目标范围之外的字节完全不变。“重新读取”和“预览配置”不会写文件。

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

EditorWindow 默认写 `~/.claude.json` 的用户级 `mcpServers`；切换到“当前项目”后写同一文件中当前项目对应的 local scope 节点，不会影响其他项目。需要团队共享时，才在项目根使用 `.mcp.json` 或执行：

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

EditorWindow 默认写 `~/.qoder/settings.json`；“当前项目”写 `<ProjectRoot>/.qoder/settings.local.json`，适合本机绝对路径且不会提交给团队。也可以在 Qoder 的 **Settings → MCP → My Servers** 查看或启用条目。本方案只使用 STDIO。

保存后观察服务器条目的连接图标，并展开工具列表确认 `tuanjie` 工具可见。首次调用仍须核对 MCP 报告的项目根；连接图标只能证明客户端完成了连接尝试。

## Cursor

EditorWindow 默认写用户级 `~/.cursor/mcp.json`；它只适合最后配置的单个团结项目。选择“当前项目”后写：

```text
<ProjectRoot>/.cursor/mcp.json
```

将通用 JSON 片段写入该文件后，在 Cursor 的 MCP 设置页刷新并检查 `tuanjie` 的工具列表。若本机安装了 Cursor Agent CLI，可用 `cursor-agent mcp list` 辅助查看注册状态；命令不存在时以 Cursor UI 为准，不要猜测替代命令。

## WorkBuddy

EditorWindow 默认写用户级 `~/.workbuddy/mcp.json`；选择“当前项目”后写：

```text
<ProjectRoot>/.workbuddy/mcp.json
```

用户级配置只适合明确的固定项目，不会自动切换工作区。配置后在 **Plugins → MCP servers → Configure MCP** 中刷新并确认条目显示绿色可用状态；红色状态时按配置路径、CLI 版本和项目根顺序排查。

## Codex

Codex 的用户级和当前项目 TOML 都由 EditorWindow 配置。默认目标是 `$CODEX_HOME/config.toml`，未设置时为 `~/.codex/config.toml`；“当前项目”目标是 `.codex/config.toml`。本仓库的 PowerShell 脚本只用于批量项目、脚本化或 CI 的项目级配置。模板见 [templates/config.toml.example](../templates/config.toml.example)。

## 统一验收与并发边界

按以下顺序验收，不要把注册状态当成实际 Editor 连接：

1. 五个 Skill 已安装到当前客户端的正确用户级目录并被发现。
2. EditorWindow 包已加入当前团结项目并完成导入，团结 Editor 已打开，Codely Bridge 按官方流程随 Editor 加载和初始化。
3. 客户端已加载所选范围的配置，且 `codely.cmd --version` 能执行。
4. 运行客户端自己的 MCP 列表/状态检查，确认 `tuanjie` 已注册或可用。
5. 仅在当前会话真实暴露只读 MCP 工具时执行只读连接检查，并将 MCP 报告的项目根与工作区绝对路径比较；不一致时停止写入。

客户端会按需启动 stdio 子进程，不要求用户手动运行长期驻留的 MCP 服务。Bridge 的生命周期仍由团结 Editor 管理。

同一个团结 Editor 同时只允许一个 Agent 执行写入操作。切换 Agent 前先结束前一会话的写入、确认 Editor 不在导入/编译/Domain Reload/保存状态，再让下一个客户端连接。

官方客户端说明：[Claude Code Skills](https://code.claude.com/docs/en/slash-commands)、[Claude Code MCP](https://code.claude.com/docs/en/mcp)、[Cursor Skills](https://prod.cursor.com/docs/skills)、[Cursor MCP](https://docs.cursor.com/context/model-context-protocol)、[Qoder Skills](https://docs.qoder.com/extensions/skills)、[Qoder MCP](https://docs.qoder.com/cli/mcp-reference)、[WorkBuddy Skills](https://www.workbuddy.ai/docs/zh/cli/skills)、[WorkBuddy MCP](https://www.workbuddy.ai/docs/zh/workbuddy/From-Beginner-to-Expert-Guide/Function-Description/MCP-Guide)。
