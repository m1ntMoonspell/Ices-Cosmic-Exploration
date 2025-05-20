namespace ICE.Enums
{
    [Flags]
    internal enum IceState
    {
        Idle = 0,
        Start = 1,
        Craft = 2,
        Gather = 4,
        Fish = 8,
        ManualMode = 16,
        GrabMission = 32,
        ExecutingMission = 64,
        ScoringMission = 128,
        AbortInProgress = 256,
        AnimationLock = 512,
        Gambling = 1024,
        Waiting = 2048,
    }
}