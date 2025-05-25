using FFXIVClientStructs.FFXIV.Client.Game.Character;
using InteropGenerator.Runtime.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ICE.Utilities.Cosmic;

[InlineArray(11)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FixedSizeArray11<T> where T : unmanaged
{
    private T _element0;
}

[Inherits<CharacterManagerInterface>]
[StructLayout(LayoutKind.Explicit, Size = 0xE20)]
public unsafe partial struct WKSManagerEx
{
    [FieldOffset(0xD34), FixedSizeArray] internal FixedSizeArray11<int> _scores;
}
