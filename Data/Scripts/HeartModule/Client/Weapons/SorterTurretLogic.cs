using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Weapons.Settings;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Client.Weapons
{
    internal class SorterTurretLogic : SorterSmartLogic
    {
        public new TurretSettings Settings => (TurretSettings)base.Settings;
        internal override WeaponSettings CreateSettings() => new TurretSettings(SorterWep.EntityId);


        public SorterTurretLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {
        }
    }
}
