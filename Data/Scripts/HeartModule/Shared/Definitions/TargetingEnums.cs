namespace Orrery.HeartModule.Shared.Definitions
{
    public enum IFFEnum
    {
        None = 0,
        TargetSelf = 1,
        TargetEnemies = 2,
        TargetFriendlies = 4,
        TargetNeutrals = 8,
        TargetUnique = 16,
    }

    public enum TargetTypeEnum
    {
        None = 0,
        TargetGrids = 1,
        TargetProjectiles = 2,
        TargetCharacters = 4,
        TargetUnique = 8,
    }
}
