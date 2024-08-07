﻿using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons.AiTargeting;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using Heart_Module.Data.Scripts.HeartModule.Weapons;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    partial class SorterTurretLogic
    {
        public float TargetAge = 0;
        public IMyEntity TargetEntity { get; private set; } = null;
        public Projectile TargetProjectile { get; private set; } = null;

        public void UpdateTurretTargeting()
        {
            MuzzleMatrix = CalcMuzzleMatrix(0); // Set stored MuzzleMatrix

            if (TargetProjectile != null)
            {
                AimPoint = TargetingHelper.InterceptionPoint(
                    MuzzleMatrix.Translation,
                    SorterWep.CubeGrid.LinearVelocity,
                    TargetProjectile, 0) ?? Vector3D.MaxValue;
                UpdateTargetState(AimPoint);
            }
            else if (TargetEntity != null)
            {
                AimPoint = TargetingHelper.InterceptionPoint(
                    MuzzleMatrix.Translation,
                    SorterWep.CubeGrid.LinearVelocity,
                    TargetEntity, 0) ?? Vector3D.MaxValue;
                UpdateTargetState(AimPoint);
            }
            else
            {
                List<object> potentialTargets = GetPotentialTargets();
                var prioritizedTargets = TargetPriority.GetPrioritizedTargets(potentialTargets, this);

                foreach (var target in prioritizedTargets)
                {
                    if (TargetPriority.ShouldConsiderTarget(target, this))
                    {
                        SetTarget(target);
                        var entityTarget = target as IMyEntity;
                        if (entityTarget != null)
                        {
                            AimPoint = TargetingHelper.InterceptionPoint(
                                MuzzleMatrix.Translation,
                                SorterWep.CubeGrid.LinearVelocity,
                                entityTarget, 0) ?? Vector3D.MaxValue;
                        }
                        else
                        {
                            var projectileTarget = target as Projectile;
                            if (projectileTarget != null)
                            {
                                AimPoint = TargetingHelper.InterceptionPoint(
                                    MuzzleMatrix.Translation,
                                    SorterWep.CubeGrid.LinearVelocity,
                                    projectileTarget, 0) ?? Vector3D.MaxValue;
                            }
                        }

                        UpdateTargetState(AimPoint);
                        break;
                    }
                }
            }

            if (!HasValidTarget())
            {
                TargetProjectile = null;
                TargetEntity = null;
                ResetTargetingState();
            }

            UpdateAzimuthElevation(AimPoint);

            TargetAge += 1 / 60f;
        }

        private List<object> GetPotentialTargets()
        {
            List<object> potentialTargets = new List<object>();
            var gridAiTargeting = WeaponManagerAi.I.GetTargeting(SorterWep.CubeGrid);
            if (gridAiTargeting != null)
            {
                potentialTargets.AddRange(gridAiTargeting.TargetedGrids.Keys);
                potentialTargets.AddRange(gridAiTargeting.TargetedCharacters.Keys);
                potentialTargets.AddRange(gridAiTargeting.TargetedProjectiles.Keys.Select(id => ProjectileManager.I.GetProjectile(id)));
            }
            return potentialTargets;
        }


        public void SetTarget(object target)
        {
            var entityTarget = target as IMyEntity;
            if (entityTarget != null && TargetEntity != entityTarget)
            {
                TargetEntity = entityTarget;
                //HeartLog.Log($"Turret '{this}' set to target entity '{entityTarget.DisplayName}'");
            }
            else
            {
                var projectileTarget = target as Projectile;
                if (projectileTarget != null && TargetProjectile != projectileTarget)
                {
                    TargetProjectile = projectileTarget;
                    //HeartLog.Log($"Turret '{this}' set to target projectile '{projectileTarget}'");
                }
            }
        }

        public void ResetTarget()
        {
            try
            {
                HeartLog.Log($"ResetTarget: Starting for turret {SorterWep?.EntityId}");

                if (SorterWep == null)
                {
                    HeartLog.Log("ResetTarget: SorterWep is null, skipping");
                    return;
                }

                TargetEntity = null;
                TargetProjectile = null;
                ResetTargetingState();

                var gridAiTargeting = WeaponManagerAi.I?.GetTargeting(SorterWep.CubeGrid);
                if (gridAiTargeting != null)
                {
                    gridAiTargeting.SetPrimaryTarget(null);
                }
                else
                {
                    HeartLog.Log("ResetTarget: gridAiTargeting is null");
                }

                Settings.ResetTargetState = true;
                Settings.Sync(SorterWep.GetPosition());

                HeartLog.Log("ResetTarget: Completed successfully");
            }
            catch (Exception ex)
            {
                HeartLog.Log($"ResetTarget: Exception occurred: {ex.Message}");
                HeartLog.Log($"ResetTarget: Stack trace: {ex.StackTrace}");
                SoftHandle.RaiseException(ex, typeof(SorterTurretLogic));
            }
        }

        public bool HasValidTarget()
        {
            return SorterWep.IsWorking &&
                   (TargetEntity != null || (TargetProjectile != null && !TargetProjectile.QueuedDispose)) // Is target not null?
                   && IsTargetInRange // Is target in range?
                   && (Definition.Targeting.RetargetTime == 0 || TargetAge > Definition.Targeting.RetargetTime);
        }

        public void ResetTargetingState()
        {
            //currentTarget = null;
            IsTargetAligned = false;
            IsTargetInRange = false;
            AutoShoot = false; // Disable automatic shooting
            AimPoint = Vector3D.MaxValue;
        }

        /// <summary>
        /// Sets state of target alignment and target range
        /// </summary>
        /// <param name="target"></param>
        /// <param name="aimPoint"></param>
        private void UpdateTargetState(Vector3D aimPoint)
        {
            double angle = Vector3D.Angle(MuzzleMatrix.Forward, (aimPoint - MuzzleMatrix.Translation).Normalized());
            IsTargetAligned = angle < Definition.Targeting.AimTolerance;

            double range = Vector3D.Distance(MuzzleMatrix.Translation, aimPoint);
            IsTargetInRange = range < Definition.Targeting.MaxTargetingRange && range > Definition.Targeting.MinTargetingRange;
        }

        public bool ShouldConsiderTarget(IMyCubeGrid targetGrid)
        {
            if (!TargetGridsState || targetGrid == null)
                return false;

            if (Definition.Targeting.RetargetTime != 0 && !HasValidTarget() && targetGrid == TargetEntity)
                return false;

            switch (targetGrid.GridSizeEnum) // Filter large/small grid
            {
                case MyCubeSize.Large:
                    if (!TargetLargeGridsState)
                        return false;
                    break;
                case MyCubeSize.Small:
                    if (!TargetSmallGridsState)
                        return false;
                    break;
            }

            if (!ShouldConsiderTarget(HeartUtils.GetRelationsBetweeenGrids(SorterWep.CubeGrid, targetGrid)))
                return false;

            Vector3D? intercept = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, SorterWep.CubeGrid.LinearVelocity, targetGrid, Magazines.SelectedAmmoId); // Check if it can even hit
            return intercept != null && CanAimAtTarget(intercept.Value); // All possible negatives have been filtered out
        }

        public bool ShouldConsiderTarget(IMyCharacter targetCharacter)
        {
            if (!TargetCharactersState || targetCharacter == null)
                return false;

            if (Definition.Targeting.RetargetTime != 0 && !HasValidTarget() && targetCharacter == TargetEntity)
                return false;

            if (!ShouldConsiderTarget(HeartUtils.GetRelationsBetweenGridAndPlayer(SorterWep.CubeGrid, targetCharacter.ControllerInfo?.ControllingIdentityId)))
                return false;

            Vector3D? intercept = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, SorterWep.CubeGrid.LinearVelocity, targetCharacter, Magazines.SelectedAmmoId); // Check if it can even hit
            return intercept != null && CanAimAtTarget(intercept.Value); // All possible negatives have been filtered out
        }

        public bool ShouldConsiderTarget(Projectile targetProjectile)
        {
            if (!TargetProjectilesState || targetProjectile == null || targetProjectile.Firer == SorterWep.EntityId)
                return false;

            if (Definition.Targeting.RetargetTime != 0 && !HasValidTarget() && targetProjectile == TargetProjectile)
                return false;

            MyRelationsBetweenPlayerAndBlock relations = MyRelationsBetweenPlayerAndBlock.NoOwnership;

            IMyEntity entity = MyAPIGateway.Entities.GetEntityById(targetProjectile.Firer);
            if (entity is IMyCharacter)
                relations = HeartUtils.GetRelationsBetweenGridAndPlayer(SorterWep.CubeGrid, ((IMyCharacter)entity).ControllerInfo?.ControllingIdentityId);
            else if (entity is IMyCubeBlock)
                relations = HeartUtils.GetRelationsBetweeenGrids(SorterWep.CubeGrid, ((IMyCubeBlock)entity).CubeGrid);

            //MyAPIGateway.Utilities.ShowNotification("" + relations, 1000 / 60);

            if (!ShouldConsiderTarget(relations))
                return false;

            Vector3D? intercept = TargetingHelper.InterceptionPoint(MuzzleMatrix.Translation, SorterWep.CubeGrid.LinearVelocity, targetProjectile, Magazines.SelectedAmmoId); // Check if it can even hit
            return intercept != null && CanAimAtTarget(intercept.Value); // All possible negatives have been filtered out
        }

        public bool ShouldConsiderTarget(MyRelationsBetweenPlayerAndBlock relations)
        {
            switch (relations) // Filter target relations
            {
                case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                    if (!TargetUnownedState)
                        return false;
                    break;
                case MyRelationsBetweenPlayerAndBlock.Owner:
                case MyRelationsBetweenPlayerAndBlock.Friends:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    if (!TargetFriendliesState)
                        return false;
                    break;
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    if (!TargetEnemiesState)
                        return false;
                    break;
                case MyRelationsBetweenPlayerAndBlock.Neutral:
                    if (!TargetNeutralsState)
                        return false;
                    break;
            }
            return true;
        }
    }
}
