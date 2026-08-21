using System;
using System.Collections.Generic;

namespace QJX.CodexTuanjieBridge.Editor
{
    public static class AgentClientRegistry
    {
        private static readonly IReadOnlyList<IAgentClientConfigurator> Clients =
            new IAgentClientConfigurator[]
            {
                new CodexClientConfigurator(),
                new ClaudeCodeClientConfigurator(),
                new CursorClientConfigurator(),
                new QoderClientConfigurator(),
                new WorkBuddyClientConfigurator()
            };

        public static IReadOnlyList<IAgentClientConfigurator> All
        {
            get { return Clients; }
        }

        public static IAgentClientConfigurator Get(AgentClientId id)
        {
            for (int index = 0; index < Clients.Count; index++)
            {
                if (Clients[index].Id == id)
                {
                    return Clients[index];
                }
            }
            throw new ArgumentOutOfRangeException("id", id, "不支持的 Agent 客户端。");
        }
    }
}
