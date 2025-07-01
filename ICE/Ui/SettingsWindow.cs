using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;

namespace ICE.Ui;

// This isn't currently wired up to anything. Can actually use this to place all the general settings for all the windows...
internal class SettingsWindow : Window
{
    public SettingsWindow() :
        base($"Ice的宇宙探险！ {P.GetType().Assembly.GetName().Version} ###ICESettingsWindow")
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
        ImGuiEx.EzTabBar("宇宙探索设置面板", Kofi.Text 
            ,("安全设置", SafetySettings, null, true)
            ,("采集配置", GatherSettings, null, true)
            ,("悬浮窗", Overlay, null, true)
            ,("其他设置", Misc, null, true)
            ,("宇宙好运道", GambaWheel, null, true)
#if DEBUG
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
        if (ImGui.Checkbox("[实验性功能] 解除动画锁", ref animationLockAbandon))
        {
            C.AnimationLockAbandon = animationLockAbandon;
            C.Save();
        }
        ImGui.Checkbox("[实验性功能] 手动解除动画锁", ref SchedulerMain.AnimationLockAbandonState);

        if (ImGui.Checkbox("遇到错误时停止", ref stopOnAbort))
        {
            C.StopOnAbort = stopOnAbort;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "警告！这是在遇到错误时的安全措施！\n" +
            "在此警告之后，禁用带来的风险自行承担。"
        );

        if (ImGui.Checkbox("忽略非宇宙探索提示", ref rejectUnknownYesNo))
        {
            C.RejectUnknownYesno = rejectUnknownYesNo;
            C.Save();
        }
        ImGuiEx.HelpMarker(
            "警告！这是避免加入别人队伍的安全措施！\n" +
            "如果不激活此选项，你会接受来自别人的组队邀请。\n" +
            "在此警告之后，禁用带来的风险自行承担。"
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

    private bool SelfRepairGather = C.SelfRepairGather;
    private float SelfRepairPercent = C.RepairPercent;
    private bool SelfSpiritbondGather = C.SelfSpiritbondGather;
    private bool AutoCordial = C.AutoCordial;
    private bool InverseCordialPrio = C.inverseCordialPrio;
    private bool UseOnFisher = C.UseOnFisher;
    private bool PreventOvercap = C.PreventOvercap;
    private int CordialMinGp = C.CordialMinGp;
    private bool useOnlyInMission = C.UseOnlyInMission;
    private string newProfileName = "";

    private string[] MissionTypes = ["限定节点", "Gather x Amount", "限时任务", "Chained Scoring", "Boon Scoring", "Chain + Boon Scoring", "双职业"];
    private int MissionIndex = 0;

    private void GatherSettings()
    {
        void DrawBuffSetting(string label, string uniqueId, bool currentEnabled, int currentMinGp, int minGpLimit, int maxGpLimit, string entryName, string ActionInfo, Action<bool> onEnabledChange, Action<int> onMinGpChange, int currentMaxUse, Action<int> onMaxUseChange)
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
                    ImGui.Text("最低GP");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                    {
                        if (minGp != currentMinGp)
                            onMinGpChange(minGp);
                    }
                    int maxUse = currentMaxUse;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("最大使用次数");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt($"###Slider{uniqueId}{entryName}_1", ref maxUse, 1))
                    {
                        if (maxUse != currentMaxUse)
                            onMaxUseChange(maxUse);
                    }
                    ImGuiEx.HelpMarker("设置为-1即可不限制使用次数 \n" +
                                       "设置为1到X来设置每一个任务重的使用次数");

                    ImGui.TreePop();
                }
                ImGui.Unindent(15);
            }
        }

        void DrawCustomBuffSetting(string label, string uniqueId, bool currentEnabled, int currentMinGp, int minGpLimit, int maxGpLimit, string entryName, string ActionInfo, Action<bool> onEnabledChange, Action<int> onMinGpChange, int currentMaxUse, Action<int> onMaxUseChange, int MinItemUsage, Action<int> onMinItemMaxUseChange)
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
                    ImGui.Text("最低GP");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200);
                    if (ImGui.SliderInt($"###Slider{uniqueId}{entryName}", ref minGp, minGpLimit, maxGpLimit))
                    {
                        if (minGp != currentMinGp)
                            onMinGpChange(minGp);
                    }
                    int maxUse = currentMaxUse;
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text("最大使用次数");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.InputInt($"###Slider{uniqueId}{entryName}_1", ref maxUse, 1))
                    {
                        if (maxUse != currentMaxUse)
                            onMaxUseChange(maxUse);
                    }
                    ImGuiEx.HelpMarker("设置为-1即可不限制使用次数 \n" +
                                       "设置为1到X来设置每一个任务中的最多使用次数");

                    int MinItem = MinItemUsage;
                    ImGui.Text($"Minimum BYII Item");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    if (ImGui.SliderInt($"###MinItemsBYII{uniqueId}{entryName}_1", ref MinItem, 2, 4))
                    {
                        if (MinItem != MinItemUsage)
                            onMinItemMaxUseChange(MinItem);
                    }
                    ImGuiEx.HelpMarker($"Set the minimum amount of items that you want BYII to activate on\n" +
                                       $"Ex. Setting it to 2 will make it to where if you only activate if you need need 2 or more items\n" +
                                       $"在双职业任务和采集N个数量物品上可以节省GP");

                    ImGui.TreePop();
                }
                ImGui.Unindent(15);
            }
        }

        int maxGp = 1200;

        if (ImGui.Checkbox("采集过程中自动修理装备", ref SelfRepairGather))
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
            ImGui.Text("耐久为");
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
        if (ImGui.Checkbox("自动精炼魔晶石", ref SelfSpiritbondGather))
        {
            if (C.SelfSpiritbondGather != SelfSpiritbondGather)
            {
                C.SelfSpiritbondGather = SelfSpiritbondGather;
                C.Save();
            }
        }
        if (ImGui.Checkbox("自动使用强心剂", ref AutoCordial))
        {
            C.AutoCordial = AutoCordial;
            C.Save();
        }
        ImGuiEx.HelpMarker("只在使用ICE时有效，手动模式下无效\n" +
                           "在月球探索时会停止pandora强心剂使用相关功能");
        if (AutoCordial)
        {
            if (ImGui.TreeNode("强心剂设置"))
            {
                if (ImGui.Checkbox("反转使用优先级 (轻型 -> 普通 -> 高级)", ref InverseCordialPrio))
                {
                    C.inverseCordialPrio = InverseCordialPrio;
                    C.Save();
                }
                if (ImGui.Checkbox("防止GP溢出", ref PreventOvercap))
                {
                    C.PreventOvercap = PreventOvercap;
                    C.Save();
                }
                if (ImGui.Checkbox("职业为捕鱼人时使用", ref UseOnFisher))
                {
                    C.UseOnFisher = UseOnFisher;
                    C.Save();
                }
                if (ImGui.Checkbox("仅在任务中使用", ref useOnlyInMission))
                {
                    C.UseOnlyInMission = useOnlyInMission;
                    C.Save();
                }
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderInt("GP阈值", ref CordialMinGp, 0, maxGp))
                {
                    C.CordialMinGp = CordialMinGp;
                    C.Save();
                }

                ImGui.TreePop();
            }
        }

        ImGui.Dummy(new(0, 5));

        ImGui.SetNextItemWidth(200);
        ImGui.InputText("新方案名称", ref newProfileName, 64);
        using (ImRaii.Disabled(newProfileName == ""))
        {
            if (ImGui.Button("添加方案") && !string.IsNullOrWhiteSpace(newProfileName))
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

        ImGui.Text("采集方案");

        bool canDelete = C.GatherSettings.Count > 1 && C.SelectedGatherIndex != 0;
        using (ImRaii.Disabled(!canDelete))
        {
            if (ImGui.Button("删除所选方案"))
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

        ImGui.Combo("任务类型", ref MissionIndex, MissionTypes, MissionTypes.Length);
        if (ImGui.Button("应用至任务类型"))
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

        // Pathfinding
        int pathfinding = entry.Pathfinding;
        string[] modes = ["简单", "就近", "巡回"];
        ImGui.SetNextItemWidth(100);
        if (ImGui.Combo("寻路模式", ref pathfinding, modes, modes.Length))
        {
            entry.Pathfinding = pathfinding;
            C.Save();
        }
        ImGuiEx.HelpMarker("简单 - 从第一个列表上第一个节点到最后一个节点\n就近 - 总是去最近的节点并在剩余所有节点当中计算出一条最短路径\n巡回 - 找到相近的一群节点，并只在这些节点中巡回");
        if (pathfinding == 2)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            int cycle = entry.TSPCycleSize;
            if (ImGui.InputInt("巡回范围", ref cycle, 1))
            {
                entry.TSPCycleSize = cycle >= 2 ? cycle : 2;
                C.Save();
            }
        }

        // GP Settings
        int minGP = entry.MinimumGP;
        ImGui.SetNextItemWidth(100);
        if (ImGui.SliderInt("开始任务时最少GP", ref minGP, -1, maxGp))
        {
            entry.MinimumGP = minGP;
            C.Save();
        }

        // Multiply gathered items on FIRST gather loop only. Should only be used for Dual Class really.
        int gatherMult = entry.InitialGatheringItemMultiplier;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("双职业任务生产所需原料数", ref gatherMult, 1))
        {
            entry.InitialGatheringItemMultiplier = gatherMult >= 1 ? gatherMult : 1;
            C.Save();
        }
        ImGuiEx.HelpMarker("这会增加你切换到生产之前你采集的物品数\n把这个功能设置到你能达到目标分数所需原材料数\n这个功能只会影响双职业任务");

        // Boon Increase 2 (+30% Increase)
        DrawBuffSetting(
            label: "沃土的馈赠II / 富矿的馈赠II",
            uniqueId: $"Boon2Inc{entry.Id}",
            currentEnabled: entry.Buffs.BoonIncrease2,
            currentMinGp: entry.Buffs.BoonIncrease2Gp,
            minGpLimit: 100,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "额外采集奖励发生率提升30%",
            onEnabledChange: newVal =>
            {
                entry.Buffs.BoonIncrease2 = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BoonIncrease2Gp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BoonIncrease2MaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BoonIncrease2MaxUse = newVal;
                C.Save();
            }
        );

        // Boon Increase 1 (+10% Increase)
        DrawBuffSetting(
            label: "沃土的馈赠I / 富矿的馈赠I",
            uniqueId: $"Boon1Inc{entry.Id}",
            currentEnabled: entry.Buffs.BoonIncrease1,
            currentMinGp: entry.Buffs.BoonIncrease1Gp,
            minGpLimit: 50,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "额外采集奖励发生率提升10%",
            onEnabledChange: newVal =>
            {
                entry.Buffs.BoonIncrease1 = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BoonIncrease1Gp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BoonIncrease1MaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BoonIncrease1MaxUse = newVal;
                C.Save();
            }
        );

        // Tidings (+2 to boon instead of +1)
        DrawBuffSetting(
            label: "诺菲卡福音 / 纳尔札尔福音",
            uniqueId: $"TidingsBuff{entry.Id}",
            currentEnabled: entry.Buffs.TidingsBool,
            currentMinGp: entry.Buffs.TidingsGp,
            minGpLimit: 200,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "额外采集奖励发生时的获得数增加1个",
            onEnabledChange: newVal =>
            {
                entry.Buffs.TidingsBool = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.TidingsGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.TidingsMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.TidingsMaxUse = newVal;
                C.Save();
            }
        );

        // Yield II (+2 to all items on node)
        DrawBuffSetting(
            label: "天赐收成II / 莫非王土II",
            uniqueId: $"Blessed/KingsYieldIIBuff{entry.Id}",
            currentEnabled: entry.Buffs.YieldII,
            currentMinGp: entry.Buffs.YieldIIGp,
            minGpLimit: 500,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "令获得数增加2个\n" +
                        "只会在采集点耐久度为满时使用",
            onEnabledChange: newVal =>
            {
                entry.Buffs.YieldII = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.YieldIIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.YieldIIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.YieldIIMaxUse = newVal;
                C.Save();
            }
        );

        // Yield I (+1 to all items on node)
        DrawBuffSetting(
            label: "天赐收成I / 莫非王土I",
            uniqueId: $"Blessed/KingsYieldIBuff{entry.Id}",
            currentEnabled: entry.Buffs.YieldI,
            currentMinGp: entry.Buffs.YieldIGp,
            minGpLimit: 400,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "令获得数增加1个\n" +
                        "只会在采集点耐久度为满时使用",
            onEnabledChange: newVal =>
            {
                entry.Buffs.YieldI = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.YieldIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.YieldIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.YieldIMaxUse = newVal;
                C.Save();
            }
        );

        // Bonus Integrity (+1 integrity)
        DrawBuffSetting(
            label: "农夫之智 / 石工之理",
            uniqueId: $"Incrase Intregity{entry.Id}",
            currentEnabled: entry.Buffs.BonusIntegrity,
            currentMinGp: entry.Buffs.BonusIntegrityGp,
            minGpLimit: 300,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "恢复1次采集次数\n" +
                        "50%几率附加理智同兴预备状态",
            onEnabledChange: newVal =>
            {
                entry.Buffs.BonusIntegrity = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BonusIntegrityGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BonusIntegrityMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BonusIntegrityMaxUse = newVal;
                C.Save();
            }
        );

        // Bountiful Yield/Harvest II (+Amount based on gathering)
        DrawCustomBuffSetting(
            label: "丰收II / 高产II",
            uniqueId: $"Bountiful Yield II {entry.Id}",
            currentEnabled: entry.Buffs.BountifulYieldII,
            currentMinGp: entry.Buffs.BountifulYieldIIGp,
            minGpLimit: 100,
            maxGpLimit: maxGp,
            entryName: entry.Name,
            ActionInfo: "令下一次采集的获得数增加 \n" +
                        "获得力影响获得数的增加量（最小1～最大3）",
            onEnabledChange: newVal =>
            {
                entry.Buffs.BountifulYieldII = newVal;
                C.Save();
            },
            onMinGpChange: newVal =>
            {
                entry.Buffs.BountifulYieldIIGp = newVal;
                C.Save();
            },
            currentMaxUse: entry.Buffs.BountifulYieldIIMaxUse,
            onMaxUseChange: newVal =>
            {
                entry.Buffs.BountifulYieldIIMaxUse = newVal;
                C.Save();
            },
            entry.Buffs.BountifulMinItem,
            onMinItemMaxUseChange: newVal =>
            {
                entry.Buffs.BountifulMinItem = newVal;
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
        if (ImGui.Checkbox("自动好运道", ref gambaEnabled))
        {
            C.GambaEnabled = gambaEnabled;
            C.Save();
        }
        ImGuiEx.HelpMarker("为了成功运行该功能，确保你已经在环行威处打开了宇宙好运道，然后点击开始。接下就就可以解放双手了。");
        if (gambaEnabled)
        {
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("好运道延迟", ref gambaDelay, 50, 2000))
            {
                C.GambaDelay = gambaDelay;
                C.Save();
            }
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("最少持有的月球信用点数", ref gambaCreditsMinimum, 0, 10000))
            {
                C.GambaCreditsMinimum = gambaCreditsMinimum;
                C.Save();
            }
        }
        if (ImGui.Checkbox("选择更小的轮盘", ref gambaPreferSmallerWheel))
        {
            C.GambaPreferSmallerWheel = gambaPreferSmallerWheel;
            C.Save();
        }
        ImGuiEx.HelpMarker("这将会让该功能倾向于选择物品更少的轮盘");
        ImGui.Separator();
        ImGui.TextUnformatted("为每个物品配置权重。更高的权重=你更想要的东西");
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
        if (ImGui.Button("重置权重"))
        {
            TaskGamba.EnsureGambaWeightsInitialized(true);
        }
    }

    private bool showOverlay = C.ShowOverlay;
    private bool ShowSeconds = C.ShowSeconds;

    private void Overlay()
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

    private bool EnableAutoSprint = C.EnableAutoSprint;

    private void Misc()
    {
        if (ImGui.Checkbox("自动冲刺", ref EnableAutoSprint))
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
