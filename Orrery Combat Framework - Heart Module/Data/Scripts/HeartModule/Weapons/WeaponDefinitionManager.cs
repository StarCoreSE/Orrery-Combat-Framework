﻿using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons
{
    internal class WeaponDefinitionManager
    {
        private static SerializableWeaponDefinition DefaultDefinition = new SerializableWeaponDefinition()
        {
            Assignments = new Assignments()
            {
                BlockSubtype = "TestWeapon",
                MuzzleSubpart = "",
                ElevationSubpart = "",
                AzimuthSubpart = "",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle01",
                },
            },
            Hardpoint = new Hardpoint()
            {
                AzimuthRate = 0.01f,
                ElevationRate = 0.01f,
                MaxAzimuth = (float)Math.PI / 2,
                MinAzimuth = (float)-Math.PI / 2,
                MaxElevation = (float)Math.PI / 4,
                MinElevation = 0,
                IdlePower = 0,
                ShotInaccuracy = (float) Math.PI,
                AimTolerance = 0.1f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                RateOfFire = 60,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 0,
                MagsToLoad = 1,
                DelayUntilFire = 0,
            },
        };

        private static SerializableWeaponDefinition TurretDefinition = new SerializableWeaponDefinition()
        {
            Assignments = new Assignments()
            {
                BlockSubtype = "TestWeaponTurret",
                MuzzleSubpart = "TestEv",
                ElevationSubpart = "TestEv",
                AzimuthSubpart = "TestAz",
                DurabilityModifier = 1,
                InventoryIconName = "",
                Muzzles = new string[]
                {
                    "muzzle01",
                },
            },
            Hardpoint = new Hardpoint()
            {
                AzimuthRate = 0.1f,
                ElevationRate = 0.1f,
                MaxAzimuth = (float)Math.PI, // /2,
                MinAzimuth = (float)-Math.PI, // /2,
                MaxElevation = (float)Math.PI, // /4,
                MinElevation = 0,
                IdlePower = 0,
                ShotInaccuracy = 0,//0.0175f,
                AimTolerance = 0.1f,
                LineOfSightCheck = true,
                ControlRotation = true,
            },
            Loading = new Loading()
            {
                RateOfFire = 60,
                BarrelsPerShot = 1,
                ProjectilesPerBarrel = 1,
                ReloadTime = 0,
                MagsToLoad = 1,
                DelayUntilFire = 0,
            },
            Visuals = new Visuals()
            {
                ShootParticle = "",//"BlockDestroyedExplosion_Small",
                ContinuousShootParticle = false,
                ReloadParticle = "",
    },
        };

        // this is after the definitions because FUCKING STATICS ARE THE WORK OF THE DEVIL
        private static Dictionary<string, SerializableWeaponDefinition> Definitions = new Dictionary<string, SerializableWeaponDefinition>()
        {
            ["TestWeapon"] = DefaultDefinition,
            ["TestWeaponTurret"] = TurretDefinition,
        };

        public static SerializableWeaponDefinition GetDefinition(string subTypeId)
        {
            MyLog.Default.WriteLine(subTypeId + " | " + HasDefinition(subTypeId) + " | " + (Definitions[subTypeId] == null));
            if (HasDefinition(subTypeId))
                return Definitions[subTypeId];
            return null;
        }

        public static bool HasDefinition(string subTypeId)
        {
            return Definitions.ContainsKey(subTypeId);
        }
    }
}