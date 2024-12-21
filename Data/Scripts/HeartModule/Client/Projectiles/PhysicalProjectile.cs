using Orrery.HeartModule.Shared.Networking;
using Sandbox.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Client.Projectiles
{
    internal class PhysicalProjectile : HitscanProjectile
    {
        public Vector3D Velocity;

        public PhysicalProjectile(SerializedSpawnProjectile data) : base(data)
        {
            Velocity = data.Velocity;
        }

        public override void Update(double deltaTime = 1/60d)
        {
            #region Movement

            {
                float dummyNaturalGravityInterference;
                Vector3D gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Raycast.From, out dummyNaturalGravityInterference) * Definition.PhysicalProjectileDef.GravityInfluenceMultiplier;

                // Update velocity based on gravity acceleration
                Velocity += gravity * deltaTime;

                // Raycast.From represents the projectile's position.
                Raycast.From += Velocity * deltaTime;

                // NextMoveStep is separate from Raycast.To because the raycast needs to check a bit in front of the next movement step.
                Raycast.To = Raycast.From + Velocity * deltaTime;
            }

            #endregion
        }

        public override void UpdateSync(SerializedSyncProjectile data)
        {
            base.UpdateSync(data);
            Velocity = data.Velocity;
        }
    }
}
