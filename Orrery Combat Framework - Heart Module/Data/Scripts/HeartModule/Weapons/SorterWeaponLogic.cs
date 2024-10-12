using Heart_Module.Data.Scripts.HeartModule;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.ResourceSystem;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting;
using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding
{
    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "TestWeapon")]
    public partial class SorterWeaponLogic : MyGameLogicComponent, IMyEventProxy
    {
        internal IMyConveyorSorter SorterWep;
        internal WeaponDefinitionBase Definition;
        public readonly Guid HeartSettingsGUID = new Guid("06edc546-3e42-41f3-bc72-1d640035fbf2");
        public const int HeartSettingsUpdateCount = 60 * 1 / 10;

        public Heart_Settings Settings = new Heart_Settings();

        public WeaponLogic_Magazines Magazines;

        public Dictionary<string, IMyModelDummy> MuzzleDummies { get; set; } = new Dictionary<string, IMyModelDummy>();
        public SubpartManager SubpartManager = new SubpartManager();
        public MatrixD MuzzleMatrix { get; internal set; } = MatrixD.Identity;
        public bool HasLoS = false;
        public readonly uint Id = uint.MaxValue;
        private WeaponResourceSystem _resourceSystem;

        public SorterWeaponLogic(IMyConveyorSorter sorterWeapon, WeaponDefinitionBase definition, uint id)
        {
            HeartLog.Log($"SorterWeaponLogic: Constructing for sorter {sorterWeapon?.EntityId}, definition {definition?.Assignments.BlockSubtype}, id {id}");

            if (sorterWeapon == null)
            {
                HeartLog.Log("SorterWeaponLogic: sorterWeapon is null");
                return;
            }

            if (definition == null)
            {
                HeartLog.Log("SorterWeaponLogic: Definition is null");
                return;
            }

            sorterWeapon.GameLogic = this;
            Init(sorterWeapon.GetObjectBuilder());
            this.Definition = definition;
            Func<IMyInventory> getInventoryFunc = () =>
            {
                if (sorterWeapon.MarkedForClose)
                {
                    HeartLog.Log($"SorterWeaponLogic: Weapon {sorterWeapon.EntityId} is marked for close, returning null inventory");
                    return null;
                }
                return sorterWeapon.GetInventory();
            };
            Magazines = new WeaponLogic_Magazines(this, getInventoryFunc, AmmoComboBox);
            _resourceSystem = new WeaponResourceSystem(definition, this);
            Id = id;

            HeartLog.Log($"SorterWeaponLogic: Constructed for sorter {sorterWeapon.EntityId}");
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        #region Event Handlers


        #endregion

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                HeartLog.Log($"UpdateOnceBeforeFrame: Starting for weapon {Entity?.EntityId}");

                if (Entity == null)
                {
                    HeartLog.Log("UpdateOnceBeforeFrame: Entity is null, skipping");
                    return;
                }

                SorterWep = Entity as IMyConveyorSorter;
                if (SorterWep == null)
                {
                    HeartLog.Log("UpdateOnceBeforeFrame: SorterWep is null, skipping");
                    return;
                }

                HeartLog.Log($"UpdateOnceBeforeFrame: Setting WeaponEntityId to {SorterWep.EntityId}");
                Settings.WeaponEntityId = SorterWep.EntityId;

                if (SorterWep.CubeGrid == null)
                {
                    HeartLog.Log("UpdateOnceBeforeFrame: CubeGrid is null, skipping");
                    return;
                }

                if (SorterWep.CubeGrid.Physics == null)
                {
                    HeartLog.Log("UpdateOnceBeforeFrame: Grid Physics is null, skipping");
                    return;
                }

                HeartLog.Log("UpdateOnceBeforeFrame: Setting NeedsUpdate");
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

                HeartLog.Log("UpdateOnceBeforeFrame: Initializing MuzzleDummies");
                if (Definition == null)
                {
                    HeartLog.Log("UpdateOnceBeforeFrame: Definition is null, skipping MuzzleDummies initialization");
                }
                else if (Definition.Assignments.HasMuzzleSubpart)
                {
                    var muzzleSubpart = SubpartManager.RecursiveGetSubpart(SorterWep, Definition.Assignments.MuzzleSubpart);
                    if (muzzleSubpart != null)
                    {
                        HeartLog.Log("UpdateOnceBeforeFrame: Muzzle subpart found, getting dummies");
                        ((IMyEntity)muzzleSubpart).Model?.GetDummies(MuzzleDummies);
                    }
                    else
                    {
                        HeartLog.Log("UpdateOnceBeforeFrame: Muzzle subpart not found");
                    }
                }
                else
                {
                    HeartLog.Log("UpdateOnceBeforeFrame: Getting dummies from SorterWep model");
                    SorterWep.Model?.GetDummies(MuzzleDummies);
                }

                HeartLog.Log("UpdateOnceBeforeFrame: Setting block damage modifier");
                SorterWep.SlimBlock.BlockGeneralDamageModifier = Definition?.Assignments.DurabilityModifier ?? 1f;

                HeartLog.Log("UpdateOnceBeforeFrame: Setting required power input");
                SorterWep.ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, Definition?.Hardpoint.IdlePower ?? 0f);

                HeartLog.Log("UpdateOnceBeforeFrame: Loading settings");
                LoadSettings();

                HeartLog.Log("UpdateOnceBeforeFrame: Getting or creating GridAiTargeting");
                var gridAiTargeting = WeaponManagerAi.I?.GetOrCreateGridAiTargeting(SorterWep.CubeGrid);
                if (gridAiTargeting != null)
                {
                    gridAiTargeting.EnableGridAiIfNeeded();
                }
                else
                {
                    HeartLog.Log("UpdateOnceBeforeFrame: Failed to get or create GridAiTargeting");
                }

                HeartLog.Log("UpdateOnceBeforeFrame: Saving settings");
                SaveSettings();

                HeartLog.Log("UpdateOnceBeforeFrame: Completed successfully");
            }
            catch (Exception ex)
            {
                HeartLog.Log($"UpdateOnceBeforeFrame: Exception occurred: {ex.Message}");
                HeartLog.Log($"UpdateOnceBeforeFrame: Stack trace: {ex.StackTrace}");
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MarkedForClose || Id == uint.MaxValue || SorterWep == null)
                {
                    HeartLog.Log($"UpdateAfterSimulation: Skipping update for weapon {Id}");
                    return;
                }

                MuzzleMatrix = CalcMuzzleMatrix(0); // Set stored MuzzleMatrix
                Magazines.UpdateReload();
                HasLoS = HasLineOfSight();

                if (!SorterWep.IsWorking) // Don't try shoot if the turret is disabled
                    return;
                TryShoot();
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }
        }

        const float GridCheckRange = 200;
        /// <summary>
        /// Checks if the turret would intersect the grid.
        /// </summary>
        /// <returns></returns>
        private bool HasLineOfSight()
        {
            if (!Definition.Hardpoint.LineOfSightCheck) // Ignore if LoS check is disabled
                return true;

            if (SorterWep == null || SorterWep.CubeGrid == null)
            {
                HeartLog.Log($"HasLineOfSight: SorterWep or CubeGrid is null for weapon {Id}");
                return false;
            }

            List<Vector3I> intersects = new List<Vector3I>();
            SorterWep.CubeGrid.RayCastCells(MuzzleMatrix.Translation, MuzzleMatrix.Translation + MuzzleMatrix.Forward * GridCheckRange, intersects);

            foreach (var intersect in intersects)
                if (SorterWep.CubeGrid.CubeExists(intersect) && SorterWep.CubeGrid.GetCubeBlock(intersect) != SorterWep.SlimBlock)
                    return false;
            return true;
        }

        float lastShoot = 0;
        internal bool AutoShoot = false;
        public int NextMuzzleIdx = 0; // For alternate firing
        public float delayCounter = 0f;
        private readonly Random random = new Random();

        public virtual void TryShoot()
        {
            if (SorterWep == null)
            {
                HeartLog.Log($"TryShoot: SorterWep is null for weapon {Id}");
                return;
            }

            float modifiedRateOfFire = Definition.Loading.RateOfFire;

            // Only apply variance if RateOfFireVariance is not zero
            if (Definition.Loading.RateOfFireVariance != 0)
            {
                modifiedRateOfFire += (float)((random.NextDouble() * 2 - 1) * Definition.Loading.RateOfFireVariance);
            }

            if (lastShoot < 60)
                lastShoot += modifiedRateOfFire; // Use the modified rate of fire

            // Manage fire delay. If there is an easier way to do this, TODO implement
            if ((ShootState || AutoShoot) && Magazines.IsLoaded && delayCounter > 0)
            {
                if (delayCounter == Definition.Loading.DelayUntilFire && !string.IsNullOrEmpty(Definition.Audio.ShootSound))
                    MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.PreShootSound, SorterWep.GetPosition());
                delayCounter -= 1 / 60f;
            }
            else if (!((ShootState || AutoShoot) && Magazines.IsLoaded) && delayCounter <= 0 && Definition.Loading.DelayUntilFire > 0) // Check for the initial delay only if not already applied
            {
                delayCounter = Definition.Loading.DelayUntilFire;
            }

            if ((ShootState || AutoShoot) &&          // Is allowed to shoot
                Magazines.IsLoaded &&                       // Is mag loaded
                lastShoot >= 60 &&                          // Fire rate is ready
                delayCounter <= 0 &&
                HasLoS)                                   // Has line of sight
            {
                if (Magazines.SelectedAmmoId == -1)
                {
                    SoftHandle.RaiseSyncException($"Invalid ammo type on weapon! Subtype: {SorterWep.BlockDefinition.SubtypeId} | AmmoId: {Magazines.SelectedAmmoId}");
                    return;
                }

                // Check if the weapon has a resource system and there are enough resources for at least one shot
                if (_resourceSystem != null && _resourceSystem.CanShoot())
                {
                    // Retrieve the AccuracyVarianceMultiplier for the selected ammo
                    float accuracyVarianceMultiplier = ProjectileDefinitionManager.GetDefinition(Magazines.SelectedAmmoId).PhysicalProjectile.AccuracyVarianceMultiplier;
                    // Calculate the effective inaccuracy by applying the multiplier, default to 1 if multiplier is 0 to avoid change
                    float effectiveInaccuracy = Definition.Hardpoint.ShotInaccuracy * (accuracyVarianceMultiplier != 0 ? accuracyVarianceMultiplier : 1);

                    while (lastShoot >= 60 && Magazines.ShotsInMag > 0) // Allows for firerates higher than 60 rps
                    {
                        ProjectileDefinitionBase ammoDef = ProjectileDefinitionManager.GetDefinition(Magazines.SelectedAmmoId);
                        for (int i = 0; i < Definition.Loading.BarrelsPerShot; i++)
                        {
                            NextMuzzleIdx++;
                            NextMuzzleIdx %= Definition.Assignments.Muzzles.Length;

                            MatrixD muzzleMatrix = CalcMuzzleMatrix(NextMuzzleIdx);
                            Vector3D muzzlePos = muzzleMatrix.Translation;

                            for (int j = 0; j < Definition.Loading.ProjectilesPerBarrel; j++)
                            {
                                if (MyAPIGateway.Session.IsServer)
                                {
                                    SorterWep.CubeGrid.Physics?.ApplyImpulse(muzzleMatrix.Backward * ammoDef.Ungrouped.Recoil, muzzleMatrix.Translation);
                                    // Use the effectiveInaccuracy instead of the original ShotInaccuracy
                                    // Don't sync hitscan projectiles!
                                    Projectile newProjectile = ProjectileManager.I.AddProjectile(Magazines.SelectedAmmoId, muzzlePos, RandomCone(muzzleMatrix.Forward, effectiveInaccuracy), SorterWep, !ammoDef.PhysicalProjectile.IsHitscan);

                                    if (newProjectile == null) // Emergency failsafe
                                        return;

                                    if (newProjectile.Guidance != null) // Assign target for self-guided projectiles
                                    {
                                        if (this is SorterTurretLogic)
                                            newProjectile.Guidance.SetTarget(((SorterTurretLogic)this).TargetEntity);
                                        else
                                            newProjectile.Guidance.SetTarget(WeaponManagerAi.I.GetTargeting(SorterWep.CubeGrid)?.PrimaryGridTarget);
                                    }
                                }
                                else
                                {
                                    if (ammoDef.PhysicalProjectile.IsHitscan)
                                        DrawHitscanBeam(ammoDef);
                                }
                            }

                            lastShoot -= 60f;

                            // Not ideal (what if fire rate is insane?) but I don't care tbh
                            if (!string.IsNullOrEmpty(Definition.Audio.ShootSound))
                                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.ShootSound, muzzlePos);
                            MuzzleFlash();

                            Magazines.UseShot(MuzzleMatrix.Translation);

                            if (lastShoot < 60)
                                break;
                        }
                    }

                    // Consume resources after shooting
                    _resourceSystem.ConsumeResources();
                }
            }
        }

        /// <summary>
        /// Fakes a hitscan beam, to lower network load.
        /// </summary>
        /// <param name="beam"></param>
        private void DrawHitscanBeam(ProjectileDefinitionBase beam)
        {
            List<IHitInfo> intersects = new List<IHitInfo>();
            Vector3D pos = MuzzleMatrix.Translation;
            Vector3D end = MuzzleMatrix.Translation + MuzzleMatrix.Forward * beam.PhysicalProjectile.MaxTrajectory;
            MyAPIGateway.Physics.CastRay(pos, end, intersects);

            if (intersects.Count > 0)
            {
                Vector3D hitPos = intersects[0].Position;
                GlobalEffects.AddLine(pos, hitPos, beam.Visual.TrailFadeTime, beam.Visual.TrailWidth, beam.Visual.TrailColor, beam.Visual.TrailTexture);

                MatrixD matrix = MatrixD.CreateWorld(hitPos, (Vector3D)intersects[0].Normal, Vector3D.CalculatePerpendicularVector(intersects[0].Normal));
                MyParticleEffect hitEffect;
                if (MyParticlesManager.TryCreateParticleEffect(beam.Visual.ImpactParticle, ref matrix, ref hitPos, uint.MaxValue, out hitEffect))
                {
                    //MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                    //hitEffect.Velocity = av.Hit.HitVelocity;

                    if (hitEffect.Loop)
                        hitEffect.Stop();
                }
            }
            else
            {
                GlobalEffects.AddLine(pos, end, beam.Visual.TrailFadeTime, beam.Visual.TrailWidth, beam.Visual.TrailColor, beam.Visual.TrailTexture);
            }
        }

        public void MuzzleFlash(bool increment = false) // GROSS AND UGLY AND STUPID
        {
            if (Definition.Visuals.HasShootParticle && !HeartData.I.DegradedMode)
            {
                MatrixD localMuzzleMatrix = CalcMuzzleMatrix(NextMuzzleIdx, true);
                MatrixD muzzleMatrix = CalcMuzzleMatrix(NextMuzzleIdx);
                Vector3D muzzlePos = muzzleMatrix.Translation;

                MyParticleEffect hitEffect;
                if (MyParticlesManager.TryCreateParticleEffect(Definition.Visuals.ShootParticle, ref localMuzzleMatrix, ref muzzlePos, SorterWep.Render.GetRenderObjectID(), out hitEffect))
                {
                    //MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                    //hitEffect.Velocity = SorterWep.CubeGrid.LinearVelocity;

                    if (hitEffect.Loop)
                        hitEffect.Stop();
                }
            }
        }

        public virtual MatrixD CalcMuzzleMatrix(int id, bool local = false)
        {
            if (SorterWep == null)
            {
                HeartLog.Log($"CalcMuzzleMatrix: SorterWep is null for weapon {Id}");
                return MatrixD.Identity;
            }

            if (MuzzleDummies == null)
            {
                HeartLog.Log($"CalcMuzzleMatrix: MuzzleDummies is null for weapon {Id}");
                return SorterWep.WorldMatrix;
            }

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

        public void SetAmmo(int AmmoId)
        {
            Magazines.SelectedAmmoId = AmmoId;
            Settings.AmmoLoadedIdx = Magazines.SelectedAmmoIndex;
            HeartLog.Log("Ammo: " + ProjectileDefinitionManager.GetDefinition(Magazines.SelectedAmmoId).Name);
        }

        public void SetAmmoByIdx(int AmmoIdx)
        {
            if (AmmoIdx < 0 || AmmoIdx >= Definition.Loading.Ammos.Length)
                return;

            Magazines.SelectedAmmoIndex = AmmoIdx;
            Settings.AmmoLoadedIdx = Magazines.SelectedAmmoIndex;
        }

        #region Terminal controls

        public bool MouseShootState
        {
            get
            {
                return Settings.MouseShootState;
            }

            set
            {
                Settings.MouseShootState = value;
                Settings.Sync(SorterWep.GetPosition());
                ShootState = false;
            }
        }

        public bool ShootState
        {
            get
            {
                return Settings.ShootState;
            }

            set
            {
                Settings.ShootState = value;
                Settings.Sync(SorterWep.GetPosition());
            }
        }

        public int AmmoComboBox
        {
            get
            {
                return Settings.AmmoLoadedIdx;
            }

            set
            {
                SetAmmoByIdx(value);

                Settings.AmmoLoadedIdx = Magazines.SelectedAmmoIndex;
                Settings.Sync(SorterWep.GetPosition());
            }
        }

        public void CycleAmmoType(bool forward)
        {
            if (forward)
                Magazines.SelectedAmmoIndex = (Magazines.SelectedAmmoIndex + 1) % Definition.Loading.Ammos.Length;
            else
                Magazines.SelectedAmmoIndex = (Magazines.SelectedAmmoIndex - 1 + Definition.Loading.Ammos.Length) % Definition.Loading.Ammos.Length;

            Settings.AmmoLoadedIdx = Magazines.SelectedAmmoIndex;
            Magazines.EmptyMagazines();

            AmmoComboBox = Magazines.SelectedAmmoIndex;
        }

        public bool HudBarrelIndicatorState
        {
            get
            {
                return Settings.HudBarrelIndicatorState;
            }

            set
            {
                Settings.HudBarrelIndicatorState = value;
                Settings.Sync(SorterWep.GetPosition());
            }
        }

        #endregion

        #region Saving


        void SaveSettings()
        {
            if (SorterWep == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; Test log 1");

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; Test log 2");

            if (SorterWep.Storage == null)
                SorterWep.Storage = new MyModStorageComponent();

            SorterWep.Storage.SetValue(HeartSettingsGUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));

            //MyAPIGateway.Utilities.ShowNotification(SettingsBlockRange.ToString(), 1000, "Red");
        }

        internal virtual void LoadDefaultSettings()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Settings.ShootState = false;
            Settings.AmmoLoadedIdx = Magazines.SelectedAmmoIndex;
            Settings.HudBarrelIndicatorState = false;
        }

        internal virtual bool LoadSettings()
        {
            if (SorterWep.Storage == null)
            {
                LoadDefaultSettings();
                return false;
            }


            string rawData;
            if (!SorterWep.Storage.TryGetValue(HeartSettingsGUID, out rawData))
            {
                LoadDefaultSettings();
                return false;
            }

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<Heart_Settings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Settings.ShootState = loadedSettings.ShootState;

                    Settings.AmmoLoadedIdx = loadedSettings.AmmoLoadedIdx;
                    Magazines.SelectedAmmoIndex = loadedSettings.AmmoLoadedIdx;

                    Settings.ControlTypeState = loadedSettings.ControlTypeState;
                    Settings.HudBarrelIndicatorState = loadedSettings.HudBarrelIndicatorState;
                    Settings.WeaponEntityId = SorterWep.EntityId;

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


        internal Vector3D RandomCone(Vector3D center, double radius)
        {
            Vector3D Axis = Vector3D.CalculatePerpendicularVector(center).Rotate(center, Math.PI * 2 * HeartData.I.Random.NextDouble());

            return center.Rotate(Axis, radius * HeartData.I.Random.NextDouble());
        }
    }
}