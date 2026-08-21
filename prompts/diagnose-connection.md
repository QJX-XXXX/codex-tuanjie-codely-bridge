# 连接诊断

请使用 $tuanjie-codely-bridge，只做诊断，不修改文件、Scene、Prefab 或项目设置。

按以下顺序报告：

1. 团结项目身份：目录结构、ProjectVersion.txt 的团结标识、当前 Editor 类型与版本。
2. 项目配置：.codex/config.toml 是否存在，mcp_servers.tuanjie 的项目路径是否与工作区一致；不要输出凭据。
3. CodelyCLI：来源（EditorPrefs、CODELY_CLI_PATH 或 PATH）、绝对路径和版本命令结果。
4. Bridge：cn.tuanjie.codely.bridge 是否已安装，descriptor 是否只存在而不读取内容。
5. MCP：当前实际暴露的 tuanjie 工具/schema、连接项目根和 Editor 状态。
6. Console/编译/Play 状态以及下一步最小修复建议。

不要因为用户要求“越快”而跳过路径核对，不要编造不存在的工具，不要在团结 MCP 失败时未经验证切换 Unity MCP。若遇 401，只按安全重试规则说明，不输出 token、端口或 descriptor。
