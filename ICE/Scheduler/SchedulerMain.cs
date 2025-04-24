using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;

#nullable disable
namespace ICE.Scheduler
{
    internal static unsafe class SchedulerMain
    {
        internal static bool AreWeTicking;
        internal static bool EnableTicking
        {
            get => AreWeTicking;
            private set => AreWeTicking = value;
        }
        internal static bool EnablePlugin()
        {
            EnableTicking = true;

            return true;
        }
        internal static bool DisablePlugin()
        {
            EnableTicking = false;

            P.navmesh.Stop();
            P.taskManager.Abort();

            NavDestination = Vector3.Zero;

            return true;
        }


        internal static void Tick()
        {
            if (AreWeTicking)
            {
                if (GenericThrottle)
                {
                    if (!P.taskManager.IsBusy)
                    {

                    }
                }
            }
        }
    }
}
