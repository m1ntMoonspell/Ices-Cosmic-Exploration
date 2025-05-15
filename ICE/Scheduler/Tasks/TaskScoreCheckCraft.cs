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
                if (SchedulerMain.State == IceState.AbortInProgress || (SchedulerMain.AnimationLockAbandonState && (!AddonHelper.IsAddonActive("WKSRecipeNotebook") || !AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft]))
                {
                    IceLogging.Error("[Score Checker] Aborting mission");
                    MissionHandler.TurnIn(z, true);
                    return;
                }

                var (currentScore, silverScore, goldScore) = MissionHandler.GetCurrentScores();

                var enoughMain = MissionHandler.HaveEnoughMain();
                if (enoughMain == null)
                {
                    IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                    SchedulerMain.State = IceState.GrabMission;
                    return;
                }

                if (enoughMain.Value)
                {
                    if (IceLogging.ShouldLog())
                    {
                        IceLogging.Debug("[Score Checker] Score != 0", true);
                        IceLogging.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal : {silverScore} | Gold Goal: {goldScore}", true);
                        IceLogging.Debug($"[Score Checker] Is Turnin Asap Enabled?: {currentMission.TurnInASAP}", true);
                    }

                    if (currentMission.TurnInASAP)
                    {
                        IceLogging.Info("$[Score Checker] Turnin Asap was enabled, and true. Firing off", true);
                        MissionHandler.TurnIn(z);
                        return;
                    }

                    IceLogging.Debug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && {enoughMain.Value} && if TurninSilver is true: {currentMission.TurnInSilver}", true);

                    if (currentScore >= silverScore && currentMission.TurnInSilver)
                    {
                        IceLogging.Info($"[Score Checker] Silver was enabled, and you also meet silver threshold.", true);
                        MissionHandler.TurnIn(z);
                        return;
                    }

                    IceLogging.Debug($"[Score Checker] Seeing if Player not busy: {PlayerHelper.IsPlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}", true);
                    if (currentScore >= goldScore)
                    {
                        IceLogging.Info($"[Score Checker] Conditions for gold was met. Turning in", true);
                        MissionHandler.TurnIn(z);
                        return;
                    }
                }

                IceLogging.Debug($"[Score Checker] Player is in state: {string.Join(',', Svc.Condition.AsReadOnlySet().Select(x => x.ToString()))}", true);
                IceLogging.Debug($"[Score Checker] Artisan is busy?: {P.Artisan.IsBusy()}", true);
                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions]) && !P.Artisan.GetEnduranceStatus())
                {
                    IceLogging.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal: {silverScore} | Gold Goal: {goldScore}", true);
                    IceLogging.Debug("[Score Checker] Player is not busy but hasnt hit score, resetting state to try craft", true);
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
