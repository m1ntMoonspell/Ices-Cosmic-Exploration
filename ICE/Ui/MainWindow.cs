// File: ICE/Ui/MainWindow.cs
using ICE.Scheduler;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ICE;
using static ICE.Utilities.Data;
using ICE.Ui;
using ImGuiNET;
using Dalamud.Interface.Windowing;

namespace ICE.Ui
{
    internal class MainWindow : Window
    {
        /// <summary>
        /// Constructor for the main window. Adjusts window size, flags, and initializes data.
        /// </summary>
        public MainWindow() :
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindow")
        {

            Flags = ImGuiWindowFlags.None;

            // Set up size constraints to ensure window cannot be too small or too large.
            // Increased minimum size to better accommodate larger font sizes
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(500, 500),
                MaximumSize = new Vector2(2000, 2000)
            };

            // Register this window with Dalamud's window system.
            P.windowSystem.AddWindow(this);

            AllowPinning = false;
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        private static List<(string Name, uint Id)> jobOptions = new()
        {
            ("CRP", 9),
            ("BSM", 10),
            ("ARM", 11),
            ("GSM", 12),
            ("LTW", 13),
            ("WVR", 14),
            ("ALC", 15),
            ("CUL", 16),
        };

        private static List<(uint RankId, string RankName)> rankOptions = new()
        {
            (1, "D"),
            (2, "C"),
            (3, "B"),
            (4, "A"),
        };

        private static int selectedIndex = 0; // Index of the currently selected job
        private static uint selectedJobId = jobOptions[selectedIndex].Id;

        private static int selectedRankIndex = 0;
        private static string selectedRankName = rankOptions[selectedRankIndex].RankName;

        private static bool delayGrab = C.DelayGrab;
        private static bool silverTurnin = C.TurninOnSilver;
        private static bool turninASAP = C.TurninASAP;
        private static bool hideUnsupported = C.HideUnsupportedMissions;

        /// <summary>
        /// Primary draw method. Responsible for drawing the entire UI of the main window.
        /// </summary>
        public override void Draw()
        {
            ImGui.Text("Run");

            ImGuiEx.HelpMarker("Please note: this will try and run based off of every rank that it can.\n" +
                                "So if you have both C & D checkmarks, it will check C first -> Check D for potential Missions.\n" +
                                "It will cycle through missions until it finds one that you have selected.\n" +
                                "Unsupported missions will be disabled and shown in red; check 'Hide unsupported missions' to filter them out.");

            ImGui.Spacing();

            using (ImRaii.Disabled(SchedulerMain.AreWeTicking))
            {
                if (ImGui.Button("Start"))
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            using (ImRaii.Disabled(!SchedulerMain.AreWeTicking))
            {
                if (ImGui.Button("Stop"))
                {
                    SchedulerMain.DisablePlugin();
                }
            }

            if (ImGui.Checkbox("Add delay to grabbing mission", ref delayGrab))
            {
                C.DelayGrab = delayGrab;
                C.Save();
            }

            if (ImGui.Checkbox("Turnin on Silver", ref silverTurnin))
            {
                if (turninASAP)
                {
                    turninASAP = false;
                    C.TurninASAP = false;
                }

                C.TurninOnSilver = silverTurnin;
                C.Save();
            }

            if (ImGui.Checkbox("Turnin ASAP", ref turninASAP))
            {
                if (silverTurnin)
                {
                    silverTurnin = false;
                    C.TurninOnSilver = false;
                }

                C.TurninASAP = turninASAP;
                C.Save();
            }

            // Hide unsupported missions toggle
            if (ImGui.Checkbox("Hide unsupported missions", ref hideUnsupported))
            {
                C.HideUnsupportedMissions = hideUnsupported;
                C.Save();
            }

            ImGui.SetNextItemWidth(75);
            if (ImGui.BeginCombo("Crafting Job", jobOptions[selectedIndex].Name))
            {
                for (int i = 0; i < jobOptions.Count; i++)
                {
                    bool isSelected = (i == selectedIndex);
                    if (ImGui.Selectable(jobOptions[i].Name, isSelected))
                    {
                        selectedIndex = i;
                        selectedJobId = jobOptions[i].Id;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SetNextItemWidth(75);
            if (ImGui.BeginCombo("Rank", rankOptions[selectedRankIndex].RankName))
            {
                for (int i = 0; i < rankOptions.Count; i++)
                {
                    bool isSelected = (i == selectedRankIndex);
                    if (ImGui.Selectable(rankOptions[i].RankName, isSelected))
                    {
                        selectedRankIndex = i;
                        selectedRankName = rankOptions[i].RankName;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            if (ImGui.BeginTable("###MissionList", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("###Enable");
                ImGui.TableSetupColumn("ID");
                ImGui.TableSetupColumn("Mission Name");
                ImGui.TableSetupColumn("Reward");

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"Rank {selectedRankName} Missions");
                ImGui.TableNextColumn();
                ImGui.Text("Reward");

                foreach (var entry in MissionInfoDict.OrderBy(x => x.Value.Name))
                {
                    if (entry.Value.JobId != selectedJobId - 1)
                        continue;

                    bool isARank = ARankIds.Contains(entry.Value.Rank);
                    if (selectedRankIndex == 3
                        ? !isARank
                        : entry.Value.Rank != rankOptions[selectedRankIndex].RankId)
                        continue;

                    bool unsupported = UnsupportedMissions.Ids.Contains(entry.Key);
                    if (unsupported && hideUnsupported)
                        continue;

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    bool temp = C.EnabledMission.Any(x => x.Id == entry.Key);
                    using (ImRaii.Disabled(unsupported))
                    {
                        if (ImGui.Checkbox($"###{entry.Value.Name} + {entry.Key}", ref temp))
                        {
                            if (temp)
                            {
                                C.EnabledMission.Add((entry.Key, entry.Value.Name));
                            }
                            else
                            {
                                C.EnabledMission.Remove((entry.Key, entry.Value.Name));
                            }
                            C.Save();
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text($"{entry.Key}");

                    ImGui.TableNextColumn();
                    if (unsupported)
                    {
                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), entry.Value.Name);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Currently not supported");
                    }
                    else
                    {
                        ImGui.Text($"{entry.Value.Name}");
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text($"{string.Join(" | ", entry.Value.ExperienceRewards.Select(exp => ExpDictionary[exp.Type] + ": " + exp.Amount))}");
                }

                ImGui.EndTable();
            }
        }
    }
}
