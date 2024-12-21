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
        public Dictionary<Type, int> TypeNetworkLoad = new Dictionary<Type, int>();

        private int _networkLoadUpdate = 0;


        public void LoadData()
        {
            I = this;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(HeartData.ServerNetworkId, ReceivedPacket);

            foreach (var type in PacketBase.Types)
                TypeNetworkLoad.Add(type, 0);

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
            _networkLoadUpdate--;
            if (_networkLoadUpdate <= 0)
            {
                _networkLoadUpdate = NetworkLoadTicks;
                TotalNetworkLoad = 0;
                foreach (var networkLoadArray in TypeNetworkLoad.Keys.ToArray())
                {
                    TotalNetworkLoad += TypeNetworkLoad[networkLoadArray];
                    TypeNetworkLoad[networkLoadArray] = 0;
                }

                TotalNetworkLoad /= (NetworkLoadTicks / 60); // Average per-second
            }
        }

        private void ReceivedPacket(ushort channelId, byte[] serialized, ulong senderSteamId, bool isSenderServer)
        {
            try
            {
                PacketBase packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(serialized);
                TypeNetworkLoad[packet.GetType()] += serialized.Length;
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



        

        public KeyValuePair<Type, int> HighestNetworkLoad()
        {
            Type highest = null;

            foreach (var networkLoadArray in TypeNetworkLoad)
            {
                if (highest == null || networkLoadArray.Value > TypeNetworkLoad[highest])
                {
                    highest = networkLoadArray.Key;
                }
            }

            return new KeyValuePair<Type, int>(highest, TypeNetworkLoad[highest]);
        }

        public static void SendToPlayer(PacketBase packet, ulong playerSteamId, byte[] serialized = null) =>
            I?.SendToPlayerInternal(packet, playerSteamId, serialized);
        public static void SendToEveryone(PacketBase packet, byte[] serialized = null) =>
            I?.SendToEveryoneInternal(packet, serialized);
        public static void SendToEveryoneInSync(PacketBase packet, Vector3D position, byte[] serialized = null) =>
            I?.SendToEveryoneInSyncInternal(packet, position, serialized);


        private void SendToPlayerInternal(PacketBase packet, ulong playerSteamId, byte[] serialized = null)
        {
            if (playerSteamId == MyAPIGateway.Multiplayer.ServerId || playerSteamId == 0)
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                    packet.Received(0);
                return;
            }

            if (serialized == null) // only serialize if necessary, and only once.
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            MyAPIGateway.Multiplayer.SendMessageTo(HeartData.ClientNetworkId, serialized, playerSteamId);
        }

        private void SendToEveryoneInternal(PacketBase packet, byte[] serialized = null)
        {
            TempPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(TempPlayers);

            foreach (IMyPlayer p in TempPlayers)
            {
                // skip sending to self (server player) or back to sender
                if (p.SteamUserId == MyAPIGateway.Multiplayer.ServerId || p.SteamUserId == 0)
                {
                    if (!MyAPIGateway.Utilities.IsDedicated)
                        packet.Received(0);
                    continue;
                }

                if (serialized == null) // only serialize if necessary, and only once.
                    serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

                MyAPIGateway.Multiplayer.SendMessageTo(HeartData.ClientNetworkId, serialized, p.SteamUserId);
            }

            TempPlayers.Clear();
        }

        private void SendToEveryoneInSyncInternal(PacketBase packet, Vector3D position, byte[] serialized = null)
        {
            List<ulong> toSend = new List<ulong>();
            foreach (var player in HeartData.I.Players)
                if (Vector3D.DistanceSquared(player.GetPosition(), position) <= HeartData.I.SyncRangeSq)
                    toSend.Add(player.SteamUserId);

            if (toSend.Count == 0)
                return;

            if (serialized == null)
                serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            foreach (var clientSteamId in toSend)
                SendToPlayer(packet, clientSteamId, serialized);
        }


        List<IMyPlayer> TempPlayers = new List<IMyPlayer>();
    }
}
