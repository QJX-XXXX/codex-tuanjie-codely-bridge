# 安装与设置

本指南是本仓库唯一的完整安装入口，用于让支持本地 MCP STDIO 的 Agent 连接团结 Editor，不要求使用 TuanjieAI。普通用户不需要克隆或下载整个仓库；前置条件由用户准备，后续按需安装 GitHub 子路径中的 Skill 套件和 EditorWindow UPM 包，再生成当前 Agent 的项目级配置。完成前置条件后，可以选择 Agent 主导接入、手动 EditorWindow 或 PowerShell 批量配置三种入口，三者不要连续执行。四个平台的具体文件位置和状态判断见[多 Agent 配置](agent-configurations.md)。

## 1. 前置条件

以下项目外部条件需要先准备好，Agent 不会替你安装或修复：

1. 安装与项目版本匹配的团结 Editor，并确认项目根包含 `Assets`、`Packages`、`ProjectSettings`，`ProjectVersion.txt` 包含团结版本字段。
2. 按[官方 Codely Bridge 安装流程](https://codely-docs.tuanjie.cn/en/using-codely/codely-bridge-installation-guide/)在团结 Package Manager 安装与 Editor 版本匹配的 `cn.tuanjie.codely.bridge`，然后打开目标团结项目；Bridge 会随 Editor 自动加载并初始化，无需单独启动 Bridge。
3. 安装 Node.js LTS（自带 npm），在 PowerShell 安装 CodelyCLI：

       npm install -g @unity-china/codely-cli

   找到 `codely.cmd` 的绝对路径并验证版本：

       $cli = (Get-Command codely.cmd -ErrorAction Stop).Source
       $cli
       & $cli --version

   也可以参考 [Codely CLI 安装说明](https://codely-docs.tuanjie.cn/learn/ai-programming-environment-setup-guide/)。
4. 安装至少一个支持本地 MCP STDIO 的 Agent，在其中打开并信任当前团结项目目录。Codex 还可以使用本仓库的全局 Skill 套件；其他 Agent 直接按[多 Agent 配置](agent-configurations.md)写入自己的 MCP 配置。

Unity 官方 Editor 项目不要使用本仓库的 Codely Bridge Skill、EditorWindow 或 `tuanjie` MCP。

## 2. Agent 主导的项目接入（推荐）

完成前置条件后，把下面这段自包含提示发送给具备本地文件和命令操作能力的 Agent。提示已经给出五个 Skill 和 EditorWindow 的公开来源，不要求用户先下载本仓库；Agent 会在一次接入流程中完成可用的 Skill 套件、EditorWindow 包、当前 Agent 的项目配置和验证，也不会重复安装前置条件中的 Bridge 或 CodelyCLI。若当前 Agent 没有 `skill-installer` 或 `codex mcp`，它必须跳过对应的 Codex 专属检查，改用[多 Agent 配置](agent-configurations.md)中该客户端的项目级入口，不得声称已完成不存在的命令。

    请使用下面的公开来源，为当前工作区完成一次团结项目接入。前置条件（团结 Editor、Codely Bridge、CodelyCLI 和当前 Agent）已由我准备好；如果当前 Agent 不支持 Codex Skills 或 `codex mcp`，按多 Agent 配置指南使用它自己的项目级入口；不要克隆或下载整个仓库。

    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-workflows
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-bridge
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-editor-automation
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-package-management
    - https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-custom-tools
    - EditorWindow UPM：https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package

    约束：
    - 只配置当前项目，不写用户级全局 MCP 配置；Unity 官方 Editor 立即停止。
    - 允许安装本仓库的全局 Skill，并允许为当前项目加入本仓库的 EditorWindow UPM 包；除此之外不要改动无关依赖。
    - 不安装或替换 Codely Bridge，不启动长期驻留的 MCP 服务，不输出 token、端口、descriptor 内容或其他凭据。
    - 修改文件前先确认规范化绝对项目路径；修改 Packages/manifest.json 或 .codex/config.toml 前分别创建 .bak 备份。

    请按顺序执行：
    1. 使用 skill-installer 逐个从上面的五个 GitHub Skill 子路径安装到用户级 Codex Skill 目录（优先使用 CODEX_HOME，否则使用 %USERPROFILE%\.codex\skills\）。如果当前会话不会动态加载新 Skill，安装后说明需要重新打开一次 Codex 对话；不要把仓库根目录当作 Skill。
    2. 检查当前项目 Packages/manifest.json 是否已有 cn.qjx.codex-codely-setup；没有时只添加上面给出的 EditorWindow UPM URL，保留其他依赖并备份 manifest.json。不要手工改 packages-lock.json，等待团结 Editor 完成导入、编译和 Domain Reload。
    3. 从 EditorPrefs、CODELY_CLI_PATH、PATH 或用户提供的路径定位 codely.cmd，运行 --version，并确认 CLI 路径是绝对路径。
    4. 使用已导入的 Window/Tuanjie Codex Setup 预览并生成当前项目的 .codex/config.toml；如果当前 Agent 使用 Claude Code、Qoder、Cursor 或 WorkBuddy，则按多 Agent 配置指南写入对应的项目级 MCP 配置，不要误写 .codex/config.toml。已有配置需要变化时先备份，只更新 tuanjie server，保留其他 MCP 配置。
    5. 重新读取 manifest、EditorWindow 包状态和当前 Agent 配置，运行该客户端实际支持的 MCP 列表/状态检查确认 tuanjie 已注册；如当前会话具备实际 MCP 工具，再执行只读连接检查并核对 MCP 项目根与工作区一致。
    6. 最后报告：Skill 安装路径（若宿主支持）、EditorWindow 包是否新增、项目路径、CodelyCLI 路径和版本、当前 Agent 配置是否新建/更新、备份路径、MCP 注册状态、实际连接验证和未完成项目。

Agent 接入流程中，EditorWindow 包是安装结果和后续手动入口；Codex 的 `.codex/config.toml` 由 EditorWindow 或 Agent 的安全合并逻辑写入，其他 Agent 的配置按[多 Agent 配置](agent-configurations.md)写入，不需要重复运行另一种入口。

### Agent 完成标准

Agent 必须分别说明：

- 五个 Skill 是否分别安装到用户级目录（仅在当前宿主支持时报告）；
- `cn.qjx.codex-codely-setup` 是否已加入当前项目；
- `config.toml` 是否新建或更新，是否创建备份；
- CodelyCLI 路径和版本是否验证；
- 当前 Agent 的 `tuanjie` MCP 是否注册，实际 MCP 只读检查是否完成；
- 没有执行的验证或需要用户手动完成的步骤。

## 3. 手动 EditorWindow 配置（不使用 Agent）

这是单项目首次设置的可视化入口；不要在 Agent 接入完成后再重复执行。

1. 在团结 Package Manager 选择 **Add package from git URL**，使用：

       https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package

   也可以将 `editor-package` 作为本地 UPM 包引用。
2. 打开 `Window/Tuanjie Codely Agent Setup`（旧版本也可使用 `Window/Tuanjie Codex Setup`），点击“刷新状态”，确认项目、Bridge、CodelyCLI 和当前路径被正确识别。UPM 包 ID 仍是 `cn.qjx.codex-codely-setup`，仅为兼容已有项目保留。
3. 点击“预览配置”，检查目标 `.codex/config.toml`。
4. 点击“生成/更新项目配置”，确认目标路径和 `config.toml.bak` 行为后写入。

窗口只负责项目配置和状态检查，不安装 Bridge，不启动 CodelyCLI 服务。首次手动设置不需要再运行 PowerShell。

> Window/Tuanjie Codex Setup 当前只生成 Codex 的 `.codex/config.toml`。Claude Code、Qoder、Cursor、WorkBuddy 的推荐项目配置、命令和状态判断统一见[多 Agent 配置](agent-configurations.md)。

## 4. 手动安装全局 Skill（不使用 Agent）

使用 [skill-installer](https://github.com/openai/skills/tree/main/skills/.system/skill-installer) 从以下 GitHub 子路径逐个安装，不要克隆整个仓库：

    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-workflows
    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-bridge
    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-editor-automation
    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-package-management
    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-custom-tools

安装后重新打开 Codex 对话，并使用 `$tuanjie-workflows` 作为入口；也可以显式使用某个专项 Skill。Skill 可以全局复用，但不会把某个项目路径固化为全局 MCP 配置。

## 5. 批量/脚本化/CI：PowerShell

只有需要批量处理多个团结项目、脚本化或 CI，且你已经取得仓库脚本文件时，才使用 PowerShell；它与 EditorWindow 是替代入口，不需要先后运行：

    .\scripts\setup-project.ps1 -ProjectPath "D:\TuanjieProjects\YourGame" -CodelyCliPath "C:\Tools\CodelyCLI\codely.cmd"

如果 `.codex/config.toml` 已存在且需要更新，增加 `-Force`。脚本只更新 `[mcp_servers.tuanjie]`，覆盖前会创建 `config.toml.bak`；首次创建不需要 `-Force`。

## 6. `config.toml` 模板

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
2. 在项目根执行 `codex mcp list`，确认 `tuanjie` server 已注册。这是配置检查，不等同于实际工具调用成功。
3. 首次对象操作前使用 `$tuanjie-workflows` 路由到相应专项 Skill；连接任务由 `$tuanjie-codely-bridge` 做只读项目根核对，Scene/Prefab/组件任务由 `$tuanjie-editor-automation` 按读取 → 最小动作 → 重读 → 保存 → 再读闭环执行。
4. 如果 Editor 正在导入、编译、Domain Reload 或切换 Play Mode，先等待稳定，再进行 MCP 调用。

当前 Agent 首次调用 `tuanjie` MCP 时，会按项目配置自动启动 `codely.cmd serve unity-mcp --stdio`；服务由 Agent 会话管理，不需要手动运行长期驻留服务。部分客户端仍需要在设置页刷新或启用条目，这只是客户端状态确认，不是启动 Bridge 的额外步骤。

## 8. 多项目使用

Skill 和 EditorWindow 包可以复用；各客户端的项目配置必须分别生成或审核，不能把一个项目的 `--unity-project-path` 当作所有项目的全局配置。多个 Agent 连接同一 Editor 时，同时只允许一个 Agent 执行写入。
