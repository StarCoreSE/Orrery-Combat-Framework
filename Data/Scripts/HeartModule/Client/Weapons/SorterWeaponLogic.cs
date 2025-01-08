using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.WeaponSettings;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Network;
using VRageMath;

namespace Orrery.HeartModule.Client.Weapons
{
    internal class SorterWeaponLogic : MyGameLogicComponent, IMyEventProxy
    {
        public readonly long Id;
        public readonly WeaponDefinitionBase Definition;
        public readonly IMyConveyorSorter SorterWep;

        /// <summary>
        /// Whether this weapon has a visible inventory.
        /// </summary>
        public bool HasInventory { get; internal set; } = true;

        internal WeaponSettings Settings = null;
        internal virtual WeaponSettings CreateSettings() => new WeaponSettings(SorterWep.EntityId);


        public SorterWeaponLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id)
        {
            SorterWep = sorterWep;
            Definition = definition;
            Id = id;

            sorterWep.OnClose += OnClose;

            sorterWep.GameLogic.Container.Add(this);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                if (Settings == null)
                {
                    Settings = CreateSettings();
                    Settings.RequestSync();
                }

                AssignInventory();

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MarkedForClose || SorterWep == null)
                    return;

                // yeah this is a bit stupid but whatever I don't care. if it's an issue will fix.
                if (!HasInventory)
                    SorterWep.ShowInInventory = false;
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }
        }

        public void OnClose(IMyEntity ent)
        {
            WeaponManager.RemoveWeapon(Id);
            SorterWep.OnClose -= OnClose;
        }

        public void PlayPreShootSound()
        {
            if (!string.IsNullOrEmpty(Definition.Audio.ShootSound))
                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.PreShootSound, SorterWep.GetPosition());
        }

        public void OnShoot()
        {
            if (!string.IsNullOrEmpty(Definition.Audio.ShootSound))
                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.ShootSound, SorterWep.GetPosition());

            // TODO: Network this
        }

        /// <summary>
        /// Inventory icon and ammo type constraints
        /// </summary>
        internal virtual void AssignInventory()
        {
            if (!SorterWep.HasInventory)
                return;
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
                HasInventory = false;
                return;
            }

            var allowedIds = MyDefinitionManager.Static.GetInventoryItemDefinitions()
                .Where(def => enumerable.Contains(def.Id.SubtypeName)).Select(def => def.Id);

            foreach (var id in allowedIds)
                inventory.Constraint.Add(id);

            inventory.Refresh();
        }
    }
}
