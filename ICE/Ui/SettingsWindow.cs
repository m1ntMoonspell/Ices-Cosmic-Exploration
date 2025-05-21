using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.ExcelServices;
using ECommons.Funding;
using ICE.Utilities.Cosmic;
using System.Collections.Generic;

namespace ICE.Ui;

// This isn't currently wired up to anything. Can actually use this to place all the general settings for all the windows...
internal class SettingsWindow : Window
{
    public SettingsWindow() :
        base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICESettingsWindow")
    {
        Flags = ImGuiWindowFlags.None;
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(100, 100),
            MaximumSize = new Vector2(2000, 2000),
        };
        P.windowSystem.AddWindow(this);
        AllowPinning = true;
    }

    public void Dispose()
    {
        P.windowSystem.RemoveWindow(this);
    }


    public override void Draw()
    {
        Kofi.DrawRight();
        ImGuiEx.EzTabBar("Ice Cosmic Settings Tab", Kofi.Text 
            ,("Safety Settings", SafetySettings, null, true)
            ,("Gathering Config", GatherSettings, null, true)
            ,("Overlay", Overlay, null, true)
            ,("Misc", Misc, null, true)
#if DEBUG
            ,("Gamble Wheel Settings", GambaWheel, null, true)
            ,("Debug", Debug, null, true)
#endif
        );
    }

    private bool animationLockAbandon = C.AnimationLockAbandon;
    private bool stopOnAbort = C.StopOnAbort;
    private bool rejectUnknownYesNo = C.RejectUnknownYesno;
    private bool delayGrabMission = C.DelayGrabMission;
    private int delayAmount = C.DelayIncrease;
    private bool delayCraft = C.DelayCraft;
    private int delayCraftAmount = C.DelayCraftIncrease;

    private void SafetySettings()
    {
        if (ImGui.Checkbox("[Experimental] Animation Lock Unstuck", ref animationLockAbandon))
        {
            C.AnimationLockAbandon = animationLockAbandon;
            C.Save();
        }
        ImGui.Checkbox("[Experimental] Animation Lock Manual Unstuck", ref SchedulerMain.AnimationLockAbandonState);

        if (ImGui.Checkbox("Stop on Errors", ref stopOnAbort))
        {
            C.StopOnAbort = stopOnAbort;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "Warning! This is a safety feature to stop if something goes wrong!\n" +
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

    private bool SelfRepairGather = C.SelfRepairGather;
    private float SelfRepairPercent = C.RepairPercent;
    private bool SelfSpiritbondGather = C.SelfSpiritbondGather;
    private string newProfileName = "";

    private string[] MissionTypes = ["Limited Nodes", "Gather x Amount", "Time Attack", "Chained Scoring", "Boon Scoring", "Chain + Boon Scoring", "Dual Class"];
    private int MissionIndex = 0;

    private void GatherSettings()
    {
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
            ImGui.Indent(15);
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
            ImGui.Unindent(15);
        }
        if (ImGui.Checkbox("Extract Spiritbond on Gather", ref SelfSpiritbondGather))
        {
            if (C.SelfSpiritbondGather != SelfSpiritbondGather)
            {
                C.SelfSpiritbondGather = SelfSpiritbondGather;
                C.Save();
            }
        }

        ImGui.Dummy(new(0, 5));

        ImGui.SetNextItemWidth(200);
        ImGui.InputText("New Profile Name", ref newProfileName, 64);
        using (ImRaii.Disabled(newProfileName == ""))
        {
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
        }

        ImGui.Columns(2, "Gather Settings Columns", false);

        // ------------------ 
        //  Left Column, Profile Settings
        // ------------------
        ImGui.SetColumnWidth(0, 350);

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

        GatherBuffProfile entry = C.GatherSettings[C.SelectedGatherIndex];

        ImGui.Combo("Mission Type", ref MissionIndex, MissionTypes, MissionTypes.Length);
        if (ImGui.Button("Apply to Mission Types"))
        {
            foreach (var mission in C.Missions)
            {
                var id = mission.Id;

                var missionDict = CosmicHelper.MissionInfoDict[id];

                bool craftMission = missionDict.Attributes.HasFlag(MissionAttributes.Craft);
                bool gatherMission = missionDict.Attributes.HasFlag(MissionAttributes.Gather);

                bool LimitedQuant = missionDict.Attributes.HasFlag(MissionAttributes.Limited);
                // Gather X Amount is just "Gather" 
                bool TimedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining);
                bool ChainedMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreChains);
                bool BoonMission = missionDict.Attributes.HasFlag(MissionAttributes.ScoreGatherersBoon);
                bool collectableMission = missionDict.Attributes.HasFlag(MissionAttributes.Collectables);
                bool stellerReductionMission = missionDict.Attributes.HasFlag(MissionAttributes.ReducedItems);

                bool GatherX = !stellerReductionMission && !collectableMission && !BoonMission && !ChainedMission && !TimedMission && !LimitedQuant;

                void UpdateMissions()
                {
                    mission.GatherSettingId = entry.Id;
                }

                if (gatherMission && (!collectableMission && !stellerReductionMission))
                {
                    if (MissionIndex == 0 && LimitedQuant)
                        UpdateMissions();
                    else if (MissionIndex == 2 && TimedMission)
                        UpdateMissions();
                    else if (MissionIndex == 3 && ChainedMission && !BoonMission)
                        UpdateMissions();
                    else if (MissionIndex == 4 && BoonMission && !ChainedMission)
                        UpdateMissions();
                    else if (MissionIndex == 5 && ChainedMission && BoonMission)
                        UpdateMissions();
                    else if (MissionIndex == 6 && craftMission)
                        UpdateMissions();
                    else if (MissionIndex == 1 && GatherX)
                        UpdateMissions();
                }
            }

            C.Save();
        }

        // ---------------------------------
        // Right Column, Gathering setttings
        // ---------------------------------

        ImGui.NextColumn();
        ImGui.SetColumnWidth(1, ImGui.GetWindowWidth() - 300);

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

        ImGui.Columns(1);
    }

    private bool gambaEnabled = C.GambaEnabled;
    private int gambaDelay = C.GambaDelay;
    private int gambaCreditsMinimum = C.GambaCreditsMinimum;
    private bool gambaPreferSmallerWheel = C.GambaPreferSmallerWheel;

    private void GambaWheel()
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

    private bool showOverlay = C.ShowOverlay;
    private bool ShowSeconds = C.ShowSeconds;

    private void Overlay()
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

    private bool EnableAutoSprint = C.EnableAutoSprint;

    private void Misc()
    {
        if (ImGui.Checkbox("Enable Auto Sprint", ref EnableAutoSprint))
        {
            C.EnableAutoSprint = EnableAutoSprint;
            C.Save();
        }
    }

#if DEBUG

    private void Debug()
    {
        ImGui.Checkbox("Force OOM Main", ref SchedulerMain.DebugOOMMain);
        ImGui.Checkbox("Force OOM Sub", ref SchedulerMain.DebugOOMSub);
        ImGui.Checkbox("Legacy Failsafe WKSRecipe Select", ref C.FailsafeRecipeSelect);
        ImGui.SliderInt("TSP Path Length", ref SchedulerMain.TSPLength, 2, 20);

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
}
