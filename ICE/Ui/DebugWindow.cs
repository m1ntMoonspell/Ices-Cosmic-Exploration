using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.UIHelpers.AddonMasterImplementations;
using ICE.Scheduler;
using ICE.Scheduler.Tasks;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Ui;

internal class DebugWindow : Window
{
    public DebugWindow() :
        base($"ICE {P.GetType().Assembly.GetName().Version} ###IceCosmicDebug1")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(100, 100),
            MaximumSize = new Vector2(3000, 3000)
        };
        P.windowSystem.AddWindow(this);
    }

    public void Dispose() { }

    // variables that hold the "ref"s for ImGui

    public override void Draw()
    {
        var sheet = Svc.Data.GetExcelSheet<WKSMissionRecipe>();

        if (ImGui.TreeNode("Main Hud"))
        {
            if (TryGetAddonMaster<WKSHud>("WKSHud", out var HudAddon))
            {
                if (ImGui.Button("Mission"))
                {
                    HudAddon.Mission();
                }

                ImGui.SameLine();

                if (ImGui.Button("Mech"))
                {
                    HudAddon.Mech();
                }

                ImGui.SameLine();
                
                if (ImGui.Button("Steller"))
                {
                    HudAddon.Steller();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Infrastructor"))
                {
                    HudAddon.Infrastructor();
                }

                ImGui.SameLine();

                if (ImGui.Button("Research"))
                {
                    HudAddon.Research();
                }

                ImGui.SameLine();

                if (ImGui.Button("ClassTracker"))
                {
                    HudAddon.ClassTracker();
                }
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Missions"))
        {
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                ImGui.Text("List of Visible Missions");
                ImGui.Text($"Selected Mission: {x.SelectedMission}");

                if (ImGui.Button("Help"))
                {
                    x.Help();
                }
                ImGui.SameLine();

                if (ImGui.Button("Mission Selection"))
                {
                    x.MissionSelection();
                }
                ImGui.SameLine();

                if (ImGui.Button("Mission Log"))
                {
                    x.MissionLog();
                }
                ImGui.SameLine();

                if (ImGui.Button("Basic Missions"))
                {
                    x.BasicMissions();
                }
                ImGui.SameLine();

                if (ImGui.Button("Provisional Missions"))
                {
                    x.ProvisionalMissions();
                }
                ImGui.SameLine();

                if (ImGui.Button("Critical Missions"))
                {
                    x.CriticalMissions();
                }

                foreach (var m in x.StellerMissions)
                {
                    ImGui.Text($"{m.Name}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Select###Select + {m.Name}"))
                    {
                        m.Select();
                    }
                }

            }
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Mission Info"))
        {
            uint currentScore = 0;
            uint silverScore = 0;
            uint goldScore = 0;

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var x) && x.IsAddonReady)
            {
                currentScore = x.CurrentScore;
                silverScore = x.SilverScore;
                goldScore = x.GoldScore;

                ImGui.Text($"Addon Ready: {IsAddonActive("WKSMissionInfomation")}");
                if (IsAddonActive("WKSMissionInfomation"))
                {
                    ImGui.Text($"Node Text: {GetNodeText("WKSMissionInfomation", 27)}");
                }

                if (ImGui.BeginTable("Mission Info", 2))
                {
                    ImGui.TableSetupColumn("###Info", ImGuiTableColumnFlags.WidthFixed, 150);
                    ImGui.TableSetupColumn("###UiInfo", ImGuiTableColumnFlags.WidthFixed, 100);

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Current Score:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{currentScore}");

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Silver Score:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{silverScore}");

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Gold Score:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{goldScore}");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Cosmo Pouch"))
                    {
                        x.CosmoPouch();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Cosmo Crafting Log"))
                    {
                        x.CosmoCraftingLog();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Steller Reduction"))
                    {
                        x.StellerReduction();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Report"))
                    {
                        x.Report();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Abandon"))
                    {
                        x.Abandon();
                    }


                    ImGui.EndTable();
                }
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Moon Recipe Notebook"))
        {
            if (TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var x) && x.IsAddonReady)
            {
                ImGui.Text(x.SelectedCraftingItem);

                if (ImGui.Button("Fill NQ"))
                {
                    x.NQItemInput();
                }
                ImGui.SameLine();

                if (ImGui.Button("Fill HQ"))
                {
                    x.HQItemInput();
                }
                ImGui.SameLine();
                
                if (ImGui.Button("Fill Both"))
                {
                    x.NQItemInput();
                    x.HQItemInput();
                }
                ImGui.SameLine();

                if (ImGui.Button("Synthesize"))
                {
                    x.Synthesize();
                }

                foreach (var m in x.CraftingItems)
                {
                    if (ImGui.Button($"Select ###Select + {m.Name}"))
                    {
                        m.Select();
                    }
                    ImGui.SameLine();
                    ImGui.Text($"{m.Name}");
                }
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Crafting Table"))
        {
            var sheetRow = sheet.GetRow(27);
            ImGui.Text($"Unknown 0: {sheetRow.Unknown0} | Unknown 1: {sheetRow.Unknown1}");
            ImGui.Text($"Unknown 2: {sheetRow.Unknown2} | Unknown 3: {sheetRow.Unknown3}");
            ImGui.Text($"Unknown 4: {sheetRow.Unknown4}");

            Table();

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Moon Recipies"))
        {
            Table2();

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Test Buttons"))
        {
            var ExpSheet = Svc.Data.GetExcelSheet<WKSMissionReward>();
            //  4 - Col 2  - Unknown 7
            //  8 - Col 3  - Unknown 0
            // 10 - Col 4  - Unknown 1
            //  3 - Col 7  - Unknown 12
            //  7 - Col 8  - Unknown 2
            //  2 - Col 10 - Unknown 13
            //  5 - Col 11 - Unknown 3
            //  1 - Col 13 - Unknown 14
            //  5 - Col 14 - Unknown 4
            //  0          - Unknown 5
            //  0          - Unknown 6
            //  0          - Unknown 8
            //  1          - Unknown 9 
            //  1          - Unknown 10
            //  1          - Unknown 11


            ImGui.Text($"UK1: {ExpSheet.GetRow(20).Unknown1.ToInt()} | UN2: {ExpSheet.GetRow(20).Unknown2.ToInt()} | UN3: {ExpSheet.GetRow(20).Unknown3.ToInt()}");
            ImGui.Text($"UK1: {ExpSheet.GetRow(20).Unknown4.ToInt()} | UN2: {ExpSheet.GetRow(20).Unknown5.ToInt()} | UN3: {ExpSheet.GetRow(20).Unknown6.ToInt()}");
            ImGui.Text($"UK1: {ExpSheet.GetRow(20).Unknown7.ToInt()} | UN2: {ExpSheet.GetRow(20).Unknown8.ToInt()} | UN3: {ExpSheet.GetRow(20).Unknown9.ToInt()}");
            ImGui.Text($"UK1: {ExpSheet.GetRow(20).Unknown10.ToInt()} | UN2: {ExpSheet.GetRow(20).Unknown11.ToInt()} | UN3: {ExpSheet.GetRow(20).Unknown12.ToInt()}");
            ImGui.Text($"UK1: {ExpSheet.GetRow(20).Unknown13.ToInt()} | UN2: {ExpSheet.GetRow(20).Unknown14.ToInt()} | UN3: {ExpSheet.GetRow(20).Unknown0.ToInt()}");
            if (ImGui.Button("Find Mission"))
            {
                TaskMissionFind.Enqueue();
            }
            if (ImGui.Button("Clear Task"))
            {
                P.taskManager.Abort();
            }

            ImGui.TreePop();
        }


    }

    private void Table()
    {
        var itemSheet = Svc.Data.GetExcelSheet<Item>();

        if (ImGui.BeginTable("Mission Info List", 17, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("ID");
            ImGui.TableSetupColumn("Mission Name", ImGuiTableColumnFlags.WidthFixed, 25);
            ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("2nd Job", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("RecipeID", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("MainItem", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Required Item", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Amount###SubItem1Amount", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("SubItem 2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Amount###SubItem2Amount", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Type 1###MissionExpType1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Amount 1###MissionExpAmount1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Type 2###MissionExpType2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Amount 2###MissionExpAmount2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Type 3###MissionExpType3", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Amount 3###MissionExpAmount3", ImGuiTableColumnFlags.WidthFixed, 100);

            ImGui.TableHeadersRow();

            foreach (var entry in MissionInfoDict)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"{entry.Key}");

                // Mission Name
                ImGui.TableNextColumn();
                ImGui.Text(entry.Value.Name);

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.JobId}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.JobId2}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.Rank}");

                ImGui.TableNextColumn();
                var RecipeSearch = entry.Value.RecipeId;
                ImGui.Text($"{RecipeSearch}");

                if (RecipeSearch != 0)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{MoonRecipies[RecipeSearch].MainItem}");
                    if (ImGui.IsItemHovered())
                    {
                        if (itemSheet.TryGetRow(MoonRecipies[RecipeSearch].MainItem, out var item))
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"{item.Name}");
                            ImGui.EndTooltip();
                        }
                    }

                    foreach (var subRecipe in MoonRecipies[RecipeSearch].RecipieItems)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{subRecipe.Key}");
                        if (ImGui.IsItemHovered())
                        {
                            if (itemSheet.TryGetRow(subRecipe.Key, out var item))
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text($"{item.Name}");
                                ImGui.EndTooltip();
                            }
                        }

                        ImGui.TableNextColumn();
                        ImGui.Text($"{subRecipe.Value}");
                    }

                    ImGui.TableSetColumnIndex(10);
                    foreach (var exp in entry.Value.ExperienceRewards)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{exp.Type}");

                        ImGui.TableNextColumn();
                        ImGui.Text($"{exp.Amount}");
                    }
                }

            }

            ImGui.EndTable();
        }
    }

    private void Table2()
    {
        if (ImGui.BeginTable("Mission Info List", 11, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("MainId/Key", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("MainItem", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Item1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Required1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Item2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Required2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Item3", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Required3", ImGuiTableColumnFlags.WidthFixed, 100);

            ImGui.TableHeadersRow();

            foreach (var entry in MoonRecipies)
            {
                ImGui.TableNextRow();

                // Mission Name
                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"{entry.Key}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.MainItem}");

                foreach (var subEntry in entry.Value.RecipieItems)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text($"{subEntry.Key}");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{subEntry.Value}");
                }
            }

            ImGui.EndTable();
        }
    }
}