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

        public List<IMyPlayer> Players = new List<IMyPlayer>();
        public int SyncRange = MyAPIGateway.Session.SessionSettings.SyncDistance;
        public int SyncRangeSq = MyAPIGateway.Session.SessionSettings.SyncDistance * MyAPIGateway.Session.SessionSettings.SyncDistance;
        public bool IsPaused = false;
        public Random Random = new Random();
    }
}
