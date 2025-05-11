using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Threading;
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

        // Dummy values that way I can figure this out
        private static uint missionA = 1;
        private static uint missionB = 2;


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
                SchedulerMain.State = IceState.CheckScoreAndTurnIn;
                return;
            }

            if (!P.TaskManager.IsBusy)
            {
                CosmicHelper.OpenStellaMission();
                var currentMission = CosmicHelper.CurrentLunarMission;

                if (!P.Visland.IsRouteRunning())
                {
                    IceLogging.Info("Visland route is currently not running, setting it to run");
                    if (EzThrottler.Throttle("Starting Visland Route"))
                    {
                        var nodeSet = GatheringUtil.GatherMissionInfo[currentMission].NodeSet;
                        var base64 = GatheringUtil.VislandDict[nodeSet].VBase64;

                        IceLogging.Info($"Nodeset: {nodeSet} found, starting visland route");

                        P.Visland.StartRoute(base64, false);
                        P.Visland.SetRoutePaused(false);
                    }
                }

                if (Svc.Condition[ConditionFlag.Gathering])
                {
                    if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var gather) && gather.IsAddonReady)
                    {
                        var missionType = GatheringUtil.GatherMissionInfo[currentMission].Type;

                        if (missionType == 2) // Quantity Style Mission (Gather x amount of each item, gather more to get score)
                        {
                            var DictEntry = GatheringItemDict[currentMission].MinGatherItems;
                            bool hasAllItems = true;
                            int itemToGather = 0;

                            foreach (var item in DictEntry)
                            {
                                if (PlayerHelper.GetItemCount((int)item.Key, out int count) && count < item.Value)
                                {
                                    hasAllItems = false;
                                    itemToGather = count;
                                }
                            }

                            // Pull up status configs for Mission Type A
                            if (!Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                            {
                                foreach (var item in gather.GatheredItems)
                                {
                                    if (hasAllItems && item.ItemID != 0)
                                    {
                                        IceLogging.Info($"HasAllItems: {hasAllItems} \n" +
                                                        $"Found Item: {item.ItemID} | {item.ItemName}");
                                        if (EzThrottler.Throttle($"Gathering: {item.ItemName}"))
                                        {
                                            IceLogging.Info($"Telling it to gather: {item.ItemName}");
                                            item.Gather();
                                        }
                                        break;
                                    }
                                    else if (!hasAllItems && item.ItemID == itemToGather)
                                    {
                                        IceLogging.Info($"HasAllItems: {hasAllItems} \n" +
                                                        $"Found Item: {item.ItemID} | {item.ItemName}");
                                        if (EzThrottler.Throttle($"Gathering: {item.ItemName}"))
                                        {
                                            IceLogging.Info($"Telling it to gather: {item.ItemName}");
                                            item.Gather();
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // need to add a P.Taskmanager thing here post to make sure once you finish the gathering route, then check the score... 
            }
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
