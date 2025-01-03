using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Orrery.HeartModule.Shared.Utility
{
    public static class RelationUtils
    {
        public static MyRelationsBetweenPlayers GetRelationsBetweenPlayers(long playerIdentity1, long playeIdentity2) // From Digi in the KSH Discord
        {
            if (playerIdentity1 == playeIdentity2)
                return MyRelationsBetweenPlayers.Self;

            var faction1 = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerIdentity1);
            var faction2 = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playeIdentity2);

            if (faction1 == null || faction2 == null)
                return MyRelationsBetweenPlayers.Enemies;

            if (faction1 == faction2)
                return MyRelationsBetweenPlayers.Allies;

            if (MyAPIGateway.Session.Factions.GetRelationBetweenFactions(faction1.FactionId, faction2.FactionId) == MyRelationsBetweenFactions.Neutral)
                return MyRelationsBetweenPlayers.Neutral;

            return MyRelationsBetweenPlayers.Enemies;
        }

        public static MyRelationsBetweenPlayerAndBlock MapToBlockRelations(this MyRelationsBetweenPlayers relations)
        {
            switch (relations)
            {
                case MyRelationsBetweenPlayers.Self:
                    return MyRelationsBetweenPlayerAndBlock.Owner;
                case MyRelationsBetweenPlayers.Neutral:
                    return MyRelationsBetweenPlayerAndBlock.Neutral;
                case MyRelationsBetweenPlayers.Allies:
                    return MyRelationsBetweenPlayerAndBlock.Friends;
                case MyRelationsBetweenPlayers.Enemies:
                    return MyRelationsBetweenPlayerAndBlock.Enemies;
            }
            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        }

        public static MyRelationsBetweenPlayerAndBlock GetRelationsBetweeenGrids(IMyCubeGrid ownGrid, IMyCubeGrid targetGrid)
        {
            if (targetGrid == null || ownGrid == null)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership;

            if (targetGrid.BigOwners.Count == 0 || ownGrid.BigOwners.Count == 0)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership;
            long targetOwner = targetGrid.BigOwners[0];
            long owner = ownGrid.BigOwners[0];

            return GetRelationsBetweenPlayers(owner, targetOwner).MapToBlockRelations();
        }

        public static MyRelationsBetweenPlayerAndBlock GetRelationsBetweenGridAndPlayer(IMyCubeGrid ownGrid, long? targetIdentity)
        {
            if (targetIdentity == null)
                return MyRelationsBetweenPlayerAndBlock.Enemies;
            if (ownGrid == null || ownGrid.BigOwners.Count == 0)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership;
            long owner = ownGrid.BigOwners[0];

            return GetRelationsBetweenPlayers(owner, targetIdentity.Value).MapToBlockRelations();
        }
    }
}
