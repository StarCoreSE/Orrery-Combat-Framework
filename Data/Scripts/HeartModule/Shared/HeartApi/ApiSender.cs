using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;

namespace Orrery.HeartModule.Shared.HeartApi
{
    internal class ApiSender
    {
        private const long HeartApiChannel = 8644; // https://xkcd.com/221/

        private readonly HeartApiMethods _methods = new HeartApiMethods();

        public void LoadData()
        {
            MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, _methods.CommunicationTuple); // Update mods that loaded before this one
            MyAPIGateway.Utilities.RegisterMessageHandler(HeartApiChannel, RecieveApiMethods);
            HeartLog.Debug("Orrery Combat Framework: HeartAPISender ready.");
        }

        public void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(HeartApiChannel, RecieveApiMethods);
            MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, false); // Tell all HeartApi instances to close
        }

        /// <summary>
        /// Listens for an API request.
        /// </summary>
        /// <param name="data"></param>
        public void RecieveApiMethods(object data)
        {
            if (data == null)
                return;

            if (data is bool && (bool)data)
            {
                MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, _methods.CommunicationTuple);
                HeartLog.Debug("Orrery Combat Framework: HeartAPISender send methods.");
            }
        }
    }
}
