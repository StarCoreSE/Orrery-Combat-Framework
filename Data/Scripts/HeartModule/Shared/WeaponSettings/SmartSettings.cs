using System;
using ProtoBuf;

namespace Orrery.HeartModule.Shared.WeaponSettings
{
    [ProtoContract]
    [ProtoInclude(92, typeof(TurretSettings))]
    internal class SmartSettings : WeaponSettings
    {
        [Obsolete("Never use this constructor. It is marked internal for protobuf.", true)]
        internal SmartSettings() { }

        public SmartSettings(long weaponId) : base(weaponId)
        {
        }

        [ProtoMember(4)]
        public ushort TargetStateContainer;

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
                Sync();
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
                Sync();
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
                Sync();
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
                Sync();
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
                Sync();
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
                Sync();
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
                Sync();
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
                Sync();
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
                Sync();
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
                Sync();
            }
        }

        #endregion

        public override string ToString()
        {
            return base.ToString() + $"\nTargetState: {TargetStateContainer}";
        }

        /// <summary>
        /// See <see cref="Orrery.HeartModule.Shared.Targeting.TargetingStateEnum">TargetingStateEnum</see>
        /// </summary>
        public static class TargetingSettingStates
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
