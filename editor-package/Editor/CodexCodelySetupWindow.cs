using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace QJX.CodexTuanjieBridge.Editor
{
    public sealed class CodexCodelySetupWindow : EditorWindow
    {
        private const string CliPathPreference = "QJX.CodexTuanjieBridge.CodelyCliPath";
        private SetupStatus _status;
        private string _preview = string.Empty;
        private Vector2 _previewScroll;
        private string _cliVersion = string.Empty;

        [MenuItem("Window/Tuanjie Codex Setup")]
        public static void Open()
        {
            GetWindow<CodexCodelySetupWindow>();
        }

        [MenuItem("Window/Tuanjie Codely Agent Setup")]
        private static void OpenAgentSetup()
        {
            Open();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Tuanjie Codely Agent Setup");
            minSize = new Vector2(520f, 420f);
            RefreshStatus();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Tuanjie + Codely Bridge 项目配置",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "此窗口只生成团结项目的 Codex MCP 配置；Claude Code、Qoder、Cursor 和 WorkBuddy 请按仓库文档配置，不会自动安装包或启动长期驻留服务。",
                MessageType.Info);

            if (GUILayout.Button("刷新状态"))
            {
                RefreshStatus();
            }
            DrawStatus();
            EditorGUILayout.Space(8f);
            DrawActions();
            DrawPreview();
        }

        private void DrawStatus()
        {
            if (_status == null)
            {
                EditorGUILayout.LabelField("状态", "尚未读取");
                return;
            }

            DrawStatusRow(
                "Editor / 项目",
                _status.Project == null
                    ? "未识别"
                    : (_status.Project.IsTuanjieEditor ? "团结 Editor" : "非团结 Editor"));
            DrawStatusRow(
                "Codely Bridge",
                _status.BridgeInstalled
                    ? "已安装 " + _status.BridgeVersion
                    : "未安装（请打开 Package Manager）");
            DrawStatusRow(
                "Bridge descriptor",
                _status.DescriptorExists ? "已存在" : "未找到");
            DrawStatusRow(
                "CodelyCLI",
                _status.CodelyCli != null && _status.CodelyCli.Found
                    ? _status.CodelyCli.Path
                    : "未找到");
            DrawStatusRow("CodelyCLI 版本", string.IsNullOrEmpty(_cliVersion) ? "未查询" : _cliVersion);
            DrawStatusRow(
                "项目 config.toml",
                _status.ProjectConfigExists ? "已存在" : "未创建");
            DrawStatusRow(
                "Codex Skill",
                _status.GlobalSkillExists ? "已安装" : "未找到");
            if (!string.IsNullOrEmpty(_status.Error))
            {
                EditorGUILayout.HelpBox(_status.Error, MessageType.Warning);
            }
        }

        private void DrawStatusRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(150f));
                EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(18f));
            }
        }

        private void DrawActions()
        {
            using (new EditorGUI.DisabledScope(_status == null))
            {
                if (GUILayout.Button("选择 CodelyCLI"))
                {
                    SelectCli();
                }
                if (GUILayout.Button("预览配置"))
                {
                    PreviewConfig();
                }
                using (new EditorGUI.DisabledScope(_status == null || !_status.CanGenerateConfig))
                {
                    if (GUILayout.Button("生成/更新项目配置"))
                    {
                        GenerateConfig();
                    }
                }
                if (GUILayout.Button("打开配置目录"))
                {
                    OpenConfigDirectory();
                }
                if (GUILayout.Button("打开 Package Manager"))
                {
                    EditorApplication.ExecuteMenuItem("Window/Package Manager");
                }
                if (GUILayout.Button("复制 Codex Skill 安装提示"))
                {
                    GUIUtility.systemCopyBuffer =
                        "将 Tuanjie Codely Skills 安装到当前 Codex 支持的用户级 Skill 目录，然后重新打开 Codex 对话；其他 Agent 请按多 Agent 配置文档添加项目级 MCP。";
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("复制只读检查提示"))
                    {
                        CopyPrompt(
                            "请先确认当前项目是团结 Editor，并读取 Codely Bridge/MCP 状态；只读检查，不修改文件。");
                    }
                    if (GUILayout.Button("复制写入冒烟提示"))
                    {
                        CopyPrompt(
                            "请先读取当前场景并做最小可回滚冒烟验证；确认后再通过 tuanjie MCP 写入一个测试对象。");
                    }
                    if (GUILayout.Button("复制连接诊断提示"))
                    {
                        CopyPrompt(
                            "请诊断当前 Agent → CodelyCLI → Codely Bridge → 团结 Editor 链路，并报告项目路径、版本和错误。");
                    }
                }
            }
        }

        private void DrawPreview()
        {
            if (string.IsNullOrEmpty(_preview))
            {
                return;
            }
            EditorGUILayout.LabelField("配置预览（只读）", EditorStyles.boldLabel);
            _previewScroll = EditorGUILayout.BeginScrollView(_previewScroll, GUILayout.Height(220f));
            EditorGUILayout.TextArea(_preview, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void RefreshStatus()
        {
            _preview = string.Empty;
            string configuredCliPath = EditorPrefs.GetString(CliPathPreference, string.Empty);
            _status = SetupStatus.Collect(
                Directory.GetParent(Application.dataPath).FullName,
                EditorApplication.applicationPath,
                configuredCliPath);
            _cliVersion = string.Empty;
            if (_status.CodelyCli != null && _status.CodelyCli.Found)
            {
                Task<string> task = CodelyCliVersionProbe.ReadVersionAsync(
                    _status.CodelyCli.Path,
                    TimeSpan.FromSeconds(5));
                task.ContinueWith(completed =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        _cliVersion = completed.Result;
                        Repaint();
                    };
                });
            }
            Repaint();
        }

        private void SelectCli()
        {
            string selected = EditorUtility.OpenFilePanel(
                "选择 CodelyCLI",
                string.Empty,
                "cmd");
            if (!string.IsNullOrEmpty(selected))
            {
                EditorPrefs.SetString(CliPathPreference, selected);
                RefreshStatus();
            }
        }

        private void PreviewConfig()
        {
            if (_status == null || _status.Project == null || !_status.Project.CanWriteConfig ||
                _status.CodelyCli == null || !_status.CodelyCli.Found)
            {
                _preview = "当前状态不允许生成配置，请先修复团结项目、Bridge 或 CodelyCLI 状态。";
                return;
            }
            string root = Directory.GetParent(Application.dataPath).FullName;
            string configPath = Path.Combine(root, ".codex", "config.toml");
            string original = File.Exists(configPath) ? File.ReadAllText(configPath) : string.Empty;
            string desired = CodexConfigEditor.BuildServerSection(
                _status.CodelyCli.Path,
                root);
            CodexConfigMergeResult result = CodexConfigEditor.Merge(original, desired);
            _preview = result.Success ? result.Content : result.Error;
        }

        private void GenerateConfig()
        {
            if (_status == null || !_status.CanGenerateConfig)
            {
                return;
            }
            string root = Directory.GetParent(Application.dataPath).FullName;
            string configPath = Path.Combine(root, ".codex", "config.toml");
            bool exists = File.Exists(configPath);
            if (exists &&
                !EditorUtility.DisplayDialog(
                    "确认更新 Codex 配置",
                    "将更新 " + configPath + "；原文件会备份为 " + configPath + ".bak。",
                    "继续",
                    "取消"))
            {
                return;
            }
            string original = exists ? File.ReadAllText(configPath) : string.Empty;
            string desired = CodexConfigEditor.BuildServerSection(
                _status.CodelyCli.Path,
                root);
            CodexConfigMergeResult merge = CodexConfigEditor.Merge(original, desired);
            if (!merge.Success)
            {
                EditorUtility.DisplayDialog("配置预览失败", merge.Error, "确定");
                return;
            }
            CodexConfigWriteResult write = CodexConfigEditor.Write(
                configPath,
                merge.Content,
                exists);
            if (!write.Success)
            {
                EditorUtility.DisplayDialog("配置写入失败", write.Error, "确定");
                return;
            }
            RefreshStatus();
        }

        private void OpenConfigDirectory()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(root, ".codex");
            if (!Directory.Exists(directory))
            {
                EditorUtility.DisplayDialog(
                    "配置目录不存在",
                    "请先生成项目配置，或手动创建 " + directory + "。",
                    "确定");
                return;
            }
            EditorUtility.RevealInFinder(directory);
        }

        private static void CopyPrompt(string prompt)
        {
            GUIUtility.systemCopyBuffer = prompt;
        }
    }

    [InitializeOnLoad]
    internal static class CodexCodelySetupPrompt
    {
        private const string PromptedKey = "QJX.CodexTuanjieBridge.Prompted";

        static CodexCodelySetupPrompt()
        {
            if (Application.isBatchMode ||
                SessionState.GetBool(PromptedKey, false) ||
                EditorPrefs.GetBool(PromptedKey, false))
            {
                return;
            }
            SessionState.SetBool(PromptedKey, true);
            EditorApplication.delayCall += PromptIfNeeded;
        }

        private static void PromptIfNeeded()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            SetupStatus status = SetupStatus.Collect(
                root,
                EditorApplication.applicationPath,
                EditorPrefs.GetString(
                    "QJX.CodexTuanjieBridge.CodelyCliPath",
                    string.Empty));
            if (status.Project != null &&
                status.Project.CanWriteConfig &&
                !status.ProjectConfigExists)
            {
                EditorUtility.DisplayDialog(
                    "Tuanjie Codely Agent Setup",
                    "检测到团结项目尚未配置 Codex MCP。可从 Window/Tuanjie Codely Agent Setup 手动预览并生成 Codex 配置；其他 Agent 请按仓库文档配置。",
                    "知道了");
                EditorPrefs.SetBool(PromptedKey, true);
            }
        }
    }
}
