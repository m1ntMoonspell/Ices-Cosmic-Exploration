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
            P.TaskManager.Enqueue(() =>
            {
                if (C.DelayGrab)
                {
                    P.TaskManager.EnqueueDelay(1500);
                }
                else
                {
                    P.TaskManager.EnqueueDelay(100);
                }
            });
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
                bool foundPreCraft = false;
                bool allCrafted = true;

                PluginDebug($"Current Mission: {currentMission} | Found Pre-Craft? {foundPreCraft}");

                foreach (var mainItem in MoonRecipies[currentMission].MainCraftsDict)
                {
                    var itemId = RecipeSheet.GetRow(mainItem.Key).ItemResult.Value.RowId;
                    var currentAmount = GetItemCount((int)itemId);
                    var goalAmount = mainItem.Value;
                    if (C.CraftMultipleMissionItems)
                    {
                        goalAmount = mainItem.Value * 2;
                    }
                    if (currentAmount < goalAmount)
                    {
                        PluginDebug("Score checker can't be done, you still have items to craft");
                        allCrafted = false;
                        break;
                    }
                }

                if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady && allCrafted)
                {
                    P.Artisan.SetEnduranceStatus(false);
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
                                return true;
                            }
                        }

                        PluginDebug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && if TurninSilver is true: {C.TurninOnSilver}");
                        if (currentScore >= silverScore && C.TurninOnSilver)
                        {
                            PluginDebug($"Silver was enabled, and you also meet silver threshold. ");
                            if (EzThrottler.Throttle("Turning in item", 100))
                            {
                                z.Report();
                                return true;
                            }
                        }

                        PluginDebug($"[Score Checker] Seeing if Player not busy: {PlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}");
                        if (PlayerNotBusy() && !Svc.Condition[ConditionFlag.PreparingToCraft])
                        {
                            PluginDebug($"[Score Checker] Conditions for gold was met. Turning in");
                            if (EzThrottler.Throttle("Turning in item"))
                            {
                                z.Report();
                                return true;
                            }
                        }
                    }
                }

                if (MoonRecipies[currentMission].PreCrafts) // new version
                {
                    PluginDebug("Pre-crafts are part of the list, checking to see if any need crafting");
                    foreach (var pre in MoonRecipies[currentMission].PreCraftDict)
                    {
                        var itemId = RecipeSheet.GetRow(pre.Key).ItemResult.Value.RowId;
                        var currentAmount = GetItemCount((int)itemId);
                        PluginDebug($"[Pre-Crafts] Checking Pre-crafts to see if {itemId} has enough.");
                        PluginDebug($"[Pre-Crafts] Item Amount: {currentAmount} | Goal Amount: {pre.Value} | RecipeId: {pre.Key}");
                        var goalAmount = pre.Value;
                        if (C.CraftMultipleMissionItems)
                        {
                            goalAmount = pre.Value * 2;
                        }

                        if (currentAmount < goalAmount)
                        {
                            foundPreCraft = true; // <--- Mark that a pre-craft is needed!

                            if (EzThrottler.Throttle($"Starting pre-craft {pre.Key}", 1000))
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

                if (!foundPreCraft)
                {
                    PluginDebug("No pre-crafts remaining! Crafting the main item");
                    foreach (var main in MoonRecipies[currentMission].MainCraftsDict)
                    {
                        var itemId = RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                        var currentAmount = GetItemCount((int)itemId);

                        PluginDebug($"[Main Item(s)] ItemId: {itemId} | Current Amount {currentAmount} | Amount Wanted: {main.Value} | RecipeId: {main.Key}");
                        var goalAmount = main.Value;
                        if (C.CraftMultipleMissionItems)
                        {
                            goalAmount = main.Value * 2;
                        }

                        PluginDebug($"[Main Item(s)] Checking if current amount[ {currentAmount} ] < goalAmount {goalAmount} | Result: {currentAmount < goalAmount}");
                        PluginDebug($"[Main Item(s)] Also checking if currentScore < goldScore && Preparing to craft | Result: {currentScore < goldScore && Svc.Condition[ConditionFlag.PreparingToCraft]}");
                        if (currentAmount < goalAmount || (currentScore < goldScore && Svc.Condition[ConditionFlag.PreparingToCraft])) // if not hit gold and there is still some items (aka its still in preparing to craft animation) we want to send it anyway
                        {
                            if (EzThrottler.Throttle("Starting Main Craft", 4000))
                            {
                                int craftamount = goalAmount - currentAmount;
                                PluginDebug($"[Main Item(s)] Telling Artisan to use recipe: {main.Key} | {craftamount}");
                                P.Artisan.CraftItem(main.Key, goalAmount);
                                P.TaskManager.EnqueueDelay(1500);
                                allCrafted = false;
                                break;
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
    }
}
