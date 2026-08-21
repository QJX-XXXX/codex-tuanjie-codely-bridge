using System;
using System.IO;

namespace QJX.CodexTuanjieBridge.Editor
{
    public sealed class ConfigurationWriteResult
    {
        public bool Success { get; set; }
        public bool Changed { get; set; }
        public string ConfigPath { get; set; }
        public string BackupPath { get; set; }
        public string Error { get; set; }
    }

    public static class ConfigurationFileWriter
    {
        public static ConfigurationWriteResult Write(ConfigurationPatchPlan plan)
        {
            if (plan == null || !plan.Success || plan.Target == null)
            {
                return Failure(string.Empty, string.Empty, "配置补丁计划无效。");
            }
            if (!plan.Changed)
            {
                return new ConfigurationWriteResult
                {
                    Success = true,
                    Changed = false,
                    ConfigPath = plan.Target.ConfigPath,
                    BackupPath = string.Empty,
                    Error = string.Empty
                };
            }

            string path = plan.Target.ConfigPath;
            string backupPath = plan.OriginalExists ? path + ".bak" : string.Empty;
            string tempPath = path + ".tmp." + Guid.NewGuid().ToString("N");
            bool destinationWritten = false;
            try
            {
                bool existsNow = File.Exists(path);
                if (existsNow != plan.OriginalExists)
                {
                    return Failure(path, backupPath, "预览后配置文件的存在状态已变化，请重新读取并预览。");
                }
                byte[] current = existsNow ? File.ReadAllBytes(path) : new byte[0];
                if (!string.Equals(
                        ConfigurationPatchEngine.ComputeHash(current),
                        plan.OriginalHash,
                        StringComparison.Ordinal) ||
                    !ConfigurationPatchEngine.BytesEqual(current, plan.OriginalBytes))
                {
                    return Failure(path, backupPath, "预览后配置文件内容已变化，请重新读取并预览。");
                }

                string directory = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directory))
                {
                    return Failure(path, backupPath, "无法确定 Agent 配置目录。");
                }
                Directory.CreateDirectory(directory);

                // 备份保存的是本次写入前的完整原文件，便于用户直接回滚。
                if (plan.OriginalExists)
                {
                    File.Copy(path, backupPath, true);
                }
                File.WriteAllBytes(tempPath, plan.DesiredBytes);

                if (plan.OriginalExists)
                {
                    try
                    {
                        File.Replace(tempPath, path, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(tempPath, path, true);
                        File.Delete(tempPath);
                    }
                    catch (NotSupportedException)
                    {
                        File.Copy(tempPath, path, true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }
                destinationWritten = true;

                byte[] actual = File.ReadAllBytes(path);
                if (!ConfigurationPatchEngine.BytesEqual(actual, plan.DesiredBytes) ||
                    !ConfigurationPatchEngine.VerifyPatchBoundary(plan, actual))
                {
                    Restore(plan, path, backupPath);
                    return Failure(path, backupPath, "写入后的逐字节边界校验失败，已回滚原配置。");
                }

                // 再按客户端格式重新定位一次参数，确保补丁没有只满足字节条件却破坏语义。
                ConfigurationPatchPlan validation = ConfigurationPatchEngine.BuildPlan(
                    plan.Target,
                    plan.CodelyCliPath,
                    plan.DesiredProjectPath);
                if (!validation.Success || validation.Changed ||
                    validation.State != ConfigurationPatchState.Current)
                {
                    Restore(plan, path, backupPath);
                    return Failure(path, backupPath, "写入后的 MCP 项目路径语义校验失败，已回滚原配置。");
                }

                return new ConfigurationWriteResult
                {
                    Success = true,
                    Changed = true,
                    ConfigPath = path,
                    BackupPath = backupPath,
                    Error = string.Empty
                };
            }
            catch (Exception exception)
            {
                if (destinationWritten)
                {
                    try
                    {
                        Restore(plan, path, backupPath);
                    }
                    catch
                    {
                        // 回滚失败会通过主错误返回；不吞掉最初的写入异常。
                    }
                }
                return Failure(path, backupPath, "写入 Agent 配置失败：" + exception.Message);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
                catch
                {
                    // 临时文件清理失败不应覆盖已经得到的配置写入结果。
                }
            }
        }

        private static void Restore(
            ConfigurationPatchPlan plan,
            string path,
            string backupPath)
        {
            if (plan.OriginalExists)
            {
                if (string.IsNullOrEmpty(backupPath) || !File.Exists(backupPath))
                {
                    throw new IOException("配置回滚所需的备份文件不存在。");
                }
                File.Copy(backupPath, path, true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static ConfigurationWriteResult Failure(
            string configPath,
            string backupPath,
            string error)
        {
            return new ConfigurationWriteResult
            {
                Success = false,
                Changed = false,
                ConfigPath = configPath,
                BackupPath = backupPath,
                Error = error
            };
        }
    }
}
