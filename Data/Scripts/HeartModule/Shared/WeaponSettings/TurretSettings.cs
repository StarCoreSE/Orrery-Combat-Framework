using ProtoBuf;

namespace Orrery.HeartModule.Shared.WeaponSettings
{
    [ProtoContract]
    internal class TurretSettings : WeaponSettings
    {
        [ProtoMember(3)]
        public float AiRange;

        [ProtoMember(4)]
        internal int TargetStateContainer;

        [ProtoMember(5)]
        public long ControlTypeState;

        #region TargetingStates

        public bool TargetGridsState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetGrids);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetGrids, value);
            }
        }

        public bool TargetSmallGridsState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetSmallGrids);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetSmallGrids, value);
            }
        }

        public bool TargetLargeGridsState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetLargeGrids);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetLargeGrids, value);
            }
        }

        public bool TargetCharactersState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetCharacters);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetCharacters, value);
            }
        }

        public bool TargetProjectilesState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetProjectiles);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetProjectiles, value);
            }
        }

        public bool TargetEnemiesState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetEnemies);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetEnemies, value);
            }
        }

        public bool TargetFriendliesState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetFriendlies);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetFriendlies, value);
            }
        }

        public bool TargetNeutralsState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetNeutrals);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetNeutrals, value);
            }
        }

        public bool TargetUnownedState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.TargetUnowned);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.TargetUnowned, value);
            }
        }

        public bool PreferUniqueTargetState
        {
            get
            {
                return ExpandValue(TargetStateContainer, TargetingSettingStates.PreferUniqueTarget);
            }
            set
            {
                CompressValue(ref TargetStateContainer, TargetingSettingStates.PreferUniqueTarget, value);
            }
        }

        #endregion

        private static class TargetingSettingStates
        {
            public const int TargetGrids = 2;
            public const int TargetLargeGrids = 4;
            public const int TargetSmallGrids = 8;
            public const int TargetProjectiles = 16;
            public const int TargetCharacters = 32;
            public const int TargetFriendlies = 64;
            public const int TargetNeutrals = 128;
            public const int TargetEnemies = 256;
            public const int TargetUnowned = 512;
            public const int PreferUniqueTarget = 1024;
        }
    }
}
