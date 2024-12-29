using System.Collections.Generic;
using System.Linq;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Orrery.HeartModule.Server.Weapons
{
    /// <summary>
    /// Creates and manages weapon objects.
    /// </summary>
    internal class WeaponManager
    {
        private static WeaponManager _;

        private Dictionary<long, SorterWeaponLogic> _weapons = new Dictionary<long, SorterWeaponLogic>();

        public WeaponManager()
        {
            _ = this;

            MyCubeGrid.OnBlockAddedGlobally += OnBlockAddedGlobally;
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;

            HeartLog.Info("WeaponManager initialized.");
        }
        public void Close()
        {
            MyCubeGrid.OnBlockAddedGlobally -= OnBlockAddedGlobally;
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;

            _ = null;
            HeartLog.Info("WeaponManager closed.");
        }

        #region Blocks

        private void AddWeapon(IMyConveyorSorter sorter, WeaponDefinitionBase definition)
        {
            SorterWeaponLogic logic;

            if (definition.Assignments.HasAzimuth && definition.Assignments.HasElevation)
                logic = new SorterTurretLogic(sorter, definition, sorter.EntityId);
            else
                logic = new SorterWeaponLogic(sorter, definition, sorter.EntityId);

            _weapons.Add(logic.Id, logic);
        }

        internal static void RemoveWeapon(long id)
        {
            _?._weapons.Remove(id);
        }

        private void OnBlockAddedGlobally<T>(T obj) where T : IMySlimBlock
        {
            if (obj?.FatBlock == null || obj.CubeGrid.Physics == null || !(obj.FatBlock is IMyConveyorSorter))
                return;

            var definition = DefinitionManager.WeaponDefinitions.Values.FirstOrDefault(dictDefinition => dictDefinition.Assignments.BlockSubtype == obj.BlockDefinition.Id.SubtypeName);
            if (definition == null)
                return;

            AddWeapon(obj.FatBlock as IMyConveyorSorter, definition);
        }

        /// <summary>
        /// Handling for grids spawning with blocks on them already
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void OnEntityAdd(IMyEntity obj)
        {
            IMyCubeGrid grid = obj as IMyCubeGrid;
            if (grid == null)
                return;

            foreach (var block in grid.GetFatBlocks<IMyConveyorSorter>())
                OnBlockAddedGlobally(block.SlimBlock);
        }

        #endregion
    }
}
