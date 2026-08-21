---
name: tuanjie-codely-bridge
description: Use when a Tuanjie Editor project needs Codely Bridge, CodelyCLI, or tuanjie MCP setup, diagnostics, or Editor automation; do not use for Unity official Editor projects.
---

# Tuanjie Codely Bridge

这个 Skill 只覆盖“团结 Editor + Codely Bridge + CodelyCLI/tuanjie MCP”链路。核心契约是：

**先确认状态和目标项目 → 选择已验证工具 → 最小动作 → 重新读取 → 保存/测试确认。**

## 入口闸门

调用任何 Editor 对象语义工具前，读取并核对：

- 工作区规范化绝对路径；
- Editor 可执行文件和版本；
- 项目是否包含团结版本标识；
- Codely Bridge 包/连接是否真实存在；
- MCP 报告的项目根是否等于当前工作区。

Unity 官方 Editor 不是本 Skill 的目标。即使用户说“用 Codely Bridge”，也不能把团结路由套到 Unity；缺失 Bridge 或路径不一致时停止对象写入，不改 manifest、不自动安装、不猜测工具。

## 工具路由

只从当前会话实际暴露的 schema 选择工具。禁止因用户提示而编造 tuanjie.execute_csharp、参数或自定义工具；也不要把工具调用成功当作连接或修改成功。

- 团结项目、Bridge 已验证且 MCP 根路径一致：使用实际可用的 tuanjie MCP。
- Unity 官方项目：按项目真实的 Pipeline 状态选择已连接的官方 Unity 工具；不调用 Codely Bridge。
- 仅代码/配置/Markdown：使用文件级工具；无需为了改文本调用 MCP。
- Bridge、Editor、项目路径或 schema 未验证：先做只读诊断；没有安全方案就停止。

## 修改契约

Scene、Prefab、GameObject、组件或资源：先读对象和引用，执行一个最小可回滚动作，立即重读并检查重复对象、关键字段、引用和脏状态，再按需保存并重新读取保存结果。失败时先判断是否部分成功；仅对幂等操作保持完全相同参数安全重试一次，删除/覆盖/Apply 等操作未确认前不重试。团结 MCP 失败不得未经验证切换到 Unity MCP。

新增或修改 C# 后，先等待资源刷新、编译和 Domain Reload 稳定，读取 Console；有本次编译错误就停止所有依赖新程序集的对象操作。最终报告必须分别说明文件修改、编译、测试、Editor 验证和未完成项。

## 参考

- 需要工具选择细节时读取 references/tool-routing.md。
- 需要验证顺序和失败处理时读取 references/verification-workflows.md。
