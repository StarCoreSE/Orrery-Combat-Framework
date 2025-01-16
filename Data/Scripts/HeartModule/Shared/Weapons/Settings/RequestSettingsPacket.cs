using Orrery.HeartModule.Server.Weapons;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Networking;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Shared.Weapons.Settings
{
    [ProtoContract]
    internal class RequestSettingsPacket : PacketBase
    {
        [ProtoMember(1)] public long WeaponId;

        public RequestSettingsPacket(long weaponId)
        {
            WeaponId = weaponId;
        }

        private RequestSettingsPacket()
        {
        }

        public override void Received(ulong SenderSteamId)
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            WeaponManager.GetWeapon(WeaponId)?.Settings?.Sync();
        }
    }
}
