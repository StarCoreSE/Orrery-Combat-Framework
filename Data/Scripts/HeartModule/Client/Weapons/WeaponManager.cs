﻿using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Orrery.HeartModule.Client.Weapons
{
    /// <summary>
    /// Creates and manages weapon objects.
    /// </summary>
    internal class WeaponManager
    {
        private static WeaponManager _;

        private Dictionary<long, SorterWeaponLogic> _weapons = new Dictionary<long, SorterWeaponLogic>();
        private HashSet<SorterWeaponLogic> _newWeapons = new HashSet<SorterWeaponLogic>();

        public WeaponManager()
        {
            _ = this;

            DefinitionManager.DefinitionApi.OnReady += () =>
            {
                MyCubeGrid.OnBlockAddedGlobally += OnBlockAddedGlobally;
                MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;

                HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);
                foreach (var entity in entities)
                    OnEntityAdd(entity);
            };

            HeartLog.Info("Client WeaponManager initialized.");
        }
        public void Close()
        {
            MyCubeGrid.OnBlockAddedGlobally -= OnBlockAddedGlobally;
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;

            _ = null;
            HeartLog.Info("Client WeaponManager closed.");
        }

        public void UpdateBeforeSimulation()
        {
            foreach (var weapon in _newWeapons)
                weapon.UpdateOnceBeforeFrame();
            _newWeapons.Clear();
        }

        #region Blocks

        private void AddWeapon(IMyConveyorSorter sorter, WeaponDefinitionBase definition)
        {
            if (_weapons.ContainsKey(sorter.EntityId))
                return;

            SorterWeaponLogic logic;

            if (definition.IsTurret)
                logic = new SorterTurretLogic(sorter, definition, sorter.EntityId);
            else if (definition.IsSmart)
                logic = new SorterSmartLogic(sorter, definition, sorter.EntityId);
            else
                logic = new SorterWeaponLogic(sorter, definition, sorter.EntityId);

            _newWeapons.Add(logic);
            _weapons.Add(logic.Id, logic);
        }

        internal static SorterWeaponLogic GetWeapon(long id)
        {
            return _?._weapons.GetValueOrDefault(id, null);
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
