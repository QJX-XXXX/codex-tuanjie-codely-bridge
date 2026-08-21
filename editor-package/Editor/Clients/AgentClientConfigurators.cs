using System;
using System.IO;

namespace QJX.CodexTuanjieBridge.Editor
{
    internal abstract class AgentClientConfiguratorBase : IAgentClientConfigurator
    {
        public abstract AgentClientId Id { get; }
        public abstract string DisplayName { get; }

        public abstract string ResolveSkillRoot(AgentClientContext context);

        public abstract AgentClientTarget ResolveTarget(
            AgentClientContext context,
            AgentConfigScope scope);

        protected static void ValidateContext(AgentClientContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (string.IsNullOrWhiteSpace(context.ProjectRoot))
            {
                throw new ArgumentException("团结项目路径不能为空。", "context");
            }
            if (string.IsNullOrWhiteSpace(context.UserHome))
            {
                throw new ArgumentException("用户目录不能为空。", "context");
            }
        }

        protected AgentClientTarget JsonTarget(
            AgentConfigScope scope,
            string configPath,
            string[] objectPath,
            int projectPathSegmentIndex,
            string reloadGuidance)
        {
            return new AgentClientTarget
            {
                ClientId = Id,
                DisplayName = DisplayName,
                Scope = scope,
                Format = AgentConfigFormat.Json,
                ConfigPath = Path.GetFullPath(configPath),
                TomlTableName = string.Empty,
                JsonObjectPath = objectPath,
                JsonProjectPathSegmentIndex = projectPathSegmentIndex,
                ReloadGuidance = reloadGuidance
            };
        }
    }

    internal sealed class CodexClientConfigurator : AgentClientConfiguratorBase
    {
        public override AgentClientId Id { get { return AgentClientId.Codex; } }
        public override string DisplayName { get { return "Codex"; } }

        public override string ResolveSkillRoot(AgentClientContext context)
        {
            ValidateContext(context);
            string codexHome = string.IsNullOrWhiteSpace(context.CodexHome)
                ? Path.Combine(context.UserHome, ".codex")
                : context.CodexHome;
            return Path.GetFullPath(Path.Combine(codexHome, "skills"));
        }

        public override AgentClientTarget ResolveTarget(
            AgentClientContext context,
            AgentConfigScope scope)
        {
            ValidateContext(context);
            string codexHome = string.IsNullOrWhiteSpace(context.CodexHome)
                ? Path.Combine(context.UserHome, ".codex")
                : context.CodexHome;
            string path = scope == AgentConfigScope.UserGlobal
                ? Path.Combine(codexHome, "config.toml")
                : Path.Combine(context.ProjectRoot, ".codex", "config.toml");
            return new AgentClientTarget
            {
                ClientId = Id,
                DisplayName = DisplayName,
                Scope = scope,
                Format = AgentConfigFormat.Toml,
                ConfigPath = Path.GetFullPath(path),
                TomlTableName = "mcp_servers.tuanjie",
                JsonObjectPath = null,
                JsonProjectPathSegmentIndex = -1,
                ReloadGuidance = "重新打开 Codex 任务，或运行 codex mcp list 检查注册状态。"
            };
        }
    }

    internal sealed class ClaudeCodeClientConfigurator : AgentClientConfiguratorBase
    {
        public override AgentClientId Id { get { return AgentClientId.ClaudeCode; } }
        public override string DisplayName { get { return "Claude Code"; } }

        public override string ResolveSkillRoot(AgentClientContext context)
        {
            ValidateContext(context);
            return Path.GetFullPath(
                Path.Combine(context.UserHome, ".claude", "skills"));
        }

        public override AgentClientTarget ResolveTarget(
            AgentClientContext context,
            AgentConfigScope scope)
        {
            ValidateContext(context);
            string path = Path.Combine(context.UserHome, ".claude.json");
            if (scope == AgentConfigScope.UserGlobal)
            {
                return JsonTarget(
                    scope,
                    path,
                    new[] { "mcpServers", "tuanjie" },
                    -1,
                    "重新启动 Claude Code 后使用 claude mcp list 或会话内 /mcp 检查。"
                );
            }
            return JsonTarget(
                scope,
                path,
                new[] { "projects", Path.GetFullPath(context.ProjectRoot), "mcpServers", "tuanjie" },
                1,
                "Claude Code local scope 仅对当前项目生效；重新进入项目后使用 /mcp 检查。"
            );
        }
    }

    internal sealed class CursorClientConfigurator : AgentClientConfiguratorBase
    {
        public override AgentClientId Id { get { return AgentClientId.Cursor; } }
        public override string DisplayName { get { return "Cursor"; } }

        public override string ResolveSkillRoot(AgentClientContext context)
        {
            ValidateContext(context);
            return Path.GetFullPath(
                Path.Combine(context.UserHome, ".cursor", "skills"));
        }

        public override AgentClientTarget ResolveTarget(
            AgentClientContext context,
            AgentConfigScope scope)
        {
            ValidateContext(context);
            string path = scope == AgentConfigScope.UserGlobal
                ? Path.Combine(context.UserHome, ".cursor", "mcp.json")
                : Path.Combine(context.ProjectRoot, ".cursor", "mcp.json");
            return JsonTarget(
                scope,
                path,
                new[] { "mcpServers", "tuanjie" },
                -1,
                "在 Cursor MCP 设置页刷新；安装了 Cursor Agent CLI 时也可运行 cursor-agent mcp list。"
            );
        }
    }

    internal sealed class QoderClientConfigurator : AgentClientConfiguratorBase
    {
        public override AgentClientId Id { get { return AgentClientId.Qoder; } }
        public override string DisplayName { get { return "Qoder"; } }

        public override string ResolveSkillRoot(AgentClientContext context)
        {
            ValidateContext(context);
            return Path.GetFullPath(
                Path.Combine(context.UserHome, ".qoder", "skills"));
        }

        public override AgentClientTarget ResolveTarget(
            AgentClientContext context,
            AgentConfigScope scope)
        {
            ValidateContext(context);
            string path = scope == AgentConfigScope.UserGlobal
                ? Path.Combine(context.UserHome, ".qoder", "settings.json")
                : Path.Combine(context.ProjectRoot, ".qoder", "settings.local.json");
            return JsonTarget(
                scope,
                path,
                new[] { "mcpServers", "tuanjie" },
                -1,
                "重启 Qoder，并在 MCP 设置页确认 tuanjie 已启用；项目目录必须已受信任。"
            );
        }
    }

    internal sealed class WorkBuddyClientConfigurator : AgentClientConfiguratorBase
    {
        public override AgentClientId Id { get { return AgentClientId.WorkBuddy; } }
        public override string DisplayName { get { return "WorkBuddy"; } }

        public override string ResolveSkillRoot(AgentClientContext context)
        {
            ValidateContext(context);
            // WorkBuddy 的 Skills 兼容目录沿用 CodeBuddy 目录名；MCP 配置仍属于 WorkBuddy。
            return Path.GetFullPath(
                Path.Combine(context.UserHome, ".codebuddy", "skills"));
        }

        public override AgentClientTarget ResolveTarget(
            AgentClientContext context,
            AgentConfigScope scope)
        {
            ValidateContext(context);
            string path = scope == AgentConfigScope.UserGlobal
                ? Path.Combine(context.UserHome, ".workbuddy", "mcp.json")
                : Path.Combine(context.ProjectRoot, ".workbuddy", "mcp.json");
            return JsonTarget(
                scope,
                path,
                new[] { "mcpServers", "tuanjie" },
                -1,
                "在 WorkBuddy 的 Plugins → MCP servers → Configure MCP 中刷新并检查状态。"
            );
        }
    }
}
