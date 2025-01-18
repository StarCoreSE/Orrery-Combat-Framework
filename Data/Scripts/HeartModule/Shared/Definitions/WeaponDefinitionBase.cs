using ProtoBuf;
using Sandbox.Game.Entities;
using System;
using System.Linq;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.ModAPI;

// ReSharper disable UnassignedField.Global
namespace Orrery.HeartModule.Shared.Definitions
{
    /// <summary>
    /// Standard serializable weapon definition.
    /// </summary>
    [ProtoContract(UseProtoMembersOnly = true)]
    public class WeaponDefinitionBase
    {
        public WeaponDefinitionBase()
        {
            //DefinitionSender.WeaponDefinitions.Add(this);
        }

        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public Targeting Targeting;
        [ProtoMember(3)] public Assignments Assignments;
        [ProtoMember(4)] public Hardpoint Hardpoint;
        [ProtoMember(5)] public Loading Loading;
        [ProtoMember(6)] public Audio Audio;
        [ProtoMember(7)] public Visuals Visuals;

        public WeaponLiveMethods LiveMethods = new WeaponLiveMethods();

        public bool IsTurret => Assignments.HasAzimuth && Assignments.HasElevation;
        public bool IsSmart => IsTurret || Loading.Ammos.Any(ammo => DefinitionManager.ProjectileDefinitions[ammo].Guidance.Length > 0);
    }

    [ProtoContract]
    public struct Targeting
    {
        /// <summary>
        /// The furthest target a turret can shoot.
        /// </summary>
        [ProtoMember(1)] public float MaxTargetingRange;
        /// <summary>
        /// The closest target a turret can shoot.
        /// </summary>
        [ProtoMember(2)] public float MinTargetingRange;
        /// <summary>
        /// Can the turret fire by itself? Tracks regardless.
        /// </summary>
        [ProtoMember(3)] public bool CanAutoShoot;
        /// <summary>
        /// Default terminal IFF settings
        /// </summary>
        [ProtoMember(4)] public IFFEnum DefaultIff;
        /// <summary>
        /// Targets this weapon is allowed to fire on.
        /// </summary>
        [ProtoMember(5)] public TargetTypeEnum AllowedTargetTypes;
        /// <summary>
        /// Time until the turret is forced to find a new target
        /// </summary>
        [ProtoMember(6)] public float RetargetTime;
        /// <summary>
        /// Maximum target angle difference in radians for autofiring.
        /// </summary>
        [ProtoMember(7)] public float AimTolerance;
    }

    [ProtoContract]
    public struct Assignments
    {
        [ProtoMember(1)] public string BlockSubtype;
        [ProtoMember(2)] public string MuzzleSubpart;
        [ProtoMember(3)] public string ElevationSubpart;
        [ProtoMember(4)] public string AzimuthSubpart;
        [ProtoMember(5)] public float DurabilityModifier;
        [ProtoMember(6)] public string InventoryIconName;
        [ProtoMember(7)] public string[] Muzzles;

        public bool HasElevation => !ElevationSubpart?.Equals("") ?? false;
        public bool HasAzimuth => !AzimuthSubpart?.Equals("") ?? false;
        public bool HasMuzzleSubpart => !MuzzleSubpart?.Equals("") ?? false;
    }

    [ProtoContract]
    public struct Hardpoint
    {
        // ALL VALUES IN RADIANS
        [ProtoMember(1)] public float AzimuthRate;
        [ProtoMember(2)] public float ElevationRate;
        [ProtoMember(3)] public float MaxAzimuth;
        [ProtoMember(4)] public float MinAzimuth;
        [ProtoMember(5)] public float MaxElevation;
        [ProtoMember(6)] public float MinElevation;
        [ProtoMember(7)] public float IdlePower;
        [ProtoMember(8)] public float ShotInaccuracy;
        [ProtoMember(9)] public bool LineOfSightCheck;
        [ProtoMember(10)] public bool ControlRotation;

        [ProtoMember(11)] public float HomeAzimuth;
        [ProtoMember(12)] public float HomeElevation;

        public bool CanRotateFull => MaxAzimuth >= -(float)Math.PI && MinAzimuth <= -(float)Math.PI;
        public bool CanElevateFull => MaxElevation >= -(float)Math.PI && MinElevation <= -(float)Math.PI;
    }

    [ProtoContract]
    public struct Loading
    {
        [ProtoMember(10)] public string[] Ammos;

        [ProtoMember(1)] public int RateOfFire; // Shots per second
        [ProtoMember(2)] public int BarrelsPerShot;
        [ProtoMember(3)] public int ProjectilesPerBarrel;
        [ProtoMember(4)] public float ReloadTime; // Seconds
        [ProtoMember(6)] public int MagazinesToLoad; // Like an autoloader clip.
        /// <summary>
        /// The maximum number of times the gun can reload.
        /// </summary>
        [ProtoMember(7)] public int MaxReloads;
        [ProtoMember(8)] public float DelayUntilFire; // Seconds
        [ProtoMember(9)] public Resource[] Resources; // TODO
        [ProtoMember(11)] public float RateOfFireVariance; // +- in variance of ROF

        [ProtoContract]
        public struct Resource // TODO
        {
            [ProtoMember(1)] public string ResourceType;
            [ProtoMember(2)] public float ResourceGeneration; // Per second
            [ProtoMember(3)] public float ResourceStorage;
            [ProtoMember(4)] public float ResourcePerShot;
            [ProtoMember(5)] public float MinResourceBeforeFire;
            // TODO: Action OnConsume
        }
    }

    [ProtoContract]
    public struct Audio
    {
        [ProtoMember(1)] public string PreShootSound;
        [ProtoMember(2)] public string ShootSound;
        [ProtoMember(3)] public string ReloadSound;
        [ProtoMember(4)] public string RotationSound;

        public MySoundPair RotationSoundPair => new MySoundPair(RotationSound);

    }

    [ProtoContract]
    public struct Visuals
    {
        [ProtoMember(1)] public string ShootParticle;
        [ProtoMember(2)] public bool ContinuousShootParticle; // TODO
        [ProtoMember(3)] public string ReloadParticle; // TODO

        public bool HasShootParticle => !ShootParticle?.Equals("") ?? false;
        public bool HasReloadParticle => !ReloadParticle?.Equals("") ?? false;
    }

    public class WeaponLiveMethods
    {
        /// <summary>
        /// Invoked when a weapon fires. Only runs on the server.
        /// <para>
        ///     Arguments: Weapon, NewProjectileId
        /// </para>
        /// </summary>
        public Action<IMyConveyorSorter, uint> ServerOnShoot = null;
        /// <summary>
        /// Invoked when a weapon's target changes. Either TargetEntity or TargetProjectile will have a value. Only runs on the server.
        /// <para>
        ///     Arguments: Weapon, TargetEntity (optional), TargetProjectile (optional)
        /// </para>
        /// </summary>
        public Action<IMyConveyorSorter, IMyEntity, uint?> ServerOnRetarget = null;
        /// <summary>
        /// Invoked when a weapon reloads. Only runs on the server.
        /// <para>
        ///     Arguments: Weapon, AmmoIndex
        /// </para>
        /// </summary>
        public Action<IMyConveyorSorter, byte> ServerOnReload = null;
        /// <summary>
        /// Invoked when a new weapon is placed. Only runs on the server.
        /// <para>
        ///     Arguments: Weapon
        /// </para>
        /// </summary>
        public Action<IMyConveyorSorter> ServerOnPlace = null;

        /// <summary>
        /// Invoked when a weapon fires. Only runs on the client.
        /// <para>
        ///     Arguments: Weapon, NewProjectileId
        /// </para>
        /// </summary>
        public Action<IMyConveyorSorter> ClientOnShoot = null;
        /// <summary>
        /// Invoked when a weapon's target changes. Either TargetEntity or TargetProjectile will have a value. Only runs on the client.
        /// <para>
        ///     Arguments: Weapon, TargetEntity (optional), TargetProjectile (optional)
        /// </para>
        /// </summary>
        public Action<IMyConveyorSorter, IMyEntity, uint?> ClientOnRetarget = null;
        ///// <summary>
        ///// Invoked when a weapon reloads. Only runs on the client.
        ///// <para>
        /////     Arguments: Weapon, AmmoIndex
        ///// </para>
        ///// </summary>
        //public Action<IMyConveyorSorter, byte> ClientOnReload = null;
        /// <summary>
        /// Invoked when a new weapon is placed. Only runs on the client.
        /// <para>
        ///     Arguments: Weapon
        /// </para>
        /// </summary>
        public Action<IMyConveyorSorter> ClientOnPlace = null;

        public static explicit operator Dictionary<string, Delegate>(WeaponLiveMethods methods)
        {
            return new Dictionary<string, Delegate>
            {
                ["Server OnShoot"] = methods.ServerOnShoot,
                ["Server OnRetarget"] = methods.ServerOnRetarget,
                ["Server OnReload"] = methods.ServerOnReload,
                ["Server OnPlace"] = methods.ServerOnPlace,
                
                ["Client OnShoot"] = methods.ClientOnShoot,
                ["Client OnRetarget"] = methods.ClientOnRetarget,
                //["Client OnReload"] = methods.ClientOnReload,
                ["Client OnPlace"] = methods.ClientOnPlace,
            };
        }

        public static explicit operator WeaponLiveMethods(Dictionary<string, Delegate> map)
        {
            return new WeaponLiveMethods
            {
                ServerOnShoot = map.GetValueOrDefault("Server OnShoot", null) as Action<IMyConveyorSorter, uint>,
                ServerOnRetarget = map.GetValueOrDefault("Server OnRetarget", null) as Action<IMyConveyorSorter, IMyEntity, uint?>,
                ServerOnReload = map.GetValueOrDefault("Server OnReload", null) as Action<IMyConveyorSorter, byte>,
                ServerOnPlace = map.GetValueOrDefault("Server OnPlace", null) as Action<IMyConveyorSorter>,

                ClientOnShoot = map.GetValueOrDefault("Client OnShoot", null) as Action<IMyConveyorSorter>,
                ClientOnRetarget = map.GetValueOrDefault("Client OnRetarget", null) as Action<IMyConveyorSorter, IMyEntity, uint?>,
                //ClientOnReload = map.GetValueOrDefault("Client OnReload", null) as Action<IMyConveyorSorter, byte>,
                ClientOnPlace = map.GetValueOrDefault("Client OnPlace", null) as Action<IMyConveyorSorter>,
            };
        }
    }
}
