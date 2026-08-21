# Failure Recovery

## 统一处理

1. 记录失败的操作、目标和非敏感错误类型，不记录 token、端口、descriptor 或临时认证信息。
2. 重新读取目标对象或资源，判断操作是否已经部分成功。
3. 只有无副作用且确认幂等的操作，才用完全相同的目标和参数重试一次。
4. 再次失败就停止当前 MCP 操作，保留已安全完成的文件修改，并报告 Editor 验证未完成。

## 不可盲重试

删除、覆盖、Apply Prefab、Revert Prefab、批量重建、批量移动和可能创建重复对象的操作，在前一次结果未确认前禁止重试。MCP 根路径不一致、Bridge 未验证、schema 没有目标能力或编译未完成时禁止对象写入。

团结项目的 `tuanjie` MCP 失败时，先检查 CLI、Bridge、Editor 和项目根；不要默认切换到 Unity MCP。若没有安全的 Editor 侧方案，只报告文件级修改和未完成的 Editor 验证。
