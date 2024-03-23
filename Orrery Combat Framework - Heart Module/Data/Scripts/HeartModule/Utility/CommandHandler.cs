using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Heart_Module.Data.Scripts.HeartModule.Utility
{
    public class CommandHandler
    {
        public static CommandHandler I;

        private Dictionary<string, Command> commands = new Dictionary<string, Command>()
        {
            ["help"] = new Command("HeartMod", "Displays command help.", (message) => I.ShowHelp()),
            ["debug.fillammo"] = new Command("HeartMod.Debug", "Fills all magazines on your current grid.", (message) => I.FillGridWeapons()),
            ["debug.reloadammo"] = new Command("HeartMod.Debug", "Forces all weapons on your current grid to reload.", (message) => I.ReloadGridWeapons()),
            ["debug.reloaddefs"] = new Command("HeartMod.Debug", "Clears and refreshes all weapon definitions.", (message) => { HeartLoad.ResetDefinitions(); MyAPIGateway.Utilities.ShowMessage("[OCF]", "All definitions cleared. Good luck fixing the bug!"); }),
            ["degraded"] = new Command("HeartMod", "Enter degraded mode for [arg1] seconds.", (message) => I.EnterDegradedMode(message)),
        };

        private void ShowHelp()
        {
            StringBuilder helpBuilder = new StringBuilder();
            List<string> modNames = new List<string>();
            foreach (var command in commands.Values)
                if (!modNames.Contains(command.modName))
                    modNames.Add(command.modName);

            MyAPIGateway.Utilities.ShowMessage("Orrery Combat Framework Help", "");

            foreach (var modName in modNames)
            {
                foreach (var command in commands)
                    if (command.Value.modName == modName)
                        helpBuilder.Append($"\n{{/ocf {command.Key}}}: " + command.Value.helpText);

                MyAPIGateway.Utilities.ShowMessage($"[{modName}]", helpBuilder + "\n");
                helpBuilder.Clear();
            }
        }

        private void FillGridWeapons()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            if (MyAPIGateway.Session.Player.PromoteLevel < MyPromoteLevel.SpaceMaster)
            {
                MyAPIGateway.Utilities.ShowMessage($"[HeartMod.Debug]", $"You need a minimum rank of Space Master to run this command!\nlook at this nerd trying to cheat");
                return;
            }

            IMyEntity entity = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity;

            if (entity is IMyCubeBlock)
            {
                int ct = 0;
                IMyCubeGrid grid = ((IMyCubeBlock)entity).CubeGrid;
                foreach (var weapon in WeaponManager.I.GridWeapons[grid])
                {
                    weapon.Magazines.MagazinesLoaded = weapon.Definition.Loading.MagazinesToLoad;
                    weapon.Magazines.ShotsInMag = ProjectileDefinitionManager.GetDefinition(weapon.Magazines.SelectedAmmoId).Ungrouped.ShotsPerMagazine;
                    ct++;
                }
                MyAPIGateway.Utilities.ShowMessage($"[HeartMod.Debug]", $"Filled {ct} weapons.");
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage($"[HeartMod.Debug]", "No grid found!");
            }
        }

        private void ReloadGridWeapons()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            if (MyAPIGateway.Session.Player.PromoteLevel < MyPromoteLevel.SpaceMaster)
            {
                MyAPIGateway.Utilities.ShowMessage($"[HeartMod.Debug]", $"You need a minimum rank of Space Master to run this command!\nlook at this nerd trying to cheat");
                return;
            }

            IMyEntity entity = MyAPIGateway.Session.Player.Controller.ControlledEntity.Entity;

            if (entity is IMyCubeBlock)
            {
                int ct = 0;
                IMyCubeGrid grid = ((IMyCubeBlock)entity).CubeGrid;
                foreach (var weapon in WeaponManager.I.GridWeapons[grid])
                {
                    weapon.Magazines.EmptyMagazines(true);
                    //weapon.Magazines.NextReloadTime = 0;
                    //weapon.Magazines.UpdateReload();
                    ct++;
                }
                MyAPIGateway.Utilities.ShowMessage("[HeartMod.Debug]", $"Force-reloaded {ct} weapons.");
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("[HeartMod.Debug]", "No grid found!");
            }
        }

        private void EnterDegradedMode(string[] message)
        {
            HeartData.I.DegradedMode = true;
            float seconds = 10;
            if (message.Length > 1)
                float.TryParse(message[1], out seconds);
            MyAPIGateway.Utilities.ShowMessage("[HeartMod]", $"Entering client degraded mode for {seconds} seconds!");
            HeartData.I.DegradedModeTicks = (int)(seconds * 60);
        }









        public void Init()
        {
            I?.Close(); // Close existing command handlers.
            I = this;
            MyAPIGateway.Utilities.MessageEnteredSender += Command_MessageEnteredSender;
            MyAPIGateway.Utilities.ShowMessage("[OCF]", "Chat commands registered - run \"/ocf help\" for help.");
        }

        private void Command_MessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            try
            {
                // Only register for commands
                if (messageText.Length == 0 || !messageText.ToLower().StartsWith("/ocf"))
                    return;

                sendToOthers = false;

                string[] parts = messageText.Substring(4).Trim(' ').Split(' '); // Convert commands to be more parseable

                if (parts[0] == "")
                {
                    ShowHelp();
                    return;
                }

                // Really basic command handler
                if (commands.ContainsKey(parts[0].ToLower()))
                    commands[parts[0].ToLower()].action.Invoke(parts);
                else
                    MyAPIGateway.Utilities.ShowMessage("[OCF]", $"Unrecognized command \"{messageText}\" ({sender})");
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(CommandHandler));
            }
        }

        public void Close()
        {
            MyAPIGateway.Utilities.MessageEnteredSender -= Command_MessageEnteredSender;
            I = null;
        }

        /// <summary>
        /// Registers a command for Orrery's command handler.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="action"></param>
        /// <param name="modName"></param>
        public static void AddCommand(string command, string helpText, Action<string[]> action, string modName = "HeartMod")
        {
            if (I == null)
                return;

            if (I.commands.ContainsKey(command))
            {
                SoftHandle.RaiseException("Attempted to add duplicate command " + command + " from [" + modName + "]", callingType: typeof(CommandHandler));
                return;
            }

            I.commands.Add(command, new Command(modName, helpText, action));
            HeartLog.Log($"Registered new chat command \"/{command}\" from [{modName}]");
        }

        private class Command
        {
            public string modName;
            public string helpText;
            public Action<string[]> action;

            public Command(string modName, string helpText, Action<string[]> action)
            {
                this.modName = modName;
                this.helpText = helpText;
                this.action = action;
            }
        }

        //  private bool _isInitialized = false;
        //
        //  public override void UpdateBeforeSimulation()
        //  {
        //      base.UpdateBeforeSimulation();
        //
        //      if (_isInitialized) return;
        //      if (MyAPIGateway.Session == null) return;
        //
        //      MyAPIGateway.Utilities.MessageEntered += OnMessageEntered;
        //      _isInitialized = true;
        //  }
        //
        //  private void OnMessageEntered(string messageText, ref bool sendToOthers)
        //  {
        //      sendToOthers = false; // Prevents the message from being broadcasted to other players
        //
        //      if (!messageText.StartsWith("/OCF ", StringComparison.OrdinalIgnoreCase)) return;
        //
        //      var args = messageText.Split(' ');
        //      if (args.Length < 2) return;
        //
        //      switch (args[1].ToLower())
        //      {
        //          case "fillammo":
        //              FillAmmoCommand(args.Skip(2).ToArray());
        //              break;
        //              // You can add more commands here
        //      }
        //  }
        //
        //  private void FillAmmoCommand()
        //  {
        //      var aimedBlock = GetAimedBlock();
        //      if (aimedBlock == null)
        //      {
        //          MyAPIGateway.Utilities.ShowNotification("No targeted block.", 2000, "Red");
        //          return;
        //      }
        //
        //      var grid = aimedBlock.CubeGrid;
        //      var sorters = new List<IMyTerminalBlock>();
        //      MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid).GetBlocksOfType<IMyConveyorSorter>(sorters);
        //
        //      foreach (var sorter in sorters)
        //      {
        //          var inventory = sorter.GetInventory();
        //          // Assume the sorter has a WeaponLogic_Magazines component attached or accessible
        //          var ammoDefinition = ProjectileDefinitionManager.GetDefinition(GetSelectedAmmo(sorter)); // Implement YourMethodToGetSelectedAmmo
        //          string magazineItem = ammoDefinition.Ungrouped.MagazineItemToConsume;
        //
        //          if (!string.IsNullOrWhiteSpace(magazineItem))
        //          {
        //              var itemToConsume = new MyDefinitionId(typeof(MyObjectBuilder_Component), magazineItem);
        //              int amountNeeded = YourMethodToDetermineAmountNeeded(sorter); // Implement YourMethodToDetermineAmountNeeded
        //
        //              // Attempt to add the ammo items to the sorter's inventory
        //              AddItemsToInventory(inventory, itemToConsume, amountNeeded);
        //              MyAPIGateway.Utilities.ShowNotification($"Filled {sorter.CustomName} with {magazineItem}.", 2000, "Green");
        //          }
        //          else
        //          {
        //              MyAPIGateway.Utilities.ShowNotification($"{sorter.CustomName} does not require specific ammo.", 2000, "Blue");
        //          }
        //      }
        //  }
        //
        //  private void AddItemsToInventory(IMyInventory inventory, MyDefinitionId itemToConsume, int amountNeeded)
        //  {
        //      // Implement logic to add the specified amount of the item to the inventory
        //      // This is a placeholder implementation
        //      var content = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemToConsume);
        //      inventory.AddItems(amountNeeded, content);
        //  }
        //
        //
        //  protected override void UnloadData()
        //  {
        //      base.UnloadData();
        //      MyAPIGateway.Utilities.MessageEntered -= OnMessageEntered;
        //  }
        //
    }
}
