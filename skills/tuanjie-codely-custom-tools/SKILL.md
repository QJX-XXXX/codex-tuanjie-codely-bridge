---
name: tuanjie-codely-custom-tools
description: Use when a Tuanjie Editor project needs a Codely Bridge custom tool designed, registered, discovered, documented, or safely invoked through the current tuanjie MCP schema; do not use for Unity official Editor projects.
---

# Tuanjie Codely Custom Tools

这个 Skill 负责项目级 Codely Bridge 自定义工具的 API 核对、实现边界、注册发现和安全调用。它不把官方文档、历史对话或用户口述当作当前 MCP 能力证明。

## 入口闸门

确认规范化项目绝对路径、团结 Editor、Bridge 包版本和当前实际暴露的 `tuanjie` MCP schema。Unity 官方 Editor、MCP 根路径不一致或目标能力未暴露时停止写入和调用。

先检查本地 Bridge 包和项目现有 Editor 程序集；API 未能从当前版本确认时，不生成固定 C# 模板。已验证的接口说明和示例见 [references/custom-tool-contract.md](references/custom-tool-contract.md)。

## 生命周期

```text
定义单一职责和副作用
→ 核对当前 Bridge API
→ 放入现有 Editor 程序集边界
→ 编译和 Domain Reload
→ 重新发现当前 MCP schema
→ 确认准确工具名、参数和返回约定
→ 最小调用
→ 重读受影响对象、保存并再读
```

必须分别报告：方法是否编译、Bridge 是否扫描、工具是否出现在 schema、调用是否成功。只有 schema 中实际出现的工具名和参数才可使用；未暴露时只做诊断，不用相似工具替代，也不默认切换 Unity MCP。

工具修改 Scene、Prefab、GameObject、组件或资源时，仍遵循 `tuanjie-editor-automation` 的读取 → 最小动作 → 重读 → 保存 → 再读契约；工具创建/调用不自动取得批量删除、覆盖或资源重建授权。

## 项目级文档

项目新增工具后维护单独 Skill 或 reference，至少记录使用条件、准确工具名、schema 参数、返回字段、输入边界和一个最小验证场景。修改注册代码或 Skill 后 reload 或开启新会话，再重新发现 schema；文件复制成功不等于当前会话已加载。
