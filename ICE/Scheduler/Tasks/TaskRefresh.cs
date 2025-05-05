using ECommons.Automation;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal class TaskRefresh
    {
        public static void Enqueue()
        {
            P.TaskManager.Enqueue(() => CloseMissionWindow(), "Closing Mission Window");
            P.TaskManager.Enqueue(() => OpenMissionWindow(), "Opening Mission Window");
        }

        internal unsafe static bool? CloseMissionWindow()
        {
            if (!AddonHelper.IsAddonActive("WKSMission"))
                return true;

            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var m) && m.IsAddonReady)
            {
                if (EzThrottler.Throttle("Closing Mission Window"))
                    Callback.Fire(m.Base, true, 1);
            }

            return false;
        }

        internal unsafe static bool? OpenMissionWindow()
        {
            if (AddonHelper.IsAddonActive("WKSMission"))
                return true;

            if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var SpaceHud) && SpaceHud.IsAddonReady)
            {
                if (EzThrottler.Throttle("Opening the mission hud"))
                    SpaceHud.Mission();
            }

            return false;
        }
    }
}
