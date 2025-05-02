using Dalamud.Game.ClientState.Conditions;
using ECommons.Throttlers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskScoreCheck
    {
        public static void TryCheckScore()
        {
            if(CurrentLunarMission == 0)
            {
                // this in theory shouldn't happen but going to add it just in case
                PluginDebug("[Score Checker] Current mission is 0, aborting");
                SchedulerMain.State = IceState.GrabMission;
                return;
            }


            PluginDebug($"Current Scoring Mission Id: {CurrentLunarMission}");
            var currentMission = C.Missions.Single(x => x.Id == CurrentLunarMission);

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                var (currentScore, silverScore, goldScore) = TaskCrafting.GetCurrentScores();

                if (currentScore != 0)
                {
                    if (LogThrottle)
                    {
                        PluginDebug("[Score Checker] Score != 0");

                        PluginDebug($"[Score Checker] Current Score: {currentScore} | Silver Goal : {silverScore} | Gold Goal: {goldScore}");

                        PluginDebug($"[Score Checker] Is Turnin Asap Enabled?: {currentMission.TurnInASAP}");
                    }

                    if (currentMission.TurnInASAP)
                    {
                        PluginDebug("$[Score Checker] Turnin Asap was enabled, and true. Firing off");
                        TurnIn(z);
                        return;
                    }

                    var enoughMain = TaskCrafting.HaveEnoughMain();
                    if(enoughMain == null)
                    {
                        PluginDebug("[Score Checker] Current mission is 0, aborting");
                        SchedulerMain.State = IceState.GrabMission;
                        return;
                    }

                    if (LogThrottle)
                        PluginDebug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && {enoughMain.Value} && if TurninSilver is true: {currentMission.TurnInSilver}");

                    if (currentScore >= silverScore && enoughMain.Value && currentMission.TurnInSilver)
                    {
                        PluginDebug($"Silver was enabled, and you also meet silver threshold. ");
                        TurnIn(z);
                        return;
                    }

                    if (LogThrottle)
                        PluginDebug($"[Score Checker] Seeing if Player not busy: {PlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}");
                    if (PlayerNotBusy() && currentScore >= goldScore)
                    {
                        PluginDebug($"[Score Checker] Conditions for gold was met. Turning in");
                        TurnIn(z);
                        return;
                    }
                }

                PluginDebug($"[Score Checker] Player is in state: {string.Join(',', Svc.Condition.AsReadOnlySet().Select(x => x.ToString()))}");
                PluginDebug($"[Score Checker] Artisan is busy?: {P.Artisan.IsBusy()}");
                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions]) && !P.Artisan.GetEnduranceStatus())
                {
                    PluginDebug("[Score Checker] Player is not busy but hasnt hit score, resetting state to try craft");
                    SchedulerMain.State = IceState.StartCraft;
                }
            }
            else
            {
                PluginDebug("[Score Checker] Addon not ready or player is busy");
                OpenStellaMission();
            }
        }

        private unsafe static void TurnIn(WKSMissionInfomation z)
        {
            if (EzThrottler.Throttle("Turning in item", 100))
            {
                P.Artisan.SetEnduranceStatus(false);
                var scores = TaskCrafting.GetCurrentScores();
                if (TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var cr) && cr.IsAddonReady && scores.currentScore < scores.goldScore)
                {
                    PluginDebug("[Score Checker] Player is preparing to craft, trying to fix");
                    cr.Addon->FireCallbackInt(-1);
                }
                P.TaskManager.EnqueueDelay(1500);

                P.TaskManager.Enqueue(TurnInInternals, "Changing to grab mission");
            }
        }

        private static bool? TurnInInternals()
        {
            if (CurrentLunarMission == 0)
            {
                SchedulerMain.State = IceState.GrabMission;
                return true;
            }

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                z.Report();
                return false;
            }

            return false;
        }
    }
}
