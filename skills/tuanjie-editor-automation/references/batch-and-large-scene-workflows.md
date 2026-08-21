# Batch and Large Scene Workflows

## 批量操作入口

只有目标集合、依赖顺序、副作用和验证方式都明确时才使用 Codely batch。当前 schema 暴露时：

- GameObject：`unity_gameobject.create_batch`、`unity_gameobject.edit_batch`；
- Asset：`unity_asset.create_batch`、`unity_asset.edit_batch`。

不要照搬其他 MCP 的固定批量上限。根据当前 schema、响应大小和可验证性分块；每块都必须有唯一目标、稳定 `id` 和可重读的结果。

## 批量执行契约

1. 先用 `unity_gameobject.find/get_components` 或 `unity_asset.search/get_info` 读取目标，生成精确操作清单。
2. 独立操作可使用 `mode="continue_on_error"`，但必须逐项收集失败；存在父子、创建后引用或顺序依赖时使用 `mode="stop_on_error"`。
3. 同一批内需要引用新对象时，只在当前 schema 支持时使用 `captureAs` 和 `$alias`；缺少该能力就拆成两批，中间重读。
4. 配置型写入优先 `ensure_component` 等实际暴露的幂等 action，避免重复对象或组件。
5. 每批完成后重新搜索/读取目标，核对数量、组件、引用、路径和脏状态，再继续下一批。

删除、覆盖、Apply/Revert Prefab、批量移动或重建不因 batch 存在而自动获得授权。结果未知时先重读，禁止整批盲重试。

## 超大 Scene 渐进读取

`unity_scene.get_hierarchy` 在大场景中可能只返回根节点。不要因此请求完整全量层级；按以下顺序缩小范围：

```text
unity_scene.get_active / get_hierarchy（浅层）
→ unity_gameobject.find（按 name/path/tag/layer/component 等当前 searchMethod）
→ unity_gameobject.list_children（限制 depth）
→ unity_gameobject.get_components（只读目标组件）
→ 修改
→ 用同一查询重新读取
```

当 `list_children` 的 schema 提供 `resultMode`、`maxInlineItems` 和 `outputPath` 时，大结果优先 `auto` 或 `file`；读取文件结果时只提取当前任务所需目标，不把整个文件回灌上下文。

## 大量资源搜索

`unity_asset.search` 当前支持 `page_number` 和 `page_size` 时，逐页读取直到没有下一页结果；按资产类型、模式或日期尽早过滤。分页过程中以资源路径/GUID 等稳定标识去重，不根据显示名称批量写入。
