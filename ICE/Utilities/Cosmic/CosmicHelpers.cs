using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Utilities;

public static partial class CosmicHelper
{
    public static MissionListInfo CurrentMissionInfo => MissionInfoDict[CurrentLunarMission];
    public static MoonRecipieInfo CurrentMoonRecipe => MoonRecipies[CurrentLunarMission];
    /// <summary>
    /// Gives the current mission that is active
    /// </summary>
    public static unsafe uint CurrentLunarMission => WKSManager.Instance()->CurrentMissionUnitRowId;
    public static unsafe uint CurrentLunarDevelopment => ExcelHelper.DevGrade.GetRow(WKSManager.Instance()->DevGrade).Unknown6;
    public static Dictionary<int, string> ExpDictionary = new()
    {
        { 1, "I" },
        { 2, "II" },
        { 3, "III" },
        { 4, "IV" }
    };


    public static void OpenStellarMission()
    {
        if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !AddonHelper.IsAddonActive("WKSMissionInfomation"))
        {
            if (EzThrottler.Throttle("Opening Stellar Missions"))
            {
                IceLogging.Debug("Opening Mission Menu");
                hud.Mission();
            }
        }
    }
}
