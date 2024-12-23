using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Shared.Utility;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Network;
using VRageMath;
using VRage.Library.Utils;

namespace Orrery.HeartModule.Server.Weapons
{
    internal class SorterWeaponLogic : MyGameLogicComponent, IMyEventProxy
    {
        public readonly IMyConveyorSorter SorterWep;
        public readonly WeaponDefinitionBase Definition;
        public readonly uint Id;

        internal Dictionary<string, IMyModelDummy> MuzzleDummies = new Dictionary<string, IMyModelDummy>();
        internal SubpartManager SubpartManager = new SubpartManager();
        internal MatrixD MuzzleMatrix = MatrixD.Identity;

        public SorterWeaponLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, uint id)
        {
            SorterWep = sorterWep;
            Definition = definition;
            Id = id;

            sorterWep.OnClose += ent => WeaponManager.RemoveWeapon(Id);

            sorterWep.GameLogic.Container.Add(this);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            try
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

                if (Definition.Assignments.HasMuzzleSubpart)
                {
                    var muzzleSubpart = SubpartManager.RecursiveGetSubpart(SorterWep, Definition.Assignments.MuzzleSubpart);
                    if (muzzleSubpart != null)
                        ((IMyEntity)muzzleSubpart).Model?.GetDummies(MuzzleDummies);
                }
                else
                {
                    SorterWep.Model?.GetDummies(MuzzleDummies);
                }

                SorterWep.SlimBlock.BlockGeneralDamageModifier = Definition?.Assignments.DurabilityModifier ?? 1f;

                SorterWep.ResourceSink.SetRequiredInputByType(MyResourceDistributorComponent.ElectricityId, Definition?.Hardpoint.IdlePower ?? 0f);
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
                if (MarkedForClose || Id == uint.MaxValue || SorterWep == null)
                    return;

                MuzzleMatrix = CalcMuzzleMatrix(0); // Set stored MuzzleMatrix

                if (!SorterWep.IsWorking) // Don't try shoot if the turret is disabled
                    return;
                TryShoot();
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }
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

        private bool AutoShoot = true;
        private bool ShootState = true;

        public virtual void TryShoot()
        {
            float modifiedRateOfFire = Definition.Loading.RateOfFire;

            if (lastShoot < 60)
                lastShoot += modifiedRateOfFire; // Use the modified rate of fire

            // Manage fire delay. If there is an easier way to do this, TODO implement
            if ((ShootState || AutoShoot) && delayCounter > 0)
            {
                delayCounter -= 1 / 60f;
            }
            else if (!(ShootState || AutoShoot) && delayCounter <= 0 && Definition.Loading.DelayUntilFire > 0) // Check for the initial delay only if not already applied
            {
                delayCounter = Definition.Loading.DelayUntilFire;
            }

            if ((ShootState || AutoShoot) &&          // Is allowed to shoot
                lastShoot >= 60 &&                          // Fire rate is ready
                delayCounter <= 0)
            {
                while (lastShoot >= 60) // Allows for firerates higher than 60 rps
                {
                    ProjectileDefinitionBase ammoDef =
                        DefinitionManager.ProjectileDefinitions[Definition.Loading.Ammos[0]];
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
                                SorterWep.CubeGrid.Physics?.ApplyImpulse(muzzleMatrix.Backward * ammoDef.UngroupedDef.Recoil, muzzleMatrix.Translation);
                                var newProjectile = ProjectileManager.SpawnProjectile(ammoDef, muzzlePos, muzzleMatrix.Forward, SorterWep);

                                //if (newProjectile == null) // Emergency failsafe
                                //    return;
                                //
                                //if (newProjectile.Guidance != null) // Assign target for self-guided projectiles
                                //{
                                //    if (this is SorterTurretLogic)
                                //        newProjectile.Guidance.SetTarget(((SorterTurretLogic)this).TargetEntity);
                                //    else
                                //        newProjectile.Guidance.SetTarget(WeaponManagerAi.I.GetTargeting(SorterWep.CubeGrid)?.PrimaryGridTarget);
                                //}
                            }
                        }

                        lastShoot -= 60f;
                        if (lastShoot < 60)
                            break;
                    }
                }
            }
        }
    }
}
