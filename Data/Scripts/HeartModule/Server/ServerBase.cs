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
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
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

            try
            {
                I = this;
                _network.LoadData();
                _gridTargetingManager = new GridTargetingManager();
                _weaponManager = new WeaponManager();
                _projectileManager = new ProjectileManager();

                HeartLog.Info("ServerBase initialized.");
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ServerBase));
                throw;
            }
        }

        protected override void UnloadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            try
            {
                _network.UnloadData();
                _projectileManager.Close();
                _weaponManager.Close();
                _gridTargetingManager.Close();
                I = null;

                HeartLog.Info("ServerBase closed.");
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ServerBase));
                throw;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            try
            {
                _weaponManager.UpdateBeforeSimulation();
                _projectileManager.UpdateBeforeSimulation();
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ServerBase));
            }
        }

        private int _ticks;
        public override void UpdateAfterSimulation()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            try
            {
                _network.Update();
                _projectileManager.UpdateAfterSimulation();
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
