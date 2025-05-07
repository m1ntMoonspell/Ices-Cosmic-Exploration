using Dalamud.Game.ClientState.Conditions;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskAnimationLock
    {
        public static void Enqueue()
        {   
            if (SchedulerMain.AnimationLockAbandonState && (Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.ExecutingCraftingAction]))
            {
                IceLogging.Info("[Animation Lock] [Wait] We were in Animation Lock fix state and seem to be fixed. Reseting.", true);
                SchedulerMain.State = IceState.GrabMission;
                SchedulerMain.PossiblyStuck = 0;
                SchedulerMain.AnimationLockAbandonState = false;
            }
            else
            {
                if (EzThrottler.Throttle("Open Recipe", 500))
                    AddonHelper.OpenRecipeNote();
            }
        }
    }
}