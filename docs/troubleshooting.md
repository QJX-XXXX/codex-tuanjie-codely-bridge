# 排错指南

## MCP 未出现在 Codex

1. 确认当前工作目录是目标项目根。
2. 确认 .codex/config.toml 的 command、CodelyCLI 路径和 --unity-project-path 都是有效绝对路径。
3. 运行 codex mcp list，检查 server 名称为 tuanjie。
4. 保持目标团结 Editor 打开；Bridge 按官方流程随 Editor 加载和初始化。重新打开 Codex 对话后，再用实际只读 MCP 调用核对项目根，不把窗口或包文件存在当作“已连接”证明。

静态全局 config.toml 不能自动绑定所有项目；切换项目后要重新审核 --unity-project-path。

## CodelyCLI 找不到或版本失败

优先顺序是 EditorWindow 中保存的路径、CODELY_CLI_PATH 环境变量、PATH 中的 codely.cmd。不要扫描任意磁盘，也不要猜测文件名。先运行：

    & "C:\Tools\CodelyCLI\codely.cmd" --version

如果命令不可执行，修复路径或权限后再刷新窗口。

## 项目被识别为 Unity

检查 Editor 可执行文件是否为 Tuanjie.exe，以及 ProjectVersion.txt 是否包含 m_TuanjieEditorVersion。Unity 官方项目不得通过改名或手工添加字段伪装为团结项目。

## Bridge 缺失

窗口只会显示缺失并打开 Package Manager 入口。按项目实际团结版本安装 cn.tuanjie.codely.bridge，等待导入和连接，再重新读取状态。工具不会自动改 Packages/manifest.json。

## MCP 根路径不一致

如果 Codex 工作区和 MCP 状态报告的项目根不同，立即停止写入。关闭错误项目的连接，连接目标项目后重新读取根路径；不要把调用成功当作写入当前项目。

## 写入前拒绝覆盖

已有 config.toml 发生差异时，PowerShell 需要 -Force，EditorWindow 需要确认对话。检查 config.toml.bak 后再继续；重复 table、边界不完整或写入校验失败都应停止。

## C# 编译错误

先等待资源刷新、编译和 Domain Reload，再读取 Console。处理本次改动引入的错误后重新等待稳定；在程序集未加载前不要附加新组件、修改 Prefab 或保存依赖新类型的资源。

## 401 Unauthorized

这是连接链路的临时认证问题，不要读取或输出 token、端口或 descriptor。等待约两秒，仅对完全相同的安全命令重试一次；仍失败则停止并报告未完成的 Editor 验证。

## 专项 Skill 未安装

先确认请求属于哪个 Skill：`tuanjie-codely-bridge` 负责连接，`tuanjie-editor-automation` 负责对象/脚本，`tuanjie-package-management` 负责包，`tuanjie-codely-custom-tools` 负责自定义工具。缺少对应目录时，从 README 或设置指南中的 GitHub 子路径安装；不要让入口 Skill 猜测专项流程，也不要克隆整个仓库。

## Skill 已复制但当前会话没有新能力

文件复制或安装成功不等于当前会话已经加载。Codex 重新打开对话；Codely Skills 按宿主支持的 `/skills reload` 或新会话刷新。刷新后再次确认实际可用的 Skill 和 `tuanjie` MCP schema。

## 路由正确但 MCP 没有目标能力

文档、历史对话和用户口述都不能替代 schema。工具未暴露、参数不匹配或项目根未核对时只做只读诊断并停止；不要用相似工具替代，也不要默认切换到 Unity MCP。

## 包解析没有完成

包请求返回后仍可能处于解析、导入、编译或 Domain Reload。暂停后续包变更，等待 Editor 稳定，再重读 manifest、实际解析版本和 Console；不要手工编辑 `packages-lock.json`。

## 自定义工具编译成功但未注册

“方法编译成功”只证明程序集可编译；还要等 Domain Reload，重新发现当前 MCP schema，并确认准确工具名和参数。如果 schema 没有该工具，报告 Bridge 扫描/暴露未完成，不调用、不回退、不声称可用。
