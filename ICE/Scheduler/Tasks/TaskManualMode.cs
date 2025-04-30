using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal class TaskManualMode
    {
        public static unsafe uint CurrentLunarMission => WKSManager.Instance()->CurrentMissionUnitRowId;
        public static void ZenMode()
        {
            if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !IsAddonActive("WKSMissionInfomation") && CurrentLunarMission == 0)
            {
                if (C.StopNextLoop)
                {
                    SchedulerMain.DisablePlugin();
                    C.StopNextLoop = false;
                    C.Save();
                }
                else
                {
                    SchedulerMain.State = IceState.GrabMission;
                }
            }
        }
    }
}
