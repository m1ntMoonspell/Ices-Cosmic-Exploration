using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

internal static class MissionHandler
    {
        internal static bool? HaveEnoughMain()
        {
            if (CosmicHelper.CurrentLunarMission == 0)
                return null;
            if (CosmicHelper.CurrentMissionInfo.IsCriticalMission)
            {
                var (currentScore, _, _) = GetCurrentScores();
                if (currentScore == 0)
                    return false;
            }
            else
            {
                foreach (var main in CosmicHelper.CurrentMoonRecipe.MainCraftsDict)
                {
                    var itemId = ExcelHelper.RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                    var mainNeed = main.Value;
                    PlayerHelper.GetItemCount((int)itemId, out var currentAmount);

                    if (currentAmount < mainNeed)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        internal unsafe static (uint currentScore, uint silverScore, uint goldScore) GetCurrentScores()
        {
            uint currentScore = 0, silverScore = 0, goldScore = 0;
            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
                {
                if (CosmicHelper.CurrentMissionInfo.IsCriticalMission)
                {
                    
                    string _stages = MemoryHelper.ReadSeStringNullTerminated((nint)z.Addon->AtkValues[5].String.Value).GetText(); // Returns Current/Max - "0/2"
                    string[] _stagesSplit = _stages.Split('/');
                    currentScore = uint.Parse(_stagesSplit[0]);
                    silverScore = 1;
                    goldScore = 2;
                }
                else
                {
                    string _currentScore = MemoryHelper.ReadSeStringNullTerminated((nint)z.Addon->AtkValues[2].String.Value).GetText();
                    string _silverScore = MemoryHelper.ReadSeStringNullTerminated((nint)z.Addon->AtkValues[3].String.Value).GetText();
                    string _goldScore = MemoryHelper.ReadSeStringNullTerminated((nint)z.Addon->AtkValues[4].String.Value).GetText();

                    // Remove all non-digit characters before parsing
                    _currentScore = new string(_currentScore.Where(char.IsDigit).ToArray());
                    _silverScore = new string(_silverScore.Where(char.IsDigit).ToArray());
                    _goldScore = new string(_goldScore.Where(char.IsDigit).ToArray());

                    uint.TryParse(_currentScore, out currentScore);
                    uint.TryParse(_silverScore, out silverScore);
                    uint.TryParse(_goldScore, out goldScore);
                }
            }

            return (currentScore, silverScore, goldScore);
        }
        internal unsafe static void TurnIn(WKSMissionInfomation z, bool abortIfNoReport = false)
        {
            if (IceLogging.ShouldLog("Turning in item", 250))
            {
                P.Artisan.SetEnduranceStatus(false);
                var (currentScore, silverScore, goldScore) = GetCurrentScores();

                if (!(AddonHelper.IsAddonActive("WKSRecipeNotebook") || AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft])
                {
                    IceLogging.Error("[TurnIn] Unexpected error. Potential Crafting Animation Lock.");
#if DEBUG
                    IceLogging.Error($"[TurnIn] PossiblyStuck: {SchedulerMain.PossiblyStuck} | AnimationLockToggle {C.AnimationLockAbandon} | AnimationLockState {SchedulerMain.AnimationLockAbandonState}");
#endif
                    if (SchedulerMain.PossiblyStuck < 2 && C.AnimationLockAbandon)
                    {
                        SchedulerMain.PossiblyStuck += 1;
                    }
                    else if (SchedulerMain.PossiblyStuck >= 2 && C.AnimationLockAbandon)
                    {
                        SchedulerMain.AnimationLockAbandonState = true;
                        Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                        {
                            Message = "[ICE] Unexpected error. I might be Animation Locked. Trigger count: " + SchedulerMain.PossiblyStuck + " " +
                            (C.AnimationLockAbandon ? "Attempting experimental unstuck." : "Please enable Experimental unstuck to attempt unstuck."),
                            Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
                        });
                    }
                }
                if (GenericHelpers.TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var cr) && cr.IsAddonReady && currentScore < goldScore)
                {
                    IceLogging.Info("[Score Checker] Player is preparing to craft, trying to fix", true);
                    P.Artisan.SetStopRequest(true);
                    // cr.Addon->FireCallbackInt(-1);
                }
            }
            if (!SchedulerMain.AnimationLockAbandonState)
                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.NormalConditions] == true, new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 1000 });

            var config = abortIfNoReport ? new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 5000, AbortOnTimeout = false } : new();
            IceLogging.Info("[TurnIn] Attempting turnin", true);
            P.TaskManager.Enqueue(TurnInInternals, "Changing to grab mission", config);

            if (abortIfNoReport && C.StopOnAbort && !SchedulerMain.AnimationLockAbandonState)
            {
                SchedulerMain.StopBeforeGrab = true;
                Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                {
                    Message = "[ICE] Unexpected error. Insufficient materials. Stopping. You failed to reach your Score Target.\n" +
                    $"If you expect Mission ID {CosmicHelper.CurrentLunarMission} to not reach " + (C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission).TurnInSilver ? "Silver" : "Gold") +
                    " - please mark it as Silver/ASAP accordingly.\n" +
                    "If you were expecting it to reach the target, check your Artisan settings/gear.",
                    Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
                });
            }
            if ((abortIfNoReport || SchedulerMain.AnimationLockAbandonState) && CosmicHelper.CurrentLunarMission != 0)
            {
                SchedulerMain.Abandon = true;
                if (SchedulerMain.AnimationLockAbandonState)
                    P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.AnimationLock, "Animation Lock", config);
                P.TaskManager.Enqueue(TaskMissionFind.AbandonMission, "Aborting mission", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 5000 });
            }
        }

        private static bool? TurnInInternals()
        {
            if (CosmicHelper.CurrentLunarMission == 0)
            {
                TaskMissionFind.BlacklistedMission.Clear();
                SchedulerMain.State = IceState.GrabMission;
                return true;
            }

            if (C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission).Type == MissionType.Critical)
            {
                if (EzThrottler.Throttle("Interacting with checkpoint", 250) && Svc.Condition[ConditionFlag.NormalConditions])
                {
                    var gameObject = Utils.TryGetObjectCollectionPoint();
                    float gameObjectDistance = 100;
                    if (gameObject is not null)
                        gameObjectDistance = PlayerHelper.GetDistanceToPlayer(gameObject);
                    Utils.TargetgameObject(gameObject);
                    if (gameObjectDistance < 5)
                        Utils.InteractWithObject(gameObject);
                }
                return false;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                z.Report();
                return false;
            }
            else 
            {
                CosmicHelper.OpenStellaMission();
                return false;
            }
        }
    }