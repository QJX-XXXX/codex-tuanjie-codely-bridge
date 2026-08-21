---
name: tuanjie-package-management
description: Use when a Tuanjie Editor project needs package discovery, installation, upgrade, removal, or resolved-version verification through verified Editor or Codely Bridge capabilities; do not use for Unity official Editor projects.
---

# Tuanjie Package Management

这个 Skill 负责团结 Package Manager 的读取、变更和解析验收。它不把 Unity 官方包管理流程或不存在的 Codely 命令套到团结项目。

## 入口闸门

确认规范化项目绝对路径、团结版本标识、目标 Editor、Bridge 和当前实际暴露的包管理能力。Unity 官方 Editor 停止本 Skill；没有真实包管理工具时，先报告能力缺失，不编造命令。

开始变更前必须知道准确包名、来源、目标版本、兼容性和用户授权。只说“最新版”时先发现可用版本并说明选择依据，不能猜版本。

## 安全规则

- 先读取 `Packages/manifest.json`、当前解析结果和相关依赖，再决定动作。
- 优先使用当前 Editor/Bridge 实际暴露的包管理能力；包名、参数和返回字段必须来自当前 schema。
- 默认不手工编辑 `manifest.json`；若能力不可用且用户明确授权，先说明影响并创建恢复备份，只做最小合并。
- 永远不要手工修改 `packages-lock.json`。
- 包请求完成后等待包解析、资源导入、编译和 Domain Reload 稳定，再读取 manifest、解析结果和 Console。

详细查询、安装、升级和移除清单见 [references/package-workflows.md](references/package-workflows.md)。

## 完成报告

分别报告包名、来源、请求版本、实际解析版本、manifest 是否变化、备份路径、解析/编译状态、失败/重试和未完成验证。文件写入成功不等于包已解析或 Editor 已稳定。
