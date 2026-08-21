---
name: tuanjie-workflows
description: Use when a Tuanjie Editor project request mentions Codely Bridge, tuanjie MCP, Scene or Prefab automation, Tuanjie packages, or Bridge custom tools and the correct specialized workflow must be selected; do not use for Unity official Editor projects.
---

# Tuanjie Workflows

这是 Tuanjie + Codely Skill 套件的入口路由。它只判断项目边界和任务类型，不重复专项命令；路由结果必须以当前工作区和实际工具能力为准。

## 使用前闸门

先读取并规范化当前工作区绝对路径，确认存在 `Assets`、`Packages`、`ProjectSettings`，再检查 `ProjectVersion.txt` 中的团结版本字段。Unity 官方 Editor 项目立即停止本 Skill 的 Tuanjie 路由，不因为用户提到 Codely Bridge 而改变引擎判断。

需要 Editor 对象语义时，还要确认 Bridge、`tuanjie` MCP 和 MCP 报告的项目根。根路径不一致、Bridge 未验证或所需 schema 未暴露时，只做诊断并停止写入。

## 任务路由

| 请求信号 | 路由 |
|---|---|
| Skill 安装、CodelyCLI、`config.toml`、MCP 注册、连接故障 | `tuanjie-codely-bridge` |
| Scene、Prefab、GameObject、组件、资源、脚本编译或保存 | `tuanjie-editor-automation` |
| 查询、安装、升级、移除或验证 UPM 包 | `tuanjie-package-management` |
| 设计、注册、发现、调用或记录 Bridge 自定义工具 | `tuanjie-codely-custom-tools` |
| 只改普通代码、配置或 Markdown | 使用文件级工具，不强制调用 MCP |

有明确文件后缀或对象关键词时优先使用对应路由；一个请求包含多个独立任务时按依赖顺序分别路由，并在每一步报告边界。不要因为某个专项 Skill 未安装而复制它的流程或猜测工具名；报告准确 Skill 名称和需要安装的公开来源。

## 共用停止条件

- 不把 Skill 文件已复制、`codex mcp list` 成功或一次工具调用成功当作实际 Editor 连接证明。
- 不猜测 `tuanjie` MCP 工具、参数、Bridge 自定义工具名称或返回结构。
- 不把 `unityMCP` 当作团结项目的默认回退。
- Editor 正在导入、编译、Domain Reload、保存或切换 Play Mode 时，先等待稳定。

## 完成报告

路由后要求专项 Skill 分别报告：已确认的项目根、选择的工具层、实际修改、编译/包解析状态、重新读取和保存验证、失败/重试/回退以及未完成验证。没有相应证据时，不得声称连接、注册或 Editor 修改已完成。
