using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskRepair
    {
        public static void GatherCheck()
        {
            var currentJob = PlayerHelper.GetClassJobId();

            if (CosmicHelper.GatheringJobList.Contains((int)currentJob))
            {
                if (C.SelfRepairGather && PlayerHelper.NeedsRepair(C.RepairPercent))
                {
                    P.TaskManager.Enqueue(() => OpenSelfRepair(), "Opening repair menu");
                    P.TaskManager.Enqueue(() => SelfRepair(), "Repairing self");
                }
            }
            P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.GrabMission);
        }

        internal unsafe static bool OpenSelfRepair()
        {
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("Repair", out var x) && GenericHelpers.IsAddonReady(x))
            {
                return true;
            }

            if (EzThrottler.Throttle("Opening Self Repair", 1000))
                ActionManager.Instance()->UseAction(ActionType.GeneralAction, 6);
            return false;
        }

        internal unsafe static bool SelfRepair()
        {
            if (!PlayerHelper.NeedsRepair(C.RepairPercent))
            {
                return true;
            }
            else if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("SelectYesno", out var addon) && GenericHelpers.IsAddonReady(addon))
            {
                if (FrameThrottler.Throttle("SelectYesnoThrottle", 300))
                {
                    Svc.Log.Debug("SelectYesno Callback");
                    ECommons.Automation.Callback.Fire(addon, true, 0);
                }
            }
            else if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("Repair", out var addon2) && GenericHelpers.IsAddonReady(addon2))
            {
                if (FrameThrottler.Throttle("GlobalTurnInGenericThrottle", 300))
                {
                    Svc.Log.Debug("Repair Callback");
                    ECommons.Automation.Callback.Fire(addon2, true, 0);
                }
            }
            return false;
        }
    }
}
