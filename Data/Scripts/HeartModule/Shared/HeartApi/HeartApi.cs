using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

// ReSharper disable MemberCanBePrivate.Global

namespace Orrery.HeartModule.Shared.HeartApi
{
    /// <summary>
    /// Self-contained ModAPI for the Orrery Combat Framework Heart Module.
    /// <para>
    ///     TODO: Write usage directions.
    /// </para>
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, int.MaxValue)]
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

        private const long HeartApiChannel = 8644; // https://xkcd.com/221/

        public override void LoadData()
        {
            IsReady = false;

            MyAPIGateway.Utilities.RegisterMessageHandler(HeartApiChannel, OnReceiveMessage);
            MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, true);
            MyLog.Default.WriteLineAndConsole(
                $"{ModContext.ModName}: HeartAPI listening for API methods...");
        }

        protected override void UnloadData()
        {
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

            #region Delegates
            // Logging
            private readonly Action<string> _logInfo;
            private readonly Action<string> _logDebug;
            private readonly Action<Exception, Type> _logException;

            // Projectiles
            private readonly Func<uint, MyTuple<string, Vector3D, Vector3D, IMyEntity>?> _projectileInfo;
            #endregion

            internal SharedMethods(IMyModContext modContext, Dictionary<string, Delegate> methodMap) : base(modContext, methodMap)
            {
                // Logging
                SetApiMethod("LogInfo", ref _logInfo);
                SetApiMethod("LogDebug", ref _logDebug);
                SetApiMethod("LogException", ref _logException);

                // Projectiles
                SetApiMethod("ProjectileInfo", ref _projectileInfo);
            }
        }

        /// <summary>
        /// HeartApi methods available to only the server. Modders should not access this directly.
        /// </summary>
        public class ServerMethods : ApiMethods
        {
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
    }
}
