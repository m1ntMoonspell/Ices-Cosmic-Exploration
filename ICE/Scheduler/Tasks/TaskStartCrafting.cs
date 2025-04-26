using Dalamud.Game.ClientState.Conditions;
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
            P.taskManager.Enqueue(() => SetArtisanEndur(false));
            P.taskManager.Enqueue(() => StartCrafting(), DConfig);
        }

        internal unsafe static void SetArtisanEndur(bool enable)
        {
            P.artisan.SetEnduranceStatus(enable);
        }

        internal unsafe static bool? StartCrafting()
        {
            uint currentScore = 0;
            uint goldScore = 0;

            string itemToCraft = "";

            var itemSheet = Svc.Data.GetExcelSheet<Item>();

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                uint tempScore = 0;
                string currentScoreText = GetNodeText("WKSMissionInfomation", 27);
                currentScoreText = currentScoreText.Replace(",", "");
                if (uint.TryParse(currentScoreText, out tempScore))
                {
                    currentScore = tempScore;
                }
                else
                {
                    currentScore = 0;
                }

                goldScore = MissionInfoDict[SchedulerMain.MissionId].GoldRequirement;

                if (currentScore != 0)
                {
                    if (currentScore >= goldScore && PlayerNotBusy())
                    {
                        if (EzThrottler.Throttle("Turning in item"))
                        {
                            z.Report();
                            return true;
                        }
                    }
                    else if (!Svc.Condition[ConditionFlag.Crafting] && PlayerNotBusy())
                    {
                        if (EzThrottler.Throttle("Turning in item"))
                        {
                            z.Report();
                            return true;
                        }
                    }
                }
            }

            if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !IsAddonActive("WKSMissionInfomation"))
            {
                if (EzThrottler.Throttle("Opening Steller Missions"))
                {
                    PluginLog.Debug("Opening Mission Menu");
                    hud.Mission();
                }
            }

            if (TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var x) && x.IsAddonReady)
            {
                PluginLog.Debug($"MissionID: {SchedulerMain.MissionId}");
                var MainRecipe = MoonRecipies[SchedulerMain.MissionId + 1];
                string mainItem = itemSheet.GetRow(MainRecipe.MainItem).Name.ToString();

                string subItem = "";

                foreach (var item in MainRecipe.RecipieItems)
                {
                    if (GetItemCount((int)item.Key) >= item.Value)
                    {
                        // Checking to see if current item count is >= than amount needed
                        // if yes, then skips item
                        PluginLog.Debug($"Have enough of the item, going to craft: {MainRecipe.MainItem}");
                        continue;
                    }
                    else
                    {
                        // You don't have enough of said item for the main craft, going to craft said item
                        subItem = itemSheet.GetRow(item.Key).Name.ToString();
                        itemToCraft = subItem;
                        PluginLog.Debug($"Missing the following sub item: {subItem}");
                    }
                }

                if (subItem == "")
                {
                    // no sub items were necessary, crafting the main thing
                    itemToCraft = mainItem;
                }

                PluginLog.Debug($"Checking if: {x.SelectedCraftingItem} == {itemToCraft}");
                if (!x.SelectedCraftingItem.Contains(itemToCraft))
                {
                    foreach (var m in x.CraftingItems)
                    {
                        if (!m.Name.Contains(itemToCraft))
                            continue;

                        if (EzThrottler.Throttle("Selecting Item"))
                            m.Select();
                    }
                }
                else
                {
                    if (EzThrottler.Throttle("Synthesizing Weapons"))
                    {
                        PluginLog.Debug("Synthesizing Tools");
                        x.NQItemInput();
                        x.HQItemInput();
                        P.artisan.SetEnduranceStatus(true);
                    }
                }
            }

            if (TryGetAddonMaster<Synthesis>("Synthesis", out var s) && s.IsAddonReady)
            {
                if (P.artisan.GetEnduranceStatus() == true)
                {
                    P.artisan.SetEnduranceStatus(false);
                }
            }

            return false;
        }
    }
}
