using System;
using System.Collections.Generic;
using System.Linq;
using Orrery.HeartModule.Server.Weapons;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
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
        internal HashSet<SorterWeaponLogic> GridWeapons = new HashSet<SorterWeaponLogic>();
        /// <summary>
        /// Weapons contained by all subpart grids
        /// </summary>
        internal HashSet<SorterWeaponLogic> AllWeapons = new HashSet<SorterWeaponLogic>();

        /// <summary>
        /// Diagonal radius of grid + maximum targeting range of weapons on grid.
        /// </summary>
        internal int MaxTargetingRange => GridSize + (int)AllWeapons.Max(wep => wep.Definition.Targeting.MaxTargetingRange);

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

        public GridTargeting(IMyCubeGrid grid)
        {
            if (grid.Physics == null)
                throw new Exception("Grid is invalid!");

            _targetingSphere = new BoundingSphereD(grid.PositionComp.GetPosition(), MaxTargetingRange);
            CheckIfLargest();

            GridGroup = grid.GetGridGroup(GridLinkTypeEnum.Physical);
            GridGroup.OnGridAdded += OnGroupModified;
            GridGroup.OnGridRemoved += OnGroupModified;

            grid.OnClosing += entity =>
            {
                GridGroup.OnGridAdded -= OnGroupModified;
                GridGroup.OnGridRemoved -= OnGroupModified;
            };
        }

        public void Update()
        {
            // Only the largest grid does targeting.
            if (!IsLargestInGroup)
                return;
        }

        public void Update10()
        {
            // Only the largest grid does targeting.
            if (!IsLargestInGroup)
                return;

            _entityBuffer.Clear();
            _targetingSphere.Center = Grid.PositionComp.GetPosition();
            _targetingSphere.Radius = MaxTargetingRange;

            // Don't waste CPU time on grids that can't target.
            if (AllWeapons.Count == 0)
                return;
            
            // Get all valid targets able to be targeted by the grid
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref _targetingSphere, _entityBuffer);
            _entityBuffer = _entityBuffer.Where(ent => ent is IMyCharacter || ent is IMyCubeGrid).ToList();
        }

        public void AddWeapon(SorterWeaponLogic weapon)
        {
            GridWeapons.Add(weapon);
            if (!IsLargestInGroup)
                MasterTargeting.AllWeapons.Add(weapon);
        }

        public void RemoveWeapon(SorterWeaponLogic weapon)
        {
            GridWeapons.Remove(weapon);
            if (!IsLargestInGroup)
                MasterTargeting.AllWeapons.Remove(weapon);
        }

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
                var slaveTargeting = GridTargetingManager.GetGridTargeting(grid);
                SlaveTargeting.Add(slaveTargeting);
                foreach (var weapon in slaveTargeting.GridWeapons)
                    AllWeapons.Add(weapon);
            }
        }

        private void OnGroupModified(IMyGridGroupData arg1, IMyCubeGrid arg2, IMyGridGroupData previousOrNewData)
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
        }
    }
}
