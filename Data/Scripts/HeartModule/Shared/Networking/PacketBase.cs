using ProtoBuf;
using System;

namespace Orrery.HeartModule.Shared.Networking
{
    [ProtoInclude(1, typeof(SerializedSpawnProjectile))]
    [ProtoInclude(2, typeof(SerializedSyncProjectile))]
    [ProtoInclude(3, typeof(SerializedCloseProjectile))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract partial class PacketBase
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
        };
    }
}
