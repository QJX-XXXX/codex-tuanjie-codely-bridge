# 安装与设置

本指南是本仓库唯一的完整安装入口，用于让支持本地 MCP STDIO 的 Agent 连接团结 Editor，不要求使用 TuanjieAI。普通用户不需要克隆或下载整个仓库；前置条件由用户准备，随后为当前 Agent 安装五个 Skill、为团结项目安装 EditorWindow UPM 包，再生成当前 Agent 的项目级配置。普通单项目使用 Agent 主导或手动流程；PowerShell 只用于批量项目、脚本化或 CI。各客户端的具体文件位置和状态判断见[多 Agent 配置](agent-configurations.md)。

## 1. 前置条件

以下项目外部条件需要先准备好，Agent 不会替你安装或修复：

1. 安装与项目版本匹配的团结 Editor，并确认项目根包含 `Assets`、`Packages`、`ProjectSettings`，`ProjectVersion.txt` 包含团结版本字段。
2. 按[官方 Codely Bridge 安装流程](https://codely-docs.tuanjie.cn/en/using-codely/codely-bridge-installation-guide/)在团结 Package Manager 安装与 Editor 版本匹配的 `cn.tuanjie.codely.bridge`，然后打开目标团结项目；Bridge 会随 Editor 自动加载并初始化，无需单独启动 Bridge。
3. 安装 Node.js LTS（自带 npm），在 PowerShell 安装 CodelyCLI：

       npm install -g @unity-china/codely-cli

   这会把 CodelyCLI 安装为当前用户可用的全局命令。只要 npm 的全局命令目录已进入 `PATH`，不需要提前手工抄写路径；接入时让 Agent 解析 `codely.cmd` 的绝对路径并验证版本：

       $cli = (Get-Command codely.cmd -ErrorAction Stop).Source
       $cli
       & $cli --version

   也可以参考 [Codely CLI 安装说明](https://codely-docs.tuanjie.cn/learn/ai-programming-environment-setup-guide/)。
4. 安装至少一个支持本地 MCP STDIO 和 Agent Skills 的客户端，在其中打开并信任当前团结项目目录。Codex、Claude Code、Cursor、Qoder 和 WorkBuddy/CodeBuddy 都要安装本仓库的五个 Skill，再按[多 Agent 配置](agent-configurations.md)写入各自 MCP 配置。

Unity 官方 Editor 项目不要使用本仓库的 Codely Bridge Skill、EditorWindow 或 `tuanjie` MCP。

## 2. Agent 主导的项目接入（推荐）

下面的提示词可以直接发送给 Codex、Claude Code、Cursor、Qoder 或 WorkBuddy/CodeBuddy。五个客户端都安装 Skills 和 EditorWindow；只有 MCP 配置入口按客户端分支。未识别的其他 Agent 必须先确认其官方 Skill 与项目级 STDIO MCP 机制，不能猜路径。

### 通用 Agent 接入提示

    请为当前工作区完成一次团结项目的 Codely MCP 接入。前置条件（团结 Editor、匹配版本的 Codely Bridge、CodelyCLI 和当前 Agent）已由我准备好；不要克隆或下载整个仓库。

    Skill 来源：
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-workflows
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-bridge
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-editor-automation
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-package-management
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-custom-tools
    EditorWindow UPM：https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package
    配置说明：https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/blob/main/docs/agent-configurations.md

    请按顺序执行：
    1. 规范化当前工作区绝对路径，确认项目根包含 Assets、Packages、ProjectSettings，并从 ProjectVersion.txt、Editor 可执行文件和包信息确认这是团结项目；若是 Unity 官方 Editor，立即停止。
    2. 从系统上下文和可用工具识别当前客户端。将上面的五个 Skill 分别安装到对应用户级目录：Codex 使用 %CODEX_HOME%\skills（未设置时为 %USERPROFILE%\.codex\skills），Claude Code 使用 %USERPROFILE%\.claude\skills，Cursor 使用 %USERPROFILE%\.cursor\skills，Qoder 使用 %USERPROFILE%\.qoder\skills，WorkBuddy/CodeBuddy 使用 %USERPROFILE%\.codebuddy\skills。优先使用当前客户端官方安装器；否则只获取五个公开 Skill 子目录并保留每个目录内的 SKILL.md 和配套资源。不要把仓库根或整个 skills 目录当作一个 Skill。
    3. 检查 Packages/manifest.json 是否已有 cn.qjx.codex-codely-setup。没有时先创建 manifest.json.bak，再只添加 EditorWindow UPM URL，保留其他依赖；不要手工修改 packages-lock.json。等待团结 Editor 完成包解析、导入、编译和 Domain Reload。
    4. 从 EditorPrefs、CODELY_CLI_PATH、PATH 或我提供的路径定位 codely.cmd，确认是绝对文件路径并运行 --version；不要扫描任意磁盘或猜路径。
    5. 只配置当前项目的 tuanjie MCP，不写用户级全局 MCP：Codex 使用 .codex/config.toml 和 [mcp_servers.tuanjie]；Claude Code 使用项目根 .mcp.json；Cursor 使用 .cursor/mcp.json；WorkBuddy 使用 .workbuddy/mcp.json；Qoder 使用 Settings → MCP → My Servers 添加本地 STDIO server。Windows JSON 客户端使用 cmd.exe /c 调用 codely.cmd。所有分支都必须传入 serve、unity-mcp、--stdio、--unity-project-path 和当前项目绝对路径。
    6. 修改现有客户端配置前创建同目录 .bak，只更新 tuanjie server，保留其他 MCP 配置。EditorWindow 的“生成/更新项目配置”当前只用于 Codex；其他客户端使用各自入口，但仍必须安装 EditorWindow 并用“刷新状态”核对项目、Bridge 和 CodelyCLI。
    7. 重新读取五个 Skill 的实际安装目录、Packages/manifest.json、EditorWindow 包状态和客户端配置。按当前客户端支持的 reload/重启方式确认 Skills 已被发现，并使用客户端自己的 MCP 列表或设置页确认 tuanjie 已注册。
    8. 如当前会话已暴露实际 tuanjie MCP 工具，执行只读连接检查并比较 MCP 报告的项目根与工作区绝对路径；不一致时停止写入。列表、文件或绿色图标只能证明配置/连接尝试，不能单独证明 Editor 实际可读。

    约束：
    - 不安装或替换 Codely Bridge，不启动长期驻留 MCP 服务，不输出 token、端口、descriptor 或其他凭据。
    - 不要同时让多个 Agent 写入同一个团结 Editor；Editor 正在导入、编译、Domain Reload、保存或切换 Play Mode 时先等待稳定。
    - 默认流程不使用仓库 PowerShell 脚本；只有用户明确要求批量处理多个项目、脚本化或 CI 时才使用。
    - 当前 Agent 无法操作某个平台 UI、无法安装 Skill 或没有实际 MCP 工具时，明确报告未完成步骤和人工操作，不得伪造完成状态。

    最后报告：客户端识别结果、项目绝对路径、五个 Skill 的安装路径与加载状态、EditorWindow 包状态、CodelyCLI 路径和版本、客户端配置文件及备份、MCP 注册状态、实际只读连接和项目根比较、未完成项目。不要输出任何凭据。

### 完成标准

Agent 必须分别说明：

- 五个 Skill 是否安装到当前客户端的正确用户级目录并被发现；
- `cn.qjx.codex-codely-setup` 是否已加入当前项目并完成导入；
- 当前客户端的项目级 MCP 配置是否新建或更新，是否创建备份；
- CodelyCLI 绝对路径和版本是否验证；
- `tuanjie` MCP 的注册状态、实际只读检查和项目根比较是否完成；
- 没有执行的验证或需要用户手动完成的步骤。

## 3. EditorWindow 安装（所有团结项目必装）

Agent 主导流程会自动完成此步骤；手动接入时按下面步骤安装。EditorWindow 是本仓库对团结项目的统一状态入口，Codex、Claude Code、Cursor、Qoder 和 WorkBuddy/CodeBuddy 都要安装。

1. 在团结 Package Manager 选择 **Add package from git URL**，使用：

       https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package

   也可以将 `editor-package` 作为本地 UPM 包引用。
2. 打开 `Window/Tuanjie Codely Agent Setup`（旧版本也可使用 `Window/Tuanjie Codex Setup`）。UPM 包 ID 仍是 `cn.qjx.codex-codely-setup`，仅为兼容已有项目保留。
3. 所有客户端都点击“刷新状态”，检查项目、Bridge 和 CodelyCLI。
4. 只有 Codex 点击“预览配置”和“生成/更新项目配置”写入 `.codex/config.toml`；其他客户端按[多 Agent 配置](agent-configurations.md)写入各自 MCP 配置。

窗口不安装 Bridge，也不启动 CodelyCLI 服务。普通单项目接入不需要再运行 PowerShell。

> `Window/Tuanjie Codely Agent Setup` 当前只生成 Codex 的 `.codex/config.toml`。Claude Code、Qoder、Cursor、WorkBuddy/CodeBuddy 的推荐项目配置、命令和状态判断统一见[多 Agent 配置](agent-configurations.md)。

## 4. 手动安装全局 Skills（所有 Agent）

五个客户端都安装相同的五个 Skill；区别只是用户级目录和客户端的 reload/重启方式：

| 客户端 | 用户级 Skill 根目录 |
|---|---|
| Codex | `$CODEX_HOME/skills/`，未设置时通常为 `~/.codex/skills/` |
| Claude Code | `~/.claude/skills/` |
| Cursor | `~/.cursor/skills/` |
| Qoder | `~/.qoder/skills/` |
| WorkBuddy/CodeBuddy | `~/.codebuddy/skills/` |

优先使用当前客户端官方 Skill 安装器；否则从以下 GitHub 子路径逐个获取并放入对应根目录，不要克隆整个仓库，也不要把仓库根或整个 `skills` 目录当作一个 Skill：

    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-workflows
    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-bridge
    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-editor-automation
    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-package-management
    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-custom-tools

安装后按当前客户端支持的 reload/重启方式确认五个 Skill 已被发现，再使用 `tuanjie-workflows` 作为入口或显式选择专项 Skill。Skill 可以全局复用，但不会把某个项目路径固化为全局 MCP 配置。

## 5. 批量/脚本化/CI：PowerShell（仅生成 Codex 配置）

只有需要批量处理多个团结项目、脚本化或 CI，且你已经取得仓库脚本文件时，才使用 PowerShell。脚本只生成/更新 Codex 的 `.codex/config.toml`；Claude Code、Qoder、Cursor、WorkBuddy/CodeBuddy 不要用它替代自己的 MCP 配置。它与 EditorWindow 是替代入口，不需要先后运行：

    .\scripts\setup-project.ps1 -ProjectPath "D:\TuanjieProjects\YourGame" -CodelyCliPath "C:\Tools\CodelyCLI\codely.cmd"

如果 `.codex/config.toml` 已存在且需要更新，增加 `-Force`。脚本只更新 `[mcp_servers.tuanjie]`，覆盖前会创建 `config.toml.bak`；首次创建不需要 `-Force`。

## 6. Codex `config.toml` 模板

将项目路径和 CodelyCLI 路径替换为实际绝对路径。Windows TOML 字符串中的反斜杠需要写成两个反斜杠：

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

完整模板位于 [templates/config.toml.example](../templates/config.toml.example)。不要把 token、端口、descriptor 或真实用户凭据提交到仓库。用户级 `~/.codex/config.toml` 可以读取，但静态 MCP 参数只能指向一个项目；多个团结项目应分别维护项目级 `.codex/config.toml`。

## 7. 连接与验证

安装和配置完成后：

1. 保持目标团结 Editor 打开，Bridge 会随 Editor 加载；不需要手动运行长期驻留的 CodelyCLI 服务。
2. Codex 在项目根执行 `codex mcp list`；Claude Code、Qoder、Cursor、WorkBuddy 使用各自的 MCP 列表或设置页确认 `tuanjie` 已注册。这只是配置检查，不等同于实际工具调用成功。
3. 确认当前客户端已经发现五个 Skill，再通过 `tuanjie-workflows` 路由到相应专项 Skill；连接任务必须先做只读项目根核对，Scene/Prefab/组件任务必须按读取 → 最小动作 → 重读 → 保存 → 再读闭环执行。
4. 如果 Editor 正在导入、编译、Domain Reload 或切换 Play Mode，先等待稳定，再进行 MCP 调用。

当前 Agent 首次调用 `tuanjie` MCP 时，会按项目配置自动启动 `codely.cmd serve unity-mcp --stdio`；服务由 Agent 会话管理，不需要手动运行长期驻留服务。部分客户端仍需要在设置页刷新或启用条目，这只是客户端状态确认，不是启动 Bridge 的额外步骤。

## 8. 多项目使用

Skill 和 EditorWindow 包可以复用；各客户端的项目配置必须分别生成或审核，不能把一个项目的 `--unity-project-path` 当作所有项目的全局配置。多个 Agent 连接同一 Editor 时，同时只允许一个 Agent 执行写入。
