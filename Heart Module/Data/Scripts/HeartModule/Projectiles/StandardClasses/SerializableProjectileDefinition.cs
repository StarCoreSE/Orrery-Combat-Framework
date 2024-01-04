﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses
{
    /// <summary>
    /// Standard serializable projectile definition.
    /// </summary>
    [ProtoContract]
    public class SerializableProjectileDefinition
    {
        public SerializableProjectileDefinition() { }

        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public Ungrouped Ungrouped;
        [ProtoMember(3)] public Damage Damage;
        [ProtoMember(4)] public PhysicalProjectile PhysicalProjectile;
        [ProtoMember(5)] public Visual Visual;
        [ProtoMember(5)] public Audio Audio;
        [ProtoMember(6)] public Guidance[] Guidance;
        [ProtoMember(7)] public LiveMethods LiveMethods = new LiveMethods();
    }

    [ProtoContract]
    public struct Ungrouped
    {
        /// <summary>
        /// Power draw during reload, in MW
        /// </summary>
        [ProtoMember(1)] public float ReloadPowerUsage;
        /// <summary>
        /// Length of projectile, in Meters. For beams, range.
        /// </summary>
        [ProtoMember(2)] public float Length;
        /// <summary>
        /// Recoil of projectile, in Newtons
        /// </summary>
        [ProtoMember(3)] public int Recoil;
        /// <summary>
        /// Impulse of projectile, in Newtons
        /// </summary>
        [ProtoMember(4)] public int Impulse;
    }

    [ProtoContract]
    public struct Damage
    {
        [ProtoMember(1)] public float SlimBlockDamageMod;
        [ProtoMember(2)] public float FatBlockDamageMod;
        [ProtoMember(3)] public float BaseDamage;
        [ProtoMember(4)] public float AreaDamage;
        [ProtoMember(5)] public int MaxImpacts;
    }

    /// <summary>
    /// Projectile information for non-hitscan projectiles.
    /// </summary>
    [ProtoContract]
    public struct PhysicalProjectile
    {
        [ProtoMember(1)] public float Speed;
        [ProtoMember(2)] public float Acceleration;
        [ProtoMember(3)] public float Health;
        [ProtoMember(4)] public float MaxTrajectory;
        [ProtoMember(5)] public float MaxLifetime;
    }

    [ProtoContract]
    public struct Visual
    {
        [ProtoMember(1)] public string Model;
        [ProtoMember(2)] public string TrailTexture;
        [ProtoMember(3)] public float  TrailFadeTime;
        [ProtoMember(4)] public string AttachedParticle;
        [ProtoMember(5)] public string ImpactParticle;
        [ProtoMember(6)] public float  VisibleChance;
    }

    [ProtoContract]
    public struct Audio
    {
        [ProtoMember(1)] public string TravelSound;
        [ProtoMember(2)] public string ImpactSound;
        [ProtoMember(3)] public float  ImpactSoundChance;
    }

    [ProtoContract]
    public struct Guidance
    {
        [ProtoMember(1)] public int TriggerTime;
        [ProtoMember(2)] public bool UseAimPrediction;
        [ProtoMember(3)] public float TurnRate;
        [ProtoMember(4)] public float TurnRateSpeedRatio;
        [ProtoMember(5)] public int IFF; // 1 is TargetSelf, 2 is TargetEnemies, 4 is TargetFriendlies
        [ProtoMember(6)] public bool DoRaycast;
        [ProtoMember(7)] public float CastCone;
    }

    [ProtoContract]
    public class LiveMethods
    {
        [ProtoMember(1)] public bool DoOnShoot;
        [ProtoMember(2)] public bool DoOnImpact;
        [ProtoMember(3)] public bool DoUpdate1;

        // TODO move to definition, and seperate
        Dictionary<string, Delegate> liveMethods = new Dictionary<string, Delegate>()
        {
            ["OnShoot"] = new Action<uint, MyEntity>(BaseOnShoot),
            ["OnImpact"] = new Action<uint, MyEntity, MyEntity, bool>(BaseOnImpact),
            ["Update1"] = new Action<uint, MyEntity>(BaseUpdate1),
        };

        private static void BaseOnShoot(uint ProjectileId, MyEntity Shooter) { }
        private static void BaseOnImpact(uint ProjectileId, MyEntity Shooter, MyEntity ImpactEntity, bool EndOfLife) { }
        private static void BaseUpdate1(uint ProjectileId, MyEntity Shooter) { }
    }
}