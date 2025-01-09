using Orrery.HeartModule.Server.Weapons.Targeting;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Weapons.Settings;
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

        internal override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();

            Settings.LockedNetworking = true;
            
            Settings.PreferUniqueTargetState = (Definition.Targeting.DefaultIff & IFFEnum.TargetUnique) == IFFEnum.TargetUnique;

            Settings.TargetGridsState = (Definition.Targeting.AllowedTargetTypes & TargetTypeEnum.TargetGrids) == TargetTypeEnum.TargetGrids;
            Settings.TargetSmallGridsState = Settings.TargetGridsState;
            Settings.TargetLargeGridsState = Settings.TargetGridsState;

            Settings.TargetCharactersState = (Definition.Targeting.AllowedTargetTypes & TargetTypeEnum.TargetCharacters) == TargetTypeEnum.TargetCharacters;
            Settings.TargetProjectilesState = (Definition.Targeting.AllowedTargetTypes & TargetTypeEnum.TargetProjectiles) == TargetTypeEnum.TargetProjectiles;

            Settings.TargetEnemiesState = (Definition.Targeting.DefaultIff & IFFEnum.TargetEnemies) == IFFEnum.TargetEnemies;
            Settings.TargetFriendliesState = (Definition.Targeting.DefaultIff & IFFEnum.TargetFriendlies) == IFFEnum.TargetFriendlies;
            Settings.TargetNeutralsState = (Definition.Targeting.DefaultIff & IFFEnum.TargetNeutrals) == IFFEnum.TargetNeutrals;
            Settings.TargetUnownedState = (Definition.Targeting.DefaultIff & IFFEnum.TargetNeutrals) == IFFEnum.TargetNeutrals;

            Settings.LockedNetworking = false;
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

            Targeting.UpdateTargeting(1/60d);
            base.UpdateAfterSimulation();
        }
    }
}
