using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace QJX.CodexTuanjieBridge.Editor
{
    public sealed class CodexCodelySetupWindow : EditorWindow
    {
        private const string CliPathPreference =
            "QJX.CodexTuanjieBridge.CodelyCliPath";
        private const string ClientPreference =
            "QJX.CodexTuanjieBridge.AgentClient";
        private const string ScopePreference =
            "QJX.CodexTuanjieBridge.AgentScope";

        private SetupStatus _status;
        private AgentClientId _selectedClient;
        private AgentConfigScope _selectedScope;
        private AgentClientTarget _target;
        private ConfigurationPatchPlan _plan;
        private string _preview = string.Empty;
        private Vector2 _windowScroll;
        private Vector2 _previewScroll;
        private string _cliVersion = string.Empty;
        private string _skillRoot = string.Empty;
        private string _skillInstallationStatus = string.Empty;
        private bool _skillInstallationInProgress;

        [MenuItem("Window/Tuanjie Codely Agent Setup")]
        public static void Open()
        {
            GetWindow<CodexCodelySetupWindow>();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Tuanjie Codely Agent Setup");
            minSize = new Vector2(650f, 620f);
            _selectedClient = ReadClientPreference();
            _selectedScope = ReadScopePreference();
            RefreshStatus();
        }

        private void OnGUI()
        {
            _windowScroll = EditorGUILayout.BeginScrollView(_windowScroll);
            EditorGUILayout.LabelField(
                "Tuanjie + Codely Bridge Agent 配置",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "选择 Agent 和配置范围。默认使用用户级全局配置，一次只绑定一个团结项目；窗口不会安装或替换 Codely Bridge，也不会启动长期驻留的 MCP 服务。",
                MessageType.Info);

            DrawClientAndScope();
            EditorGUILayout.Space(8f);
            DrawStatus();
            EditorGUILayout.Space(8f);
            DrawSafetyContract();
            DrawActions();
            DrawPreview();
            EditorGUILayout.EndScrollView();
        }

        private void DrawClientAndScope()
        {
            EditorGUILayout.LabelField("客户端与范围", EditorStyles.boldLabel);
            IReadOnlyList<IAgentClientConfigurator> clients = AgentClientRegistry.All;
            string[] names = new string[clients.Count];
            int selectedIndex = 0;
            for (int index = 0; index < clients.Count; index++)
            {
                names[index] = clients[index].DisplayName;
                if (clients[index].Id == _selectedClient) selectedIndex = index;
            }

            int nextClientIndex = EditorGUILayout.Popup("Client", selectedIndex, names);
            if (nextClientIndex != selectedIndex)
            {
                _selectedClient = clients[nextClientIndex].Id;
                EditorPrefs.SetInt(ClientPreference, (int)_selectedClient);
                RefreshStatus();
            }

            int scopeIndex = _selectedScope == AgentConfigScope.UserGlobal ? 0 : 1;
            int nextScopeIndex = GUILayout.Toolbar(
                scopeIndex,
                new[] { "用户级全局（单项目，默认）", "当前项目（多项目并行推荐）" });
            if (nextScopeIndex != scopeIndex)
            {
                _selectedScope = nextScopeIndex == 0
                    ? AgentConfigScope.UserGlobal
                    : AgentConfigScope.CurrentProject;
                EditorPrefs.SetInt(ScopePreference, (int)_selectedScope);
                RefreshStatus();
            }

            if (_target != null)
            {
                DrawStatusRow("目标配置", _target.ConfigPath);
                string scopeMessage = _target.IsUserGlobal
                    ? "当前项目会成为 " + _target.DisplayName +
                      " 唯一的全局团结项目目标；配置其他项目会替换这个路径。需要同时打开多个项目时，请改用当前项目范围。"
                    : "当前项目范围只绑定这个工作区，适合同时使用多个团结项目。";
                EditorGUILayout.HelpBox(
                    scopeMessage,
                    _target.IsUserGlobal ? MessageType.Warning : MessageType.Info);
            }
        }

        private void DrawStatus()
        {
            EditorGUILayout.LabelField("环境与配置状态", EditorStyles.boldLabel);
            if (_status == null)
            {
                EditorGUILayout.LabelField("状态", "尚未读取");
                return;
            }

            DrawStatusRow(
                "Editor / 项目",
                _status.Project == null
                    ? "未识别"
                    : (_status.Project.IsTuanjieEditor
                        ? "团结 Editor / " + GetProjectRoot()
                        : "非团结 Editor"));
            DrawStatusRow(
                "Codely Bridge",
                _status.BridgeInstalled
                    ? "已安装 " + _status.BridgeVersion + "；随 Editor 加载并初始化"
                    : "未安装（请打开 Package Manager）");
            DrawStatusRow(
                "CodelyCLI",
                _status.CodelyCli != null && _status.CodelyCli.Found
                    ? _status.CodelyCli.Path
                    : "未找到");
            DrawStatusRow(
                "CodelyCLI 版本",
                string.IsNullOrEmpty(_cliVersion) ? "未查询" : _cliVersion);
            DrawStatusRow(
                "Agent Skills",
                string.IsNullOrEmpty(_skillRoot)
                    ? "未解析"
                    : TuanjieSkillInstallationService.CountInstalledSkills(_skillRoot) +
                      "/5 已安装 · " + _skillRoot);
            DrawStatusRow("tuanjie MCP", GetPatchStateLabel());
            if (!string.IsNullOrEmpty(_status.Error))
            {
                EditorGUILayout.HelpBox(_status.Error, MessageType.Warning);
            }
            if (_plan != null && !_plan.Success)
            {
                EditorGUILayout.HelpBox(_plan.Error, MessageType.Error);
            }
            if (!string.IsNullOrEmpty(_skillInstallationStatus))
            {
                EditorGUILayout.HelpBox(
                    _skillInstallationStatus,
                    _skillInstallationInProgress
                        ? MessageType.Info
                        : MessageType.None);
            }
        }

        private void DrawSafetyContract()
        {
            EditorGUILayout.LabelField("唯一变更保证", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "已有 tuanjie 配置只替换 --unity-project-path 后的一个路径字符串；其他配置、注释、顺序和空白逐字节保持。目标重复、缺少参数或结构异常时拒绝写入。缺少 tuanjie 时才新增最小配置。",
                MessageType.None);
        }

        private void DrawStatusRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(150f));
                EditorGUILayout.SelectableLabel(
                    value ?? string.Empty,
                    EditorStyles.textField,
                    GUILayout.Height(18f));
            }
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("选择 CodelyCLI")) SelectCli();
                if (GUILayout.Button("重新读取")) RefreshStatus();
                if (GUILayout.Button("打开配置目录")) OpenConfigDirectory();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           _status == null || !_status.CanConfigureClient))
                {
                    if (GUILayout.Button("预览配置")) PreviewConfig();
                }
                if (GUILayout.Button("打开 Package Manager"))
                {
                    EditorApplication.ExecuteMenuItem("Window/Package Manager");
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                           _skillInstallationInProgress || string.IsNullOrEmpty(_skillRoot)))
                {
                    if (GUILayout.Button("安装/更新 Skills")) InstallSkills();
                }
                using (new EditorGUI.DisabledScope(_target == null))
                {
                    if (GUILayout.Button("复制客户端刷新说明"))
                    {
                        GUIUtility.systemCopyBuffer = _target.ReloadGuidance;
                    }
                }
            }
            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(
                       _status == null || !_status.CanConfigureClient))
            {
                if (GUILayout.Button(
                        "配置客户端",
                        GUILayout.ExpandWidth(true),
                        GUILayout.Height(EditorGUIUtility.singleLineHeight * 3f)))
                {
                    ConfigureClient();
                }
            }
        }

        private void DrawPreview()
        {
            if (string.IsNullOrEmpty(_preview)) return;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("唯一变更预览（只读）", EditorStyles.boldLabel);
            _previewScroll = EditorGUILayout.BeginScrollView(
                _previewScroll,
                GUILayout.MinHeight(170f),
                GUILayout.MaxHeight(260f));
            EditorGUILayout.TextArea(_preview, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void RefreshStatus()
        {
            _preview = string.Empty;
            string projectRoot = GetProjectRoot();
            string configuredCliPath = EditorPrefs.GetString(
                CliPathPreference,
                string.Empty);
            _status = SetupStatus.Collect(
                projectRoot,
                EditorApplication.applicationPath,
                configuredCliPath);
            _target = ResolveTarget(projectRoot);
            _skillRoot = ResolveSkillRoot(projectRoot);
            RefreshPatchPlan(projectRoot);
            ProbeCliVersion();
            Repaint();
        }

        private void RefreshPatchPlan(string projectRoot)
        {
            _plan = null;
            if (_status == null || _status.CodelyCli == null ||
                !_status.CodelyCli.Found || _target == null)
            {
                return;
            }
            _plan = ConfigurationPatchEngine.BuildPlan(
                _target,
                _status.CodelyCli.Path,
                projectRoot);
        }

        private void ProbeCliVersion()
        {
            _cliVersion = string.Empty;
            if (_status == null || _status.CodelyCli == null ||
                !_status.CodelyCli.Found)
            {
                return;
            }
            Task<string> task = CodelyCliVersionProbe.ReadVersionAsync(
                _status.CodelyCli.Path,
                TimeSpan.FromSeconds(5));
            task.ContinueWith(completed =>
            {
                string version = completed.Status == TaskStatus.RanToCompletion
                    ? completed.Result
                    : "读取 CodelyCLI 版本失败。";
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    _cliVersion = version;
                    Repaint();
                };
            });
        }

        private AgentClientTarget ResolveTarget(string projectRoot)
        {
            try
            {
                return AgentClientRegistry.Get(_selectedClient).ResolveTarget(
                    BuildClientContext(projectRoot),
                    _selectedScope);
            }
            catch (Exception exception)
            {
                Debug.LogError("无法解析 Agent 配置目标：" + exception.Message);
                return null;
            }
        }

        private string ResolveSkillRoot(string projectRoot)
        {
            try
            {
                return AgentClientRegistry.Get(_selectedClient)
                    .ResolveSkillRoot(BuildClientContext(projectRoot));
            }
            catch (System.Exception exception)
            {
                Debug.LogError("无法解析 Agent Skill 目录：" + exception.Message);
                return string.Empty;
            }
        }

        private static AgentClientContext BuildClientContext(string projectRoot)
        {
            return new AgentClientContext
            {
                ProjectRoot = projectRoot,
                UserHome = Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                CodexHome = Environment.GetEnvironmentVariable("CODEX_HOME")
            };
        }

        private void SelectCli()
        {
            string selected = EditorUtility.OpenFilePanel(
                "选择 CodelyCLI",
                string.Empty,
                "cmd");
            if (string.IsNullOrEmpty(selected)) return;
            EditorPrefs.SetString(CliPathPreference, Path.GetFullPath(selected));
            RefreshStatus();
        }

        private void PreviewConfig()
        {
            RefreshPatchPlan(GetProjectRoot());
            _preview = _plan == null
                ? "当前状态无法生成预览，请先确认团结项目、Codely Bridge 和 CodelyCLI。"
                : (_plan.Success ? _plan.Preview : _plan.Error);
        }

        private void ConfigureClient()
        {
            string projectRoot = GetProjectRoot();
            RefreshPatchPlan(projectRoot);
            if (_plan == null || !_plan.Success)
            {
                EditorUtility.DisplayDialog(
                    "无法安全配置客户端",
                    _plan == null ? "请先完成环境检查。" : _plan.Error,
                    "确定");
                return;
            }
            if (!_plan.Changed)
            {
                EditorUtility.DisplayDialog(
                    "无需更新",
                    _target.DisplayName + " 已指向当前团结项目。",
                    "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "确认配置 " + _target.DisplayName,
                    _plan.Preview,
                    "仅执行上述变更",
                    "取消"))
            {
                return;
            }

            ConfigurationWriteResult result = ConfigurationFileWriter.Write(_plan);
            if (!result.Success)
            {
                EditorUtility.DisplayDialog("配置写入失败", result.Error, "确定");
                return;
            }
            string resultMessage = result.Changed
                ? "已更新：" + result.ConfigPath +
                  (string.IsNullOrEmpty(result.BackupPath)
                      ? string.Empty
                      : "\n备份：" + result.BackupPath) +
                  "\n\n" + _target.ReloadGuidance
                : "配置未发生变化。";
            RefreshStatus();
            EditorUtility.DisplayDialog("客户端配置完成", resultMessage, "确定");
        }

        private void OpenConfigDirectory()
        {
            if (_target == null) return;
            string directory = Path.GetDirectoryName(_target.ConfigPath);
            if (string.IsNullOrEmpty(directory)) return;
            if (!Directory.Exists(directory))
            {
                EditorUtility.DisplayDialog(
                    "配置目录不存在",
                    "先预览并配置客户端，或手动创建：" + directory,
                    "确定");
                return;
            }
            EditorUtility.RevealInFinder(directory);
        }

        private void InstallSkills()
        {
            if (_skillInstallationInProgress || string.IsNullOrEmpty(_skillRoot))
            {
                return;
            }

            string skillList = string.Join(
                "、",
                new[]
                {
                    "tuanjie-workflows",
                    "tuanjie-codely-bridge",
                    "tuanjie-editor-automation",
                    "tuanjie-package-management",
                    "tuanjie-codely-custom-tools"
                });
            if (!EditorUtility.DisplayDialog(
                    "安装/更新 Tuanjie Codely Skills",
                    "将从本仓库 main 分支获取五个公开 Skill，并安装到：\n" +
                    _skillRoot + "\n\n" + skillList +
                    "\n\n已有非本工具文件的 Skill 目录会拒绝覆盖。",
                    "安装/更新",
                    "取消"))
            {
                return;
            }

            _skillInstallationInProgress = true;
            _skillInstallationStatus = "正在获取并校验五个 Skill，请稍候……";
            Repaint();
            TuanjieSkillInstallationService.InstallOrUpdateAsync(
                _skillRoot,
                result =>
                {
                    _skillInstallationInProgress = false;
                    if (result.Success)
                    {
                        _skillInstallationStatus =
                            "已完成：新增 " + result.Added +
                            "，更新 " + result.Updated +
                            "，无需变化 " + result.Unchanged +
                            "；安装目录：" + result.InstallRoot +
                            (string.IsNullOrEmpty(result.Warning)
                                ? string.Empty
                                : "\n" + result.Warning);
                        EditorUtility.DisplayDialog(
                            "Skills 安装完成",
                            _skillInstallationStatus,
                            "确定");
                    }
                    else
                    {
                        _skillInstallationStatus = "安装失败：" + result.Error;
                        EditorUtility.DisplayDialog(
                            "Skills 安装失败",
                            result.Error,
                            "确定");
                    }
                    Repaint();
                });
        }

        private string GetPatchStateLabel()
        {
            if (_plan == null) return "尚未检查";
            if (!_plan.Success) return "配置异常，已拒绝写入";
            switch (_plan.State)
            {
                case ConfigurationPatchState.Missing:
                    return "未注册（可新增最小配置）";
                case ConfigurationPatchState.Current:
                    return "已指向当前项目";
                case ConfigurationPatchState.NeedsUpdate:
                    return "已注册，但指向其他项目";
                default:
                    return "配置异常";
            }
        }

        private static AgentClientId ReadClientPreference()
        {
            int value = EditorPrefs.GetInt(ClientPreference, (int)AgentClientId.Codex);
            return Enum.IsDefined(typeof(AgentClientId), value)
                ? (AgentClientId)value
                : AgentClientId.Codex;
        }

        private static AgentConfigScope ReadScopePreference()
        {
            int value = EditorPrefs.GetInt(
                ScopePreference,
                (int)AgentConfigScope.UserGlobal);
            return Enum.IsDefined(typeof(AgentConfigScope), value)
                ? (AgentConfigScope)value
                : AgentConfigScope.UserGlobal;
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Directory.GetParent(Application.dataPath).FullName);
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
            string root = Path.GetFullPath(
                Directory.GetParent(Application.dataPath).FullName);
            SetupStatus status = SetupStatus.Collect(
                root,
                EditorApplication.applicationPath,
                EditorPrefs.GetString(
                    "QJX.CodexTuanjieBridge.CodelyCliPath",
                    string.Empty));
            if (status.Project == null || !status.Project.CanWriteConfig) return;
            EditorUtility.DisplayDialog(
                "Tuanjie Codely Agent Setup",
                "可从 Window/Tuanjie Codely Agent Setup 选择 Codex、Claude Code、Cursor、Qoder 或 WorkBuddy，并安全配置用户级或当前项目的 tuanjie MCP。",
                "知道了");
            EditorPrefs.SetBool(PromptedKey, true);
        }
    }
}
