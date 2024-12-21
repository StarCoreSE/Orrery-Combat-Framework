using System;
using System.Collections.Generic;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Utility;
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
    internal class HitscanProjectile
    {
        public uint Id;
        public ProjectileDefinitionBase Definition;
        public LineD Raycast;
        public IMyEntity Owner;

        private List<MyLineSegmentOverlapResult<MyEntity>> _raycastCache =
            new List<MyLineSegmentOverlapResult<MyEntity>>();
        private List<MyEntity> _areaHitCache = new List<MyEntity>();


        public bool IsActive = true;
        public int HitCount = 0;
        public float Age { get; set; } = 0;

        public HitscanProjectile(ProjectileDefinitionBase definition, Vector3D start, Vector3D direction, IMyEntity owner = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            Definition = definition;
            Raycast = new LineD(start, direction * Definition.PhysicalProjectileDef.MaxTrajectory);
            Owner = owner;
        }

        public virtual void UpdateTick(double deltaTime)
        {
            if (!IsActive)
                return;

            Age += (float) deltaTime;

            #region IsActive Checking

            {
                if (Age > Definition.PhysicalProjectileDef.MaxLifetime)
                    IsActive = false;
            }

            #endregion

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
                if (Owner != null && e.Element.IsEntityInHierarchy(Owner))
                    continue;

                if (!(e.Element is MyCubeGrid))
                {
                    OnImpact(e.Element, Raycast.From + Raycast.Direction * e.Distance);
                    continue;
                }

                MyCubeGrid impactGrid = (MyCubeGrid) e.Element;
                List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
                impactGrid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(grids);

                IMySlimBlock closestBlock = null;
                Vector3D closestIntersect = Vector3D.Zero;

                {
                    double closestDistance = double.MaxValue;
                    LineD testLine = Raycast;
                    testLine.To -= Raycast.Direction * 0.5f;
                    testLine.From += Raycast.Direction * 0.5f;

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

                if (closestBlock != null)
                {
                    int prevHitCount = HitCount;
                    OnImpact(closestBlock);

                    // AoE damage
                    if (Definition.DamageDef.AreaDamage != 0 && Definition.DamageDef.AreaRadius > 0)
                        DoAreaDamage(closestIntersect, (MyEntity) closestBlock.CubeGrid);

                    if (Definition.UngroupedDef.Impulse != 0)
                        closestBlock.CubeGrid.Physics?.ApplyImpulse(Raycast.Direction * Definition.UngroupedDef.Impulse * (HitCount - prevHitCount), closestIntersect);
                }
            }

            _raycastCache.Clear();
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
                    hitEntity.Physics?.ApplyImpulse(Raycast.Direction * Definition.UngroupedDef.Impulse * (Definition.DamageDef.MaxImpacts <= 0 ? 1 : Definition.DamageDef.MaxImpacts - prevHitCount), hitPosition);
                return;
            }

            OnImpact(destroyableObject);

            // AoE damage
            if (Definition.DamageDef.AreaDamage != 0 && Definition.DamageDef.AreaRadius > 0)
                DoAreaDamage(hitPosition, hitEntity);

            if (Definition.UngroupedDef.Impulse != 0)
                hitEntity.Physics?.ApplyImpulse(Raycast.Direction * Definition.UngroupedDef.Impulse * (HitCount - prevHitCount), hitPosition);
        }

        internal virtual void OnImpact(IMyDestroyableObject destroyableObject)
        {
            // If MaxImpacts <= 0, hit once. Else, hit until out of MaxImpacts.
            do
            {
                // Note that we are intentionally not doing deformation. TODO: Add a config option for deformation
                // This can only run on the server, so damage should always be synced.
                if (Definition.DamageDef.BaseDamage != 0)
                    destroyableObject.DoDamage(Definition.DamageDef.BaseDamage, MyDamageType.Bullet, true, attackerId: Owner?.EntityId ?? 0);

                HitCount++;
            }
            while (Definition.DamageDef.MaxImpacts > 0 && HitCount < Definition.DamageDef.MaxImpacts && destroyableObject.Integrity > 0);

            // Mark the projectile as inactive if out of hits
            if (Definition.DamageDef.MaxImpacts <= 0 || HitCount >= Definition.DamageDef.MaxImpacts)
                IsActive = false;
        }

        internal virtual void DoAreaDamage(Vector3D hitPosition, MyEntity hitEntity)
        {
            BoundingSphereD aoeSphere = new BoundingSphereD(hitPosition, Definition.DamageDef.AreaRadius);
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref aoeSphere, _areaHitCache);

            foreach (var entity in _areaHitCache)
            {
                if (entity.Physics == null || entity == hitEntity)
                    continue;

                if (entity is IMyCubeGrid)
                {
                    var grid = (IMyCubeGrid) entity;
                    foreach (var block in grid.GetBlocksInsideSphere(ref aoeSphere))
                    {
                        // TODO AreaFalloff
                        block.DoDamage(Definition.DamageDef.AreaDamage, MyDamageType.Explosion, true, attackerId: Owner?.EntityId ?? 0);
                    }
                }
                else if (entity is IMyDestroyableObject)
                {
                    ((IMyDestroyableObject) entity).DoDamage(Definition.DamageDef.AreaDamage, MyDamageType.Explosion, true, attackerId: Owner?.EntityId ?? 0);
                }
            }
            _areaHitCache.Clear();

            {
                if (hitEntity is IMyCubeGrid)
                {
                    var grid = (IMyCubeGrid) hitEntity;
                    foreach (var block in grid.GetBlocksInsideSphere(ref aoeSphere))
                    {
                        // TODO AreaFalloff
                        block.DoDamage(Definition.DamageDef.AreaDamage, MyDamageType.Explosion, true, attackerId: Owner?.EntityId ?? 0);
                    }
                }
                else if (hitEntity is IMyDestroyableObject)
                {
                    ((IMyDestroyableObject) hitEntity).DoDamage(Definition.DamageDef.AreaDamage, MyDamageType.Explosion, true, attackerId: Owner?.EntityId ?? 0);
                }
            }
            

            // TODO check for projectiles
        }

        internal virtual SerializedSpawnProjectile ToSerializedSpawnProjectile()
        {
            return new SerializedSpawnProjectile
            {
                // It's okay if this throws an exception.
                // ReSharper disable once PossibleInvalidOperationException
                DefinitionId = Definition.GetId().Value,
                Id = Id,
                Position = Raycast.From,
                Direction = Raycast.Direction,
                OwnerId = Owner?.EntityId ?? 0,
            };
        }
        internal virtual SerializedSyncProjectile ToSerializedSyncProjectile()
        {
            return new SerializedSyncProjectile
            {
                Id = Id,
                Direction = Raycast.Direction,
                Position = Raycast.From,
            };
        }

        internal virtual SerializedCloseProjectile ToSerializedCloseProjectile()
        {
            return new SerializedCloseProjectile
            {
                Id = Id,
                Position = Raycast.From,
            };
        }

        public static explicit operator SerializedSpawnProjectile(HitscanProjectile p) => p.ToSerializedSpawnProjectile();
        public static explicit operator SerializedSyncProjectile(HitscanProjectile p) => p.ToSerializedSyncProjectile();
        public static explicit operator SerializedCloseProjectile(HitscanProjectile p) => p.ToSerializedCloseProjectile();
    }
}
