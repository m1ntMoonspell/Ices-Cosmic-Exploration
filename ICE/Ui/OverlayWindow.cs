using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ICE.Scheduler;
using ICE.Scheduler.Handlers;

namespace ICE.Ui
{
    internal class OverlayWindow : Window
    {
        public OverlayWindow() : base("ICE Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize)
        {
            P.windowSystem.AddWindow(this);
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        public override void Draw()
        {
            ImGui.Text($"Current state: " + SchedulerMain.State.ToString());

            ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(2);

            (string currentWeather, string nextWeather, string nextWeatherTime) = WeatherForecastHandler.GetNextWeather();

            ImGui.Text($"Weather: {currentWeather} -> {nextWeather} in [{nextWeatherTime}]");

            (var currentTimedBonus, var nextTimedBonus) = PlayerHandlers.GetTimedJob();
            if (currentTimedBonus.Value == null)
            {
                ImGui.Text($"Timed Mission(s): None -> {string.Join(", ", nextTimedBonus.Value)} [{nextTimedBonus.Key.start:D2}:00]");
            }
            else
            {
                ImGui.Text($"Timed Mission(s): {string.Join(", ", currentTimedBonus.Value)} -> {string.Join(", ", nextTimedBonus.Value)} [{nextTimedBonus.Key.start:D2}:00]");
            }

            ImGuiHelpers.ScaledDummy(2);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(2);

            if (ImGuiEx.IconButton("\uf013##Config", "Open ICE"))
            {
                P.mainWindow.IsOpen = true;
            }
            ImGui.SameLine();

            // Start button (disabled while already ticking).
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !UsingSupportedJob()))
            {
                if (ImGui.Button("Start"))
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            // Stop button (disabled while not ticking).
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("Stop"))
                {
                    SchedulerMain.DisablePlugin();
                }
            }
            ImGui.SameLine();
            ImGui.Checkbox("Stop after current mission", ref SchedulerMain.StopBeforeGrab);
        }
    }
}
