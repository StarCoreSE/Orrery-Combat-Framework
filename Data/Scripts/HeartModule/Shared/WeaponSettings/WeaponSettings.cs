using ProtoBuf;

namespace Orrery.HeartModule.Shared.WeaponSettings
{
    [ProtoContract]
    internal class WeaponSettings
    {
        [ProtoMember(1)]
        internal short ShootStateContainer;

        [ProtoMember(2)]
        public int AmmoLoadedIdx;

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
            }
        }

        public bool IsSyncRequest
        {
            get
            {
                return ExpandValue(ShootStateContainer, ShootStates.IsSyncRequest);
            }
            set
            {
                CompressValue(ref ShootStateContainer, ShootStates.IsSyncRequest, value);
            }
        }

        #endregion

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

        private static class ShootStates
        {
            public const int Shoot = 1;
            public const int MouseShoot = 2;
            public const int HudBarrelIndicator = 4;
            public const int IsSyncRequest = 8;
            public const int ResetTarget = 16;
        }
    }
}
