using System;
using ProtoBuf;

namespace Orrery.HeartModule.Shared.WeaponSettings
{
    [ProtoContract]
    internal class TurretSettings : SmartSettings
    {
        [Obsolete("Never use this constructor. It is marked internal for protobuf.", true)]
        internal TurretSettings() { }

        public TurretSettings(long weaponId) : base(weaponId)
        {
        }

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
