---
name: tuanjie-codely-bridge
description: Use when a Tuanjie Editor project needs Codely Bridge, CodelyCLI, Codex MCP configuration, connection diagnostics, or read-only connection acceptance; do not use for Unity official Editor projects.
---

# Tuanjie Codely Bridge

这个 Skill 只负责连接层，不替换团结 Editor、Codely Bridge 或 CodelyCLI。它把“Skill 已安装”“MCP 已注册”和“Editor 实际可读”分开证明。

## Skill 宿主与生效

本 Skill 可被不同宿主发现，但安装目录和刷新方式不同；不要把“文件已复制”当作“当前会话已加载”。

- Codex：安装到 `$CODEX_HOME/skills/tuanjie-codely-bridge`（未设置时通常是 `~/.codex/skills/tuanjie-codely-bridge`），在新会话中使用 `$tuanjie-codely-bridge`。安装或修改后，当前会话没有重新加载证明时，不得声称新版本已生效。
- Codely CLI / Tuanjie AI：工作区使用 `.codely-cli/skills/` 或 `.agents/skills/`，用户级使用 `~/.codely-cli/skills/` 或 `~/.agents/skills/`；可用 `codely skills install <skill-directory>` 管理一个 Skill 目录。通过 `@` 选择 Skill，使用 `/skills list` 或 `/skills reload` 检查和刷新；修改后按官方行为开启新会话或 reload。
- 当前仓库是多目录仓库时，只安装 `skills/tuanjie-codely-bridge` 这个 Skill 目录，不把仓库根目录当作 Skill。

详细宿主路径和刷新规则见 [references/codely-integration.md](references/codely-integration.md)。需要生成或诊断项目级 `.codex/config.toml` 时读取 [references/setup-and-config.md](references/setup-and-config.md)；需要分层判断连接状态时读取 [references/connection-diagnostics.md](references/connection-diagnostics.md)。

## 入口闸门

开始连接操作前，读取并核对：

- 工作区规范化绝对路径；
- Editor 可执行文件和版本；
- 项目是否包含团结版本标识；
- Codely Bridge 包/连接是否真实存在；
- MCP 报告的项目根是否等于当前工作区；
- 当前会话真实暴露的 `tuanjie` MCP schema。

Unity 官方 Editor 不是本 Skill 的目标。即使用户说“用 Codely Bridge”，也不能把团结路由套到 Unity。缺失 Bridge、MCP schema 或路径不一致时只做只读诊断，不改 manifest、不自动安装、不猜测工具。

## 连接层职责

1. 从 EditorPrefs、`CODELY_CLI_PATH`、`PATH` 或用户提供的绝对路径定位 `codely.cmd`，运行 `--version`。
2. 只更新当前项目 `.codex/config.toml` 的 `[mcp_servers.tuanjie]`；已有配置变化时先备份，保留其他 MCP table。
3. 用 `codex mcp list` 证明 server 注册；这不等于 Editor 实际连接。
4. 如果实际暴露只读 MCP 工具，核对返回的项目根；否则报告实际连接验收未完成。
5. 不读取、复制或输出 token、端口、descriptor 或临时认证信息。

## 委派边界

- Scene、Prefab、GameObject、组件和资源操作：**REQUIRED SUB-SKILL:** 使用 `tuanjie-editor-automation`。
- 包查询、安装、升级、移除和解析版本验收：**REQUIRED SUB-SKILL:** 使用 `tuanjie-package-management`。
- Bridge 自定义工具设计、注册、发现和调用：**REQUIRED SUB-SKILL:** 使用 `tuanjie-codely-custom-tools`。
- 只修改普通代码、配置或文档：使用文件级工具，不为文本修改调用 MCP。

连接问题的完成报告必须分别写出：Skill 生效状态、CLI 绝对路径和版本、项目配置是否新建/更新及备份、MCP 注册状态、实际只读连接状态、失败/重试以及未完成项。没有实际读取证据时，不得声称 Bridge 已连接。
