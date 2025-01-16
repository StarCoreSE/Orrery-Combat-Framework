using System;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Orrery.HeartModule
{
    internal class HeartData
    {
        public static HeartData I;

        public const ushort ClientNetworkId = 9930;
        public const ushort ServerNetworkId = 9931;

        #region Constant Values
        public readonly int SyncRange = MyAPIGateway.Session.SessionSettings.SyncDistance;
        public readonly int SyncRangeSq = MyAPIGateway.Session.SessionSettings.SyncDistance * MyAPIGateway.Session.SessionSettings.SyncDistance;
        public readonly Random Random = new Random();
        public readonly Guid HeartSettingsGUID = new Guid("06edc546-3e42-41f3-bc72-1d640035fbf2");
        public readonly Version Version = new Version(0, 1);
        #endregion

        #region Global Variables
        public readonly List<IMyPlayer> Players = new List<IMyPlayer>();
        public bool IsPaused = false;
        #endregion
    }
}
