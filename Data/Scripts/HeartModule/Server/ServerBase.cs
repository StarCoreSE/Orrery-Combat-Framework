using System;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Server.Weapons;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Orrery.HeartModule.Server
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ServerBase : MySessionComponentBase
    {
        public static ServerBase I;
        private ServerNetwork _network = new ServerNetwork();
        private ProjectileManager _projectileManager = new ProjectileManager();
        private WeaponManager _weaponManager;

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            
            I = this;
            _network.LoadData();
            _weaponManager = new WeaponManager();

            HeartLog.Info("ServerBase initialized.");
        }

        protected override void UnloadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            _network.UnloadData();
            _projectileManager.Close();
            _weaponManager.Close();
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

                MyAPIGateway.Utilities.ShowNotification($"Server: {ProjectileManager.ActiveProjectiles}", 1000/60);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ServerBase));
            }
        }
    }
}
