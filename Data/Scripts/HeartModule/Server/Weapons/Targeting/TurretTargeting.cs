using Orrery.HeartModule.Shared.Utility;
using System;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Weapons.Targeting
{
    internal class TurretTargeting : IWeaponTargeting
    {
        public Vector3D? TargetPosition { get; private set; }
        public SorterWeaponLogic Weapon { get; private set; }
        public SorterTurretLogic Turret { get; private set; }

        public MyEntity Target { get; private set; }

        public TurretTargeting(SorterTurretLogic weapon)
        {
            Weapon = weapon;
            Turret = weapon;
            TargetPosition = null;
        }

        public void UpdateTargeting()
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

            Turret.DesiredAngle = GetAngleToTarget();
        }

        public void SetTarget(MyEntity target)
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
        private Vector2D GetAngleToTarget()
        {
            if (TargetPosition == null)
                return Turret.HomeAngle;

            Vector3D vecFromTarget = Turret.MuzzleMatrix.Translation - TargetPosition.Value;

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
        private bool CanAimAtTarget(out Vector2D neededAngle)
        {
            neededAngle = Vector2D.Zero;
            if (TargetPosition == null || Vector3D.DistanceSquared(Turret.MuzzleMatrix.Translation, TargetPosition.Value) > Turret.Settings.AiRange * Turret.Settings.AiRange) // Range check
                return false;

            neededAngle = GetAngleToTarget();
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
    }
}
