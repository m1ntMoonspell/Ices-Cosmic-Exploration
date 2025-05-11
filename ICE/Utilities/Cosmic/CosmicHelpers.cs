using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Utilities;

public static partial class CosmicHelper
{
    private static ExcelSheet<WKSDevGrade>? DevGrade;

    public static MissionListInfo CurrentMissionInfo => MissionInfoDict[CurrentLunarMission];
    public static MoonRecipieInfo CurrentMoonRecipe => MoonRecipies[CurrentLunarMission];
    /// <summary>
    /// Gives the current mission that is active
    /// </summary>
    public static unsafe uint CurrentLunarMission => WKSManager.Instance()->CurrentMissionUnitRowId;
    public static unsafe uint CurrentLunarDevelopment => DevGrade.GetRow(WKSManager.Instance()->DevGrade).Unknown6;
    public static Dictionary<int, string> ExpDictionary = new()
    {
        { 1, "I" },
        { 2, "II" },
        { 3, "III" },
        { 4, "IV" }
    };

    public static void Init()
    {
        DevGrade ??= Svc.Data.GetExcelSheet<WKSDevGrade>();
    }

    public static void OpenStellaMission()
    {
        if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !AddonHelper.IsAddonActive("WKSMissionInfomation"))
        {
            if (EzThrottler.Throttle("Opening Steller Missions"))
            {
                IceLogging.Debug("Opening Mission Menu");
                hud.Mission();
            }
        }
    }
}
