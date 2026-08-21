using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace QJX.CodexTuanjieBridge.Editor
{
    public sealed class CodexConfigMergeResult
    {
        public bool Success { get; set; }
        public bool Changed { get; set; }
        public string Content { get; set; }
        public string Error { get; set; }
    }

    public sealed class CodexConfigWriteResult
    {
        public bool Success { get; set; }
        public bool Changed { get; set; }
        public string ConfigPath { get; set; }
        public string BackupPath { get; set; }
        public string Error { get; set; }
    }

    public static class CodexConfigEditor
    {
        private const string TargetName = "mcp_servers.tuanjie";
        private const string TargetHeaderPattern =
            @"^\s*\[{1,2}mcp_servers\.tuanjie\]{1,2}\s*(?:#.*)?$";
        private const string TableHeaderPattern =
            @"^\s*\[{1,2}[^\]\r\n]+\]{1,2}\s*(?:#.*)?$";

        public static string BuildServerSection(string codelyCliPath, string projectRoot)
        {
            if (string.IsNullOrEmpty(codelyCliPath))
            {
                throw new ArgumentException("CodelyCLI 路径不能为空。", "codelyCliPath");
            }
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new ArgumentException("团结项目路径不能为空。", "projectRoot");
            }

            string newline = Environment.NewLine;
            string cli = EscapeToml(codelyCliPath);
            string project = EscapeToml(projectRoot);
            string[] lines =
            {
                "[mcp_servers.tuanjie]",
                "command = \"cmd.exe\"",
                "args = [",
                "    \"/c\",",
                "    \"" + cli + "\",",
                "    \"serve\",",
                "    \"unity-mcp\",",
                "    \"--stdio\",",
                "    \"--unity-project-path\",",
                "    \"" + project + "\"",
                "]",
                "startup_timeout_sec = 30",
                "tool_timeout_sec = 120",
                "enabled = true"
            };
            return string.Join(newline, lines) + newline;
        }

        public static CodexConfigMergeResult Merge(string original, string desiredSection)
        {
            string source = original ?? string.Empty;
            if (desiredSection == null)
            {
                return Failure(source, "目标 Codely MCP 配置不能为空。");
            }

            string newline = DetectNewline(source);
            string section = NormalizeNewlines(desiredSection, newline).TrimEnd('\r', '\n');
            string[] lines = Regex.Split(source, @"\r\n|\n|\r");
            var targets = new List<int>();

            for (int index = 0; index < lines.Length; index++)
            {
                if (Regex.IsMatch(lines[index], TargetHeaderPattern))
                {
                    targets.Add(index);
                }
                else if (LooksLikeIncompleteTarget(lines[index]))
                {
                    return Failure(source, "检测到不完整的 [mcp_servers.tuanjie] table 边界。");
                }
            }

            if (targets.Count > 1)
            {
                return Failure(source, "config.toml 包含重复的 [mcp_servers.tuanjie] table。");
            }

            string content;
            string[] sectionLines = Regex.Split(section, Regex.Escape(newline));
            if (targets.Count == 0)
            {
                string sectionWithNewline = string.Join(newline, sectionLines) + newline;
                if (source.Length == 0)
                {
                    content = sectionWithNewline;
                }
                else if (source.EndsWith(newline, StringComparison.Ordinal))
                {
                    content = source + sectionWithNewline;
                }
                else
                {
                    content = source + newline + sectionWithNewline;
                }
            }
            else
            {
                int start = targets[0];
                int end = lines.Length;
                for (int index = start + 1; index < lines.Length; index++)
                {
                    if (Regex.IsMatch(lines[index], TableHeaderPattern))
                    {
                        end = index;
                        break;
                    }
                }

                var output = new List<string>();
                for (int index = 0; index < start; index++)
                {
                    output.Add(lines[index]);
                }
                for (int index = 0; index < sectionLines.Length; index++)
                {
                    output.Add(sectionLines[index]);
                }
                for (int index = end; index < lines.Length; index++)
                {
                    output.Add(lines[index]);
                }
                content = string.Join(newline, output.ToArray());
                if (source.EndsWith(newline, StringComparison.Ordinal) &&
                    !content.EndsWith(newline, StringComparison.Ordinal))
                {
                    content += newline;
                }
            }

            return new CodexConfigMergeResult
            {
                Success = true,
                Changed = !string.Equals(content, source, StringComparison.Ordinal),
                Content = content,
                Error = string.Empty
            };
        }

        public static CodexConfigWriteResult Write(
            string configPath,
            string content,
            bool createBackup)
        {
            string absolutePath = string.Empty;
            string tempPath = string.Empty;
            string backupPath = null;
            try
            {
                if (string.IsNullOrEmpty(configPath))
                {
                    throw new ArgumentException("config.toml 路径不能为空。", "configPath");
                }
                if (content == null)
                {
                    throw new ArgumentNullException("content");
                }

                absolutePath = Path.GetFullPath(configPath);
                string directory = Path.GetDirectoryName(absolutePath);
                if (string.IsNullOrEmpty(directory))
                {
                    throw new IOException("无法确定 config.toml 所在目录。");
                }
                Directory.CreateDirectory(directory);
                tempPath = absolutePath + ".tmp";
                backupPath = createBackup && File.Exists(absolutePath)
                    ? absolutePath + ".bak"
                    : null;

                if (File.Exists(absolutePath) &&
                    string.Equals(
                        File.ReadAllText(absolutePath, Encoding.UTF8),
                        content,
                        StringComparison.Ordinal))
                {
                    return new CodexConfigWriteResult
                    {
                        Success = true,
                        Changed = false,
                        ConfigPath = absolutePath,
                        BackupPath = null,
                        Error = string.Empty
                    };
                }

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                File.WriteAllText(tempPath, content, new UTF8Encoding(false));

                if (File.Exists(absolutePath))
                {
                    if (backupPath != null && File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Replace(tempPath, absolutePath, backupPath);
                }
                else
                {
                    File.Move(tempPath, absolutePath);
                }

                string written = File.ReadAllText(absolutePath, Encoding.UTF8);
                bool valid = string.Equals(written, content, StringComparison.Ordinal);
                return new CodexConfigWriteResult
                {
                    Success = valid,
                    Changed = true,
                    ConfigPath = absolutePath,
                    BackupPath = backupPath,
                    Error = valid ? string.Empty : "配置写入后的内容校验失败。"
                };
            }
            catch (Exception exception)
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                return new CodexConfigWriteResult
                {
                    Success = false,
                    Changed = false,
                    ConfigPath = absolutePath,
                    BackupPath = backupPath,
                    Error = exception.Message
                };
            }
        }

        private static string EscapeToml(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string DetectNewline(string source)
        {
            return source.IndexOf("\r\n", StringComparison.Ordinal) >= 0
                ? "\r\n"
                : "\n";
        }

        private static string NormalizeNewlines(string text, string newline)
        {
            return Regex.Replace(text, @"\r\n|\r|\n", newline);
        }

        private static bool LooksLikeIncompleteTarget(string line)
        {
            return Regex.IsMatch(
                line,
                @"^\s*\[{1,2}mcp_servers\.tuanjie[^\]]*$",
                RegexOptions.IgnoreCase);
        }

        private static CodexConfigMergeResult Failure(string original, string error)
        {
            return new CodexConfigMergeResult
            {
                Success = false,
                Changed = false,
                Content = original,
                Error = error
            };
        }
    }
}
