using Orrery.HeartModule.Client.Projectiles;
using Orrery.HeartModule.Client.Weapons;
using Orrery.HeartModule.Server.Weapons.Targeting;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Networking;
using Orrery.HeartModule.Shared.Targeting.Generics;
using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;

namespace Orrery.HeartModule.Shared.Weapons
{
    [ProtoContract]
    internal class SerializedTargetingEvent : PacketBase
    {
        [ProtoMember(1)] public long WeaponId;
        [ProtoMember(2)] private long _targetId;
        [ProtoMember(3)] private bool _isTargetingProjectile;

        public SerializedTargetingEvent(SmartWeaponTargeting targeting)
        {
            WeaponId = targeting.Weapon.Id;

            if (targeting.Target is TargetableEntity)
            {
                _targetId = ((TargetableEntity)targeting.Target).Entity.EntityId;
                _isTargetingProjectile = false;
            }
            else if (targeting.Target is TargetableProjectile)
            {
                _targetId = ((TargetableProjectile)targeting.Target).Projectile.Id;
                _isTargetingProjectile = true;
            }
        }

        private SerializedTargetingEvent() { }

        public override void Received(ulong SenderSteamId)
        {
            var weapon = WeaponManager.GetWeapon(WeaponId) as SorterSmartLogic;
            if (weapon == null)
                return;
            
            ITargetable target = null;
            if (_isTargetingProjectile)
            {
                var projectile = ProjectileManager.GetProjectile((uint)_targetId) as PhysicalProjectile;
                if (projectile != null)
                    target = new TargetableProjectile(projectile);
            }
            else
            {
                var entity = MyAPIGateway.Entities.GetEntityById(_targetId);
                if (entity != null)
                    target = new TargetableEntity(entity);
            }

            weapon.Target = target;
            try
            {
                weapon.Definition.LiveMethods.ClientOnRetarget?.Invoke(weapon.SorterWep, (target as TargetableEntity)?.Entity, (target as TargetableProjectile)?.Projectile?.Id);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(SorterWeaponLogic));
            }
        }
    }
}
