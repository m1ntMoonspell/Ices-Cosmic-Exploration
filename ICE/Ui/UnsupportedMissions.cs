using System.Collections.Generic;

namespace ICE.Ui
{
    /// <summary>
    /// IDs of missions that should be disabled and shown as unsupported.
    /// </summary>
    public static class UnsupportedMissions
    {
        public static readonly HashSet<uint> Ids = new HashSet<uint>
        {
            0, // blacklisted mission ID
            512, 513, 514, // CRP
            515, 516, 517, // BSM
            518, 519, 520, // ARM
            521, 522, 523, // GSM
            524, 525, 526, // LTW
            527, 528, 529, // WVR
            530, 531, 532, // ALC
            533, 534, 535  // CUL
        };
    }
}
