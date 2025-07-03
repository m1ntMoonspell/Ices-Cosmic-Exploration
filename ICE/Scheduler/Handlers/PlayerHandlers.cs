using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;
using Time = (int start, int end);

namespace ICE.Scheduler.Handlers;

internal static unsafe class PlayerHandlers
{
    public static readonly Dictionary<Time, string[]> timeMap = new()
    {
            { (0, 1), new[] { "��ľ��", "������ʿ" } },
            { (2, 3), new[] { "�ɿ�" } },
            { (4, 5), new[] { "������", "���ʦ" } },
            { (6, 7), new[] { "������" } },
            { (8, 9), new[] { "���׽�" } },
            { (10, 11), new[] { "԰�չ�" } },
            { (12, 13), new[] { "���" } },
            { (16, 17), new[] { "�Ƹｳ" } },
            { (20, 21), new[] { "���½�" } }
        };
    public static readonly Dictionary<Time, string[]> stage9TimeMap = new()
    {
        { (0, 1), new[] { "��ľ��", "������ʿ", "���" } },
        { (2, 3), new[] { "�ɿ�" } },
        { (4, 5), new[] { "������", "԰�չ�", "�Ƹｳ" } },
        { (6, 7), new[] { "������" } },
        { (8, 9), new[] { "���׽�", "���½�",  } },
        { (10, 11), new[] { "԰�չ�" } },
        { (12, 13), new[] { "���", "��ľ��", "������ʿ" } },
        { (14, 15), new[] { "�ɿ�" } },
        { (16, 17), new[] { "�Ƹｳ", "������", "���ʦ" } },
        //{ (18, 19), new[] { "FSH" } },
        { (20, 21), new[] { "���½�", "���׽�" } },
        { (22, 23), new[] { "԰�չ�" } }
    };
    private static readonly uint stellarSprintID = 4398;

    public static float Distance(this Vector3 v, Vector3 v2)
    {
        return new Vector2(v.X - v2.X, v.Z - v2.Z).Length();
    }
    public static unsafe bool IsMoving()
    {
        return AgentMap.Instance()->IsPlayerMoving;
    }

    internal static void Tick()
    {
        P.overlayWindow.IsOpen = C.ShowOverlay && PlayerHelper.IsInCosmicZone() && PlayerHelper.UsingSupportedJob();

        if (C.EnableAutoSprint && PlayerHelper.IsInCosmicZone() && !PlayerHelper.HasStatusId(stellarSprintID) && Svc.Condition[ConditionFlag.NormalConditions] && IsMoving()) UseSprint();

        if ((!PlayerHelper.IsInCosmicZone() || !PlayerHelper.UsingSupportedJob()) && SchedulerMain.State != IceState.Idle)
        {
            DisablePlugin();
        }
    }

    internal static void DisablePlugin()
    {
        if (SchedulerMain.State != IceState.Idle)
        {
            P.TaskManager.Abort();
            SchedulerMain.DisablePlugin();
        }
    }

    private static void UseSprint()
    {
        var am = ActionManager.Instance();
        var isSprintReady = am->GetActionStatus(ActionType.GeneralAction, 4) == 0;

        if (isSprintReady) am->UseAction(ActionType.GeneralAction, 4);
    }

    private static (long, long) GetEorzeaTime()
    {
        var eorzeaTime = Framework.Instance()->ClientTime.EorzeaTime;
        long hours = eorzeaTime / 3600 % 24;
        long minutes = eorzeaTime / 60 % 60;
        return (hours, minutes);
    }

    internal static (KeyValuePair<(int start, int end), string[]>, KeyValuePair<(int start, int end), string[]>) GetTimedJob()
    {
        KeyValuePair<(int start, int end), string[]> currentTimeBonus = default;
        KeyValuePair<(int start, int end), string[]> nextTimeBonus = default;
        Dictionary<Time, string[]> currentTimeMap = timeMap;
        if (CosmicHelper.CurrentLunarDevelopment >= 9) currentTimeMap = stage9TimeMap;

        (long hours, _) = GetEorzeaTime();
        var currentTime = currentTimeMap.FirstOrDefault(time => hours >= time.Key.start && hours <= time.Key.end);
        if (!currentTime.Equals(default(KeyValuePair<(int, int), string[]>))) currentTimeBonus = currentTime;

        var nextTime = currentTimeMap.FirstOrDefault(time => hours < time.Key.start);
        if (!nextTime.Equals(default(KeyValuePair<(int, int), string[]>))) nextTimeBonus = nextTime;
        else nextTimeBonus = currentTimeMap.First();

        return (currentTimeBonus, nextTimeBonus);
    }
}
