using System;
using Orrery.HeartModule.Client;
using Orrery.HeartModule.Server;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace Orrery.HeartModule
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, priority: int.MaxValue)]
    internal class MasterSession : MySessionComponentBase
    {
        public static MasterSession I;
        private HeartLog _heartLog;
        private CriticalHandle _criticalHandle;
        private int _ticks;

        public override void LoadData()
        {
            I = this;
            _heartLog = new HeartLog();
            _criticalHandle = new CriticalHandle();
            _criticalHandle.LoadData();
            HeartLog.Info("Logging and exception handling initialized.");

            HeartData.I = new HeartData();
            DefinitionManager.LoadData();

            // TODO: Temporary definition, remove this later.
            DefinitionManager.DefinitionApi.OnReady += () =>
            {
                DefinitionManager.DefinitionApi.RegisterDefinition("testdef", new ProjectileDefinitionBase
                {
                    Name = "testdef",
                    UngroupedDef = new UngroupedDef()
                    {
                        ReloadPowerUsage = 10,
                        Recoil = 5000,
                        Impulse = 5000,
                        ShotsPerMagazine = 100,
                        MagazineItemToConsume = "",
                    },
                    NetworkingDef = new NetworkingDef()
                    {
                        NetworkingMode = NetworkingDef.NetworkingModeEnum.FireEvent,
                        DoConstantSync = false,
                        NetworkPriority = 0,
                    },
                    DamageDef = new DamageDef()
                    {
                        SlimBlockDamageMod = 1,
                        FatBlockDamageMod = 1,
                        BaseDamage = 5000,
                        AreaDamage = 0,
                        AreaRadius = 0,
                        MaxImpacts = 1,
                        DamageToProjectiles = 0.4f,
                        DamageToProjectilesRadius = 0.2f,
                    },
                    PhysicalProjectileDef = new PhysicalProjectileDef()
                    {
                        Velocity = 80,
                        VelocityVariance = 0,
                        Acceleration = 0,
                        Health = 0,
                        MaxTrajectory = 4000,
                        MaxLifetime = -1,
                        IsHitscan = false,
                        GravityInfluenceMultiplier = 0.01f,
                        ProjectileSize = 0.5f,
                    },
                    VisualDef = new VisualDef()
                    {
                        Model = "Models\\Weapons\\Projectile_Missile.mwm",
                        TrailTexture = MyStringId.GetOrCompute("WeaponLaser"),
                        TrailFadeTime = 0.1f,
                        TrailLength = 2,
                        TrailWidth = 0.5f,
                        TrailColor = new VRageMath.Vector4(61, 24, 24, 200),
                        AttachedParticle = "Smoke_Missile",
                        ImpactParticle = "MaterialHit_Metal",
                        VisibleChance = 1f,
                    },
                    AudioDef = new ProjectileAudioDef()
                    {
                        TravelSound = "",
                        TravelVolume = 100,
                        TravelMaxDistance = 1000,
                        ImpactSound = "WepSmallWarheadExpl",
                        SoundChance = 1f,
                    },
                    Guidance = new GuidanceDef[]
                    {
                    }
                });
            };

            HeartLog.Info("[MasterSession] finished LoadData.");
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                HeartData.I.IsPaused = false;
                _criticalHandle.Update();

                // Get players
                if (_ticks % 10 == 0 && MyAPIGateway.Session.IsServer)
                {
                    HeartData.I.Players.Clear(); // KEEN DOESN'T. CLEAR. THE LIST. AUTOMATICALLY. AUGH. -aristeas
                    MyAPIGateway.Multiplayer.Players.GetPlayers(HeartData.I.Players);
                }

                _ticks++;
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(MasterSession));
            }
        }

        protected override void UnloadData()
        {
            HeartLog.Info("[MasterSession] Begin UnloadData.");

            DefinitionManager.UnloadData();

            _criticalHandle.UnloadData();
            _heartLog.Close();
            I = null;
        }

        public override void UpdatingStopped()
        {
            HeartData.I.IsPaused = true;
        }
    }
}
