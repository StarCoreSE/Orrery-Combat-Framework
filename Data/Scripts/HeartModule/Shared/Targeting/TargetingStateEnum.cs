using System;

namespace Orrery.HeartModule.Shared.Targeting
{
    [Flags]
    internal enum TargetingStateEnum
    {
        Grids = 1,
        LargeGrids = 2,
        SmallGrids = 4,
        Projectiles = 8,
        Characters = 16,
        Friendlies = 32,
        Neutrals = 64,
        Enemies = 128,
        Unowned = 256,
        Unique = 512,
    }
}
