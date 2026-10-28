using UnityEngine;
using UnityEngine.AI;

namespace DungeonEscape.Navigation
{
    /// <summary>NavMesh ajan tipi ve alan adlari. Editor kurulumu ve calisma zamani ayni sabitleri kullanir.</summary>
    public static class NavAreas
    {
        public const string RatAgent = "Rat";
        public const string GuardAgent = "Guard";
        public const string RatTunnelArea = "RatTunnel";
        public const string WalkableArea = "Walkable";
        public const string NotWalkableArea = "Not Walkable";

        /// <summary>Ajan tipinin kimligi; bulunamazsa varsayilan (Humanoid) 0 doner.</summary>
        public static int AgentTypeId(string agentName)
        {
            for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
            {
                var settings = NavMesh.GetSettingsByIndex(i);
                if (NavMesh.GetSettingsNameFromID(settings.agentTypeID) == agentName)
                    return settings.agentTypeID;
            }
            Debug.LogWarning($"NavMesh ajan tipi bulunamadi: {agentName}");
            return 0;
        }

        /// <summary>Muhafizlar fare tunelini kullanamaz; fare her yeri kullanabilir.</summary>
        public static int GuardAreaMask => NavMesh.AllAreas & ~(1 << NavMesh.GetAreaFromName(RatTunnelArea));
    }
}
