using ProtoBuf;
using VRageMath;

namespace Orrery.HeartModule.Shared.Networking
{
    /// <summary>
    /// Used to keep already-active projectiles in sync.
    /// </summary>
    [ProtoContract]
    internal class SerializedSyncProjectile : PacketBase
    {
        [ProtoMember(1)] public uint Id;
        [ProtoMember(2)] public Vector3D Position;
        [ProtoMember(3)] public Vector3 Direction;
        [ProtoMember(4, IsRequired = false)] public Vector3 Velocity;
        public override void Received(ulong SenderSteamId)
        {
            Client.Projectiles.ProjectileManager.NetUpdateProjectile(this);
        }
    }

    /// <summary>
    /// Used when spawning projectiles.
    /// </summary>
    [ProtoContract]
    internal class SerializedSpawnProjectile : PacketBase
    {
        [ProtoMember(1)] public ushort DefinitionId;
        [ProtoMember(2)] public uint Id;
        [ProtoMember(3)] public Vector3D Position;
        [ProtoMember(4)] public Vector3 Direction;
        [ProtoMember(5, IsRequired = false)] public Vector3 Velocity = Vector3.NegativeInfinity;
        [ProtoMember(6)] public long OwnerId;
        public override void Received(ulong SenderSteamId)
        {
            Client.Projectiles.ProjectileManager.NetSpawnProjectile(this);
        }
    }

    /// <summary>
    /// Used when closing projectiles.
    /// </summary>
    [ProtoContract]
    internal class SerializedCloseProjectile : PacketBase
    {
        [ProtoMember(1)] public uint Id;
        [ProtoMember(2)] public Vector3D Position;
        [ProtoMember(3)] public bool DidImpact;
        public override void Received(ulong SenderSteamId)
        {
            Client.Projectiles.ProjectileManager.NetCloseProjectile(this);
        }
    }
}
