# Custom Tool Contract

## 当前可验证的 Bridge API

本 reference 的 C# 签名依据本机团结项目可读取的 `cn.tuanjie.codely.bridge@1.0.75/Editor/Bridge/Tools/ExecuteCustomTool.cs`。其他 Bridge 版本必须重新检查本地程序集/源码；版本无法确认时不要复制下面的接口。

在该版本中：

- attribute 类型是 `UnityTcp.Editor.Tools.ExecuteCustomTool.CustomToolAttribute`；
- attribute 构造参数是工具名和可选描述；
- 方法必须是 `public static object`；
- 方法必须只有一个 `Codely.Newtonsoft.Json.Linq.JObject` 参数；
- Bridge 扫描当前程序集中的 public static 方法并建立名称到方法的注册；
- `execute_custom_tool` 的外层参数需要准确的 `tool_name` 和对象形式的 `parameters`，但当前 MCP schema 仍是最终事实来源。

## 最小方法形状

仅在当前 Bridge 版本再次确认后，才可以按项目命名和输入规则改写这个形状：

```csharp
using System.Collections.Generic;
using Codely.Newtonsoft.Json.Linq;
using UnityTcp.Editor.Tools;

public static class ProjectCodelyTools
{
    [ExecuteCustomTool.CustomTool("project.validate_input", "Validate project-owned input")]
    public static object ValidateInput(JObject parameters)
    {
        var value = parameters?["value"]?.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["message"] = "value is required"
            };
        }

        return new Dictionary<string, object>
        {
            ["success"] = true,
            ["message"] = "Input accepted",
            ["data"] = new Dictionary<string, object> { ["value"] = value }
        };
    }
}
```

示例工具名只是方法形状示例，不代表当前项目已注册。调用前必须从当前 schema 重新确认名称、外层参数、返回字段和项目根；schema 没有它时禁止调用。

## 发现与调用检查

1. 等待编译和 Domain Reload，读取 Console，确认没有本次错误。
2. 重新列出当前 `tuanjie` MCP schema，确认 `execute_custom_tool` 或等价能力真实存在。
3. 用当前 schema 中准确的工具名和参数做一次最小调用。
4. 读取返回的 `success`、`message`、`data` 或 schema 定义的实际字段；失败时先判断是否有副作用。
5. 如果工具修改 Editor 对象，重新读取对象、引用、重复状态和脏状态，按需保存后再读。

不要读取或输出端口、descriptor、token、临时认证信息。工具注册成功不能代替具体调用和对象保存验证。
