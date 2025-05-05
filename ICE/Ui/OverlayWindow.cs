using System.Globalization;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ICE.Scheduler;
using ICE.Scheduler.Handlers;
using ICE.Utilities;

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

            if (currentWeather != null)
            {
                ImGui.Text($"Weather: {currentWeather} -> {nextWeather} in [{nextWeatherTime}]");
            }

            (var currentTimedBonus, var nextTimedBonus) = PlayerHandlers.GetTimedJob();
            if (currentTimedBonus.Value == null)
            {
                ImGui.Text($"Timed Mission(s): None -> {string.Join(", ", nextTimedBonus.Value)} [{nextTimedBonus.Key.start:D2}:00]");
            }
            else
            {
                ImGui.Text($"Timed Mission(s): {string.Join(", ", currentTimedBonus.Value)} -> {string.Join(", ", nextTimedBonus.Value)} [{nextTimedBonus.Key.start:D2}:00]");
            }

            (string type, var locations) = AnnouncementHandlers.CheckForRedAlert();
            if (type != null && locations != null)
            {
                ImGui.Text($"[Red Alert] {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(type)}");
                ImGui.Spacing();
                for (int i = 0; i < locations.Length; i++)
                {
                    if (locations.Length > 0)
                    {
                        ImGui.Text($"Variant [{i + 1}]");
                        ImGui.SameLine();
                    }

                    (string job, uint territoryId, float x, float y) = locations[i].first;
                    if (ImGui.Button($"{job}"))
                    {
                        SetFlagForNPC(territoryId, x, y);
                    }

                    ImGui.SameLine();

                    (job, territoryId, x, y) = locations[i].second;
                    if (ImGui.Button($"{job}"))
                    {
                        SetFlagForNPC(territoryId, x, y);
                    }
                    ImGui.Spacing();
                }
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
            //    //    Type = Dalamud.Game.Text.XivChatType.Debug,
            //    //});
            //}
        }
    }
}
