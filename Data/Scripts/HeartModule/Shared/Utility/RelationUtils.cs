using System.Linq;
using Orrery.HeartModule.Shared.Targeting;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;

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

        public static MyRelationsBetweenPlayerAndBlock GetRelationsBetweenGridAndEntity(IMyCubeGrid thisGrid, MyEntity entity)
        {
            // Entity type
            var grid = entity as IMyCubeGrid;
            var character = entity as IMyCharacter;
            if (grid != null)
            {
                if (grid.BigOwners.Count == 0)
                    return MyRelationsBetweenPlayerAndBlock.NoOwnership;
                return GetRelationsBetweeenGrids(thisGrid, grid);
            }
            else if (character != null)
            {
                var player = HeartData.I.Players.FirstOrDefault(p => p.Character == character);
                if (player == null) // I'm too lazy to let offline characters be fired on.
                    return MyRelationsBetweenPlayerAndBlock.NoOwnership;

                return GetRelationsBetweenGridAndPlayer(thisGrid, player.IdentityId);
            }

            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        }

        public static MyRelationsBetweenPlayerAndBlock GetRelationsBetweenBlockAndEntity(IMyCubeBlock thisBlock, MyEntity entity)
        {
            // Entity type
            var grid = entity as IMyCubeGrid;
            var character = entity as IMyCharacter;
            if (grid != null)
            {
                if (grid.BigOwners.Count == 0)
                    return MyRelationsBetweenPlayerAndBlock.NoOwnership;
                return thisBlock.GetUserRelationToOwner(grid.BigOwners[0]);
            }
            else if (character != null)
            {
                var player = HeartData.I.Players.FirstOrDefault(p => p.Character == character);
                if (player == null) // I'm too lazy to let offline characters be fired on.
                    return MyRelationsBetweenPlayerAndBlock.NoOwnership;

                return thisBlock.GetUserRelationToOwner(player.IdentityId);
            }

            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        }
    }
}
