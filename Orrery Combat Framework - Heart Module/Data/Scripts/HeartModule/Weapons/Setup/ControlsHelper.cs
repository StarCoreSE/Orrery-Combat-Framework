using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Text;
using VRage.Utils;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;

namespace Heart_Module.Data.Scripts.HeartModule.Weapons.Setup
{
    internal static class ControlsHelper
    {
        const string IdPrefix = "ModularHeartMod_"; // highly recommended to tag your properties/actions like this to avoid colliding with other mods'

        public static IMyTerminalControlOnOffSwitch CreateToggle<T>(string id, string displayName, string toolTip, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visible = null) where T : SorterWeaponLogic
        {
            var ShootToggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyConveyorSorter>(IdPrefix + id);
            ShootToggle.Title = MyStringId.GetOrCompute(displayName);
            ShootToggle.Tooltip = MyStringId.GetOrCompute(toolTip);
            ShootToggle.SupportsMultipleBlocks = true; // wether this control should be visible when multiple blocks are selected (as long as they all have this control).
                                                       // callbacks to determine if the control should be visible or not-grayed-out(Enabled) depending on whatever custom condition you want, given a block instance.
                                                       // optional, they both default to true.
            Func<IMyTerminalBlock, bool> visibleFunc; // TODO: This is causing *massive* performance load when using many many weapons.
            if (typeof(T) == typeof(SorterTurretLogic))
                visibleFunc = (b) => HasTurretLogic(b) && (visible?.Invoke(b) ?? true);
            else
                visibleFunc = (b) => HasWeaponLogic(b) && (visible?.Invoke(b) ?? true);

            ShootToggle.Visible = visibleFunc;
            //c.Enabled = CustomVisibleCondition;
            ShootToggle.OnText = MySpaceTexts.SwitchText_On;
            ShootToggle.OffText = MySpaceTexts.SwitchText_Off;
            //c.OffText = MyStringId.GetOrCompute("Off");
            // setters and getters should both be assigned on all controls that have them, to avoid errors in mods or PB scripts getting exceptions from them.
            ShootToggle.Getter = getter;  // Getting the value
            ShootToggle.Setter = setter; // Setting the value

            MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(ShootToggle);

            return ShootToggle;
        }

        public static IMyTerminalControlSlider CreateSlider<T>(string id, string displayName, string toolTip, float min, float max, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Action<IMyTerminalBlock, StringBuilder> writer, Func<IMyTerminalBlock, bool> visible = null)
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyConveyorSorter>(IdPrefix + id);
            slider.Title = MyStringId.GetOrCompute(displayName);
            slider.Tooltip = MyStringId.GetOrCompute(toolTip);
            slider.SetLimits(min, max); // Set the minimum and maximum values for the slider
            slider.Getter = getter; // Replace with your property
            slider.Setter = setter; // Replace with your property
            slider.Writer = writer; // Replace with your property

            Func<IMyTerminalBlock, bool> visibleFunc;
            if (typeof(T) == typeof(SorterTurretLogic))
                visibleFunc = (b) => HasTurretLogic(b) && (visible?.Invoke(b) ?? true);
            else
                visibleFunc = (b) => HasWeaponLogic(b) && (visible?.Invoke(b) ?? true);

            slider.Visible = visibleFunc;
            slider.Enabled = (b) => true; // or your custom condition
            slider.SupportsMultipleBlocks = true;

            MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(slider);
            return slider;
        }

        static bool HasWeaponLogic(IMyTerminalBlock b)
        {
            // only visible for the blocks having this gamelogic comp
            return b?.GameLogic?.GetAs<SorterWeaponLogic>() != null;
        }

        static bool HasTurretLogic(IMyTerminalBlock b)
        {
            // only visible for the blocks having this gamelogic comp
            return b?.GameLogic?.GetAs<SorterTurretLogic>() != null;
        }

        public static IMyTerminalAction CreateAction<T>(string id, string displayName, Action<IMyTerminalBlock> action, Action<IMyTerminalBlock, StringBuilder> writer, string icon) where T : SorterWeaponLogic
        {
            var cycleControlForwardAction = MyAPIGateway.TerminalControls.CreateAction<IMyConveyorSorter>(IdPrefix + id);
            cycleControlForwardAction.Name = new StringBuilder(displayName);
            cycleControlForwardAction.Action = action;
            cycleControlForwardAction.Writer = writer;
            cycleControlForwardAction.Icon = icon;

            Func<IMyTerminalBlock, bool> visibleFunc;
            if (typeof(T) == typeof(SorterTurretLogic))
                visibleFunc = HasTurretLogic;
            else
                visibleFunc = HasWeaponLogic;

            cycleControlForwardAction.Enabled = visibleFunc;
            MyAPIGateway.TerminalControls.AddAction<IMyConveyorSorter>(cycleControlForwardAction);

            return cycleControlForwardAction;
        }
    }
}
