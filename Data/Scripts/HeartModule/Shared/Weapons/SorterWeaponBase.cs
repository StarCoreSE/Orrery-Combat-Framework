using System;
using System.Collections.Generic;
using System.Linq;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Utility;
using Orrery.HeartModule.Shared.Weapons.Settings;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.Weapons
{
    public abstract class SorterWeaponBase : MyGameLogicComponent
    {
        public readonly long Id;
        public readonly IMyConveyorSorter SorterWep;
        protected readonly SubpartManager SubpartManager = new SubpartManager();
        public readonly WeaponDefinitionBase Definition;

        /// <summary>
        /// Whether this weapon has a visible inventory.
        /// </summary>
        public bool HasInventory { get; internal set; } = true;

        public IReadOnlyDictionary<string, IMyModelDummy> MuzzleDummies { get; private set; } = null;
        private IMyEntity _muzzlePart;

        internal WeaponSettings Settings;
        internal virtual WeaponSettings CreateSettings() => new WeaponSettings(SorterWep.EntityId);


        protected SorterWeaponBase(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id)
        {
            SorterWep = sorterWep;
            Definition = definition;
            Id = id;

            sorterWep.GameLogic.Container.Add(this);
        }


        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                // We can assume that the SorterWep actually exists, as we're manually instantiating the weapon logic.
                SetupMuzzles();
                HasInventory = AssignInventory();

                SorterWep.SlimBlock.BlockGeneralDamageModifier = Definition?.Assignments.DurabilityModifier ?? 1f;
                SorterWep.ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId,
                    Definition?.Hardpoint.IdlePower ?? 0f);

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponBase));
            }
        }


        /// <summary>
        /// Inventory icon and ammo type constraints
        /// </summary>
        /// <returns>True if the block needs an inventory, false otherwise.</returns>
        private bool AssignInventory()
        {
            if (!SorterWep.HasInventory)
                return false;
            var inventory = (MyInventory) SorterWep.GetInventory();

            inventory.Constraint = new MyInventoryConstraint("ammo")
            {
                m_useDefaultIcon = false,
            };

            if (!string.IsNullOrEmpty(Definition.Assignments.InventoryIconName))
            {
                //inventory.Constraint.Icon = Platform.Structure.ModPath + "\\Textures\\GUI\\Icons\\" + Definition.Assignments.InventoryIconName;
                inventory.Constraint.UpdateIcon();
            }

            var allowedAmmos = Definition.Loading.Ammos.Select(name =>
                DefinitionManager.ProjectileDefinitions[name].UngroupedDef.MagazineItemToConsume).Where(item => !string.IsNullOrEmpty(item));
            var enumerable = allowedAmmos as string[] ?? allowedAmmos.ToArray();
            if (enumerable.Length == 0)
            {
                SorterWep.ShowInInventory = false;
                inventory.SetFlags(0);
                inventory.MaxVolume = 0;
                return false;
            }

            var allowedIds = MyDefinitionManager.Static.GetInventoryItemDefinitions()
                .Where(def => Enumerable.Contains(enumerable, def.Id.SubtypeName)).Select(def => def.Id);

            foreach (var id in allowedIds)
                inventory.Constraint.Add(id);

            inventory.Refresh();
            return true;
        }

        private void SetupMuzzles()
        {
            if (Definition.Assignments.HasMuzzleSubpart)
                _muzzlePart = SubpartManager.RecursiveGetSubpart(SorterWep, Definition.Assignments.MuzzleSubpart);
            else
                _muzzlePart = SorterWep;

            if (_muzzlePart == null)
                throw new Exception("Invalid muzzle part detected!");

            var _bufferDict = new Dictionary<string, IMyModelDummy>();
            _muzzlePart.Model?.GetDummies(_bufferDict);
            MuzzleDummies = _bufferDict;
        }

        protected MatrixD CalcMuzzleMatrix(int id)
        {
            if (Definition.Assignments.Muzzles.Length == 0 || !MuzzleDummies.ContainsKey(Definition.Assignments.Muzzles[id]))
                return SorterWep.WorldMatrix;

            var ownerWorldMatrix = string.IsNullOrEmpty(Definition.Assignments.MuzzleSubpart)
                ? SorterWep.WorldMatrix
                : SubpartManager.RecursiveGetSubpart(SorterWep, Definition.Assignments.MuzzleSubpart).WorldMatrix;
            return MuzzleDummies[Definition.Assignments.Muzzles[id]].Matrix * ownerWorldMatrix;
        }
    }
}
