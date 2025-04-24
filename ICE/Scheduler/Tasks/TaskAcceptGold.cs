using ECommons.Logging;
using ECommons.Throttlers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskAcceptGold
    {
        public static void Enqueue()
        {
            P.taskManager.Enqueue(() => TurninGold(), "Waiting for valid turnin", DConfig);
            P.taskManager.Enqueue(() => PluginLog.Information("Turnin Complete"));
        }

        internal unsafe static bool? TurninGold()
        {
            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var x) && x.IsAddonReady)
            {
                if (x.GoldScore >= x.CurrentScore && PlayerNotBusy())
                {
                    if (EzThrottler.Throttle("Turning in item"))
                    {
                        x.Report();
                        return true;
                    }
                }
            }

            return false;
        }

        internal unsafe static bool? LeaveTurnin()
        {
            if (!IsAddonActive("WKSMissionInfomation"))
            {
                return true;
            }

            return false; //
        }
    }
}
