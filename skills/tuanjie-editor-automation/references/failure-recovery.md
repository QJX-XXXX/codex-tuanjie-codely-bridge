# Failure Recovery

## 统一处理

1. 记录失败的操作、目标和非敏感错误类型，不记录 token、端口、descriptor 或临时认证信息。
2. 重新读取目标对象或资源，判断操作是否已经部分成功。
3. 只有无副作用且确认幂等的操作，才用完全相同的目标和参数重试一次。
4. 再次失败就停止当前 MCP 操作，保留已安全完成的文件修改，并报告 Editor 验证未完成。

## 不可盲重试

删除、覆盖、Apply Prefab、Revert Prefab、批量重建、批量移动和可能创建重复对象的操作，在前一次结果未确认前禁止重试。MCP 根路径不一致、Bridge 未验证、schema 没有目标能力或编译未完成时禁止对象写入。

团结项目的 `tuanjie` MCP 失败时，先检查 CLI、Bridge、Editor 和项目根；不要默认切换到 Unity MCP。若没有安全的 Editor 侧方案，只报告文件级修改和未完成的 Editor 验证。

## Codely 状态恢复矩阵

| 症状 | 当前 schema 中的恢复入口 | 约束 |
|---|---|---|
| 编译、导入或后台任务繁忙 | `unity_editor.get_state`，必要时 `unity_editor.wait_for_idle` | 稳定后重读目标；不要重复提交写入 |
| Play Mode 阻止写入 | `unity_editor.get_state` | 默认切到只读；只有任务明确要求且用户允许时才改变 Play Mode |
| 调用明确返回 detached/pending job | `unity_job.status`/`check`，使用响应给出的 `job_id` | 普通同步调用不轮询；完成结果只收集一次 |
| 不确定有哪些未完成 job | `unity_job.list` | 只查看当前 in-flight job，不从历史名称猜 job id |
| 原生模态框阻塞 Editor | `unity_dialog.click` | 只匹配已报告的标题/按钮；确认、丢弃、覆盖等按钮先取得用户授权 |
| 脚本出现 stale SHA | `unity_script.get_sha` 后重新生成精确编辑 | 先重读文件，不用旧补丁覆盖用户变化 |
| 超时但可能已写入 | 重新调用对应只读查询 | 已部分成功时验证并停止重复写入 |

工具名或 action 未出现在当前 schema 时，回到统一处理，不用 MCP for Unity 的同名能力替代。
