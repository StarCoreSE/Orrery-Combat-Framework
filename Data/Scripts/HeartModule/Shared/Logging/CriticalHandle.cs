using System;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Utils;

namespace Orrery.HeartModule.Shared.Logging
{

    public class CriticalHandle
    {
        const int WarnTimeSeconds = 20;
        private static CriticalHandle _;
        private long _criticalCloseTime = -1;
        private Exception _exception;

        public void LoadData()
        {
            _ = this;
        }

        public void Update()
        {
            if (_criticalCloseTime == -1)
                return;
            double secondsRemaining = Math.Round((_criticalCloseTime - DateTime.UtcNow.Ticks) / (double)TimeSpan.TicksPerSecond, 1);

            if (secondsRemaining <= 0)
            {
                _criticalCloseTime = -1;
                if (!MyAPIGateway.Utilities.IsDedicated)
                    MyVisualScriptLogicProvider.SessionClose(1000, false, true);
                else
                {
                    //throw Exception;
                    MyAPIGateway.Session.Unload(); // This might cause improver unloading
                    MyAPIGateway.Session.UnloadDataComponents();
                }

            }

            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification($"HeartMod CRITICAL ERROR - Shutting down in {secondsRemaining}s", 1000 / 60);
        }

        public void UnloadData()
        {
            _ = null;
        }

        public static void ThrowCriticalException(Exception ex, Type callingType, ulong callerId = ulong.MaxValue)
        {
            _?.m_ThrowCriticalException(ex, callingType, callerId);
        }

        private void m_ThrowCriticalException(Exception ex, Type callingType, ulong callerId = ulong.MaxValue)
        {
            HeartLog.Debug("Start Throw Critical Exception " + _criticalCloseTime);
            if (_criticalCloseTime != -1)
                return;

            _exception = ex;
            HeartLog.Exception(ex, callingType);
            MyAPIGateway.Utilities.ShowMessage("HeartMod", $"CRITICAL ERROR - Shutting down in {WarnTimeSeconds} seconds.");
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Utilities.SendMessage($"HeartMod CRITICAL ERROR - Shutting down in {WarnTimeSeconds} seconds.");
            HeartLog.Info($"HeartMod: CRITICAL ERROR - Shutting down in {WarnTimeSeconds} seconds.");
            _criticalCloseTime = DateTime.UtcNow.Ticks + WarnTimeSeconds * TimeSpan.TicksPerSecond;
        }
    }
}
