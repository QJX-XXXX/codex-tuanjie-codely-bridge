using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QJX.CodexTuanjieBridge.Editor
{
    internal static class TomlConfigurationPatcher
    {
        private const string TargetTable = "mcp_servers.tuanjie";
        private const string ProjectPathMarker = "--unity-project-path";

        private sealed class LineInfo
        {
            public int Start;
            public int End;
            public int NextStart;
        }

        private sealed class TableInfo
        {
            public int HeaderStart;
            public int ContentStart;
            public int End;
            public string Name;
            public bool IsArrayTable;
        }

        private sealed class StringToken
        {
            public int Start;
            public int End;
            public string Value;
        }

        public static TextPatchResult BuildPatch(
            string source,
            string codelyCliPath,
            string projectRoot)
        {
            string text = source ?? string.Empty;
            if (text.IndexOf("\"\"\"", StringComparison.Ordinal) >= 0 ||
                text.IndexOf("'''", StringComparison.Ordinal) >= 0)
            {
                return Failure("config.toml 包含多行字符串；当前版本无法证明 table 边界唯一，已拒绝修改。");
            }

            List<TableInfo> tables;
            string tableError;
            if (!TryReadTables(text, out tables, out tableError))
            {
                return Failure(tableError);
            }

            var targets = new List<TableInfo>();
            for (int index = 0; index < tables.Count; index++)
            {
                if (string.Equals(tables[index].Name, TargetTable, StringComparison.Ordinal))
                {
                    if (tables[index].IsArrayTable)
                    {
                        return Failure("检测到 [[mcp_servers.tuanjie]] 数组 table；只支持唯一的普通 table，已拒绝修改。");
                    }
                    targets.Add(tables[index]);
                }
            }
            if (targets.Count > 1)
            {
                return Failure("config.toml 包含重复的 [mcp_servers.tuanjie] table。");
            }
            if (targets.Count == 0)
            {
                string newline = DetectNewline(text);
                string separator = string.Empty;
                if (text.Length > 0)
                {
                    separator = EndsWithNewline(text)
                        ? newline
                        : newline + newline;
                }
                return new TextPatchResult
                {
                    Success = true,
                    CreatesServer = true,
                    Start = text.Length,
                    Length = 0,
                    Replacement = separator + BuildServerTable(
                        codelyCliPath,
                        projectRoot,
                        newline),
                    OriginalProjectPath = string.Empty,
                    Error = string.Empty
                };
            }

            TableInfo target = targets[0];
            int argsValueStart;
            string argsError;
            if (!TryFindArgsAssignment(
                    text,
                    target.ContentStart,
                    target.End,
                    out argsValueStart,
                    out argsError))
            {
                return Failure(argsError);
            }

            List<StringToken> arguments;
            int arrayEnd;
            string arrayError;
            if (!TryParseStringArray(
                    text,
                    argsValueStart,
                    target.End,
                    out arguments,
                    out arrayEnd,
                    out arrayError))
            {
                return Failure(arrayError);
            }

            int markerIndex = -1;
            for (int index = 0; index < arguments.Count; index++)
            {
                if (string.Equals(
                        arguments[index].Value,
                        ProjectPathMarker,
                        StringComparison.Ordinal))
                {
                    if (markerIndex >= 0)
                    {
                        return Failure("[mcp_servers.tuanjie].args 包含重复的 --unity-project-path 参数。");
                    }
                    markerIndex = index;
                }
            }
            if (markerIndex < 0)
            {
                return Failure("[mcp_servers.tuanjie].args 缺少 --unity-project-path 参数；只允许修改已有路径，已拒绝重写 table。");
            }
            if (markerIndex + 1 >= arguments.Count)
            {
                return Failure("--unity-project-path 后没有路径字符串。");
            }

            StringToken pathToken = arguments[markerIndex + 1];
            bool samePath = PathsEqual(pathToken.Value, projectRoot);
            return new TextPatchResult
            {
                Success = true,
                CreatesServer = false,
                Start = pathToken.Start,
                Length = pathToken.End - pathToken.Start,
                Replacement = samePath
                    ? text.Substring(pathToken.Start, pathToken.End - pathToken.Start)
                    : "\"" + EscapeTomlBasicString(projectRoot) + "\"",
                OriginalProjectPath = pathToken.Value,
                Error = string.Empty
            };
        }

        private static bool TryReadTables(
            string text,
            out List<TableInfo> tables,
            out string error)
        {
            tables = new List<TableInfo>();
            error = string.Empty;
            List<LineInfo> lines = ReadLines(text);
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                LineInfo line = lines[lineIndex];
                string name;
                bool isHeader;
                bool isArrayTable;
                string headerError;
                if (!TryParseTableHeader(
                        text,
                        line.Start,
                        line.End,
                        out isHeader,
                        out isArrayTable,
                        out name,
                        out headerError))
                {
                    error = headerError;
                    return false;
                }
                if (!isHeader)
                {
                    continue;
                }
                if (tables.Count > 0)
                {
                    tables[tables.Count - 1].End = line.Start;
                }
                tables.Add(new TableInfo
                {
                    HeaderStart = line.Start,
                    ContentStart = line.NextStart,
                    End = text.Length,
                    Name = name,
                    IsArrayTable = isArrayTable
                });
            }
            return true;
        }

        private static bool TryParseTableHeader(
            string text,
            int start,
            int end,
            out bool isHeader,
            out bool isArrayTable,
            out string name,
            out string error)
        {
            isHeader = false;
            isArrayTable = false;
            name = string.Empty;
            error = string.Empty;
            int index = start;
            SkipSpaces(text, ref index, end);
            if (index >= end || text[index] == '#')
            {
                return true;
            }
            if (text[index] != '[')
            {
                return true;
            }

            bool arrayTable = index + 1 < end && text[index + 1] == '[';
            isArrayTable = arrayTable;
            int openLength = arrayTable ? 2 : 1;
            int contentStart = index + openLength;
            int closeStart = FindHeaderClose(text, contentStart, end, arrayTable);
            if (closeStart < 0)
            {
                error = "config.toml 包含无法识别的 table header，已拒绝修改。";
                return false;
            }
            int afterClose = closeStart + openLength;
            SkipSpaces(text, ref afterClose, end);
            if (afterClose < end && text[afterClose] != '#')
            {
                error = "config.toml table header 后包含无法识别的内容。";
                return false;
            }

            string dottedName;
            if (!TryParseDottedKey(
                    text,
                    contentStart,
                    closeStart,
                    out dottedName))
            {
                error = "config.toml 包含无法安全解析的 table 名称。";
                return false;
            }
            isHeader = true;
            name = dottedName;
            return true;
        }

        private static int FindHeaderClose(
            string text,
            int start,
            int end,
            bool arrayTable)
        {
            bool inDouble = false;
            bool inSingle = false;
            bool escaped = false;
            for (int index = start; index < end; index++)
            {
                char current = text[index];
                if (inDouble)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == '"')
                    {
                        inDouble = false;
                    }
                    continue;
                }
                if (inSingle)
                {
                    if (current == '\'')
                    {
                        inSingle = false;
                    }
                    continue;
                }
                if (current == '"')
                {
                    inDouble = true;
                }
                else if (current == '\'')
                {
                    inSingle = true;
                }
                else if (current == ']')
                {
                    if (!arrayTable || (index + 1 < end && text[index + 1] == ']'))
                    {
                        return index;
                    }
                }
            }
            return -1;
        }

        private static bool TryParseDottedKey(
            string text,
            int start,
            int end,
            out string name)
        {
            name = string.Empty;
            var segments = new List<string>();
            int index = start;
            while (true)
            {
                SkipSpaces(text, ref index, end);
                string segment;
                if (!TryReadKeySegment(text, ref index, end, out segment))
                {
                    return false;
                }
                segments.Add(segment);
                SkipSpaces(text, ref index, end);
                if (index >= end)
                {
                    break;
                }
                if (text[index] != '.')
                {
                    return false;
                }
                index++;
            }
            name = string.Join(".", segments.ToArray());
            return segments.Count > 0;
        }

        private static bool TryReadKeySegment(
            string text,
            ref int index,
            int end,
            out string segment)
        {
            segment = string.Empty;
            if (index >= end)
            {
                return false;
            }
            if (text[index] == '"' || text[index] == '\'')
            {
                StringToken token;
                string stringError;
                if (!TryReadString(text, index, end, out token, out stringError))
                {
                    return false;
                }
                segment = token.Value;
                index = token.End;
                return true;
            }
            int start = index;
            while (index < end)
            {
                char current = text[index];
                if (!(char.IsLetterOrDigit(current) || current == '_' || current == '-'))
                {
                    break;
                }
                index++;
            }
            if (index == start)
            {
                return false;
            }
            segment = text.Substring(start, index - start);
            return true;
        }

        private static bool TryFindArgsAssignment(
            string text,
            int start,
            int end,
            out int valueStart,
            out string error)
        {
            valueStart = -1;
            error = string.Empty;
            int count = 0;
            List<LineInfo> lines = ReadLines(text, start, end);
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                LineInfo line = lines[lineIndex];
                int index = line.Start;
                SkipSpaces(text, ref index, line.End);
                if (index >= line.End || text[index] == '#')
                {
                    continue;
                }
                string key;
                int keyIndex = index;
                if (!TryReadKeySegment(text, ref keyIndex, line.End, out key))
                {
                    continue;
                }
                SkipSpaces(text, ref keyIndex, line.End);
                if (keyIndex >= line.End || text[keyIndex] != '=')
                {
                    continue;
                }
                if (!string.Equals(key, "args", StringComparison.Ordinal))
                {
                    continue;
                }
                count++;
                valueStart = keyIndex + 1;
            }
            if (count == 0)
            {
                error = "[mcp_servers.tuanjie] 缺少 args；为避免重写其他字段，已拒绝修改。";
                return false;
            }
            if (count > 1)
            {
                error = "[mcp_servers.tuanjie] 包含重复的 args 键。";
                return false;
            }
            return true;
        }

        private static bool TryParseStringArray(
            string text,
            int start,
            int limit,
            out List<StringToken> values,
            out int arrayEnd,
            out string error)
        {
            values = new List<StringToken>();
            arrayEnd = -1;
            error = string.Empty;
            int index = start;
            SkipTomlTrivia(text, ref index, limit);
            if (index >= limit || text[index] != '[')
            {
                error = "[mcp_servers.tuanjie].args 不是字符串数组。";
                return false;
            }
            index++;
            bool expectValue = true;
            while (index < limit)
            {
                SkipTomlTrivia(text, ref index, limit);
                if (index >= limit)
                {
                    break;
                }
                if (text[index] == ']')
                {
                    arrayEnd = index + 1;
                    return true;
                }
                if (!expectValue || (text[index] != '"' && text[index] != '\''))
                {
                    error = "[mcp_servers.tuanjie].args 必须只包含字符串。";
                    return false;
                }
                StringToken token;
                string stringError;
                if (!TryReadString(text, index, limit, out token, out stringError))
                {
                    error = stringError;
                    return false;
                }
                values.Add(token);
                index = token.End;
                SkipTomlTrivia(text, ref index, limit);
                if (index < limit && text[index] == ',')
                {
                    index++;
                    expectValue = true;
                    continue;
                }
                if (index < limit && text[index] == ']')
                {
                    arrayEnd = index + 1;
                    return true;
                }
                error = "[mcp_servers.tuanjie].args 的字符串之间缺少逗号。";
                return false;
            }
            error = "[mcp_servers.tuanjie].args 数组没有闭合。";
            return false;
        }

        private static bool TryReadString(
            string text,
            int start,
            int limit,
            out StringToken token,
            out string error)
        {
            token = null;
            error = string.Empty;
            if (start >= limit || (text[start] != '"' && text[start] != '\''))
            {
                error = "需要 TOML 字符串。";
                return false;
            }
            char quote = text[start];
            var builder = new StringBuilder();
            int index = start + 1;
            while (index < limit)
            {
                char current = text[index++];
                if (current == quote)
                {
                    token = new StringToken
                    {
                        Start = start,
                        End = index,
                        Value = builder.ToString()
                    };
                    return true;
                }
                if (current == '\r' || current == '\n')
                {
                    error = "TOML 单行字符串意外换行。";
                    return false;
                }
                if (quote == '\'' || current != '\\')
                {
                    builder.Append(current);
                    continue;
                }
                if (index >= limit)
                {
                    error = "TOML 字符串以不完整转义结束。";
                    return false;
                }
                char escaped = text[index++];
                switch (escaped)
                {
                    case 'b': builder.Append('\b'); break;
                    case 't': builder.Append('\t'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'r': builder.Append('\r'); break;
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case 'u':
                    case 'U':
                        int digits = escaped == 'u' ? 4 : 8;
                        int codePoint;
                        if (!TryReadHex(text, ref index, limit, digits, out codePoint))
                        {
                            error = "TOML 字符串包含无效 Unicode 转义。";
                            return false;
                        }
                        try
                        {
                            builder.Append(char.ConvertFromUtf32(codePoint));
                        }
                        catch
                        {
                            error = "TOML 字符串包含无效 Unicode 码点。";
                            return false;
                        }
                        break;
                    default:
                        error = "TOML 字符串包含不支持的转义：\\" + escaped;
                        return false;
                }
            }
            error = "TOML 字符串没有闭合。";
            return false;
        }

        private static bool TryReadHex(
            string text,
            ref int index,
            int limit,
            int digits,
            out int value)
        {
            value = 0;
            if (index + digits > limit)
            {
                return false;
            }
            for (int count = 0; count < digits; count++)
            {
                int hex = HexValue(text[index++]);
                if (hex < 0)
                {
                    return false;
                }
                value = (value << 4) | hex;
            }
            return true;
        }

        private static int HexValue(char value)
        {
            if (value >= '0' && value <= '9') return value - '0';
            if (value >= 'a' && value <= 'f') return value - 'a' + 10;
            if (value >= 'A' && value <= 'F') return value - 'A' + 10;
            return -1;
        }

        private static void SkipTomlTrivia(string text, ref int index, int limit)
        {
            while (index < limit)
            {
                if (char.IsWhiteSpace(text[index]))
                {
                    index++;
                    continue;
                }
                if (text[index] == '#')
                {
                    while (index < limit && text[index] != '\r' && text[index] != '\n')
                    {
                        index++;
                    }
                    continue;
                }
                break;
            }
        }

        private static List<LineInfo> ReadLines(string text)
        {
            return ReadLines(text, 0, text.Length);
        }

        private static List<LineInfo> ReadLines(string text, int start, int end)
        {
            var lines = new List<LineInfo>();
            int index = start;
            while (index < end)
            {
                int lineStart = index;
                while (index < end && text[index] != '\r' && text[index] != '\n')
                {
                    index++;
                }
                int lineEnd = index;
                if (index < end && text[index] == '\r') index++;
                if (index < end && text[index] == '\n') index++;
                lines.Add(new LineInfo
                {
                    Start = lineStart,
                    End = lineEnd,
                    NextStart = index
                });
            }
            if (start == end)
            {
                lines.Add(new LineInfo { Start = start, End = end, NextStart = end });
            }
            return lines;
        }

        private static void SkipSpaces(string text, ref int index, int end)
        {
            while (index < end && (text[index] == ' ' || text[index] == '\t'))
            {
                index++;
            }
        }

        private static string BuildServerTable(
            string codelyCliPath,
            string projectRoot,
            string newline)
        {
            string[] lines =
            {
                "[mcp_servers.tuanjie]",
                "command = \"cmd.exe\"",
                "args = [",
                "    \"/c\",",
                "    \"" + EscapeTomlBasicString(codelyCliPath) + "\",",
                "    \"serve\",",
                "    \"unity-mcp\",",
                "    \"--stdio\",",
                "    \"--unity-project-path\",",
                "    \"" + EscapeTomlBasicString(projectRoot) + "\"",
                "]",
                "startup_timeout_sec = 30",
                "tool_timeout_sec = 120",
                "enabled = true"
            };
            return string.Join(newline, lines) + newline;
        }

        private static string EscapeTomlBasicString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\b", "\\b")
                .Replace("\t", "\\t")
                .Replace("\n", "\\n")
                .Replace("\f", "\\f")
                .Replace("\r", "\\r");
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
