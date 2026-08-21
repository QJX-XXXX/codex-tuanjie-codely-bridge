# 团结项目代理补充规则

- 任务入口优先使用 `tuanjie-workflows`；连接配置使用 `tuanjie-codely-bridge`，Editor 对象和脚本闭环使用 `tuanjie-editor-automation`，包变更使用 `tuanjie-package-management`，Bridge 自定义工具使用 `tuanjie-codely-custom-tools`。
- 团结项目通过 Codely Bridge 连接时，优先使用当前实际可用的 tuanjie MCP；Unity 官方项目不要使用本规则。
- 首次 Editor 操作前核对规范化绝对项目根、Tuanjie.exe、Bridge 包、实际 schema 和 MCP 报告根路径。
- Scene、Prefab、GameObject 或组件修改遵循：读取 → 最小修改 → 重新读取 → 保存 → 再读取。
- 包变更先读取 manifest 和解析状态，等待解析/导入/编译稳定后核对实际解析版本；不得手工修改 packages-lock.json。
- 自定义工具必须先核对当前 Bridge API，再等编译和 Domain Reload，最后以 MCP schema 中真实出现的工具名和参数作为可调用证明。
- 不编造 MCP 工具名、参数、端口、token 或 descriptor；不把调用成功当作状态验证。
- C# 修改后等待刷新、编译和 Domain Reload，读取 Console；有编译错误时停止依赖新程序集的对象操作。
- 删除、覆盖、Apply Prefab 或批量操作在结果未确认前不盲目重试。
