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
        public List<(uint Id, string Name)> EnabledMission { get; set; } = new List<(uint, string)>();

        // Delay grabbing mission
        public bool DelayGrab { get; set; } = false;

        // Turnin options
        public bool TurninOnSilver { get; set; } = false;
        public bool TurninASAP { get; set; } = false;
        public bool CraftMultipleMissionItems = false;

        // Table settings
        public bool HideUnsupportedMissions = false;
        public bool TableSortByName = true;

        public void Save()
        {
            EzConfig.Save();
        }
    }
}