using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Orrery.HeartModule.Shared.Targeting;
using Orrery.HeartModule.Shared.Utility;
using Orrery.HeartModule.Shared.Weapons.Settings;
using Sandbox.ModAPI;
using System;
using VRage.Game.Entity;
using VRageMath;

namespace Orrery.HeartModule.Client.Weapons
{
    internal class SorterTurretLogic : SorterSmartLogic
    {
        public new TurretSettings Settings => (TurretSettings)base.Settings;
        internal override WeaponSettings CreateSettings() => new TurretSettings(SorterWep.EntityId);

        public float Azimuth { get; internal set; } = 0;
        public float Elevation { get; internal set; } = 0;
        public Vector2D DesiredAngle = Vector2D.Zero;
        public Vector2D HomeAngle = Vector2D.Zero;

        public SorterTurretLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {
            HomeAngle = new Vector2D(Definition.Hardpoint.HomeAzimuth, Definition.Hardpoint.HomeElevation);
        }

        public override void UpdateAfterSimulation()
        {
            if (!SorterWep.IsWorking)
                return;
            base.UpdateAfterSimulation();

            DesiredAngle = GetAngleToTarget(GetTargetPosition(Target));
            UpdateTurretSubparts();
        }

        #region Subparts

        internal Vector3D? GetTargetPosition(ITargetable target)
        {
            if (target == null)
                return null;

            var owner = SorterWep.CubeGrid;
            var ownerCenter = owner.Physics.CenterOfMassWorld;
            var inheritedVelocity = owner.Physics.LinearVelocity +
                                    owner.Physics.AngularVelocity.Cross(MuzzleMatrix.Translation - ownerCenter);
            return TargetingUtils.InterceptionPoint(MuzzleMatrix.Translation, inheritedVelocity, target, CurrentAmmo); // TODO block targeting
        }

        /// <summary>
        /// Returns the angle needed to reach a target.
        /// </summary>
        /// <returns></returns>
        private Vector2D GetAngleToTarget(Vector3D? targetPos)
        {
            if (targetPos == null)
                return HomeAngle;

            Vector3D vecFromTarget = MuzzleMatrix.Translation - targetPos.Value;

            vecFromTarget = Vector3D.Rotate(vecFromTarget.Normalized(), MatrixD.Invert(SorterWep.WorldMatrix));

            double desiredAzimuth = Math.Atan2(vecFromTarget.X, vecFromTarget.Z);
            if (double.IsNaN(desiredAzimuth))
                desiredAzimuth = Math.PI;

            double desiredElevation = Math.Asin(-vecFromTarget.Y);
            if (double.IsNaN(desiredElevation))
                desiredElevation = Math.PI;

            return new Vector2D(desiredAzimuth, desiredElevation);
        }

        public void UpdateTurretSubparts(float delta = 1/60f)
        {
            if (!Definition.Hardpoint.ControlRotation)
                return;
            if (Azimuth == DesiredAngle.X && Elevation == DesiredAngle.Y) // Don't move if you're already there
                return;


            MyEntitySubpart azimuth = SubpartManager.GetSubpart(SorterWep, Definition.Assignments.AzimuthSubpart);
            if (azimuth == null)
            {
                SoftHandle.RaiseException($"Azimuth subpart null on \"{SorterWep?.CustomName}\"");
                return;
            }
            MyEntitySubpart elevation = SubpartManager.GetSubpart(azimuth, Definition.Assignments.ElevationSubpart);
            if (elevation == null)
            {
                SoftHandle.RaiseException($"Elevation subpart null on \"{SorterWep?.CustomName}\"");
                return;
            }

            SubpartManager.LocalRotateSubpartAbs(azimuth, GetAzimuthMatrix(delta));
            SubpartManager.LocalRotateSubpartAbs(elevation, GetElevationMatrix(delta));
        }

        private Matrix GetAzimuthMatrix(float delta)
        {
            var _limitedAzimuth = MathUtils.LimitRotationSpeed(Azimuth, DesiredAngle.X, Definition.Hardpoint.AzimuthRate * delta);

            if (!Definition.Hardpoint.CanRotateFull)
                Azimuth = (float)MathUtils.Clamp(_limitedAzimuth, Definition.Hardpoint.MinAzimuth, Definition.Hardpoint.MaxAzimuth); // Basic angle clamp
            else
                Azimuth = (float)MathUtils.NormalizeAngle(_limitedAzimuth); // Adjust rotation to (-180, 180), but don't have any limits
            return Matrix.CreateFromYawPitchRoll(Azimuth, 0, 0);
        }

        private Matrix GetElevationMatrix(float delta)
        {
            var _limitedElevation = MathUtils.LimitRotationSpeed(Elevation, DesiredAngle.Y, Definition.Hardpoint.ElevationRate * delta);

            if (!Definition.Hardpoint.CanElevateFull)
                Elevation = (float)MathUtils.Clamp(_limitedElevation, Definition.Hardpoint.MinElevation, Definition.Hardpoint.MaxElevation);
            else
                Elevation = (float)MathUtils.NormalizeAngle(_limitedElevation);
            return Matrix.CreateFromYawPitchRoll(0, Elevation, 0);
        }

        #endregion
    }
}
