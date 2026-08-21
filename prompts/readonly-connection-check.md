# 只读连接检查

请使用 $tuanjie-codely-bridge。

这是只读检查，不创建、删除、移动或修改任何对象，不写 Scene/Prefab/Asset，不启动 CodelyCLI 服务。请按顺序：

1. 规范化当前工作区绝对路径，确认 Editor 可执行文件名、版本和团结项目标识。
2. 从当前实际 schema 中选择可用的 tuanjie MCP 工具，读取 Editor 状态和 MCP 报告的项目根。
3. 检查 Bridge 是否真实连接、当前场景和编译/Play 状态。
4. 比较 MCP 项目根与工作区路径；不一致时停止。

不要编造工具名、参数、端口、token 或 descriptor 内容。最后分开报告：项目身份、Bridge/MCP 连接、场景/Editor 状态、路径一致性和未完成验证。
