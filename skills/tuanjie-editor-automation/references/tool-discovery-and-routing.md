# Codely Tool Discovery and Routing

本 reference 使用 Codely Bridge/tuanjie MCP 的逻辑工具名；宿主显示的 `mcp__tuanjie__` 等前缀不是工具契约的一部分。每次会话先读取当前 schema：名称或 action 未暴露时，停止使用该条目，不用 MCP for Unity、Unity Pipeline 或历史名称替代。

## State First

对象写入前按当前 schema 选择：

```text
unity_editor.get_state
→ unity_editor.get_project_root
→ 比较工作区与 MCP 项目根
→ unity_editor.wait_for_idle（仅在有导入、编译或后台任务时）
→ 读取目标 Scene/对象/资源
→ 执行最小动作
```

`get_state` 用于 Play Mode、编译、导入、活动 Scene 和脏状态；`get_project_root` 用于防止写错项目。不要把 `wait_for_idle` 当作固定开场仪式：状态已稳定时不增加无意义等待。

## 能力路由

| Codely 工具 | 优先用途 | 写前/写后检查 |
|---|---|---|
| `unity_editor` | 状态、项目根、编译流水线、等待空闲、Play Mode | `get_state`、`get_project_root`；状态变化后重读 |
| `unity_scene` | 活动 Scene、层级、打开和保存 | `get_active`/`get_hierarchy`；保存用 `ensure_scene_saved` 后重读 |
| `unity_gameobject` | 查找、子层级、组件、创建和修改对象 | 先 `find`/`get_components`；配置型写入优先真实存在的 `ensure_*` action |
| `unity_script` | 脚本读取、创建、更新、校验和基于 SHA 的编辑 | 写后进入编译流水线；`validate` 不代替 Console |
| `unity_asset` | 资源搜索、信息、组件、移动和元文件完整性 | 先 `search`/`get_info`；重读路径、Importer/属性和 `.meta` 状态 |
| `unity_console` | 建立错误边界并读取日志 | 一般操作使用 `clear` → 动作 → `get`；编译流水线已清理时直接 `get` |
| `unity_screenshot` | Game/Scene View、相机、资源或 UI Toolkit 视觉证据 | 先做数据级验证；截图后仍核对对象、引用和保存状态 |
| `unity_job` | 只处理明确返回 detached/pending job 的调用 | 使用返回的 `job_id` 调 `status`/`check`；不要为普通同步调用创建轮询 |
| `unity_dialog` | 处理明确报告的原生模态框 | 只点击已知标题和按钮；可能丢数据的按钮先取得用户确认 |
| `unity_package` | 包安装、移除、列表和 UPM 等待 | **REQUIRED SUB-SKILL:** 使用 `tuanjie-package-management` |
| `execute_custom_tool` | 调用当前 schema 已注册的 Bridge 自定义工具 | **REQUIRED SUB-SKILL:** 使用 `tuanjie-codely-custom-tools` |
| `execute_csharp_script` | 当前内置工具确实缺少的一次性低风险 Editor 查询/操作 | 短小、目标明确；复杂或需复用逻辑写项目侧 Editor 工具 |

## API 可信度

```text
当前会话实际 schema/运行时返回
> 当前项目与已安装 Bridge 源码或程序集
> 对应版本官方文档
> 历史会话、示例或模型记忆
```

表格是发现提示，不是能力保证。参数名、action 和返回字段始终以当前 schema 为准。

## 常见误用

- 把宿主前缀写进项目文档：记录 `unity_scene`，不要固化某个客户端的 `mcp__...` 名称。
- 把本表当成已连接证明：先看当前 schema，再核对 `get_project_root`。
- 每次操作都调用 `wait_for_idle`：只有状态显示繁忙或存在后台任务时才等待。
- 内置工具能完成时直接用 `execute_csharp_script`：优先职责明确、返回可验证的 Codely 内置工具。
- 用截图代替保存或引用验证：截图只补充视觉证据。
