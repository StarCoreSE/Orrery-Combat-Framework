using System;
using System.Collections.Generic;
using Orrery.HeartModule.Shared.Logging;

namespace Orrery.HeartModule.Shared.HeartApi
{
    /// <summary>
    /// Contains every HeartApi method.
    /// </summary>
    internal class HeartApiMethods
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods = new Dictionary<string, Delegate>
        {
            // Logging
            ["Log_WriteDebug"] = new Action<string>(HeartLog.Debug),
            ["Log_WriteInfo"] = new Action<string>(HeartLog.Info),
            ["Log_WriteException"] = new Action<Exception, Type>(HeartLog.Exception),
        };
    }
}
