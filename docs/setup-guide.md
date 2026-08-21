# 安装与设置

本指南是本仓库唯一的完整安装入口，用于让 Codex 直接连接团结 Editor，不要求使用 TuanjieAI。普通用户不需要克隆或下载整个仓库；前置条件由用户准备，后续只安装 GitHub 子路径中的 Skill 和 EditorWindow UPM 包，再生成项目配置。完成前置条件后，可以选择 Agent 主导接入、手动 EditorWindow 或 PowerShell 批量配置三种入口，三者不要连续执行。

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
4. 安装 Codex，在其中打开并信任当前团结项目目录。

Unity 官方 Editor 项目不要使用本仓库的 Codely Bridge Skill、EditorWindow 或 `tuanjie` MCP。

## 2. Agent 主导的项目接入（推荐）

完成前置条件后，把下面这段自包含提示发送给 Codex。提示已经给出 Skill 和 EditorWindow 的公开来源，不要求用户先下载本仓库；Agent 会在一次接入流程中完成 Skill、EditorWindow 包、项目 `config.toml` 和验证，也不会重复安装前置条件中的 Bridge 或 CodelyCLI。

    请使用下面两个公开来源，为当前工作区完成一次团结项目接入。前置条件（团结 Editor、Codely Bridge、CodelyCLI 和 Codex）已由我准备好；不要克隆或下载整个仓库。

    - Skill 来源：https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-bridge
    - EditorWindow UPM 来源：https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package

    约束：
    - 只配置当前项目，不写用户级全局 MCP 配置；Unity 官方 Editor 立即停止。
    - 允许安装本仓库的全局 Skill，并允许为当前项目加入本仓库的 EditorWindow UPM 包；除此之外不要改动无关依赖。
    - 不安装或替换 Codely Bridge，不启动长期驻留的 MCP 服务，不输出 token、端口、descriptor 内容或其他凭据。
    - 修改文件前先确认规范化绝对项目路径；修改 Packages/manifest.json 或 .codex/config.toml 前分别创建 .bak 备份。

    请按顺序执行：
    1. 使用 skill-installer 从上面的 GitHub Skill 子路径安装到用户级 Codex Skill 目录（优先使用 CODEX_HOME，否则使用 %USERPROFILE%\.codex\skills\tuanjie-codely-bridge）。如果当前会话不会动态加载新 Skill，安装后说明需要重新打开一次 Codex 对话。
    2. 检查当前项目 Packages/manifest.json 是否已有 cn.qjx.codex-codely-setup；没有时只添加上面给出的 EditorWindow UPM URL，保留其他依赖并备份 manifest.json。不要手工改 packages-lock.json，等待团结 Editor 完成导入、编译和 Domain Reload。
    3. 从 EditorPrefs、CODELY_CLI_PATH、PATH 或用户提供的路径定位 codely.cmd，运行 --version，并确认 CLI 路径是绝对路径。
    4. 使用已导入的 Window/Tuanjie Codex Setup 预览并生成当前项目的 .codex/config.toml；如果当前 Agent 无法操作 EditorWindow，则使用等价的安全合并逻辑完成同一写入，不要求获取仓库脚本。已有配置需要变化时先备份为 config.toml.bak，只更新 [mcp_servers.tuanjie]，保留其他 MCP 配置。
    5. 重新读取 manifest、EditorWindow 包状态和 config.toml，运行 codex mcp list 确认 tuanjie server 已注册；如当前会话具备实际 MCP 工具，再执行只读连接检查并核对 MCP 项目根与工作区一致。
    6. 最后报告：Skill 安装路径、EditorWindow 包是否新增、项目路径、CodelyCLI 路径和版本、config.toml 是否新建/更新、备份路径、MCP 注册状态、实际连接验证和未完成项目。

Agent 接入流程中，EditorWindow 包是安装结果和后续手动入口；`config.toml` 由 Agent 的安全合并逻辑写入，不需要再点击 EditorWindow 生成一次，也不需要随后重复运行 PowerShell。

### Agent 完成标准

Agent 必须分别说明：

- Skill 是否安装到用户级目录；
- `cn.qjx.codex-codely-setup` 是否已加入当前项目；
- `config.toml` 是否新建或更新，是否创建备份；
- CodelyCLI 路径和版本是否验证；
- `tuanjie` MCP 是否注册，实际 MCP 只读检查是否完成；
- 没有执行的验证或需要用户手动完成的步骤。

## 3. 手动 EditorWindow 配置（不使用 Agent）

这是单项目首次设置的可视化入口；不要在 Agent 接入完成后再重复执行。

1. 在团结 Package Manager 选择 **Add package from git URL**，使用：

       https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge.git?path=/editor-package

   也可以将 `editor-package` 作为本地 UPM 包引用。
2. 打开 `Window/Tuanjie Codex Setup`，点击“刷新状态”，确认项目、Bridge、CodelyCLI 和当前路径被正确识别。
3. 点击“预览配置”，检查目标 `.codex/config.toml`。
4. 点击“生成/更新项目配置”，确认目标路径和 `config.toml.bak` 行为后写入。

窗口只负责项目配置和状态检查，不安装 Bridge，不启动 CodelyCLI 服务。首次手动设置不需要再运行 PowerShell。

## 4. 手动安装全局 Skill（不使用 Agent）

使用 [skill-installer](https://github.com/openai/skills/tree/main/skills/.system/skill-installer) 从以下 GitHub 子路径安装，不要克隆整个仓库：

    https://github.com/QJX-XXXX/codex-tuanjie-codely-bridge/tree/main/skills/tuanjie-codely-bridge

安装后重新打开 Codex 对话，并使用 `$tuanjie-codely-bridge`。Skill 可以全局复用，但不会把某个项目路径固化为全局 MCP 配置。

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
3. 首次对象操作前发送 [只读连接检查](../prompts/readonly-connection-check.md)，核对 MCP 报告的项目根与当前工作区一致；通过后再发送 [写入冒烟测试](../prompts/write-smoke-test.md)。
4. 如果 Editor 正在导入、编译、Domain Reload 或切换 Play Mode，先等待稳定，再进行 MCP 调用。

Codex 首次调用 `tuanjie` MCP 时，会按项目配置自动启动 `codely.cmd serve unity-mcp --stdio`；服务由 Codex 会话管理，不需要每次点击连接。

## 8. 多项目使用

Skill 和 EditorWindow 包可以复用；项目 `.codex/config.toml` 必须随项目分别生成或审核，不能把一个项目的 `--unity-project-path` 当作所有项目的全局配置。
