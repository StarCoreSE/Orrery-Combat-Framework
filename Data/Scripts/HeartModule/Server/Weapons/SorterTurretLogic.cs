using Orrery.HeartModule.Shared.Definitions;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Server.Weapons
{
    internal class SorterTurretLogic : SorterWeaponLogic
    {
        public SorterTurretLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, uint id) : base(sorterWep, definition, id)
        {

        }
    }
}
