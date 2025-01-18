using System;
using System.Collections.Generic;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Networking;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Projectiles
{
    internal class HitscanProjectile : IHitscanProjectile
    {
        public uint Id { get; internal set; }
        public ProjectileDefinitionBase Definition { get; }
        public LineD Raycast;
        public IMyEntity Owner { get; }
        public Vector3D Position
        {
            get
            {
                return Raycast.From;
            }
            set
            {
                Raycast.From = value;
            }
        }

        public Vector3D Direction
        {
            get
            {
                return Raycast.Direction.Normalized();
            }
            set
            {
                Raycast.Direction = value;
            }
        }

        private List<MyLineSegmentOverlapResult<MyEntity>> _raycastCache =
            new List<MyLineSegmentOverlapResult<MyEntity>>();
        private List<MyEntity> _areaHitCache = new List<MyEntity>();
        private HashSet<PhysicalProjectile> _projectileBuffer;

        public bool IsActive { get; set; } = true;

        public int HitCount = 0;
        public float Age { get; set; } = 0;

        public HitscanProjectile(ProjectileDefinitionBase definition, Vector3D start, Vector3D direction, IMyEntity owner = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            Definition = definition;
            Raycast = new LineD(start, start + direction * Definition.PhysicalProjectileDef.MaxTrajectory);
            Owner = owner;

            // Start with some pre-allocation to save performance
            _projectileBuffer = new HashSet<PhysicalProjectile>(Definition.DamageDef.DamageToProjectiles > 0 ? 5 : 0);
        }

        public virtual void UpdateTick(double deltaTime)
        {
            if (!IsActive || deltaTime == 0)
                return;

            Age += (float) deltaTime;

            #region IsActive Checking

            {
                if (Age > Definition.PhysicalProjectileDef.MaxLifetime && Definition.PhysicalProjectileDef.MaxLifetime > 0)
                    IsActive = false;
            }

            #endregion
        }

        public virtual void UpdateAfterTick(double deltaTime)
        {
            if (!IsActive || deltaTime == 0)
                return;

            CheckImpact();
        }

        /// <summary>
        /// Run this on the main physics thread!
        /// </summary>
        internal virtual void CheckImpact()
        {
            // Check if it's worth doing a physics raycast
            MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref Raycast, _raycastCache);

            foreach (var e in _raycastCache)
            {
                if (!IsActive)
                    break;

                // Ignore physicsless
                if (e.Element.Physics == null)
                    continue;

                // Close the projectile if we hit a non-damagable entity
                if (!(e.Element is IMyDestroyableObject || e.Element is IMyCubeGrid))
                {
                    IsActive = false;
                    break;
                }

                // Ignore owner
                if (Owner != null && e.Element == Owner)
                    continue;

                if (!(e.Element is MyCubeGrid))
                {
                    OnImpact(e.Element, Position + Direction * e.Distance);
                    continue;
                }

                MyCubeGrid impactGrid = (MyCubeGrid) e.Element;
                List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
                impactGrid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(grids);

                IMySlimBlock closestBlock = null;
                Vector3D closestIntersect = Vector3D.Zero;

                {
                    double closestDistance = double.MaxValue;
                    LineD testLine = new LineD(Raycast.From - Direction * 0.5f, Raycast.To + Direction * 0.5f);

                    foreach (var grid in grids)
                    {
                        double distance;
                        IMySlimBlock block;
                        Vector3D? intersect = grid.GetLineIntersectionExactAll(ref testLine, out distance, out block);

                        if (intersect == null || !(distance < closestDistance))
                            continue;

                        closestDistance = distance;
                        closestBlock = block;
                        closestIntersect = intersect.Value;
                    }
                }

                if (closestBlock != null && (closestBlock.FatBlock == null || closestBlock.FatBlock != Owner))
                {
                    int prevHitCount = HitCount;

                    float damageModifier = Definition.DamageDef.FatBlockDamageMod;
                    if (closestBlock.FatBlock == null)
                        damageModifier = Definition.DamageDef.SlimBlockDamageMod;
                    OnImpact(closestBlock, damageModifier);

                    // AoE damage
                    if (Definition.DamageDef.AreaDamage != 0 && Definition.DamageDef.AreaRadius > 0)
                        CheckAreaDamage(closestIntersect, (MyEntity) closestBlock.CubeGrid);

                    if (Definition.UngroupedDef.Impulse != 0)
                        closestBlock.CubeGrid.Physics?.ApplyImpulse(Direction * Definition.UngroupedDef.Impulse * (HitCount - prevHitCount), closestIntersect);

                    try
                    {
                        Definition.LiveMethods.ServerOnImpact?.Invoke(Id, Position, Direction, closestBlock.CubeGrid);
                    }
                    catch (Exception ex)
                    {
                        HeartLog.Exception(ex, typeof(HitscanProjectile));
                    }
                }
            }

            _raycastCache.Clear();

            #region Projectile Impact Checking

            if (IsActive && Definition.DamageDef.DamageToProjectiles > 0)
            {
                ProjectileManager.GetProjectilesInLine(Raycast, ref _projectileBuffer);

                Vector3D? firstImpact = null;
                foreach (var projectile in _projectileBuffer)
                {
                    bool ownersMatch = projectile.Owner == Owner || (projectile.Owner is IMyConveyorSorter &&
                                                                     Owner is IMyConveyorSorter &&
                                                                     ((IMyConveyorSorter)projectile.Owner)?.CubeGrid ==
                                                                     ((IMyConveyorSorter)Owner).CubeGrid);
                    if (projectile == this || ownersMatch)
                        continue;

                    projectile.Health -= Definition.DamageDef.DamageToProjectiles;
                    HitCount++;
                    firstImpact = projectile.Position;
                    if (Definition.DamageDef.MaxImpacts <= 0 || HitCount >= Definition.DamageDef.MaxImpacts)
                        IsActive = false;
                }

                if (firstImpact != null)
                {
                    CheckAreaDamage(firstImpact.Value, null);

                    try
                    {
                        Definition.LiveMethods.ServerOnImpact?.Invoke(Id, Position, Direction, null);
                    }
                    catch (Exception ex)
                    {
                        HeartLog.Exception(ex, typeof(HitscanProjectile));
                    }
                }
            }

            #endregion
        }

        internal virtual void OnImpact(MyEntity hitEntity, Vector3D hitPosition)
        {
            var destroyableObject = hitEntity as IMyDestroyableObject;
            int prevHitCount = HitCount;

            // Mark the projectile as inactive if the hit object can't be damaged.
            if (destroyableObject == null)
            {
                if (Definition.DamageDef.MaxImpacts > 0)
                    HitCount = Definition.DamageDef.MaxImpacts;
                IsActive = false;

                if (Definition.UngroupedDef.Impulse != 0)
                    hitEntity.Physics?.ApplyImpulse(Direction * Definition.UngroupedDef.Impulse * (Definition.DamageDef.MaxImpacts <= 0 ? 1 : Definition.DamageDef.MaxImpacts - prevHitCount), hitPosition);
                return;
            }

            OnImpact(destroyableObject);

            // AoE damage
            if (Definition.DamageDef.AreaDamage != 0 && Definition.DamageDef.AreaRadius > 0)
                CheckAreaDamage(hitPosition, hitEntity);

            if (Definition.UngroupedDef.Impulse != 0)
                hitEntity.Physics?.ApplyImpulse(Direction * Definition.UngroupedDef.Impulse * (HitCount - prevHitCount), hitPosition);

            try
            {
                Definition.LiveMethods.ServerOnImpact?.Invoke(Id, hitPosition, Direction, hitEntity);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(HitscanProjectile));
            }
        }

        internal virtual void OnImpact(IMyDestroyableObject destroyableObject, float damageModifier = 1)
        {
            // If MaxImpacts <= 0, hit once. Else, hit until out of MaxImpacts.
            do
            {
                // Note that we are intentionally not doing deformation. TODO: Add a config option for deformation
                // This can only run on the server, so damage should always be synced.
                if (Definition.DamageDef.BaseDamage != 0)
                    destroyableObject.DoDamage(Definition.DamageDef.BaseDamage * damageModifier, MyDamageType.Bullet, true, attackerId: Owner?.EntityId ?? 0);
                
                HitCount++;
            }
            while (Definition.DamageDef.MaxImpacts > 0 && HitCount < Definition.DamageDef.MaxImpacts && destroyableObject.Integrity > 0);

            // Mark the projectile as inactive if out of hits
            if (Definition.DamageDef.MaxImpacts <= 0 || HitCount >= Definition.DamageDef.MaxImpacts)
                IsActive = false;
        }

        internal virtual void CheckAreaDamage(Vector3D hitPosition, MyEntity hitEntity)
        {
            BoundingSphereD aoeSphere = new BoundingSphereD(hitPosition, Definition.DamageDef.AreaRadius);
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref aoeSphere, _areaHitCache);

            foreach (var entity in _areaHitCache)
            {
                if (entity.Physics == null || entity == hitEntity)
                    continue;

                ApplyAreaDamageInternal(entity, aoeSphere);
            }
            _areaHitCache.Clear();

            // Sometimes GetAllTopMostEntitiesInSphere misses the hit entity, this ensures it is always hit.
            if (hitEntity != null)
                ApplyAreaDamageInternal(hitEntity, aoeSphere);


            #region Projectile AoE Damage
            if (Definition.DamageDef.DamageToProjectiles > 0 && Definition.DamageDef.DamageToProjectilesRadius > 0)
            {
                ProjectileManager.GetProjectilesInSphere(new BoundingSphereD(Position, Definition.DamageDef.DamageToProjectilesRadius), ref _projectileBuffer);

                foreach (var projectile in _projectileBuffer)
                    projectile.Health -= Definition.DamageDef.DamageToProjectiles;
            }
            #endregion
        }

        private void ApplyAreaDamageInternal(MyEntity hitEntity, BoundingSphereD aoeSphere)
        {
            if (hitEntity is IMyCubeGrid)
            {
                var grid = (IMyCubeGrid) hitEntity;
                foreach (var block in grid.GetBlocksInsideSphere(ref aoeSphere))
                {
                    float damageModifier = Definition.DamageDef.FatBlockDamageMod;
                    if (block.FatBlock == null)
                        damageModifier = Definition.DamageDef.SlimBlockDamageMod;

                    // TODO AreaFalloff
                    block.DoDamage(Definition.DamageDef.AreaDamage * damageModifier, MyDamageType.Explosion, true, attackerId: Owner?.EntityId ?? 0);
                }
            }
            else if (hitEntity is IMyDestroyableObject)
            {
                ((IMyDestroyableObject) hitEntity).DoDamage(Definition.DamageDef.AreaDamage, MyDamageType.Explosion, true, attackerId: Owner?.EntityId ?? 0);
            }
        }

        public void Sync() => ServerNetwork.SendToEveryoneInSync((SerializedSyncProjectile) this, Position);

        internal virtual SerializedSpawnProjectile ToSerializedSpawnProjectile()
        {
            return new SerializedSpawnProjectile
            {
                // It's okay if this throws an exception.
                // ReSharper disable once PossibleInvalidOperationException
                DefinitionId = Definition.GetId().Value,
                Id = Id,
                Position = Position,
                Direction = Direction,
                OwnerId = Owner?.EntityId ?? 0,
            };
        }
        internal virtual SerializedSyncProjectile ToSerializedSyncProjectile()
        {
            return new SerializedSyncProjectile
            {
                Id = Id,
                Direction = Direction,
                Position = Position,
            };
        }

        internal virtual SerializedCloseProjectile ToSerializedCloseProjectile()
        {
            return new SerializedCloseProjectile
            {
                Id = Id,
                Position = Position,
                DidImpact = HitCount > 0,
            };
        }

        public static explicit operator SerializedSpawnProjectile(HitscanProjectile p) => p?.ToSerializedSpawnProjectile();
        public static explicit operator SerializedSyncProjectile(HitscanProjectile p) => p?.ToSerializedSyncProjectile();
        public static explicit operator SerializedCloseProjectile(HitscanProjectile p) => p?.ToSerializedCloseProjectile();
    }
}
