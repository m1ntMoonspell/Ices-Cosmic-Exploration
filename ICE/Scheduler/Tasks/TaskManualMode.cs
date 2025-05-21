namespace ICE.Scheduler.Tasks
{
    internal class TaskManualMode
    {
        public static void ZenMode()
        {
            if (CosmicHelper.CurrentLunarMission == 0)
            {
                SchedulerMain.State = IceState.GrabMission;
            }
            if (!C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission).ManualMode && C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission).JobId <= 15)
            {
                SchedulerMain.State &= ~IceState.ManualMode;
            }
        }
    }
}
