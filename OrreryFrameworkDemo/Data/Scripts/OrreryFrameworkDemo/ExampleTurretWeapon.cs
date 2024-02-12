﻿using OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication.WeaponBases;
using System;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    partial class HeartDefinitions
    {
        WeaponDefinitionBase ExampleTurretWeapon => new WeaponDefinitionBase()
        {
            Targeting = new Targeting()
            {
                MaxTargetingRange = 1000,
                MinTargetingRange = 0,
                CanAutoShoot = true,
                RetargetTime = 0,
                AimTolerance = 0.0175f,
                DefaultIFF = IFF_Enum.TargetEnemies | IFF_Enum.TargetNeutrals,
                AllowedTargetTypes = TargetType_Enum.TargetGrids | TargetType_Enum.TargetCharacters,
            },
            Assignments = new Assignments()
            {
                BlockSubtype = "OCF_ExampleTurretWeapon",
                MuzzleSubpart = "reshephbarrels",
                ElevationSubpart = "reshephbarrels",
                AzimuthSubpart = "reshephtop",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle_projectile_1",
                },
            },
            Hardpoint = new Hardpoint()
            {
                AzimuthRate = 0.5f,
                ElevationRate = 0.5f,
                MaxAzimuth = (float)Math.PI,
                MinAzimuth = (float)-Math.PI,
                MaxElevation = (float)Math.PI,
                MinElevation = -0.1745f,
                HomeAzimuth = 0,
                HomeElevation = 0,
                IdlePower = 10,
                ShotInaccuracy = 0.0025f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                Ammos = new string[]
                {
                    ExampleAmmoProjectile.Name,
                },

                RateOfFire = 10,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 6,
                DelayUntilFire = 2,
                MagazinesToLoad = 1,

                MaxReloads = -1,
            },
            Audio = new Audio()
            {
                PreShootSound = "ArcWepRailgunLargeCharge",
                ShootSound = "PunisherNewFire",
                ReloadSound = "PunisherNewReload",
                RotationSound = "WepTurretGatlingRotate",
            },
            Visuals = new Visuals()
            {
                ShootParticle = "Muzzle_Flash_Autocannon",
                ContinuousShootParticle = false,
                ReloadParticle = "",
            },
        };
    }
}
