using Dalamud.Game.ClientState.Conditions;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskScoreCheckCraft
    {
        public static void TryCheckScore()
        {
            if (SchedulerMain.AnimationLockAbandonState)
            {
                SchedulerMain.State = IceState.AnimationLock;
                return;
            }

            if (CosmicHelper.CurrentLunarMission == 0)
            {
                // this in theory shouldn't happen but going to add it just in case
                IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                SchedulerMain.State = IceState.GrabMission;
                return;
            }

            IceLogging.Debug($"[Score Checker] Current Scoring Mission Id: {CosmicHelper.CurrentLunarMission}", true);
            var currentMission = C.Missions.Single(x => x.Id == CosmicHelper.CurrentLunarMission);
            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                var (currentScore, silverScore, goldScore) = MissionHandler.GetCurrentScores();
                if (SchedulerMain.AnimationLockAbandonState && (!AddonHelper.IsAddonActive("WKSRecipeNotebook") || !AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft])
                {
                    IceLogging.Error("[Score Checker] Aborting mission");
                    MissionHandler.TurnIn(z, true);
                    return;
                }

                var enoughMain = MissionHandler.HaveEnoughMain();
                if (enoughMain == null)
                {
                    IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                    SchedulerMain.State = IceState.GrabMission;
                    return;
                }

                if (enoughMain.Value || SchedulerMain.State == IceState.AbortInProgress)
                {
                    uint targetLevel = 0;
                    if (currentMission.TurnInGold)
                        targetLevel = 3;
                    else if (currentMission.TurnInSilver)
                        targetLevel = 2;
                    else if (currentMission.TurnInASAP)
                        targetLevel = 1;
                    IceLogging.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal: {silverScore} | Gold Goal: {goldScore} | Target Level: {targetLevel}", true);

                    if (targetLevel == 3)
                    {
                        if (currentScore >= goldScore ||
                        (SchedulerMain.State == IceState.AbortInProgress && currentMission.TurnInSilver && currentScore > silverScore) ||
                        (SchedulerMain.State == IceState.AbortInProgress && currentMission.TurnInASAP))
                        {
                            MissionHandler.TurnIn(z);
                            return;
                        }
                    }
                    else if (targetLevel == 2)
                    {
                        if (currentScore >= silverScore ||
                        (SchedulerMain.State == IceState.AbortInProgress && currentMission.TurnInASAP))
                        {
                            MissionHandler.TurnIn(z);
                            return;
                        }
                    }
                    else if (targetLevel <= 1)
                    {
                        MissionHandler.TurnIn(z);
                        return;
                    }
                    else if (SchedulerMain.State == IceState.AbortInProgress)
                    {
                        MissionHandler.TurnIn(z, true);
                        return;
                    }
                }
                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions]) && !P.Artisan.GetEnduranceStatus())
                {
                    SchedulerMain.State = IceState.StartCraft;
                }
            }
            else
            {
                IceLogging.Debug("[Score Checker] Addon not ready or player is busy");
                CosmicHelper.OpenStellaMission();
            }
        }
    }
}
