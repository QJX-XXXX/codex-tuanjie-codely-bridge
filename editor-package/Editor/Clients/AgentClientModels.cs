using System;

namespace QJX.CodexTuanjieBridge.Editor
{
    public enum AgentClientId
    {
        Codex,
        ClaudeCode,
        Cursor,
        Qoder,
        WorkBuddy
    }

    public enum AgentConfigScope
    {
        UserGlobal,
        CurrentProject
    }

    public enum AgentConfigFormat
    {
        Toml,
        Json
    }

    public sealed class AgentClientContext
    {
        public string ProjectRoot { get; set; }
        public string UserHome { get; set; }
        public string CodexHome { get; set; }
    }

    public sealed class AgentClientTarget
    {
        public AgentClientId ClientId { get; set; }
        public string DisplayName { get; set; }
        public AgentConfigScope Scope { get; set; }
        public AgentConfigFormat Format { get; set; }
        public string ConfigPath { get; set; }
        public string TomlTableName { get; set; }
        public string[] JsonObjectPath { get; set; }
        public int JsonProjectPathSegmentIndex { get; set; }
        public string ReloadGuidance { get; set; }

        public bool IsUserGlobal
        {
            get { return Scope == AgentConfigScope.UserGlobal; }
        }
    }

    public interface IAgentClientConfigurator
    {
        AgentClientId Id { get; }
        string DisplayName { get; }
        string ResolveSkillRoot(AgentClientContext context);
        AgentClientTarget ResolveTarget(
            AgentClientContext context,
            AgentConfigScope scope);
    }
}
