using Dalamud.Game.ClientState.Conditions;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskScoreCheck
    {
        public static void TryCheckScore()
        {
            if (CurrentLunarMission == 0)
            {
                // this in theory shouldn't happen but going to add it just in case
                PluginLog.Debug("[Score Checker] Current mission is 0, aborting");
                SchedulerMain.State = IceState.GrabMission;
                return;
            }

            PluginLog.Debug($"Current Scoring Mission Id: {CurrentLunarMission}");
            var currentMission = C.Missions.Single(x => x.Id == CurrentLunarMission);

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                if (SchedulerMain.State == IceState.AbortInProgress)
                {
                    PluginWarning("[Score Checker] Aborting mission");
                    TurnIn(z, true);
                    return;
                }

                var (currentScore, silverScore, goldScore) = TaskCrafting.GetCurrentScores();

                if (currentScore != 0)
                {
                    if (LogThrottle)
                    {
                        PluginLog.Debug("[Score Checker] Score != 0");

                        PluginLog.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal : {silverScore} | Gold Goal: {goldScore}");

                        PluginLog.Debug($"[Score Checker] Is Turnin Asap Enabled?: {currentMission.TurnInASAP}");
                    }

                    if (currentMission.TurnInASAP)
                    {
                        PluginLog.Debug("$[Score Checker] Turnin Asap was enabled, and true. Firing off");
                        TurnIn(z);
                        return;
                    }

                    var enoughMain = TaskCrafting.HaveEnoughMain();
                    if (enoughMain == null)
                    {
                        PluginLog.Debug("[Score Checker] Current mission is 0, aborting");
                        SchedulerMain.State = IceState.GrabMission;
                        return;
                    }

                    if (LogThrottle)
                        PluginLog.Debug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && {enoughMain.Value} && if TurninSilver is true: {currentMission.TurnInSilver}");

                    if (currentScore >= silverScore && enoughMain.Value && currentMission.TurnInSilver)
                    {
                        PluginLog.Debug($"Silver was enabled, and you also meet silver threshold. ");
                        TurnIn(z);
                        return;
                    }

                    if (LogThrottle)
                        PluginLog.Debug($"[Score Checker] Seeing if Player not busy: {PlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}");
                    if (currentScore >= goldScore)
                    {
                        PluginLog.Debug($"[Score Checker] Conditions for gold was met. Turning in");
                        TurnIn(z);
                        return;
                    }
                }

                PluginLog.Debug($"[Score Checker] Player is in state: {string.Join(',', Svc.Condition.AsReadOnlySet().Select(x => x.ToString()))}");
                PluginLog.Debug($"[Score Checker] Artisan is busy?: {P.Artisan.IsBusy()}");
                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions]) && !P.Artisan.GetEnduranceStatus())
                {
                    PluginLog.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal: {silverScore} | Gold Goal: {goldScore}");
                    PluginLog.Debug("[Score Checker] Player is not busy but hasnt hit score, resetting state to try craft");
                    SchedulerMain.State = IceState.StartCraft;
                }
            }
            else
            {
                PluginLog.Debug("[Score Checker] Addon not ready or player is busy");
                OpenStellaMission();
            }
        }

        private unsafe static void TurnIn(WKSMissionInfomation z, bool abortIfNoReport = false)
        {
            if (EzThrottler.Throttle("Turning in item", 100))
            {
                P.Artisan.SetEnduranceStatus(false);
                var scores = TaskCrafting.GetCurrentScores();
                if (TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var cr) && cr.IsAddonReady && scores.currentScore < scores.goldScore)
                {
                    PluginLog.Debug("[Score Checker] Player is preparing to craft, trying to fix");
                    cr.Addon->FireCallbackInt(-1);
                }
                // P.TaskManager.EnqueueDelay(1500);
                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.NormalConditions] == true);

                var config = abortIfNoReport ? new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 5000, AbortOnTimeout = false } : new();

                P.TaskManager.Enqueue(TurnInInternals, "Changing to grab mission", config);

                if (abortIfNoReport && C.StopOnAbort)
                {
                    SchedulerMain.StopBeforeGrab = true;
                    Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                    {
                        Message = "[ICE] Unexpected error. Insufficient materials. Stopping. You failed to reach your Score Target.\n" +
                        $"If you expect Mission ID {CurrentLunarMission} to not reach " + (C.Missions.SingleOrDefault(x => x.Id == CurrentLunarMission).TurnInSilver ? "Silver" : "Gold") +
                        " - please mark it as Silver/ASAP accordingly.\n" +
                        "If you were expecting it to reach the target, check your Artisan settings/gear.",
                        Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
                    });
                }
                if (abortIfNoReport && CurrentLunarMission != 0)
                {
                    SchedulerMain.Abandon = true;
                    SchedulerMain.State = IceState.GrabMission;
                    P.TaskManager.Enqueue(TaskMissionFind.AbandonMission, "Aborting mission", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 5000 });
                }
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
