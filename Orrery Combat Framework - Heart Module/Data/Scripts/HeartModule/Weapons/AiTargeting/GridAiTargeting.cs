using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting
{
    internal class GridAiTargeting
    {
        IMyCubeGrid Grid;
        List<SorterWeaponLogic> Weapons
        {
            get
            {
                List<SorterWeaponLogic> weapons;
                if (WeaponManager.I.GridWeapons.TryGetValue(Grid, out weapons))
                {
                    return weapons;
                }
                else
                {
                    HeartLog.Log($"Weapons list not found for grid '{Grid.DisplayName}'");
                    return new List<SorterWeaponLogic>();
                }
            }
        }
        Vector3D gridPosition => Grid.PositionComp.WorldAABB.Center;

        public SortedList<IMyCubeGrid, int> TargetedGrids = new SortedList<IMyCubeGrid, int>();
        public SortedList<IMyCharacter, int> TargetedCharacters = new SortedList<IMyCharacter, int>();
        public SortedList<uint, int> TargetedProjectiles = new SortedList<uint, int>();
        SortedList<MyEntity, int> PriorityTargets = new SortedList<MyEntity, int>();

        private GenericKeenTargeting keenTargeting = new GenericKeenTargeting();

        /// <summary>
        /// The main focused target 
        /// </summary>
        public IMyCubeGrid PrimaryGridTarget { get; private set; }

        public bool Enabled = false;
        float MaxTargetingRange = 1000;
        bool DoesTargetGrids = true;
        bool DoesTargetCharacters = true;
        bool DoesTargetProjectiles = true;

        public GridAiTargeting(IMyCubeGrid grid)
        {
            HeartLog.Log($"Initializing GridAiTargeting for grid '{grid.DisplayName}'");

            if (grid == null || grid.Physics == null)
            {
                HeartLog.Log($"GridAiTargeting: Grid is null or has no physics. Skipping initialization.");
                return;
            }

            Grid = grid;
            Grid.OnBlockAdded += Grid_OnBlockAdded;
            Grid.OnBlockRemoved += Grid_OnBlockRemoved;

            GridComparer = Comparer<IMyCubeGrid>.Create((x, y) =>
            {
                return (int)(Vector3D.DistanceSquared(gridPosition, x.GetPosition()) - Vector3D.DistanceSquared(gridPosition, y.GetPosition()));
            });
            CharacterComparer = Comparer<IMyCharacter>.Create((x, y) =>
            {
                return (int)(Vector3D.DistanceSquared(gridPosition, x.GetPosition()) - Vector3D.DistanceSquared(gridPosition, y.GetPosition()));
            });
            ProjectileComparer = Comparer<uint>.Create((x, y) =>
            {
                return (int)(Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(x).Position) - Vector3D.DistanceSquared(gridPosition, ProjectileManager.I.GetProjectile(y).Position));
            });

            SetTargetingFlags();
            HeartLog.Log($"GridAiTargeting initialized for grid '{grid.DisplayName}' with targeting enabled: {Enabled}");
        }

        private void Grid_OnBlockAdded(IMySlimBlock obj)
        {
            if (obj.FatBlock is SorterWeaponLogic)
            {
                EnableGridAiIfNeeded();
            }
        }

        private void Grid_OnBlockRemoved(IMySlimBlock obj)
        {
            if (obj.FatBlock is SorterWeaponLogic)
            {
                DisableGridAiIfNeeded();
            }
        }

        public void EnableGridAiIfNeeded()
        {
            if (!Enabled && Weapons.Count > 0)
            {
                Enabled = true;
                SetTargetingFlags();
                HeartLog.Log($"GridAiTargeting enabled for grid '{Grid.DisplayName}'");
            }
        }

        public void DisableGridAiIfNeeded()
        {
            if (Enabled && Weapons.Count == 0)
            {
                Enabled = false;
                HeartLog.Log($"GridAiTargeting disabled for grid '{Grid.DisplayName}' due to no weapons");
            }
        }

        public void SetPrimaryTarget(IMyCubeGrid entity)
        {
            PrimaryGridTarget = entity;
        }

        private DateTime lastLogTime = DateTime.MinValue;

        public void UpdateTargeting()
        {
            if (Grid.Physics == null) return;

            try
            {
                if (!Enabled) return;

                SetTargetingFlags();
                ScanForTargets();

                MyEntity manualTarget = null;
                bool isTargetLocked = keenTargeting != null && keenTargeting.IsTargetLocked(Grid);

                if (isTargetLocked)
                {
                    manualTarget = keenTargeting.GetTarget(Grid);
                    var gridTarget = manualTarget as IMyCubeGrid;
                    if (gridTarget != null)
                        PrimaryGridTarget = gridTarget;
                }

                List<object> potentialTargets = new List<object>();
                potentialTargets.AddRange(TargetedGrids.Keys.Cast<object>());
                potentialTargets.AddRange(TargetedCharacters.Keys.Cast<object>());
                potentialTargets.AddRange(TargetedProjectiles.Keys.Select(id => (object)ProjectileManager.I.GetProjectile(id)));

                foreach (var weapon in Weapons)
                {
                    var turret = weapon as SorterTurretLogic;
                    if (turret == null || !turret.SorterWep.IsWorking)
                        continue;

                    bool turretHasTarget = false;

                    if (isTargetLocked)
                    {
                        if (manualTarget != null && ShouldConsiderTarget(manualTarget, turret))
                        {
                            turret.SetTarget(manualTarget);
                            turretHasTarget = true;
                        }
                        continue;
                    }

                    var prioritizedTargets = GetPrioritizedTargets(potentialTargets, turret);

                    if (turret.PreferUniqueTargetsState)
                    {
                        var assignedTargets = AssignUniqueTargets(prioritizedTargets);

                        foreach (var target in assignedTargets)
                        {
                            if (ShouldConsiderTarget(target, turret))
                            {
                                turret.SetTarget(target);
                                turretHasTarget = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var target in prioritizedTargets)
                        {
                            if (ShouldConsiderTarget(target, turret))
                            {
                                turret.SetTarget(target);
                                turretHasTarget = true;
                                break;
                            }
                        }
                    }

                    if (!turretHasTarget)
                    {
                        turret.ResetTargetingState();
                    }
                }
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(GridAiTargeting));
            }
        }

        private List<object> GetPrioritizedTargets(List<object> targets, SorterTurretLogic turret)
        {
            targets.Sort((a, b) => TargetPriority.GetTargetPriority(b, turret).CompareTo(TargetPriority.GetTargetPriority(a, turret)));
            return targets;
        }

        private List<object> AssignUniqueTargets(List<object> prioritizedTargets)
        {
            Dictionary<object, int> targetAssignments = new Dictionary<object, int>();
            List<object> assignedTargets = new List<object>();

            foreach (var target in prioritizedTargets)
            {
                targetAssignments[target] = 0;
            }

            foreach (var weapon in Weapons)
            {
                var turret = weapon as SorterTurretLogic;
                if (turret == null || !turret.SorterWep.IsWorking)
                    continue;

                object leastAssignedTarget = targetAssignments.OrderBy(kvp => kvp.Value).FirstOrDefault().Key;
                if (leastAssignedTarget != null)
                {
                    assignedTargets.Add(leastAssignedTarget);
                    targetAssignments[leastAssignedTarget]++;
                }
            }

            return assignedTargets;
        }

        private bool ShouldConsiderTarget(object target, SorterTurretLogic turret)
        {
            return TargetPriority.ShouldConsiderTarget(target, turret);
        }

        /// <summary>
        /// Scan all turrets for flags
        /// </summary>
        private void SetTargetingFlags()
        {
            try
            {
                Enabled = Weapons.Count > 0; // Disable if it has no weapons
                if (!Enabled)
                    return;

                DoesTargetGrids = false;
                DoesTargetCharacters = false;
                DoesTargetProjectiles = false;
                MaxTargetingRange = 0;
                foreach (var weapon in Weapons)
                {
                    var turret = weapon as SorterTurretLogic;
                    if (turret != null) // Only set targeting flags with turrets
                    {
                        DoesTargetGrids |= turret.Settings.TargetGridsState;
                        DoesTargetCharacters |= turret.Settings.TargetCharactersState;
                        DoesTargetProjectiles |= turret.Settings.TargetProjectilesState;
                    }

                    float maxTrajectory = ProjectileDefinitionManager.GetDefinition(weapon.Magazines.SelectedAmmoId)?.PhysicalProjectile.MaxTrajectory ?? 0;
                    if (maxTrajectory > MaxTargetingRange)
                        MaxTargetingRange = maxTrajectory;
                }

                MaxTargetingRange *= 1.1f; // Increase range by a little bit to make targeting less painful

                if (Enabled) // Disable if MaxRange = 0.
                    Enabled = MaxTargetingRange > 0;

                // Other targeting logic here
            }
            catch (Exception ex)
            {
                HeartLog.LogException(ex, typeof(GridAiTargeting), "Error in SetTargetingFlags: ");
                Enabled = false;
            }
        }

        private void ScanForTargets()
        {
            if (!Enabled)
                return;

            if (Grid.Physics == null)
            {
                return;
            }

            BoundingSphereD sphere = new BoundingSphereD(Grid.PositionComp.WorldAABB.Center, MaxTargetingRange);

            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, entities);

            List<IMyCubeGrid> allGrids = new List<IMyCubeGrid>();
            List<IMyCharacter> allCharacters = new List<IMyCharacter>();

            foreach (var entity in entities)
            {
                if (entity == Grid || entity.Physics == null)
                    continue;
                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    allGrids.Add(grid);
                }
                else
                {
                    var character = entity as IMyCharacter;
                    if (character != null)
                    {
                        allCharacters.Add(character);
                    }
                }
            }

            List<Projectile> allProjectiles = new List<Projectile>();
            ProjectileManager.I.GetProjectilesInSphere(sphere, ref allProjectiles, true);

            UpdateAvailableTargets(allGrids, allCharacters, allProjectiles, false);
        }

        public void UpdateAvailableTargets(List<IMyCubeGrid> allGrids, List<IMyCharacter> allCharacters, List<Projectile> allProjectiles, bool distanceCheck = true)
        {
            float maxRangeSq = MaxTargetingRange * MaxTargetingRange;

            Dictionary<IMyCubeGrid, int> gridBuffer = new Dictionary<IMyCubeGrid, int>();
            Dictionary<IMyCharacter, int> charBuffer = new Dictionary<IMyCharacter, int>();
            Dictionary<uint, int> projBuffer = new Dictionary<uint, int>();

            if (DoesTargetGrids) // Limit valid grids to those in range
                foreach (var grid in allGrids)
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, grid.GetPosition()) < maxRangeSq)
                        gridBuffer.Add(grid, 0);

            if (DoesTargetCharacters) // Limit valid characters to those in range
                foreach (var character in allCharacters)
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, character.GetPosition()) < maxRangeSq)
                        charBuffer.Add(character, 0);

            if (DoesTargetProjectiles) // Limit valid projectiles to those in range
                foreach (var projectile in allProjectiles)
                    if (!distanceCheck || Vector3D.DistanceSquared(gridPosition, projectile.Position) < maxRangeSq)
                        projBuffer.Add(projectile.Id, 0);

            TargetedGrids = new SortedList<IMyCubeGrid, int>(gridBuffer, GridComparer);
            TargetedCharacters = new SortedList<IMyCharacter, int>(charBuffer, CharacterComparer);
            TargetedProjectiles = new SortedList<uint, int>(projBuffer, ProjectileComparer);
        }

        public void Close()
        {
            HeartLog.Log($"Closing GridAiTargeting for grid '{Grid.DisplayName}'");
            Grid.OnBlockAdded -= Grid_OnBlockAdded;
            Grid.OnBlockRemoved -= Grid_OnBlockRemoved;
            TargetedGrids.Clear();
            TargetedCharacters.Clear();
            TargetedProjectiles.Clear();
        }

        private Comparer<IMyCubeGrid> GridComparer;
        private Comparer<IMyCharacter> CharacterComparer;
        private Comparer<uint> ProjectileComparer;
    }
}
