using Orrery.HeartModule.Client.Projectiles;
using Orrery.HeartModule.Shared.Targeting;
using Orrery.HeartModule.Shared.Targeting.Generics;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Shared.Networking
{
    /// <summary>
    /// Syncs guidance target.
    /// </summary>
    [ProtoContract]
    internal class SerializedGuidance : PacketBase
    {
        [ProtoMember(1)] private long _targetId;
        [ProtoMember(2)] private bool _isTargetingProjectile;
        [ProtoMember(3)] private uint _thisProjectileId;

        private SerializedGuidance()
        {
        }

        public SerializedGuidance(ProjectileGuidance guidance)
        {
            _thisProjectileId = guidance.Projectile.Id;

            if (guidance.Target is TargetableEntity)
            {
                _targetId = ((TargetableEntity)guidance.Target).Entity.EntityId;
                _isTargetingProjectile = false;
            }
            else if (guidance.Target is TargetableProjectile)
            {
                _targetId = ((TargetableProjectile)guidance.Target).Projectile.Id;
                _isTargetingProjectile = true;
            }
        }

        public override void Received(ulong SenderSteamId)
        {
            var guidance = (ProjectileManager.GetProjectile(_thisProjectileId) as PhysicalProjectile)?.Guidance;
            if (guidance == null)
                return;

            ITargetable target;
            if (_isTargetingProjectile)
                target = new TargetableProjectile(ProjectileManager.GetProjectile((uint)_targetId) as PhysicalProjectile);
            else
                target = new TargetableEntity(MyAPIGateway.Entities.GetEntityById(_targetId));

            guidance.SetTarget(target, false);
        }
    }
}
