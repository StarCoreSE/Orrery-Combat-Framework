using Orrery.HeartModule.Client.Networking;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Networking;
using ProtoBuf;
using Sandbox.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.WeaponSettings
{
    [ProtoContract]
    internal class WeaponSettings : PacketBase
    {
        public WeaponSettings(long weaponId)
        {
            WeaponId = weaponId;
        }

        /// <summary>
        /// DON'T USE THIS.
        /// </summary>
        internal WeaponSettings() { }


        [ProtoMember(1)]
        public long WeaponId;


        [ProtoMember(2)]
        private short ShootStateContainer;

        public int AmmoLoadedIdx
        {
            get
            {
                return _ammoLoadedIdx;
            }
            set
            {
                _ammoLoadedIdx = value;
                Sync();
            }
        }

        [ProtoMember(3)]
        private int _ammoLoadedIdx;

        #region ShootStates

        public bool ShootState
        {
            get
            {
                return ExpandValue(ShootStateContainer, ShootStates.Shoot);
            }
            set
            {
                CompressValue(ref ShootStateContainer, ShootStates.Shoot, value);
                Sync();
            }
        }

        public bool MouseShootState
        {
            get
            {
                return ExpandValue(ShootStateContainer, ShootStates.MouseShoot);
            }
            set
            {
                CompressValue(ref ShootStateContainer, ShootStates.MouseShoot, value);
                Sync();
            }
        }

        public bool HudBarrelIndicatorState
        {
            get
            {
                return ExpandValue(ShootStateContainer, ShootStates.HudBarrelIndicator);
            }
            set
            {
                CompressValue(ref ShootStateContainer, ShootStates.HudBarrelIndicator, value);
                Sync();
            }
        }

        #endregion

        #region Utils

        public override string ToString()
        {
            return $"ShootState: {ShootState}\nAmmoLoadedIdx: {AmmoLoadedIdx}";
        }

        internal bool ExpandValue(int bitwise, int enumValue)
        {
            return (bitwise & enumValue) == enumValue;
        }

        internal void CompressValue(ref int bitwise, int enumValue, bool state)
        {
            if (state)
                bitwise |= enumValue;
            else
                bitwise &= ~enumValue; // AND with negated enumValue
        }

        internal bool ExpandValue(short bitwise, int enumValue)
        {
            return (bitwise & enumValue) == enumValue;
        }

        internal void CompressValue(ref short bitwise, int enumValue, bool state)
        {
            if (state)
                bitwise |= (short)enumValue;
            else
                bitwise &= (short)~enumValue; // AND with negated enumValue
        }

        /// <summary>
        /// Send settings to others.
        /// </summary>
        internal void Sync()
        {
            // Special handling for localhost
            if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
            {
                HeartLog.Info("[LocalServer] Syncing settings for " + WeaponId);
                ServerNetwork.SendToEveryoneInSync(this, MyAPIGateway.Entities.GetEntityById(WeaponId)?.GetPosition() ?? Vector3D.Zero);

                var weaponClient = Client.Weapons.WeaponManager.GetWeapon(WeaponId);
                if (weaponClient != null)
                    weaponClient.Settings = this;
                var weaponServer = Server.Weapons.WeaponManager.GetWeapon(WeaponId);
                if (weaponServer != null)
                    weaponServer.Settings = this;

                return;
            }

            if (MyAPIGateway.Session.IsServer)
            {
                HeartLog.Info("[Server] Syncing settings for " + WeaponId);
                ServerNetwork.SendToEveryoneInSync(this, MyAPIGateway.Entities.GetEntityById(WeaponId)?.GetPosition() ?? Vector3D.Zero);
            }
            else
            {
                HeartLog.Info("[Client] Syncing settings for " + WeaponId);
                ClientNetwork.SendToServer(this);
            }
        }

        /// <summary>
        /// Requests settings from the server if a client.
        /// </summary>
        internal void RequestSync()
        {
            if (!MyAPIGateway.Session.IsServer)
                ClientNetwork.SendToServer(new RequestSettingsPacket(WeaponId));
        }

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                // Special handling for localhost
                if (!MyAPIGateway.Utilities.IsDedicated && SenderSteamId == 0)
                    return;

                var weapon = Server.Weapons.WeaponManager.GetWeapon(WeaponId);
                if (weapon != null)
                {
                    weapon.Settings = this;
                    weapon.Settings.Sync();
                }
            }
            else
            {
                var weapon = Client.Weapons.WeaponManager.GetWeapon(WeaponId);
                if (weapon != null)
                    weapon.Settings = this;
            }
        }

        #endregion

        private static class ShootStates
        {
            public const int Shoot = 1;
            public const int MouseShoot = 2;
            public const int HudBarrelIndicator = 4;
        }
    }
}
