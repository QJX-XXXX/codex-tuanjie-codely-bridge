# Codely 宿主与 Bridge 扩展参考

## Skill 宿主

本仓库的 `skills/tuanjie-codely-bridge` 是一个标准 `SKILL.md` 目录，可以被支持 Skills 的 Agent 宿主（包括 Codex、Codely CLI/Tuanjie AI）发现；不同宿主的目录和刷新命令不能混用。Claude Code、Qoder、Cursor、WorkBuddy 即使不加载 Codex Skill，也可以按各自的用户级或当前项目 MCP 配置使用同一 Codely Bridge 链路。

| 宿主 | 发现路径 | 选择/刷新 | 生效时机 |
|---|---|---|---|
| Codex | `$CODEX_HOME/skills/`，通常为 `~/.codex/skills/` | `$tuanjie-codely-bridge`；按当前 Codex 的 Skill 加载规则 | 新会话或已确认的 reload 后 |
| Codely CLI/Tuanjie AI 工作区 | `.codely-cli/skills/` 或 `.agents/skills/` | `@` 选择；`/skills list`、`/skills reload` | reload 或新会话 |
| Codely CLI/Tuanjie AI 用户级 | `~/.codely-cli/skills/` 或 `~/.agents/skills/` | `@` 选择；`/skills list`、`/skills reload` | reload 或新会话 |

Codely CLI 支持安装一个独立 Skill 目录，例如 `codely skills install <skill-directory>`。本仓库是多目录仓库时，只将 `skills/tuanjie-codely-bridge` 作为 Skill 目录提供给宿主；不要把仓库根目录当作 Skill，也不要为了使用 Skill 要求用户克隆整个仓库。

官方参考：[Codely Skills](https://codely-docs.tuanjie.cn/features-introduction/skills-experimental/)。

四个平台的 MCP 配置路径和验证方式见 [agent-client-configurations.md](agent-client-configurations.md)。

## Bridge 边界

Bridge 随团结 Editor 加载和初始化；Skill 安装、MCP 注册和实际 Editor 连接是三个不同状态。自定义工具的 API、注册、schema 发现和调用由 `tuanjie-codely-custom-tools` 负责；本 Skill 只判断连接前提是否成立。

官方参考：[Codely Bridge 自定义](https://codely-docs.tuanjie.cn/using-codely/codely-bridge/)。
