using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskAcceptGold
    {
        public static void Enqueue()
        {
            P.TaskManager.Enqueue(() => TurninGold(), "Waiting for valid turnin", Utils.TaskConfig);
            P.TaskManager.Enqueue(() => IceLogging.Info("Turnin Complete"));
        }

        internal unsafe static bool? TurninGold()
        {
            uint currentScore, goldScore;
            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var x) && x.IsAddonReady)
            {
                currentScore = x.CurrentScore;
                goldScore = x.GoldScore;
                bool scorecheck = currentScore != 0 && goldScore != 0;

                if (goldScore <= currentScore && PlayerHelper.IsPlayerNotBusy() && scorecheck)
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
            if (!AddonHelper.IsAddonActive("WKSMissionInfomation"))
            {
                return true;
            }

            return false;
        }
    }
}
