using ProtoBuf;

namespace Orrery.HeartModule.Shared.WeaponSettings
{
    [ProtoContract]
    internal class TurretSettings : WeaponSettings
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

        [ProtoMember(4)]
        private ushort _aiRange;

        [ProtoMember(5)]
        private ushort _targetStateContainer;

        #region TargetingStates

        public bool TargetGridsState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetGrids);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetGrids, value);
                Sync();
            }
        }

        public bool TargetSmallGridsState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetSmallGrids);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetSmallGrids, value);
                Sync();
            }
        }

        public bool TargetLargeGridsState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetLargeGrids);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetLargeGrids, value);
                Sync();
            }
        }

        public bool TargetCharactersState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetCharacters);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetCharacters, value);
                Sync();
            }
        }

        public bool TargetProjectilesState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetProjectiles);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetProjectiles, value);
                Sync();
            }
        }

        public bool TargetEnemiesState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetEnemies);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetEnemies, value);
                Sync();
            }
        }

        public bool TargetFriendliesState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetFriendlies);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetFriendlies, value);
                Sync();
            }
        }

        public bool TargetNeutralsState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetNeutrals);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetNeutrals, value);
                Sync();
            }
        }

        public bool TargetUnownedState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.TargetUnowned);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.TargetUnowned, value);
                Sync();
            }
        }

        public bool PreferUniqueTargetState
        {
            get
            {
                return ExpandValue(_targetStateContainer, TargetingSettingStates.PreferUniqueTarget);
            }
            set
            {
                CompressValue(ref _targetStateContainer, TargetingSettingStates.PreferUniqueTarget, value);
                Sync();
            }
        }

        #endregion

        public override string ToString()
        {
            return base.ToString() + $"\nAiRange: {AiRange}\nTargetState: {_targetStateContainer}";
        }

        private static class TargetingSettingStates
        {
            public const ushort TargetGrids = 1;
            public const ushort TargetLargeGrids = 2;
            public const ushort TargetSmallGrids = 4;
            public const ushort TargetProjectiles = 8;
            public const ushort TargetCharacters = 16;
            public const ushort TargetFriendlies = 32;
            public const ushort TargetNeutrals = 64;
            public const ushort TargetEnemies = 128;
            public const ushort TargetUnowned = 256;
            public const ushort PreferUniqueTarget = 512;
        }
    }
}
