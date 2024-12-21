using ProtoBuf;
using VRageMath;

namespace Orrery.HeartModule.Server.Projectiles
{
    /// <summary>
    /// Used to keep already-active projectiles in sync.
    /// </summary>
    [ProtoContract]
    internal class SerializedSyncProjectile
    {
        [ProtoMember(1)] public uint Id;
        [ProtoMember(2)] public Vector3D Position;
        [ProtoMember(3)] public Vector3 Direction;
        [ProtoMember(4, IsRequired = false)] public Vector3 Velocity;
    }

    /// <summary>
    /// Used when spawning projectiles.
    /// </summary>
    [ProtoContract]
    internal class SerializedSpawnProjectile
    {
        [ProtoMember(1)] public ushort DefinitionId;
        [ProtoMember(2)] public uint Id;
        [ProtoMember(3)] public Vector3D Position;
        [ProtoMember(4)] public Vector3 Direction;
        [ProtoMember(5, IsRequired = false)] public Vector3 Velocity;
        [ProtoMember(6)] public long OwnerId;
    }

    /// <summary>
    /// Used when closing projectiles.
    /// </summary>
    [ProtoContract]
    internal class SerializedCloseProjectile
    {
        [ProtoMember(1)] public uint Id;
        [ProtoMember(2)] public Vector3D Position;
    }
}
