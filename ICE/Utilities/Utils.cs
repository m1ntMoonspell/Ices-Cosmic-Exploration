using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Textures;
using ECommons;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices.Legacy;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System.Collections.Generic;

namespace ICE.Utilities;

public static unsafe class Utils
{
    #region Plugin/Ecoms stuff

    public static bool HasPlugin(string name) => DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);
    internal static bool GenericThrottle => FrameThrottler.Throttle("AutoRetainerGenericThrottle", 10);
    public static TaskManagerConfiguration DConfig => new(timeLimitMS: 10 * 60 * 3000, abortOnTimeout: false);

    public static void PluginVerbos(string message) => PluginLog.Verbose(message);
    public static void PluginInfo(string message) => PluginLog.Information(message);
    public static void PluginDebug(string message) => PluginLog.Debug(message);

    #endregion

    #region Player Information

    public static uint GetClassJobId() => Svc.ClientState.LocalPlayer!.ClassJob.RowId;
    public static unsafe int GetLevel(int expArrayIndex = -1)
    {
        if (expArrayIndex == -1) expArrayIndex = Svc.ClientState.LocalPlayer?.ClassJob.Value.ExpArrayIndex ?? 0;
        return UIState.Instance()->PlayerState.ClassJobLevels[expArrayIndex];
    }
    internal static unsafe short GetCurrentLevelFromSheet(Job? job = null)
    {
        PlayerState* playerState = PlayerState.Instance();
        return playerState->ClassJobLevels[Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault((uint)(job ?? (Player.Available ? Player.Object.GetJob() : 0)))?.ExpArrayIndex ?? 0];
    }

    public static bool IsInZone(uint zoneID) => Svc.ClientState.TerritoryType == zoneID;
    public static uint CurrentTerritory() => GameMain.Instance()->CurrentTerritoryTypeId;

    public static bool IsBetweenAreas => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51];

    public static bool PlayerNotBusy()
    {
        return Player.Available
               && Player.Object.CastActionId == 0
               && !IsOccupied()
               && !Player.IsJumping
               && Player.Object.IsTargetable
               && !Player.IsAnimationLocked;
    }

    public static unsafe bool HasStatusId(params uint[] statusIDs)
    {
        var statusID = Svc.ClientState.LocalPlayer!.StatusList
            .Select(se => se.StatusId)
            .ToList().Intersect(statusIDs)
            .FirstOrDefault();

        return statusID != default;
    }

    public static int GetGp()
    {
        var gp = Svc.ClientState.LocalPlayer?.CurrentGp ?? 0;
        return (int)gp;
    }

    internal static unsafe float GetDistanceToPlayer(Vector3 v3) => Vector3.Distance(v3, Player.GameObject->Position);
    internal static unsafe float GetDistanceToPlayer(IGameObject gameObject) => GetDistanceToPlayer(gameObject.Position);

    public static unsafe int GetItemCount(int itemID, bool includeHq = true)
    => includeHq ? InventoryManager.Instance()->GetInventoryItemCount((uint)itemID, true)
    + InventoryManager.Instance()->GetInventoryItemCount((uint)itemID) + InventoryManager.Instance()->GetInventoryItemCount((uint)itemID + 500_000)
    : InventoryManager.Instance()->GetInventoryItemCount((uint)itemID) + InventoryManager.Instance()->GetInventoryItemCount((uint)itemID + 500_000);

    public static Vector3 NavDestination = Vector3.Zero;

    #endregion

    #region Target Information

    internal static bool? TargetgameObject(IGameObject? gameObject)
    {
        var x = gameObject;
        if (Svc.Targets.Target != null && Svc.Targets.Target.DataId == x.DataId)
            return true;

        if (!IsOccupied())
        {
            if (x != null)
            {
                if (EzThrottler.Throttle($"Throttle Targeting {x.DataId}"))
                {
                    Svc.Targets.SetTarget(x);
                    ECommons.Logging.PluginLog.Information($"Setting the target to {x.DataId}");
                }
            }
        }
        return false;
    }
    internal static bool TryGetObjectByDataId(ulong dataId, out IGameObject? gameObject) => (gameObject = Svc.Objects.OrderBy(GetDistanceToPlayer).FirstOrDefault(x => x.DataId == dataId)) != null;
    internal static unsafe void InteractWithObject(IGameObject? gameObject)
    {
        try
        {
            if (gameObject == null || !gameObject.IsTargetable)
                return;
            var gameObjectPointer = (GameObject*)gameObject.Address;
            TargetSystem.Instance()->InteractWithObject(gameObjectPointer, false);
        }
        catch (Exception ex)
        {
            Svc.Log.Info($"InteractWithObject: Exception: {ex}");
        }
    }

    #endregion

    #region Addon Information

    public static bool IsAddonActive(string AddonName) // Used to see if the addon is active/ready to be fired on
    {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(AddonName);
        return addon != null && addon->IsVisible && addon->IsReady;
    }

    public static unsafe bool IsNodeVisible(string addonName, params int[] ids)
    {
        var ptr = Svc.GameGui.GetAddonByName(addonName, 1);
        if (ptr == nint.Zero)
            return false;

        var addon = (AtkUnitBase*)ptr;
        var node = GetNodeByIDChain(addon->GetRootNode(), ids);
        return node != null && node->IsVisible();
    }

    public static unsafe string GetNodeText(string addonName, params int[] nodeNumbers)
    {

        var ptr = Svc.GameGui.GetAddonByName(addonName, 1);

        var addon = (AtkUnitBase*)ptr;
        var uld = addon->UldManager;

        AtkResNode* node = null;
        var debugString = string.Empty;
        for (var i = 0; i < nodeNumbers.Length; i++)
        {
            var nodeNumber = nodeNumbers[i];

            var count = uld.NodeListCount;

            node = uld.NodeList[nodeNumber];
            debugString += $"[{nodeNumber}]";

            // More nodes to traverse
            if (i < nodeNumbers.Length - 1)
            {
                uld = ((AtkComponentNode*)node)->Component->UldManager;
            }
        }

        if (node->Type == NodeType.Counter)
            return ((AtkCounterNode*)node)->NodeText.ToString();

        var textNode = (AtkTextNode*)node;
        return textNode->NodeText.GetText();
    }

    private static unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params int[] ids)
    {
        if (node == null || ids.Length <= 0)
            return null;

        if (node->NodeId == ids[0])
        {
            if (ids.Length == 1)
                return node;

            var newList = new List<int>(ids);
            newList.RemoveAt(0);

            var childNode = node->ChildNode;
            if (childNode != null)
                return GetNodeByIDChain(childNode, [.. newList]);

            if ((int)node->Type >= 1000)
            {
                var componentNode = node->GetAsAtkComponentNode();
                var component = componentNode->Component;
                var uldManager = component->UldManager;
                childNode = uldManager.NodeList[0];
                return childNode == null ? null : GetNodeByIDChain(childNode, [.. newList]);
            }

            return null;
        }

        //check siblings
        var sibNode = node->PrevSiblingNode;
        return sibNode != null ? GetNodeByIDChain(sibNode, ids) : null;
    }

    #endregion

    #region LoadOnBoot

    public static void DictionaryCreation()
    {
        var MoonMissionSheet = Svc.Data.GetExcelSheet<WKSMissionUnit>();
        var MoonRecipeSheet = Svc.Data.GetExcelSheet<WKSMissionRecipe>();
        var RecipeSheet = Svc.Data.GetExcelSheet<Recipe>();
        var ItemSheet = Svc.Data.GetExcelSheet<Item>();
        var ExpSheet = Svc.Data.GetExcelSheet<WKSMissionReward>();

        foreach (var item in MoonMissionSheet)
        {
            List<(int Type, int Amount)> Exp = new List<(int Type, int Amount)>();
            uint keyId = item.RowId;
            string LeveName = item.Unknown0.ToString();
            LeveName = LeveName.Replace("<nbsp>", " ");
            LeveName = LeveName.Replace("<->", "");

            if (LeveName == "")
                continue;

            uint JobId = item.Unknown1;
            uint Job2 = item.Unknown2;

            if (JobId == 8)
                CRPMissions.Add(LeveName);
            else if (JobId == 9)
                BSMMissions.Add(LeveName);
            else if (JobId == 10)
                ARMMissions.Add(LeveName);
            else if (JobId == 11)
                GSMMissions.Add(LeveName);
            else if (JobId == 12)
                LTWMissions.Add(LeveName);
            else if (JobId == 13)
                WVRMissions.Add(LeveName);
            else if (JobId == 14)
                ALCMissions.Add(LeveName);
            else if (JobId == 15)
                CULMissions.Add(LeveName);

            uint silver = item.Unknown5;
            uint gold = item.Unknown6;

            uint rank = item.Unknown17;
            uint RecipeId = item.Unknown12;

            // Col 3 -> Cosmocredits - Unknown 0
            // Col 4 -> Lunar Credits - Unknown 1
            // Col 7 ->  Lv. 1 Type - Unknown 12
            // Col 8 ->  Lv. 1 Exp - Unknown 2
            // Col 10 -> Lv. 2 Type - Unknown 13
            // Col 11 -> Lv. 2 Exp - Unknown 3
            // Col 13 -> Lv. 3 Type - Unknown 14
            // Col 14 -> Lv. 3 Exp - Unknown 4

            uint Cosmo = ExpSheet.GetRow(keyId).Unknown0;
            uint Lunar = ExpSheet.GetRow(keyId).Unknown1;

            if (ExpSheet.GetRow(keyId).Unknown2 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown12, ExpSheet.GetRow(keyId).Unknown2));
            }
            if (ExpSheet.GetRow(keyId).Unknown3 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown13, ExpSheet.GetRow(keyId).Unknown3));
            }
            if (ExpSheet.GetRow(keyId).Unknown4 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown14, ExpSheet.GetRow(keyId).Unknown4));
            }

            if (!MissionInfoDict.ContainsKey(keyId))
            {
                MissionInfoDict[keyId] = new MissionListInfo()
                {
                    Name = LeveName,
                    JobId = JobId - 1,
                    JobId2 = Job2 - 1,
                    Rank = rank,
                    RecipeId = RecipeId,
                    SilverRequirement = silver,
                    GoldRequirement = gold,
                    CosmoCredit = Cosmo,
                    LunarCredit = Lunar,
                    ExperienceRewards = Exp
                };
            }
        }



        foreach (var item in MoonRecipeSheet)
        {
            // Moon Recipe Sheet -> Check to see if row 0-4 has recipies
            // The last entry in this that isn't 0 is the main item
            // Maybe store each recipeID as a list? And work backwards...
            List<uint> MissionRecipies = new List<uint>();
            Dictionary<uint, int> RequiredCrafts = new Dictionary<uint, int>();
            RequiredCrafts.Clear();
            uint MainRecipe = 0;

            if (item.Unknown0 == 0)
                continue;

            if (!MissionRecipies.Contains(item.Unknown0) && item.Unknown0 != 0)
            {
                MainRecipe = item.Unknown0;
            }
            if (!MissionRecipies.Contains(item.Unknown1) && item.Unknown1 != 0)
            {
                MainRecipe = item.Unknown1;
            }
            if (!MissionRecipies.Contains(item.Unknown2) && item.Unknown2 != 0)
            {
                MainRecipe = item.Unknown2;
            }
            if (!MissionRecipies.Contains(item.Unknown3) && item.Unknown3 != 0)
            {
                MainRecipe = item.Unknown3;
            }
            if (!MissionRecipies.Contains(item.Unknown4) && item.Unknown4 != 0)
            {
                MainRecipe = item.Unknown4;
            }

            MissionRecipies.Add(MainRecipe);

            if (MissionRecipies.Count != 0)
            {
                foreach (var entry in MissionRecipies)
                {
                    var recipieRow = RecipeSheet.GetRow(entry);

                    var itemResult = recipieRow.ItemResult.Value.RowId;

                    for (int i = 0; i < 8; i++)
                    {
                        uint recipieIngrediant = recipieRow.Ingredient[i].Value.RowId;
                        int amountNeeded = recipieRow.AmountIngredient[i];

                        if (recipieIngrediant == 0)
                            break;

                        if (!RequiredCrafts.ContainsKey(recipieIngrediant))
                        {
                            RequiredCrafts.Add(recipieIngrediant, amountNeeded);
                        }
                    }

                    if (!MoonRecipies.ContainsKey(item.RowId))
                    {
                        MoonRecipies.Add(item.RowId, new MoonRecipieInfo
                        {
                            MainItem = itemResult,
                            RecipieItems = RequiredCrafts
                        });
                    }
                }
            }
        }
    }

    #endregion

    #region Useful Functions

    public static Vector3 RoundVector3(Vector3 v, int decimals)
    {
        return new Vector3(
            (float)Math.Round(v.X, decimals),
            (float)Math.Round(v.Y, decimals),
            (float)Math.Round(v.Z, decimals)
        );
    }


    #endregion

}
