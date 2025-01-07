using Orrery.HeartModule.Server.GridTargeting;
using Orrery.HeartModule.Shared.Targeting;
using Orrery.HeartModule.Shared.Targeting.Generics;
using System;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game;
using VRageMath;

namespace Orrery.HeartModule.Server.Weapons.Targeting
{
    internal class SmartWeaponTargeting
    {
        public Vector3D? TargetPosition { get; internal set; }
        public readonly SorterSmartLogic Weapon;
        public ITargetable Target { get; internal set; }
        public readonly GridTargeting.GridTargeting GridTargeting;

        public SmartWeaponTargeting(SorterSmartLogic weapon)
        {
            Weapon = weapon;
            TargetPosition = null;
            GridTargeting = GridTargetingManager.GetGridTargeting(Weapon.SorterWep.CubeGrid);
        }

        public virtual void UpdateTargeting()
        {
            TargetPosition = GetTargetPosition(Target);
            var prevTarget = Target;
            if (TrySelectTarget())
            {
                GridTargeting.UpdateWeaponTarget(prevTarget, false);
                GridTargeting.UpdateWeaponTarget(Target, true);
                TargetPosition = GetTargetPosition(Target);
            }

            MyAPIGateway.Utilities.ShowNotification($"Target: {Target?.GetType().Name ?? "None"} {(Target as TargetableEntity)?.Entity.GetFriendlyName() ?? ""}{(Target as TargetableProjectile)?.Projectile.Id.ToString() ?? ""} {Target?.GetRelations(Weapon.SorterWep)} {TargetPosition != null}", 1000/60);
        }


        public void ForceSetTarget(ITargetable target)
        {
            Target = target;
            UpdateTargeting();
        }

        #region Target Interface

        /// <summary>
        /// Selects a target from the grid's targeting list.
        /// </summary>
        /// <returns>True if the target was changed, false otherwise.</returns>
        internal virtual bool TrySelectTarget()
        {
            // Prefer original target.
            bool isPrevTargetable = IsSelectionTargetable(Target) && IsRelationTargetable(Target);
            if (!(Target?.IsClosed ?? true) && isPrevTargetable)
                return false;

            var prevTarget = Target;
            Target = null;

            // Grids
            if (Weapon.Settings.TargetGridsState)
            {
                // I could compact this down, yes, but that would take work. sorry.
                if (Weapon.Settings.TargetLargeGridsState && Weapon.Settings.TargetSmallGridsState)
                    Target = GetFirstTargetOfType(TargetingStateEnum.Grids); // Prioritize closer targets
                else if (Weapon.Settings.TargetLargeGridsState)
                    Target = GetFirstTargetOfType(TargetingStateEnum.LargeGrids); // Prioritize closer targets
                else if (Weapon.Settings.TargetSmallGridsState)
                    Target = GetFirstTargetOfType(TargetingStateEnum.SmallGrids); // Prioritize closer targets

                // If there aren't any valid targets in this section, check the next section.
                if (Target != null)
                    return true;
            }
            // Characters
            if (Weapon.Settings.TargetCharactersState)
            {
                Target = GetFirstTargetOfType(TargetingStateEnum.Characters);

                if (Target != null)
                    return true;
            }
            // Projectiles
            if (Weapon.Settings.TargetProjectilesState)
            {
                Target = GetFirstTargetOfType(TargetingStateEnum.Projectiles);

                if (Target != null)
                    return true;
            }

            // If no other valid targets exist, check if the current target is still valid.
            // If not, keep the target as null and return "target changed."
            if (prevTarget != null && (TargetPosition == null || prevTarget.IsClosed || !isPrevTargetable))
                return true;

            // Keep trying to point at the old target if in range.
            Target = prevTarget;
            return false;
        }

        internal virtual Vector3D? GetTargetPosition(ITargetable target)
        {
            return target?.Position;
        }

        #endregion

        #region Target Selection

        internal virtual ITargetable GetFirstTargetOfType(TargetingStateEnum type)
        {
            // There is VERY MUCH a better way of doing this, I am so sorry.

            // Look for target with the least number of locks.
            if (Weapon.Settings.PreferUniqueTargetState)
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
        internal bool IsRelationTargetable(ITargetable entity)
        {
            if (entity == null || entity.IsClosed)
                return false;
            var relations = entity.GetRelations(Weapon.SorterWep);

            switch (relations)
            {
                case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                    return Weapon.Settings.TargetUnownedState;
                case MyRelationsBetweenPlayerAndBlock.Owner:
                case MyRelationsBetweenPlayerAndBlock.Friends:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    return Weapon.Settings.TargetFriendliesState;
                case MyRelationsBetweenPlayerAndBlock.Neutral:
                    return Weapon.Settings.TargetNeutralsState;
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    return Weapon.Settings.TargetEnemiesState;
                default:
                    throw new Exception("Invalid relationship state!");
            }
        }

        /// <summary>
        /// Checks if an entity's type is targetable by the turret.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal virtual bool IsSelectionTargetable(ITargetable target)
        {
            if (!IsInRange(GetTargetPosition(target)))
                return false;

            if (target is TargetableEntity)
            {
                var entity = ((TargetableEntity)target).Entity;

                var grid = entity as IMyCubeGrid;
                var character = entity as IMyCharacter;
                if (grid != null)
                {
                    if (grid.GridSizeEnum == MyCubeSize.Large)
                        return Weapon.Settings.TargetGridsState && Weapon.Settings.TargetLargeGridsState;
                    return Weapon.Settings.TargetGridsState && Weapon.Settings.TargetSmallGridsState;
                }
                else if (character != null)
                {
                    return Weapon.Settings.TargetCharactersState;
                }
            }
            else if (target is TargetableProjectile)
            {
                return Weapon.Settings.TargetProjectilesState;
            }
            

            return false;
        }

        internal virtual bool IsInRange(Vector3D? position)
        {
            if (position == null)
                return false;

            double rangeSq = Vector3D.DistanceSquared(Weapon.MuzzleMatrix.Translation, position.Value);
            return rangeSq < Weapon.Magazine.CurrentAmmo.PhysicalProjectileDef.MaxTrajectory * Weapon.Magazine.CurrentAmmo.PhysicalProjectileDef.MaxTrajectory &&
                   rangeSq > Weapon.Definition.Targeting.MinTargetingRange * Weapon.Definition.Targeting.MinTargetingRange;
        }

        #endregion
    }
}
