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
        public bool DelayGrabMission { get; set; } = true;
        public bool DelayCraft { get; set; } = true;
        public int DelayIncrease { get; set; } = 500;
        public int DelayCraftIncrease { get; set; } = 2500;
        public int PossiblyStuck = 0;
        public bool AnimationLockAbandon { get; set; } = true;
#if DEBUG
        public bool FailsafeRecipeSelect = false;
#endif

        // Mission settings
        public bool OnlyGrabMission { get; set; } = false;
        public bool StopOnceHitCosmoCredits { get; set; } = false;
        public bool StopOnceHitLunarCredits { get; set; } = false;
        public int CosmoCreditsCap { get; set; } = 30000;
        public int LunarCreditsCap { get; set; } = 10000;
        public byte SequenceMissionPriority { get; set; } = 1;
        public byte WeatherMissionPriority { get; set; } = 2;
        public byte TimedMissionPriority { get; set; } = 3;

        public int TargetLevel { get; set; } = 10;
        public bool StopWhenLevel { get; set; } = false;

        // Overlay settings
        public bool ShowOverlay { get; set; } = false;
        public bool ShowSeconds { get; set; } = false;

        // Table settings
        public bool HideUnsupportedMissions { get; set; } = false;
        public bool AutoPickCurrentJob { get; set; } = false;
        public int TableSortOption = 0;
        public bool ShowExpColums { get; set; } = true;
        public bool ShowCreditsColumn { get; set; } = true;

        // Gathering Settings
        public bool SelfRepairGather {  get; set; } = true;
        public int RepairPercent { get; set; } = 50;
        public int SelectedGatherIndex { get; set; } = 0;
        public List<GatherBuffProfile> GatherSettings { get; set; } = new()
        {
            new GatherBuffProfile { Id = 0, Name = "Default"},
            new GatherBuffProfile { Id = 1, Name = "Limited Nodes"},
            new GatherBuffProfile { Id = 2, Name = "Quantity"},
            new GatherBuffProfile { Id = 3, Name = "Time Attack"},
            new GatherBuffProfile { Id = 4, Name = "Chained"},
            new GatherBuffProfile { Id = 5, Name = "Gather's Boon"},
            new GatherBuffProfile { Id = 6, Name = "Chain + Boon"},
            new GatherBuffProfile { Id = 7, Name = "Collectables"},
            new GatherBuffProfile { Id = 8, Name = "Steller Reduction"},
            new GatherBuffProfile { Id = 9, Name = "Dual Class"},
        };

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
        public bool ManualMode { get; set; } = false;
        public int GatherSettingId { get; set; } = 0;
        [JsonIgnore]
        public GatherBuffProfile GatherSetting => C.GatherSettings.FirstOrDefault(x => x.Id == GatherSettingId)
                                          ?? C.GatherSettings[0]; // fallback to default
        public string TurnInMode;
    }

    public class GatherBuffs
    {
        public bool BoonIncrease2 { get; set; } = false;
        public int BoonIncrease2Gp { get; set; } = 100;
        public bool BoonIncrease1 { get; set; } = false;
        public int BoonIncrease1Gp { get; set; } = 50;
        public bool TidingsBool { get; set; } = false;
        public int TidingsGp { get; set; } = 200;
        public bool YieldII { get; set; } = false;
        public int YieldIIGp { get; set; } = 500;
        public bool YieldI { get; set; } = false;
        public int YieldIGp { get; set; } = 400;
        public bool BonusIntegrity { get; set; } = false;
        public int BonusIntegrityGp { get; set; } = 300;
        public bool IntegrityBool { get; set; } = true;
    }

    public class GatherBuffProfile
    {
        public int Id { get; set; } // Index being set for quick reference
        public string Name { get; set; } = ""; // Name, moreso for Ui
        public GatherBuffs Buffs { get; set; } = new();
    }

    public enum MissionType
    {
        Standard = 0,
        Sequential = 1,
        Weather = 2,
        Timed = 3,
        Critical = 4
    }//
}