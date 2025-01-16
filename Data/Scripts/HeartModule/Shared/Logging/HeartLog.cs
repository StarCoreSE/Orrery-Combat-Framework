using System;
using System.IO;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Shared.Logging
{
    public class HeartLog
    {
        private readonly TextWriter _writer;
        private static HeartLog _;

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            _?._Log($"[INFO] {message}");
        }

        /// <summary>
        /// Logs a message if Orrery was compiled in debug mode.
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            _?._Log($"[DEBUG] {message}");
        }

        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="callingType"></param>
        public static void Exception(Exception ex, Type callingType)
        {
            _?._LogException(ex, callingType);
        }


        public HeartLog()
        {
            _?.Close();
            _ = this;
            _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage("debug.log", typeof(HeartLog));

            int utcOffset = (DateTime.Now - DateTime.UtcNow).Hours;

            _Log($"Log writer opened.");
            _Log($"Local DateTime: {DateTime.Now:G} (UTC {(utcOffset > 0 ? "+" : "")}{utcOffset:00}:{(DateTime.Now - DateTime.UtcNow).Minutes:00})");
            _Log($"");
            _Log($"Heart Module v{HeartData.I.Version} | Space Engineers v{MyAPIGateway.Session.Version}");
            _Log($"Server: {MyAPIGateway.Session.IsServer} | Client: {!MyAPIGateway.Utilities.IsDedicated}");
            _Log($"Session: {MyAPIGateway.Session.Name ?? "NoSession"} | Client Info: {MyAPIGateway.Multiplayer.MyName ?? "NoClient"}::{MyAPIGateway.Multiplayer.MyId}");
            _Log( "=================================================");
        }

        public void Close()
        {
            _Log("Closing log writer.");
            _writer.Close();
            _ = null;
        }

        private void _Log(string message)
        {
            _writer?.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: {message}");
            _writer?.Flush();
        }

        private void _LogException(Exception ex, Type callingType)
        {
            if (ex == null)
            {
                _Log("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            _Log($"[EXCEPTION] Exception in {callingType.FullName}!\n{ex}");
        }
    }
}
