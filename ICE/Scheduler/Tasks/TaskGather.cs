using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ICE.Utilities;
using System.Collections.Generic;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
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

        public static void TryEnqueueGathering()
        {
            if (CosmicHelper.CurrentLunarMission != 0)
            {
                Job targetClass;
                if (((Job)CosmicHelper.CurrentMissionInfo.JobId).IsDol())
                    targetClass = (Job)CosmicHelper.CurrentMissionInfo.JobId;
                else if (((Job)CosmicHelper.CurrentMissionInfo.JobId2).IsDol())
                    targetClass = (Job)CosmicHelper.CurrentMissionInfo.JobId2;
                else
                    return;
                if ((Job)PlayerHelper.GetClassJobId() != targetClass)
                    GearsetHandler.TaskClassChange(targetClass);
                else
                    MakeGatheringTask();
            }
        }

        // Version 2 of the gathering task. Trying to improve on it all...
        internal static void MakeGatheringTask()
        {
            var (currentScore, bronzeScore, silverScore, goldScore) = MissionHandler.GetCurrentScores();
            if (AddonHelper.GetNodeText("WKSMissionInfomation", 23).Contains("00:00"))
            {
                SchedulerMain.State |= IceState.AbortInProgress;
                return;
            }

            if (currentScore == 0 && silverScore == 0 && goldScore == 0)
            {
                IceLogging.Debug("Failed to get scores, aborting");
                return;
            }

            if (currentScore >= goldScore)
            {
                IceLogging.Error("[TaskGathering | Current Score] We shouldn't be here, stopping and progressing");
                SchedulerMain.State |= IceState.ScoringMission;
                return;
            }

            if (!P.TaskManager.IsBusy)
            {
                int currentIndex = SchedulerMain.CurrentIndex;

                CosmicHelper.OpenStellarMission();
                var currentMission = CosmicHelper.CurrentLunarMission;

                List<uint> MissionNodes = new List<uint>();

                foreach (var entry in SchedulerMain.CurrentNodeSet)
                {
                    if (MissionInfoDict[currentMission].NodeSet == entry.NodeSet)
                    {
                        MissionNodes.Add(entry.NodeId);
                    }
                }
                uint nodeId = MissionNodes[currentIndex];

                // Checking to make sure that you're not currently gathering
                if (!Svc.Condition[ConditionFlag.Gathering])
                {

                    Vector3 nodeLoc = GatheringUtil.MoonNodeInfoList.Where(x => x.NodeId == nodeId).FirstOrDefault().LandZone;
                    if (PlayerHelper.GetDistanceToPlayer(nodeLoc) > 1.5f)
                    {
                        // Seen that the distance between you and the node is greater than 2, pathfinding
                        P.TaskManager.Enqueue(() => PathToNode(nodeLoc), "Pathing to node");
                        return;
                    }
                    else
                    {
                        IGameObject? gameObject = null;
                        Utils.TryGetObjectByDataId(nodeId, out gameObject);
                        if (gameObject != null)
                        {
                            // Game object has been found. Now time to check stuff on it.
                            if (gameObject.IsTargetable)
                            {
                                P.TaskManager.Enqueue(() => Utils.TargetgameObject(gameObject), "Targeting gameObject");
                                P.TaskManager.Enqueue(() => InteractGather(gameObject), "Interacting with Object");
                                P.TaskManager.Enqueue(() => GatheringAddonReady(), "Making sure gathering addon is ready");
                                return;
                            }
                            else
                            {
                                P.TaskManager.Enqueue(() => UpdateIndex(MissionNodes), "Increasing the index by 1");
                                return;
                            }
                        }
                    }
                }
                else if (Svc.Condition[ConditionFlag.Gathering])
                {
                    // Probably not necessary to check, but also helps me keep track of what this should be

                    // Check for addon to make sure it's visible (master)
                    // Check to see if buffs need applied
                    //   -> If yes, then run a task to apply said buff (might be best to just make this a generic task to save some sanity...
                    //      ->  Make sure to return post this so it doesn't try and continue to run 
                    //   -> If no, continue on
                    // Make a task to gather an item, make it return true when you enter the gatheringaction state
                    // Wait for you to exit gatherActionState
                    if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var x) && x.IsAddonReady)
                    {
                        uint Boon1 = 0;
                        uint Boon2 = 0;
                        uint Tidings = 0;
                        uint Yield1 = 0;
                        uint Yield2 = 0;
                        uint IntegInc = 0;
                        uint BonusInteg = 0;
                        uint BYieldII = 0;

                        if (PlayerHelper.GetClassJobId() == 17)
                        {
                            Boon1 = GatheringUtil.GathActionDict["BoonIncrease1"].BtnActionId;
                            Boon2 = GatheringUtil.GathActionDict["BoonIncrease2"].BtnActionId;
                            Tidings = GatheringUtil.GathActionDict["Tidings"].BtnActionId;
                            Yield1 = GatheringUtil.GathActionDict["YieldI"].BtnActionId;
                            Yield2 = GatheringUtil.GathActionDict["YieldII"].BtnActionId;
                            IntegInc = GatheringUtil.GathActionDict["IntegrityIncrease"].BtnActionId;
                            BonusInteg = GatheringUtil.GathActionDict["BonusIntegrityChance"].BtnActionId;
                            BYieldII = GatheringUtil.GathActionDict["BountifulYieldII"].BtnActionId;
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
                            BYieldII = GatheringUtil.GathActionDict["BountifulYieldII"].MinActionId;
                        }

                        bool Collectable = MissionInfoDict[currentMission].Attributes.HasFlag(MissionAttributes.Collectables);
                        bool Reducable = MissionInfoDict[currentMission].Attributes.HasFlag(MissionAttributes.ReducedItems);

                        if (!(Collectable || Reducable) && x.TotalIntegrity != 0)
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

                            if (!Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                            {
                                var profileId = C.Missions.Where(x => x.Id == currentMission).FirstOrDefault().GatherSettingId;
                                var gBuffs = C.GatherSettings.Where(g => g.Id == profileId).FirstOrDefault();
                                bool missingDur = x.CurrentIntegrity < x.TotalIntegrity;
                                bool useAction = false;

                                foreach (var item in x.GatheredItems)
                                {
                                    if (hasAllItems && item.ItemID != 0)
                                    {
                                        #nullable disable
                                        int boonChance = item.BoonChance;
                                        if (BoonIncrease2Bool(boonChance, gBuffs) && !missingDur)
                                        {
                                            IceLogging.Debug($"Should be activating buff...", true);
                                            useAction = true;
                                            if (EzThrottler.Throttle("Boon2 Action Usage"))
                                            {
                                                IceLogging.Debug("Activating Boon% 2");
                                                GatherBuffs(Boon2);
                                            }
                                            return;
                                        }
                                        else if (BoonIncrease1Bool(boonChance, gBuffs) && !missingDur)
                                        {
                                            useAction = true;
                                            if (EzThrottler.Throttle("Boon1 Action Usage"))
                                            {
                                                IceLogging.Debug("Activating Boon% 1");
                                                GatherBuffs(Boon1);
                                            }
                                            return;
                                        }
                                        else if (TidingsBool(gBuffs))
                                        {
                                            useAction = true;
                                            if (EzThrottler.Throttle("Tidings Action Usage") && !missingDur)
                                            {
                                                IceLogging.Debug("Activating Bonus Item from Tidings");
                                                GatherBuffs(Tidings);
                                            }
                                            return;
                                        }
                                        else if (Yield2Bool(gBuffs))
                                        {
                                            useAction = true;
                                            if (EzThrottler.Throttle("Using Yield2 Action Usage") && !missingDur)
                                            {
                                                IceLogging.Debug("Activating Kings Yield II [or equivelent]");
                                                GatherBuffs(Yield2);
                                            }
                                            return;
                                        }
                                        else if (Yield1Bool(gBuffs))
                                        {
                                            useAction = true;
                                            if (EzThrottler.Throttle("Using Yield1 Action Usage") && !missingDur)
                                            {
                                                IceLogging.Debug("Activating Kings Yield II [or equivelent]");
                                                GatherBuffs(Yield1);
                                            }
                                            return;
                                        }
                                        else if (BYield2Bool(gBuffs))
                                        {
                                            useAction = true;
                                            if (EzThrottler.Throttle("Using Bountiful Yield Action"))
                                            {
                                                IceLogging.Debug("Activating Bountiful Yield/Harvest II");
                                                GatherBuffs(BYieldII);
                                            }
                                        }
                                        else if (BonusIntegrityBool(missingDur))
                                        {
                                            useAction = true;
                                            if (EzThrottler.Throttle("Using Bonus Intregrity Usage"))
                                            {
                                                IceLogging.Debug("Activating Bonus Yield Button");
                                                GatherBuffs(BonusInteg);
                                            }
                                            return;
                                        }
                                        else if (IntegrityBool(missingDur, gBuffs))
                                        {
                                            useAction = true;
                                            if (EzThrottler.Throttle("Missing Dur, using action"))
                                            {
                                                IceLogging.Debug("Activing Integrity Increase Button [Hoping for bonus Integ]");
                                                GatherBuffs(IntegInc);
                                            }
                                            return;
                                        }
                                        else
                                        {
                                            IceLogging.Info($"HasAllItems: {hasAllItems} \n" +
                                                $"Found Item: {item.ItemID} | {item.ItemName}", true);
                                            if (EzThrottler.Throttle($"Gathering: {item.ItemName}"))
                                            {
                                                IceLogging.Info($"Telling it to item: {item.ItemName}");
                                                GatherItem(item);
                                            }
                                        }
                                    }
                                    else if (!hasAllItems && item.ItemID == itemToGather)
                                    {
                                        IceLogging.Debug($"[Condition F] Mission is aiming to gather: {itemToGather}", true);

                                        int boonChance = item.BoonChance;
                                        if (BoonIncrease2Bool(boonChance, gBuffs) && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Boon% 2");
                                            useAction = true;
                                            if (EzThrottler.Throttle("Boon2 Action Usage"))
                                            {
                                                GatherBuffs(Boon2);
                                            }
                                            return;
                                        }
                                        else if (BoonIncrease1Bool(boonChance, gBuffs) && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Boon% 1");
                                            useAction = true;
                                            if (EzThrottler.Throttle("Boon1 Action Usage"))
                                            {
                                                GatherBuffs(Boon1);
                                            }
                                            return;
                                        }
                                        else if (TidingsBool(gBuffs))
                                        {
                                            IceLogging.Debug("Activating Bonus Item from Tidings");
                                            useAction = true;
                                            if (EzThrottler.Throttle("Tidings Action Usage") && !missingDur)
                                            {
                                                GatherBuffs(Tidings);
                                            }
                                            return;
                                        }
                                        else if (Yield2Bool(gBuffs) && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Kings Yield II [or equivelent]");
                                            useAction = true;
                                            if (EzThrottler.Throttle("Using Yield2 Action Usage") && !missingDur)
                                            {
                                                GatherBuffs(Yield2);
                                            }
                                            return;
                                        }
                                        else if (Yield1Bool(gBuffs) && !missingDur)
                                        {
                                            IceLogging.Debug("Activating Kings Yield I [or equivelent]");
                                            useAction = true;
                                            if (EzThrottler.Throttle("Using Yield1 Action Usage") && !missingDur)
                                            {
                                                GatherBuffs(Yield1);
                                            }
                                            return;
                                        }
                                        else if (BYield2Bool(gBuffs))
                                        {
                                            IceLogging.Debug("Activating Bountiful Yield/Harvest II");
                                            useAction = true;
                                            if (EzThrottler.Throttle("Using Bountiful Yield Action"))
                                            {
                                                GatherBuffs(BYieldII);
                                            }
                                        }
                                        else if (BonusIntegrityBool(missingDur))
                                        {
                                            IceLogging.Debug("Activating Bonus Yield Button");
                                            useAction = true;
                                            if (EzThrottler.Throttle("Using Bonus Intregrity Usage"))
                                            {
                                                GatherBuffs(BonusInteg);
                                            }
                                            return;
                                        }
                                        else if (IntegrityBool(missingDur, gBuffs))
                                        {
                                            IceLogging.Debug("Activing Integrity Increase Button [Hoping for bonus Integ]");
                                            useAction = true;
                                            if (EzThrottler.Throttle("Missing Dur, using action"))
                                            {
                                                GatherBuffs(IntegInc);
                                            }
                                            return;
                                        }
                                        else
                                        {
                                            IceLogging.Info($"HasAllItems: {hasAllItems} \n" +
                                                $"Found Item: {item.ItemID} | {item.ItemName}", true);
                                            if (EzThrottler.Throttle($"Gathering: {item.ItemName}"))
                                            {
                                                IceLogging.Info($"Telling it to item: {item.ItemName}");
                                                GatherItem(item);
                                            }
                                        }
                                    }
#nullable enable
                                }
                            }
                        }
                        else if (x.CurrentIntegrity == 0)
                        {
                            P.TaskManager.Enqueue(() => IntegrityCheck(x));
                        }

                    }

                }

                // Check the score
                P.TaskManager.Enqueue(() => SchedulerMain.State |= IceState.ScoringMission, "Checking score");
            }
        }

        /// <summary>
        /// Checks to see distance to the node. If you're to far away, will pathfind to it.
        /// </summary>
        /// <param name="id"></param>
        internal static bool? PathToNode(Vector3 nodeLoc)
        {
            if (PlayerHelper.GetDistanceToPlayer(nodeLoc) > 1.5f && !P.Navmesh.IsRunning())
            {
                if (EzThrottler.Throttle("Throttling pathfind"))
                {
                    P.Navmesh.PathfindAndMoveTo(nodeLoc, false);
                }
            }
            else if (PlayerHelper.GetDistanceToPlayer(nodeLoc) < 1.5f)
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
            if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var m) && m.IsAddonReady && !Svc.Condition[ConditionFlag.ExecutingGatheringAction])
            {
                return true;
            }

            return false;
        }

        private unsafe static void GatherBuffs(uint actionId)
        {
            bool? UseBuffs()
            {
                if (Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                    return true;

                if (EzThrottler.Throttle($"Trying to activate buff: {actionId}"))
                {
                    ActionManager.Instance()->UseAction(ActionType.Action, actionId);
                }

                return false;
            }

            P.TaskManager.Enqueue(() => UseBuffs(), "Applying buffs to character");
            P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction], "Waiting for gather buffs");
        }

        private unsafe static void GatherItem(Gathering.GatheredItem item)
        {
            bool? HasAttemptedGathering()
            {
                if (Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                    return true;

                if (EzThrottler.Throttle($"Attempting to item item: {item.ItemName}"))
                {
                    item.Gather();
                }

                return false;
            }

            P.TaskManager.Enqueue(() => HasAttemptedGathering(), "Gathering Item Task");
            P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction], "Waiting for gathering attempt");
        }

        private static bool BoonIncrease1Bool(int boonChance, GatherBuffProfile gatherBuffs)
        {
            return gatherBuffs.Buffs.BoonIncrease1
                && boonChance < 100
                && !PlayerHelper.HasStatusId(GatheringUtil.GathActionDict["BoonIncrease1"].StatusId)
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

        public static bool BYield2Bool(GatherBuffProfile gatherBuffs)
        {
            return gatherBuffs.Buffs.BountifulYieldII
                   && !PlayerHelper.HasStatusId(GatheringUtil.GathActionDict["BountifulYieldII"].StatusId)
                   && PlayerHelper.GetGp() >= GatheringUtil.GathActionDict["BountifulYieldII"].RequiredGp
                   && PlayerHelper.GetGp() >= gatherBuffs.Buffs.BountifulYieldIIGp;
        }

        internal unsafe static bool? UpdateIndex(List<uint> MissionNodes)
        {
            if (SchedulerMain.CurrentIndex < MissionNodes.Count - 1)
            {
                IceLogging.Debug($"Mission count: {MissionNodes.Count}");
                IceLogging.Debug($"Current index: {SchedulerMain.CurrentIndex}. Adding +1 to it");
                SchedulerMain.CurrentIndex += 1;
                SchedulerMain.NodesVisited += 1;
                IceLogging.Debug($"New index value: {SchedulerMain.CurrentIndex}");
            }
            else
            {
                IceLogging.Debug($"Resetting index value to 0");
                SchedulerMain.CurrentIndex = 0;
            }
            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Limited)
                && SchedulerMain.NodesVisited >= SchedulerMain.PreviousNodeSet.Count)
                SchedulerMain.State |= IceState.AbortInProgress;
            SchedulerMain.State |= IceState.ScoringMission;
            return true;
        }

        internal static bool? IntegrityCheck(Gathering x)
        {
            if (Svc.Condition[ConditionFlag.Gathering])
            {
                if (x.IsAddonReady && x.CurrentIntegrity != 0)
                {
                    return true;
                }
            }
            else if (PlayerHelper.IsPlayerNotBusy())
            {
                return true;
            }

            return false;
        }
    }
}
