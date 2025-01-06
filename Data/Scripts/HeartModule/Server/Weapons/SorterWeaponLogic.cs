using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using Orrery.HeartModule.Server.GridTargeting;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Shared.Utility;
using Orrery.HeartModule.Shared.WeaponSettings;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Network;
using VRageMath;

namespace Orrery.HeartModule.Server.Weapons
{
    /// <summary>
    /// Controls fixed weapons.
    /// </summary>
    internal class SorterWeaponLogic : MyGameLogicComponent, IMyEventProxy
    {
        public readonly IMyConveyorSorter SorterWep;
        public readonly WeaponDefinitionBase Definition;
        public readonly long Id;

        internal Dictionary<string, IMyModelDummy> MuzzleDummies = new Dictionary<string, IMyModelDummy>();
        internal SubpartManager SubpartManager = new SubpartManager();
        internal MatrixD MuzzleMatrix = MatrixD.Identity;
        internal WeaponLogicMagazines Magazine;
        internal WeaponSettings Settings;

        internal virtual WeaponSettings CreateSettings() => new WeaponSettings(SorterWep.EntityId);

        public SorterWeaponLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id)
        {
            SorterWep = sorterWep;
            Definition = definition;
            Id = id;

            Magazine = new WeaponLogicMagazines(this, null); // TODO GetInventoryFunc

            sorterWep.GameLogic.Container.Add(this);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            SorterWep.OnClosing += OnClosing;

            GridTargetingManager.GetGridTargeting(SorterWep.CubeGrid).AddWeapon(this);
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                Settings = CreateSettings();

                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

                if (Definition.Assignments.HasMuzzleSubpart)
                {
                    var muzzleSubpart = SubpartManager.RecursiveGetSubpart(SorterWep, Definition.Assignments.MuzzleSubpart);
                    ((IMyEntity)muzzleSubpart)?.Model?.GetDummies(MuzzleDummies);
                }
                else
                {
                    SorterWep.Model?.GetDummies(MuzzleDummies);
                }

                SorterWep.SlimBlock.BlockGeneralDamageModifier = Definition?.Assignments.DurabilityModifier ?? 1f;

                SorterWep.ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, Definition?.Hardpoint.IdlePower ?? 0f);

                LoadSettings();
                SaveSettings();

                Settings.Sync();
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
                if (MarkedForClose || SorterWep == null || !SorterWep.IsWorking)
                    return;

                MuzzleMatrix = CalcMuzzleMatrix(0); // Set stored MuzzleMatrix

                if (!SorterWep.IsWorking) // Don't try shoot if the turret is disabled
                    return;
                Magazine.UpdateReload();
                TryShoot();
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }
        }

        public void OnClosing(IMyEntity entity)
        {
            GridTargetingManager.TryGetGridTargeting(SorterWep.CubeGrid)?.RemoveWeapon(this);
            WeaponManager.RemoveWeapon(Id);
            MarkedForClose = true;
            SorterWep.OnClosing -= OnClosing;
        }

        public virtual MatrixD CalcMuzzleMatrix(int id, bool local = false)
        {
            if (Definition.Assignments.Muzzles.Length == 0 || !MuzzleDummies.ContainsKey(Definition.Assignments.Muzzles[id]))
                return SorterWep.WorldMatrix;

            MatrixD dummyMatrix = MuzzleDummies[Definition.Assignments.Muzzles[id]].Matrix; // Dummy's local matrix
            if (local)
                return dummyMatrix;

            MatrixD worldMatrix = SorterWep.WorldMatrix; // Block's world matrix

            // Combine the matrices by multiplying them to get the transformation of the dummy in world space

            return dummyMatrix * worldMatrix;

            // Now combinedMatrix.Translation is the muzzle position in world coordinates,
            // and combinedMatrix.Forward is the forward direction in world coordinates.
        }

        float lastShoot = 0;
        public int NextMuzzleIdx = 0; // For alternate firing
        public float delayCounter = 0f; // TODO sync
        /// <summary>
        /// Automatic firing that ignores settings.
        /// </summary>
        public bool AutoShoot = false;

        public virtual void TryShoot()
        {
            // TODO: Clean up the logic on this
            // TODO: Re-introduce random values; they don't need to be synced because the client isn't real.

            float modifiedRateOfFire = Definition.Loading.RateOfFire;

            if (lastShoot < 60)
                lastShoot += modifiedRateOfFire; // Use the modified rate of fire

            // Manage fire delay. If there is an easier way to do this, TODO implement
            if ((Settings.ShootState || AutoShoot) && delayCounter > 0)
            {
                delayCounter -= 1 / 60f;
            }
            else if (!(Settings.ShootState || AutoShoot) && delayCounter <= 0 && Definition.Loading.DelayUntilFire > 0) // Check for the initial delay only if not already applied
            {
                delayCounter = Definition.Loading.DelayUntilFire;
            }

            if ((Settings.ShootState || AutoShoot) && // Is allowed to shoot
                lastShoot >= 60 &&           // Fire rate is ready
                Magazine.IsLoaded &&         // Magazine is loaded
                delayCounter <= 0)
            {
                while (lastShoot >= 60 && Magazine.IsLoaded) // Allows for firerates higher than 60 rps
                {
                    FireOnce();
                }
            }
        }

        /// <summary>
        /// Fires a single shot with no safety checks.
        /// </summary>
        internal virtual void FireOnce()
        {
            ProjectileDefinitionBase ammoDef =
                DefinitionManager.ProjectileDefinitions[Definition.Loading.Ammos[Magazine.SelectedAmmoIndex]];

            for (int i = 0; i < Definition.Loading.BarrelsPerShot; i++)
            {
                NextMuzzleIdx++;
                NextMuzzleIdx %= Definition.Assignments.Muzzles.Length;

                var muzzleMatrix = CalcMuzzleMatrix(NextMuzzleIdx);
                var muzzlePos = muzzleMatrix.Translation;

                for (int j = 0; j < Definition.Loading.ProjectilesPerBarrel; j++)
                {
                    SorterWep.CubeGrid.Physics?.ApplyImpulse(muzzleMatrix.Backward * ammoDef.UngroupedDef.Recoil, muzzleMatrix.Translation);
                    var newProjectile = ProjectileManager.SpawnProjectile(ammoDef, muzzlePos, muzzleMatrix.Forward, SorterWep) as PhysicalProjectile;
                    
                    if (this is SorterSmartLogic && newProjectile?.Guidance != null) // Assign target for self-guided projectiles
                    {
                        newProjectile.Guidance.SetTarget(((SorterSmartLogic) this).Targeting.Target);
                    }
                }

                lastShoot -= 60f;
                if (lastShoot < 60)
                    break;
            }

            Magazine.UseShot();
        }


        #region Settings Saving

        void SaveSettings()
        {
            if (SorterWep == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; Test log 1");

            if (SorterWep.Storage == null)
                SorterWep.Storage = new MyModStorageComponent();

            SorterWep.Storage.SetValue(HeartData.I.HeartSettingsGUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
        }

        internal virtual void LoadDefaultSettings()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Settings.LockedNetworking = true;
            Settings.ShootState = false;
            Settings.MouseShootState = false;
            Settings.AmmoLoadedIdx = 0;
            Settings.HudBarrelIndicatorState = false;
            Settings.LockedNetworking = false;
        }

        internal bool LoadSettings()
        {
            if (SorterWep.Storage == null)
            {
                LoadDefaultSettings();
                return false;
            }


            string rawData;
            if (!SorterWep.Storage.TryGetValue(HeartData.I.HeartSettingsGUID, out rawData))
            {
                LoadDefaultSettings();
                return false;
            }

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<WeaponSettings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null && loadedSettings.GetType() == Settings.GetType())
                {
                    Settings = loadedSettings;
                    Settings.WeaponId = Id;

                    // Handling for when ammo definition is changed
                    if (Settings.AmmoLoadedIdx >= Definition.Loading.Ammos.Length)
                        Settings.AmmoLoadedIdx = 0;

                    return true;
                }
            }
            catch (Exception e)
            {
                SoftHandle.RaiseException(e, typeof(SorterWeaponLogic));
            }

            return false;
        }

        public override bool IsSerialized()
        {
            try
            {
                SaveSettings();
                //MyAPIGateway.Utilities.ShowNotification("AAAHH I'M SERIALIZING AAAHHHHH", 2000, "Red");
            }
            catch (Exception e)
            {
                //should probably log this tbqh
            }

            return base.IsSerialized();
        }

        #endregion
    }
}
