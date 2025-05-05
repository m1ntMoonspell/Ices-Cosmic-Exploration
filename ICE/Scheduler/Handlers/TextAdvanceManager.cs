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
                if (SchedulerMain.State == IceState.Idle)
                {
                    WasChanged = false;
                    UnlockTA();
                    IceLogging.Debug($"TextAdvance unlocked", true);
                }
            }
            else
            {
                if (SchedulerMain.State != IceState.Idle)
                {
                    WasChanged = true;
                    LockTA();
                    IceLogging.Debug($"TextAdvance locked", true);
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

