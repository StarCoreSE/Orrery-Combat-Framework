﻿using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Network;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Sandbox.ModAPI;
using System;

namespace Heart_Module.Data.Scripts.HeartModule
{
    internal class HeartData
    {
        public static HeartData I;
        public const ushort HeartNetworkId = (ushort)(65198749845 % ushort.MaxValue);

        public bool IsSuspended = false;
        public bool IsPaused = false;
        public HeartNetwork Net = new HeartNetwork();
        public HeartLog Log = new HeartLog();
        public int SyncRange = MyAPIGateway.Session.SessionSettings.SyncDistance;
        public int SyncRangeSq = MyAPIGateway.Session.SessionSettings.SyncDistance * MyAPIGateway.Session.SessionSettings.SyncDistance;
        public Random Random = new Random();
        public ulong SteamId = 0;
        public SubpartManager SubpartManager = new SubpartManager();
    }
}