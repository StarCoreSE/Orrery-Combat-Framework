﻿using Heart_Module.Data.Scripts.HeartModule.Network;
using ProtoBuf;
using Sandbox.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses
{
    /// <summary>
    /// Used for syncing between server and clients, and in the API.
    /// </summary>
    [ProtoContract]
    public class n_SerializableProjectile : PacketBase
    {
        // ProtoMember IDs are high to avoid collisions
        [ProtoMember(22)] public bool? IsActive = true;
        [ProtoMember(23)] public uint Id;
        [ProtoMember(211)] public uint TimestampFromMidnight; // surely this will not bite me in the ass later

        // All non-required values are nullable
        [ProtoMember(24)] public int? DefinitionId;
        [ProtoMember(25)] public Vector3D? Position;
        [ProtoMember(26)] public Vector3? Direction;
        [ProtoMember(27)] public Vector3? InheritedVelocity;
        [ProtoMember(28)] public float? Velocity;
        //[ProtoMember(29)] public int? RemainingImpacts;
        //[ProtoMember(210)] public Dictionary<string, byte[]> OverridenValues;
        [ProtoMember(212)] public long? Firer;


        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            ProjectileManager.I.UpdateProjectileSync(this);
        }
    }
}
