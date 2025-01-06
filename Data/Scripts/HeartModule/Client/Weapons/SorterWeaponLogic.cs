using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.WeaponSettings;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Network;

namespace Orrery.HeartModule.Client.Weapons
{
    internal class SorterWeaponLogic : MyGameLogicComponent, IMyEventProxy
    {
        public readonly long Id;
        public readonly WeaponDefinitionBase Definition;
        public readonly IMyConveyorSorter SorterWep;

        internal WeaponSettings Settings;
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
                Settings = CreateSettings();
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

                Settings.RequestSync();
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
    }
}
