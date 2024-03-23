using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using VRage.Utils;

namespace Heart_Module.Data.Scripts.HeartModule.ExceptionHandler
{

    public class CriticalHandle
    {
        const int WarnTimeSeconds = 20;
        private static CriticalHandle I;
        private long CriticalCloseTime = -1;
        private Exception Exception;

        public void LoadData()
        {
            I = this;
        }

        public void Update()
        {
            if (CriticalCloseTime == -1)
                return;
            double secondsRemaining = Math.Round((CriticalCloseTime - DateTime.UtcNow.Ticks) / (double)TimeSpan.TicksPerSecond, 1);

            if (secondsRemaining <= 0)
            {
                CriticalCloseTime = -1;
                if (!MyAPIGateway.Utilities.IsDedicated)
                    MyVisualScriptLogicProvider.SessionClose(1000, false, true);
                else
                {
                    //throw Exception;
                    HeartLoad.I.UnloadDataConditional();
                    MyAPIGateway.Session.Unload(); // This might cause improver unloading
                }

            }

            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification($"HeartMod CRITICAL ERROR - Shutting down in {secondsRemaining}s", 1000 / 60);
        }

        public void UnloadData()
        {
            I = null;
        }

        public static void ThrowCriticalException(Exception ex, Type callingType, ulong callerId = ulong.MaxValue)
        {
            I?.m_ThrowCriticalException(ex, callingType, callerId);
        }

        public static void ThrowCriticalException(n_SerializableError ex, Type callingType, ulong callerId = ulong.MaxValue)
        {
            I?.m_ThrowCriticalException(ex, callingType, callerId);
        }

        private void m_ThrowCriticalException(Exception ex, Type callingType, ulong callerId = ulong.MaxValue)
        {
            HeartData.I.IsSuspended = true;
            HeartLog.Log("Start Throw Critical Exception " + CriticalCloseTime);
            if (CriticalCloseTime != -1)
                return;

            Exception = ex;
            HeartLog.LogException(ex, callingType, (callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "") + "Critical ");
            MyAPIGateway.Utilities.ShowMessage("HeartMod", $"CRITICAL ERROR - Shutting down in {WarnTimeSeconds} seconds.");
            MyLog.Default.WriteLineAndConsole($"HeartMod: CRITICAL ERROR - Shutting down in {WarnTimeSeconds} seconds.");
            CriticalCloseTime = DateTime.UtcNow.Ticks + WarnTimeSeconds * TimeSpan.TicksPerSecond;

            if (MyAPIGateway.Session.IsServer)
                HeartData.I.Net.SendToEveryone(new n_SerializableError(Exception, true));
        }

        private void m_ThrowCriticalException(n_SerializableError ex, Type callingType, ulong callerId = ulong.MaxValue)
        {
            HeartData.I.IsSuspended = true;
            HeartLog.Log("Start Throw Critical Exception " + CriticalCloseTime);
            if (CriticalCloseTime != -1)
                return;

            Exception = new Exception(ex.ExceptionMessage);
            HeartLog.LogException(ex, callingType, (callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "") + "Critical ");
            MyAPIGateway.Utilities.ShowMessage("HeartMod", $"CRITICAL ERROR - Shutting down in {WarnTimeSeconds} seconds.");
            MyLog.Default.WriteLineAndConsole($"HeartMod: CRITICAL ERROR - Shutting down in {WarnTimeSeconds} seconds.");
            CriticalCloseTime = DateTime.UtcNow.Ticks + WarnTimeSeconds * TimeSpan.TicksPerSecond;

            if (MyAPIGateway.Session.IsServer)
                HeartData.I.Net.SendToEveryone(new n_SerializableError(Exception, true));
        }
    }
}
