# Agent 自动设置本地连接

本指南用于让已安装 tuanjie-codely-bridge Skill 的 Agent，在当前团结项目根自动完成本地 Codex MCP 配置。它只配置当前项目的 .codex/config.toml，不把某个项目路径写成全局通用配置。

## 前置条件

- 当前工作区是目标团结项目根，并且项目使用团结 Editor。
- 团结 Editor 已打开，Codely Bridge 已按[官方安装指南](https://codely-docs.tuanjie.cn/en/using-codely/codely-bridge-installation-guide/)安装，并在 Bridge 自带的状态窗口确认 `Connected/Ready`。
- CodelyCLI 已安装；Agent 能从 EditorPrefs、CODELY_CLI_PATH、PATH 或用户提供的路径中找到 codely.cmd。
- 当前项目已经在 Codex 中打开并信任。

Bridge 缺失、Editor 类型是 Unity 官方 Editor、MCP 根路径不一致或 CodelyCLI 无法验证时，Agent 应停止配置并说明阻断原因。包安装和 EditorWindow 导入仍由用户明确操作，不由这段提示自动修改 manifest。

## 可复制给 Agent 的设置请求

    请使用 $tuanjie-codely-bridge，在当前工作区完成“当前项目”的本地 Codely Bridge MCP 配置。

    约束：
    - 只针对当前项目写入 .codex/config.toml，不修改用户级全局 MCP 配置。
    - 这是团结项目工作流；如果当前是 Unity 官方 Editor，立即停止，不调用 tuanjie MCP。
    - 不自动安装 Bridge、不修改 Packages/manifest.json、不启动长期运行的服务。
    - 不输出或读取 token、端口、descriptor 内容或其他凭据。

    请按顺序执行：
    1. 规范化并确认当前项目绝对路径、Tuanjie.exe、团结版本和 Bridge 连接。
    2. 从 EditorPrefs、CODELY_CLI_PATH、PATH 或已提供路径中定位 codely.cmd，并运行 --version 验证。
    3. 确认当前 MCP 连接报告的项目根与工作区完全一致；不一致时停止。
    4. 使用仓库 scripts/setup-project.ps1 或等价的安全配置逻辑，生成/合并当前项目的 .codex/config.toml。
    5. 如果已有配置需要变化，先创建 config.toml.bak；只更新 mcp_servers.tuanjie，保留其他 MCP table。
    6. 运行 codex mcp list 或当前环境等价的只读检查，确认 tuanjie server 已加载。
    7. 重新读取配置并报告：项目路径、CodelyCLI 来源/版本、Bridge 状态、MCP 根路径、备份路径和未完成验证。

    不要因为用户要求“越快”而跳过项目路径或工具 schema 核对；不要编造 MCP 工具名或参数。没有证据时不要报告“连接成功”。

## Agent 完成标准

Agent 必须分别报告：

1. 配置文件是否新建或更新；
2. 是否创建了备份；
3. CodelyCLI 路径和版本是否验证；
4. tuanjie MCP 是否加载且指向当前项目；
5. Bridge、Editor 和 Console 是否有未解决错误；
6. 没有执行的验证或需要用户手动完成的步骤。

## 与 EditorWindow 的关系

这是自动化入口，不是 EditorWindow 的前置步骤。首次手动设置请选择 EditorWindow；批量/脚本化场景请选择 PowerShell；希望由 Agent 在当前项目完成配置时使用本指南。三者最终维护同一个项目级 .codex/config.toml，不需要串联执行。
