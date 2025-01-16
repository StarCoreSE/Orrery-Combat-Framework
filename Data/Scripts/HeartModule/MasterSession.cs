using System;
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
            HeartData.I = new HeartData();

            _heartLog = new HeartLog();
            _criticalHandle = new CriticalHandle();
            _criticalHandle.LoadData();
            HeartLog.Info("Logging and exception handling initialized.");

            DefinitionManager.LoadData();

            HeartLog.Info("[MasterSession] finished LoadData.");
            MyLog.Default.WriteLineAndConsole("\n========================================\nOrrery Combat Framework initialized - check [\\Storage\\3130655435.sbm_HeartModule\\debug.log] for logs.\n========================================");
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
