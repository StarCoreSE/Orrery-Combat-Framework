using Orrery.HeartModule.Shared.Networking;
using ProtoBuf;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Shared.WeaponSettings
{
    [ProtoContract]
    internal class SettingsPacket : PacketBase
    {
        [ProtoMember(1)] private WeaponSettings _settings;

        private SettingsPacket()
        {
        }

        public static explicit operator SettingsPacket(WeaponSettings settings) => new SettingsPacket { _settings = settings };

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                // Special handling for localhost
                if (!MyAPIGateway.Utilities.IsDedicated && SenderSteamId == 0)
                    return;

                var weapon = Server.Weapons.WeaponManager.GetWeapon(_settings.WeaponId);
                if (weapon != null)
                {
                    bool needsReload = weapon.Settings.AmmoLoadedIdx != _settings.AmmoLoadedIdx;

                    weapon.Settings = _settings;
                    if (needsReload)
                        weapon.Magazine.EmptyMagazines();

                    weapon.Settings.Sync();
                }
            }
            else
            {
                var weapon = Client.Weapons.WeaponManager.GetWeapon(_settings.WeaponId);
                if (weapon != null)
                {
                    weapon.Settings = _settings;
                }
            }
        }
    }
}
