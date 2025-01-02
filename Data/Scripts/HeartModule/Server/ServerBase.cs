using System;
using Orrery.HeartModule.Server.GridTargeting;
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
        private ProjectileManager _projectileManager;
        private WeaponManager _weaponManager;
        private GridTargetingManager _gridTargetingManager;

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            
            I = this;
            _network.LoadData();
            _weaponManager = new WeaponManager();
            _projectileManager = new ProjectileManager();
            _gridTargetingManager = new GridTargetingManager();

            HeartLog.Info("ServerBase initialized.");
        }

        protected override void UnloadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            _network.UnloadData();
            _projectileManager.Close();
            _weaponManager.Close();
            _gridTargetingManager.Close();
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
                _gridTargetingManager.Update();

                MyAPIGateway.Utilities.ShowNotification($"Server: {ProjectileManager.ActiveProjectiles}", 1000/60);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ServerBase));
            }
        }
    }
}
