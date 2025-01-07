using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Networking;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Networking
{
    internal class ServerNetwork
    {
        public static ServerNetwork I;

        public int NetworkLoadTicks = 240;
        public int TotalNetworkLoad { get; private set; } = 0;
        private int _bufferNetworkLoad = 0;

        private Dictionary<ulong, HashSet<PacketBase>> _packetQueue = new Dictionary<ulong, HashSet<PacketBase>>();

        private int _networkLoadUpdate = 0;


        public void LoadData()
        {
            I = this;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(HeartData.ServerNetworkId, ReceivedPacket);

            HeartLog.Info("Initialized ServerNetwork.");
        }

        public void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(HeartData.ServerNetworkId, ReceivedPacket);
            I = null;
            HeartLog.Info("Closed ServerNetwork.");
        }

        public void Update()
        {
            foreach (var queuePair in _packetQueue)
            {
                if (queuePair.Value.Count == 0)
                    continue;

                MyAPIGateway.Multiplayer.SendMessageTo(HeartData.ClientNetworkId, MyAPIGateway.Utilities.SerializeToBinary(queuePair.Value.ToArray()), queuePair.Key);
            }

            _networkLoadUpdate--;
            if (_networkLoadUpdate <= 0)
            {
                _networkLoadUpdate = NetworkLoadTicks;
                TotalNetworkLoad = _bufferNetworkLoad;
                _bufferNetworkLoad = 0;

                TotalNetworkLoad /= (NetworkLoadTicks / 60); // Average per-second
            }
        }

        private void ReceivedPacket(ushort channelId, byte[] serialized, ulong senderSteamId, bool isSenderServer)
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
                SoftHandle.RaiseException(ex, typeof(ServerNetwork));
            }
        }

        private void HandlePacket(PacketBase packet, ulong senderSteamId)
        {
            packet.Received(senderSteamId);
        }



        


        public static void SendToPlayer(PacketBase packet, ulong playerSteamId) =>
            I?.SendToPlayerInternal(packet, playerSteamId);
        public static void SendToEveryone(PacketBase packet) =>
            I?.SendToEveryoneInternal(packet);
        public static void SendToEveryoneInSync(PacketBase packet, Vector3D position) =>
            I?.SendToEveryoneInSyncInternal(packet, position);


        private void SendToPlayerInternal(PacketBase packet, ulong playerSteamId)
        {
            if (playerSteamId == MyAPIGateway.Multiplayer.ServerId || playerSteamId == 0)
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                    packet.Received(0);
                return;
            }

            if (!_packetQueue.ContainsKey(playerSteamId))
                _packetQueue[playerSteamId] = new HashSet<PacketBase>();
            _packetQueue[playerSteamId].Add(packet);
        }

        private void SendToEveryoneInternal(PacketBase packet)
        {
            _tempPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(_tempPlayers);

            foreach (IMyPlayer p in _tempPlayers)
            {
                // skip sending to self (server player) or back to sender
                if (p.SteamUserId == MyAPIGateway.Multiplayer.ServerId || p.SteamUserId == 0)
                {
                    if (!MyAPIGateway.Utilities.IsDedicated)
                        packet.Received(0);
                    continue;
                }

                if (!_packetQueue.ContainsKey(p.SteamUserId))
                    _packetQueue[p.SteamUserId] = new HashSet<PacketBase>();
                _packetQueue[p.SteamUserId].Add(packet);
            }

            _tempPlayers.Clear();
        }

        private void SendToEveryoneInSyncInternal(PacketBase packet, Vector3D position)
        {
            List<ulong> toSend = new List<ulong>();
            foreach (var player in HeartData.I.Players)
                if (Vector3D.DistanceSquared(player.GetPosition(), position) <= HeartData.I.SyncRangeSq)
                    toSend.Add(player.SteamUserId);

            if (toSend.Count == 0)
                return;

            foreach (var clientSteamId in toSend)
                SendToPlayerInternal(packet, clientSteamId);
        }


        private readonly List<IMyPlayer> _tempPlayers = new List<IMyPlayer>();
    }
}
