using System;
using System.Collections.Generic;
using System.Linq;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Server.Weapons;
using Orrery.HeartModule.Shared.Targeting;
using Orrery.HeartModule.Shared.Utility;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.GridTargeting
{
    internal class GridTargeting
    {
        public readonly IMyCubeGrid Grid;

        /// <summary>
        /// List of all targetable entities, updated every 10 ticks.
        /// </summary>
        private List<MyEntity> _entityBuffer = new List<MyEntity>();
        private BoundingSphereD _targetingSphere;
        /// <summary>
        /// Weapons contained by this grid
        /// </summary>
        private HashSet<SorterWeaponLogic> GridWeapons = new HashSet<SorterWeaponLogic>();
        /// <summary>
        /// Weapons contained by all subpart grids
        /// </summary>
        internal HashSet<SorterWeaponLogic> AllWeapons = new HashSet<SorterWeaponLogic>();

        /// <summary>
        /// Diagonal radius of grid + maximum targeting range of weapons on grid.
        /// </summary>
        internal int MaxTargetingRange => GridSize + (AllWeapons.Count > 0 ? (int)AllWeapons.Max(wep => wep.Definition.Targeting.MaxTargetingRange) : 0);

        internal int GridSize => (Grid.Max - Grid.Min).Length() / 2;

        public bool IsLargestInGroup { get; private set; }

        /// <summary>
        /// If this is the largest grid in the grid group, contains all gridtargetings.
        /// </summary>
        public HashSet<GridTargeting> SlaveTargeting = new HashSet<GridTargeting>();
        /// <summary>
        /// If this is not the largest grid in the grid group, contains the primary gridtargeting.
        /// </summary>
        public GridTargeting MasterTargeting = null;

        internal IMyGridGroupData GridGroup;
        private bool _needsGroupUpdate = false;

        /// <summary>
        /// Preferred targets of each type, mapped to <see cref="TargetingStateEnum">TargetingStateEnum</see>
        /// </summary>
        public Dictionary<TargetingStateEnum, List<ITargetable>> AvailableTargets = new Dictionary<TargetingStateEnum, List<ITargetable>>
        {
            [TargetingStateEnum.Grids] = new List<ITargetable>(),
            [TargetingStateEnum.LargeGrids] = new List<ITargetable>(),
            [TargetingStateEnum.SmallGrids] = new List<ITargetable>(),
            [TargetingStateEnum.Projectiles] = new List<ITargetable>(),
            [TargetingStateEnum.Characters] = new List<ITargetable>(),
        };

        /// <summary>
        /// Maps targets to number of turrets locked. Used for PreferUnique
        /// </summary>
        public Dictionary<ITargetable, int> TargetLocks = new Dictionary<ITargetable, int>();

        public TargetingStateEnum AllowedTargetTypes { get; private set; }

        #region Internal

        public GridTargeting(IMyCubeGrid grid)
        {
            Grid = grid;

            _targetingSphere = new BoundingSphereD();

            GridGroup = grid.GetGridGroup(GridLinkTypeEnum.Physical);
            GridGroup.OnGridAdded += OnGroupModified;
            GridGroup.OnGridRemoved += OnGroupModified;

            CheckIfLargest();
        }

        public void Update()
        {
            if (Grid.Closed)
            {
                Close();
                return;
            }

            if (_needsGroupUpdate)
            {
                if (GridGroup != Grid.GetGridGroup(GridLinkTypeEnum.Physical))
                {
                    GridGroup.OnGridAdded -= OnGroupModified;
                    GridGroup.OnGridRemoved -= OnGroupModified;
                    GridGroup = Grid.GetGridGroup(GridLinkTypeEnum.Physical);
                    GridGroup.OnGridAdded += OnGroupModified;
                    GridGroup.OnGridRemoved += OnGroupModified;
                }

                CheckIfLargest();
                _needsGroupUpdate = false;
            }

            // Only the largest grid does targeting.
            if (!IsLargestInGroup)
                return;
        }

        public void Update10()
        {
            // Only the largest grid does targeting.
            if (!IsLargestInGroup)
            {
                AvailableTargets = MasterTargeting.AvailableTargets;
                AllowedTargetTypes = MasterTargeting.AllowedTargetTypes;
                return;
            }

            // Pull targets
            {
                _entityBuffer.Clear();
                _targetingSphere.Center = Grid.PositionComp.GetPosition();
                _targetingSphere.Radius = MaxTargetingRange;

                // Don't waste CPU time on grids that can't target.
                if (AllWeapons.Count == 0)
                    return;

                List<IMyCubeGrid> attachedGrids = new List<IMyCubeGrid>();
                GridGroup.GetGrids(attachedGrids);
            
                // Get all valid targets able to be targeted by the grid
                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref _targetingSphere, _entityBuffer);
                _entityBuffer = _entityBuffer.Where(ent => (ent is IMyCharacter || ent is IMyCubeGrid) && !attachedGrids.Contains(ent as IMyCubeGrid)).ToList();
            }

            // Sort targets
            {
                TargetLocks.Clear();

                // Always allow grid targeting
                AllowedTargetTypes = TargetingStateEnum.Grids | TargetingStateEnum.LargeGrids | TargetingStateEnum.SmallGrids;
                foreach (var turret in AllWeapons.OfType<SorterTurretLogic>())
                {
                    AllowedTargetTypes |= (TargetingStateEnum) turret.Settings.TargetStateContainer;
                    UpdateTurretTarget(turret.Targeting.Target, true);
                }

                foreach (var list in AvailableTargets.Values)
                    list.Clear();

                foreach (var entity in _entityBuffer)
                {
                    MyRelationsBetweenPlayerAndBlock relations = MyRelationsBetweenPlayerAndBlock.NoOwnership;

                    // Entity type
                    var grid = entity as IMyCubeGrid;
                    var character = entity as IMyCharacter;
                    if (grid != null)
                    {
                        AvailableTargets[TargetingStateEnum.Grids].Add(new TargetableEntity(grid));
                        if (grid.GridSizeEnum == MyCubeSize.Large)
                            AvailableTargets[TargetingStateEnum.LargeGrids].Add(new TargetableEntity(grid));
                        else
                            AvailableTargets[TargetingStateEnum.SmallGrids].Add(new TargetableEntity(grid));
                    }
                    else if (character != null)
                    {
                        var player = HeartData.I.Players.FirstOrDefault(p => p.Character == character);
                        if (player == null) // I'm too lazy to let offline characters be fired on.
                            continue;

                        AvailableTargets[TargetingStateEnum.Characters].Add(new TargetableEntity(character));
                    }
                }

                // Don't waste cpu time looking for projectiles if we can't target them.
                if ((AllowedTargetTypes & TargetingStateEnum.Projectiles) == TargetingStateEnum.Projectiles)
                {
                    foreach (var projectile in ProjectileManager.GetProjectilesInSphere(_targetingSphere))
                    {
                        var target = new TargetableProjectile(projectile);
                        var relations = target.GetRelations(Grid);
                        if (relations == MyRelationsBetweenPlayerAndBlock.Enemies || relations == MyRelationsBetweenPlayerAndBlock.NoOwnership || relations == MyRelationsBetweenPlayerAndBlock.Neutral)
                            AvailableTargets[TargetingStateEnum.Projectiles].Add(target);
                    }
                }

                // Sort targets by distance to grid
                var gridPosition = Grid.PositionComp.GetPosition();
                foreach (var list in AvailableTargets.ToArray())
                    AvailableTargets[list.Key] = list.Value.OrderBy(ent => Vector3D.DistanceSquared(gridPosition, ent.GetPosition())).ToList();
            }
        }

        public void Close()
        {
            GridGroup.OnGridAdded -= OnGroupModified;
            GridGroup.OnGridRemoved -= OnGroupModified;
        }

        #endregion

        #region Interface

        public void UpdateTurretTarget(ITargetable target, bool isLocked)
        {
            if (target == null)
                return;

            if (isLocked)
            {
                if (!TargetLocks.ContainsKey(target))
                    TargetLocks[target] = 1;
                else
                    TargetLocks[target]++;
            }
            else
            {
                if (!TargetLocks.ContainsKey(target))
                    TargetLocks[target] = 0;
                else
                    TargetLocks[target]--;
            }
        }

        public void AddWeapon(SorterWeaponLogic weapon)
        {
            GridWeapons.Add(weapon);
            if (!IsLargestInGroup)
                MasterTargeting.AllWeapons.Add(weapon);
            else
                AllWeapons.Add(weapon);
        }

        public void RemoveWeapon(SorterWeaponLogic weapon)
        {
            GridWeapons.Remove(weapon);
            if (!IsLargestInGroup)
                MasterTargeting.AllWeapons.Remove(weapon);
            else
                AllWeapons.Remove(weapon);
        }

        #endregion

        #region Private

        private void CheckIfLargest()
        {
            // Reset vars
            List<IMyCubeGrid> attachedGrids = new List<IMyCubeGrid>();
            GridGroup.GetGrids(attachedGrids);
            IsLargestInGroup = true;
            MasterTargeting = null;
            SlaveTargeting.Clear();
            AllWeapons.Clear();

            // Check if largest grid in group
            foreach (var grid in attachedGrids)
            {
                if (grid == Grid)
                    continue;

                int checkingGridSize = (grid.Max - grid.Min).Length() / 2;
                if (checkingGridSize < GridSize)
                    continue;
                // If not, check if grid in question is largest in group
                IsLargestInGroup = false;
                if (MasterTargeting == null || MasterTargeting.GridSize < checkingGridSize)
                    MasterTargeting = GridTargetingManager.GetGridTargeting(grid);
            }

            if (!IsLargestInGroup)
                return;

            // Add slave targetings
            foreach (var grid in attachedGrids)
            {
                if (grid == Grid)
                    continue;

                var slaveTargeting = GridTargetingManager.GetGridTargeting(grid);
                SlaveTargeting.Add(slaveTargeting);
                foreach (var weapon in slaveTargeting.GridWeapons)
                    AllWeapons.Add(weapon);
            }

            // Add own weapons
            foreach (var weapon in GridWeapons)
                AllWeapons.Add(weapon);
        }

        private void OnGroupModified(IMyGridGroupData arg1, IMyCubeGrid arg2, IMyGridGroupData previousOrNewData)
        {
            _needsGroupUpdate = true;
        }

        #endregion
    }
}
