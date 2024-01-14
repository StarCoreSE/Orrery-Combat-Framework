﻿using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Game;
using Sandbox.Game;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    public class GenericKeenTargeting
    {
        public MyEntity GetTarget(IMyCubeGrid grid, bool targetGrids, bool targetLargeGrids, bool targetSmallGrids,
                                  bool targetFriendlies, bool targetNeutrals, bool targetEnemies, bool targetUnowned)
        {
            if (grid == null)
            {
                MyAPIGateway.Utilities.ShowNotification("No grid found", 1000 / 60, VRage.Game.MyFontEnum.Red);
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
                            bool isLargeGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == VRage.Game.MyCubeSize.Large;
                            bool isSmallGrid = targetEntity is IMyCubeGrid && ((IMyCubeGrid)targetEntity).GridSizeEnum == VRage.Game.MyCubeSize.Small;

                            if ((isLargeGrid && targetLargeGrids) || (isSmallGrid && targetSmallGrids))
                            {
                                // Pass the grid owner parameter when calling the filtering method
                                var filteredTarget = FilterTargetBasedOnFactionRelation(targetEntity, targetFriendlies, targetNeutrals, targetEnemies, targetUnowned);

                                if (filteredTarget != null)
                                {
                                    MyAPIGateway.Utilities.ShowNotification("Target selected: " + filteredTarget.DisplayName, 1000 / 60, VRage.Game.MyFontEnum.Blue);
                                }
                                else
                                {
                                    MyAPIGateway.Utilities.ShowNotification("Target filtered out based on faction relationship", 1000 / 60, VRage.Game.MyFontEnum.Red);
                                }

                                return filteredTarget;
                            }
                        }
                    }
                }
            }

            MyAPIGateway.Utilities.ShowNotification("No valid target found", 1000 / 60, VRage.Game.MyFontEnum.Red);
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

                // Check if the grid has no owners
                bool isUnowned = grid.BigOwners == null || (relation == MyRelationsBetweenPlayerAndBlock.NoOwnership || grid.BigOwners.Count == 0);

                // Display the faction relationship as a debug message
                MyAPIGateway.Utilities.ShowNotification($"Faction Relation: {relation}", 1000 / 60, VRage.Game.MyFontEnum.White);

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

            if (ownerFaction != null)
            {
                IMyFaction playerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(MyAPIGateway.Session.Player.IdentityId);

                if (playerFaction != null)
                {
                    if (ownerFaction.FactionId == playerFaction.FactionId)
                        return MyRelationsBetweenPlayerAndBlock.Friends;
                    else
                        return MyRelationsBetweenPlayerAndBlock.Enemies;
                }
            }

            return MyRelationsBetweenPlayerAndBlock.NoOwnership; // Treat as unowned if the owner has no faction
        }
    }
}
