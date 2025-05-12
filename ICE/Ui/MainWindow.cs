// File: ICE/Ui/MainWindow.cs
using ICE.Scheduler;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using ICE.Enums;
using Dalamud.Interface.Colors;
using static ICE.Utilities.CosmicHelper;
using ICE.Utilities.Cosmic;
using System.Reflection;
using Dalamud.Interface.Utility;
using ICE.Scheduler.Handlers;

namespace ICE.Ui
{
    internal class MainWindow : Window
    {
        /// <summary>
        /// Constructor for the main window. Adjusts window size, flags, and initializes data.
        /// </summary>
        public MainWindow() :
#if DEBUG
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion} Debug build ###ICEMainWindow")
#else
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindow")
#endif
        {
            // No special window flags by default.
            Flags = ImGuiWindowFlags.None;

            // Set up size constraints to ensure window cannot be too small or too large.
            // Increased minimum size to better accommodate larger font sizes.
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(500, 500),
                MaximumSize = new Vector2(2000, 2000)
            };

            // Register this window with Dalamud's window system.
            P.windowSystem.AddWindow(this);

            // Disable pinning of this window.
            AllowPinning = false;
        }

        /// <summary>
        /// Dispose of the window by removing it from the window system.
        /// </summary>
        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        // Available jobs and their IDs.
        public static List<(string Name, uint Id)> jobOptions = new()
        {
            ("CRP", 9),
            ("BSM", 10),
            ("ARM", 11),
            ("GSM", 12),
            ("LTW", 13),
            ("WVR", 14),
            ("ALC", 15),
            ("CUL", 16),
            ("MIN", 17),
            ("BTN", 18),
            ("FSH", 19),
        };

        // Available mission ranks and their identifiers.
        private static List<(uint RankId, string RankName)> rankOptions = new()
        {
            (1, "D"),
            (2, "C"),
            (3, "B"),
            (4, "A")
        };

        private static List<(uint Id, string SortOptionName, Func<IEnumerable<KeyValuePair<uint, MissionListInfo>>, IEnumerable<KeyValuePair<uint, MissionListInfo>>> SortFunc)> sortOptions = new()
        {
            (0, "", missions => missions),
            (1, "Name", missions => missions.OrderBy(x => x.Value.Name)),
            (2, "Mission ID", missions => missions),
            (3, "Cosmocredits", missions => missions.OrderByDescending(x => x.Value.CosmoCredit)),
            (4, "Lunar Credits", missions => missions.OrderByDescending(x => x.Value.LunarCredit)),
            (5, "Research I", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "I").FirstOrDefault().Amount)),
            (6, "Research II", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "II").FirstOrDefault().Amount)),
            (7, "Research III", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "III").FirstOrDefault().Amount)),
            (8, "Research IV", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => CosmicHelper.ExpDictionary[exp.Type] == "IV").FirstOrDefault().Amount))
        };

        // Index of the currently selected job in jobOptions.
        private static int selectedJobIndex = 0;
        // ID of the currently selected crafting job.
        private static uint selectedJobId = jobOptions[selectedJobIndex].Id;
        private static uint? currentJobId => PlayerHelper.GetClassJobId();
        private static bool isCrafter => currentJobId >= 8 && currentJobId <= 15;
        private static bool isGatherer => currentJobId >= 16 && currentJobId <= 18;
        private static bool usingSupportedJob => jobOptions.Any(job => job.Id == currentJobId + 1);

        // Index of the currently selected rank in rankOptions.
        private static int selectedRankIndex = 0;
        // Name of the currently selected rank (for displaying in header).
        private static string selectedRankName = rankOptions[selectedRankIndex].RankName;

        // Configuration booleans bound to checkboxes.
        private static bool animationLockAbandon = C.AnimationLockAbandon;
        private static bool AnimationLockAbandonState = SchedulerMain.AnimationLockAbandonState;
        private static bool stopOnAbort = C.StopOnAbort;
        private static bool rejectUnknownYesNo = C.RejectUnknownYesno;
        private static bool delayGrabMission = C.DelayGrabMission;
        private static bool delayCraft = C.DelayCraft;
        private static int delayAmount = C.DelayIncrease;
        private static int delayCraftAmount = C.DelayCraftIncrease;
        private static bool hideUnsupported = C.HideUnsupportedMissions;
        private static bool onlyGrabMission = C.OnlyGrabMission;
        private static bool showOverlay = C.ShowOverlay;
        private static bool stopOnceHitCosmoCredits = C.StopOnceHitCosmoCredits;
        private static bool stopOnceHitLunarCredits = C.StopOnceHitLunarCredits;
        private static int cosmoCreditsCap = C.CosmoCreditsCap;
        private static int lunarCreditsCap = C.LunarCreditsCap;
        private static bool stopWhenLevel = C.StopWhenLevel;
        private static int targetLevel = C.TargetLevel;
        private static bool ShowSeconds = C.ShowSeconds;
        private static bool EnableAutoSprint = C.EnableAutoSprint;
        private static bool autoPickCurrentJob = C.AutoPickCurrentJob;
        private static int SortOption = C.TableSortOption;
        private static bool showExp = C.ShowExpColums;
        private static bool showCredits = C.ShowCreditsColumn;

        /// <summary>
        /// Primary draw method. Responsible for drawing the entire UI of the main window.
        /// </summary>
        public override void Draw()
        {
            using var tabbar = ImRaii.TabBar("ConfigTabs###GatherBuddyConfigTabs", ImGuiTabBarFlags.Reorderable);
            if (!tabbar)
                return;

            DrawMissionsTab();
            DrawConfigTab();
        }

        public void DrawMissionsTab()
        {
            var tab = ImRaii.TabItem("Missions");

            if (!tab)
                return;

            // Title text for the run controls.
            ImGui.Text("Run");
            // Help marker explaining how missions are selected and run.
            ImGuiEx.HelpMarker(
                "Please note: this will try and run based off of every rank that it can.\n" +
                "So if you have both C & D checkmarks, it will check C first -> Check D for potential Missions.\n" +
                "It will cycle through missions until it finds one that you have selected.\n" +
                "Unsupported missions will be disabled and shown in red; check 'Hide unsupported missions' to filter them out."
            );

            ImGui.Text($"Current state: " + SchedulerMain.State.ToString());


            ImGui.Spacing();

            // Start button (disabled while already ticking).
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
            {
                if (ImGui.Button("Start"))
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            // Stop button (disabled while not ticking).
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("Stop"))
                {
                    SchedulerMain.DisablePlugin();
                }
            }

            ImGui.SameLine();
            ImGui.Checkbox("Stop after current mission", ref SchedulerMain.StopBeforeGrab);

            ImGui.SameLine();
            ImGui.NewLine();

            if (C.AutoPickCurrentJob && usingSupportedJob)
            {
                selectedJobIndex = jobOptions.IndexOf(job => job.Id == currentJobId + 1);
                selectedJobId = jobOptions[selectedJobIndex].Id;

                ImGui.Text($"Job: " + jobOptions[selectedJobIndex].Name);
            }
            else
            {
                // Crafting Job selection combo.
                ImGui.SetNextItemWidth(75);
                if (ImGui.BeginCombo("Job", jobOptions[selectedJobIndex].Name))
                {
                    for (int i = 0; i < jobOptions.Count; i++)
                    {
                        bool isSelected = (i == selectedJobIndex);
                        if (ImGui.Selectable(jobOptions[i].Name, isSelected))
                        {
                            selectedJobIndex = i;
                            selectedJobId = jobOptions[i].Id;
                        }
                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }
            }

            ImGui.Spacing();

            ImGui.SetNextItemWidth(150);
            if (ImGui.BeginCombo("Sort By", sortOptions[SortOption].SortOptionName))
            {
                for (int i = 0; i < sortOptions.Count; i++)
                {
                    bool isSelected = (i == SortOption);
                    if (ImGui.Selectable(sortOptions[i].SortOptionName, isSelected))
                    {
                        SortOption = i;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                    if (SortOption != C.TableSortOption)
                    {
                        C.TableSortOption = SortOption;
                        C.Save();
                    }
                }
                ImGui.EndCombo();
            }

            // Rank selection combo.
            ImGui.SetNextItemWidth(100);
            IEnumerable<KeyValuePair<uint, MissionListInfo>> criticalMissions =
                MissionInfoDict
            .Where(m => m.Value.JobId == selectedJobId - 1)
            .Where(m => m.Value.IsCriticalMission);
            criticalMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(criticalMissions);
            bool criticalGather = criticalMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
            DrawMissionsDropDown($"Critical Missions - {criticalMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} enabled", criticalMissions, criticalGather);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> weatherRestrictedMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1 || m.Value.JobId2 == selectedJobId - 1)
                        .Where(m => m.Value.Weather != CosmicWeather.FairSkies)
                        .Where(m => !m.Value.IsCriticalMission);
            bool weatherGather = weatherRestrictedMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
            weatherRestrictedMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(weatherRestrictedMissions);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> timeRestrictedMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1)
                        .Where(m => m.Value.Time != 0);
            bool timeGather = timeRestrictedMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
            timeRestrictedMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(timeRestrictedMissions);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> sequentialMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1)
                        .Where(m => m.Value.PreviousMissionID != 0);
            bool sequentialGather = sequentialMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
            sequentialMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(sequentialMissions);

            void DrawWeatherMissions() => DrawMissionsDropDown($"Weather-restricted Missions - {weatherRestrictedMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} enabled", weatherRestrictedMissions, weatherGather);
            void DrawTimedMissions() => DrawMissionsDropDown($"Time-restricted Missions - {timeRestrictedMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} enabled", timeRestrictedMissions, timeGather);
            void DrawSequentialMissions() => DrawMissionsDropDown($"Sequential Missions - {sequentialMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} enabled", sequentialMissions, sequentialGather);

            var missionList = new List<(int prio, Action action)>
                {
                    (C.SequenceMissionPriority, DrawSequentialMissions),
                    (C.TimedMissionPriority, DrawTimedMissions),
                    (C.WeatherMissionPriority, DrawWeatherMissions)
                };
            foreach (var (_, drawMission) in missionList.OrderBy(t => t.prio))
            {
                drawMission();
            }

            foreach (var rank in rankOptions.OrderBy(r => r.RankName))
            {
                IEnumerable<KeyValuePair<uint, MissionListInfo>> missions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1 || m.Value.JobId2 == selectedJobId - 1)
                        .Where(m => (m.Value.Rank == rank.RankId) || (rank.RankName == "A" && ARankIds.Contains(m.Value.Rank)))
                        .Where(m => !m.Value.IsCriticalMission)
                        .Where(m => m.Value.Time == 0)
                        .Where(m => m.Value.PreviousMissionID == 0)
                        .Where(m => m.Value.Weather == CosmicWeather.FairSkies);
                missions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(missions);

                bool missionGather = missions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
                DrawMissionsDropDown($"Class {rank.RankName} Missions - {missions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} enabled", missions, missionGather);
            }

            ImGui.EndTabItem();
        }

        public void DrawMissionsDropDown(string tabName, IEnumerable<KeyValuePair<uint, MissionListInfo>> missions, bool showGatherConfig = false)
        {
            var tabId = tabName.Split('-')[0];
            if (ImGui.CollapsingHeader(string.Format("{0}###{1}", tabName, tabId)))
            {
                ImGui.Spacing();
                // Missions table with four columns: checkbox, ID, dynamic Rank header, Rewards.

                int columnAmount = 5;
                if (showCredits)
                    columnAmount += 2;
                if (showExp)
                    columnAmount += 4;
                if (showGatherConfig)
                    columnAmount += 1;

                if (ImGui.BeginTable($"MissionList###{tabId}", columnAmount, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                {
                    var sortSpecs = ImGui.TableGetSortSpecs();
                    float col1Width = 0;
                    float col2Width = 0;
                    float col3Width = 15;

                    // used to keep track of where the column index is at, mainly for centering text
                    int columnIndex = 0;

                    // First column: checkbox (empty header)
                    ImGui.TableSetupColumn("Enable##Enable", ImGuiTableColumnFlags.WidthFixed, 50);
                    columnIndex++;

                    // Second column: ID
                    ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, col1Width);
                    columnIndex++;

                    // Third column: dynamic header showing selected rank missions
                    ImGui.TableSetupColumn("Mission Name", ImGuiTableColumnFlags.WidthFixed, col2Width);
                    columnIndex++;

                    if (showCredits)
                    {
                        // Fourth column: Rewards
                        ImGui.TableSetupColumn("Cosmocredits");
                        columnIndex++;

                        ImGui.TableSetupColumn("Lunar Credits");
                        columnIndex++;
                    }

                    // Dynamic EXP columns
                    IOrderedEnumerable<KeyValuePair<int, string>> orderedExp = ExpDictionary.ToList().OrderBy(exp => exp.Key);
                    if (C.ShowExpColums) // Option to not show the EXP Columns if they'd like
                    {
                        foreach (var exp in orderedExp)
                        {
                            ImGui.TableSetupColumn($"###{exp.Value}", ImGuiTableColumnFlags.WidthFixed, col3Width);
                            col3Width = Math.Max(col3Width, ImGui.CalcTextSize(exp.Value).X + 5);
                            columnIndex++;
                        }
                    }

                    // Settings column
                    ImGui.TableSetupColumn("Turn In", ImGuiTableColumnFlags.WidthFixed, 100);

                    if (showGatherConfig)
                    {
                        float columnWidth = ImGui.CalcTextSize("Gather Config").X + 5;
                        ImGui.TableSetupColumn("Gather Config", ImGuiTableColumnFlags.WidthFixed, columnWidth);
                    }

                    // Final column: Notes
                    ImGui.TableSetupColumn("Notes", ImGuiTableColumnFlags.WidthStretch);

                    // Render the header row (static headers get drawn here)
                    ImGui.TableHeadersRow();

                    // Manually center the EXP headers (more personal OCD here)
                    int dynamicStartCol = columnIndex - orderedExp.Count();
                    int dynamicCol = 0;

                    if (C.ShowExpColums)
                    {
                        foreach (var exp in orderedExp)
                        {
                            ImGui.TableSetColumnIndex(dynamicStartCol + dynamicCol++);
                            float colWidth = ImGui.GetColumnWidth();
                            Vector2 textSize = ImGui.CalcTextSize(exp.Value);
                            float offset = (colWidth - textSize.X) / 2;

                            if (offset > 0)
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

                            ImGui.TextUnformatted(exp.Value);
                        }
                    }

                    foreach (var entry in missions)
                    {
                        // Skip unsupported missions if the user has chosen to hide them

                        bool unsupported = UnsupportedMissions.Ids.Contains(entry.Key);

                        if (entry.Value.JobId2 != 0 || (entry.Value.JobId >= 16 && entry.Value.JobId <= 18) || entry.Value.IsCriticalMission)
                            unsupported = true;

#if DEBUG
                        if (!UnsupportedMissions.Ids.Contains(entry.Key))
                        {
                            unsupported = false;
                        }
#endif

                        if (unsupported && hideUnsupported)
                            continue;

                        var mission = C.Missions.Single(x => x.Id == entry.Key);
                        var isEnabled = mission.Enabled;

                        // Start a new row
                        ImGui.TableNextRow();

                        // Column 0: Enable checkbox
                        ImGui.TableSetColumnIndex(0);
                        // Estimate the width of the checkbox (label is invisible, so the box is all that matters)
                        float cellWidth = ImGui.GetContentRegionAvail().X;
                        float checkboxWidth = ImGui.GetFrameHeight(); // Width of the square checkbox only
                        float offset = (cellWidth - checkboxWidth) * 0.5f;

                        if (offset > 0f)
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

                        // Use an invisible label for the checkbox to avoid text spacing
                        if (ImGui.Checkbox($"###{entry.Value.Name}_{entry.Key}", ref isEnabled))
                        {
                            mission.Enabled = isEnabled;
                            CosmicMission chain;

                            if (isEnabled)
                            {
                                var prevChainList = GetOnlyPreviousMissionsRecursive(mission.Id);
                                foreach (var missionId in prevChainList)
                                {
                                    chain = C.Missions.Single(x => x.Id == missionId);
                                    chain.Enabled = isEnabled;
                                }
                            }
                            else
                            {
                                var nextChainList = GetOnlyNextMissionsRecursive(mission.Id);
                                foreach (var missionId in nextChainList)
                                {
                                    chain = C.Missions.Single(x => x.Id == missionId);
                                    chain.Enabled = isEnabled;
                                }
                            }

                            C.Save();
                        }

                        // Column 1: Mission ID
                        ImGui.TableNextColumn();
                        string MissionId = entry.Key.ToString();
                        col1Width = Math.Max(ImGui.CalcTextSize(MissionId).X + 10, col1Width);
                        CenterTextInTableCell($"{MissionId}");

                        // Column 2: Mission Name
                        ImGui.TableNextColumn();
                        string MissionName = entry.Value.Name;
                        if (unsupported)
                        {
                            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), MissionName);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Currently can only be done in manual mode");
                            }
                        }
                        else
                        {
                            ImGui.Text($"{MissionName}");
                        }
                        col2Width = Math.Max(ImGui.CalcTextSize(MissionName).X + 10, col2Width);

                        // Column 3: Rewards
                        if (showCredits)
                        {
                            ImGui.TableNextColumn();
                            CenterTextInTableCell(entry.Value.CosmoCredit.ToString());
                            ImGui.TableNextColumn();
                            CenterTextInTableCell(entry.Value.LunarCredit.ToString());
                        }

                        if (showExp) // If show EXP Values are enabled, will show the exp values. 
                        {
                            foreach (var expType in orderedExp)
                            {
                                ImGui.TableNextColumn();
                                var relicXp = entry.Value.ExperienceRewards.Where(exp => exp.Type == expType.Key).FirstOrDefault().Amount.ToString();
                                if (relicXp == "0")
                                {
                                    relicXp = "-";
                                }
                                col3Width = Math.Max(ImGui.CalcTextSize(relicXp).X + 10, col3Width);
                                CenterTextInTableCell(relicXp);
                            }
                        }

                        ImGui.TableNextColumn();
                        string[] modes;
                        int currentModeIndex = 0;
                        if (unsupported)
                        {
                            modes = ["Manual"];
                            mission.ManualMode = true;
                        }
                        else
                        {
                            modes = ["Gold", "Silver", "ASAP", "Manual"];
                            if (mission.TurnInSilver)
                                currentModeIndex = 1;
                            if (mission.TurnInASAP)
                                currentModeIndex = 2;
                            if (mission.ManualMode)
                                currentModeIndex = 3;
                        }

                        ImGui.SetNextItemWidth(-1);
                        if (ImGui.Combo($"###{entry.Value.Name}_{entry.Key}_turninMode", ref currentModeIndex, modes, modes.Length))
                        {
                            mission.TurnInSilver = mission.TurnInASAP = mission.ManualMode = false;
                            switch (modes[currentModeIndex])
                            {
                                case "Silver":
                                    mission.TurnInSilver = true;
                                    break;
                                case "ASAP":
                                    mission.TurnInASAP = true;
                                    break;
                                case "Manual":
                                    mission.ManualMode = true;
                                    break;
                            }

                            C.Save();
                        }

                        if (showGatherConfig)
                            ImGui.TableNextColumn();
                        if (GatheringJobList.Contains((int)entry.Value.JobId) || GatheringJobList.Contains((int)entry.Value.JobId2))
                        {
                            ImGui.SetNextItemWidth(-1);
                            if (ImGui.BeginCombo($"###GatherProfile{entry.Value.Name}_{entry.Key}", mission.GatherSetting.Name))
                            {
                                foreach (var profile in C.GatherSettings)
                                {
                                    bool isSelected = mission.GatherSettingId == profile.Id;

                                    if (ImGui.Selectable(profile.Name, isSelected))
                                    {
                                        mission.GatherSettingId = profile.Id;
                                    }

                                    if (isSelected)
                                        ImGui.SetItemDefaultFocus();
                                }

                                ImGui.EndCombo();
                            }

                        }

                        // debug
                        ImGui.TableNextColumn();
                        bool hasPreviousNotes = false;
                        if (entry.Value.Weather != CosmicWeather.FairSkies)
                        {
                            hasPreviousNotes = true;

                            ImGui.Text(entry.Value.Weather.ToString());
                        }
                        else if (entry.Value.Time != 0)
                        {
                            hasPreviousNotes = true;

                            ImGui.Text($"{2 * (entry.Value.Time - 1)}:00 - {2 * (entry.Value.Time)}:00");
                        }
                        else if (entry.Value.PreviousMissionID != 0)
                        {
                            hasPreviousNotes = true;

                            var (Id, Name) = MissionInfoDict.Where(m => m.Key == entry.Value.PreviousMissionID).Select(m => (Id: m.Key, Name: m.Value.Name)).FirstOrDefault();
                            ImGui.Text($"[{Id}] {Name}");
                        }
                        if (entry.Value.JobId2 != 0)
                        {
                            if (hasPreviousNotes) ImGui.SameLine();
                            ImGui.Text($"{jobOptions.Find(job => job.Id == entry.Value.JobId + 1).Name}/{jobOptions.Find(job => job.Id == entry.Value.JobId2 + 1).Name}");
                        }
                    }

                    ImGui.EndTable();
                }
            }
        }

        private static string newProfileName = ""; // This should be outside the function and persist

        public static void DrawConfigTab()
        {
            var tab = ImRaii.TabItem("Config");

            if (!tab)
                return;

            DrawLink("Say no to global warming, support Ice today: ", "https://ko-fi.com/ice643269", "https://ko-fi.com/ice643269");

            if (ImGui.CollapsingHeader("Safety Settings"))
            {
                if (ImGui.Checkbox("[Experimental] Animation Lock Unstuck", ref animationLockAbandon))
                {
                    C.AnimationLockAbandon = animationLockAbandon;
                    C.Save();
                }
                ImGui.Checkbox("[Experimental] Animation Lock Manual Unstuck", ref SchedulerMain.AnimationLockAbandonState);

                if (ImGui.Checkbox("Stop on Out of Materials", ref stopOnAbort))
                {
                    C.StopOnAbort = stopOnAbort;
                    C.Save();
                }
                ImGuiEx.HelpMarker(
                    "Warning! This is a safety feature to avoid wasting time on broken crafts!\n" +
                    "If you abort, you need to fix your ICE/Artisan settings or gear!\n" +
                    "You have been warned. Disable at your own risk."
                );

                if (ImGui.Checkbox("Ignore non-Cosmic prompts", ref rejectUnknownYesNo))
                {
                    C.RejectUnknownYesno = rejectUnknownYesNo;
                    C.Save();
                }
                ImGuiEx.HelpMarker(
                    "Warning! This is a safety feature to avoid joining random parties!\n" +
                    "If you you uncheck this, YOU WILL JOIN random party invites.\n" +
                    "You have been warned. Disable at your own risk."
                );
                if (ImGui.Checkbox("Add delay to mission menu", ref delayGrabMission))
                {
                    C.DelayGrabMission = delayGrabMission;
                    C.Save();
                }
                ImGuiEx.HelpMarker(
                    "This is here for safety! If you want to decrease the delay between missions be my guest.\n" +
                    "Safety is around... 250? If you're having animation locks you can absolutely increase it higher\n" +
                    "Or if you're feeling daredevil. Lower it. I'm not your dad (will tell dad jokes though.");
                if (delayGrabMission)
                {
                    ImGui.SetNextItemWidth(150);
                    ImGui.SameLine();
                    if (ImGui.SliderInt("ms###Mission", ref delayAmount, 0, 1000))
                    {
                        if (C.DelayIncrease != delayAmount)
                        {
                            C.DelayIncrease = delayAmount;
                            C.Save();
                        }
                    }
                }
                if (ImGui.Checkbox("Add delay to crafting menu", ref delayCraft))
                {
                    C.DelayCraft = delayCraft;
                    C.Save();
                }
                ImGuiEx.HelpMarker(
                    "This is here for safety! If you want to decrease the delay before turnin be my guest.\n" +
                    "Safety is around... 2500? If you're having animation locks you can absolutely increase it higher\n" +
                    "Or if you're feeling daredevil. Lower it. I'm not your dad (will tell dad jokes though.");
                if (delayCraft)
                {
                    ImGui.SetNextItemWidth(150);
                    ImGui.SameLine();
                    if (ImGui.SliderInt("ms###Crafting", ref delayCraftAmount, 0, 10000))
                    {
                        if (C.DelayCraftIncrease != delayCraftAmount)
                        {
                            C.DelayCraftIncrease = delayCraftAmount;
                            C.Save();
                        }
                    }
                }
            }

            if (ImGui.CollapsingHeader("Mission Settings"))
            {
                if (ImGui.Checkbox("Only Grab Mission", ref onlyGrabMission))
                {
                    C.OnlyGrabMission = onlyGrabMission;
                    C.Save();
                }
                if (ImGui.Checkbox("Stop @ Cosmocredits", ref stopOnceHitCosmoCredits))
                {
                    C.StopOnceHitCosmoCredits = stopOnceHitCosmoCredits;
                    C.Save();
                }
                if (stopOnceHitCosmoCredits)
                {
                    ImGui.SetNextItemWidth(150);
                    ImGui.SameLine();
                    if (ImGui.SliderInt("###Cosmocredits", ref cosmoCreditsCap, 0, 30000))
                    {
                        if (C.CosmoCreditsCap != cosmoCreditsCap)
                        {
                            C.CosmoCreditsCap = cosmoCreditsCap;
                            C.Save();
                        }
                    }
                }
                if (ImGui.Checkbox("Stop @ Lunar Credits", ref stopOnceHitLunarCredits))
                {
                    C.StopOnceHitLunarCredits = stopOnceHitLunarCredits;
                    C.Save();
                }
                if (stopOnceHitLunarCredits)
                {
                    ImGui.SetNextItemWidth(150);
                    ImGui.SameLine();
                    if (ImGui.SliderInt("###Lunar Credits", ref lunarCreditsCap, 0, 10000))
                    {
                        if (C.LunarCreditsCap != lunarCreditsCap)
                        {
                            C.LunarCreditsCap = lunarCreditsCap;
                            C.Save();
                        }
                    }
                }
                if (ImGui.Checkbox("Stop after @ level", ref stopWhenLevel))
                {
                    C.StopWhenLevel = stopWhenLevel;
                    C.Save();
                }
                if (stopWhenLevel)
                {
                    ImGui.SetNextItemWidth(100f);
                    ImGui.SameLine();
                    if (ImGui.InputInt("Level", ref targetLevel))
                    {
                        if (targetLevel < MinimumLevel)
                            targetLevel = MinimumLevel;
                        else if (targetLevel > MaximumLevel)
                            targetLevel = MaximumLevel;
                        C.TargetLevel = targetLevel;
                        C.Save();
                    }
                }
            }

            void DrawBuffSetting(string label, string uniqueId, bool currentEnabled, int currentMinGp, int minGpLimit, int maxGpLimit, string entryName, Action<bool> onEnabledChange, Action<int> onMinGpChange)
            {
                bool enabled = currentEnabled;
                if (ImGui.Checkbox($"{label}###Enable{uniqueId}", ref enabled))
                {
                    if (enabled != currentEnabled)
                        onEnabledChange(enabled);
                }

                if (enabled)
                {
                    if (ImGui.TreeNode($"{label} Settings###Tree{uniqueId}{entryName}"))
                    {
                        int minGp = currentMinGp;
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text("Minimum GP");
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(200);
                        if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                        {
                            if (minGp != currentMinGp)
                                onMinGpChange(minGp);
                        }

                        ImGui.TreePop();
                    }
                }
            }

#if DEBUG
            var headerColor = new Vector4(0.2f, 0.5f, 0.7f, 1.0f); // Light blue

            ImGui.PushStyleColor(ImGuiCol.Header, headerColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, headerColor * 1.2f);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, headerColor * 1.5f);

            if (ImGui.CollapsingHeader("Gathering Settings"))
            {
                ImGui.PopStyleColor(3);
                int maxGp = 1200;

                ImGui.InputText("New Profile Name", ref newProfileName, 64);
                if (ImGui.Button("Add Profile") && !string.IsNullOrWhiteSpace(newProfileName))
                {
                    if (!C.GatherSettings.Any(x => x.Name == newProfileName))
                    {
                        int newId = C.GatherSettings.Max(x => x.Id) + 1;
                        C.GatherSettings.Add(new GatherBuffProfile { Id = newId, Name = newProfileName });
                        C.Save();
                        newProfileName = ""; // Reset input
                    }
                }

                ImGui.Text("Gather Profiles");

                ImGui.BeginChild("GatherProfileChild", new Vector2(300, ImGui.GetTextLineHeightWithSpacing() * 5 + 10), true);
                for (int i = 0; i < C.GatherSettings.Count; i++)
                {
                    bool isSelected = (i == C.SelectedGatherIndex);

                    if (ImGui.Selectable(C.GatherSettings[i].Name, isSelected))
                    {
                        C.SelectedGatherIndex = i;
                        C.Save();
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndChild();


                bool canDelete = C.GatherSettings.Count > 1 && C.SelectedGatherIndex != 0;
                using (ImRaii.Disabled(!canDelete))
                {
                    if (ImGui.Button("Delete Selected Profile"))
                    {
                        var deletedProfile = C.GatherSettings[C.SelectedGatherIndex];
                        int deletedId = deletedProfile.Id;

                        // Remove the profile
                        C.GatherSettings.RemoveAt(C.SelectedGatherIndex);

                        // Update all missions using this GatherSettingId
                        foreach (var mission in C.Missions)
                        {
                            if (mission.GatherSettingId == deletedId)
                            {
                                mission.GatherSettingId = C.GatherSettings[0].Id; // fallback to default
                            }
                        }

                        // Clamp the selected index and save
                        C.SelectedGatherIndex = Math.Clamp(C.SelectedGatherIndex, 0, C.GatherSettings.Count - 1);
                        C.Save();
                    }
                }



                GatherBuffProfile entry = C.GatherSettings[C.SelectedGatherIndex];


                // Boon Increase 2 (+30% Increase)
                DrawBuffSetting(
                    label: "Boon Increase 2",
                    uniqueId: $"Boon2Inc{entry.Id}",
                    currentEnabled: entry.Buffs.BoonIncrease2,
                    currentMinGp: entry.Buffs.BoonIncrease2Gp,
                    minGpLimit: 100,
                    maxGpLimit: maxGp,
                    entryName: entry.Name,
                    onEnabledChange: newVal =>
                    {
                        entry.Buffs.BoonIncrease2 = newVal;
                        C.Save();
                    },
                    onMinGpChange: newVal =>
                    {
                        entry.Buffs.BoonIncrease2Gp = newVal;
                        C.Save();
                    }
                );

                // Boon Increase 1 (+10% Increase)
                DrawBuffSetting(
                    label: "Boon Increase 1",
                    uniqueId: $"Boon1Inc{entry.Id}",
                    currentEnabled: entry.Buffs.BoonIncrease1,
                    currentMinGp: entry.Buffs.BoonIncrease1Gp,
                    minGpLimit: 50,
                    maxGpLimit: maxGp,
                    entryName: entry.Name,
                    onEnabledChange: newVal =>
                    {
                        entry.Buffs.BoonIncrease1 = newVal;
                        C.Save();
                    },
                    onMinGpChange: newVal =>
                    {
                        entry.Buffs.BoonIncrease1Gp = newVal;
                        C.Save();
                    }
                );

                // Tidings (+2 to boon instead of +1)
                DrawBuffSetting(
                    label: "Tidings Buff",
                    uniqueId: $"TidingsBuff{entry.Id}",
                    currentEnabled: entry.Buffs.TidingsBool,
                    currentMinGp: entry.Buffs.TidingsGp,
                    minGpLimit: 200,
                    maxGpLimit: maxGp,
                    entryName: entry.Name,
                    onEnabledChange: newVal =>
                    {
                        entry.Buffs.TidingsBool = newVal;
                        C.Save();
                    },
                    onMinGpChange: newVal =>
                    {
                        entry.Buffs.TidingsGp = newVal;
                        C.Save();
                    }
                );

                // Yield II (+2 to all items on node)
                DrawBuffSetting(
                    label: "Blessed/Kings Yield II",
                    uniqueId: $"Blessed/KingsYieldIIBuff{entry.Id}",
                    currentEnabled: entry.Buffs.YieldII,
                    currentMinGp: entry.Buffs.YieldIIGp,
                    minGpLimit: 500,
                    maxGpLimit: maxGp,
                    entryName: entry.Name,
                    onEnabledChange: newVal =>
                    {
                        entry.Buffs.YieldII = newVal;
                        C.Save();
                    },
                    onMinGpChange: newVal =>
                    {
                        entry.Buffs.YieldIIGp = newVal;
                        C.Save();
                    }
                );

                // Yield I (+1 to all items on node)
                DrawBuffSetting(
                    label: "Blessed/Kings Yield I",
                    uniqueId: $"Blessed/KingsYieldIBuff{entry.Id}",
                    currentEnabled: entry.Buffs.YieldI,
                    currentMinGp: entry.Buffs.YieldIGp,
                    minGpLimit: 400,
                    maxGpLimit: maxGp,
                    entryName: entry.Name,
                    onEnabledChange: newVal =>
                    {
                        entry.Buffs.YieldI = newVal;
                        C.Save();
                    },
                    onMinGpChange: newVal =>
                    {
                        entry.Buffs.YieldIGp = newVal;
                        C.Save();
                    }
                );

                // Bonus Integrity (+1 integrity)
                DrawBuffSetting(
                    label: "Increase Integrity",
                    uniqueId: $"Incrase Intregity{entry.Id}",
                    currentEnabled: entry.Buffs.BonusIntegrity,
                    currentMinGp: entry.Buffs.BonusIntegrityGp,
                    minGpLimit: 300,
                    maxGpLimit: maxGp,
                    entryName: entry.Name,
                    onEnabledChange: newVal =>
                    {
                        entry.Buffs.BonusIntegrity = newVal;
                        C.Save();
                    },
                    onMinGpChange: newVal =>
                    {
                        entry.Buffs.BonusIntegrityGp = newVal;
                        C.Save();
                    }
                );
            }
            else
            {
                ImGui.PopStyleColor(3);
            }
#endif

            if (ImGui.CollapsingHeader("Overlay Settings"))
            {
                if (ImGui.Checkbox("Show Overlay", ref showOverlay))
                {
                    C.ShowOverlay = showOverlay;
                    C.Save();
                }

                if (ImGui.Checkbox("Show Seconds", ref ShowSeconds))
                {
                    C.ShowSeconds = ShowSeconds;
                    C.Save();
                }
            }

            if (ImGui.CollapsingHeader("Table Settings"))
            {
                // Checkbox: Hide unsupported missions.
                if (ImGui.Checkbox("Hide unsupported missions", ref hideUnsupported))
                {
                    C.HideUnsupportedMissions = hideUnsupported;
                    C.Save();
                }
                if (ImGui.Checkbox("Auto Pick Current Job", ref autoPickCurrentJob))
                {
                    C.AutoPickCurrentJob = autoPickCurrentJob;
                    C.Save();
                }
                if (ImGui.Checkbox($"Show EXP on Columns", ref showExp))
                {
                    C.ShowExpColums = showExp;
                    C.Save();
                }
                if (ImGui.Checkbox($"Show Cosmocredits", ref showCredits))
                {
                    C.ShowCreditsColumn = showCredits;
                    C.Save();
                }
            }

            if (ImGui.CollapsingHeader("Misc Settings"))
            {
                if (ImGui.Checkbox("Enable Auto Sprint", ref EnableAutoSprint))
                {
                    C.EnableAutoSprint = EnableAutoSprint;
                    C.Save();
                }
            }

#if DEBUG
            if (ImGui.CollapsingHeader("Debug Settings"))
            {
                ImGui.Checkbox("Force OOM Main", ref SchedulerMain.DebugOOMMain);
                ImGui.Checkbox("Force OOM Sub", ref SchedulerMain.DebugOOMSub);
                ImGui.Checkbox("Legacy Failsafe WKSRecipe Select", ref C.FailsafeRecipeSelect);

                var missionMap = new List<(string name, Func<byte> get, Action<byte> set)>
                {
                    ("Sequence Missions", new Func<byte>(() => C.SequenceMissionPriority), new Action<byte>(v => { C.SequenceMissionPriority = v; C.Save(); })),
                    ("Timed Missions", new Func<byte>(() => C.TimedMissionPriority), new Action<byte>(v => { C.TimedMissionPriority = v; C.Save(); })),
                    ("Weather Missions", new Func<byte>(() => C.WeatherMissionPriority), new Action<byte>(v => { C.WeatherMissionPriority = v; C.Save(); }))
                };

                var sorted = missionMap
                    .Select((m, i) => new { Index = i, Name = m.name, Priority = m.get() })
                    .OrderBy(m => m.Priority)
                    .ToList();
                ImGuiHelpers.ScaledDummy(5, 0);
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Provision Mission Priority"))
                {
                    for (int i = 0; i < sorted.Count; i++)
                    {
                        var item = sorted[i];
                        ImGuiHelpers.ScaledDummy(5, 0);
                        ImGui.SameLine();
                        ImGui.Selectable(item.Name);
                        if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                        {
                            int nextIndex = i + (ImGui.GetMouseDragDelta(0).Y < 0f ? -1 : 1);
                            if (nextIndex >= 0 && nextIndex < sorted.Count)
                            {
                                // Swap the priority values
                                var otherItem = sorted[nextIndex];

                                // Swap their priority values via the original setters
                                byte temp = missionMap[item.Index].get();
                                missionMap[item.Index].set(missionMap[otherItem.Index].get());
                                missionMap[otherItem.Index].set(temp);
                                ImGui.ResetMouseDragDelta();
                            }
                        }
                    }
                }

                if (ImGui.Button("Get Sinus Forecast"))
                {
                    List<WeatherForecast> forecast = WeatherForecastHandler.GetTerritoryForecast(1237);
                    Func<WeatherForecast, string> formatTime = (forecast) => WeatherForecastHandler.FormatForecastTime(forecast.Time);

                    Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                    {
                        Message = $"Sinus Ardorum Weather - {forecast[0].Name}",
                        Type = Dalamud.Game.Text.XivChatType.Echo,
                    });
                    for (int i = 1; i < forecast.Count; i++)
                    {
                        Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                        {
                            Message = $"{forecast[i].Name} In {formatTime(forecast[i])}",
                            Type = Dalamud.Game.Text.XivChatType.Echo,
                        });
                    }
                }

                using (ImRaii.Disabled(!PlayerHelper.IsInCosmicZone()))
                {
                    if (ImGui.Button("Refresh Forecast"))
                    {
                        WeatherForecastHandler.GetForecast();
                    }
                }
            }
#endif

            ImGui.EndTabItem();
        }

        public static void DrawLink(string label, string link, string url)
        {
            ImGui.Text(label);
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.TankBlue);
            ImGui.Text(link);
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            if (ImGui.IsItemClicked())
            {
                Dalamud.Utility.Util.OpenLink(url);
            }
        }

        public static void CenterTextInTableCell(string text)
        {
            float cellWidth = ImGui.GetContentRegionAvail().X;
            float textWidth = ImGui.CalcTextSize(text).X;
            float offset = (cellWidth - textWidth) * 0.5f;

            if (offset > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

            ImGui.TextUnformatted(text);
        }

        public static void CenterCheckboxInTableCell(string label, ref bool value, ref bool config)
        {
            float cellWidth = ImGui.GetContentRegionAvail().X;
            float checkboxWidth = ImGui.CalcTextSize(label).X + ImGui.GetFrameHeight(); // estimate checkbox width
            float offset = (cellWidth - checkboxWidth) * 0.5f;

            if (offset > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

            if (ImGui.Checkbox(label, ref value))
            {

            }
        }
        private static List<uint> GetOnlyPreviousMissionsRecursive(uint missionId)
        {
            if (!MissionInfoDict.TryGetValue(missionId, out var missionInfo) || missionInfo.PreviousMissionID == 0)
                return [];

            var chain = GetOnlyPreviousMissionsRecursive(missionInfo.PreviousMissionID);
            chain.Add(missionInfo.PreviousMissionID);
            return chain;
        }

        private static List<uint> GetOnlyNextMissionsRecursive(uint missionId)
        {
            uint? nextMissionId = MissionInfoDict
                .Where(m => m.Value.PreviousMissionID == missionId)
                .Select(m => (uint?)m.Key)
                .FirstOrDefault();

            if (!nextMissionId.HasValue)
                return [];

            var chain = new List<uint> { nextMissionId.Value };
            chain.AddRange(GetOnlyNextMissionsRecursive(nextMissionId.Value));
            return chain;
        }
    }
}
