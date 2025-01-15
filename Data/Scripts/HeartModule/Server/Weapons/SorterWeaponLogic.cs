using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using Orrery.HeartModule.Server.GridTargeting;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Shared.Utility;
using Orrery.HeartModule.Shared.Weapons.Settings;
using VRage.ModAPI;
using VRageMath;
using Orrery.HeartModule.Shared.Weapons;

namespace Orrery.HeartModule.Server.Weapons
{
    /// <summary>
    /// Controls fixed weapons.
    /// </summary>
    internal class SorterWeaponLogic : SorterWeaponBase
    {
        internal MatrixD MuzzleMatrix = MatrixD.Identity;
        internal WeaponLogicMagazines Magazine;

        public SorterWeaponLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {
            Magazine = new WeaponLogicMagazines(this, null); // TODO GetInventoryFunc

            SorterWep.OnClosing += OnClosing;

            GridTargetingManager.GetGridTargeting(SorterWep.CubeGrid).AddWeapon(this);
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            try
            {
                Settings = CreateSettings();

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

                MuzzleMatrix = this.CalcMuzzleMatrix(0); // Set stored MuzzleMatrix

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

            float modifiedRateOfFire = Definition.Loading.RateOfFire;
            if (Definition.Loading.RateOfFireVariance > 0)
                modifiedRateOfFire += Definition.Loading.RateOfFire * (float) (Definition.Loading.RateOfFireVariance * (HeartData.I.Random.NextDouble() - 0.5) * 2);

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
                ServerNetwork.SendToEveryoneInSync(new SerializedPrefireEvent(this), SorterWep.Position);
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
            // Inaccuracy in radians. If the multiplier is negative, ignore.
            float inaccuracy = Definition.Hardpoint.ShotInaccuracy *
                             (ammoDef.PhysicalProjectileDef.AccuracyVarianceMultiplier < 0 ? 1 : ammoDef.PhysicalProjectileDef.AccuracyVarianceMultiplier);

            for (int i = 0; i < Definition.Loading.BarrelsPerShot; i++)
            {
                NextMuzzleIdx++;
                NextMuzzleIdx %= Definition.Assignments.Muzzles.Length;

                var muzzleMatrix = this.CalcMuzzleMatrix(NextMuzzleIdx);
                var muzzlePos = muzzleMatrix.Translation;

                for (int j = 0; j < Definition.Loading.ProjectilesPerBarrel; j++)
                {
                    SorterWep.CubeGrid.Physics?.ApplyImpulse(muzzleMatrix.Backward * ammoDef.UngroupedDef.Recoil, muzzleMatrix.Translation);
                    var direction = muzzleMatrix.Forward;
                    if (inaccuracy > 0)
                        direction = MathUtils.RandomCone(muzzleMatrix.Forward, inaccuracy);
                    var newProjectile = ProjectileManager.SpawnProjectile(ammoDef, muzzlePos, direction, SorterWep) as PhysicalProjectile;
                    
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

        internal void SaveSettings()
        {
            if (SorterWep == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; Test log 1");

            if (SorterWep.Storage == null)
                SorterWep.Storage = new MyModStorageComponent();

            SorterWep.Storage.SetValue(HeartData.I.HeartSettingsGUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
            HeartLog.Info("Save " + Settings.ToString());
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

                if (loadedSettings != null)
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

        #endregion
    }
}
