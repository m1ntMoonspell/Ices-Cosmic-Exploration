using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskCrafting
    {
        internal static bool PossiblyStuck = false;
        private static ExcelSheet<Item>? ItemSheet;
        private static ExcelSheet<Recipe>? RecipeSheet;

        public static void TryEnqueueCrafts()
        {
            EnsureInit();
            if (CosmicHelper.CurrentLunarMission != 0)
                MakeCraftingTasks();
        }

        private static void EnsureInit()
        {
            ItemSheet ??= Svc.Data.GetExcelSheet<Item>(); // Only need to grab once, it won't change
            RecipeSheet ??= Svc.Data.GetExcelSheet<Recipe>(); // Only need to grab once, it won't change
        }

        internal static bool IsArtisanBusy()
        {
            if (!P.Artisan.IsBusy() && !P.Artisan.GetEnduranceStatus())
            {
                return true;
            }
            else
            {
                if (Throttles.OneSecondThrottle)
                    IceLogging.Debug("Waiting for Artisan to not be busy");
            }

            return false;
        }

        internal static void MakeCraftingTasks()
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
                IceLogging.Debug("[TaskCrafting | Current Score] We shouldn't be here, stopping and progressing", true);
                SchedulerMain.State = IceState.CheckScoreAndTurnIn;
                return;
            }


            if (!P.TaskManager.IsBusy) // ensure no pending tasks or manual craft while plogon enabled
            {
                PossiblyStuck = false;
                SchedulerMain.State = IceState.CraftInProcess;

                CosmicHelper.OpenStellaMission();

                var needPreCraft = false;
                var itemsToCraft = new Dictionary<ushort, Tuple<int, int>>();
                var preItemsToCraft = new Dictionary<ushort, Tuple<int, int>>();

                var mainCrafts = CosmicHelper.CurrentMoonRecipe.MainCraftsDict;
                var preCrafts = CosmicHelper.CurrentMoonRecipe.PreCraftDict;

                // Calculate if we need to do more than base amount of crafts
                int craftsDone = mainCrafts.Sum(main => PlayerHelper.GetItemCount((int)RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId, out var count) ? count : 0); // How many mains we made
                int craftsNeeded = mainCrafts.Sum(main => main.Value); // How many we need for mission
                int CraftMultipleMissionItems = (craftsDone / craftsNeeded) + 1; // How many whole sets (+1) of crafts we did
                IceLogging.Debug($"[Loop] Number: {CraftMultipleMissionItems} | Items Done: {craftsDone} | Items Needed: {craftsNeeded}");

                bool OOMMain = false;
                bool OOMSub = false;

                foreach (var main in mainCrafts)
                {
                    var itemId = RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                    var subItem = RecipeSheet.GetRow(main.Key).Ingredient[0].Value.RowId; // need to directly reference this in the future
                    var mainNeed = main.Value;
                    var subItemNeed = RecipeSheet.GetRow(main.Key).AmountIngredient[0].ToInt() * main.Value;

                    if(!PlayerHelper.GetItemCount((int) itemId, out var currentAmount))
                    {
                        IceLogging.Error($"Failed to get item count of {itemId} ({currentAmount})");
                        return;
                    }

                    if(!PlayerHelper.GetItemCount((int)subItem, out var currentSubItemAmount))
                    {
                        IceLogging.Error($"Failed to get sub item count of {subItem} ({currentSubItemAmount})");
                        return;
                    }

                    var mainItemName = ItemSheet.GetRow(itemId).Name.ToString();

                    IceLogging.Debug($"RecipeID: {main.Key}", true);
                    IceLogging.Debug($"ItemID: {itemId}", true);

                    if ((currentSubItemAmount / (subItemNeed / mainNeed)) == 0) // This should OOM only if not enough to craft a single Main
                    {
                        IceLogging.Error($"[OOM] Not enough to craft main item");
                        OOMMain = true; // All current 3x Main items share Sub items
                    }

                    IceLogging.Debug($"[Main Item(s)] Main ItemID: {itemId} [{mainItemName}] | Current Amount: {currentAmount} | RecipeId {main.Key}", true);
                    IceLogging.Debug($"[Main Item(s)] Required Items for Recipe: ItemID: {subItem} | Currently have: {currentSubItemAmount} | Amount Needed [Base]: {subItemNeed}", true);

                    // Increase how many crafts we want to have made if needed so we can reach Score Checker goals.
                    subItemNeed = subItemNeed * CraftMultipleMissionItems;
                    mainNeed = mainNeed * CraftMultipleMissionItems;

                    if (currentAmount < mainNeed)
                    {
                        subItemNeed = subItemNeed - currentAmount;

                        IceLogging.Debug($"[Main Item(s)] You currently don't have the required amount of item: {ItemSheet.GetRow(itemId).Name}]. Checking to see if you have enough pre-crafts", true);
                        if (currentSubItemAmount >= subItemNeed)
                        {
                            IceLogging.Debug($"[Main Item(s) You have the required amount to make the necessary amount of main items. Continuing on", true);
                            int craftAmount = mainNeed - currentAmount;
                            itemsToCraft.Add(main.Key, new(craftAmount, mainNeed));
                        }
                        else
                        {
                            int craftAmount = mainNeed - currentAmount;
                            itemsToCraft.Add(main.Key, new(craftAmount, mainNeed));
                            needPreCraft = true;
                        }
                    }
                }

                if (needPreCraft)
                {
                    if (PlayerHelper.GetItemCount(48233, out var count) && count == 0) // Subs only have Cosmo Containers as requirement.
                    {
                        IceLogging.Error($"[OOM] Not enough to craft sub item");
                        OOMSub = true;
                    }
                    else
                    {
                        IceLogging.Debug($"[Pre-craft Items] You need pre-craft items. Starting the process of finding pre-crafts", true);
                        foreach (var pre in preCrafts)
                        {
                            var itemId = RecipeSheet.GetRow(pre.Key).ItemResult.Value.RowId;
                            if (PlayerHelper.GetItemCount((int)itemId, out var currentAmount))
                            {

                                var PreCraftItemName = ItemSheet.GetRow(itemId).Name.ToString();
                                IceLogging.Debug($"[Pre-Crafts] Checking Pre-crafts to see if {itemId} [{PreCraftItemName}] has enough.", true);
                                IceLogging.Debug($"[Pre-Crafts] Item Amount: {currentAmount} | Goal Amount: {pre.Value} | RecipeId: {pre.Key}", true);
                                var goalAmount = pre.Value;

                                if (currentAmount < goalAmount)
                                {
                                    IceLogging.Debug($"[Pre-Crafts] Found an item that needs to be crafted: {itemId} | Item Name: {PreCraftItemName}", true);
                                    int craftAmount = goalAmount - currentAmount;
                                    preItemsToCraft.Add(pre.Key, new(craftAmount, goalAmount));
                                }
                            }
                            else
                            {
                                IceLogging.Error($"Failed to get item count of {itemId}");
                            }
                        }
                    }
                }
                #if DEBUG
                OOMMain = OOMMain || SchedulerMain.DebugOOMMain;
                OOMSub = OOMSub || SchedulerMain.DebugOOMSub;
                #endif

                if (OOMMain && (OOMSub || !needPreCraft) && CosmicHelper.CurrentLunarMission < 361) // We only OOM if both are true: 1) Main is OOM, 2) Either Sub is OOM and we somehow don't need PreCrafts.
                {
                    IceLogging.Error($"[OOM] Not enough to craft");
                    SchedulerMain.State = IceState.AbortInProgress;
                    return;
                }

                P.TaskManager.BeginStack(); // Enable stack mode


                P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.WaitForCrafts, "Change state to wait for crafts");

                if (preItemsToCraft.Count > 0)
                {
                    IceLogging.Debug("Queuing up pre-craft items", true);
                    foreach (var pre in preItemsToCraft)
                    {
                        var item = ItemSheet.GetRow(RecipeSheet.GetRow(pre.Key).ItemResult.RowId);
                        IceLogging.Debug($"[Craft] Adding precraft {pre}", true);
                        P.TaskManager.Enqueue(() => !P.Artisan.IsBusy());
                        P.TaskManager.Enqueue(() => Craft(pre.Key, pre.Value.Item1, item), "PreCraft item");
                        P.TaskManager.EnqueueDelay(2000); // Give artisan a moment before we track it.
                        P.TaskManager.Enqueue(() => WaitTillActuallyDone(), "Wait for item", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration()
                        {
                            TimeLimitMS = 240000, // 4 minute limit per craft
                        });
                        P.TaskManager.EnqueueDelay(250); // Post-craft delay between Synthesis and RecipeLog reopening
                    }
                }

                if (itemsToCraft.Count > 0)
                {
                    IceLogging.Debug("Queuing up main craft items", true);
                    foreach (var main in itemsToCraft)
                    {
                        var item = ItemSheet.GetRow(RecipeSheet.GetRow(main.Key).ItemResult.RowId);
                        IceLogging.Debug($"[Main Item(s)] Queueing up for {item.Name}", true);
                        IceLogging.Debug($"[Craft] Adding craft {main}", true);
                        P.TaskManager.Enqueue(() => !P.Artisan.IsBusy());
                        P.TaskManager.Enqueue(() => Craft(main.Key, main.Value.Item1, item), "Craft item");
                        P.TaskManager.EnqueueDelay(2000); // Give artisan a moment before we track it.
                        P.TaskManager.Enqueue(() => WaitTillActuallyDone(), "Wait for item", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration()
                        {
                            TimeLimitMS = 240000, // 4 minute limit per craft, maybe need to work out a reasonable time? experts more? maybe 1m 30s per item?
                        });
                        P.TaskManager.EnqueueDelay(250); // Post-craft delay between Synthesis and RecipeLog reopening
                    }
                }

                P.TaskManager.Enqueue(() =>
                {
                    IceLogging.Debug("Check score and turn in cause crafting is done.", true);
                    SchedulerMain.State = IceState.CheckScoreAndTurnIn;
                }, "Check score and turn in if complete");

                P.TaskManager.EnqueueStack();
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

        internal static void Craft(ushort id, int craftAmount, Item item)
        {
            // if (GenericHelpers.TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var m) && m.IsAddonReady)
            // {
            //     if (EzThrottler.Throttle("Selecting Item"))
            //     {
            //         if (!m.SelectedCraftingItem.Contains($"{item.Name}"))
            //         {
            //             foreach (var i in m.CraftingItems)
            //             {
            //                 if (i.Name.Contains(item.Name.ToString()))
            //                 {
            //                     IceLogging.Debug($"[Craft failsafe] Selecting item: {i.Name}", true);
            //                     i.Select();
            //                 }
            //                 else
            //                 {
            //                     continue;
            //                 }
            //             }
            //         }
            //     }
            // }

            IceLogging.Debug($"[Main Item(s)] Telling Artisan to use recipe: {id} | {craftAmount} for {item.Name}", true);
            P.Artisan.CraftItem(id, craftAmount);
        }

        internal static bool? WaitTillActuallyDone()
        {
            if (EzThrottler.Throttle("WaitTillActuallyDone", 1000))
            {
                if (TaskScoreCheck.AnimationLockAbandonState && (Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.ExecutingCraftingAction]))
                {
                    IceLogging.Info("[WaitTillActuallyDone] We were in Animation Lock fix state and seem to be fixed. Reseting.", true);
                    SchedulerMain.State = IceState.StartCraft;
                    TaskScoreCheck.AnimationLockAbandonState = false;
                    P.Artisan.SetStopRequest(true);
                    return true;
                }
              var (currentScore, silverScore, goldScore) = GetCurrentScores(); // some scoring checks
              var currentMission = C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission);

              var enoughMain = HaveEnoughMain();
              if (enoughMain == null || currentMission == null)
              {
                  IceLogging.Error($"Current mission is {CosmicHelper.CurrentLunarMission}, aborting");
                  SchedulerMain.State = IceState.GrabMission;
                  return false;
              }

              if (currentMission.TurnInSilver && currentScore >= silverScore && enoughMain.Value)
              {
                  IceLogging.Debug("[WaitTillActuallyDone] Silver wanted. Silver reached.", true);
                  P.Artisan.SetStopRequest(true);
                  return true;
              }
              else if (currentScore >= goldScore && enoughMain.Value)
              {
                  IceLogging.Debug("[WaitTillActuallyDone] Gold wanted. Gold reached.", true);
                  P.Artisan.SetStopRequest(true);
                  return true;
              }

              if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions]) && !P.Artisan.GetEnduranceStatus())
              {
                  IceLogging.Debug("[WaitTillActuallyDone] We seem to no longer be crafting", true);
                  return true;
              }
            }
            return false;
        }

        internal static bool? WaitingForCrafting()
        {
            if (Svc.Condition[ConditionFlag.NormalConditions])
            {
                return true;
            }

            return false;
        }

        internal static bool? HaveEnoughMain()
        {
            EnsureInit();

            IceLogging.Debug($"[Item(s) Check] Checking.");

            if (CosmicHelper.CurrentLunarMission == 0)
                return null;

            foreach (var main in CosmicHelper.CurrentMoonRecipe.MainCraftsDict)
            {
                var itemId = RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                var mainNeed = main.Value;
                PlayerHelper.GetItemCount((int)itemId, out var currentAmount);

                if (currentAmount < mainNeed)
                {
                    IceLogging.Debug($"[Item(s) Check] You currently don't have the required amount of item: {ItemSheet.GetRow(itemId).Name}.");
                    return false;
                }
            }

            IceLogging.Debug($"[Item(s) Check] You currently have the required amount of items.");
            return true;
        }
    }
}
