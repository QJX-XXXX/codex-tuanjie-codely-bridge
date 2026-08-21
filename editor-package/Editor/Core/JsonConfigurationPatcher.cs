using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QJX.CodexTuanjieBridge.Editor
{
    internal static class JsonConfigurationPatcher
    {
        private const string ProjectPathMarker = "--unity-project-path";

        private enum JsonNodeKind
        {
            Object,
            Array,
            String,
            Primitive
        }

        private sealed class JsonProperty
        {
            public string Name;
            public int NameStart;
            public JsonNode Value;
        }

        private sealed class JsonNode
        {
            public JsonNodeKind Kind;
            public int Start;
            public int End;
            public int CloseStart;
            public bool HasTrailingComma;
            public string StringValue;
            public List<JsonProperty> Properties;
            public List<JsonNode> Elements;
        }

        private sealed class JsonTextParser
        {
            private readonly string _text;
            private int _index;

            public JsonTextParser(string text)
            {
                _text = text ?? string.Empty;
            }

            public bool TryParseDocument(
                out JsonNode root,
                out bool containsOnlyTrivia,
                out string error)
            {
                root = null;
                containsOnlyTrivia = false;
                error = string.Empty;
                if (!SkipTrivia(out error))
                {
                    return false;
                }
                if (_index >= _text.Length)
                {
                    containsOnlyTrivia = true;
                    return true;
                }
                if (!TryParseValue(out root, out error))
                {
                    return false;
                }
                if (!SkipTrivia(out error))
                {
                    return false;
                }
                if (_index != _text.Length)
                {
                    error = "JSON/JSONC 根对象后包含额外内容。";
                    return false;
                }
                return true;
            }

            private bool TryParseValue(out JsonNode node, out string error)
            {
                node = null;
                error = string.Empty;
                if (!SkipTrivia(out error))
                {
                    return false;
                }
                if (_index >= _text.Length)
                {
                    error = "JSON/JSONC 值意外结束。";
                    return false;
                }
                char current = _text[_index];
                if (current == '{') return TryParseObject(out node, out error);
                if (current == '[') return TryParseArray(out node, out error);
                if (current == '"') return TryParseStringNode(out node, out error);
                return TryParsePrimitive(out node, out error);
            }

            private bool TryParseObject(out JsonNode node, out string error)
            {
                int start = _index++;
                node = new JsonNode
                {
                    Kind = JsonNodeKind.Object,
                    Start = start,
                    Properties = new List<JsonProperty>(),
                    Elements = null,
                    CloseStart = -1
                };
                error = string.Empty;
                if (!SkipTrivia(out error)) return false;
                if (_index < _text.Length && _text[_index] == '}')
                {
                    node.CloseStart = _index;
                    node.End = ++_index;
                    return true;
                }

                while (_index < _text.Length)
                {
                    JsonNode nameNode;
                    if (!TryParseStringNode(out nameNode, out error)) return false;
                    if (!SkipTrivia(out error)) return false;
                    if (_index >= _text.Length || _text[_index] != ':')
                    {
                        error = "JSON/JSONC 对象属性后缺少冒号。";
                        return false;
                    }
                    _index++;
                    JsonNode value;
                    if (!TryParseValue(out value, out error)) return false;
                    node.Properties.Add(new JsonProperty
                    {
                        Name = nameNode.StringValue,
                        NameStart = nameNode.Start,
                        Value = value
                    });
                    if (!SkipTrivia(out error)) return false;
                    if (_index >= _text.Length)
                    {
                        error = "JSON/JSONC 对象没有闭合。";
                        return false;
                    }
                    if (_text[_index] == '}')
                    {
                        node.CloseStart = _index;
                        node.End = ++_index;
                        return true;
                    }
                    if (_text[_index] != ',')
                    {
                        error = "JSON/JSONC 对象属性之间缺少逗号。";
                        return false;
                    }
                    _index++;
                    if (!SkipTrivia(out error)) return false;
                    if (_index < _text.Length && _text[_index] == '}')
                    {
                        node.HasTrailingComma = true;
                        node.CloseStart = _index;
                        node.End = ++_index;
                        return true;
                    }
                }
                error = "JSON/JSONC 对象没有闭合。";
                return false;
            }

            private bool TryParseArray(out JsonNode node, out string error)
            {
                int start = _index++;
                node = new JsonNode
                {
                    Kind = JsonNodeKind.Array,
                    Start = start,
                    Properties = null,
                    Elements = new List<JsonNode>(),
                    CloseStart = -1
                };
                error = string.Empty;
                if (!SkipTrivia(out error)) return false;
                if (_index < _text.Length && _text[_index] == ']')
                {
                    node.CloseStart = _index;
                    node.End = ++_index;
                    return true;
                }

                while (_index < _text.Length)
                {
                    JsonNode value;
                    if (!TryParseValue(out value, out error)) return false;
                    node.Elements.Add(value);
                    if (!SkipTrivia(out error)) return false;
                    if (_index >= _text.Length)
                    {
                        error = "JSON/JSONC 数组没有闭合。";
                        return false;
                    }
                    if (_text[_index] == ']')
                    {
                        node.CloseStart = _index;
                        node.End = ++_index;
                        return true;
                    }
                    if (_text[_index] != ',')
                    {
                        error = "JSON/JSONC 数组元素之间缺少逗号。";
                        return false;
                    }
                    _index++;
                    if (!SkipTrivia(out error)) return false;
                    if (_index < _text.Length && _text[_index] == ']')
                    {
                        node.HasTrailingComma = true;
                        node.CloseStart = _index;
                        node.End = ++_index;
                        return true;
                    }
                }
                error = "JSON/JSONC 数组没有闭合。";
                return false;
            }

            private bool TryParseStringNode(out JsonNode node, out string error)
            {
                node = null;
                error = string.Empty;
                if (_index >= _text.Length || _text[_index] != '"')
                {
                    error = "JSON/JSONC 对象键或字符串必须使用双引号。";
                    return false;
                }
                int start = _index++;
                var builder = new StringBuilder();
                while (_index < _text.Length)
                {
                    char current = _text[_index++];
                    if (current == '"')
                    {
                        node = new JsonNode
                        {
                            Kind = JsonNodeKind.String,
                            Start = start,
                            End = _index,
                            CloseStart = _index - 1,
                            StringValue = builder.ToString()
                        };
                        return true;
                    }
                    if (current < 0x20)
                    {
                        error = "JSON/JSONC 字符串包含未转义控制字符。";
                        return false;
                    }
                    if (current != '\\')
                    {
                        builder.Append(current);
                        continue;
                    }
                    if (_index >= _text.Length)
                    {
                        error = "JSON/JSONC 字符串以不完整转义结束。";
                        return false;
                    }
                    char escaped = _text[_index++];
                    switch (escaped)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            int value;
                            if (!TryReadUnicodeEscape(out value))
                            {
                                error = "JSON/JSONC 字符串包含无效 Unicode 转义。";
                                return false;
                            }
                            builder.Append((char)value);
                            break;
                        default:
                            error = "JSON/JSONC 字符串包含无效转义：\\" + escaped;
                            return false;
                    }
                }
                error = "JSON/JSONC 字符串没有闭合。";
                return false;
            }

            private bool TryReadUnicodeEscape(out int value)
            {
                value = 0;
                if (_index + 4 > _text.Length) return false;
                for (int count = 0; count < 4; count++)
                {
                    int hex = HexValue(_text[_index++]);
                    if (hex < 0) return false;
                    value = (value << 4) | hex;
                }
                return true;
            }

            private bool TryParsePrimitive(out JsonNode node, out string error)
            {
                int start = _index;
                while (_index < _text.Length)
                {
                    char current = _text[_index];
                    if (char.IsWhiteSpace(current) || current == ',' ||
                        current == '}' || current == ']')
                    {
                        break;
                    }
                    if (current == '/' && _index + 1 < _text.Length &&
                        (_text[_index + 1] == '/' || _text[_index + 1] == '*'))
                    {
                        break;
                    }
                    _index++;
                }
                if (_index == start)
                {
                    node = null;
                    error = "JSON/JSONC 包含无法识别的值。";
                    return false;
                }
                string token = _text.Substring(start, _index - start);
                if (!IsValidPrimitive(token))
                {
                    node = null;
                    error = "JSON/JSONC 包含无效的原始值：" + token;
                    return false;
                }
                node = new JsonNode
                {
                    Kind = JsonNodeKind.Primitive,
                    Start = start,
                    End = _index,
                    CloseStart = _index
                };
                error = string.Empty;
                return true;
            }

            private static bool IsValidPrimitive(string token)
            {
                if (string.Equals(token, "true", StringComparison.Ordinal) ||
                    string.Equals(token, "false", StringComparison.Ordinal) ||
                    string.Equals(token, "null", StringComparison.Ordinal))
                {
                    return true;
                }
                int index = 0;
                if (index < token.Length && token[index] == '-') index++;
                if (index >= token.Length) return false;
                if (token[index] == '0')
                {
                    index++;
                }
                else if (token[index] >= '1' && token[index] <= '9')
                {
                    while (index < token.Length && char.IsDigit(token[index])) index++;
                }
                else
                {
                    return false;
                }
                if (index < token.Length && token[index] == '.')
                {
                    index++;
                    int fractionStart = index;
                    while (index < token.Length && char.IsDigit(token[index])) index++;
                    if (index == fractionStart) return false;
                }
                if (index < token.Length && (token[index] == 'e' || token[index] == 'E'))
                {
                    index++;
                    if (index < token.Length && (token[index] == '+' || token[index] == '-')) index++;
                    int exponentStart = index;
                    while (index < token.Length && char.IsDigit(token[index])) index++;
                    if (index == exponentStart) return false;
                }
                return index == token.Length;
            }

            private bool SkipTrivia(out string error)
            {
                error = string.Empty;
                while (_index < _text.Length)
                {
                    if (char.IsWhiteSpace(_text[_index]))
                    {
                        _index++;
                        continue;
                    }
                    if (_text[_index] != '/' || _index + 1 >= _text.Length)
                    {
                        return true;
                    }
                    if (_text[_index + 1] == '/')
                    {
                        _index += 2;
                        while (_index < _text.Length &&
                               _text[_index] != '\r' && _text[_index] != '\n')
                        {
                            _index++;
                        }
                        continue;
                    }
                    if (_text[_index + 1] == '*')
                    {
                        int close = _text.IndexOf("*/", _index + 2, StringComparison.Ordinal);
                        if (close < 0)
                        {
                            error = "JSON/JSONC 块注释没有闭合。";
                            return false;
                        }
                        _index = close + 2;
                        continue;
                    }
                    return true;
                }
                return true;
            }
        }

        public static TextPatchResult BuildPatch(
            string source,
            string[] objectPath,
            int projectPathSegmentIndex,
            string codelyCliPath,
            string projectRoot)
        {
            string text = source ?? string.Empty;
            if (objectPath == null || objectPath.Length < 2)
            {
                return Failure("JSON MCP 配置路径无效。");
            }

            var parser = new JsonTextParser(text);
            JsonNode root;
            bool onlyTrivia;
            string parseError;
            if (!parser.TryParseDocument(out root, out onlyTrivia, out parseError))
            {
                return Failure("配置文件不是可安全处理的 JSON/JSONC：" + parseError);
            }
            if (onlyTrivia)
            {
                string newline = DetectNewline(text);
                string prefix = text.Length == 0 || EndsWithNewline(text)
                    ? string.Empty
                    : newline;
                return new TextPatchResult
                {
                    Success = true,
                    CreatesServer = true,
                    Start = text.Length,
                    Length = 0,
                    Replacement = prefix + BuildRootObject(
                        objectPath,
                        codelyCliPath,
                        projectRoot,
                        newline),
                    OriginalProjectPath = string.Empty,
                    Error = string.Empty
                };
            }
            if (root == null || root.Kind != JsonNodeKind.Object)
            {
                return Failure("JSON/JSONC 根值必须是对象。");
            }

            JsonNode current = root;
            for (int segmentIndex = 0; segmentIndex < objectPath.Length; segmentIndex++)
            {
                if (current.Kind != JsonNodeKind.Object)
                {
                    return Failure("JSON 路径 " + JoinPath(objectPath, segmentIndex) + " 不是对象。");
                }

                List<JsonProperty> matches = FindProperties(
                    current,
                    objectPath[segmentIndex],
                    segmentIndex == projectPathSegmentIndex);
                if (matches.Count > 1)
                {
                    return Failure("JSON 配置包含重复或等价的键：" + objectPath[segmentIndex]);
                }
                if (matches.Count == 0)
                {
                    string newline = DetectNewline(text);
                    string insertion = BuildObjectInsertion(
                        text,
                        current,
                        objectPath,
                        segmentIndex,
                        codelyCliPath,
                        projectRoot,
                        newline);
                    return new TextPatchResult
                    {
                        Success = true,
                        CreatesServer = true,
                        Start = current.CloseStart,
                        Length = 0,
                        Replacement = insertion,
                        OriginalProjectPath = string.Empty,
                        Error = string.Empty
                    };
                }

                JsonNode value = matches[0].Value;
                if (segmentIndex == objectPath.Length - 1)
                {
                    return BuildExistingServerPatch(text, value, projectRoot);
                }
                current = value;
            }
            return Failure("无法定位 JSON MCP 配置。");
        }

        private static TextPatchResult BuildExistingServerPatch(
            string text,
            JsonNode server,
            string projectRoot)
        {
            if (server == null || server.Kind != JsonNodeKind.Object)
            {
                return Failure("mcpServers.tuanjie 不是对象。");
            }
            List<JsonProperty> argsProperties = FindProperties(server, "args", false);
            if (argsProperties.Count == 0)
            {
                return Failure("mcpServers.tuanjie 缺少 args；只允许修改已有路径，已拒绝重写 server。");
            }
            if (argsProperties.Count > 1)
            {
                return Failure("mcpServers.tuanjie 包含重复的 args 键。");
            }
            JsonNode args = argsProperties[0].Value;
            if (args == null || args.Kind != JsonNodeKind.Array)
            {
                return Failure("mcpServers.tuanjie.args 不是字符串数组。");
            }
            for (int index = 0; index < args.Elements.Count; index++)
            {
                if (args.Elements[index].Kind != JsonNodeKind.String)
                {
                    return Failure("mcpServers.tuanjie.args 必须只包含字符串。");
                }
            }

            int markerIndex = -1;
            for (int index = 0; index < args.Elements.Count; index++)
            {
                if (string.Equals(
                        args.Elements[index].StringValue,
                        ProjectPathMarker,
                        StringComparison.Ordinal))
                {
                    if (markerIndex >= 0)
                    {
                        return Failure("mcpServers.tuanjie.args 包含重复的 --unity-project-path 参数。");
                    }
                    markerIndex = index;
                }
            }
            if (markerIndex < 0)
            {
                return Failure("mcpServers.tuanjie.args 缺少 --unity-project-path 参数。");
            }
            if (markerIndex + 1 >= args.Elements.Count)
            {
                return Failure("--unity-project-path 后没有路径字符串。");
            }

            JsonNode pathNode = args.Elements[markerIndex + 1];
            bool samePath = PathsEqual(pathNode.StringValue, projectRoot);
            return new TextPatchResult
            {
                Success = true,
                CreatesServer = false,
                Start = pathNode.Start,
                Length = pathNode.End - pathNode.Start,
                Replacement = samePath
                    ? text.Substring(pathNode.Start, pathNode.End - pathNode.Start)
                    : QuoteJson(projectRoot),
                OriginalProjectPath = pathNode.StringValue,
                Error = string.Empty
            };
        }

        private static List<JsonProperty> FindProperties(
            JsonNode node,
            string name,
            bool compareAsPath)
        {
            var matches = new List<JsonProperty>();
            if (node == null || node.Properties == null)
            {
                return matches;
            }
            for (int index = 0; index < node.Properties.Count; index++)
            {
                string candidate = node.Properties[index].Name;
                bool match = compareAsPath
                    ? PathsEqual(candidate, name)
                    : string.Equals(candidate, name, StringComparison.Ordinal);
                if (match)
                {
                    matches.Add(node.Properties[index]);
                }
            }
            return matches;
        }

        private static string BuildObjectInsertion(
            string text,
            JsonNode parent,
            string[] path,
            int missingIndex,
            string codelyCliPath,
            string projectRoot,
            string newline)
        {
            string parentIndent = GetClosingIndent(text, parent.CloseStart);
            string childIndent = DetectChildIndent(text, parent, parentIndent);
            string property = QuoteJson(path[missingIndex]) + ": " +
                              BuildNestedValue(
                                  path,
                                  missingIndex + 1,
                                  childIndent,
                                  codelyCliPath,
                                  projectRoot,
                                  newline);
            string prefix;
            if (parent.Properties.Count == 0)
            {
                prefix = newline;
            }
            else
            {
                prefix = parent.HasTrailingComma ? newline : "," + newline;
            }
            return prefix + childIndent + property + newline + parentIndent;
        }

        private static string BuildRootObject(
            string[] path,
            string codelyCliPath,
            string projectRoot,
            string newline)
        {
            string indent = "  ";
            return "{" + newline + indent + QuoteJson(path[0]) + ": " +
                   BuildNestedValue(
                       path,
                       1,
                       indent,
                       codelyCliPath,
                       projectRoot,
                       newline) +
                   newline + "}" + newline;
        }

        private static string BuildNestedValue(
            string[] path,
            int nextSegment,
            string propertyIndent,
            string codelyCliPath,
            string projectRoot,
            string newline)
        {
            if (nextSegment >= path.Length)
            {
                return BuildServerObject(
                    propertyIndent,
                    codelyCliPath,
                    projectRoot,
                    newline);
            }
            string childIndent = propertyIndent + "  ";
            return "{" + newline + childIndent + QuoteJson(path[nextSegment]) + ": " +
                   BuildNestedValue(
                       path,
                       nextSegment + 1,
                       childIndent,
                       codelyCliPath,
                       projectRoot,
                       newline) +
                   newline + propertyIndent + "}";
        }

        private static string BuildServerObject(
            string propertyIndent,
            string codelyCliPath,
            string projectRoot,
            string newline)
        {
            string fieldIndent = propertyIndent + "  ";
            string argumentIndent = fieldIndent + "  ";
            var builder = new StringBuilder();
            builder.Append("{").Append(newline);
            builder.Append(fieldIndent).Append("\"command\": \"cmd.exe\",").Append(newline);
            builder.Append(fieldIndent).Append("\"args\": [").Append(newline);
            string[] arguments =
            {
                "/c",
                codelyCliPath,
                "serve",
                "unity-mcp",
                "--stdio",
                ProjectPathMarker,
                projectRoot
            };
            for (int index = 0; index < arguments.Length; index++)
            {
                builder.Append(argumentIndent).Append(QuoteJson(arguments[index]));
                if (index + 1 < arguments.Length) builder.Append(',');
                builder.Append(newline);
            }
            builder.Append(fieldIndent).Append("]").Append(newline);
            builder.Append(propertyIndent).Append("}");
            return builder.ToString();
        }

        private static string DetectChildIndent(
            string text,
            JsonNode parent,
            string parentIndent)
        {
            if (parent.Properties != null && parent.Properties.Count > 0)
            {
                int nameStart = parent.Properties[0].NameStart;
                int lineStart = FindLineStart(text, nameStart);
                string indent = text.Substring(lineStart, nameStart - lineStart);
                if (IsIndent(indent)) return indent;
            }
            return parentIndent + "  ";
        }

        private static string GetClosingIndent(string text, int closeStart)
        {
            int lineStart = FindLineStart(text, closeStart);
            string indent = text.Substring(lineStart, closeStart - lineStart);
            return IsIndent(indent) ? indent : string.Empty;
        }

        private static int FindLineStart(string text, int index)
        {
            int lineBreak = text.LastIndexOf('\n', Math.Max(0, index - 1));
            return lineBreak < 0 ? 0 : lineBreak + 1;
        }

        private static bool IsIndent(string text)
        {
            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] != ' ' && text[index] != '\t') return false;
            }
            return true;
        }

        private static string QuoteJson(string value)
        {
            var builder = new StringBuilder();
            builder.Append('"');
            string source = value ?? string.Empty;
            for (int index = 0; index < source.Length; index++)
            {
                char current = source[index];
                switch (current)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (current < 0x20)
                        {
                            builder.Append("\\u").Append(((int)current).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(current);
                        }
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }

        private static bool PathsEqual(string left, string right)
        {
            try
            {
                string normalizedLeft = Path.GetFullPath(left ?? string.Empty)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string normalizedRight = Path.GetFullPath(right ?? string.Empty)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(
                    normalizedLeft,
                    normalizedRight,
                    Path.DirectorySeparatorChar == '\\'
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static int HexValue(char value)
        {
            if (value >= '0' && value <= '9') return value - '0';
            if (value >= 'a' && value <= 'f') return value - 'a' + 10;
            if (value >= 'A' && value <= 'F') return value - 'A' + 10;
            return -1;
        }

        private static string JoinPath(string[] path, int inclusiveEnd)
        {
            var builder = new StringBuilder();
            for (int index = 0; index <= inclusiveEnd && index < path.Length; index++)
            {
                if (index > 0) builder.Append('.');
                builder.Append(path[index]);
            }
            return builder.ToString();
        }

        private static string DetectNewline(string text)
        {
            return text.IndexOf("\r\n", StringComparison.Ordinal) >= 0
                ? "\r\n"
                : "\n";
        }

        private static bool EndsWithNewline(string text)
        {
            return text.EndsWith("\n", StringComparison.Ordinal) ||
                   text.EndsWith("\r", StringComparison.Ordinal);
        }

        private static TextPatchResult Failure(string error)
        {
            return new TextPatchResult
            {
                Success = false,
                CreatesServer = false,
                Start = 0,
                Length = 0,
                Replacement = string.Empty,
                OriginalProjectPath = string.Empty,
                Error = error
            };
        }
    }
}
