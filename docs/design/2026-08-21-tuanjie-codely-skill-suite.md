# Tuanjie + Codely Skill 套件设计

## 背景

当前仓库只有 `tuanjie-codely-bridge` 一个 Skill，同时承担接入、连接诊断、工具路由、Editor 对象修改、自定义工具和验证规则。它能阻止最危险的误用，但职责过多，Agent 很难从触发描述判断应该加载哪些细节，也无法像 Unity Technologies 的 Skills 仓库一样按任务组合能力。

本次设计参考 Unity Technologies Skills 的组织模式：入口 Skill 负责判断和分流，专项 Skill 负责具体流程，较长的操作细节放入 `references/`。只借鉴组织原则，不复制 Unity 官方 Skill 的文本、命令或 Unity 6、Unity Pipeline、UGS 等平台假设。

## 目标

- 将现有单体 Skill 重构为五个可独立安装、可组合调用的 Tuanjie + Codely Skills。
- 保留 `tuanjie-codely-bridge` 名称和公开 GitHub 子路径，避免破坏现有安装说明。
- 为常见团结项目任务提供明确路由：连接接入、Editor 对象操作、包管理、自定义 Bridge 工具。
- 所有 Editor 写入都以当前会话实际暴露的 `tuanjie` MCP schema 和规范化项目根为能力证明。
- 对 Unity 官方 Editor、连接错项目、编译未完成、工具未暴露等情况安全停止。
- 保持普通用户无需克隆整个仓库的安装方式。

## 非目标

- 不移植 Unity Technologies 仓库的全部领域 Skill。
- 不提供 Unity 官方 Editor、Unity Pipeline 或 `unityMCP` 的兼容层。
- 不自动安装或替换团结 Editor、Codely Bridge、CodelyCLI、Node.js 或 Codex。
- 不启动长期驻留的 MCP 服务，不读取或输出 token、端口、descriptor 或临时认证内容。
- 不在第一版增加 UI、导航、物理、音频、商业化等领域 Skill；这些能力以后通过同一结构独立扩展。
- 不把测试场景、验证日志、`docs/superpowers` 或任何具体业务项目内容提交到仓库。

## 套件结构

```text
skills/
├─ tuanjie-workflows/
│  ├─ SKILL.md
│  └─ agents/openai.yaml
├─ tuanjie-codely-bridge/
│  ├─ SKILL.md
│  ├─ agents/openai.yaml
│  └─ references/
│     ├─ codely-integration.md
│     ├─ setup-and-config.md
│     └─ connection-diagnostics.md
├─ tuanjie-editor-automation/
│  ├─ SKILL.md
│  ├─ agents/openai.yaml
│  └─ references/
│     ├─ object-workflows.md
│     ├─ script-compile-workflows.md
│     └─ failure-recovery.md
├─ tuanjie-package-management/
│  ├─ SKILL.md
│  ├─ agents/openai.yaml
│  └─ references/package-workflows.md
└─ tuanjie-codely-custom-tools/
   ├─ SKILL.md
   ├─ agents/openai.yaml
   └─ references/custom-tool-contract.md
```

每个目录都是完整 Skill，不依赖另一个 Skill 的相对文件路径。入口可以委派给已安装的专项 Skill；专项 Skill 自身仍包含必要的入口闸门和安全边界，因此用户只安装某一个专项 Skill 时不会失去安全约束。

## Skill 职责

### `tuanjie-workflows`

这是套件入口和路由器，不重复专项命令。

它先识别：

- 当前工作区是否包含 `Assets`、`Packages`、`ProjectSettings`；
- `ProjectVersion.txt` 是否存在团结版本字段；
- 当前 Editor 是否为团结 Editor；
- 当前任务是否需要 Editor 对象语义。

然后按任务路由：

| 用户意图 | 目标 Skill |
|---|---|
| 安装 Skill、配置 MCP、定位 CodelyCLI、诊断连接 | `tuanjie-codely-bridge` |
| Scene、Prefab、GameObject、组件、资源、脚本编译 | `tuanjie-editor-automation` |
| 查询、安装、升级或移除 UPM 包 | `tuanjie-package-management` |
| 创建、注册、发现或调用 Bridge 自定义工具 | `tuanjie-codely-custom-tools` |
| 只修改普通代码、配置或文档 | 文件级工具，不强制调用 MCP |
| Unity 官方 Editor | 停止 Tuanjie 路由，交由适用的 Unity 工作流 |

如果目标专项 Skill 未安装，入口应报告准确名称和安装来源，不自行伪造专项流程。

### `tuanjie-codely-bridge`

现有 Skill 保留名称，但收窄为连接层：

- Codex 与 Codely Skills 的安装、生效和刷新规则；
- 团结项目识别、CodelyCLI 绝对路径定位与版本验证；
- 项目级 `.codex/config.toml` 的安全合并和备份规则；
- Editor、Bridge、MCP 注册、项目根一致性的分层诊断；
- 只读连接验收和连接失败报告。

它不再描述 Scene/Prefab 修改细节，也不把自定义工具的开发流程混入连接诊断。

### `tuanjie-editor-automation`

负责 Editor 对象语义和代码到 Editor 的闭环：

- Scene、Prefab、GameObject、组件与序列化引用；
- ScriptableObject、材质、Importer 和其他资源；
- C# 文件写入后的资源刷新、编译、Domain Reload 和 Console 检查；
- 保存 Scene、Prefab、Asset 后重新读取；
- 检查重复对象、重复组件、丢失引用和部分成功；
- 幂等重试一次，以及破坏性操作禁止盲重试。

统一写入契约：

```text
核对项目根和 Editor 状态
→ 读取目标对象、引用和脏状态
→ 执行一个最小动作
→ 重新读取并检查重复和引用
→ 按需保存
→ 再次读取保存后的状态
```

如果当前会话没有满足任务所需的真实 MCP 工具，Skill 只能保留安全完成的文件修改，并明确 Editor 验证未完成。

### `tuanjie-package-management`

负责团结 Package Manager 工作流：

- 读取 `Packages/manifest.json` 和当前解析状态；
- 优先使用当前 Editor/Bridge 实际暴露的包管理能力；
- 安装、升级、移除前确认准确包名、来源和目标版本；
- 等待包解析、资源导入、编译和 Domain Reload；
- 重新读取 manifest 与解析结果，报告请求版本和实际解析版本。

默认不手工编辑 `manifest.json`。只有用户明确授权、当前 Editor 包管理能力不可用且安全合并方案已确定时，才允许备份后修改；不得手工修改 `packages-lock.json`。

### `tuanjie-codely-custom-tools`

负责 Codely Bridge 项目级扩展：

- 先检查当前 Bridge 版本和官方/本地 API 事实；
- 定义单一职责的静态工具入口、输入结构、返回结构和错误语义；
- 将自定义工具放入项目现有 Editor 代码边界；
- 等待编译和 Bridge 重新发现；
- 以当前 `tuanjie` MCP schema 是否出现准确工具名和参数作为注册成功证明；
- 调用后仍执行读取、最小动作、重读、保存和再读验证。

第一版不在仓库中固化未经当前 Bridge 版本验证的 C# API 模板。参考文件会给出生成模板前必须核对的 API 清单；实现阶段只有在官方文档与本地 Bridge 程序集能够确认一致签名时，才加入可复制资源。

## 共用入口闸门

所有会触达 Editor 的专项 Skill 都必须独立执行以下检查：

1. 规范化当前工作区绝对路径。
2. 验证团结项目标识和团结 Editor 类型/版本。
3. 确认 Codely Bridge 与当前会话的 `tuanjie` MCP 实际存在。
4. 比较 MCP 报告的项目根和当前工作区。
5. 从当前 schema 选择工具及参数，禁止根据 Skill 文本猜测能力。
6. Editor 正在导入、编译、Domain Reload、保存或切换 Play Mode 时先等待稳定。

任何一项不满足时，不执行对象写入。Unity 官方项目立即退出 Tuanjie 路由。

## 失败与恢复

- 调用失败后先读取状态，判断是否已经部分成功。
- 只对幂等操作以完全相同的目标和参数重试一次。
- 删除、覆盖、Apply Prefab、批量重建等操作在前一次结果未确认前不得重试。
- `tuanjie` MCP 连续失败时优先检查 CodelyCLI、Bridge、Editor 和项目路径，不默认切换到 Unity MCP。
- 编译失败时停止所有依赖新程序集或新类型的操作。
- 最终报告分别列出文件修改、编译、测试、Editor 验证、重试/回退和未完成项。

## 安装与发现

README 和设置指南需要同时提供两种方式：

- 推荐：安装全部五个 GitHub Skill 子路径，使入口路由可以完整委派。
- 精简：只安装一个专项 Skill；该 Skill 仍能独立安全运行，但不会获得其他领域能力。

普通用户仍不需要克隆仓库。Codex 使用 `skill-installer` 从 GitHub 子路径安装；Codely Skills 使用其当前官方支持的 Skill 目录安装方式。安装后必须按宿主规则 reload 或开启新会话，不得把文件复制成功等同于当前会话已加载。

## 文档与元数据

- README 增加 Skills 套件清单、任务示例、整套和单项安装方式。
- `docs/setup-guide.md` 的 Agent 接入提示改为安装整套 Skills，并保留 EditorWindow 与项目级配置流程。
- `docs/architecture.md` 增加 Skill 路由层，但不改变 Codex → CodelyCLI → Bridge → Tuanjie Editor 的连接链路。
- 每个 Skill 提供独立 `agents/openai.yaml`，名称、简介和默认提示只描述自身职责。
- Skill 正文保持短而可扫描；只有任务需要时才读取对应 reference。

## 验证策略

验证材料写入已忽略的 `artifacts/`，不提交测试目录。每个 Skill 至少覆盖一个正常场景和一个拒绝/失败场景：

| Skill | 正常场景 | 拒绝或失败场景 |
|---|---|---|
| `tuanjie-workflows` | Scene 任务路由到 Editor automation | Unity 官方项目停止 Tuanjie 路由 |
| `tuanjie-codely-bridge` | 定位 CLI、验证配置与项目根 | MCP 根指向其他项目时禁止写入 |
| `tuanjie-editor-automation` | 读取、修改、重读、保存闭环 | 编译错误时禁止附加新组件 |
| `tuanjie-package-management` | 包变更后验证实际解析版本 | 无授权时拒绝手改 manifest |
| `tuanjie-codely-custom-tools` | schema 中发现后调用并验证 | 文档提到但 schema 未暴露时停止 |

静态验证包括：

- `quick_validate.py` 检查每个 Skill 的 frontmatter、名称和描述；
- Markdown 相对链接与文件存在性；
- Skill 目录名和 frontmatter `name` 一致；
- 仓库中不含具体业务项目、凭据、端口或 descriptor 内容；
- README 和设置指南中的 Skill 清单与实际目录一致。

实际验收使用一个已打开的通用团结项目执行只读连接、项目根核对和工具发现。涉及写入的验收只做用户明确授权的最小、可回滚冒烟操作，不把验收项目内容写入仓库。

## 兼容与发布

- 现有 `tuanjie-codely-bridge` URL、Skill 名称和 EditorWindow UPM URL保持不变。
- 新增 Skill 不改变 EditorWindow 包名 `cn.qjx.codex-codely-setup`。
- 文档先列出套件，再说明原单 Skill 安装仍受支持。
- 实现按独立可审查单元提交：入口路由、连接 Skill 收窄、Editor automation、包管理、自定义工具、文档与整体验收。
- 完成验证后再推送，不提交本地测试产物和业务项目内容。

## 设计参考

- [Unity Technologies Skills 总览](https://github.com/Unity-Technologies/skills/blob/main/README.md)
- [New Unity Project：入口编排与专项委派](https://github.com/Unity-Technologies/skills/blob/main/skills/new-unity-project/SKILL.md)
- [Unity UI：路由 Skill](https://github.com/Unity-Technologies/skills/blob/main/skills/ui/SKILL.md)
- [Unity Package Management：独立包管理 Skill](https://github.com/Unity-Technologies/skills/blob/main/skills/unity-package-management/SKILL.md)
- [Codely Skills 官方文档](https://codely-docs.tuanjie.cn/features-introduction/skills-experimental/)
- [Codely Bridge 自定义官方文档](https://codely-docs.tuanjie.cn/using-codely/codely-bridge/)
