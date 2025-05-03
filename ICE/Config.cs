using System.Collections.Generic;
using System.Text.Json.Serialization;
using ECommons.Configuration;

namespace ICE
{
    public class Config : IEzConfig
    {
        [JsonIgnore]
        public const int CurrentConfigVersion = 1;

        // Missions the user has enabled
        public List<CosmicMission> Missions { get; set; } = [];

        // Safety settings
        public bool StopOnAbort { get; set; } = true;
        public bool RejectUnknownYesno { get; set; } = true;

        // Mission settings
        public bool OnlyGrabMission { get; set; } = false;
        public bool StopOnceHitCosmoCredits { get; set; } = false;
        public bool StopOnceHitLunarCredits { get; set; } = false;

        // Overlay settings
        public bool ShowOverlay { get; set; } = false;
        public bool ShowSeconds { get; set; } = false;

        // Table settings
        public bool HideUnsupportedMissions { get; set; } = false;
        public bool AutoPickCurrentJob { get; set; } = false;
        public int TableSortOption = 0;
        public bool ShowExpColums { get; set; } = true;
        public bool ShowCreditsColumn { get; set; } = true;

        // Misc settings
        public bool EnableAutoSprint { get; set; } = true;

        public void Save()
        {
            EzConfig.Save();
        }
    }

    public class CosmicMission
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public MissionType Type { get; set; } = MissionType.Standard;
        public bool Enabled { get; set; } = false;
        public uint PreviousMissionId { get; set; } = 0;
        public uint JobId { get; set; }
        public bool TurnInSilver { get; set; } = false;
        public bool TurnInASAP { get; set; } = false;
    }

    public enum MissionType
    {
        Standard = 0,
        Sequential = 1,
        Weather = 2,
        Timed = 3,
        Critical = 4
    }
}