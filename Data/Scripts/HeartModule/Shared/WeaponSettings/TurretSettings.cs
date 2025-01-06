using ProtoBuf;

namespace Orrery.HeartModule.Shared.WeaponSettings
{
    [ProtoContract]
    internal class TurretSettings : SmartSettings
    {
        public TurretSettings(long weaponId) : base(weaponId)
        {
        }

        /// <summary>
        /// DON'T USE THIS.
        /// </summary>
        internal TurretSettings() { }

        public float AiRange
        {
            get
            {
                return _aiRange;
            }
            set
            {
                _aiRange = (ushort) value;
                Sync();
            }
        }

        [ProtoMember(5)]
        private ushort _aiRange;

        public override string ToString()
        {
            return base.ToString() + $"\nAiRange: {AiRange}";
        }
    }
}
