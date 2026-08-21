# Package Workflows

所有流程都从项目根、Editor/Bridge、当前 manifest 和实际解析状态开始。使用当前 schema 中真实存在的 Codely 能力，不凭记忆调用命令。

## Codely `unity_package` 路由

当前 schema 暴露时使用以下逻辑工具名和 action；宿主的 `mcp__tuanjie__` 前缀不属于契约：

| action | 用途 | 关键输入 |
|---|---|---|
| `unity_package.list_packages` | 读取已解析包和实际版本 | 无 |
| `unity_package.install_package` | 安装 registry 包或 Git URL | `id_or_url`，可选 `version`/`timeoutSeconds` |
| `unity_package.remove_package` | 移除准确包名 | `package_name`，可选 `timeoutSeconds` |
| `unity_package.wait_for_upm` | 等待已经存在的 UPM 操作稳定 | 仅在状态/响应证明 UPM 正在运行时使用 |

`install_package` 和 `remove_package` 在当前 Codely 语义下返回时已经完成解析和 Domain Reload；普通调用不要创建 `op_id` 或额外轮询。`wait_for_upm` 不是每次安装后的固定步骤，只用于外部 Package Manager 操作或明确进行中的 UPM 状态。action 未出现在当前 schema 时，停止使用本表。

## 查询

先调用 `unity_package.list_packages`（当前 schema 存在时），再读取 manifest/lock 的直接依赖和解析来源，核对目标包当前版本、来源和依赖。若用户要求最新版，先列出实际可用版本和团结 Editor 兼容性，再请求确认目标版本；没有准确包名时停止询问。

## 安装

确认准确包名、来源、目标版本和现有依赖。使用 `unity_package.install_package` 的 `id_or_url` 及当前 schema 支持的 `version`；不要同时拼接互相冲突的两种版本表达。调用返回后读取 Console，并再次 `list_packages`、重读 manifest，检查实际解析版本、重复依赖和冲突。

## 升级

读取当前版本和依赖约束，确认目标版本不是未经验证的猜测。使用 `unity_package.install_package` 请求准确的新版本；返回后再次 `list_packages` 并读取 Console。若实际解析版本不符或失败，保留原配置证据并报告，不盲目降级或覆盖其他包。

## 移除

通过 `list_packages`、manifest/lock 和项目引用确认包没有被保留的直接/传递依赖，明确影响范围后调用 `unity_package.remove_package(package_name=...)`。返回后再次 `list_packages`、重读 manifest 和 Console；不手工清理 `packages-lock.json`。

## manifest 例外

只有当前 Editor/Bridge 包管理能力确实不可用且用户明确授权时，才可备份后最小修改 `manifest.json`。保留其他依赖、来源和格式；任何 lock 文件变化都由团结 Editor 生成，不由 Agent 手写。
