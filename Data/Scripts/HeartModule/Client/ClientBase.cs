using Orrery.HeartModule.Client.Networking;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using System;
using Orrery.HeartModule.Client.Interface;
using Orrery.HeartModule.Client.Projectiles;
using VRage.Game.Components;

namespace Orrery.HeartModule.Client
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ClientBase : MySessionComponentBase
    {
        private ClientNetwork _network = new ClientNetwork();
        private ProjectileManager _projectileManager = new ProjectileManager();

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            _network.LoadData();
            BlockCategoryManager.Init();

            HeartLog.Info("ClientBase initialized.");
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            BlockCategoryManager.Close();
            _projectileManager.Close();
            _network.UnloadData();

            HeartLog.Info("ClientBase closed.");
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            try
            {
                _network.Update();
                _projectileManager.Update();
                MyAPIGateway.Utilities.ShowNotification($"Client: {ProjectileManager.ActiveProjectiles}", 1000/60);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ClientBase));
            }
        }

        public override void Draw()
        {
            _projectileManager.UpdateDraw();
        }
    }
}
