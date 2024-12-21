using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Networking;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Projectiles
{
    internal class PhysicalProjectile : HitscanProjectile
    {
        public Vector3D InheritedVelocity = Vector3D.Zero;
        public Vector3D Velocity;
        public double DistanceTravelled { get; private set; } = 0;

        public PhysicalProjectile(ProjectileDefinitionBase definition, Vector3D start, Vector3D direction, IMyEntity owner = null) : base(definition, start, direction, owner)
        {
            Raycast = new LineD(start, start + direction, Definition.PhysicalProjectileDef.Velocity);
            Velocity = direction * Definition.PhysicalProjectileDef.Velocity;
            if (owner?.Physics != null)
                InheritedVelocity = owner.Physics.GetVelocityAtPoint(start);
        }

        public override void UpdateTick(double deltaTime)
        {
            if (!IsActive)
                return;

            #region Movement

            {
                float dummyNaturalGravityInterference;
                Vector3D gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Raycast.From, out dummyNaturalGravityInterference) * Definition.PhysicalProjectileDef.GravityInfluenceMultiplier;

                // Update velocity based on gravity acceleration
                Velocity += gravity * deltaTime;

                // Raycast.From represents the projectile's position.
                Raycast.From += (InheritedVelocity + Velocity) * deltaTime;
                DistanceTravelled += (InheritedVelocity + Velocity).Length() * deltaTime;

                Raycast.To = Raycast.From + (InheritedVelocity + Velocity) * deltaTime;
                Raycast.To += Raycast.Direction * 0.1f; // Add some extra length to the raycast to make it more reliable; otherwise colliders could slip in between the movement steps (somehow)
            }

            #endregion

            #region IsActive Checking

            {
                if (DistanceTravelled > Definition.PhysicalProjectileDef.MaxTrajectory)
                    IsActive = false;
            }

            #endregion
            

            base.UpdateTick(deltaTime);
        }

        internal override SerializedSpawnProjectile ToSerializedSpawnProjectile()
        {
            var data = base.ToSerializedSpawnProjectile();
            data.Velocity = Velocity + InheritedVelocity;
            return data;
        }

        internal override SerializedSyncProjectile ToSerializedSyncProjectile()
        {
            var data = base.ToSerializedSyncProjectile();
            data.Velocity = Velocity + InheritedVelocity;
            return data;
        }
    }
}
