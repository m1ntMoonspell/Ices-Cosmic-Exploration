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
            5 // blacklisted mission ID
        };
    }
}
