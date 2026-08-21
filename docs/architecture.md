# 架构说明

## 项目定位

本仓库面向使用不同 MCP Agent 的团结项目团队，提供一条不依赖 TuanjieAI 的本地 Editor 连接路径。Agent 负责交互，CodelyCLI 和 Codely Bridge 负责把 MCP 请求送入团结 Editor；团结 Editor 仍是项目运行和资源状态的事实来源。

## Skill 路由层

在连接链路之上，Skills 套件按任务分工：

```text
Tuanjie Workflows
├─ Codely Bridge / CLI / MCP 配置与诊断
├─ Editor Scene / Prefab / GameObject / 资源自动化
├─ Package Manager 查询与解析验收
└─ Bridge 自定义工具 API、注册与 schema 验证
```

`tuanjie-workflows` 只负责判断团结/Unity 边界和选择专项 Skill；专项 Skill 可以独立安装，并且各自重复执行项目根、Editor 类型和实际 schema 闸门。只修改普通代码、配置或文档时，使用文件级工具，不强制调用 MCP。

## Agent 配置层

不同客户端的配置文件和状态命令不同，但都指向同一个 CodelyCLI stdio 入口。EditorWindow 默认选择用户级全局（单项目），也可选择当前项目（多项目并行推荐）：

```text
Codex             → ~/.codex/config.toml 或 <ProjectRoot>/.codex/config.toml
Claude Code       → ~/.claude.json（user 或当前项目 local scope）
Qoder             → ~/.qoder/settings.json 或 <ProjectRoot>/.qoder/settings.local.json
Cursor            → ~/.cursor/mcp.json 或 <ProjectRoot>/.cursor/mcp.json
WorkBuddy         → ~/.workbuddy/mcp.json 或 <ProjectRoot>/.workbuddy/mcp.json
                           ↓
        codely.cmd serve unity-mcp --stdio --unity-project-path <ProjectRoot>
```

配置层只负责声明如何启动本地 MCP 子进程；它不安装 Bridge、不启动长期驻留服务，也不改变项目根。每个客户端都必须在实际使用前重新验证 MCP 报告的项目根。

## 连接链路

    任意支持本地 STDIO 的 Agent
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

只有团结标识、团结 Editor、Bridge、实际 schema 和目标连接根路径同时通过，才允许对象语义写入。Unity 官方 Editor 不走本仓库的 Codely Bridge 路由；不会因为用户提到 Codely Bridge 而把 Unity 项目切到团结流程。

## 配置层级

Skill 是通用行为规则，可以安装到支持 Skill 的用户级目录并复用。MCP server 的 args 包含 `--unity-project-path`，必须指向具体项目。EditorWindow 默认用户级全局配置以减少单项目首次接入步骤，但这个静态条目只能指向最后配置的一个项目；多个项目同时使用时选择当前项目范围。完整路径和示例见[多 Agent 配置](agent-configurations.md)。

PowerShell 模块只处理 Codex 项目 TOML；EditorWindow 通过五个显式客户端适配器处理 TOML 或 JSON/JSONC。已有 `tuanjie` 时，EditorWindow 只替换 `--unity-project-path` 后面的字符串，其他字节保持不变；缺少条目时才最小插入。写入前检查预览是否过期并创建恢复备份，写入后执行字节边界和语义双重校验。

## EditorWindow 边界

`Window/Tuanjie Codely Agent Setup` 提供五客户端选择、用户级/当前项目范围、状态、CLI 选择、五 Skill 安装/更新、唯一变更预览、显式写入和配置目录。客户端注册表是固定列表，不通过反射增加第六个客户端。窗口不自动安装包、不写 manifest、不读取 descriptor 内容、不启动 CodelyCLI 服务；“重新读取”和预览始终只读。

## 验证闭环

对象操作遵循 state → action → re-read → save → re-read。C# 文件修改先等待资源刷新、编译和 Domain Reload，再读取 Console；有本次编译错误就停止依赖新程序集的 Scene/Prefab 操作。包变更还要等待解析并核对实际版本；自定义工具还要区分方法编译、Bridge 扫描、MCP schema 暴露和实际调用。失败只对已确认幂等动作使用相同参数重试一次。
