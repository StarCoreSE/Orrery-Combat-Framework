﻿using System;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;

namespace Orrery.HeartModule.Server
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ServerBase : MySessionComponentBase
    {
        public static ServerBase I;
        private ServerNetwork _network = new ServerNetwork();
        private ProjectileManager _projectileManager = new ProjectileManager();

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            
            I = this;
            _network.LoadData();

            HeartLog.Info("ServerBase initialized.");
        }

        protected override void UnloadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            _network.UnloadData();
            I = null;

            HeartLog.Info("ServerBase closed.");
        }

        private int _ticks;
        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            try
            {
                _network.Update();
                _projectileManager.Update();

                if (_projectileManager.ActiveProjectiles < 20 && _ticks++ % 30 == 0)
                {
                    PhysicalProjectile p = new PhysicalProjectile(DefinitionManager.ProjectileDefinitions["testdef"], Vector3D.Zero, Vector3D.Forward);

                    _projectileManager.SpawnProjectile(p);
                }
                //beam.Owner = MyAPIGateway.Session?.Player?.Character;

                MyAPIGateway.Utilities.ShowNotification($"Server: {_projectileManager.ActiveProjectiles}", 1000/60);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ServerBase));
            }
        }
    }
}
