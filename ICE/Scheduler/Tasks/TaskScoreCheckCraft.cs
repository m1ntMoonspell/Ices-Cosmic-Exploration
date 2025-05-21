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
            var (currentScore, silverScore, goldScore) = MissionHandler.GetCurrentScores();
            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                if (SchedulerMain.AnimationLockAbandonState && (!AddonHelper.IsAddonActive("WKSRecipeNotebook") || !AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft])
                {
                    IceLogging.Error("[Score Checker] Aborting mission");
                    MissionHandler.TurnIn(z, true);
                    return;
                }

                bool? enoughMain = MissionHandler.HaveEnoughMain();
                if (enoughMain == null)
                {
                    IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                    SchedulerMain.State = IceState.GrabMission;
                    return;
                }

                if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Craft) && CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Gather))
                {
                    var (craft, gather) = MissionHandler.HaveEnoughMainDual();
                    PlayerHelper.GetItemCount(48233, out int count);
                    if (gather)
                    {
                        SchedulerMain.State &= ~IceState.Gather;
                        return;
                    }
                    else if (count > 0 && !gather && !craft)
                    {
                        SchedulerMain.State |= IceState.Gather;
                        return;
                    }
                    else if (count == 0)
                    {
                        SchedulerMain.State |= IceState.AbortInProgress;
                    }
                }

                if (enoughMain.Value || SchedulerMain.State.HasFlag(IceState.AbortInProgress))
                {
                    uint targetLevel = 0;
                    if (currentMission.TurnInGold)
                        targetLevel = 3;
                    else if (currentMission.TurnInSilver)
                        targetLevel = 2;
                    else if (currentMission.TurnInASAP)
                        targetLevel = 1;
                    IceLogging.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal: {silverScore} | Gold Goal: {goldScore} | Target Level: {targetLevel} | Abort State: {SchedulerMain.State.HasFlag(IceState.AbortInProgress)}", true);

                    if (targetLevel == 3)
                    {
                        if (currentScore >= goldScore ||
                        (SchedulerMain.State.HasFlag(IceState.AbortInProgress) && currentMission.TurnInSilver && currentScore > silverScore) ||
                        (SchedulerMain.State.HasFlag(IceState.AbortInProgress) && currentMission.TurnInASAP))
                        {
                            IceLogging.Debug("[Score Checker] Gold was enabled, and you also meet gold threshold.", true);
                            MissionHandler.TurnIn(z);
                            return;
                        }
                    }
                    else if (targetLevel == 2)
                    {
                        if (currentScore >= silverScore ||
                        (SchedulerMain.State.HasFlag(IceState.AbortInProgress) && currentMission.TurnInASAP))
                        {
                            IceLogging.Debug("[Score Checker] Silver was enabled, and you also meet silver threshold.", true);
                            MissionHandler.TurnIn(z);
                            return;
                        }
                    }
                    else if (targetLevel <= 1)
                    {
                        IceLogging.Debug("[Score Checker] Turnin Asap was enabled, and true. Firing off", true);
                        MissionHandler.TurnIn(z);
                        return;
                    }
                    if (SchedulerMain.State.HasFlag(IceState.AbortInProgress))
                    {
                        IceLogging.Error("[Score Checker] Aborting mission");
                        MissionHandler.TurnIn(z, true);
                        return;
                    }
                }
                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.Gathering]) && !P.Artisan.GetEnduranceStatus())
                {
                    SchedulerMain.State &= ~IceState.ScoringMission;
                }
            }
            else
            {
                IceLogging.Debug("[Score Checker] Addon not ready or player is busy");
                CosmicHelper.OpenStellarMission();
            }
        }
    }
}
