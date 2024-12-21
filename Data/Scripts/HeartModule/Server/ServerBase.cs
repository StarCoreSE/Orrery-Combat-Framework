using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Orrery.HeartModule.Server
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ServerBase : MySessionComponentBase
    {
        private Queue<double> elapsed = new Queue<double>();
        public readonly HashSet<HitscanProjectile> Projectiles = new HashSet<HitscanProjectile>();
        private readonly List<HitscanProjectile> _deadProjectiles = new List<HitscanProjectile>();

        public Dictionary<HitscanProjectile, IMyGps> Gps = new Dictionary<HitscanProjectile, IMyGps>();

        public override void LoadData()
        {

        }

        private int _ticks;
        public override void UpdateAfterSimulation()
        {
            try
            {
                if (Projectiles.Count < 20 && _ticks++ % 30 == 0)
                {
                    PhysicalProjectile p = new PhysicalProjectile(new ProjectileDefinitionBase
                    {
                        PhysicalProjectileDef = new PhysicalProjectileDef
                        {
                            MaxTrajectory = 5000,
                            Velocity = 500,
                            GravityInfluenceMultiplier = 1,
                            MaxLifetime = 15,
                        },
                        DamageDef = new DamageDef
                        {
                            BaseDamage = 1000,
                            AreaDamage = 2000,
                            AreaRadius = 2.5f,
                        }
                    }, Vector3D.Zero, Vector3D.Forward);

                    Projectiles.Add(p);

                    IMyGps g = MyAPIGateway.Session.GPS.Create("Projectile " + Projectiles.Count, "", p.Raycast.From,
                        true, true);
                    Gps.Add(p, g);
                    MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.Player.IdentityId, g);
                }
                //beam.Owner = MyAPIGateway.Session?.Player?.Character;

                Stopwatch watch = Stopwatch.StartNew();

                foreach (var projectile in Projectiles)
                {
                    projectile.UpdateTick(1/60d);
                    if (!projectile.IsActive)
                        _deadProjectiles.Add(projectile);

                    Vector4 color = Color.AliceBlue;
                    MySimpleObjectDraw.DrawLine(projectile.Raycast.From, projectile.Raycast.To,
                        MyStringId.GetOrCompute("WeaponLaser"), ref color, 0.4f);

                    Gps[projectile].Coords = projectile.Raycast.From;
                    MyAPIGateway.Session.GPS.ModifyGps(MyAPIGateway.Session.Player.IdentityId, Gps[projectile]);
                }

                foreach (var deadProjectile in _deadProjectiles)
                {
                    MyAPIGateway.Session.GPS.RemoveGps(MyAPIGateway.Session.Player.IdentityId, Gps[deadProjectile]);
                    Gps.Remove(deadProjectile);
                    Projectiles.Remove(deadProjectile);
                }

                _deadProjectiles.Clear();

                watch.Stop();

                elapsed.Enqueue(watch.ElapsedTicks/(double) TimeSpan.TicksPerMillisecond);
                while (elapsed.Count > 120)
                    elapsed.Dequeue();

                MyAPIGateway.Utilities.ShowNotification($"{elapsed.Sum()/elapsed.Count:N}", 1000/60);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ServerBase));
            }
        }
    }
}
