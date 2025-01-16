using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Orrery.HeartModule.Shared.Weapons.Settings;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Client.Weapons
{
    internal class SorterSmartLogic : SorterWeaponLogic
    {
        public new SmartSettings Settings => (SmartSettings)base.Settings;
        internal override WeaponSettings CreateSettings() => new SmartSettings(SorterWep.EntityId);
        public ITargetable Target = null;

        public SorterSmartLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {
        }
    }
}
