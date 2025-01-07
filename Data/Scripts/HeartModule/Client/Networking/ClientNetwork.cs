using Orrery.HeartModule.Shared.Logging;
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
        internal double EstimatedPing = 0;

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
            EstimatedPing = DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
            if (!MyAPIGateway.Session.IsServer)
                SendToServer(new n_TimeSyncPacket { OutgoingTimestamp = EstimatedPing });
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
                    HandlePacket(packet, senderSteamId);
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
        }

        /// <summary>
        /// Packet used for syncing time betweeen client and server.
        /// </summary>
        [ProtoContract]
        internal class n_TimeSyncPacket : PacketBase
        {
            [ProtoMember(21)] public double OutgoingTimestamp;
            [ProtoMember(22)] public double IncomingTimestamp;

            public n_TimeSyncPacket() { }

            public override void Received(ulong SenderSteamId)
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    ServerNetwork.SendToPlayer(new n_TimeSyncPacket
                    {
                        IncomingTimestamp = this.OutgoingTimestamp,
                        OutgoingTimestamp = DateTime.UtcNow.TimeOfDay.TotalMilliseconds
                    }, SenderSteamId);
                }
                else
                {
                    I.EstimatedPing = DateTime.UtcNow.TimeOfDay.TotalMilliseconds - I.EstimatedPing;
                    I.ServerTimeOffset = OutgoingTimestamp - IncomingTimestamp - I.EstimatedPing;
                    HeartLog.Debug("Outgoing Timestamp: " + OutgoingTimestamp + "\nIncoming Timestamp: " + IncomingTimestamp);
                    HeartLog.Debug("Total ping time (ms): " + I.EstimatedPing);
                }
            }
        }
    }
}
