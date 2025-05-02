using FFXIVClientStructs.FFXIV.Client.Game.WKS;

namespace ICE.Scheduler.Tasks
{
    internal class TaskManualMode
    {
        public static unsafe uint CurrentLunarMission => WKSManager.Instance()->CurrentMissionUnitRowId;
        public static void ZenMode()
        {
            if (CurrentLunarMission == 0)
            {
                SchedulerMain.State = IceState.GrabMission;
            }
        }
    }
}
