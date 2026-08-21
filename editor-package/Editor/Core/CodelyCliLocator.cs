using System;
using System.Collections.Generic;
using System.IO;

namespace QJX.CodexTuanjieBridge.Editor
{
    public sealed class CodelyCliResolution
    {
        public bool Found { get; set; }
        public string Path { get; set; }
        public string Source { get; set; }
        public string Error { get; set; }
    }

    public static class CodelyCliLocator
    {
        public static CodelyCliResolution Resolve(
            string configuredPath,
            string environmentPath,
            Func<string, bool> fileExists,
            Func<IReadOnlyList<string>> findOnPath)
        {
            if (fileExists == null)
            {
                throw new ArgumentNullException("fileExists");
            }
            if (findOnPath == null)
            {
                throw new ArgumentNullException("findOnPath");
            }

            CodelyCliResolution configured = TryResolve(
                configuredPath,
                "EditorPrefs",
                fileExists);
            if (configured.Found)
            {
                return configured;
            }

            CodelyCliResolution environment = TryResolve(
                environmentPath,
                "CODELY_CLI_PATH",
                fileExists);
            if (environment.Found)
            {
                return environment;
            }

            IReadOnlyList<string> pathCandidates;
            try
            {
                pathCandidates = findOnPath();
            }
            catch (Exception exception)
            {
                return Missing("查询 PATH 中的 CodelyCLI 失败：" + exception.Message);
            }

            if (pathCandidates != null)
            {
                for (int index = 0; index < pathCandidates.Count; index++)
                {
                    CodelyCliResolution pathResult = TryResolve(
                        pathCandidates[index],
                        "PATH",
                        fileExists);
                    if (pathResult.Found)
                    {
                        return pathResult;
                    }
                }
            }

            return Missing("未找到 CodelyCLI，请在窗口中设置 EditorPrefs、CODELY_CLI_PATH 或 PATH。");
        }

        private static CodelyCliResolution TryResolve(
            string candidate,
            string source,
            Func<string, bool> fileExists)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return Missing(string.Empty);
            }

            string fullPath;
            try
            {
                fullPath = System.IO.Path.GetFullPath(candidate.Trim());
            }
            catch
            {
                return Missing(string.Empty);
            }

            bool exists;
            try
            {
                exists = fileExists(fullPath);
            }
            catch
            {
                exists = false;
            }
            if (!exists)
            {
                return Missing(string.Empty);
            }

            return new CodelyCliResolution
            {
                Found = true,
                Path = fullPath,
                Source = source,
                Error = string.Empty
            };
        }

        private static CodelyCliResolution Missing(string error)
        {
            return new CodelyCliResolution
            {
                Found = false,
                Path = string.Empty,
                Source = string.Empty,
                Error = error
            };
        }
    }
}
