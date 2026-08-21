# Setup and Config

## 适用范围

仅在当前工作区已经确认是团结项目、用户允许修改项目配置且目标是连接 Codex 与当前项目时读取本文件。不要用它安装或替换团结 Editor、Codely Bridge、CodelyCLI 或 Node.js。

## 安全顺序

```text
规范化项目绝对路径
→ 从 EditorPrefs、CODELY_CLI_PATH、PATH 或用户路径定位 codely.cmd
→ 确认路径为绝对文件并运行 --version
→ 预览项目级 .codex/config.toml 合并
→ 目标文件已有变化时创建 config.toml.bak
→ 只更新 [mcp_servers.tuanjie]
→ 重读配置并运行 codex mcp list
```

修改前报告目标路径和备份行为。只保留其他 MCP table、注释和无关配置；不要把用户级 MCP 配置当作当前项目的自动切换机制。不要手工修改 `packages-lock.json`。

## 状态含义

| 检查 | 能证明 | 不能证明 |
|---|---|---|
| `codely.cmd --version` | CLI 路径和版本可执行 | Bridge 已连接 |
| `config.toml` 存在 | 项目配置文件已写入 | 配置参数可用或项目根正确 |
| `codex mcp list` 出现 `tuanjie` | MCP server 已注册 | 团结 Editor 正在运行或实际可读 |
| 只读 MCP 返回项目根一致 | 当前会话实际到达目标项目 | 任意写入动作成功 |

如果当前会话无法操作 EditorWindow，可以用等价的安全合并逻辑完成配置，但不能为了证明连接而启动长期驻留服务或输出敏感连接信息。
