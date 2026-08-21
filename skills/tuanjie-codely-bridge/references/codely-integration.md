# Codely 宿主与 Bridge 扩展参考

## Skill 宿主

本仓库的 `skills/tuanjie-codely-bridge` 是一个标准 `SKILL.md` 目录，可以被 Codex 或 Codely CLI/Tuanjie AI 发现；两个宿主的目录和刷新命令不能混用。

| 宿主 | 发现路径 | 选择/刷新 | 生效时机 |
|---|---|---|---|
| Codex | `$CODEX_HOME/skills/`，通常为 `~/.codex/skills/` | `$tuanjie-codely-bridge`；按当前 Codex 的 Skill 加载规则 | 新会话或已确认的 reload 后 |
| Codely CLI/Tuanjie AI 工作区 | `.codely-cli/skills/` 或 `.agents/skills/` | `@` 选择；`/skills list`、`/skills reload` | reload 或新会话 |
| Codely CLI/Tuanjie AI 用户级 | `~/.codely-cli/skills/` 或 `~/.agents/skills/` | `@` 选择；`/skills list`、`/skills reload` | reload 或新会话 |

Codely CLI 支持安装一个独立 Skill 目录，例如 `codely skills install <skill-directory>`。本仓库是多目录仓库时，只将 `skills/tuanjie-codely-bridge` 作为 Skill 目录提供给宿主；不要把仓库根目录当作 Skill，也不要为了使用 Skill 要求用户克隆整个仓库。

官方参考：[Codely Skills](https://codely-docs.tuanjie.cn/features-introduction/skills-experimental/)。

## Bridge 自定义工具

Codely Bridge 可以把项目中的静态 C# 方法注册为自定义工具。自定义工具通常由 `CustomToolAttribute` 描述名称和用途，并通过固定参数对象接收输入；具体注册方式和签名以当前 Bridge 版本及项目代码为准。

使用自定义工具时：

1. 先列出当前会话实际暴露的 `tuanjie` MCP schema，确认 `execute_custom_tool` 或等价工具真实存在。
2. 只使用 schema 中出现的工具名、参数和返回约定；文档示例、Skill 文本、历史会话和用户口述都不能替代 schema。
3. 工具未暴露、参数不匹配或 MCP 根路径未核对时，只做只读诊断并停止。
4. 自定义工具会修改 Scene、Prefab、GameObject、组件或资源时，仍遵循主 Skill 的读取 → 最小动作 → 重读 → 保存 → 再读契约。

官方参考：[Codely Bridge 自定义](https://codely-docs.tuanjie.cn/using-codely/codely-bridge/)。

## 自定义工具的 Skill 文档

如果项目新增了 Bridge 自定义工具，应为该工具维护单独的 Skill 文档，至少说明：何时使用、准确工具名、参数结构、返回字段和一个最小调用示例。文档只解释能力，不代替当前 MCP schema；修改 Skill 或工具注册后，先 reload 或开启新会话，再重新发现工具并核对项目根。
