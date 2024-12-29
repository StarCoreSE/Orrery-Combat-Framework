using System.Collections.Generic;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Orrery.HeartModule.Client.Weapons.Controls
{
    public static class HideSorterControls
    {
        static bool Done = false;

        public static void DoOnce()
        {
            if (Done)
                return;
            Done = true;

            EditControls();
            EditActions();
            HeartLog.Info("Removed conveyor sorter controls.");
        }

        static bool AppendedCondition(IMyTerminalBlock block)
        {
            return block?.GameLogic?.GetAs<SorterWeaponLogic>() == null;
        }

        static void EditControls()
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyConveyorSorter>(out controls);

            foreach (IMyTerminalControl c in controls)
            {
                switch (c.Id)
                {
                    case "DrainAll":
                    case "blacklistWhitelist":
                    case "CurrentList":
                    case "removeFromSelectionButton":
                    case "candidatesList":
                    case "addToSelectionButton":
                        {
                            // appends a custom condition after the original condition with an AND.
                            //MyAPIGateway.Utilities.ShowNotification("Removing terminal actions!!");
                            // pick which way you want it to work:
                            //c.Enabled = TerminalChainedDelegate.Create(c.Enabled, AppendedCondition); // grays out
                            c.Visible = TerminalChainedDelegate.Create(c.Visible, AppendedCondition); // hides
                            break;
                        }
                }
            }
        }

        static void EditActions()
        {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMyConveyorSorter>(out actions);

            foreach (IMyTerminalAction a in actions)
            {
                switch (a.Id)
                {
                    case "DrainAll":
                    case "DrainAll_On":
                    case "DrainAll_Off":
                        {
                            // appends a custom condition after the original condition with an AND.

                            a.Enabled = TerminalChainedDelegate.Create(a.Enabled, AppendedCondition);
                            // action.Enabled hides it, there is no grayed-out for actions.

                            break;
                        }
                }
            }
        }
    }
}