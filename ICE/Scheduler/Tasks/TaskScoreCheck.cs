using Dalamud.Game.ClientState.Conditions;
using ECommons.Throttlers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskScoreCheck
    {
        public static void TryCheckScore()
        {
            PluginDebug($"Current Scoring Mission Id: {CurrentLunarMission}");

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady && Svc.Condition[ConditionFlag.NormalConditions])
            {
                var (currentScore, silverScore, goldScore) = TaskCrafting.GetCurrentScores();

                if (currentScore != 0)
                {
                    PluginDebug("[Score Checker] Score != 0");

                    PluginDebug($"[Score Checker] Current Score: {currentScore} | Silver Goal : {silverScore} | Gold Goal: {goldScore}");

                    PluginDebug($"[Score Checker] Is Turnin Asap Enabled?: {C.TurninASAP}");
                    if (C.TurninASAP)
                    {
                        PluginDebug("$[Score Checker] Turnin Asap was enabled, and true. Firing off");
                        TurnIn(z);
                    }

                    PluginDebug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && if TurninSilver is true: {C.TurninOnSilver}");
                    if (currentScore >= silverScore && C.TurninOnSilver)
                    {
                        PluginDebug($"Silver was enabled, and you also meet silver threshold. ");
                        TurnIn(z);
                    }

                    PluginDebug($"[Score Checker] Seeing if Player not busy: {PlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}");
                    if (PlayerNotBusy() && !Svc.Condition[ConditionFlag.PreparingToCraft])
                    {
                        PluginDebug($"[Score Checker] Conditions for gold was met. Turning in");
                        TurnIn(z);
                    }
                }
            }
            else if(Svc.Condition[ConditionFlag.PreparingToCraft] && !P.Artisan.IsBusy())
            {
                PluginDebug("[Score Checker] Player is not busy but hasnt hit score, resetting state to try craft");
                SchedulerMain.State = IceState.StartCraft;
            }
            else
            {
                PluginDebug("[Score Checker] Addon not ready or player is busy");
            }
        }

        private static void TurnIn(WKSMissionInfomation z)
        {
            if (EzThrottler.Throttle("Turning in item", 100))
            {
                z.Report();
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
