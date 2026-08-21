# 工具路由参考

## 决策表

| 事实 | 路由 | 禁止 |
|---|---|---|
| 团结版本标识 + Tuanjie.exe + Bridge/tuanjie MCP 已验证，且根路径一致 | 当前 schema 中的 tuanjie MCP | unityMCP 作为默认替代 |
| Unity 官方 Editor | 按 com.unity.pipeline 实际状态选择已连接的官方工具 | Codely Bridge、凭名称猜测 Pipeline |
| Bridge 缺失、MCP 未连接或路径不一致 | 只读诊断、修复连接后再继续 | 改 manifest、自动安装、写错项目 |
| 纯文件代码/配置/文档 | 文件级工具 | 为文本修改调用对象语义 MCP |

### 最小状态核对

1. 读取目标项目绝对路径和 Editor 类型/版本。
2. 读取 MCP 连接报告的绝对项目根，执行字符串规范化后比较。
3. 列出当前会话真实可用的 MCP 工具和参数；若没有所需能力，停止并说明。
4. 不读取、复制或输出端口、token、descriptor 或临时认证信息。

### 自定义工具

只使用当前 Bridge 注册并实际暴露的自定义工具。文档、用户提示或历史会话中的名称都不是能力证明；若 schema 中没有某个工具，不能用相似名称替代。
