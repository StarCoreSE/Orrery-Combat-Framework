using Orrery.HeartModule.Shared.Utility;
using System;
using Orrery.HeartModule.Shared.Targeting;
using VRageMath;
using Orrery.HeartModule.Shared.Targeting.Generics;

namespace Orrery.HeartModule.Server.Weapons.Targeting
{
    internal class TurretWeaponTargeting : SmartWeaponTargeting
    {
        public new SorterTurretLogic Weapon => (SorterTurretLogic) base.Weapon;

        public TurretWeaponTargeting(SorterSmartLogic weapon) : base(weapon)
        {
        }

        public override void UpdateTargeting(double delta)
        {
            base.UpdateTargeting(delta);

            Weapon.DesiredAngle = GetAngleToTarget(TargetPosition);
        }

        public bool IsTargetAligned => TargetPosition != null &&
                                       Vector3D.Angle(Weapon.MuzzleMatrix.Forward,
                                           TargetPosition.Value - Weapon.MuzzleMatrix.Translation) <=
                                       Weapon.Definition.Targeting.AimTolerance;

        /// <summary>
        /// Returns the angle needed to reach a target.
        /// </summary>
        /// <returns></returns>
        private Vector2D GetAngleToTarget(Vector3D? targetPos)
        {
            if (targetPos == null)
                return Weapon.HomeAngle;

            Vector3D vecFromTarget = Weapon.MuzzleMatrix.Translation - targetPos.Value;

            vecFromTarget = Vector3D.Rotate(vecFromTarget.Normalized(), MatrixD.Invert(Weapon.SorterWep.WorldMatrix));

            double desiredAzimuth = Math.Atan2(vecFromTarget.X, vecFromTarget.Z);
            if (double.IsNaN(desiredAzimuth))
                desiredAzimuth = Math.PI;

            double desiredElevation = Math.Asin(-vecFromTarget.Y);
            if (double.IsNaN(desiredElevation))
                desiredElevation = Math.PI;

            return new Vector2D(desiredAzimuth, desiredElevation);
        }

        /// <summary>
        /// Determines if a target position is within the Weapon's aiming bounds.
        /// </summary>
        /// <param name="targetPos"></param>
        /// <returns></returns>
        internal bool CanAimAt(Vector3D? targetPos)
        {
            // Range check is performed in base.IsSelectionTargetable().

            var neededAngle = GetAngleToTarget(targetPos);
            neededAngle.X = MathUtils.NormalizeAngle(neededAngle.X - Math.PI);
            neededAngle.Y = -MathUtils.NormalizeAngle(neededAngle.Y, Math.PI / 2);

            bool canAimAzimuth = Weapon.Definition.Hardpoint.CanRotateFull;

            if (!canAimAzimuth && !(neededAngle.X < Weapon.Definition.Hardpoint.MaxAzimuth && neededAngle.X > Weapon.Definition.Hardpoint.MinAzimuth))
                return false; // Check azimuth constrainst

            bool canAimElevation = Weapon.Definition.Hardpoint.CanElevateFull;

            if (!canAimElevation && !(neededAngle.Y < Weapon.Definition.Hardpoint.MaxElevation && neededAngle.Y > Weapon.Definition.Hardpoint.MinElevation))
                return false; // Check elevation constraints

            return true;
        }

        internal override Vector3D? GetTargetPosition(ITargetable target)
        {
            if (target == null)
                return null;

            var owner = Weapon.SorterWep.CubeGrid;
            var ownerCenter = owner.Physics.CenterOfMassWorld;
            var inheritedVelocity = owner.Physics.LinearVelocity +
                                    owner.Physics.AngularVelocity.Cross(Weapon.MuzzleMatrix.Translation - ownerCenter);
            return TargetingUtils.InterceptionPoint(Weapon.MuzzleMatrix.Translation, inheritedVelocity, target, Weapon.Magazine.CurrentAmmo); // TODO block targeting
        }

        internal override bool IsSelectionTargetable(ITargetable target)
        {
            return base.IsSelectionTargetable(target) && CanAimAt(GetTargetPosition(Target));
        }

        internal override bool IsInRange(Vector3D? position)
        {
            if (position == null)
                return false;

            double rangeSq = Vector3D.DistanceSquared(Weapon.MuzzleMatrix.Translation, position.Value);
            return rangeSq < Weapon.Settings.AiRange * Weapon.Settings.AiRange &&
                   rangeSq > Weapon.Definition.Targeting.MinTargetingRange * Weapon.Definition.Targeting.MinTargetingRange;
        }
    }
}
