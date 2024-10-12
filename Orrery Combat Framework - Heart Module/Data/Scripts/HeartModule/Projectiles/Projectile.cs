using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.GuidanceHelpers;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

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
        public long Firer = -1;
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

        // "I lack the words to describe the level of my disgust"
        private Vector3D NormalizeDirection(Vector3D direction)
        {
            return Vector3D.Normalize(direction);
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
            Firer = projectile.Firer.GetValueOrDefault(0);
            IsHitscan = Definition.PhysicalProjectile.IsHitscan;
            Health = Definition.PhysicalProjectile.Health;
            if (!IsHitscan)
                Velocity = Definition.PhysicalProjectile.Velocity;
            else
                Definition.PhysicalProjectile.MaxLifetime = 1 / 60f;

            if (Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);

            Definition.LiveMethods.OnSpawn?.Invoke(Id, (MyEntity)MyAPIGateway.Entities.GetEntityById(Firer));
            UpdateFromSerializable(projectile);
        }

        /// <summary>
        /// Spawn a projectile with a grid as a reference.
        /// </summary>
        /// <param name="DefinitionId"></param>
        /// <param name="Position"></param>
        /// <param name="Direction"></param>
        /// <param name="block"></param>
        public Projectile(int DefinitionId, Vector3D Position, Vector3D Direction, IMyCubeBlock block) : this(DefinitionId, Position, Direction, block.EntityId, block.CubeGrid?.LinearVelocity ?? Vector3D.Zero)
        {
        }

        public Projectile(int DefinitionId, Vector3D Position, Vector3D Direction, long firer = 0, Vector3D InitialVelocity = new Vector3D())
        {
            if (!ProjectileDefinitionManager.HasDefinition(DefinitionId))
            {
                SoftHandle.RaiseSyncException("Unable to spawn projectile - invalid DefinitionId!");
                return;
            }

            this.DefinitionId = DefinitionId;
            Definition = ProjectileDefinitionManager.GetDefinition(DefinitionId);

            this.Position = Position;

            // Modify the Direction assignment to use the normalization helper
            this.Direction = NormalizeDirection(Direction);

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

            Definition.LiveMethods.OnSpawn?.Invoke(Id, (MyEntity)MyAPIGateway.Entities.GetEntityById(Firer));
        }

        public void TickUpdate(float delta)
        {
            if ((Definition.PhysicalProjectile.MaxTrajectory != -1 && Definition.PhysicalProjectile.MaxTrajectory < DistanceTravelled) || (Definition.PhysicalProjectile.MaxLifetime != -1 && Definition.PhysicalProjectile.MaxLifetime < Age))
                QueueDispose();

            if (Guidance == null && Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);

            Age += delta;
            if (!IsHitscan)
            {
                Guidance?.RunGuidance(delta);
                CheckHits();

                // Apply gravity as an acceleration
                float gravityMultiplier = Definition.PhysicalProjectile.GravityInfluenceMultiplier;
                float dummyNaturalGravityInterference;
                Vector3D gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Position, out dummyNaturalGravityInterference);
                Vector3D gravityDirection = NormalizeDirection(gravity); // Ensure gravity direction is normalized
                double gravityAcceleration = gravity.Length() * gravityMultiplier;

                // Update velocity based on gravity acceleration
                Velocity += (float)(gravityAcceleration * delta);

                // Ensure Direction is normalized before using it to update Position
                Direction = NormalizeDirection(Direction);
                Position += (InheritedVelocity + Direction * Velocity) * delta;

                // Update distance travelled
                DistanceTravelled += Velocity * delta;

                // Calculate next move step with gravity acceleration
                NextMoveStep = Position + (InheritedVelocity + Direction * Velocity) * delta;

                if (gravityAcceleration <= 0)
                {
                    // No gravity, continue with current velocity
                    NextMoveStep = Position + (InheritedVelocity + Direction * Velocity) * delta;
                }
                else
                {
                    // Apply gravity acceleration to velocity
                    Velocity += (float)(gravityAcceleration * delta);

                    // Adjust direction based on gravity and normalize
                    Direction = NormalizeDirection(Direction + gravityDirection * gravityMultiplier);

                    // Calculate next move step with gravity acceleration
                    NextMoveStep = Position + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectile.Acceleration * delta)) * delta;
                }
            }
            else // Beams are really special, and need their own handling.
            {
                if (!MyAPIGateway.Session.IsServer)
                    RemainingImpacts = Definition.Damage.MaxImpacts;

                // Normalize Direction for beams as well
                Direction = NormalizeDirection(Direction);
                NextMoveStep = Position + Direction * Definition.PhysicalProjectile.MaxTrajectory;

                if (RemainingImpacts > 0)
                {
                    MaxBeamLength = CheckHits(); // Set visual beam length
                    if (MaxBeamLength == -1)
                        MaxBeamLength = Definition.PhysicalProjectile.MaxTrajectory;
                }

                DrawUpdate();
                QueueDispose();
            }
            if (MyAPIGateway.Session.IsServer)
                UpdateAudio();
        }

        public float CheckHits()
        {
            if (NextMoveStep == Vector3D.Zero)
                return -1;

            double len = IsHitscan ? Definition.PhysicalProjectile.MaxTrajectory : Vector3D.Distance(Position, NextMoveStep);
            double dist = -1;

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
                        dist = intersectDist.Value;
                        projectile.Health -= Definition.Damage.DamageToProjectiles;

                        damageToProjectilesInAoE += Definition.Damage.DamageToProjectiles;

                        Vector3D hitPos = Position + Direction * dist;

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

            if (RemainingImpacts > 0)
            {
                List<IHitInfo> intersects = new List<IHitInfo>();
                MyAPIGateway.Physics.CastRay(Position, NextMoveStep, intersects);

                foreach (var hitInfo in intersects)
                {
                    if (RemainingImpacts <= 0)
                        break;

                    if (hitInfo.HitEntity.EntityId == Firer)
                        continue; // Skip firer

                    dist = hitInfo.Fraction * len;

                    if (MyAPIGateway.Session.IsServer)
                    {
                        if (hitInfo.HitEntity is IMyCubeGrid)
                            DamageHandler.QueueEvent(new DamageEvent(hitInfo.HitEntity, DamageEvent.DamageEntType.Grid, this, hitInfo.Position, hitInfo.Normal, Position, NextMoveStep));
                        else if (hitInfo.HitEntity is IMyCharacter)
                            DamageHandler.QueueEvent(new DamageEvent(hitInfo.HitEntity, DamageEvent.DamageEntType.Character, this, hitInfo.Position, hitInfo.Normal, Position, NextMoveStep));
                    }

                    if (MyAPIGateway.Session.IsServer)
                        PlayImpactAudio(hitInfo.Position); // Audio is global
                    if (!MyAPIGateway.Utilities.IsDedicated)
                        DrawImpactParticle(hitInfo.Position, hitInfo.Normal); // Visuals are clientside

                    Definition.LiveMethods.OnImpact?.Invoke(Id, hitInfo.Position, Direction, (MyEntity)hitInfo.HitEntity);

                    RemainingImpacts--;
                }
            }

            if (RemainingImpacts <= 0)
                QueueDispose();

            return (float)dist;
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
                Firer = projectile.Firer.Value;
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
                    projectile.Firer = Firer;
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
                    projectile.Firer = Firer;
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
