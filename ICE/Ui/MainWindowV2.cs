using Dalamud.Interface.Utility.Raii;
using ICE.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui
{
    internal class MainWindowV2 : Window
    {
        public MainWindowV2() :
#if DEBUG
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion} Debug build ###ICEMainWindow")
#else
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindow")
#endif
        {
            Flags = ImGuiWindowFlags.None;

            // Set up size constraints to ensure window cannot be too small or too large.
            // Increased minimum size to better accommodate larger font sizes.
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(100, 100),
                MaximumSize = new Vector2(2000, 2000)
            };

            P.windowSystem.AddWindow(this);

            AllowPinning = true;
            AllowClickthrough = true;
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        public override void Draw()
        {
            // Calculate scaling factors based on current font size
            float fontScale = ImGui.GetIO().FontGlobalScale;
            float textLineHeight = ImGui.GetTextLineHeight();
            float scaledSpacing = ImGui.GetStyle().ItemSpacing.Y * fontScale;
            float headerPadding = textLineHeight * 1.2f;

            float headerHeight = textLineHeight + headerPadding * 2;
            float contentAreaHeight = ImGui.GetWindowHeight() - headerHeight - 4;
            float labelHeight = ImGui.GetTextLineHeightWithSpacing();
            float childHeight = ImGui.GetContentRegionAvail().Y - labelHeight;

            // Setting up the columns to be 3 right here. 
            float leftPanelWidth = Math.Max(220, textLineHeight * 14);
            float middlePanelWidth = Math.Max(350, textLineHeight * 22);

            ImGui.Columns(3, "Main Window", true);
            // ----------------------------
            //  LEFT PANEL (Start/Stop, Class Selection, Filter Ui)
            // ----------------------------

            ImGui.SetColumnWidth(0, leftPanelWidth);

            ImGui.BeginChild("###Filter Panel", new Vector2(0, childHeight), true);

            LeftColumn();

            ImGui.EndChild();
        }

        // Available jobs and their IDs.
        public static List<(string Name, uint Id)> jobOptions = new()
        {
            ("CRP", 9),
            ("BSM", 10),
            ("ARM", 11),
            ("GSM", 12),
            ("LTW", 13),
            ("WVR", 14),
            ("ALC", 15),
            ("CUL", 16),
            ("MIN", 17),
            ("BTN", 18),
            ("FSH", 19),
        };

        // Available mission ranks and their identifiers.
        private static List<(uint RankId, string RankName)> rankOptions = new()
        {
            (1, "D"),
            (2, "C"),
            (3, "B"),
            (4, "A")
        };

        private static uint? currentJobId => PlayerHelper.GetClassJobId();
        private static bool usingSupportedJob => jobOptions.Any(job => job.Id == currentJobId + 1);

        private static void LeftColumn()
        {
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
            {
                if (ImGui.Button("Start"))
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("Stop"))
                {
                    SchedulerMain.DisablePlugin();
                }
            }

            ImGui.Checkbox("Stop after current mission", ref SchedulerMain.StopBeforeGrab);
        }
    }
}
