using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility.Timing;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Threading;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Delegates;
using static FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton;
using static ICE.Utilities.CosmicHelper;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskGather
    {
        /* Moreso laying the floorplans for all of this, because this is going to get messy w/o any pre-planning
         * First things first, there's several types of missions for gathering
         * -> Quantity Limited (Gather x amount on limited amount of nodes)
         * -> Quantity (Gather x amount, gather more for increased score)
         * -> Timed (Gather x amount in the time limit)
         * -> Chain (Increase score based on chain)
         * -> Gatherer's Boon (Increase score by hitting boon % chance)
         * -> Chain + Boon (Get score from chain nodes + boon % chance)
         * -> Collectables (This is going to be annoying)
         * -> Time Steller Reduction (???) (Assuming Collectables -> Reducing for score... fuck)
         * 
         * Which gives us... 8 different kinds to account for. gdi.
         * 
        */

        private static ExcelSheet<Item>? ItemSheet;

        public static void TryEnqueueGathering()
        {
            EnsureInit();
            if (CosmicHelper.CurrentLunarMission != 0)
                MakeGatheringTask();
        }

        // Ensures that the sheets are loaded properly
        private static void EnsureInit()
        {
            ItemSheet ??= Svc.Data.GetExcelSheet<Item>(); // Only need to grab once
        }

        internal static void MakeGatheringTask()
        {
            EnsureInit();
            var (currentScore, silverScore, goldScore) = GetCurrentScores();

            if (currentScore == 0 && silverScore == 0 && goldScore == 0)
            {
                IceLogging.Error("Failed to get scores on first attempt retrying");
                (currentScore, silverScore, goldScore) = GetCurrentScores();
                if (currentScore == 0 && silverScore == 0 && goldScore == 0)
                {
                    IceLogging.Error("Failed to get scores on second attempt retrying");
                    (currentScore, silverScore, goldScore) = GetCurrentScores();
                    if (currentScore == 0 && silverScore == 0 && goldScore == 0)
                    {
                        IceLogging.Error("Failed to get scores on third attempt aborting");
                        SchedulerMain.State = IceState.Idle;
                        return;
                    }
                }
            }

            if (currentScore >= goldScore)
            {
                IceLogging.Error("[TaskGathering | Current Score] We shouldn't be here, stopping and progressing");
                SchedulerMain.State = IceState.CraftCheckScoreAndTurnIn;
                return;
            }

            if (!P.TaskManager.IsBusy)
            {
                int currentIndex = SchedulerMain.currentIndex;

                CosmicHelper.OpenStellaMission();
                var currentMission = CosmicHelper.CurrentLunarMission;

                List<uint> MissionNodes = new List<uint>();
                foreach (var entry in GatheringUtil.MoonNodeInfoList)
                {
                    if (GatheringUtil.GatherMissionInfo[currentMission].NodeSet == entry.NodeSet)
                    {
                        MissionNodes.Add(entry.NodeId);
                    }
                }

                uint nodeId = MissionNodes[currentIndex];
                P.TaskManager.BeginStack(); // Enable stack mode

                P.TaskManager.Enqueue(() => PathToNode(nodeId), "Pathing to node");

                IGameObject? gameObject = null;
                P.TaskManager.Enqueue(() => PlayerHelper.IsPlayerNotBusy(), "Waiting for player to not be busy");
                P.TaskManager.Enqueue(() => Utils.TryGetObjectByDataId(nodeId, out gameObject), "Getting Objec by DataId");
                P.TaskManager.Enqueue(() => Utils.TargetgameObject(gameObject), "Targeting gameObject");
                P.TaskManager.Enqueue(() => InteractGather(gameObject), "Interacting with Object");

                P.TaskManager.Enqueue(() => GatheringAddonReady(), "Making sure gathering addon is ready");
                P.TaskManager.Enqueue(() => NormalGathering(currentMission), "Starting Gathering");
                P.TaskManager.Enqueue(() => UpdateIndex(MissionNodes), "Updating index value");
                P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.GatherScoreandTurnIn);

                P.TaskManager.EnqueueStack();
            }
        }

        /// <summary>
        /// Checks to see distance to the node. If you're to far away, will pathfind to it.
        /// </summary>
        /// <param name="id"></param>
        internal static bool? PathToNode(uint id)
        {
            Vector3 nodeLoc = GatheringUtil.MoonNodeInfoList.Where(x => x.NodeId == id).FirstOrDefault().LandZone;

            if (PlayerHelper.GetDistanceToPlayer(nodeLoc) > 2 && !P.Navmesh.IsRunning())
            {
                if (EzThrottler.Throttle("Throttling pathfind"))
                {
                    P.Navmesh.PathfindAndMoveTo(nodeLoc, false);
                }
            }
            else if (PlayerHelper.GetDistanceToPlayer(nodeLoc) < 2)
            {
                if (P.Navmesh.IsRunning())
                {
                    P.Navmesh.Stop();
                }

                return true;
            }

            return false;
        }

        internal unsafe static bool? InteractGather(IGameObject? gameObject)
        {
            if (Svc.Condition[ConditionFlag.Gathering])
            {
                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Trying to interact w/ node"))
                {
                    try
                    {
                        var gameObjectPointer = (GameObject*)gameObject.Address;
                        TargetSystem.Instance()->InteractWithObject(gameObjectPointer, false);
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Info($"InteractWithObject: Exception: {ex}");
                    }
                }
            }

            return false;
        }

        internal unsafe static bool? GatheringAddonReady()
        {
            if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var m) && m.IsAddonReady)
            {

                return true;
            }

            return false;
        }

        internal unsafe static bool? NormalGathering(uint currentMission)
        {
            if (Svc.Condition[ConditionFlag.Gathering])
            {
                if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var gather) && gather.IsAddonReady)
                {
                    uint Boon1 = 0;
                    uint Boon2 = 0;
                    uint Tidings = 0;
                    uint Yield1 = 0;
                    uint Yield2 = 0;
                    uint IntegInc = 0;
                    uint BonusInteg = 0;

                    if (PlayerHelper.GetClassJobId() == 17)
                    {
                        Boon1 = GatheringUtil.GathActionDict["BoonIncrease1"].BtnActionId;
                        Boon2 = GatheringUtil.GathActionDict["BoonIncrease2"].BtnActionId;
                        Tidings = GatheringUtil.GathActionDict["Tidings"].BtnActionId;
                        Yield1 = GatheringUtil.GathActionDict["YieldI"].BtnActionId;
                        Yield2 = GatheringUtil.GathActionDict["YieldII"].BtnActionId;
                        IntegInc = GatheringUtil.GathActionDict["IntegrityIncrease"].BtnActionId;
                        BonusInteg = GatheringUtil.GathActionDict["BonusIntegrityChance"].BtnActionId;
                    }
                    else if (PlayerHelper.GetClassJobId() == 16)
                    {
                        Boon1 = GatheringUtil.GathActionDict["BoonIncrease1"].MinActionId;
                        Boon2 = GatheringUtil.GathActionDict["BoonIncrease2"].MinActionId;
                        Tidings = GatheringUtil.GathActionDict["Tidings"].MinActionId;
                        Yield1 = GatheringUtil.GathActionDict["YieldI"].MinActionId;
                        Yield2 = GatheringUtil.GathActionDict["YieldII"].MinActionId;
                        IntegInc = GatheringUtil.GathActionDict["IntegrityIncrease"].MinActionId;
                        BonusInteg = GatheringUtil.GathActionDict["BonusIntegrityChance"].MinActionId;
                    }

                    var missionType = GatheringUtil.GatherMissionInfo[currentMission].Type;

                    if (missionType is 1 or 2 or 3) // Quantity Style Mission (Gather x amount of each item, gather more to get score)
                    {
                        var DictEntry = GatheringItemDict[currentMission].MinGatherItems;
                        bool hasAllItems = true;
                        uint itemToGather = 0;

                        foreach (var item in DictEntry)
                        {
                            if (PlayerHelper.GetItemCount((int)item.Key, out int count) && count < item.Value)
                            {
                                hasAllItems = false;
                                itemToGather = item.Key;
                            }
                        }

                        // Pull up status configs for Mission Type A
                        if (!Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                        {
                            var profileId = C.Missions.Where(x => x.Id == currentMission).FirstOrDefault().GatherSettingId;
                            var gBuffs = C.GatherSettings.Where(g => g.Id == profileId).FirstOrDefault();
                            IceLogging.Debug($"[Gathering] Profile ID: {profileId}", true);
                            IceLogging.Debug($"[Gathering] Boon Increase II: {gBuffs.Buffs.BoonIncrease2}", true);
                            IceLogging.Debug($"[Gathering] Boon Increase I: {gBuffs.Buffs.BoonIncrease1}", true);
                            IceLogging.Debug($"[Gathering] Tidings: {gBuffs.Buffs.TidingsBool}", true);
                            IceLogging.Debug($"[Gathering] Yield II: {gBuffs.Buffs.YieldII}", true);
                            IceLogging.Debug($"[Gathering] Yield I: {gBuffs.Buffs.YieldI}", true);
                            IceLogging.Debug($"[Gathering] Bonus Integrity: {gBuffs.Buffs.IntegrityBool}", true);
                            bool missingDur = gather.CurrentIntegrity < gather.TotalIntegrity;
                            IceLogging.Debug($"[Gathering] Missing Durability? {missingDur}", true);
                            bool useAction = false;

                            foreach (var item in gather.GatheredItems)
                            {
                                if (hasAllItems && item.ItemID != 0)
                                {
                                    int boonChance = item.BoonChance;
                                    IceLogging.Debug($"Boon Increase 2: {BoonIncrease2Bool(boonChance, gBuffs)} && Missing durability: {missingDur}");
                                    if (BoonIncrease2Bool(boonChance, gBuffs) && !missingDur)
                                    {
                                        IceLogging.Debug($"Should be activating buff...", true);
                                        useAction = true;
                                        if (EzThrottler.Throttle("Boon2 Action Usage"))
                                        {
                                            IceLogging.Debug("Activating Boon% 2");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Boon2);
                                        }
                                    }
                                    else if (BoonIncrease1Bool(boonChance, gBuffs) && !missingDur)
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Boon1 Action Usage"))
                                        {
                                            IceLogging.Debug("Activating Boon% 1");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Boon1);
                                        }
                                    }
                                    else if (TidingsBool(gBuffs))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Tidings Action Usage") && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Bonus Item from Tidings");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Tidings);
                                        }
                                    }
                                    else if (Yield2Bool(gBuffs))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Using Yield2 Action Usage") && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Kings Yield II [or equivelent]");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Yield2);
                                        }

                                    }
                                    else if (Yield1Bool(gBuffs))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Using Yield1 Action Usage") && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Kings Yield II [or equivelent]");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Yield1);
                                        }
                                    }
                                    else if (BonusIntegrityBool(missingDur))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Using Bonus Intregrity Usage"))
                                        {
                                            IceLogging.Debug("Activating Bonus Yield Button");
                                            ActionManager.Instance()->UseAction(ActionType.Action, BonusInteg);
                                        }
                                    }
                                    else if (IntegrityBool(missingDur, gBuffs))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Missing Dur, using action"))
                                        {
                                            IceLogging.Debug("Activing Integrity Increase Button [Hoping for bonus Integ]");
                                            ActionManager.Instance()->UseAction(ActionType.Action, IntegInc);
                                        }
                                    }

                                    if (!useAction)
                                    {
                                        IceLogging.Info($"HasAllItems: {hasAllItems} \n" +
                                                        $"Found Item: {item.ItemID} | {item.ItemName}", true);
                                        if (EzThrottler.Throttle($"Gathering: {item.ItemName}"))
                                        {
                                            IceLogging.Info($"Telling it to gather: {item.ItemName}");
                                            item.Gather();
                                        }
                                    }
                                    return false;
                                }
                                else if (!hasAllItems && item.ItemID == itemToGather)
                                {
                                    int boonChance = item.BoonChance;
                                    IceLogging.Debug($"Boon Increase 2: {BoonIncrease2Bool(boonChance, gBuffs)} && Missing durability: {missingDur}", true);
                                    if (BoonIncrease2Bool(boonChance, gBuffs) && !missingDur)
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Boon2 Action Usage"))
                                        {
                                            IceLogging.Debug("Activating Boon% 2");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Boon2);
                                        }
                                        return false;
                                    }
                                    else if (BoonIncrease1Bool(boonChance, gBuffs) && !missingDur)
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Boon1 Action Usage"))
                                        {
                                            IceLogging.Debug("Activating Boon% 1");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Boon1);
                                        }
                                    }
                                    else if (TidingsBool(gBuffs))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Tidings Action Usage") && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Bonus Item from Tidings");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Tidings);
                                        }
                                    }
                                    else if (Yield2Bool(gBuffs))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Using Yield2 Action Usage") && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Kings Yield II [or equivelent]");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Yield2);
                                        }

                                    }
                                    else if (Yield1Bool(gBuffs))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Using Yield1 Action Usage") && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Kings Yield II [or equivelent]");
                                            ActionManager.Instance()->UseAction(ActionType.Action, Yield1);
                                        }
                                    }
                                    else if (BonusIntegrityBool(missingDur))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Using Bonus Intregrity Usage"))
                                        {
                                            IceLogging.Debug("Activating Bonus Yield Button");
                                            ActionManager.Instance()->UseAction(ActionType.Action, BonusInteg);
                                        }
                                    }
                                    else if (IntegrityBool(missingDur, gBuffs))
                                    {
                                        useAction = true;
                                        if (EzThrottler.Throttle("Missing Dur, using action"))
                                        {
                                            IceLogging.Debug("Activing Integrity Increase Button [Hoping for bonus Integ]");
                                            ActionManager.Instance()->UseAction(ActionType.Action, IntegInc);
                                        }
                                    }

                                    if (!useAction)
                                    {
                                        IceLogging.Info($"HasAllItems: {hasAllItems} \n" +
                                                        $"Found Item: {item.ItemID} | {item.ItemName}", true);
                                        if (EzThrottler.Throttle($"Gathering: {item.ItemName}"))
                                        {
                                            IceLogging.Info($"Telling it to gather: {item.ItemName}");
                                            item.Gather();
                                        }
                                    }
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            else if (!Svc.Condition[ConditionFlag.Gathering])
            {
                // No longer in the gathering condition
                return true;
            }

            return false;
        }

        private static bool BoonIncrease1Bool(int boonChance, GatherBuffProfile gatherBuffs)
        {
            return gatherBuffs.Buffs.BoonIncrease1
                && boonChance < 100
                && PlayerHelper.HasStatusId(GatheringUtil.GathActionDict["BoonIncrease1"].StatusId)
                && PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["BoonIncrease1"].RequiredGp
                && PlayerHelper.GetGp() >= gatherBuffs.Buffs.BoonIncrease1Gp;
        }

        public static bool BoonIncrease2Bool(int boonChance, GatherBuffProfile gatherBuffs)
        {
            return gatherBuffs.Buffs.BoonIncrease2
                && boonChance < 100
                && !PlayerHelper.HasStatusId(GatheringUtil.GathActionDict["BoonIncrease2"].StatusId)
                && PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["BoonIncrease2"].RequiredGp
                && PlayerHelper.GetGp() >= gatherBuffs.Buffs.BoonIncrease2Gp;
        }

        public static bool TidingsBool(GatherBuffProfile gatherBuffs)
        {
            return gatherBuffs.Buffs.TidingsBool
                && !PlayerHelper.HasStatusId(GatheringUtil.GathActionDict["Tidings"].StatusId)
                && PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["Tidings"].RequiredGp
                && PlayerHelper.GetGp() >= gatherBuffs.Buffs.TidingsGp;
        }

        public static bool Yield1Bool(GatherBuffProfile gatherBuffs)
        {
            return gatherBuffs.Buffs.YieldI
                && !PlayerHelper.HasStatusId(GatheringUtil.GathActionDict["YieldI"].StatusId)
                && PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["YieldI"].RequiredGp
                && PlayerHelper.GetGp() >= gatherBuffs.Buffs.YieldIGp;
        }

        public static bool Yield2Bool(GatherBuffProfile gatherBuffs)    
        {
            return gatherBuffs.Buffs.YieldII
                && !PlayerHelper.HasStatusId(GatheringUtil.GathActionDict["YieldII"].StatusId)
                && PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["YieldII"].RequiredGp
                && PlayerHelper.GetGp() >= gatherBuffs.Buffs.YieldIIGp;
        }

        public static bool IntegrityBool(bool durMissing, GatherBuffProfile gatherBuffs)
        {
            return gatherBuffs.Buffs.BonusIntegrity
                && PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["IntegrityIncrease"].RequiredGp
                && PlayerHelper.GetGp() >= gatherBuffs.Buffs.BonusIntegrityGp
                && durMissing;
        }

        public static bool BonusIntegrityBool(bool durMissing)
        {
            return PlayerHelper.HasStatusId(GatheringUtil.GathActionDict["BonusIntegrityChance"].StatusId)
                && durMissing;
        }

        internal unsafe static bool? UpdateIndex(List<uint> MissionNodes)
        {
            if (SchedulerMain.currentIndex < MissionNodes.Count - 1)
            {
                IceLogging.Debug($"Mission count: {MissionNodes.Count}");
                IceLogging.Debug($"Current index: {SchedulerMain.currentIndex}. Adding +1 to it");
                SchedulerMain.currentIndex += 1;
                IceLogging.Debug($"New index value: {SchedulerMain.currentIndex}");
                return true;
            }
            else
            {
                IceLogging.Debug($"Resetting index value to 0");
                SchedulerMain.currentIndex = 0;
                return true;
            }
        }

        internal static bool? HaveEnoughMain()
        {
            EnsureInit();

            if (CurrentLunarMission == 0)
                return null;

            var DictEntry = GatheringItemDict[CurrentLunarMission].MinGatherItems;
            foreach (var item in DictEntry)
            {
                if (PlayerHelper.GetItemCount((int)item.Key, out int count) && count < item.Value)
                {
                    return false;
                }
            }

            return true;
        }

        internal static (uint currentScore, uint silverScore, uint goldScore) GetCurrentScores()
        {
            EnsureInit();
            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                var goldScore = CosmicHelper.CurrentMissionInfo.GoldRequirement;
                var silverScore = CosmicHelper.CurrentMissionInfo.SilverRequirement;

                string currentScoreText = AddonHelper.GetNodeText("WKSMissionInfomation", 27);
                currentScoreText = currentScoreText.Replace(",", ""); // English client comma's
                currentScoreText = currentScoreText.Replace(" ", ""); // French client spacing
                currentScoreText = currentScoreText.Replace(".", ""); // French client spacing
                if (uint.TryParse(currentScoreText, out uint tempScore))
                {
                    return (tempScore, silverScore, goldScore);
                }
                else
                {
                    return (0, silverScore, goldScore);
                }
            }

            return (0, 0, 0);
        }
    }
}
