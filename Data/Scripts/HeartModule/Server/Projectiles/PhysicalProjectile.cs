using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Networking;
using Orrery.HeartModule.Shared.Targeting;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Projectiles
{
    internal class PhysicalProjectile : HitscanProjectile, IPhysicalProjectile
    {
        public Vector3D InheritedVelocity { get; } = Vector3D.Zero;
        public Vector3D Velocity { get; set; }
        public BoundingSphereD CollisionSphere;

        private float _health;

        public float Health
        {
            get
            {
                return _health;
            }
            set
            {
                _health = value;
                if (_health <= 0 && Definition.PhysicalProjectileDef.Health > 0)
                    IsActive = false;
            }
        }

        internal ProjectileGuidance Guidance = null;
        private double _distanceTravelled = 0;

        public PhysicalProjectile(ProjectileDefinitionBase definition, Vector3D start, Vector3D direction, IMyEntity owner = null) : base(definition, start, direction, owner)
        {
            Raycast = new LineD(start, start + direction, Definition.PhysicalProjectileDef.Velocity);
            Health = Definition.PhysicalProjectileDef.Health;
            CollisionSphere = new BoundingSphereD(Position, Definition.PhysicalProjectileDef.ProjectileSize);

            if (owner?.Physics != null)
            {
                Vector3D ownerCenter;
                if (owner is IMyCharacter)
                    ownerCenter = owner.Physics.Center;
                else if (owner is IMyConveyorSorter)
                {
                    owner = ((IMyConveyorSorter)owner).CubeGrid;
                    ownerCenter = owner.Physics.CenterOfMassWorld;
                }
                else
                    ownerCenter = owner.Physics.CenterOfMassWorld;

                // Add linear velocity at point; this accounts for angular velocity and linear velocity
                InheritedVelocity = owner.Physics.LinearVelocity + owner.Physics.AngularVelocity.Cross(start - ownerCenter);
            }

            Velocity = direction * Definition.PhysicalProjectileDef.Velocity + InheritedVelocity;

            if (Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);
        }

        public override void UpdateTick(double deltaTime)
        {
            if (!IsActive || deltaTime == 0)
                return;

            Guidance?.Update(deltaTime);

            #region Movement

            {
                float dummyNaturalGravityInterference;
                Vector3D gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Position, out dummyNaturalGravityInterference) * Definition.PhysicalProjectileDef.GravityInfluenceMultiplier;

                Velocity += (gravity + Direction * Definition.PhysicalProjectileDef.Acceleration) * deltaTime;

                // Position represents the projectile's position.
                Position += Velocity * deltaTime;
                _distanceTravelled += Velocity.Length() * deltaTime;

                Raycast.To = Position + Velocity * deltaTime;
                Raycast.To += Direction * 0.1f; // Add some extra length to the raycast to make it more reliable; otherwise colliders could slip in between the movement steps (somehow)

                CollisionSphere.Center = Position;
            }

            #endregion

            #region IsActive Checking

            {
                if (_distanceTravelled > Definition.PhysicalProjectileDef.MaxTrajectory)
                    IsActive = false;
            }

            #endregion
            

            base.UpdateTick(deltaTime);
        }

        internal override SerializedSpawnProjectile ToSerializedSpawnProjectile()
        {
            var data = base.ToSerializedSpawnProjectile();
            data.Velocity = Velocity;
            return data;
        }

        internal override SerializedSyncProjectile ToSerializedSyncProjectile()
        {
            var data = base.ToSerializedSyncProjectile();
            data.Velocity = Velocity;
            return data;
        }
    }
}
