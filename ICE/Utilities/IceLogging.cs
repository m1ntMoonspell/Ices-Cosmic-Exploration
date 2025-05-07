namespace ICE.Utilities;

internal class IceLogging
{
    public static bool ShouldLog(string name = "LogThrottle", int delay = 2000) => EzThrottler.Throttle(name, delay);
    public static void Debug(string message, bool noThrottle = false, string name = "LogThrottle", int delay = 1500)
    {
#if DEBUG
        if (EzThrottler.Throttle(name, delay) || noThrottle)
#endif
            PluginLog.Debug(message);
    }

    public static void Info(string message, bool noThrottle = false, string name = "LogThrottle", int delay = 2000)
    {
#if DEBUG
        if (EzThrottler.Throttle(name, delay) || noThrottle)
#endif
            PluginLog.Information(message);
    }

    public static void Error(string message)
    {
        PluginLog.Error(message);
    }
}
