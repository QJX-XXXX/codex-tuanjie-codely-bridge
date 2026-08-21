# Visual and Runtime Verification

视觉证据用于补充对象和序列化验证，不替代它们。先重读组件、引用、Importer 和保存状态，再根据任务选择当前 schema 中真实存在的 `unity_screenshot` action。

## Codely 截图路由

| 结果类型 | 优先 action |
|---|---|
| 游戏实际画面 | `capture_game_view` 或当前统一 `capture` |
| Scene 编辑视图、Gizmo、线框 | `capture_scene_view`，按需使用 `render_mode` |
| 主相机/指定相机 | `capture_main_camera` / `capture_specific_camera` |
| 材质、Prefab、纹理等资源 | `capture_asset` |
| UI Toolkit 文档 | `capture_ui_toolkit` |
| 指定 Scene 相机视角 | `capture_scene_camera` |

action、`view`、尺寸、相机名、资源路径和返回方式以当前 schema 为准；不要复制其他 MCP 的工具名或未暴露参数。

## 验收闭环

- UI/布局：核对 RectTransform/UIDocument、引用和层级，再截 Game View/Scene View，检查裁切、遮挡、锚点和文本。
- 相机：核对相机对象、Transform、投影和目标引用，再截相机画面，检查构图和目标可见性。
- 材质/灯光：核对 shader、材质引用、Renderer 和渲染管线，再截资源或场景画面，检查实际显示。
- Prefab/资源：核对 Asset 路径、组件和 Override，再用资源预览或实例画面补充确认。

截图失败不等于对象修改失败；保留数据级验证，明确报告视觉验收未完成。截图成功也不能证明 Scene/Prefab 已保存。

## Console 错误边界

对视觉或运行时操作需要判断新错误时，使用当前 Codely `unity_console`：

```text
unity_console.clear
→ 执行动作
→ unity_console.get（error、warning、exception）
```

不要读取未清理的旧 Console 后猜测哪些消息属于本次操作。脚本编译由 `unity_editor.start_compilation_pipeline` 建立边界时，按编译 reference 直接读取后续 Console。
