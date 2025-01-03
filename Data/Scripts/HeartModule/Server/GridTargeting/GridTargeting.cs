using System;
using System.Collections.Generic;
using System.Linq;
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
        public Dictionary<TargetingStateEnum, List<IMyEntity>> AvailableTargets = new Dictionary<TargetingStateEnum, List<IMyEntity>>
        {
            [TargetingStateEnum.Grids] = new List<IMyEntity>(),
            [TargetingStateEnum.LargeGrids] = new List<IMyEntity>(),
            [TargetingStateEnum.SmallGrids] = new List<IMyEntity>(),
            [TargetingStateEnum.Projectiles] = new List<IMyEntity>(),
            [TargetingStateEnum.Characters] = new List<IMyEntity>(),
            [TargetingStateEnum.Friendlies] = new List<IMyEntity>(),
            [TargetingStateEnum.Neutrals] = new List<IMyEntity>(),
            [TargetingStateEnum.Enemies] = new List<IMyEntity>(),
            [TargetingStateEnum.Unowned] = new List<IMyEntity>(),
        };

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

            // Get valid target types
            {
                // Always allow grid targeting
                AllowedTargetTypes = TargetingStateEnum.Grids | TargetingStateEnum.LargeGrids | TargetingStateEnum.SmallGrids;
                foreach (var turret in AllWeapons.OfType<SorterTurretLogic>())
                    AllowedTargetTypes |= (TargetingStateEnum) turret.Settings.TargetStateContainer;

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
                        AvailableTargets[TargetingStateEnum.Grids].Add(grid);
                        if (grid.GridSizeEnum == MyCubeSize.Large)
                            AvailableTargets[TargetingStateEnum.LargeGrids].Add(grid);
                        else
                            AvailableTargets[TargetingStateEnum.SmallGrids].Add(grid);

                        relations = RelationUtils.GetRelationsBetweeenGrids(Grid, grid);
                    }
                    else if (character != null)
                    {
                        var player = HeartData.I.Players.FirstOrDefault(p => p.Character == character);
                        if (player == null) // I'm too lazy to let offline characters be fired on.
                            continue;

                        AvailableTargets[TargetingStateEnum.Characters].Add(character);
                        relations = RelationUtils.GetRelationsBetweenGridAndPlayer(Grid, player.IdentityId);
                    }

                    // Relation type
                    // Relations should always be set because only grids and characters can be in the entity buffer.
                    switch (relations)
                    {
                        case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                            AvailableTargets[TargetingStateEnum.Unowned].Add(entity);
                            break;
                        case MyRelationsBetweenPlayerAndBlock.Owner:
                        case MyRelationsBetweenPlayerAndBlock.Friends:
                        case MyRelationsBetweenPlayerAndBlock.FactionShare:
                            AvailableTargets[TargetingStateEnum.Friendlies].Add(entity);
                            break;
                        case MyRelationsBetweenPlayerAndBlock.Neutral:
                            AvailableTargets[TargetingStateEnum.Neutrals].Add(entity);
                            break;
                        case MyRelationsBetweenPlayerAndBlock.Enemies:
                            AvailableTargets[TargetingStateEnum.Enemies].Add(entity);
                            break;
                        default:
                            throw new Exception("Invalid relationship state!");
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
