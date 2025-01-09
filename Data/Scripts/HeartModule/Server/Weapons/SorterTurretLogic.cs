using System;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using Orrery.HeartModule.Server.Weapons.Targeting;
using VRage.Game.Entity;
using VRageMath;
using Orrery.HeartModule.Shared.Utility;
using Orrery.HeartModule.Shared.Weapons.Settings;

namespace Orrery.HeartModule.Server.Weapons
{
    internal class SorterTurretLogic : SorterSmartLogic
    {
        public new TurretSettings Settings => (TurretSettings)base.Settings;

        public float Azimuth { get; internal set; } = 0;
        public float Elevation { get; internal set; } = 0;
        public Vector2D DesiredAngle = Vector2D.Zero;
        public Vector2D HomeAngle = Vector2D.Zero;

        public new TurretWeaponTargeting Targeting => (TurretWeaponTargeting)base.Targeting;

        internal override SmartWeaponTargeting CreateTargeting() => new TurretWeaponTargeting(this);
        internal override WeaponSettings CreateSettings() => new TurretSettings(SorterWep.EntityId);

        public SorterTurretLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {
            HomeAngle = new Vector2D(Definition.Hardpoint.HomeAzimuth, Definition.Hardpoint.HomeElevation);
        }

        internal override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();

            Settings.LockedNetworking = true;
            
            Settings.AiRange = Definition.Targeting.MaxTargetingRange;

            Settings.LockedNetworking = false;
        }

        public override void UpdateAfterSimulation()
        {
            if (!SorterWep.IsWorking) // Don't turn if the turret is disabled
                return;

            UpdateTurretSubparts();
            base.UpdateAfterSimulation();
        }

        public override void TryShoot()
        {
            AutoShoot = Definition.Targeting.CanAutoShoot && Targeting.IsTargetAligned && Targeting.IsInRange(Targeting.TargetPosition);
            base.TryShoot();
        }

        #region Subparts

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
