using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
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
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
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
            MyLog.Default.WriteLineAndConsole($"{ModContext.ModName}: HeartAPI unloaded.");
        }

        private void OnReceiveMessage(object obj)
        {
            try
            {
                var message =
                    obj as MyTuple<int, Dictionary<string, Delegate>, Dictionary<string, Delegate>,
                        Dictionary<string, Delegate>>?;
                if (IsReady || message == null)
                    return;

                // Exceptions *will* be thrown if server tries to access client API, and that's okay.
                Shared = new SharedMethods(message.Value.Item2);
                if (MyAPIGateway.Session.IsServer)
                    Server = new ServerMethods(message.Value.Item3);
                if (!MyAPIGateway.Utilities.IsDedicated)
                    Client = new ClientMethods(message.Value.Item4);

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
        /// HeartApi methods available to both the server and client.
        /// </summary>
        public class SharedMethods : ApiMethods
        {
            // Logging
            public readonly Action<string> LogInfo;
            public readonly Action<string> LogDebug;
            public readonly Action<Exception, Type> LogException;

            // Projectiles
            public readonly Func<uint, MyTuple<string, Vector3D, Vector3D, IMyEntity>?> ProjectileInfo;

            public SharedMethods(Dictionary<string, Delegate> methodMap) : base(methodMap)
            {
                // Logging
                SetApiMethod("LogInfo", ref LogInfo);
                SetApiMethod("LogDebug", ref LogDebug);
                SetApiMethod("LogException", ref LogException);

                // Projectiles
                SetApiMethod("ProjectileInfo", ref ProjectileInfo);
            }
        }

        public class ServerMethods : ApiMethods
        {
            public readonly Func<string, Vector3D, Vector3D, IMyEntity, uint> ProjectileSpawn;

            public ServerMethods(Dictionary<string, Delegate> methodMap) : base(methodMap)
            {
                SetApiMethod("ProjectileSpawn", ref ProjectileSpawn);
            }
        }

        public class ClientMethods : ApiMethods
        {
            public ClientMethods(Dictionary<string, Delegate> methodMap) : base(methodMap)
            {
            }
        }

        public abstract class ApiMethods
        {
            private readonly Dictionary<string, Delegate> _methodMap;

            internal ApiMethods(Dictionary<string, Delegate> methodMap)
            {
                _methodMap = methodMap;
            }

            /// <summary>
            ///     Assigns a single API endpoint.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name">Shared endpoint name; matches with the framework mod.</param>
            /// <param name="method">Method to assign.</param>
            /// <exception cref="Exception"></exception>
            internal void SetApiMethod<T>(string name, ref T method) where T : class
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
