using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ECommons.EzIpcManager;
using ICE.Scheduler;
using ICE.Ui;
using ICE.IPC;
using ICE.Scheduler.Handlers;
using Lumina;

namespace ICE;

public sealed class ICE : IDalamudPlugin
{
    public string Name => "ICE";
    internal static ICE P = null!;
    public static Config C => P.config;
    private Config config;

    // Window's that I use, base window to the settings... need these to actually show shit 
    internal WindowSystem windowSystem;
    internal MainWindow mainWindow;
    internal SettingsWindow settingWindow;
    internal DebugWindow debugWindow;

    // Taskmanager from Ecommons
    internal TaskManager taskManager;

    // Internal IPC's that I use for... well plugins. 
    internal LifestreamIPC lifestream;
    internal NavmeshIPC navmesh;
    internal PandoraIPC pandora;
    internal ArtisanIPC artisan;

    public ICE(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, P, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);
        new ECommons.Schedulers.TickScheduler(Load);
    }

    public void Load()
    {
        EzConfig.Migrate<Config>();
        config = EzConfig.Init<Config>();

        //IPC's that are used
        taskManager = new();
        lifestream = new();
        navmesh = new();
        pandora = new();
        artisan = new();

        // all the windows
        windowSystem = new();
        mainWindow = new();
        settingWindow = new();
        debugWindow = new();

        taskManager = new(new(abortOnTimeout: true, timeLimitMS: 20000, showDebug: true));
        Svc.PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi += () =>
        {
            mainWindow.IsOpen = true;
        };
        Svc.PluginInterface.UiBuilder.OpenConfigUi += () =>
        {
            settingWindow.IsOpen = true;
        };
        EzCmd.Add("/IceCosmic", OnCommand, """
            Open plugin interface
            """);
        EzCmd.Add("/ice", OnCommand);
        Svc.Framework.Update += Tick;

        DictionaryCreation();
    }

    private void Tick(object _)
    {
        if (SchedulerMain.AreWeTicking && Svc.ClientState.LocalPlayer != null)
        {
            SchedulerMain.Tick();
        }
        GenericManager.Tick();
        TextAdvancedManager.Tick();
        YesAlreadyManager.Tick();
    }

    public void Dispose()
    {
        Safe(() => Svc.Framework.Update -= Tick);
        Safe(() => Svc.PluginInterface.UiBuilder.Draw -= windowSystem.Draw);
        
        // Cleanup IPC objects
        Safe(() => { lifestream = null; });
        Safe(() => { navmesh = null; });
        Safe(() => { pandora = null; });
        Safe(() => { artisan = null; });
        
        // Shutdown taskManager
        Safe(() => taskManager?.Abort());
        
        ECommonsMain.Dispose();
        Safe(TextAdvancedManager.UnlockTA);
        Safe(YesAlreadyManager.Unlock);
    }

    private void OnCommand(string command, string args)
    {
        var subcommands = args.Split(' ');

        if (subcommands.Length == 0 || args == "")
        {
            mainWindow.IsOpen = !mainWindow.IsOpen;
            return;
        }

        var firstArg = subcommands[0];

        if (firstArg.ToLower() == "d" || firstArg.ToLower() == "debug")
        {
            debugWindow.IsOpen = true;
            return;
        }
        else if (firstArg.ToLower() == "s" || firstArg.ToLower() == "settings")
        {
            settingWindow.IsOpen = true;
            return;
        }
        else if (firstArg.ToLower() == "clear")
        {
            C.EnabledMission.Clear();
        }
    }
}
