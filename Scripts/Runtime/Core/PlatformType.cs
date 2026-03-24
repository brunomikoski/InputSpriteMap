using System;

namespace BrunoMikoski.InputSpriteMap
{
    [Flags]
    public enum PlatformType
    {
        None = 0,
        MouseAndKeyboard = 1 << 0,
        Playstation = 1 << 1,
        Xbox = 1 << 2,
        Switch = 1 << 3,
        SteamDeck = 1 << 4,
        Gamepad = Playstation | Xbox | Switch | SteamDeck
    }
}
