# Script Compile Workflows

## 新增或修改脚本后

```text
写入 C# 文件
→ unity_editor.start_compilation_pipeline（当前 schema 存在时）
→ unity_console.get 读取该次编译的 error/warning/exception
→ 确认类型可用
→ 再执行对象操作
→ 重读组件、字段和引用
→ 保存并确认
```

当前 Codely schema 中，`start_compilation_pipeline` 会建立 Console 边界并在编译完成后返回；不要再调用不存在的 `wait_for_compile`，也不要为同步完成的编译创建轮询。若当前版本没有该 action，则使用实际状态能力等待资源刷新、编译和 Domain Reload 稳定，再建立清晰的 Console 边界。

文件存在不等于程序集已加载，`unity_script.validate` 也不能代替实际编译和 Console。Editor 正在导入或编译时，不要并发调用依赖新类型的 MCP 操作；出现本次编译错误时停止附加组件、创建依赖对象和其他依赖新程序集的动作，先报告错误位置和未完成项。

只读检查可以在编译前做，但不要把“脚本文件写入成功”报告为“组件已可用”。编译恢复后仍需重新读取目标 GameObject，确认没有重复组件、丢失引用或旧程序集状态。
