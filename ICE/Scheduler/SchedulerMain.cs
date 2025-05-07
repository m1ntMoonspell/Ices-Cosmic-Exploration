using ICE.Scheduler.Tasks;

namespace ICE.Scheduler
{
    internal static unsafe class SchedulerMain
    {
        internal static bool EnablePlugin()
        {
            State = IceState.ResumeChecker;
            return true;
        }
        internal static bool DisablePlugin()
        {
            P.TaskManager.Abort();
            StopBeforeGrab = false;
            State = IceState.Idle;
            return true;
        }

        internal static string MissionName = string.Empty;
        internal static bool inMission = false;
        internal static bool Abandon = false;
        internal static bool StopBeforeGrab = false;
        #if DEBUG
        // Debug only settings
        internal static bool DebugOOMMain = false;
        internal static bool DebugOOMSub = false;
        #endif

        internal static IceState State = IceState.Idle;

        internal static void Tick()
        {
            if (Throttles.GenericThrottle && P.TaskManager.Tasks.Count == 0)
            {
                switch (State)
                {
                    case IceState.Idle:
                        break;
                    case IceState.AnimationLock:
                        TaskAnimationLock.Enqueue();
                        break;
                    case IceState.WaitForCrafts:
                    case IceState.CraftInProcess:
                    case IceState.GrabbingMission:
                        break;
                    case IceState.WaitForNonStandard:
                        TaskMissionFind.WaitForNonStandard();
                        break;
                    case IceState.GrabMission:
                        TaskMissionFind.Enqueue();
                        break;
                    case IceState.StartCraft:
                        TaskCrafting.TryEnqueueCrafts();
                        break;
                    case IceState.AbortInProgress:
                    case IceState.CheckScoreAndTurnIn:
                        TaskScoreCheck.TryCheckScore();
                        break;
                    case IceState.ManualMode:
                        TaskManualMode.ZenMode();
                        break;
                    case IceState.ResumeChecker:
                        TaskMissionFind.EnqueueResumeCheck();
                        break;
                    default:
                        throw new Exception("Invalid state");
                }
            }
        }
    }

    internal enum IceState
    {
        Idle,
        AnimationLock,
        GrabMission,
        GrabbingMission,
        StartCraft,
        CraftInProcess,
        AbortInProgress,
        CheckScoreAndTurnIn,
        WaitForCrafts,
        ManualMode,
        ResumeChecker,
        WaitForNonStandard
    }
}
