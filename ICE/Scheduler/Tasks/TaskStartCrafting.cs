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
        public static void Enqueue()
        {
            P.TaskManager.Enqueue(() => IsArtisanBusy(), "Checking to see if artisan is busy");

            Svc.Log.Debug("Artisan is not busy...");
            P.TaskManager.Enqueue(() => P.Artisan.SetEnduranceStatus(false), "Ensuring endurance is off", DConfig);
            P.TaskManager.Enqueue(() => StartCraftingOld(), "Starting old crafting mothod", DConfig);
            // P.TaskManager.Enqueue(StartCrafting, "Starting Crafting Process", DConfig);
            if (C.DelayGrab)
            {
                P.TaskManager.EnqueueDelay(1500);
            }
            else
            {
                P.TaskManager.EnqueueDelay(750);
            }
            P.TaskManager.Enqueue(() => WaitingForCrafting(), "Waiting for you to not be in a crafting animation", DConfig);
        }

        internal static bool? IsArtisanBusy()
        {
            if (!P.Artisan.IsBusy())
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

            if ((P.Artisan.GetEnduranceStatus() == false && !IsAddonActive("Synthesis")) || P.Artisan.IsBusy())
            {
                P.Artisan.SetEnduranceStatus(false);

                if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !IsAddonActive("WKSMissionInfomation"))
                {
                    if (EzThrottler.Throttle("Opening Steller Missions"))
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
                    if (currentAmount < goalAmount && (currentAmount < baseGoal && currentScore != goldScore))
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
                            if (EzThrottler.Throttle("Turning in item", 100))
                            {
                                z.Report();
                            }
                        }

                        PluginDebug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && if TurninSilver is true: {C.TurninOnSilver}");
                        if (currentScore >= silverScore && C.TurninOnSilver)
                        {
                            PluginDebug($"Silver was enabled, and you also meet silver threshold. ");
                            if (EzThrottler.Throttle("Turning in item", 100))
                            {
                                z.Report();
                            }
                        }

                        PluginDebug($"[Score Checker] Seeing if Player not busy: {PlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}");
                        if (PlayerNotBusy() && !Svc.Condition[ConditionFlag.PreparingToCraft])
                        {
                            PluginDebug($"[Score Checker] Conditions for gold was met. Turning in");
                            if (EzThrottler.Throttle("Turning in item"))
                            {
                                z.Report();
                            }
                        }
                    }
                }

                foreach (var main in MoonRecipies[currentMission].MainCraftsDict)
                {
                    var itemId = RecipeSheet.GetRow(main.Key).ItemRequired.Value.RowId;
                    var subItem = RecipeSheet.GetRow(main.Key).Ingredient[0].Value.RowId; // need to directly reference this in the future
                    var mainNeed = main.Value;
                    var subItemNeed = RecipeSheet.GetRow(main.Key).AmountIngredient[0].ToInt() * main.Value;
                    var currentAmount = GetItemCount((int)itemId);
                    var currentSubItemAmount = GetItemCount((int)subItem);

                    PluginDebug($"[Main Item(s)] Main ItemID: {itemId} | Current Amount: {currentAmount} | RecipeId {main.Key}");
                    PluginDebug($"[Main Item(s)] Required Items for Recipe: ItemID: {subItem} | Currently have: {currentSubItemAmount} | Amount Needed [Base]: {subItemNeed}");
                    if (C.CraftMultipleMissionItems)
                    {
                        subItemNeed = subItemNeed * 2;
                        mainNeed = mainNeed * 2;
                    }

                    if (currentAmount < mainNeed)
                    {
                        subItemNeed = subItemNeed - currentAmount;

                        PluginDebug($"[Main Item(s)] You currently don't have the required amount of items. Checking to see if you have enough pre-crafts");
                        if (currentSubItemAmount >= subItemNeed)
                        {
                            PluginDebug($"[Main Item(s) You have the required amount to make the necessary amount of main items. Continuing on]");
                            if (EzThrottler.Throttle("[Main Item(s)] Crafting Main Item(s)", 4000))
                            {
                                int craftAmount = mainNeed - currentAmount;
                                PluginDebug($"[Main Item(s)] Telling Artisan to use recipe: {main.Key} | {craftAmount}");
                                P.Artisan.CraftItem(main.Key, craftAmount);
                                needPreCraft = false;
                                break;
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
                        PluginDebug($"[Pre-Crafts] Checking Pre-crafts to see if {itemId} has enough.");
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
                                PluginDebug($"[Pre-Crafts] Found an item that needs to be crafted: {itemId}");
                                int craftAmount = goalAmount - currentAmount;
                                PluginDebug($"[Pre-Crafts] Telling Artisan to craft: {pre.Key} with the amount: {craftAmount}");
                                P.Artisan.CraftItem(pre.Key, craftAmount);
                            }
                            break; // <-- Important: break out after starting a pre-craft to avoid multiple crafts at once
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
            if (!Svc.Condition[ConditionFlag.Crafting] && 
                !Svc.Condition[ConditionFlag.Crafting40] &&
                !Svc.Condition[ConditionFlag.PreparingToCraft])
            {
                return true;
            }

            return false;
        }
    }
}
