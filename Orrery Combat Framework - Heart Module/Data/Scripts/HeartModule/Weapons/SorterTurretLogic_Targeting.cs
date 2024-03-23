using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    partial class SorterTurretLogic
    {
        public float TargetAge = 0;
        public IMyEntity TargetEntity { get; private set; } = null;
        public Projectile TargetProjectile { get; private set; } = null;

        public void UpdateTargeting()
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
                ResetTargetingState();

            if (!HasValidTarget())
            {
                TargetProjectile = null;
                TargetEntity = null;
                ResetTargetingState();
            }

            UpdateAzimuthElevation(AimPoint);

            TargetAge += 1 / 60f;
        }

        public void SetTarget(object target)
        {
            TargetEntity = null;
            TargetProjectile = null;
            TargetAge = 0;

            if (target is IMyEntity)
                TargetEntity = (IMyEntity)target;
            else if (target is Projectile)
                TargetProjectile = (Projectile)target;
        }

        public bool HasValidTarget()
        {
            return (TargetEntity != null || (TargetProjectile != null && !TargetProjectile.QueuedDispose)) // Is target not null?
                && IsTargetInRange && // Is target in range?
                (Definition.Targeting.RetargetTime == 0 ||
                TargetAge > Definition.Targeting.RetargetTime);
        }

        private void ResetTargetingState()
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
            if (!TargetProjectilesState || targetProjectile == null || targetProjectile.Firer == SorterWep)
                return false;

            if (Definition.Targeting.RetargetTime != 0 && !HasValidTarget() && targetProjectile == TargetProjectile)
                return false;

            MyRelationsBetweenPlayerAndBlock relations = MyRelationsBetweenPlayerAndBlock.NoOwnership;

            IMyEntity entity = targetProjectile.Firer;
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
