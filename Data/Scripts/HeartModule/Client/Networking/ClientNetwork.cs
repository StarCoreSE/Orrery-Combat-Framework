﻿using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Networking;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System;
using System.Linq;
using Orrery.HeartModule.Server.Networking;
using ProtoBuf;
using VRage.Game.ModAPI;
using VRage.Serialization;

namespace Orrery.HeartModule.Client.Networking
{
    internal class ClientNetwork
    {
        public static ClientNetwork I;
        public int NetworkLoadTicks = 240;
        public int TotalNetworkLoad { get; private set; } = 0;
        private int _bufferNetworkLoad = 0;

        private int _networkLoadUpdate = 0;

        public double ServerTimeOffset { get; internal set; } = 0;
        internal double EstimatedPing = 0d;
        private long _lastTimeSync = 0;

        // We only need one because it's only being sent to the server.
        private HashSet<PacketBase> _packetQueue = new HashSet<PacketBase>();

        public void LoadData()
        {
            I = this;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(HeartData.ClientNetworkId, ReceivedPacket);

            UpdateTimeOffset();
            HeartLog.Info("Initialized client network.");
        }

        private void UpdateTimeOffset()
        {
            // Client-host delay is always zero, so we don't need to update it.
            if (MyAPIGateway.Session.IsServer)
                return;

            _lastTimeSync = DateTime.UtcNow.Ticks;
            SendToServer(new TimeSyncPacket { SendTimestamp = DateTime.UtcNow.Ticks });
        }

        public void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(HeartData.ClientNetworkId, ReceivedPacket);
            HeartLog.Info("Closed client network.");
        }

        int _tickCounter = 0;
        public void Update()
        {
            if (_packetQueue.Count > 0)
            {
                MyAPIGateway.Multiplayer.SendMessageToServer(HeartData.ServerNetworkId, MyAPIGateway.Utilities.SerializeToBinary(_packetQueue.ToArray()));
                //HeartLog.Info("Send packets " + _packetQueue.Count);
                _packetQueue.Clear();
            }

            _networkLoadUpdate--;
            if (_networkLoadUpdate <= 0)
            {
                _networkLoadUpdate = NetworkLoadTicks;
                TotalNetworkLoad = _bufferNetworkLoad;
                _bufferNetworkLoad = 0;
                TotalNetworkLoad /= (NetworkLoadTicks / 60); // Average per-second
            }

            if (_tickCounter % 307 == 0)
                UpdateTimeOffset();
            _tickCounter++;
        }

        void ReceivedPacket(ushort channelId, byte[] serialized, ulong senderSteamId, bool isSenderServer)
        {
            try
            {
                PacketBase[] packets = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase[]>(serialized);
                _bufferNetworkLoad += serialized.Length;
                foreach (var packet in packets)
                {
                    //HeartLog.Info("Receive packet " + packet.GetType().Name);
                    HandlePacket(packet, senderSteamId);
                }
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(ClientNetwork));
            }
        }

        void HandlePacket(PacketBase packet, ulong senderSteamId)
        {
            packet.Received(senderSteamId);
        }





        public static void SendToServer(PacketBase packet) =>
            I?.SendToServerInternal(packet);

        private void SendToServerInternal(PacketBase packet)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                packet.Received(0);
                return;
            }

            _packetQueue.Add(packet);
        }

        /// <summary>
        /// Packet used for syncing time betweeen client and server.
        /// </summary>
        [ProtoContract]
        internal class TimeSyncPacket : PacketBase
        {
            [ProtoMember(1)] public long SendTimestamp = -1;
            [ProtoMember(2)] public long ReceiveTimestamp = -1;

            public TimeSyncPacket() { }

            public override void Received(ulong SenderSteamId)
            {
                if (ReceiveTimestamp != -1)
                {
                    I.EstimatedPing = (DateTime.UtcNow.Ticks - I._lastTimeSync) / (double) TimeSpan.TicksPerSecond;
                    I.ServerTimeOffset = ((ReceiveTimestamp - SendTimestamp) - (DateTime.UtcNow.Ticks - I._lastTimeSync)) / (double) TimeSpan.TicksPerSecond;
                    //HeartLog.Info("[TimeSync] Outgoing Timestamp: " + SendTimestamp + "\nIncoming Timestamp: " + ReceiveTimestamp);
                    HeartLog.Info("[TimeSync] Total ping time (ms): " + I.EstimatedPing);
                }
                else if (MyAPIGateway.Session.IsServer)
                {
                    ReceiveTimestamp = DateTime.UtcNow.Ticks;
                    ServerNetwork.SendToPlayer(this, SenderSteamId);
                }
            }
        }
    }
}
