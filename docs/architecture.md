# 架构说明

## 项目定位

本仓库面向选择 Codex 的团结项目团队，提供一条不依赖 TuanjieAI 的本地 Editor 连接路径。Codex 负责 Agent 交互，CodelyCLI 和 Codely Bridge 负责把 MCP 请求送入团结 Editor；团结 Editor 仍是项目运行和资源状态的事实来源。

## 连接链路

    Codex
      ↓ MCP stdio
    CodelyCLI (serve unity-mcp)
      ↓ TCP / Bridge protocol
    Codely Bridge package
      ↓ Editor API
    Tuanjie Editor

CodelyCLI 是连接宿主和 MCP 进程，不等同于独立 Unity CLI。Codely Bridge 是团结 Editor 侧的适配包；它不是 Unity 官方 Pipeline 包的同名替代。

## 项目识别

仓库工具同时检查：

- Assets、Packages、ProjectSettings 三个目录；
- ProjectVersion.txt 中的 m_EditorVersion 与 m_TuanjieEditorVersion；
- 当前 Editor 可执行文件名 Tuanjie.exe；
- Packages/manifest.json 中的 cn.tuanjie.codely.bridge；
- MCP 报告的项目根是否与工作区绝对路径一致。

只有团结标识、团结 Editor、Bridge 和目标连接同时通过，才允许对象语义写入。Unity 官方 Editor 不走本仓库的 Codely Bridge 路由。

## 配置层级

Skill 是通用行为规则，可以安装到用户级 Skill 目录并复用。MCP server 的 args 则包含 --unity-project-path，必须指向具体项目，因此项目级 .codex/config.toml 是推荐做法。用户级 ~/.codex/config.toml 适合固定的单项目或作为模板，不能自动发现当前工作区并安全切换项目。

PowerShell 模块和 EditorWindow 都只更新精确的 mcp_servers.tuanjie table；其他 MCP server 原样保留。已有配置需要变化时，PowerShell 的 Force 或 EditorWindow 的确认对话都会先创建恢复备份。

## EditorWindow 边界

Window/Tuanjie Codex Setup 提供状态、CLI 选择、配置预览、显式生成、打开配置目录、打开 Package Manager 和复制提示。它不自动安装包、不写 manifest、不读取 descriptor 内容、不启动 CodelyCLI 服务。预览是只读的，写入前显示目标路径和 config.toml.bak 行为。

## 验证闭环

对象操作遵循 state → action → re-read → save → re-read。C# 文件修改先等待资源刷新、编译和 Domain Reload，再读取 Console；有本次编译错误就停止依赖新程序集的 Scene/Prefab 操作。失败只对已确认幂等动作使用相同参数重试一次。
