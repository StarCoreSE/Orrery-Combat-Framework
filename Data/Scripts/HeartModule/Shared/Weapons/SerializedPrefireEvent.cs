using Orrery.HeartModule.Server.Weapons;
using Orrery.HeartModule.Shared.Networking;
using ProtoBuf;

namespace Orrery.HeartModule.Shared.Weapons
{
    /// <summary>
    /// Triggered when a weapon is prefired.
    /// </summary>
    [ProtoContract]
    internal class SerializedPrefireEvent : PacketBase
    {
        [ProtoMember(1)] public long WeaponId;

        public SerializedPrefireEvent(SorterWeaponLogic weapon)
        {
            WeaponId = weapon.Id;
        }

        private SerializedPrefireEvent()
        {

        }

        public override void Received(ulong SenderSteamId)
        {
            Client.Weapons.WeaponManager.GetWeapon(WeaponId)?.PlayPreShootSound();
        }
    }
}
