using Dalamud.Game.ClientState.Objects.Types;

namespace ICE
{
    internal class Service
    {
        internal static Config Config { get; set; } = null!;
        internal static IGameObject gameObject { get; private set; } = null!;
        public static IObjectTable ObjectTable { get; private set; } = null!;
    }
}
