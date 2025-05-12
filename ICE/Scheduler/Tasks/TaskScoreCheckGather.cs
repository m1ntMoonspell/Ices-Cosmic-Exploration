using Dalamud.Game.ClientState.Conditions;
using ICE.Scheduler.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskScoreCheckGather
    {
        public static void TryCheckScore()
        {
            if (CosmicHelper.CurrentLunarMission == 0)
            {
                // this in theory shouldn't happen but going to add it just in case
                IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                SchedulerMain.State = IceState.GrabMission;
                return;
            }

            // Something to think about here:
            // Timed missions don't really have a way to turn them in quicker, it's just a matter of "did you complete it"
            // So might just need to make these always be "turnin if complete" to help save the headache.
            // Something else to consider is make it to where if you get to the node and it's not targetable, then check if turnin is possible -> turnin
            // and if not, then just abandon...

            IceLogging.Debug($"[Score Checker] Current Scoring Mission Id: {CosmicHelper.CurrentLunarMission}", true);
            var currentMission = C.Missions.Single(x => x.Id == CosmicHelper.CurrentLunarMission);

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                var (currentScore, silverScore, goldScore) = TaskGather.GetCurrentScores();
                var missionType = GatheringUtil.GatherMissionInfo[CosmicHelper.CurrentLunarMission].Type;

                if (currentScore != 0 && missionType is 1 or 2 or 4 or 5 or 6) // Base ones that have item counters
                {
                    if (IceLogging.ShouldLog())
                    {
                        IceLogging.Debug("[Score Checker] Score != 0", true);
                        IceLogging.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal : {silverScore} | Gold Goal: {goldScore}", true);
                        IceLogging.Debug($"[Score Checker] Is Turnin Asap Enabled?: {currentMission.TurnInASAP}", true);
                    }

                    var hasAllItems = TaskGather.HaveEnoughMain();
                    if (hasAllItems == null)
                    {
                        IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                        SchedulerMain.State = IceState.GrabMission;
                        return;
                    }

                    if (hasAllItems.Value && currentMission.TurnInASAP)
                    {
                        IceLogging.Info("$[Score Checker] Turnin Asap was enabled, and true. Firing off", true);
                        P.TaskManager.Enqueue(() => Turnin(), "Turning in ASAP");
                        return;
                    }

                    IceLogging.Debug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && {hasAllItems.Value} && if TurninSilver is true: {currentMission.TurnInSilver}", true);
                    if (currentScore >= silverScore && hasAllItems.Value && currentMission.TurnInSilver)
                    {
                        IceLogging.Info($"[Score Checker] Silver was enabled, and you also meet silver threshold.", true);
                        P.TaskManager.Enqueue(() => Turnin(), "Turning in Silver Reward");
                        return;
                    }

                    if (currentScore >= goldScore)
                    {
                        IceLogging.Info($"[Score Checker] Conditions for gold was met. Turning in", true);
                        P.TaskManager.Enqueue(() => Turnin(), "Turning in Gold Reward");
                        return;
                    }
                }
                else if (missionType is 3)
                {
                    var hasAllItems = TaskGather.HaveEnoughMain();
                    if (hasAllItems == null)
                    {
                        IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                        SchedulerMain.State = IceState.GrabMission;
                        return;
                    }

                    IceLogging.Debug($"[Score Checker] [Timed Missions] Have Enough Items? {hasAllItems.Value}");
                    if (hasAllItems.Value)
                    {
                        P.TaskManager.Enqueue(() => Turnin(), "Turning in on timed mission");
                        return;
                    }
                }

                if (PlayerHelper.IsPlayerNotBusy())
                {
                    IceLogging.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal: {silverScore} | Gold Goal: {goldScore}", true);
                    IceLogging.Debug($"[Score Checker] Player has not hit the score requirements, going back to the gathering mines", true);
                    SchedulerMain.State = IceState.GatherNormal;
                }
            }
            else
            {
                IceLogging.Debug("[Score Checker] Addon not ready or player is busy");
                CosmicHelper.OpenStellaMission();
            }
        }

        private unsafe static bool? Turnin ()
        {
            if (CosmicHelper.CurrentLunarMission == 0)
            {
                SchedulerMain.State = IceState.GrabMission;
                return true;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                //IceLogging.Debug("[Score Checker] REPORTING", true);
                z.Report();
            }
            else
            {
                CosmicHelper.OpenStellaMission();
            }

            return false;
        }
    }
}
