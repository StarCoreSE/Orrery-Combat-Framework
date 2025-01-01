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
    [ProtoInclude(91, typeof(TurretSettings))]
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

        /// <summary>
        /// Disables networking for this Settings instance if true.
        /// </summary>
        public bool LockedNetworking = false;

        [ProtoMember(1)]
        public long WeaponId;


        [ProtoMember(2)]
        private byte _shootStateContainer;

        public byte AmmoLoadedIdx
        {
            get
            {
                return _ammoLoadedIdx;
            }
            set
            {
                _ammoLoadedIdx = value;
                Server.Weapons.WeaponManager.GetWeapon(WeaponId)?.Magazine.EmptyMagazines();
                Sync();
            }
        }

        [ProtoMember(3)]
        private byte _ammoLoadedIdx;

        #region ShootStates

        public bool ShootState
        {
            get
            {
                return ExpandValue(_shootStateContainer, ShootStates.Shoot);
            }
            set
            {
                CompressValue(ref _shootStateContainer, ShootStates.Shoot, value);
                Sync();
            }
        }

        public bool MouseShootState
        {
            get
            {
                return ExpandValue(_shootStateContainer, ShootStates.MouseShoot);
            }
            set
            {
                CompressValue(ref _shootStateContainer, ShootStates.MouseShoot, value);
                Sync();
            }
        }

        public bool HudBarrelIndicatorState
        {
            get
            {
                return ExpandValue(_shootStateContainer, ShootStates.HudBarrelIndicator);
            }
            set
            {
                CompressValue(ref _shootStateContainer, ShootStates.HudBarrelIndicator, value);
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

        internal bool ExpandValue(ushort bitwise, ushort enumValue)
        {
            return (bitwise & enumValue) == enumValue;
        }

        internal void CompressValue(ref ushort bitwise, ushort enumValue, bool state)
        {
            if (state)
                bitwise |= enumValue;
            else
                bitwise &= (ushort)~enumValue; // AND with negated enumValue
        }

        internal bool ExpandValue(byte bitwise, byte enumValue)
        {
            return (bitwise & enumValue) == enumValue;
        }

        internal void CompressValue(ref byte bitwise, byte enumValue, bool state)
        {
            if (state)
                bitwise |= enumValue;
            else
                bitwise &= (byte)~enumValue; // AND with negated enumValue
        }

        /// <summary>
        /// Send settings to others.
        /// </summary>
        internal void Sync()
        {
            if (LockedNetworking)
                return;

            // Special handling for localhost
            if (MyAPIGateway.Session.IsServer && !MyAPIGateway.Utilities.IsDedicated)
            {
                ServerNetwork.SendToEveryoneInSync(this, MyAPIGateway.Entities.GetEntityById(WeaponId)?.GetPosition() ?? Vector3D.Zero);

                var weaponClient = Client.Weapons.WeaponManager.GetWeapon(WeaponId);
                if (weaponClient != null)
                {
                    weaponClient.Settings = this;
                }
                var weaponServer = Server.Weapons.WeaponManager.GetWeapon(WeaponId);
                if (weaponServer != null)
                {
                    bool needsReload = weaponServer.Settings.AmmoLoadedIdx != AmmoLoadedIdx;
                    weaponServer.Settings = this;
                    if (needsReload)
                        weaponServer.Magazine.EmptyMagazines();
                }

                return;
            }

            if (MyAPIGateway.Session.IsServer)
            {
                ServerNetwork.SendToEveryoneInSync(this, MyAPIGateway.Entities.GetEntityById(WeaponId)?.GetPosition() ?? Vector3D.Zero);
            }
            else
            {
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
                    bool needsReload = weapon.Settings.AmmoLoadedIdx != AmmoLoadedIdx;

                    weapon.Settings = this;
                    if (needsReload)
                        weapon.Magazine.EmptyMagazines();

                    weapon.Settings.Sync();
                }
            }
            else
            {
                var weapon = Client.Weapons.WeaponManager.GetWeapon(WeaponId);
                if (weapon != null)
                {
                    weapon.Settings = this;
                }
            }
        }

        #endregion

        private static class ShootStates
        {
            public const byte Shoot = 1;
            public const byte MouseShoot = 2;
            public const byte HudBarrelIndicator = 4;
        }
    }
}
