using Orrery.HeartModule.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Orrery.HeartModule.Server.GridTargeting;
using Orrery.HeartModule.Shared.Targeting;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Weapons.Targeting
{
    internal class TurretTargeting : IWeaponTargeting
    {
        public Vector3D? TargetPosition { get; private set; }
        public SorterWeaponLogic Weapon { get; private set; }
        public SorterTurretLogic Turret { get; private set; }
        public readonly GridTargeting.GridTargeting GridTargeting;

        public ITargetable Target { get; private set; }

        public TurretTargeting(SorterTurretLogic weapon)
        {
            Weapon = weapon;
            Turret = weapon;
            TargetPosition = null;
            GridTargeting = GridTargetingManager.GetGridTargeting(Turret.SorterWep.CubeGrid);
        }

        public void UpdateTargeting()
        {
            UpdateTargetPosition();
            var prevTarget = Target;
            if (TrySelectTarget())
            {
                GridTargeting.UpdateTurretTarget(prevTarget, false);
                GridTargeting.UpdateTurretTarget(Target, true);
                UpdateTargetPosition();
            }

            MyAPIGateway.Utilities.ShowNotification($"Target: {Target?.GetType().Name ?? "None"} {(Target as TargetableProjectile)?.Projectile.Id.ToString() ?? ""} {Target?.GetRelations(Turret.SorterWep)} {TargetPosition != null}", 1000/60);

            Turret.DesiredAngle = GetAngleToTarget(TargetPosition);
        }

        public void SetTarget(ITargetable target)
        {
            Target = target;
            UpdateTargeting();
        }

        public bool IsTargetAligned => TargetPosition != null &&
                                       Vector3D.Angle(Turret.MuzzleMatrix.Forward,
                                           TargetPosition.Value - Turret.MuzzleMatrix.Translation) <=
                                       Turret.Definition.Targeting.AimTolerance;
        public bool IsTargetInRange
        {
            get
            {
                if (TargetPosition == null)
                    return false;

                double rangeSq = Vector3D.DistanceSquared(Turret.MuzzleMatrix.Translation, TargetPosition.Value);
                return rangeSq < Turret.Settings.AiRange * Turret.Settings.AiRange &&
                       rangeSq > Turret.Definition.Targeting.MinTargetingRange * Turret.Definition.Targeting.MinTargetingRange;
            }
        }

        /// <summary>
        /// Returns the angle needed to reach a target.
        /// </summary>
        /// <returns></returns>
        private Vector2D GetAngleToTarget(Vector3D? targetPos)
        {
            if (targetPos == null)
                return Turret.HomeAngle;

            Vector3D vecFromTarget = Turret.MuzzleMatrix.Translation - targetPos.Value;

            vecFromTarget = Vector3D.Rotate(vecFromTarget.Normalized(), MatrixD.Invert(Turret.SorterWep.WorldMatrix));

            double desiredAzimuth = Math.Atan2(vecFromTarget.X, vecFromTarget.Z);
            if (desiredAzimuth == double.NaN)
                desiredAzimuth = Math.PI;

            double desiredElevation = Math.Asin(-vecFromTarget.Y);
            if (desiredElevation == double.NaN)
                desiredElevation = Math.PI;

            return new Vector2D(desiredAzimuth, desiredElevation);
        }

        /// <summary>
        /// Determines if a target position is within the turret's aiming bounds.
        /// </summary>
        /// <param name="neededAngle"></param>
        /// <returns></returns>
        private bool CanAimAtTarget(Vector3D? targetPos, out Vector2D neededAngle)
        {
            neededAngle = Vector2D.Zero;
            if (targetPos == null || Vector3D.DistanceSquared(Turret.MuzzleMatrix.Translation, targetPos.Value) > Turret.Settings.AiRange * Turret.Settings.AiRange) // Range check
                return false;

            neededAngle = GetAngleToTarget(targetPos);
            neededAngle.X = MathUtils.NormalizeAngle(neededAngle.X - Math.PI);
            neededAngle.Y = -MathUtils.NormalizeAngle(neededAngle.Y, Math.PI / 2);

            bool canAimAzimuth = Turret.Definition.Hardpoint.CanRotateFull;

            if (!canAimAzimuth && !(neededAngle.X < Turret.Definition.Hardpoint.MaxAzimuth && neededAngle.X > Turret.Definition.Hardpoint.MinAzimuth))
                return false; // Check azimuth constrainst

            bool canAimElevation = Turret.Definition.Hardpoint.CanElevateFull;

            if (!canAimElevation && !(neededAngle.Y < Turret.Definition.Hardpoint.MaxElevation && neededAngle.Y > Turret.Definition.Hardpoint.MinElevation))
                return false; // Check elevation constraints

            return true;
        }

        /// <summary>
        /// Selects a target from the grid's targeting list.
        /// </summary>
        /// <returns>True if the target was changed, false otherwise.</returns>
        private bool TrySelectTarget()
        {
            // Prefer original target.
            Vector2D discard;
            bool isPrevTargetable = IsSelectionTargetable(Target) && IsRelationTargetable(Target);
            if (!(Target?.IsClosed() ?? true) && CanAimAtTarget(TargetPosition, out discard) && isPrevTargetable)
                return false;

            var prevTarget = Target;
            Target = null;

            // Grids
            if (Turret.Settings.TargetGridsState)
            {
                // I could compact this down, yes, but that would take work. sorry.
                if (Turret.Settings.TargetLargeGridsState && Turret.Settings.TargetSmallGridsState)
                    Target = GetFirstTargetOfType(TargetingStateEnum.Grids); // Prioritize closer targets
                else if (Turret.Settings.TargetLargeGridsState)
                    Target = GetFirstTargetOfType(TargetingStateEnum.LargeGrids); // Prioritize closer targets
                else if (Turret.Settings.TargetSmallGridsState)
                    Target = GetFirstTargetOfType(TargetingStateEnum.SmallGrids); // Prioritize closer targets

                // If there aren't any valid targets in this section, check the next section.
                if (Target != null)
                    return true;
            }
            // Characters
            if (Turret.Settings.TargetCharactersState)
            {
                Target = GetFirstTargetOfType(TargetingStateEnum.Characters);

                if (Target != null)
                    return true;
            }
            // Projectiles
            if (Turret.Settings.TargetProjectilesState)
            {
                Target = GetFirstTargetOfType(TargetingStateEnum.Projectiles);

                if (Target != null)
                    return true;
            }

            // If no other valid targets exist, check if the current target is still valid.
            // If not, keep the target as null and return "target changed."
            if (prevTarget != null && (TargetPosition == null || prevTarget.IsClosed() || !isPrevTargetable ||
                Vector3D.DistanceSquared(TargetPosition.Value, Turret.MuzzleMatrix.Translation) >
                Turret.Settings.AiRange * Turret.Settings.AiRange))
                return true;

            // Keep trying to point at the old target if in range.
            Target = prevTarget;
            return false;
        }

        private void UpdateTargetPosition()
        {
            if (Target == null)
                TargetPosition = null;
            else
            {
                var owner = Turret.SorterWep.CubeGrid;
                var ownerCenter = owner.Physics.CenterOfMassWorld;
                var inheritedVelocity = owner.Physics.LinearVelocity +
                                        owner.Physics.AngularVelocity.Cross(Turret.MuzzleMatrix.Translation - ownerCenter);
                TargetPosition = TargetingUtils.InterceptionPoint(Turret.MuzzleMatrix.Translation, inheritedVelocity, Target, Turret.Magazine.CurrentAmmo); // TODO block targeting
            }
        }

        private ITargetable GetFirstTargetOfType(TargetingStateEnum type)
        {
            // There is VERY MUCH a better way of doing this, I am so sorry.

            // Look for target with the least number of locks.
            if (Turret.Settings.PreferUniqueTargetState)
            {
                int numLocks = int.MaxValue;
                ITargetable bestTarget = null;
                foreach (var target in GridTargeting.AvailableTargets[type])
                {
                    if (!IsRelationTargetable(target))
                        continue;
                    int checkLocks = 0;
                    if (!GridTargeting.TargetLocks.TryGetValue(target, out checkLocks) || checkLocks < numLocks)
                    {
                        numLocks = checkLocks;
                        bestTarget = target;
                    }
                    if (numLocks == 0)
                        break;
                }
                return bestTarget;
            }

            // Otherwise, look for the closest target.
            return GridTargeting.AvailableTargets[type].FirstOrDefault(IsRelationTargetable);
        }

        /// <summary>
        /// Checks if an entity's relationship to the grid is targetable by the turret.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool IsRelationTargetable(ITargetable entity)
        {
            if (entity == null || entity.IsClosed())
                return false;
            var relations = entity.GetRelations(Turret.SorterWep);

            switch (relations)
            {
                case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                    return Turret.Settings.TargetUnownedState;
                case MyRelationsBetweenPlayerAndBlock.Owner:
                case MyRelationsBetweenPlayerAndBlock.Friends:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    return Turret.Settings.TargetFriendliesState;
                case MyRelationsBetweenPlayerAndBlock.Neutral:
                    return Turret.Settings.TargetNeutralsState;
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    return Turret.Settings.TargetEnemiesState;
                default:
                    throw new Exception("Invalid relationship state!");
            }
        }

        /// <summary>
        /// Checks if an entity's type is targetable by the turret.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool IsSelectionTargetable(ITargetable target)
        {
            if (target is TargetableEntity)
            {
                var entity = ((TargetableEntity)target).Entity;

                var grid = entity as IMyCubeGrid;
                var character = entity as IMyCharacter;
                if (grid != null)
                {
                    if (grid.GridSizeEnum == MyCubeSize.Large)
                        return Turret.Settings.TargetGridsState && Turret.Settings.TargetLargeGridsState;
                    return Turret.Settings.TargetGridsState && Turret.Settings.TargetSmallGridsState;
                }
                else if (character != null)
                {
                    return Turret.Settings.TargetCharactersState;
                }
            }
            else if (target is TargetableProjectile)
            {
                return Turret.Settings.TargetProjectilesState;
            }
            

            return false;
        }
    }
}
