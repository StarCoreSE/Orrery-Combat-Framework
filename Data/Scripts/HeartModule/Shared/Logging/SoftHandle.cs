using System;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Shared.Logging
{
    public class SoftHandle
    {
        public static void RaiseException(string message, Exception ex = null, Type callingType = null)
        {
            if (message == null)
                return;

            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + message);
            Exception soft = new Exception(message, ex);
            HeartLog.Exception(soft, callingType ?? typeof(SoftHandle));
        }

        public static void RaiseException(Exception exception, Type callingType = null)
        {
            if (exception == null)
                return;
            if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification("Minor Exception: " + exception.Message);
            HeartLog.Exception(exception, callingType ?? typeof(SoftHandle));
        }
    }
}
