using System;
using Orrery.HeartModule.Client;
using Orrery.HeartModule.Server;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Orrery.HeartModule
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, priority: int.MaxValue)]
    internal class MasterSession : MySessionComponentBase
    {
        public static MasterSession I;
        private HeartLog _heartLog;
        private CriticalHandle _criticalHandle;

        public override void LoadData()
        {
            I = this;
            _heartLog = new HeartLog();
            _criticalHandle = new CriticalHandle();
            _criticalHandle.LoadData();
            HeartLog.Info("Logging and exception handling initialized.");

            DefinitionManager.LoadData();

            HeartLog.Info("[MasterSession] finished LoadData.");
        }

        public override void UpdateAfterSimulation()
        {
            _criticalHandle.Update();
        }

        protected override void UnloadData()
        {
            HeartLog.Info("[MasterSession] Begin UnloadData.");

            DefinitionManager.UnloadData();

            _criticalHandle.UnloadData();
            _heartLog.Close();
            I = null;
        }
    }
}
