using Orrery.HeartModule.Shared.Definitions;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Projectiles
{
    internal class PhysicalProjectile : HitscanProjectile
    {
        public Vector3D InheritedVelocity = Vector3D.Zero;
        public Vector3D Velocity;
        public Vector3D NextMoveStep = Vector3D.Zero;
        public Vector3D Direction = Vector3D.Forward;
        public double DistanceTravelled { get; private set; } = 0;

        public PhysicalProjectile(ProjectileDefinitionBase definition, Vector3D start, Vector3D direction, IMyEntity owner = null) : base(definition, start, direction, owner)
        {
            Direction = direction;
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
                // Apply gravity as an acceleration
                float gravityMultiplier = Definition.PhysicalProjectileDef.GravityInfluenceMultiplier;
                Vector3D gravity;
                float dummyNaturalGravityInterference;
                gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Raycast.From, out dummyNaturalGravityInterference);
                Vector3D gravityDirection = Vector3D.Normalize(gravity);
                double gravityAcceleration = gravity.Length() * gravityMultiplier;

                // Update velocity based on gravity acceleration
                Velocity += (float)(gravityAcceleration * deltaTime);

                // Update position accounting for gravity
                Raycast.From += (InheritedVelocity + Direction * Velocity) * deltaTime;

                // Update distance travelled
                DistanceTravelled += Velocity.Length() * deltaTime;

                // Calculate next move step with gravity acceleration
                NextMoveStep = Raycast.From + (InheritedVelocity + Direction * Velocity) * deltaTime;

                // Ensure the projectile continues its trajectory when leaving gravity
                if (gravityAcceleration <= 0)
                {
                    // No gravity, continue with current velocity
                    NextMoveStep = Raycast.From + (InheritedVelocity + Direction * Velocity) * deltaTime;
                }
                else
                {
                    // Apply gravity acceleration to velocity
                    Velocity += (float)(gravityAcceleration * deltaTime);

                    // Adjust direction based on gravity
                    Direction = Vector3D.Normalize(Direction + gravityDirection * gravityMultiplier);

                    // Calculate next move step with gravity acceleration
                    NextMoveStep = Raycast.From + (InheritedVelocity + Direction * (Velocity + Definition.PhysicalProjectileDef.Acceleration * deltaTime)) * deltaTime;
                }

                Raycast.To = NextMoveStep;
                Raycast.To += Raycast.Direction * 0.1f; // Add some extra length to the raycast to make it more reliable
                //Raycast.From -= Direction * distance * 0.1f;
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
