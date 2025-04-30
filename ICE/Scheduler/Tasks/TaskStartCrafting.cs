using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.Throttlers;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskStartCrafting
    {
        private static bool ManualSetEndurance = false;
        private static int ManualItemId = 0;
        private static int ManualItemAmount = 0;

        public static void Enqueue()
        {
            P.TaskManager.Enqueue(() => IsArtisanBusy(), "Checking to see if artisan is busy");

            Svc.Log.Debug("Artisan is not busy...");
            if (C.DelayGrab) // honestly the entire delaygrab seems unnessecary
            {
                P.TaskManager.EnqueueDelay(4000);
            }
            else
            {
                // P.TaskManager.EnqueueDelay(2000); // Not needed
            }

            // P.TaskManager.Enqueue(() => P.Artisan.SetEnduranceStatus(false), "Ensuring endurance is off", DConfig);
            P.TaskManager.Enqueue(() => StartCraftingOld(), "Starting old crafting mothod", DConfig);
            // P.TaskManager.Enqueue(StartCrafting, "Starting Crafting Process", DConfig);

            P.TaskManager.Enqueue(() => WaitingForCrafting(), "Waiting for you to not be in a crafting animation", DConfig);
        }

        internal static bool? IsArtisanBusy()
        {
            if (!P.Artisan.IsBusy() && !P.Artisan.GetEnduranceStatus())
            {
                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Waiting for Artisan to not be busy"))
                    PluginLog.Debug("Waiting for Artisan to not be busy");
            }

            return false;
        }

        internal unsafe static void SetArtisanEndurance(bool enable)
        {
            P.Artisan.SetEnduranceStatus(enable);
        }

        internal static bool? StartCraftingOld()
        {
            // this version is to be depreciated post artisan update. 

            var (currentScore, silverScore, goldScore) = GetCurrentScores();

            var itemSheet = Svc.Data.GetExcelSheet<Item>();

            if (ManualSetEndurance)
            {
                if (GetItemCount(ManualItemId) <= ManualItemAmount)
                {
                    PluginDebug("[Manual Endurance Endurance] Endurance mode was set manually, waiting for ItemCount to meet requirement");
                    return false;
                }
                else
                {
                    ManualSetEndurance = false;
                    ManualItemAmount = 0;
                    ManualItemId = 0;
                    PluginDebug($"[Manual Endurance] The item amount has been met, stopping the craft ");
                    return false;
                }
            }
            // else if ((P.Artisan.GetEnduranceStatus() == false && !IsAddonActive("Synthesis")) || !P.Artisan.IsBusy())
            else if (!IsAddonActive("Synthesis"))
            {
                if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !IsAddonActive("WKSMissionInfomation"))
                {
                    if (EzThrottler.Throttle("Opening Stellar Missions"))
                    {
                        PluginLog.Debug("Opening Mission Menu");
                        hud.Mission();
                    }
                }

                var RecipeSheet = Svc.Data.GetExcelSheet<Recipe>();
                var currentMission = CurrentLunarMission;
                bool needPreCraft = true;
                bool foundPreCraft = false;
                bool allCrafted = true;

                if (currentMission == 0)
                {
                    return true;
                }

                PluginDebug($"Current Mission: {currentMission} | Found Pre-Craft? {foundPreCraft}");

                // Score checker section. 
                foreach (var mainItem in MoonRecipies[currentMission].MainCraftsDict)
                {
                    var itemId = RecipeSheet.GetRow(mainItem.Key).ItemResult.Value.RowId;
                    var currentAmount = GetItemCount((int)itemId);
                    var goalAmount = mainItem.Value;
                    var baseGoal = goalAmount;
                    if (C.CraftMultipleMissionItems)
                    {
                        goalAmount = mainItem.Value * 2;
                    }
                    uint targetScore = C.TurninOnSilver ? silverScore : goldScore;

                    if (currentAmount < goalAmount && (currentAmount < baseGoal && currentScore != targetScore))
                    {
                        PluginDebug("Score checker can't be done, you still have items to craft");
                        allCrafted = false;
                        break;
                    }
                }

                if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady && allCrafted)
                {
                    (currentScore, silverScore, goldScore) = GetCurrentScores();

                    if (currentScore != 0)
                    {
                        PluginDebug("Score != 0");

                        PluginDebug($"[Score Checker] Current Score: {currentScore} | Silver Goal : {silverScore} | Gold Goal: {goldScore}");

                        PluginDebug($"[Score Checker] Is Turnin Asap Enabled?: {C.TurninASAP}");
                        if (C.TurninASAP)
                        {
                            PluginDebug("$[Score Checker] Turnin Asap was enabled, and true. Firing off");
                            P.Artisan.SetEnduranceStatus(false);
                            if (EzThrottler.Throttle("Turning in item", 100))
                            {
                                z.Report();
                                return false;
                            }
                        }

                        PluginDebug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && if TurninSilver is true: {C.TurninOnSilver}");
                        if (currentScore >= silverScore && C.TurninOnSilver)
                        {
                            PluginDebug($"Silver was enabled, and you also meet silver threshold. ");
                            P.Artisan.SetEnduranceStatus(false);
                            if (EzThrottler.Throttle("Turning in item", 100))
                            {
                                z.Report();
                                return false;
                            }
                        }

                        PluginDebug($"[Score Checker] Seeing if Player not busy: {PlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}");
                        if (PlayerNotBusy() && !Svc.Condition[ConditionFlag.PreparingToCraft])
                        {
                            PluginDebug($"[Score Checker] Conditions for gold was met. Turning in");
                            P.Artisan.SetEnduranceStatus(false);
                            if (EzThrottler.Throttle("Turning in item", 100))
                            {
                                z.Report();
                                return false;
                            }
                        }
                    }
                }

                foreach (var main in MoonRecipies[currentMission].MainCraftsDict)
                {
                    var itemId = RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                    var subItem = RecipeSheet.GetRow(main.Key).Ingredient[0].Value.RowId; // need to directly reference this in the future
                    var mainNeed = main.Value;
                    var subItemNeed = RecipeSheet.GetRow(main.Key).AmountIngredient[0].ToInt() * main.Value;
                    var currentAmount = GetItemCount((int)itemId);
                    var currentSubItemAmount = GetItemCount((int)subItem);
                    var mainItemName = Svc.Data.GetExcelSheet<Item>().GetRow(itemId).Name.ToString();

                    PluginDebug($"RecipeID: {main.Key}");
                    PluginDebug($"ItemID: {itemId}");

                    PluginDebug($"[Main Item(s)] Main ItemID: {itemId} [{mainItemName}] | Current Amount: {currentAmount} | RecipeId {main.Key}");
                    PluginDebug($"[Main Item(s)] Required Items for Recipe: ItemID: {subItem} | Currently have: {currentSubItemAmount} | Amount Needed [Base]: {subItemNeed}");
                    if (currentAmount == mainNeed || C.CraftMultipleMissionItems)
                    {
                        subItemNeed = subItemNeed * 2;
                        mainNeed = mainNeed * 2;
                    }

                    if (currentAmount < mainNeed)
                    {
                        subItemNeed = subItemNeed - currentAmount;

                        PluginDebug($"[Main Item(s)] You currently don't have the required amount of item: {Svc.Data.GetExcelSheet<Item>().GetRow(itemId).Name.ToString()}]. Checking to see if you have enough pre-crafts");
                        if (currentSubItemAmount >= subItemNeed)
                        {
                            PluginDebug($"[Main Item(s) You have the required amount to make the necessary amount of main items. Continuing on");
                            if (EzThrottler.Throttle("[Main Item(s)] Crafting Main Item(s)", 4000))
                            {
                                int craftAmount = mainNeed - currentAmount;
                                PluginDebug($"[Main Item(s)] Telling Artisan to use recipe: {main.Key} | {craftAmount} for {Svc.Data.GetExcelSheet<Item>().GetRow(itemId).Name.ToString()}]");
                                // P.Artisan.CraftItem(main.Key, craftAmount); // Just caused issues, failsafe is being forced instead
                                needPreCraft = false;
                                break;
                            }
                            else if (EzThrottler.GetRemainingTime("[Main Item(s)] Starting Main Craft") < 3950) // temporarily off of 3800
                            {
                                PluginWarning($"[Main-craft failsafe] It seems like artisan failed to start crafting the item. Starting failsafe mode");
                                if (TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var m) && m.IsAddonReady)
                                {
                                    if (!m.SelectedCraftingItem.Contains($"{mainItemName}"))
                                    {
                                        if (EzThrottler.Throttle("Selecting Item"))
                                        {
                                            foreach (var item in m.CraftingItems)
                                            {
                                                if (item.Name.Contains(mainItemName))
                                                {
                                                    PluginDebug($"[Main-craft failsafe] Selecting item: {mainItemName}");
                                                    item.Select();
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        PluginDebug($"[Main-craft failsafe] Starting Crafting Process (if not throttled)");
                                        if (EzThrottler.Throttle("Starting Backup Crafting Process"))
                                        {
                                            PluginDebug($"[Main-craft failsafe] Starting the backup crafting process");
                                            int craftAmount = mainNeed;
                                            m.NQItemInput();
                                            m.HQItemInput();
                                            P.Artisan.SetEnduranceStatus(true);
                                            ManualItemAmount = craftAmount;
                                            ManualItemId = (int)itemId;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (needPreCraft)
                {
                    PluginDebug($"[Pre-craft Items] you need pre-craft items. Starting the process of finding pre-crafts");
                    foreach (var pre in MoonRecipies[currentMission].PreCraftDict)
                    {
                        var itemId = RecipeSheet.GetRow(pre.Key).ItemResult.Value.RowId;
                        var currentAmount = GetItemCount((int)itemId);
                        var PreCraftItemName = Svc.Data.GetExcelSheet<Item>().GetRow(itemId).Name.ToString();
                        PluginDebug($"[Pre-Crafts] Checking Pre-crafts to see if {itemId} [{PreCraftItemName}] has enough.");
                        PluginDebug($"[Pre-Crafts] Item Amount: {currentAmount} | Goal Amount: {pre.Value} | RecipeId: {pre.Key}");
                        var goalAmount = pre.Value;

                        PluginDebug($"[Pre-Crafts] Craft x 2 items state: {C.CraftMultipleMissionItems}");
                        if (C.CraftMultipleMissionItems)
                        {
                            goalAmount = pre.Value * 2;
                        }

                        if (currentAmount < goalAmount)
                        {
                            if (EzThrottler.Throttle($"Starting pre-craft {pre.Key}", 4000))
                            {
                                PluginDebug($"[Pre-Crafts] Found an item that needs to be crafted: {itemId} | Item Name: {PreCraftItemName}");
                                int craftAmount = goalAmount - currentAmount;
                                PluginDebug($"[Pre-Crafts] Telling Artisan to craft: {pre.Key} with the amount: {craftAmount}");
                                P.Artisan.CraftItem(pre.Key, craftAmount);
                                break;
                            }
                            else if (EzThrottler.GetRemainingTime("[Main Item(s)] Starting Main Craft") < 3000)
                            {
                                PluginWarning($"[Pre-craft failsafe] It seems like artisan failed to start crafting the item. Starting failsafe mode");
                                if (TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var m) && m.IsAddonReady)
                                {
                                    if (!m.SelectedCraftingItem.Contains($"{PreCraftItemName}"))
                                    {
                                        if (EzThrottler.Throttle("Selecting Item"))
                                        {
                                            foreach (var item in m.CraftingItems)
                                            {
                                                if (item.Name.Contains(PreCraftItemName))
                                                {
                                                    PluginDebug($"[Pre-craft failsafe] Selecting {PreCraftItemName} in recipe log");
                                                    item.Select();
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        PluginDebug($"[Pre-craft failsafe] Starting Crafting Process (if not throttled)");
                                        if (EzThrottler.Throttle("Starting Backup Crafting Process", 1500))
                                        {
                                            PluginDebug($"[Pre-craft failsafe] Starting Backup Crafting process for: {PreCraftItemName}");
                                            int craftAmount = goalAmount;
                                            m.NQItemInput();
                                            m.HQItemInput();
                                            P.Artisan.SetEnduranceStatus(true);
                                            ManualItemAmount = craftAmount;
                                            ManualItemId = (int)itemId;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        internal static (uint currentScore, uint silverScore, uint goldScore) GetCurrentScores()
        {
            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                var goldScore = MissionInfoDict[CurrentLunarMission].GoldRequirement;
                var silverScore = MissionInfoDict[CurrentLunarMission].SilverRequirement;

                string currentScoreText = GetNodeText("WKSMissionInfomation", 27);
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

        internal static bool? WaitingForCrafting()
        {
            if (Svc.Condition[ConditionFlag.NormalConditions] && !Svc.Condition[ConditionFlag.Crafting]) // crafting condition here likely not needed
            {
                return true;
            }

            return false;
        }
    }
}
