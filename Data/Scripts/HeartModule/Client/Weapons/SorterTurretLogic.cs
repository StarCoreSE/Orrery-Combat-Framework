using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.WeaponSettings;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Network;

namespace Orrery.HeartModule.Client.Weapons
{
    internal class SorterTurretLogic : SorterWeaponLogic
    {
        public new TurretSettings Settings
        {
            get
            {
                return (TurretSettings)base.Settings;
            }
            set
            {
                base.Settings = value;
            }
        }

        public SorterTurretLogic(IMyConveyorSorter block, WeaponDefinitionBase definition, long id) : base(block, definition, id)
        {
            Settings = new TurretSettings();
        }
    }
}
