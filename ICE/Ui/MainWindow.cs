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
using ECommons.ExcelServices;
using Dalamud.Interface;

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
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindowOld")
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
        private static bool SelfRepairGather = C.SelfRepairGather;
        private static float SelfRepairPercent = C.RepairPercent;
        private static bool gambaEnabled = C.GambaEnabled;
        private static bool gambaPreferSmallerWheel = C.GambaPreferSmallerWheel;
        private static int gambaCreditsMinimum = C.GambaCreditsMinimum;
        private static int gambaDelay = C.GambaDelay;

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
            ImGui.Text("运行");
            // Help marker explaining how missions are selected and run.
            ImGuiEx.HelpMarker(
                "请注意：运行时会基于所有能运行的项目\n" +
                "如果你同时勾选了C和D级的任务，运行时会先检查C级任务然后再检查D级任务\n" +
                "运行时会循环检查所有任务直到插件找到一个你已经选中的任务\n" +
                "不支持的任务会被禁用并以红色标出，勾选'隐藏不支持的任务'可以过滤掉这些任务"
            );

            ImGui.Text($"Current state: " + SchedulerMain.State.ToString());


            ImGui.Spacing();

            // Start button (disabled while already ticking).
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
            {
                if (ImGui.Button("开始"))
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            // Stop button (disabled while not ticking).
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("停止"))
                {
                    SchedulerMain.DisablePlugin();
                }
            }

            ImGui.SameLine();
            ImGui.Checkbox("在完成当前任务后停止", ref SchedulerMain.StopBeforeGrab);

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
            if (ImGui.BeginCombo("分类", sortOptions[SortOption].SortOptionName))
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
            .Where(m => m.Value.Attributes.HasFlag(MissionAttributes.Critical));
            criticalMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(criticalMissions);
            bool criticalGather = criticalMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
            DrawMissionsDropDown($"Critical Missions - {criticalMissions.Count(x => C.Missions.Any(y => y.Id == x.Key && y.Enabled))} enabled", criticalMissions, criticalGather);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> weatherRestrictedMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1 || m.Value.JobId2 == selectedJobId - 1)
                        .Where(m => m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather))
                        .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.Critical));
            bool weatherGather = weatherRestrictedMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
            weatherRestrictedMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(weatherRestrictedMissions);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> timeRestrictedMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1)
                        .Where(m => m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed));
            bool timeGather = timeRestrictedMissions.Any(g => GatheringJobList.Contains((int)g.Value.JobId) || GatheringJobList.Contains((int)g.Value.JobId2));
            timeRestrictedMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(timeRestrictedMissions);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> sequentialMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1)
                        .Where(m => m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential));
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
                        .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.Critical))
                        .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed))
                        .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalSequential))
                        .Where(m => !m.Value.Attributes.HasFlag(MissionAttributes.ProvisionalWeather));
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
                        ImGui.TableSetupColumn("宇宙信用点");
                        columnIndex++;

                        ImGui.TableSetupColumn("月球信用点");
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
                    ImGui.TableSetupColumn("提交", ImGuiTableColumnFlags.WidthFixed, 100);

                    if (showGatherConfig)
                    {
                        float columnWidth = ImGui.CalcTextSize("采集配置").X + 5;
                        ImGui.TableSetupColumn("采集配置", ImGuiTableColumnFlags.WidthFixed, columnWidth);
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

                        if (entry.Value.JobId2 != 0 || (entry.Value.JobId >= 16 && entry.Value.JobId <= 18))
                            unsupported = true;

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
                                ImGui.SetTooltip("当前任务仅能在手动模式下完成");
                            }
                        }
                        else
                        {
                            ImGui.Text($"{MissionName}");
                        }

                        MissionListInfo info = MissionInfoDict[mission.Id];
                        if (info.MarkerId != 0)
                        {
                            ImGui.SameLine();
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.Text(FontAwesomeIcon.Flag.ToIconString());
                            ImGui.PopFont();
                            if (ImGui.IsItemClicked())
                                Utils.SetGatheringRing(info.TerritoryId, info.X, info.Y, info.Radius, info.Name);
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
                        bool[] selectedModes;
                        if (unsupported)
                        {
                            modes = ["手动"];
                            selectedModes = [mission.ManualMode];
                        }
                        else
                        {
                            modes = ["金牌", "银牌", "铜牌", "手动"];
                            selectedModes =
                            [
                                mission.TurnInGold,
                                mission.TurnInSilver,
                                mission.TurnInASAP,
                                mission.ManualMode
                            ];
                        }

                        ImGui.SetNextItemWidth(-1);
                        bool changed = false;
                        if (ImGui.BeginCombo($"###{entry.Value.Name}_{entry.Key}_turninMode", string.Join(", ", modes.Where((m, i) => selectedModes[i]))))
                        {
                            for (int i = 0; i < modes.Length; i++)
                            {
                                bool selected = selectedModes[i];
                                if (ImGui.Selectable(modes[i], selected, ImGuiSelectableFlags.DontClosePopups))
                                {
                                    selectedModes[i] = !selected;
                                    changed = true;
                                }
                            }
                            ImGui.EndCombo();
                        }
                        if (changed)
                        {
                            if (unsupported)
                            {
                                mission.ManualMode = true;
                            }
                            else if (entry.Value.Attributes.HasFlag(MissionAttributes.ProvisionalTimed) || entry.Value.Attributes.HasFlag(MissionAttributes.Critical))
                            {
                                mission.TurnInASAP = true;
                                mission.ManualMode = selectedModes[1];
                            }
                            else
                            {
                                mission.TurnInGold = selectedModes[0];
                                mission.TurnInSilver = selectedModes[1];
                                mission.TurnInASAP = selectedModes[2];
                                mission.ManualMode = selectedModes[3];
                            }
                            C.Save();
                        }
                        if (showGatherConfig)
                        {
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

            if (ImGui.CollapsingHeader("安全设置"))
            {
                if (ImGui.Checkbox("[实验性功能] 解除动画锁", ref animationLockAbandon))
                {
                    C.AnimationLockAbandon = animationLockAbandon;
                    C.Save();
                }
                ImGui.Checkbox("[实验性功能] Animation Lock Manual Unstuck", ref SchedulerMain.AnimationLockAbandonState);

                if (ImGui.Checkbox("报错时停止", ref stopOnAbort))
                {
                    C.StopOnAbort = stopOnAbort;
                    C.Save();
                }
                ImGuiEx.HelpMarker(
                    "警告！这是在遇到错误时的安全措施！\n" +
                    "在此警告之后，禁用带来的风险由你自己承担。"
                );

                if (ImGui.Checkbox("忽略非宇宙探索提示", ref rejectUnknownYesNo))
                {
                    C.RejectUnknownYesno = rejectUnknownYesNo;
                    C.Save();
                }
                ImGuiEx.HelpMarker(
                    "警告！这是避免加入别人队伍的安全措施！\n" +
                    "如果不激活此选项，你会接受来自别人的组队邀请。\n" +
                    "在此警告之后，禁用带来的风险由你自己承担。"
                );
                if (ImGui.Checkbox("在任务界面增加延迟", ref delayGrabMission))
                {
                    C.DelayGrabMission = delayGrabMission;
                    C.Save();
                }
                ImGuiEx.HelpMarker(
                    "这项功能是为了安全而存在的！如果你想降低接取任务间的延迟，请便。\n" +
                    "安全范围大概是在250ms左右？如果你有动画锁的话你可以适当增加延迟。\n" +
                    "或者如果你不怕死的话拉到多低都没问题。I'm not your dad (will tell dad jokes though.");
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
                if (ImGui.Checkbox("在生产界面增加延迟", ref delayCraft))
                {
                    C.DelayCraft = delayCraft;
                    C.Save();
                }
                ImGuiEx.HelpMarker(
                    "这项功能是为了安全而存在的！如果你想降低提交物品前的延迟，请便。\n" +
                    "安全范围大概是在2500ms左右？如果你有动画锁的话你可以适当增加延迟。\n" +
                    "或者如果你不怕死的话拉到多低都没问题。 I'm not your dad (will tell dad jokes though.");
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

            if (ImGui.CollapsingHeader("任务设置"))
            {
                if (ImGui.Checkbox("仅接取任务", ref onlyGrabMission))
                {
                    C.OnlyGrabMission = onlyGrabMission;
                    C.Save();
                }
                if (ImGui.Checkbox("在指定数量宇宙信用点时停止", ref stopOnceHitCosmoCredits))
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
                if (ImGui.Checkbox("在指定数量月球信用点时停止", ref stopOnceHitLunarCredits))
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
                if (ImGui.Checkbox("在到达指定等级后停止", ref stopWhenLevel))
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

            void DrawBuffSetting(string label, string uniqueId, bool currentEnabled, int currentMinGp, int minGpLimit, int maxGpLimit, string entryName, string ActionInfo, Action<bool> onEnabledChange, Action<int> onMinGpChange)
            {
                bool enabled = currentEnabled;
                if (ImGui.Checkbox($"{label}###Enable{uniqueId}", ref enabled))
                {
                    if (enabled != currentEnabled)
                        onEnabledChange(enabled);
                }
                ImGuiEx.HelpMarker(ActionInfo);

                if (enabled)
                {
                    ImGui.Indent(15);

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
                    ImGui.Unindent(15);
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

                if (ImGui.Checkbox("Self Repair on Gather", ref SelfRepairGather))
                {
                    if (C.SelfRepairGather != SelfRepairGather)
                    {
                        C.SelfRepairGather = SelfRepairGather;
                        C.Save();
                    }
                }
                if (SelfRepairGather)
                {
                    ImGui.Text("Repair at");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150);
                    if (ImGui.SliderFloat("###Repair %", ref SelfRepairPercent, 0f, 99f, "%.0f%%"))
                    {
                        if (C.RepairPercent != SelfRepairPercent)
                        {
                            C.RepairPercent = (int)SelfRepairPercent;
                            C.Save();
                        }
                    }
                }

                ImGui.Dummy(new(0, 5));

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

                ImGui.SameLine();
                if (ImGui.BeginChild("GatherProfileSettings", new Vector2(500, 250)))
                {
                    GatherBuffProfile entry = C.GatherSettings[C.SelectedGatherIndex];

                    // Boon Increase 2 (+30% Increase)
                    DrawBuffSetting(
                        label: "Pioneer's / Mountaineer's Gift II",
                        uniqueId: $"Boon2Inc{entry.Id}",
                        currentEnabled: entry.Buffs.BoonIncrease2,
                        currentMinGp: entry.Buffs.BoonIncrease2Gp,
                        minGpLimit: 100,
                        maxGpLimit: maxGp,
                        entryName: entry.Name,
                        ActionInfo: "Apply a 30% buff to your boon chance.",
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
                        label: "Pioneer's / Mountaineer's Gift I",
                        uniqueId: $"Boon1Inc{entry.Id}",
                        currentEnabled: entry.Buffs.BoonIncrease1,
                        currentMinGp: entry.Buffs.BoonIncrease1Gp,
                        minGpLimit: 50,
                        maxGpLimit: maxGp,
                        entryName: entry.Name,
                        ActionInfo: "Apply a 10% buff to your boon chance.",
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
                        label: "Nophica's / Nald'thal's Tidings Buff",
                        uniqueId: $"TidingsBuff{entry.Id}",
                        currentEnabled: entry.Buffs.TidingsBool,
                        currentMinGp: entry.Buffs.TidingsGp,
                        minGpLimit: 200,
                        maxGpLimit: maxGp,
                        entryName: entry.Name,
                        ActionInfo: "Increases item yield from Gatherer's Boon by 1",
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
                        label: "Blessed / Kings Yield II",
                        uniqueId: $"Blessed/KingsYieldIIBuff{entry.Id}",
                        currentEnabled: entry.Buffs.YieldII,
                        currentMinGp: entry.Buffs.YieldIIGp,
                        minGpLimit: 500,
                        maxGpLimit: maxGp,
                        entryName: entry.Name,
                        ActionInfo: "Increases the number of items obtained when gathering by 2",
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
                        label: "Blessed / Kings Yield I",
                        uniqueId: $"Blessed/KingsYieldIBuff{entry.Id}",
                        currentEnabled: entry.Buffs.YieldI,
                        currentMinGp: entry.Buffs.YieldIGp,
                        minGpLimit: 400,
                        maxGpLimit: maxGp,
                        entryName: entry.Name,
                        ActionInfo: "Increases the number of items obtained when gathering by 1",
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
                        label: "Ageless Words / Solid Reason",
                        uniqueId: $"Incrase Intregity{entry.Id}",
                        currentEnabled: entry.Buffs.BonusIntegrity,
                        currentMinGp: entry.Buffs.BonusIntegrityGp,
                        minGpLimit: 300,
                        maxGpLimit: maxGp,
                        entryName: entry.Name,
                        ActionInfo: "Increase the Integrity by 1\n" +
                                    "50% chance to grant Eureka Moment",
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

                    // Bountiful Yield/Harvest II (+Amount based on gathering)
                    DrawBuffSetting(
                        label: "Bountiful Yield II / Bountiful Harvest II",
                        uniqueId: $"Bountiful Yield II {entry.Id}",
                        currentEnabled: entry.Buffs.BountifulYieldII,
                        currentMinGp: entry.Buffs.BountifulYieldIIGp,
                        minGpLimit: 100,
                        maxGpLimit: maxGp,
                        entryName: entry.Name,
                        ActionInfo: "Increase item's gained on next gathering attempt by 1, 2, or 3 \n" +
                                    "This is based on your gathering rating",
                        onEnabledChange: newVal =>
                        {
                            entry.Buffs.BountifulYieldII = newVal;
                            C.Save();
                        },
                        onMinGpChange: newVal =>
                        {
                            entry.Buffs.BountifulYieldIIGp = newVal;
                            C.Save();
                        }
                    );

                    ImGui.EndChild();
                }

            }
            else
            {
                ImGui.PopStyleColor(3);
            }
#endif

#if DEBUG
            TaskGamba.EnsureGambaWeightsInitialized();
            if (ImGui.CollapsingHeader("Gamba Settings"))
            {
                if (ImGui.Checkbox("Enable Gamba", ref gambaEnabled))
                {
                    C.GambaEnabled = gambaEnabled;
                    C.Save();
                }
                if (gambaEnabled)
                {
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150);
                    if (ImGui.SliderInt("Gamba Delay", ref gambaDelay, 50, 2000))
                    {
                        C.GambaDelay = gambaDelay;
                        C.Save();
                    }
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(150);
                    if (ImGui.SliderInt("Mininum credits to keep", ref gambaCreditsMinimum, 0, 10000))
                    {
                        C.GambaCreditsMinimum = gambaCreditsMinimum;
                        C.Save();
                    }
                }
                if (ImGui.Checkbox("Prefer smaller wheel", ref gambaPreferSmallerWheel))
                {
                    C.GambaPreferSmallerWheel = gambaPreferSmallerWheel;
                    C.Save();
                }
                ImGuiEx.HelpMarker("This will make the Gamba prefer wheels with less items.");
                ImGui.Separator();
                ImGui.TextUnformatted("Configure the weights for each item in the Gamba. Higher weight = more desirable.");
                ImGui.Spacing();
                foreach (GambaType type in Enum.GetValues(typeof(GambaType)))
                {
                    var itemsType = C.GambaItemWeights.Where(x => x.Type == type).OrderBy(x => x.ItemId).ToList();
                    if (itemsType.Count == 0) continue;
                    if (ImGui.TreeNodeEx($"{type} ({itemsType.Count})##gamba_type_{type}", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.Indent();
                        foreach (var gamba in itemsType)
                        {
                            var itemName = ExcelItemHelper.GetName(gamba.ItemId);
                            int weight = gamba.Weight;
                            ImGui.SetNextItemWidth(120f);
                            if (ImGui.InputInt($"[{gamba.ItemId}] {itemName}##gamba_weight", ref weight))
                            {
                                gamba.Weight = weight;
                                C.Save();
                            }
                        }
                        ImGui.Unindent();
                        ImGui.TreePop();
                    }
                }
                if (ImGui.Button("Reset Weights"))
                {
                    TaskGamba.EnsureGambaWeightsInitialized(true);
                }
            }
#endif
            if (ImGui.CollapsingHeader("悬浮窗设置"))
            {
                if (ImGui.Checkbox("显示悬浮窗", ref showOverlay))
                {
                    C.ShowOverlay = showOverlay;
                    C.Save();
                }

                if (ImGui.Checkbox("显示秒数", ref ShowSeconds))
                {
                    C.ShowSeconds = ShowSeconds;
                    C.Save();
                }
            }

            if (ImGui.CollapsingHeader("Table Settings"))
            {
                // Checkbox: Hide unsupported missions.
                if (ImGui.Checkbox("隐藏不支持的任务", ref hideUnsupported))
                {
                    C.HideUnsupportedMissions = hideUnsupported;
                    C.Save();
                }
                if (ImGui.Checkbox("自动选择当前职业b", ref autoPickCurrentJob))
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

            if (ImGui.CollapsingHeader("其他设置"))
            {
                if (ImGui.Checkbox("自动冲刺", ref EnableAutoSprint))
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
