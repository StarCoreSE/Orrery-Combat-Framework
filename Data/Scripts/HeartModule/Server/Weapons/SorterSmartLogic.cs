using Orrery.HeartModule.Server.Weapons.Targeting;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.WeaponSettings;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Server.Weapons
{
    /// <summary>
    /// Controls "smart" weapons, like turrets and guided missile launchers. Cannot automatically fire.
    /// </summary>
    internal class SorterSmartLogic : SorterWeaponLogic
    {
        public new SmartSettings Settings => (SmartSettings)base.Settings;

        internal virtual SmartWeaponTargeting CreateTargeting() => new SmartWeaponTargeting(this);
        internal override WeaponSettings CreateSettings() => new SmartSettings(SorterWep.EntityId);

        public SmartWeaponTargeting Targeting { get; internal set; }

        public SorterSmartLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {

        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            Targeting = CreateTargeting();
        }

        public override void UpdateAfterSimulation()
        {
            if (!SorterWep.IsWorking) // Don't turn if the turret is disabled
                return;

            Targeting.UpdateTargeting();
            base.UpdateAfterSimulation();
        }
    }
}
