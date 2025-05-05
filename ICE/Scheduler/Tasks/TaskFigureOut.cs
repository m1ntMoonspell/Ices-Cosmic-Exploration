using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskFigureOut
    {
        public static void Enqueue()
        {
            P.TaskManager.Enqueue(() => InMission());
        }

        internal unsafe static bool? InMission()
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var mission) && mission.IsAddonReady)
            {
                SchedulerMain.inMission = false;
                return true;
            }


            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var info) && info.IsAddonReady)
            {
                SchedulerMain.inMission = true;
                return true;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
            {
                if (EzThrottler.Throttle("Opening Mission Hud", 1000))
                {
                    hud.Mission();
                }
            }

            return false;
        }
    }
}
