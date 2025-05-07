using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using System.Threading.Tasks;

using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ECommons.GenericHelpers;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskAnimationLock
    {
        private static uint MissionId = 0;

        public static void Enqueue()
        {
            if (SchedulerMain.State != IceState.AnimationLock)
            {
                SchedulerMain.State = IceState.GrabMission;
                return;
            }
            if (SchedulerMain.AnimationLockAbandonState && (Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.ExecutingCraftingAction]))
            {
                IceLogging.Info("[Animation Lock] [Wait] We were in Animation Lock fix state and seem to be fixed. Reseting.", true);
                SchedulerMain.State = IceState.GrabMission;
                SchedulerMain.PossiblyStuck = 0;
                SchedulerMain.AnimationLockAbandonState = false;
                return;
            }
            else
            {
                P.TaskManager.Enqueue(TaskMissionFind.UpdateValues, "Updating Task Mission Values");
                P.TaskManager.Enqueue(TaskMissionFind.OpenMissionFinder, "Opening the Mission finder");
                P.TaskManager.Enqueue(TaskMissionFind.BasicMissionButton, "Selecting Basic Missions");
                P.TaskManager.Enqueue(FindResetMission, "Finding Basic Mission");
                P.TaskManager.Enqueue(GrabMission, "Grabbing the mission");
                P.TaskManager.Enqueue(async () =>
                {
                    if (EzThrottler.Throttle("Opening Steller Missions"))
                    {
                        if (CosmicHelper.CurrentLunarMission != 0)
                        {
                            if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !AddonHelper.IsAddonActive("WKSMissionInfomation"))
                            {
                                IceLogging.Debug("Opening Mission Menu", true);
                                hud.Mission();

                                while (!AddonHelper.IsAddonActive("WKSMissionInfomation"))
                                {
                                    IceLogging.Debug("Waiting for WKSMissionInfomation to be active");
                                    await Task.Delay(500);
                                }
                            }
                        }
                    }
                });
                P.TaskManager.Enqueue(AbandonMission, "Abandoning mission");
            }
        }

        internal unsafe static bool? FindResetMission()
        {
            if (EzThrottler.Throttle("FindResetMission"))
            {
                IceLogging.Debug($"[Animation Lock] Mission Name: {SchedulerMain.MissionName} | MissionId {MissionId}", true);

                if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    IceLogging.Debug("[Animation Lock] Found mission was false", true);
                    var currentClassJob = PlayerHelper.GetClassJobId();


                    if (!x.StellerMissions.Any(x => CosmicHelper.MissionInfoDict[x.MissionId].JobId == currentClassJob)) //Tryin to reroll but on wrong job list
                    {
                        if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                        {
                            if (EzThrottler.Throttle("Opening Mission Hud"))
                            {
                                hud.Mission();
                                Task.Delay(200);
                                hud.Mission();
                            }
                        }
                        IceLogging.Debug("[Animation Lock] Wrong class mission list, Restarting", true);
                        return false;
                    }

                    var rankToReset = 1;

                    foreach (var m in x.StellerMissions)
                    {
                        var missionEntry = CosmicHelper.MissionInfoDict.FirstOrDefault(e => e.Key == m.MissionId);

                        if (missionEntry.Value == null)
                            continue;

                        //IceLogging.Debug($"[Reset Mission Finder] Mission: {m.Name} | Mission rank: {missionEntry.Value.Rank} | Rank to reset: {rankToReset}", true);
                        if (missionEntry.Value.Rank == rankToReset)
                        {
                            IceLogging.Debug($"[Animation Lock] Setting SchedulerMain.MissionName = {m.Name}", true);
                            m.Select();
                            SchedulerMain.MissionName = m.Name;
                            MissionId = missionEntry.Key;

                            IceLogging.Debug($"[Animation Lock] Mission Name: {SchedulerMain.MissionName}", true);

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal unsafe static bool? GrabMission()
        {
            if (EzThrottler.Throttle("GrabMission", 250))
            {
                IceLogging.Debug($"[Animation Lock] Mission Name: {SchedulerMain.MissionName} | MissionId {MissionId}");
                if (TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
                {
                    string[] commenceStrings = ["選択したミッションを開始します。よろしいですか？", "Commence selected mission?", "Ausgewählte Mission wird gestartet.Fortfahren?", "Commencer la mission sélectionnée ?"];
                    if (commenceStrings.Any(select.Text.Contains) || !C.RejectUnknownYesno)
                    {
                        IceLogging.Debug($"[Animation Lock] Expected Commence window: {select.Text}", true);
                        select.Yes();
                    }
                    else
                    {
                        IceLogging.Error($"[Animation Lock] Unexpected Commence window: {select.Text}");
                        select.No();
                    }
                    return false;
                }
                else if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
                {
                    if (!CosmicHelper.MissionInfoDict.ContainsKey(MissionId))
                    {
                        IceLogging.Debug($"[Animation Lock] No values were found for mission id {MissionId}... which is odd. Stopping the process");
                        SchedulerMain.DisablePlugin();
                    }
                    else
                        Callback.Fire(x.Base, true, 13, MissionId); // Firing off to initiate quest
                }
                else if (!AddonHelper.IsAddonActive("WKSMission"))
                {
                    return true;
                }
            }
            return false;
        }
        internal unsafe static bool? AbandonMission()
        {
            if (CosmicHelper.CurrentLunarMission == 0)
                return true;
            else if (EzThrottler.Throttle("AbandonMission", 250))
            {
                if (TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
                {
                    string[] abandonStrings = ["受注中のミッションを破棄します。よろしいですか？", "Abandon mission?", "Aktuelle Mission abbrechen?", "Êtes-vous sûr de vouloir abandonner la mission en cours ?"];
                    if (abandonStrings.Any(select.Text.Contains) || !C.RejectUnknownYesno)
                    {
                        IceLogging.Debug($"[Abandoning Mission] Expected Abandon window: {select.Text}");
                        select.Yes();
                        return true;
                    }
                    else
                    {
                        IceLogging.Error($"[Abandoning Mission] Unexpected Abandon window: {select.Text}");
                        select.No();
                        return false;
                    }
                }
                if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var addon) && addon.IsAddonReady)
                {
                    IceLogging.Debug("[Abandoning Mission] Attempting Abandon.");
                    addon.Abandon();
                }
                else if (TryGetAddonMaster<WKSHud>("WKSHud", out var SpaceHud) && SpaceHud.IsAddonReady)
                {
                    IceLogging.Debug("[Abandoning Mission] WKSMissionInformation missing. Attempting opening.");
                    SpaceHud.Mission();
                }
            }
            return false;
        }
    }
}