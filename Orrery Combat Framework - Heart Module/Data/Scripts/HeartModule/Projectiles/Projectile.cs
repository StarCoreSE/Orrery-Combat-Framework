using Heart_Module.Data.Scripts.HeartModule.Debug;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.GuidanceHelpers;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Drawing;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using static Sandbox.Engine.Physics.MyPhysics.CollisionLayers;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    public partial class Projectile // TODO: Make physical, beams, and guided projectiles inheritors, and make a projectile struct class
    {
        #region Definition Values
        public uint Id { get; private set; }
        public readonly ProjectileDefinitionBase Definition;
        public readonly int DefinitionId;
        Dictionary<string, object> Overrides = new Dictionary<string, object>();
        public Vector3D InheritedVelocity;
        #endregion

        public ProjectileGuidance Guidance;
        public bool IsHitscan { get; private set; } = false;
        public MyEntity Firer = null;
        public Vector3D Position = Vector3D.Zero;
        public Vector3D Direction = Vector3D.Up;
        public float Velocity = 0;
        public int RemainingImpacts = 0;

        public Action<Projectile> OnClose = (p) =>
        {
            p.Definition.LiveMethods.OnEndOfLife?.Invoke(p.Id);
            p.CloseDrawing();
        };

        /// <summary>
        /// LastUpdate in absolute TICKS
        /// </summary>
        public long LastUpdate { get; set; }

        public float DistanceTravelled { get; private set; } = 0;
        public float Age { get; set; } = 0;
        public bool QueuedDispose { get; private set; } = false;

        private float _health = 0;
        public float Health
        {
            get
            {
                return _health;
            }
            set
            {
                _health = value;
                if (_health <= 0 && Definition.PhysicalProjectile.Health > 0)
                    QueueDispose();
            }
        }

        public Projectile() { }

        public Projectile(n_SerializableProjectile projectile)
        {
            if (!ProjectileManager.I.IsIdAvailable(projectile.Id))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - duplicate Id!");
                //ProjectileManager.I.GetProjectile(projectile.Id)?.UpdateFromSerializable(projectile);
                return;
            }

            if (!projectile.DefinitionId.HasValue || !ProjectileDefinitionManager.HasDefinition(projectile.DefinitionId.Value))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - invalid DefinitionId!");
                DefinitionId = -1;
                return;
            }

            Id = projectile.Id;
            DefinitionId = projectile.DefinitionId.Value;
            Definition = ProjectileDefinitionManager.GetDefinition(projectile.DefinitionId.Value);
            Firer = (MyEntity) MyAPIGateway.Entities.GetEntityById(projectile.Firer.GetValueOrDefault(0));
            IsHitscan = Definition.PhysicalProjectile.IsHitscan;
            Health = Definition.PhysicalProjectile.Health;
            if (!IsHitscan)
                Velocity = Definition.PhysicalProjectile.Velocity;
            else
                Definition.PhysicalProjectile.MaxLifetime = 1 / 60f;

            if (Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);

            Definition.LiveMethods.OnSpawn?.Invoke(Id, Firer);
            UpdateFromSerializable(projectile);
        }

        /// <summary>
        /// Spawn a projectile with a grid as a reference.
        /// </summary>
        /// <param name="DefinitionId"></param>
        /// <param name="Position"></param>
        /// <param name="Direction"></param>
        /// <param name="block"></param>
        public Projectile(int DefinitionId, Vector3D Position, Vector3D Direction, IMyCubeBlock block) : this(DefinitionId, Position, Direction, (MyEntity) block, block.CubeGrid?.LinearVelocity ?? Vector3D.Zero)
        {
        }

        public Projectile(int DefinitionId, Vector3D Position, Vector3D Direction, MyEntity firer = null, Vector3D InitialVelocity = new Vector3D())
        {
            if (!ProjectileDefinitionManager.HasDefinition(DefinitionId))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - invalid DefinitionId!");
                return;
            }

            this.DefinitionId = DefinitionId;
            Definition = ProjectileDefinitionManager.GetDefinition(DefinitionId);

            this.Position = Position;
            this.Direction = Direction;
            this.Firer = firer;

            IsHitscan = Definition.PhysicalProjectile.IsHitscan;

            // Apply velocity variance
            if (!IsHitscan)
            {
                // Randomly adjust velocity within the variance range
                float variance = (float)(new Random().NextDouble() * 2 - 1) * Definition.PhysicalProjectile.VelocityVariance;
                Velocity = Definition.PhysicalProjectile.Velocity + variance;
                this.InheritedVelocity = InitialVelocity;
            }
            else
            {
                Definition.PhysicalProjectile.MaxLifetime = 1 / 60f;
            }

            RemainingImpacts = Definition.Damage.MaxImpacts;
            Health = Definition.PhysicalProjectile.Health;

            if (Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);

            Definition.LiveMethods.OnSpawn?.Invoke(Id, Firer);
        }

        public void TickUpdate(float delta, HashSet<IMyEntity> entities = null)
        {
            if ((Definition.PhysicalProjectile.MaxTrajectory != -1 && Definition.PhysicalProjectile.MaxTrajectory < DistanceTravelled) || (Definition.PhysicalProjectile.MaxLifetime != -1 && Definition.PhysicalProjectile.MaxLifetime < Age))
                QueueDispose();

            if (QueuedDispose)
                return;

            if (Guidance == null && Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);

            Age += delta;
            if (!IsHitscan)
            {
                Guidance?.RunGuidance(delta);

                CheckHits(entities);

                // Apply gravity as an acceleration
                float gravityMultiplier = Definition.PhysicalProjectile.GravityInfluenceMultiplier;
                Vector3D gravity;
                float dummyNaturalGravityInterference;
                gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Position, out dummyNaturalGravityInterference);
                Vector3D gravityDirection = Vector3D.Normalize(gravity);
                double gravityAcceleration = gravity.Length() * gravityMultiplier;

                // Update velocity based on gravity acceleration
                Velocity += (float)(gravityAcceleration * delta);

                // Update position accounting for gravity
                Position += (InheritedVelocity + Direction * Velocity) * delta;

                // Update distance travelled
                DistanceTravelled += Velocity * delta;

                // Calculate next move step with gravity acceleration
                NextMoveStep = Position + (InheritedVelocity + Direction * Velocity) * delta;

                // Ensure the projectile continues its trajectory when leaving gravity
                if (gravityAcceleration <= 0)
                {
                    // No gravity, continue with current velocity
                    NextMoveStep = Position + (InheritedVelocity + Direction * Velocity) * delta;
                }
                else
                {
                    // Apply gravity acceleration to velocity
                    Velocity += (float)(gravityAcceleration * delta);

                    // Adjust direction based on gravity
                    Direction = Vector3D.Normalize(Direction + gravityDirection * gravityMultiplier);

                    // Calculate next move step with gravity acceleration
                    NextMoveStep = Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * delta)) * delta;
                }
            }
            else // Beams are really special, and need their own handling.
            {
                if (!MyAPIGateway.Session.IsServer)
                    RemainingImpacts = Definition.Damage.MaxImpacts;
                NextMoveStep = Position + Direction * Definition.PhysicalProjectile.MaxTrajectory;

                if (RemainingImpacts > 0)
                {
                    CheckHits(entities); // Set visual beam length
                    if (MaxBeamLength == -1)
                        MaxBeamLength = Definition.PhysicalProjectile.MaxTrajectory;
                }

                DrawUpdate();
                QueueDispose();
            }
            if (MyAPIGateway.Session.IsServer)
                UpdateAudio();
        }

        public void CheckHits(HashSet<IMyEntity> entities)
        {
            if (NextMoveStep == Vector3D.Zero)
                return;

            double len = IsHitscan ? Definition.PhysicalProjectile.MaxTrajectory : Vector3D.Distance(Position, NextMoveStep);

            // Run projectile collision checks
            if (MyAPIGateway.Session.IsServer && RemainingImpacts > 0 && Definition.Damage.DamageToProjectiles > 0)
            {
                List<Projectile> hittableProjectiles = new List<Projectile>();
                ProjectileManager.I.GetProjectilesInSphere(new BoundingSphereD(Position, len), ref hittableProjectiles, true);

                float damageToProjectilesInAoE = 0;
                List<Projectile> projectilesInAoE = new List<Projectile>();
                ProjectileManager.I.GetProjectilesInSphere(new BoundingSphereD(Position, Definition.Damage.DamageToProjectilesRadius), ref projectilesInAoE, true);

                RayD ray = new RayD(Position, Direction);

                foreach (var projectile in hittableProjectiles)
                {
                    if (RemainingImpacts <= 0 || projectile == this)
                        continue;

                    Vector3D offset = Vector3D.Half * projectile.Definition.PhysicalProjectile.ProjectileSize;
                    BoundingBoxD box = new BoundingBoxD(projectile.Position - offset, projectile.Position + offset);
                    double? intersectDist = ray.Intersects(box);
                    if (intersectDist != null)
                    {
                        MaxBeamLength = (float) intersectDist.Value;
                        projectile.Health -= Definition.Damage.DamageToProjectiles;

                        damageToProjectilesInAoE += Definition.Damage.DamageToProjectiles;

                        Vector3D hitPos = Position + Direction * MaxBeamLength;

                        if (MyAPIGateway.Session.IsServer)
                            PlayImpactAudio(hitPos); // Audio is global
                        if (!MyAPIGateway.Utilities.IsDedicated)
                            DrawImpactParticle(hitPos, Direction); // Visuals are clientside

                        Definition.LiveMethods.OnImpact?.Invoke(Id, hitPos, Direction, null);

                        RemainingImpacts--;
                    }
                }

                if (damageToProjectilesInAoE > 0)
                    foreach (var projectile in projectilesInAoE)
                        if (projectile != this)
                            projectile.Health -= damageToProjectilesInAoE;
            }

            // Check if a raycast is needed, and if it is, perform a physics cast.
            RayD travelLine = new RayD(Position, Direction);
            double checkDistSq = (NextMoveStep - Position).LengthSquared();

            foreach (var entity in entities)
            {
                double? dist = entity.WorldVolume.Intersects(travelLine); // This seems to be the cheapest form of line checking
                if (!dist.HasValue || dist * dist > checkDistSq)
                    continue;

                PerformRaycastRecursive(len);
                break;
            }


            if (RemainingImpacts <= 0)
                QueueDispose();
        }

        private void PerformRaycastRecursive(double length)
        {
            MaxBeamLength = -1;

            if (RemainingImpacts <= 0)
                return;

            BoundingSphereD sphere = new BoundingSphereD(Position, NextMoveStep.Length());
            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, entities);
            if (entities.Count == 0)
                return;

            LineD ray = new LineD(Position, NextMoveStep);
            Vector3D normalDirection = (NextMoveStep - Position).Normalized();
            double totalDist = (NextMoveStep - Position).Length();
            List<MyLineSegmentOverlapResult<MyEntity>> results = new List<MyLineSegmentOverlapResult<MyEntity>>();
            MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref ray, results, MyEntityQueryType.Both);

            foreach (var result in results)
            {
                MyAPIGateway.Utilities.ShowNotification("HitType: " + (result.Element as IMyCubeGrid)?.CustomName, 2000);
                if (result.Distance > totalDist || RemainingImpacts <= 0 || result.Element == Firer.GetTopMostParent())
                    continue;

                Vector3D hitPosition = Position + normalDirection * result.Distance;

                DebugDraw.AddLine(hitPosition, hitPosition + normalDirection, VRageMath.Color.Blue, 2);
                DebugDraw.AddLine(Position, NextMoveStep, VRageMath.Color.Green, 2);

                MaxBeamLength = (float) result.Distance;

                if (MyAPIGateway.Session.IsServer)
                {
                    if (result.Element is IMyCubeGrid)
                        DamageHandler.QueueEvent(new DamageEvent(result.Element, DamageEvent.DamageEntType.Grid, this, hitPosition, normalDirection, hitPosition + normalDirection, hitPosition - normalDirection));
                    else if (result.Element is IMyCharacter)
                        DamageHandler.QueueEvent(new DamageEvent(result.Element, DamageEvent.DamageEntType.Character, this, hitPosition, normalDirection, hitPosition + normalDirection, hitPosition - normalDirection));
                }

                if (MyAPIGateway.Session.IsServer)
                    PlayImpactAudio(hitPosition); // Audio is global
                if (!MyAPIGateway.Utilities.IsDedicated)
                    DrawImpactParticle(hitPosition, normalDirection); // Visuals are clientside

                Definition.LiveMethods.OnImpact?.Invoke(Id, hitPosition, Direction, result.Element);

                RemainingImpacts--;
            }

            //MyAPIGateway.Physics.CastRayParallel(ref Position, ref NextMoveStep, 0, (hitInfo) =>
            //{
            //    if (RemainingImpacts <= 0 || hitInfo.HitEntity.EntityId == Firer)
            //        return;
            //
            //    DebugDraw.AddLine(hitInfo.Position, hitInfo.Position - hitInfo.Normal, VRageMath.Color.Blue, 2);
            //
            //    MaxBeamLength = (float) (hitInfo.Fraction * length);
            //
            //    if (MyAPIGateway.Session.IsServer)
            //    {
            //        if (hitInfo.HitEntity is IMyCubeGrid)
            //            DamageHandler.QueueEvent(new DamageEvent(hitInfo.HitEntity, DamageEvent.DamageEntType.Grid, this, hitInfo.Position, hitInfo.Normal, hitInfo.Position + hitInfo.Normal, hitInfo.Position - hitInfo.Normal));
            //        else if (hitInfo.HitEntity is IMyCharacter)
            //            DamageHandler.QueueEvent(new DamageEvent(hitInfo.HitEntity, DamageEvent.DamageEntType.Character, this, hitInfo.Position, hitInfo.Normal, hitInfo.Position + hitInfo.Normal, hitInfo.Position - hitInfo.Normal));
            //    }
            //
            //    if (MyAPIGateway.Session.IsServer)
            //        PlayImpactAudio(hitInfo.Position); // Audio is global
            //    if (!MyAPIGateway.Utilities.IsDedicated)
            //        DrawImpactParticle(hitInfo.Position, hitInfo.Normal); // Visuals are clientside
            //
            //    Definition.LiveMethods.OnImpact?.Invoke(Id, hitInfo.Position, Direction, (MyEntity)hitInfo.HitEntity);
            //
            //    RemainingImpacts--;
            //});

            //if (dist == -1)

            //double nextDist = PerformRaycastRecursive(length);
            //
            //if (nextDist == -1)
            //    return dist;
            //
            //// Get the furthest impact distance.
            //return Math.Max(dist, nextDist);
        }

        public Vector3D NextMoveStep = Vector3D.Zero;

        public void UpdateFromSerializable(n_SerializableProjectile projectile)
        {
            if (projectile.IsActive.HasValue)
                QueuedDispose = !projectile.IsActive.Value;

            LastUpdate = DateTime.UtcNow.Date.AddMilliseconds(projectile.TimestampFromMidnight).Ticks;
            float delta = (DateTime.UtcNow.Ticks - LastUpdate) / (float)TimeSpan.TicksPerSecond;

            // The following values may be null to save network load
            if (projectile.Direction.HasValue)
                Direction = projectile.Direction.Value;
            if (projectile.Position.HasValue)
                Position = projectile.Position.Value;
            if (projectile.Velocity.HasValue)
                Velocity = projectile.Velocity.Value;
            if (projectile.InheritedVelocity.HasValue)
                InheritedVelocity = projectile.InheritedVelocity.Value;
            if (projectile.Firer.HasValue)
                Firer = (MyEntity) MyAPIGateway.Entities.GetEntityById(projectile.Firer ?? 0);
            TickUpdate(delta);
        }

        public void UpdateHitscan(Vector3D newPosition, Vector3D newDirection)
        {
            Age = 0;
            Position = newPosition;
            Direction = newDirection;
            RemainingImpacts = Definition.Damage.MaxImpacts;
        }

        /// <summary>
        /// Returns the projectile as a network-ready projectile info class. 0 = max detail, 2+ = min detail
        /// </summary>
        /// <param name="DetailLevel"></param>
        /// <returns></returns>
        public n_SerializableProjectile AsSerializable(int DetailLevel = 1)
        {
            n_SerializableProjectile projectile = new n_SerializableProjectile()
            {
                Id = Id,
                TimestampFromMidnight = (uint)DateTime.Now.TimeOfDay.TotalMilliseconds, // Surely this will not bite me in the ass later
            };

            switch (DetailLevel)
            {
                case 0:
                    projectile.IsActive = !QueuedDispose;
                    projectile.DefinitionId = DefinitionId;
                    projectile.Position = Position;
                    projectile.Direction = Direction;
                    projectile.InheritedVelocity = InheritedVelocity;
                    projectile.Firer = Firer.EntityId;
                    //projectile.Velocity = Velocity;
                    break;
                case 1:
                    projectile.IsActive = !QueuedDispose;
                    projectile.Position = Position;
                    if (IsHitscan || Definition.Guidance.Length > 0)
                        projectile.Direction = Direction;
                    if (!IsHitscan && Definition.PhysicalProjectile.Acceleration > 0)
                        projectile.Velocity = Velocity;
                    break;
                case 3:
                    projectile.DefinitionId = DefinitionId;
                    projectile.Position = Position;
                    projectile.Direction = Direction;
                    projectile.InheritedVelocity = InheritedVelocity;
                    projectile.Firer = Firer.EntityId;
                    break;
            }

            return projectile;
        }

        public void QueueDispose()
        {
            QueuedDispose = true;
        }

        public void SetId(uint id)
        {
            if (Id == 0)
                Id = id;
        }
    }
}
