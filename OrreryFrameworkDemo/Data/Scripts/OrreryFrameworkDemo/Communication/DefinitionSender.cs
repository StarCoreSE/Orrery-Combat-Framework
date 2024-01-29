﻿using Sandbox.Game.GUI.DebugInputComponents;
using Sandbox.ModAPI;
using System.Security.Cryptography;
using VRage.Game.Components;
using VRage.Utils;

namespace OrreryFrameworkDemo.Data.Scripts.OrreryFrameworkDemo.Communication
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, Priority = int.MaxValue)]
    internal class DefinitionSender : MySessionComponentBase
    {
        const int DefinitionMessageId = 8643;

        byte[] SerializedStorage;
        DefinitionContainer storedDef = null;

        public override void LoadData()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;
            HeartApi.LoadData(ModContext, InitAndSendDefinitions); // Doing it this way because we don't get async stuff :(

            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, InputHandler);
        }

        private void InitAndSendDefinitions()
        {
            storedDef = HeartDefinitions.GetBaseDefinitions();
            SerializedStorage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);
            MyLog.Default.WriteLineAndConsole($"OrreryDefinition [{ModContext.ModName}]: Packaged definitions & preparing to send.");

            MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, SerializedStorage);
            foreach (var def in storedDef.AmmoDefs)
                def.LiveMethods.RegisterMethods(def.Name);
            MyLog.Default.WriteLineAndConsole($"OrreryDefinition [{ModContext.ModName}]: Sent definitions & returning to sleep.");
        }

        private void InputHandler(object o)
        {
            if (o is bool && (bool)o && storedDef != null)
            {
                MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, SerializedStorage);
                foreach (var def in storedDef.AmmoDefs)
                    def.LiveMethods.RegisterMethods(def.Name);
                MyLog.Default.WriteLineAndConsole($"OrreryDefinition [{ModContext.ModName}]: Sent definitions & returning to sleep.");
            }
        }

        protected override void UnloadData()
        {
            HeartApi.UnloadData();
            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, InputHandler);
        }
    }
}
