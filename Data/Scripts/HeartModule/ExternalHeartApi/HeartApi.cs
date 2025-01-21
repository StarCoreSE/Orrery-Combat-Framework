using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

// ReSharper disable MemberCanBePrivate.Global

namespace OrreryFramework.Communication
{
    /// <summary>
    /// Self-contained ModAPI for the Orrery Combat Framework Heart Module.
    /// <para>
    ///     Access using the static HeartApi interface; i.e. HeartApi.Shared.LogInfo("");
    ///     TODO: Write usage directions.
    /// </para>
    /// <para>
    ///     Self-initializes, and is ready when <see cref="IsReady">HeartApi.IsReady</see> is true.
    /// </para>
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, int.MinValue)]
    internal class HeartApi : MySessionComponentBase
    {
        /*
         * This is an absolute monster of a class, and for that I apologize.
         * It was written as such to make it as easy as possible for the end user to use.
         * Good luck, and happy modding!
         * - Aristeas
         */

        public const int ApiVersion = 1;
        public static bool IsReady { get; private set; } = false;

        /// <summary>
        /// HeartApi methods available to both the server and client.
        /// </summary>
        public static SharedMethods Shared = null;
        /// <summary>
        /// HeartApi methods available to only the server. Will throw an exception if accessed from a client-only instance.
        /// </summary>
        public static ServerMethods Server = null;
        /// <summary>
        /// HeartApi methods available to only the client. Will throw an exception if accessed from a server-only instance.
        /// </summary>
        public static ClientMethods Client = null;
        /// <summary>
        /// DefinitionApi from the DefinitionHelper mod. <see href="https://steamcommunity.com/sharedfiles/filedetails/?id=3407764326"/>
        /// </summary>
        public static DefinitionApi DefinitionApi = null;

        private const long HeartApiChannel = 8644; // https://xkcd.com/221/

        internal static List<ProjectileDefinitionBase> ProjectileDefinitions = new List<ProjectileDefinitionBase>();
        internal static List<WeaponDefinitionBase> WeaponDefinitions = new List<WeaponDefinitionBase>();


        #region API Internals

        public override void LoadData()
        {
            IsReady = false;

            MyAPIGateway.Utilities.RegisterMessageHandler(HeartApiChannel, OnReceiveMessage);
            MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, true);
            MyLog.Default.WriteLineAndConsole(
                $"{ModContext.ModName}: HeartAPI listening for API methods...");

            DefinitionApi = new DefinitionApi();
            DefinitionApi.Init(ModContext, InitAndSendDefinitions);
        }

        protected override void UnloadData()
        {
            DefinitionApi.UnloadData();
            DefinitionApi = null;

            MyAPIGateway.Utilities.UnregisterMessageHandler(HeartApiChannel, OnReceiveMessage);
            IsReady = false;
            Server = null;
            Client = null;
            Shared = null;
            MyLog.Default.WriteLineAndConsole($"{ModContext.ModName}: HeartAPI unloaded.");
        }

        private void OnReceiveMessage(object obj)
        {
            try
            {
                if (obj is bool && !(bool)obj) // Receiving 'false' closes the API.
                {
                    IsReady = false;
                    Server = null;
                    Client = null;
                    Shared = null;
                    return;
                }

                var message =
                    obj as MyTuple<int, Dictionary<string, Delegate>, Dictionary<string, Delegate>,
                        Dictionary<string, Delegate>>?;
                if (IsReady || message == null)
                    return;

                // Exceptions *will* be thrown if server tries to access client API, and that's okay.
                Shared = new SharedMethods(ModContext, message.Value.Item2);
                if (MyAPIGateway.Session.IsServer)
                    Server = new ServerMethods(ModContext, message.Value.Item3);
                if (!MyAPIGateway.Utilities.IsDedicated)
                    Client = new ClientMethods(ModContext, message.Value.Item4);

                if (message.Value.Item1 != ApiVersion)
                    Shared.LogInfo($"HeartApi version ({ApiVersion}) differs from HeartMod version {message.Value.Item1} - issues may occur.");

                IsReady = true;
                Shared.LogInfo($"Registered HeartApi v({ApiVersion}).");
            }
            catch (Exception ex)
            {
                // We really want to notify the player if something goes wrong here.
                MyLog.Default.WriteLineAndConsole($"{ModContext.ModName}: Exception in HeartApi! " + ex);
                MyAPIGateway.Utilities.ShowMessage(ModContext.ModName, "Exception in HeartApi!\n" + ex);
            }
        }

        private void InitAndSendDefinitions()
        {
            var discard = new HeartDefinitions(); // Definitions are added to the lists by their constructor.

            foreach (var def in ProjectileDefinitions)
                def.Register();
            DefinitionApi.LogInfo($"Sent {ProjectileDefinitions.Count} projectile definitions.");

            foreach (var def in WeaponDefinitions)
                def.Register();
            DefinitionApi.LogInfo($"Sent {WeaponDefinitions.Count} weapon definitions.");

            ProjectileDefinitions.ClearAndTrim(0);
            WeaponDefinitions.ClearAndTrim(0);
        }

        /// <summary>
        /// HeartApi methods available to both the server and client. Modders should not access this directly.
        /// </summary>
        public class SharedMethods : ApiMethods
        {
            #region Utilities
            public Random Random = new Random();

            public Vector3D RandomCone(Vector3D centerDirection, double radius)
            {
                Vector3D axis = Vector3D.CalculatePerpendicularVector(centerDirection).Rotate(centerDirection, Math.PI * 2 * Random.NextDouble());
                return centerDirection.Rotate(axis, radius * Random.NextDouble());
            }
            #endregion

            #region Logging
            /// <summary>
            /// Writes information to the HeartMod's log file.
            /// </summary>
            /// <param name="text"></param>
            public void LogInfo(string text) => _logInfo?.Invoke($"[{ModContext.ModName}] {text}");
            /// <summary>
            /// Writes information to the HeartMod's debug log file.
            /// </summary>
            /// <param name="text"></param>
            public void LogDebug(string text) => _logDebug?.Invoke($"[{ModContext.ModName}] {text}");
            /// <summary>
            /// Logs an exception to the HeartLog's log file.
            /// </summary>
            /// <param name="exception"></param>
            /// <param name="type">Type where the exception was caught.</param>
            public void LogException(Exception exception, Type type) => _logException?.Invoke(exception, type);
            /// <summary>
            /// Logs an exception to the HeartLog's log file.
            /// </summary>
            /// <param name="exception"></param>
            public void LogException(Exception exception) => _logException?.Invoke(exception, typeof(HeartApi));
            #endregion

            #region Projectiles

            public MyTuple<string, Vector3D, Vector3D, IMyEntity>? GetProjectileInfo(uint projectileId) =>
                _projectileInfo?.Invoke(projectileId);

            #endregion

            #region Weapons

            public WeaponSettings GetWeaponSettings(IMyConveyorSorter weapon) => MyAPIGateway.Utilities.SerializeFromBinary<WeaponSettings>(_getWeaponSettings?.Invoke(weapon));
            public void SetWeaponSettings(IMyConveyorSorter weapon, WeaponSettings settings) =>
                _setWeaponSettings?.Invoke(weapon, MyAPIGateway.Utilities.SerializeToBinary(settings));

            public IEnumerable<IMyConveyorSorter> GetGridWeapons(IMyCubeGrid grid) => _getGridWeapons?.Invoke(grid);
            public bool HasWeapon(IMyConveyorSorter sorter) => _hasWeapon?.Invoke(sorter) ?? false;

            public void RegisterOnWeaponAdd(Action<IMyConveyorSorter> action) => _registerOnWeaponAdd?.Invoke(action);
            public void UnregisterOnWeaponAdd(Action<IMyConveyorSorter> action) => _unregisterOnWeaponAdd?.Invoke(action);
            public void RegisterOnWeaponClose(Action<IMyConveyorSorter> action) => _registerOnWeaponClose?.Invoke(action);
            public void UnregisterOnWeaponClose(Action<IMyConveyorSorter> action) => _unregisterOnWeaponClose?.Invoke(action);

            public MyTuple<IMyEntity, uint?>? GetWeaponTarget(IMyConveyorSorter sorterWep) => _getWeaponTarget.Invoke(sorterWep);
            public Vector3D? GetWeaponAimpoint(IMyConveyorSorter sorterWep) => _getWeaponAimpoint.Invoke(sorterWep);
            public MatrixD GetWeaponBarrelMatrix(IMyConveyorSorter sorterWep) => _getWeaponBarrelMatrix.Invoke(sorterWep);
            //public WeaponDefinitionBase GetWeaponDefinition(IMyConveyorSorter sorterWep)

            #endregion

            #region Delegates
            // Logging
            private readonly Action<string> _logInfo;
            private readonly Action<string> _logDebug;
            private readonly Action<Exception, Type> _logException;

            // Projectiles
            private readonly Func<uint, MyTuple<string, Vector3D, Vector3D, IMyEntity>?> _projectileInfo;

            // Weapons
            private readonly Func<IMyConveyorSorter, byte[]> _getWeaponSettings;
            private readonly Action<IMyConveyorSorter, byte[]> _setWeaponSettings;
            private readonly Func<IMyCubeGrid, IEnumerable<IMyConveyorSorter>> _getGridWeapons;
            private readonly Func<IMyConveyorSorter, bool> _hasWeapon;

            private readonly Action<Action<IMyConveyorSorter>> _registerOnWeaponAdd;
            private readonly Action<Action<IMyConveyorSorter>> _unregisterOnWeaponAdd;
            private readonly Action<Action<IMyConveyorSorter>> _registerOnWeaponClose;
            private readonly Action<Action<IMyConveyorSorter>> _unregisterOnWeaponClose;

            private readonly Func<IMyConveyorSorter, MyTuple<IMyEntity, uint?>?> _getWeaponTarget;
            private readonly Func<IMyConveyorSorter, Vector3D?> _getWeaponAimpoint;
            private readonly Func<IMyConveyorSorter, MatrixD> _getWeaponBarrelMatrix;
            #endregion

            internal SharedMethods(IMyModContext modContext, Dictionary<string, Delegate> methodMap) : base(modContext, methodMap)
            {
                // Logging
                SetApiMethod("LogInfo", ref _logInfo);
                SetApiMethod("LogDebug", ref _logDebug);
                SetApiMethod("LogException", ref _logException);

                // Projectiles
                SetApiMethod("ProjectileInfo", ref _projectileInfo);

                // Weapons
                SetApiMethod("GetWeaponSettings", ref _getWeaponSettings);
                SetApiMethod("SetWeaponSettings", ref _setWeaponSettings);
                SetApiMethod("GetGridWeapons", ref _getGridWeapons);
                SetApiMethod("HasWeapon", ref _hasWeapon);

                SetApiMethod("RegisterOnWeaponAdd", ref _registerOnWeaponAdd);
                SetApiMethod("UnregisterOnWeaponAdd", ref _unregisterOnWeaponAdd);
                SetApiMethod("RegisterOnWeaponClose", ref _registerOnWeaponClose);
                SetApiMethod("UnregisterOnWeaponClose", ref _unregisterOnWeaponClose);

                SetApiMethod("GetWeaponTarget", ref _getWeaponTarget);
                SetApiMethod("GetWeaponAimpoint", ref _getWeaponAimpoint);
                SetApiMethod("GetWeaponBarrelMatrix", ref _getWeaponBarrelMatrix);
            }
        }

        /// <summary>
        /// HeartApi methods available to only the server. Modders should not access this directly.
        /// </summary>
        public class ServerMethods : ApiMethods
        {
            // TODO: Look into Delegates for projectiles, weapons, logging, etc
            #region Projectiles

            public uint ProjectileSpawn(string definitionName, Vector3D position, Vector3D direction,
                IMyEntity owner) => _projectileSpawn?.Invoke(definitionName, position, direction, owner) ?? uint.MaxValue;
            #endregion

            #region Delegates
            // Projectiles
            private readonly Func<string, Vector3D, Vector3D, IMyEntity, uint> _projectileSpawn;
            #endregion

            internal ServerMethods(IMyModContext modContext, Dictionary<string, Delegate> methodMap) : base(modContext, methodMap)
            {
                SetApiMethod("ProjectileSpawn", ref _projectileSpawn);
            }
        }

        /// <summary>
        /// HeartApi methods available to only the client. Modders should not access this directly.
        /// </summary>
        public class ClientMethods : ApiMethods
        {
            internal ClientMethods(IMyModContext modContext, Dictionary<string, Delegate> methodMap) : base(modContext, methodMap)
            {
            }
        }

        public abstract class ApiMethods
        {
            protected readonly IMyModContext ModContext;
            private readonly Dictionary<string, Delegate> _methodMap;

            internal ApiMethods(IMyModContext modContext, Dictionary<string, Delegate> methodMap)
            {
                ModContext = modContext;
                _methodMap = methodMap;
            }

            /// <summary>
            ///     Assigns a single API endpoint.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name">Shared endpoint name; matches with the framework mod.</param>
            /// <param name="method">Method to assign.</param>
            /// <exception cref="Exception"></exception>
            protected void SetApiMethod<T>(string name, ref T method) where T : class
            {
                if (_methodMap == null)
                {
                    method = null;
                    return;
                }

                if (!_methodMap.ContainsKey(name))
                    throw new Exception("Method Map does not contain method " + name);
                var del = _methodMap[name];
                if (del.GetType() != typeof(T))
                    throw new Exception(
                        $"Method {name} type mismatch! [MapMethod: {del.GetType().Name} | ApiMethod: {typeof(T).Name}]");
                method = _methodMap[name] as T;
            }
        }

        #endregion

        #region Settings Classes

        [ProtoContract(UseProtoMembersOnly = true)]
        [ProtoInclude(91, typeof(SmartSettings))]
        internal class WeaponSettings
        {
            [ProtoMember(1)]
            public long WeaponId;


            [ProtoMember(2)]
            private byte _shootStateContainer;

            [ProtoMember(3)]
            public byte AmmoLoadedIdx;

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

            #endregion

            private static class ShootStates
            {
                public const byte Shoot = 1;
                public const byte MouseShoot = 2;
                public const byte HudBarrelIndicator = 4;
            }
        }

        [ProtoContract]
        [ProtoInclude(92, typeof(TurretSettings))]
        internal class SmartSettings : WeaponSettings
        {
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

        [ProtoContract]
        internal class TurretSettings : SmartSettings
        {
            public float AiRange
            {
                get
                {
                    return _aiRange;
                }
                set
                {
                    _aiRange = (ushort)value;
                }
            }

            [ProtoMember(5)]
            private ushort _aiRange;

            public override string ToString()
            {
                return base.ToString() + $"\nAiRange: {AiRange}";
            }
        }

        #endregion
    }
}
