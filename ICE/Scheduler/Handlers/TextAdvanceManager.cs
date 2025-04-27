using ECommons.EzSharedDataManager;
using System.Collections.Generic;

namespace ICE.Scheduler.Handlers
{
    internal static class TextAdvancedManager
    {
        private static bool WasChanged = false;
        internal static void Tick()
        {
            if (WasChanged)
            {
                if (!SchedulerMain.AreWeTicking)
                {
                    WasChanged = false;
                    UnlockTA();
                    PluginDebug($"TextAdvance unlocked");
                }
            }
            else
            {
                if (SchedulerMain.AreWeTicking)
                {
                    WasChanged = true;
                    LockTA();
                    PluginDebug($"TextAdvance locked");
                }
            }
        }
        internal static void LockTA()
        {
            if (EzSharedData.TryGet<HashSet<string>>("TextAdvance.StopRequests", out var data))
            {
                data.Add(Name);
            }
        }

        internal static void UnlockTA()
        {
            if (EzSharedData.TryGet<HashSet<string>>("TextAdvance.StopRequests", out var data))
            {
                data.Remove(Name);
            }
        }
    }
}

