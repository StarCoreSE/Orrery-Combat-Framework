using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.WeaponSettings;
using Sandbox.ModAPI;
using System;

namespace Orrery.HeartModule.Server.Weapons
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

        public SorterTurretLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {
            Settings = new TurretSettings(sorterWep.EntityId);
        }

        internal override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();

            Settings.LockedNetworking = true;

            Settings.AiRange = Definition.Targeting.MaxTargetingRange;
            Settings.PreferUniqueTargetState = false;
            Settings.TargetGridsState = true;
            Settings.TargetSmallGridsState = true;
            Settings.TargetLargeGridsState = true;
            Settings.TargetCharactersState = true;
            Settings.TargetProjectilesState = true;
            Settings.TargetEnemiesState = true;
            Settings.TargetFriendliesState = false;
            Settings.TargetNeutralsState = false;
            Settings.TargetUnownedState = true;

            Settings.LockedNetworking = false;
        }
    }
}
