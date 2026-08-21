---
name: tuanjie-editor-automation
description: Use when a Tuanjie Editor project needs safe Scene, Prefab, GameObject, component, asset, or script-to-Editor automation through a verified tuanjie MCP; do not use for Unity official Editor projects.
---

# Tuanjie Editor Automation

这个 Skill 负责团结 Editor 的对象语义操作和“脚本文件 → Editor 类型”闭环。它不编造 MCP 工具名，也不把文件写入成功当作对象或程序集已经可用。

## 入口闸门

开始前确认：

1. 工作区规范化绝对路径、团结版本标识和目标 Editor 类型/版本；Unity 官方 Editor 停止本 Skill。
2. Codely Bridge、`tuanjie` MCP 和当前会话真实暴露的 schema。
3. MCP 报告的项目根与工作区一致。
4. Editor 不在导入、编译、Domain Reload、保存或切换 Play Mode 状态。
5. 目标对象、范围、修改权限和可回滚边界已明确。

任一项不满足时只做诊断，不写 Scene、Prefab、GameObject、组件或资源。

## 选择参考

- Scene、GameObject、组件、Prefab、ScriptableObject、材质或 Importer：读取 [references/object-workflows.md](references/object-workflows.md)。
- 新增/修改 C# 后要附加组件或调用新类型：读取 [references/script-compile-workflows.md](references/script-compile-workflows.md)。
- 调用超时、返回失败、可能部分成功或需要重试：读取 [references/failure-recovery.md](references/failure-recovery.md)。

## 修改契约

```text
核对项目根和 Editor 状态
→ 读取目标对象、层级、引用和脏状态
→ 执行一个最小动作
→ 重新读取关键属性并检查重复对象/重复组件/丢失引用
→ 按需保存
→ 再次读取保存后的状态
```

只从当前 schema 选择实际存在的工具和参数；不要用相似工具替代。对象写入后必须报告读取、动作、重读、保存和再读的证据。删除、覆盖、Apply Prefab 和批量重建等操作在前一次结果未确认前禁止盲重试。

## 完成报告

分别列出文件修改、编译和 Domain Reload、相关测试、Editor 重新读取与保存验证、失败/重试/回退、风险和未完成验证。没有对应证据时，不得声称对象修改或 Editor 验收完成。
