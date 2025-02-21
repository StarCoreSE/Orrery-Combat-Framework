﻿using Orrery.HeartModule.Shared.Networking;
using Orrery.HeartModule.Shared.Targeting;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Sandbox.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Client.Projectiles
{
    internal class PhysicalProjectile : HitscanProjectile, IPhysicalProjectile
    {
        public Vector3D Velocity { get; set; }
        internal ProjectileGuidance Guidance = null;

        public PhysicalProjectile(SerializedSpawnProjectile data) : base(data)
        {
            Velocity = data.Velocity;

            if (Definition.Guidance.Length > 0)
                Guidance = new ProjectileGuidance(this);
        }

        public override void Update(double deltaTime = 1/60d)
        {
            if (deltaTime == 0)
                return;

            #region Movement
            
            Guidance?.Update(deltaTime);

            {
                float dummyNaturalGravityInterference;
                Vector3D gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(Position, out dummyNaturalGravityInterference) * Definition.PhysicalProjectileDef.GravityInfluenceMultiplier;

                // Update velocity based on gravity acceleration
                Velocity += (gravity + Direction * Definition.PhysicalProjectileDef.Acceleration) * deltaTime;

                // Position represents the projectile's position.
                Position += Velocity * deltaTime;

                // NextMoveStep is separate from Raycast.To because the raycast needs to check a bit in front of the next movement step.
                Raycast.To = Position + Velocity * deltaTime;
            }

            #endregion

            base.Update(deltaTime);
        }

        public override void UpdateDraw(double deltaTime = 1/60d)
        {
            MaxBeamLength = 0;
            if (Definition.VisualDef.HasTrail && !HeartData.I.IsPaused)
                GlobalEffects.AddLine(Position, Position - Velocity.Normalized() * Definition.VisualDef.TrailLength, Definition.VisualDef.TrailFadeTime, Definition.VisualDef.TrailWidth, Definition.VisualDef.TrailColor, Definition.VisualDef.TrailTexture);

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
