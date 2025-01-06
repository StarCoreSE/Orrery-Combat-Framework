using ProtoBuf;
using System;
using Orrery.HeartModule.Shared.WeaponSettings;

namespace Orrery.HeartModule.Shared.Networking
{
    [ProtoInclude(101, typeof(SerializedSpawnProjectile))]
    [ProtoInclude(102, typeof(SerializedSyncProjectile))]
    [ProtoInclude(103, typeof(SerializedCloseProjectile))]
    [ProtoInclude(104, typeof(SerializedGuidance))]
    [ProtoInclude(105, typeof(SettingsPacket))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract class PacketBase
    {
        /// <summary>
        /// Called whenever your packet is recieved.
        /// </summary>
        /// <param name="SenderSteamId"></param>
        public abstract void Received(ulong SenderSteamId);

        public static Type[] Types =
        {
            typeof(PacketBase),
            typeof(SerializedSpawnProjectile),
            typeof(SerializedSyncProjectile),
            typeof(SerializedCloseProjectile),
            typeof(SerializedGuidance),
            typeof(SettingsPacket),
        };
    }
}
