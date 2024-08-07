﻿using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    public class GenericKeenTargeting
    {
        public MyEntity GetTarget(IMyCubeGrid grid)
        {
            return GetTarget(grid, true, true, true, true, true, true, true);
        }

        public MyEntity GetTarget(IMyCubeGrid grid, bool targetGrids, bool targetLargeGrids, bool targetSmallGrids,
                                  bool targetFriendlies, bool targetNeutrals, bool targetEnemies, bool targetUnowned)
        {
            if (grid == null)
            {
                //MyAPIGateway.Utilities.ShowNotification("No grid found", 1000 / 60, MyFontEnum.Red);
                return null;
            }

            var myCubeGrid = grid as MyCubeGrid;
            if (myCubeGrid != null)
            {
                MyShipController activeController = null;

                foreach (var block in myCubeGrid.GetFatBlocks<MyShipController>())
                {
                    if (block.NeedsPerFrameUpdate)
                    {
                        activeController = block;
                        break;
                    }
                }

                if (activeController != null && activeController.Pilot != null)
                {
                    var targetLockingComponent = activeController.Pilot.Components.Get<MyTargetLockingComponent>();
                    if (targetLockingComponent != null && targetLockingComponent.IsTargetLocked)
                    {
                        var targetEntity = targetLockingComponent.TargetEntity;
                        if (targetEntity != null && targetGrids)
                        {
                            bool isLargeGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == MyCubeSize.Large;
                            bool isSmallGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == MyCubeSize.Small;

                            if (isLargeGrid && targetLargeGrids || isSmallGrid && targetSmallGrids)
                            {
                                // Pass the grid owner parameter when calling the filtering method
                                var filteredTarget = FilterTargetBasedOnFactionRelation(targetEntity, targetFriendlies, targetNeutrals, targetEnemies, targetUnowned);

                                if (filteredTarget != null)
                                {
                                    //MyAPIGateway.Utilities.ShowNotification("Target selected: " + filteredTarget.DisplayName, 1000 / 60, MyFontEnum.Blue);
                                }
                                else
                                {
                                    //MyAPIGateway.Utilities.ShowNotification("Target filtered out based on faction relationship", 1000 / 60, MyFontEnum.Red);
                                }

                                return filteredTarget;
                            }
                        }
                    }
                }
            }

            //MyAPIGateway.Utilities.ShowNotification("No valid target found", 1000 / 60, MyFontEnum.Red);
            return null;
        }

        private MyEntity FilterTargetBasedOnFactionRelation(MyEntity targetEntity, bool targetFriendlies, bool targetNeutrals, bool targetEnemies, bool targetUnowned)
        {
            IMyCubeGrid grid = targetEntity as IMyCubeGrid;
            if (grid != null)
            {
                MyRelationsBetweenPlayerAndBlock relation = GetRelationsToGrid(grid);
                bool isFriendly = relation == MyRelationsBetweenPlayerAndBlock.Friends;
                bool isNeutral = relation == MyRelationsBetweenPlayerAndBlock.Neutral;
                bool isEnemy = relation == MyRelationsBetweenPlayerAndBlock.Enemies;
                bool isOwner = relation == MyRelationsBetweenPlayerAndBlock.Owner;
                bool isFactionShare = relation == MyRelationsBetweenPlayerAndBlock.FactionShare;
                bool isNoOwnership = relation == MyRelationsBetweenPlayerAndBlock.NoOwnership;

                // Get reputation if the grid is owned
                int reputation = 0;
                if (grid.BigOwners.Count > 0)
                {
                    long gridOwner = grid.BigOwners[0];
                    IMyFaction ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(gridOwner);
                    if (ownerFaction != null)
                    {
                        reputation = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(MyAPIGateway.Session.Player.IdentityId, ownerFaction.FactionId);
                    }
                }

                // Special condition: Treat enemies with reputation above -500 as neutrals
                if (isEnemy && reputation > -500)
                {
                    isNeutral = true;
                    isEnemy = false;
                }

                // Display the faction relationship and reputation as a debug message
                //MyAPIGateway.Utilities.ShowNotification($"Faction Relation: {relation}, Reputation: {reputation}", 1000 / 60, MyFontEnum.White);

                if ((isFriendly || isFactionShare) && targetFriendlies) // Consider same faction and faction share as friendly
                {
                    return targetEntity;
                }
                else if (isNeutral && targetNeutrals)
                {
                    return targetEntity;
                }
                else if (isEnemy && targetEnemies)
                {
                    return targetEntity;
                }
                else if (isOwner && targetFriendlies) // Consider owner as friendly as well
                {
                    return targetEntity;
                }
                else if (isNoOwnership && targetUnowned)
                {
                    return targetEntity;
                }
            }

            return null;
        }


        private MyRelationsBetweenPlayerAndBlock GetRelationsToGrid(IMyCubeGrid grid)
        {
            if (grid.BigOwners == null || grid.BigOwners.Count == 0)
                return MyRelationsBetweenPlayerAndBlock.NoOwnership; // Unowned grid

            long gridOwner = grid.BigOwners[0];

            IMyFaction ownerFaction = gridOwner != 0 ? MyAPIGateway.Session.Factions.TryGetPlayerFaction(gridOwner) : null;
            IMyFaction playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(MyAPIGateway.Session.Player.IdentityId);

            // Check if player is not in any faction
            if (playerFaction == null)
            {
                // If player is not in a faction, determine the relation based on grid ownership
                return gridOwner == MyAPIGateway.Session.Player.IdentityId ? MyRelationsBetweenPlayerAndBlock.Owner : MyRelationsBetweenPlayerAndBlock.Enemies; // Example logic
            }

            if (ownerFaction != null && playerFaction != null)
            {
                if (ownerFaction.FactionId == playerFaction.FactionId)
                    return MyRelationsBetweenPlayerAndBlock.Friends;

                int reputation = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(MyAPIGateway.Session.Player.IdentityId, ownerFaction.FactionId);
                if (reputation > -500)
                    return MyRelationsBetweenPlayerAndBlock.Neutral;

                if (ownerFaction.IsNeutral(playerFaction.FactionId))
                    return MyRelationsBetweenPlayerAndBlock.Neutral;
                else
                    return MyRelationsBetweenPlayerAndBlock.Enemies;
            }

            return MyRelationsBetweenPlayerAndBlock.NoOwnership; // Treat as unowned if the owner has no faction
        }

        public bool IsTargetLocked(IMyCubeGrid grid)
        {
            if (grid == null)
                return false;

            var myCubeGrid = grid as MyCubeGrid;
            if (myCubeGrid != null)
            {
                MyShipController activeController = null;

                foreach (var block in myCubeGrid.GetFatBlocks<MyShipController>())
                {
                    if (block.NeedsPerFrameUpdate)
                    {
                        activeController = block;
                        break;
                    }
                }

                if (activeController != null && activeController.Pilot != null)
                {
                    var targetLockingComponent = activeController.Pilot.Components.Get<MyTargetLockingComponent>();
                    if (targetLockingComponent != null)
                    {
                        return targetLockingComponent.IsTargetLocked;
                    }
                }
            }
            return false;
        }


    }
}
