using System;
using System.IO;
using System.Text.RegularExpressions;

namespace QJX.CodexTuanjieBridge.Editor
{
    public sealed class TuanjieProjectStatus
    {
        public bool IsTuanjieProject { get; set; }
        public bool IsTuanjieEditor { get; set; }
        public bool CanWriteConfig { get; set; }
        public string EditorVersion { get; set; }
        public string TuanjieVersion { get; set; }
        public string Error { get; set; }
    }

    public static class TuanjieProjectDetector
    {
        public static TuanjieProjectStatus Detect(
            string projectRoot,
            string editorApplicationPath)
        {
            var status = new TuanjieProjectStatus
            {
                EditorVersion = string.Empty,
                TuanjieVersion = string.Empty,
                Error = string.Empty
            };

            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            {
                status.Error = "当前路径不是有效的团结项目目录。";
                return status;
            }

            string[] requiredDirectories = { "Assets", "Packages", "ProjectSettings" };
            for (int index = 0; index < requiredDirectories.Length; index++)
            {
                if (!Directory.Exists(Path.Combine(projectRoot, requiredDirectories[index])))
                {
                    status.Error = "当前项目缺少 " + requiredDirectories[index] + " 目录。";
                    return status;
                }
            }

            string versionPath = Path.Combine(projectRoot, "ProjectSettings", "ProjectVersion.txt");
            if (!File.Exists(versionPath))
            {
                status.Error = "当前项目缺少 ProjectSettings/ProjectVersion.txt。";
                return status;
            }

            string versionText;
            try
            {
                versionText = File.ReadAllText(versionPath);
            }
            catch (Exception exception)
            {
                status.Error = "读取 ProjectVersion.txt 失败：" + exception.Message;
                return status;
            }

            status.EditorVersion = ReadVersion(versionText, "m_EditorVersion");
            status.TuanjieVersion = ReadVersion(versionText, "m_TuanjieEditorVersion");
            status.IsTuanjieProject = !string.IsNullOrEmpty(status.TuanjieVersion);

            string editorFileName = string.Empty;
            if (!string.IsNullOrWhiteSpace(editorApplicationPath))
            {
                editorFileName = Path.GetFileName(editorApplicationPath);
            }
            status.IsTuanjieEditor =
                string.Equals(editorFileName, "Tuanjie.exe", StringComparison.OrdinalIgnoreCase);

            if (status.IsTuanjieProject && status.IsTuanjieEditor)
            {
                status.CanWriteConfig = true;
                return status;
            }

            if (status.IsTuanjieProject != status.IsTuanjieEditor)
            {
                status.Error = "项目标识与当前 Editor 不一致，仅支持团结 Editor，已禁止写入配置。";
            }
            else
            {
                status.Error = "仅支持团结 Editor，已禁止写入配置。";
            }
            return status;
        }

        private static string ReadVersion(string text, string key)
        {
            Match match = Regex.Match(
                text ?? string.Empty,
                @"(?m)^\s*" + Regex.Escape(key) + @":\s*(?<value>\S.*?)\s*$");
            return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
        }
    }
}
