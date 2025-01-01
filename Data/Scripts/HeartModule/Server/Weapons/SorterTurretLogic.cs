using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.WeaponSettings;
using Sandbox.ModAPI;
using Orrery.HeartModule.Server.Weapons.Targeting;
using VRage.Game.Entity;
using VRageMath;
using Orrery.HeartModule.Shared.Utility;

namespace Orrery.HeartModule.Server.Weapons
{
    internal class SorterTurretLogic : SorterWeaponLogic
    {
        public new TurretSettings Settings
        {
            get
            {
                return (TurretSettings)base.Settings;
            }
            set
            {
                base.Settings = value;
            }
        }

        public float Azimuth { get; internal set; } = 0;
        public float Elevation { get; internal set; } = 0;
        public Vector2D DesiredAngle = Vector2D.Zero;
        public Vector2D HomeAngle = Vector2D.Zero;
        public readonly TurretTargeting Targeting;

        public SorterTurretLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {
            Settings = new TurretSettings(sorterWep.EntityId);
            HomeAngle = new Vector2D(Definition.Hardpoint.HomeAzimuth, Definition.Hardpoint.HomeElevation);
            Targeting = new TurretTargeting(this);
        }

        internal override void LoadDefaultSettings()
        {
            base.LoadDefaultSettings();

            Settings.LockedNetworking = true;

            Settings.AiRange = Definition.Targeting.MaxTargetingRange;
            Settings.PreferUniqueTargetState = false;
            Settings.TargetGridsState = true;
            Settings.TargetSmallGridsState = true;
            Settings.TargetLargeGridsState = true;
            Settings.TargetCharactersState = true;
            Settings.TargetProjectilesState = true;
            Settings.TargetEnemiesState = true;
            Settings.TargetFriendliesState = false;
            Settings.TargetNeutralsState = false;
            Settings.TargetUnownedState = true;

            Settings.LockedNetworking = false;
        }

        public override void UpdateAfterSimulation()
        {
            if (!SorterWep.IsWorking) // Don't turn if the turret is disabled
                return;

            Targeting.SetTarget((MyEntity) MyAPIGateway.Session.Player.Character); // TODO remove
            Targeting.UpdateTargeting();
            UpdateTurretSubparts();
            base.UpdateAfterSimulation();
        }

        public override void TryShoot()
        {
            AutoShoot = Definition.Targeting.CanAutoShoot && Targeting.IsTargetAligned && Targeting.IsTargetInRange;
            base.TryShoot();
        }

        #region Subparts

        public override MatrixD CalcMuzzleMatrix(int id, bool local = false)
        {
            if (Definition.Assignments.Muzzles.Length == 0 || !MuzzleDummies.ContainsKey(Definition.Assignments.Muzzles[id]))
                return SorterWep.WorldMatrix;

            try
            {
                MyEntitySubpart azSubpart = SubpartManager.GetSubpart((MyEntity)SorterWep, Definition.Assignments.AzimuthSubpart);
                MyEntitySubpart evSubpart = SubpartManager.GetSubpart(azSubpart, Definition.Assignments.ElevationSubpart);

                MatrixD partMatrix = evSubpart.WorldMatrix;
                Matrix muzzleMatrix = MuzzleDummies[Definition.Assignments.Muzzles[id]].Matrix;

                if (local)
                {
                    return muzzleMatrix * evSubpart.PositionComp.LocalMatrixRef * azSubpart.PositionComp.LocalMatrixRef;
                }

                if (muzzleMatrix != null)
                    return muzzleMatrix * partMatrix;
            }
            catch { }
            return MatrixD.Identity;
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
