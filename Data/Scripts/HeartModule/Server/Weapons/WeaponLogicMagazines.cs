using System;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Weapons
{
    internal class WeaponLogicMagazines
    {
        Loading Definition;
        Audio DefinitionAudio;
        private SorterWeaponLogic _weapon;
        private readonly Func<IMyInventory> GetInventoryFunc;

        private int ShotsPerMag => CurrentAmmo.UngroupedDef.ShotsPerMagazine;
        public ProjectileDefinitionBase CurrentAmmo => DefinitionManager.ProjectileDefinitions[Definition.Ammos[SelectedAmmoIndex]];
        public byte SelectedAmmoIndex => _weapon.Settings?.AmmoLoadedIdx ?? 0;

        public WeaponLogicMagazines(SorterWeaponLogic weapon, Func<IMyInventory> getInventoryFunc, bool startLoaded = false)
        {
            Definition = weapon.Definition.Loading;
            DefinitionAudio = weapon.Definition.Audio;
            GetInventoryFunc = getInventoryFunc;
            _weapon = weapon;
            RemainingReloads = Definition.MaxReloads;
            NextReloadTime = Definition.ReloadTime;
            if (startLoaded)
            {
                MagazinesLoaded = Definition.MagazinesToLoad;
                ShotsInMag = CurrentAmmo.UngroupedDef.ShotsPerMagazine;
            }
        }

        public int MagazinesLoaded = 0;
        public int ShotsInMag = 0;
        public float NextReloadTime = -1; // In seconds
        public int RemainingReloads;

        public void UpdateReload(float delta = 1 / 60f)
        {
            if (RemainingReloads == 0)
                return;

            if (MagazinesLoaded >= Definition.MagazinesToLoad) // Don't load mags if already at capacity
                return;

            if (NextReloadTime == -1)
                return;

            NextReloadTime -= delta;

            if (NextReloadTime <= 0)
            {
                var inventory = GetInventoryFunc?.Invoke();
                string magazineItem = CurrentAmmo.UngroupedDef.MagazineItemToConsume;

                // Check and remove the specified item from the inventory
                if (!string.IsNullOrWhiteSpace(magazineItem) && inventory != null)
                {
                    var itemToConsume = new MyDefinitionId(typeof(MyObjectBuilder_Component), magazineItem);
                    if (inventory.ContainItems(1, itemToConsume))
                    {
                        inventory.RemoveItemsOfType(1, itemToConsume);

                        // Notify item consumption
                        MyVisualScriptLogicProvider.ShowNotification($"Consumed 1 {magazineItem} for reloading.", 1000 / 60, "White");

                        // Reload logic
                        MagazinesLoaded++;
                        RemainingReloads--;
                        NextReloadTime = Definition.ReloadTime;
                        ShotsInMag += ShotsPerMag;
                    }
                    else
                    {
                        // Notify item not available
                        //MyVisualScriptLogicProvider.ShowNotification($"Unable to reload - {magazineItem} not found in inventory.", 1000 / 60, "Red");
                        return;
                    }
                }
                else
                {
                    // Notify when MagazineItemToConsume is not specified
                    // TODO: Note in debug log
                    //MyVisualScriptLogicProvider.ShowNotification("MagazineItemToConsume not specified, proceeding with default reload behavior.", 1000 / 60, "Blue");
                }

                if (!string.IsNullOrEmpty(DefinitionAudio.ReloadSound))
                {
                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(DefinitionAudio.ReloadSound, _weapon.SorterWep.Position); // TODO move this clientside.
                }

                MagazinesLoaded++;
                RemainingReloads--;
                NextReloadTime = Definition.ReloadTime;
                ShotsInMag += ShotsPerMag;

                try
                {
                    _weapon.Definition.LiveMethods.ServerOnReload?.Invoke(_weapon.SorterWep, SelectedAmmoIndex);
                }
                catch (Exception ex)
                {
                    HeartLog.Exception(ex, typeof(WeaponLogicMagazines));
                }
            }
        }

        public bool IsLoaded => ShotsInMag > 0;

        /// <summary>
        /// Mark a bullet as fired.
        /// </summary>
        public void UseShot()
        {
            ShotsInMag--;
            if (ShotsInMag % ShotsPerMag == 0)
            {
                MagazinesLoaded--;
            }
        }

        public void EmptyMagazines()
        {
            ShotsInMag = 0;
            MagazinesLoaded = 0;
            NextReloadTime = Definition.ReloadTime;
        }
    }
}
