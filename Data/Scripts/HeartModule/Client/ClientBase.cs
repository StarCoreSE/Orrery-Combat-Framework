using Orrery.HeartModule.Client.Networking;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using System;
using Orrery.HeartModule.Client.Interface;
using Orrery.HeartModule.Client.Projectiles;
using Orrery.HeartModule.Client.Weapons;
using Orrery.HeartModule.Client.Weapons.Controls;
using VRage.Game.Components;

namespace Orrery.HeartModule.Client
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class ClientBase : MySessionComponentBase
    {
        private ClientNetwork _network = new ClientNetwork();
        private WeaponManager _weaponManager;
        private ProjectileManager _projectileManager;

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            _network.LoadData();
            BlockCategoryManager.Init();
            _weaponManager = new WeaponManager();
            _projectileManager = new ProjectileManager();

            HeartLog.Info("ClientBase initialized.");
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            BlockCategoryManager.Close();
            _projectileManager.Close();
            _weaponManager.Close();
            _network.UnloadData();

            HeartLog.Info("ClientBase closed.");
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            try
            {
                if (!SorterWeaponTerminalControls.Done)
                {
                    HideSorterControls.DoOnce();
                    SorterWeaponTerminalControls.DoOnce(ModContext);
                }

                _network.Update();
                _projectileManager.Update();
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
