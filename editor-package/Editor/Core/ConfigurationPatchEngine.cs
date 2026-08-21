using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QJX.CodexTuanjieBridge.Editor
{
    public enum ConfigurationPatchState
    {
        Missing,
        Current,
        NeedsUpdate,
        Invalid
    }

    public sealed class ConfigurationPatchPlan
    {
        public bool Success { get; internal set; }
        public bool Changed { get; internal set; }
        public ConfigurationPatchState State { get; internal set; }
        public AgentClientTarget Target { get; internal set; }
        public string OriginalProjectPath { get; internal set; }
        public string DesiredProjectPath { get; internal set; }
        public string Preview { get; internal set; }
        public string Error { get; internal set; }
        public bool CreatesServer { get; internal set; }
        public string BackupPath { get; internal set; }

        internal bool OriginalExists { get; set; }
        internal byte[] OriginalBytes { get; set; }
        internal byte[] DesiredBytes { get; set; }
        internal string OriginalHash { get; set; }
        internal int PatchStart { get; set; }
        internal int PatchLength { get; set; }
        internal byte[] PatchBytes { get; set; }
        internal string CodelyCliPath { get; set; }
    }

    internal sealed class TextPatchResult
    {
        public bool Success { get; set; }
        public bool CreatesServer { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string Replacement { get; set; }
        public string OriginalProjectPath { get; set; }
        public string Error { get; set; }
    }

    public static class ConfigurationPatchEngine
    {
        private static readonly UTF8Encoding StrictUtf8 =
            new UTF8Encoding(false, true);

        public static ConfigurationPatchPlan BuildPlan(
            AgentClientTarget target,
            string codelyCliPath,
            string projectRoot)
        {
            if (target == null)
            {
                return Failure(null, "Agent 配置目标不能为空。");
            }
            if (string.IsNullOrWhiteSpace(projectRoot) ||
                string.IsNullOrWhiteSpace(codelyCliPath))
            {
                return Failure(target, "项目路径和 CodelyCLI 路径不能为空。");
            }

            string normalizedProject;
            string normalizedCli;
            try
            {
                normalizedProject = Path.GetFullPath(projectRoot ?? string.Empty);
                normalizedCli = Path.GetFullPath(codelyCliPath ?? string.Empty);
            }
            catch (Exception exception)
            {
                return Failure(target, "无法规范化项目或 CodelyCLI 路径：" + exception.Message);
            }

            if (!Path.IsPathRooted(normalizedProject) ||
                !Path.IsPathRooted(normalizedCli))
            {
                return Failure(target, "项目路径和 CodelyCLI 路径必须是绝对路径。");
            }

            byte[] originalBytes;
            bool originalExists = File.Exists(target.ConfigPath);
            try
            {
                originalBytes = originalExists
                    ? File.ReadAllBytes(target.ConfigPath)
                    : new byte[0];
            }
            catch (Exception exception)
            {
                return Failure(target, "读取 Agent 配置失败：" + exception.Message);
            }

            int preambleLength = HasUtf8Preamble(originalBytes) ? 3 : 0;
            string source;
            try
            {
                source = StrictUtf8.GetString(
                    originalBytes,
                    preambleLength,
                    originalBytes.Length - preambleLength);
            }
            catch (DecoderFallbackException)
            {
                return Failure(target, "配置文件不是有效 UTF-8；为避免破坏原文件，已拒绝修改。");
            }

            TextPatchResult textPatch;
            if (target.Format == AgentConfigFormat.Toml)
            {
                textPatch = TomlConfigurationPatcher.BuildPatch(
                    source,
                    normalizedCli,
                    normalizedProject);
            }
            else
            {
                textPatch = JsonConfigurationPatcher.BuildPatch(
                    source,
                    target.JsonObjectPath,
                    target.JsonProjectPathSegmentIndex,
                    normalizedCli,
                    normalizedProject);
            }

            if (!textPatch.Success)
            {
                return Failure(target, textPatch.Error);
            }

            int patchStart;
            int patchLength;
            byte[] replacementBytes;
            try
            {
                patchStart = preambleLength + StrictUtf8.GetByteCount(
                    source.Substring(0, textPatch.Start));
                patchLength = StrictUtf8.GetByteCount(
                    source.Substring(textPatch.Start, textPatch.Length));
                replacementBytes = StrictUtf8.GetBytes(textPatch.Replacement ?? string.Empty);
            }
            catch (Exception exception)
            {
                return Failure(target, "无法计算安全补丁范围：" + exception.Message);
            }

            byte[] desiredBytes = ApplyPatch(
                originalBytes,
                patchStart,
                patchLength,
                replacementBytes);
            bool changed = !BytesEqual(originalBytes, desiredBytes);
            ConfigurationPatchState state = textPatch.CreatesServer
                ? ConfigurationPatchState.Missing
                : (changed
                    ? ConfigurationPatchState.NeedsUpdate
                    : ConfigurationPatchState.Current);

            var plan = new ConfigurationPatchPlan
            {
                Success = true,
                Changed = changed,
                State = state,
                Target = target,
                OriginalProjectPath = textPatch.OriginalProjectPath ?? string.Empty,
                DesiredProjectPath = normalizedProject,
                Preview = BuildPreview(
                    target,
                    textPatch.CreatesServer,
                    textPatch.OriginalProjectPath,
                    normalizedProject,
                    changed),
                Error = string.Empty,
                CreatesServer = textPatch.CreatesServer,
                BackupPath = originalExists ? target.ConfigPath + ".bak" : string.Empty,
                OriginalExists = originalExists,
                OriginalBytes = originalBytes,
                DesiredBytes = desiredBytes,
                OriginalHash = ComputeHash(originalBytes),
                PatchStart = patchStart,
                PatchLength = patchLength,
                PatchBytes = replacementBytes,
                CodelyCliPath = normalizedCli
            };
            return plan;
        }

        internal static bool VerifyPatchBoundary(
            ConfigurationPatchPlan plan,
            byte[] actualBytes)
        {
            if (plan == null || actualBytes == null || plan.OriginalBytes == null ||
                plan.PatchBytes == null)
            {
                return false;
            }
            int expectedLength = plan.OriginalBytes.Length - plan.PatchLength +
                                 plan.PatchBytes.Length;
            if (actualBytes.Length != expectedLength)
            {
                return false;
            }
            for (int index = 0; index < plan.PatchStart; index++)
            {
                if (actualBytes[index] != plan.OriginalBytes[index])
                {
                    return false;
                }
            }
            int originalSuffixStart = plan.PatchStart + plan.PatchLength;
            int actualSuffixStart = plan.PatchStart + plan.PatchBytes.Length;
            int suffixLength = plan.OriginalBytes.Length - originalSuffixStart;
            for (int index = 0; index < suffixLength; index++)
            {
                if (actualBytes[actualSuffixStart + index] !=
                    plan.OriginalBytes[originalSuffixStart + index])
                {
                    return false;
                }
            }
            return true;
        }

        internal static string ComputeHash(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes ?? new byte[0]);
                var builder = new StringBuilder(hash.Length * 2);
                for (int index = 0; index < hash.Length; index++)
                {
                    builder.Append(hash[index].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        internal static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }
            return true;
        }

        private static byte[] ApplyPatch(
            byte[] source,
            int start,
            int length,
            byte[] replacement)
        {
            if (start < 0 || length < 0 || start + length > source.Length)
            {
                throw new ArgumentOutOfRangeException("start", "补丁范围超出配置文件边界。");
            }
            byte[] output = new byte[source.Length - length + replacement.Length];
            Buffer.BlockCopy(source, 0, output, 0, start);
            Buffer.BlockCopy(replacement, 0, output, start, replacement.Length);
            int sourceSuffixStart = start + length;
            int outputSuffixStart = start + replacement.Length;
            Buffer.BlockCopy(
                source,
                sourceSuffixStart,
                output,
                outputSuffixStart,
                source.Length - sourceSuffixStart);
            return output;
        }

        private static bool HasUtf8Preamble(byte[] bytes)
        {
            return bytes != null && bytes.Length >= 3 &&
                   bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        }

        private static string BuildPreview(
            AgentClientTarget target,
            bool createsServer,
            string originalPath,
            string desiredPath,
            bool changed)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Client：" + target.DisplayName);
            builder.AppendLine("范围：" + (target.IsUserGlobal
                ? "用户级全局（单项目）"
                : "当前项目（多项目并行推荐）"));
            builder.AppendLine("目标：" + target.ConfigPath);
            builder.AppendLine("操作：" + (createsServer
                ? "新增最小 tuanjie MCP 配置"
                : (changed
                    ? "只替换 --unity-project-path 后的路径字符串"
                    : "无需修改，项目路径已经一致")));
            if (!createsServer)
            {
                builder.AppendLine("旧路径：" + (originalPath ?? string.Empty));
            }
            builder.AppendLine("新路径：" + desiredPath);
            builder.AppendLine("其他配置、注释、顺序和空白：0 项变更");
            if (changed && File.Exists(target.ConfigPath))
            {
                builder.AppendLine("备份：" + target.ConfigPath + ".bak");
            }
            return builder.ToString().TrimEnd();
        }

        private static ConfigurationPatchPlan Failure(
            AgentClientTarget target,
            string error)
        {
            return new ConfigurationPatchPlan
            {
                Success = false,
                Changed = false,
                State = ConfigurationPatchState.Invalid,
                Target = target,
                OriginalProjectPath = string.Empty,
                DesiredProjectPath = string.Empty,
                Preview = string.Empty,
                Error = error ?? "配置无法安全修改。",
                CreatesServer = false,
                BackupPath = string.Empty,
                OriginalBytes = new byte[0],
                DesiredBytes = new byte[0],
                PatchBytes = new byte[0]
            };
        }
    }
}
