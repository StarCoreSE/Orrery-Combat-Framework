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

            base.Update();
        }

        public override void UpdateDraw(double deltaTime = 1/60d)
        {
            MaxBeamLength = 0;
            if (Definition.VisualDef.HasTrail && !HeartData.I.IsPaused)
                GlobalEffects.AddLine(Raycast.From, Raycast.From - Velocity.Normalized() * Definition.VisualDef.TrailLength, Definition.VisualDef.TrailFadeTime, Definition.VisualDef.TrailWidth, Definition.VisualDef.TrailColor, Definition.VisualDef.TrailTexture);

            base.UpdateDraw(deltaTime);
        }

        public override void UpdateSync(SerializedSyncProjectile data)
        {
            base.UpdateSync(data);
            Velocity = data.Velocity;
        }

        internal override void UpdateAudio()
        {
            if (!HasAudio || !Definition.AudioDef.HasTravelSound) return;

            base.UpdateAudio();
            ProjectileSound.SetVelocity(Velocity);
        }
    }
}
