using ECommons.EzSharedDataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Scheduler.Handlers
{
    internal static class YesAlreadyManager
    {
        private static bool WasChanged = false;
        internal static void Tick()
        {
            if (WasChanged)
            {
                if (SchedulerMain.State == IceState.Idle)
                {
                    WasChanged = false;
                    Unlock();
                    PluginDebug($"YesAlready unlocked");
                }
            }
            else
            {
                if (SchedulerMain.State != IceState.Idle)
                {
                    WasChanged = true;
                    Lock();
                    PluginDebug($"YesAlready locked");
                }
            }
        }
        internal static void Lock()
        {
            if (EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var data))
            {
                data.Add(Name);
            }
        }

        internal static void Unlock()
        {
            if (EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var data))
            {
                data.Remove(Name);
            }
        }
    }
}
