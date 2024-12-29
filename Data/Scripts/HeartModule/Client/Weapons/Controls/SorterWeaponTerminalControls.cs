using System;
using System.Collections.Generic;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Orrery.HeartModule.Client.Weapons.Controls
{
    public static class SorterWeaponTerminalControls
    {
        const string IdPrefix = "ModularHeartMod_";
        public static bool Done { get; private set; } = false;

        public static void DoOnce(IMyModContext context)
        {
            if (Done)
                return;
            Done = true;
            CreateControls();
            CreateActions(context);

            MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
            HeartLog.Info("Created terminal controls and actions.");
        }

        private static void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            SorterWeaponLogic logic = block?.GameLogic?.GetAs<SorterWeaponLogic>();
            if (logic == null)
                return;

            foreach (var control in controls)
            {
                if (control.Id == (IdPrefix + "HeartAmmoComboBox")) // Set ammos based on availability
                {
                    ((IMyTerminalControlCombobox)control).ComboBoxContent = (list) =>
                    {
                        for (int i = 0; i < logic.Definition.Loading.Ammos.Length; i++)
                            list.Add(new MyTerminalControlComboBoxItem() { Key = i, Value = MyStringId.GetOrCompute(logic.Definition.Loading.Ammos[i]) });
                    };
                    break;
                }
            }
        }

        static bool WeaponVisibleCondition(IMyTerminalBlock b)
        {
            // only visible for the blocks having this gamelogic comp
            return b?.GameLogic?.GetAs<SorterWeaponLogic>() != null;
        }

        static bool TurretVisibleCondition(IMyTerminalBlock b)
        {
            // only visible for the blocks having this gamelogic comp
            return b?.GameLogic?.GetAs<SorterTurretLogic>() != null;
        }

        /// <summary>
        /// Return the ammo name of a given projectile.
        /// </summary>
        /// <param name="ammoKey"></param>
        /// <returns></returns>
        private static string GetAmmoTypeName(this SorterWeaponLogic weapon)
        {
            return DefinitionManager.ProjectileDefinitions.GetValueOrDefault(weapon.Definition.Loading.Ammos[weapon.Settings.AmmoLoadedIdx])?.Name ?? "Unknown Ammo";
        }

        static void CreateControls()
        {
            #region HeartWeaponOptions
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyConveyorSorter>(""); // separators don't store the id
                c.SupportsMultipleBlocks = true;
                c.Visible = WeaponVisibleCondition;
                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyConveyorSorter>(IdPrefix + "HeartWeaponOptionsDivider");
                c.Label = MyStringId.GetOrCompute("=== HeartWeaponOptions ===");
                c.SupportsMultipleBlocks = true;
                c.Visible = WeaponVisibleCondition;
                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                ControlsHelper.CreateToggle<SorterWeaponLogic>(
                   "HeartWeaponShoot",
                   "Toogle Shoot",
                   "TargetGridsDesc",
                   (b) => b.GameLogic.GetAs<SorterWeaponLogic>().Settings.ShootState,
                   (b, v) => b.GameLogic.GetAs<SorterWeaponLogic>().Settings.ShootState = v
                   );
            }
            {
                ControlsHelper.CreateToggle<SorterWeaponLogic>(
                   "HeartWeaponMouseShoot",
                   "Toogle Mouse Shoot",
                   "TargetGridsDesc",
                   (b) => b.GameLogic.GetAs<SorterWeaponLogic>().Settings.MouseShootState,
                   (b, v) => b.GameLogic.GetAs<SorterWeaponLogic>().Settings.MouseShootState = v
                   );
            }
            {
                var AmmoComboBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyConveyorSorter>(IdPrefix + "HeartAmmoComboBox");
                AmmoComboBox.Title = MyStringId.GetOrCompute("Ammo Type");
                AmmoComboBox.Tooltip = MyStringId.GetOrCompute("HeartAmmoComboBoxDesc");
                AmmoComboBox.SupportsMultipleBlocks = true;
                AmmoComboBox.Visible = WeaponVisibleCondition;

                // Link the combobox to the Terminal_Heart_AmmoComboBox property
                AmmoComboBox.Getter = (b) =>
                {
                    var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                    if (logic != null)
                    {
                        return logic.Settings.AmmoLoadedIdx;
                    }
                    return -1; // Return a default value (e.g., -1) when the index is out of bounds
                };
                AmmoComboBox.Setter = (b, key) => b.GameLogic.GetAs<SorterWeaponLogic>().Settings.AmmoLoadedIdx = (int)key;
                //AmmoComboBox.ComboBoxContent = HeartData.I.AmmoComboBoxSetter; // Set combo box based on what's open

                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(AmmoComboBox);
            }
            #endregion

            #region HeartWeaponTargetingOptions
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyConveyorSorter>(IdPrefix + "HeartWeaponTargetingOptionsDivider");
                c.Label = MyStringId.GetOrCompute("=== HeartWeaponTargetingOptions === ");
                c.SupportsMultipleBlocks = true;
                c.Visible = TurretVisibleCondition;
                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                ControlsHelper.CreateSlider<SorterTurretLogic>(
                    "HeartAIRange",
                    "AI Range",
                    "HeartSliderDesc",
                    0,
                    10000,
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.AiRange,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.AiRange = v,
                    (b, sb) => sb.Append($"Current value: {Math.Round(b.GameLogic.GetAs<SorterTurretLogic>().Settings.AiRange)}")
                    )
                    .SetLimits(
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Definition.Targeting.MinTargetingRange,
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Definition.Targeting.MaxTargetingRange
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetUnique",
                    "Prefer Unique Targets",
                    "TargetUniqueDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.PreferUniqueTargetState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.PreferUniqueTargetState = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetGrids",
                    "Target Grids",
                    "TargetGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetGridsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetGridsState = v,
                    // Hide controls if not allowed to target
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetTypeEnum.TargetGrids) == TargetTypeEnum.TargetGrids
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetLargeGrids",
                    "Target Large Grids",
                    "TargetLargeGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetLargeGridsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetLargeGridsState = v,
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetTypeEnum.TargetGrids) == TargetTypeEnum.TargetGrids
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetSmallGrids",
                    "Target Small Grids",
                    "TargetSmallGridsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetSmallGridsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetSmallGridsState = v,
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetTypeEnum.TargetGrids) == TargetTypeEnum.TargetGrids
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetProjectiles",
                    "Target Projectiles",
                    "TargetProjectilesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetProjectilesState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetProjectilesState = v,
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetTypeEnum.TargetProjectiles) == TargetTypeEnum.TargetProjectiles
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetCharacters",
                    "Target Characters",
                    "TargetCharactersDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetCharactersState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetCharactersState = v,
                    (b) => ((b.GameLogic?.GetAs<SorterTurretLogic>()?.Definition.Targeting.AllowedTargetTypes ?? 0) & TargetTypeEnum.TargetCharacters) == TargetTypeEnum.TargetCharacters
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetFriendlies",
                    "Target Friendlies",
                    "TargetFriendliesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetFriendliesState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetFriendliesState = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetNeutrals",
                    "Target Neutrals",
                    "TargetNeutralsDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetNeutralsState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetNeutralsState = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetEnemies",
                    "Target Enemies",
                    "TargetEnemiesDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetEnemiesState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetEnemiesState = v
                    );
            }
            {
                ControlsHelper.CreateToggle<SorterTurretLogic>(
                    "HeartTargetUnowned",
                    "Target Unowned",
                    "TargetUnownedDesc",
                    (b) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetUnownedState,
                    (b, v) => b.GameLogic.GetAs<SorterTurretLogic>().Settings.TargetUnownedState = v
                    );
            }
            #endregion

            #region HUDOptions
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyConveyorSorter>(IdPrefix + "HeartWeaponHUDDivider");
                c.Label = MyStringId.GetOrCompute("=== HUD ===");
                c.SupportsMultipleBlocks = true;
                c.Visible = WeaponVisibleCondition;
                MyAPIGateway.TerminalControls.AddControl<IMyConveyorSorter>(c);
            }
            {
                ControlsHelper.CreateToggle<SorterWeaponLogic>(
                    "HeartHUDBarrelIndicatorToggle",
                    "HUD Barrel Indicator",
                    "HUDBarrelIndicatorDesc",
                    (b) => b.GameLogic.GetAs<SorterWeaponLogic>().Settings.HudBarrelIndicatorState,
                    (b, v) => b.GameLogic.GetAs<SorterWeaponLogic>().Settings.HudBarrelIndicatorState = v
                    );
            }
            #endregion
        }

        static void CreateActions(IMyModContext context)
        {
            {
                ControlsHelper.CreateAction<SorterWeaponLogic>(
                    "ToggleShoot",
                    "Toggle Shoot",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Shoot" option and ensure sync
                            logic.Settings.ShootState = !logic.Settings.ShootState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.ShootState ? "Shoot ON" : "Shoot OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterWeaponLogic>(
                    "ToggleMouseShoot",
                    "Toggle Mouse Shoot",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Shoot" option and ensure sync
                            logic.Settings.MouseShootState = !logic.Settings.MouseShootState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterWeaponLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.MouseShootState ? "Mouse ON" : "Mouse OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            //{
            //    ControlsHelper.CreateAction<SorterWeaponLogic>(
            //        "HeartControlType",
            //        "Control Type",
            //        (b) => b.GameLogic.GetAs<SorterWeaponLogic>().CycleControlType(true),
            //        (b, sb) => sb.Append($"{GetControlTypeName(b.GameLogic.GetAs<SorterWeaponLogic>().Terminal_ControlType_ComboBox)}"),
            //        @"Textures\GUI\Icons\Actions\MovingObjectToggle.dds"
            //        );
            //}
            {
                ControlsHelper.CreateAction<SorterWeaponLogic>(
                    "HeartCycleAmmoForward",
                    "Cycle Ammo",
                    (b) =>
                    {
                        var logic = b.GameLogic.GetAs<SorterWeaponLogic>();
                        if (logic == null || logic.Definition.Loading.Ammos.Length <= 1)
                            return;
                        if (logic.Settings.AmmoLoadedIdx + 1 < logic.Definition.Loading.Ammos.Length)
                            logic.Settings.AmmoLoadedIdx++;
                        else
                            logic.Settings.AmmoLoadedIdx = 0;
                    },
                    (b, sb) => sb.Append($"{b.GameLogic.GetAs<SorterWeaponLogic>().GetAmmoTypeName()}"),
                    @"Textures\GUI\Icons\Actions\MissileToggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "IncreaseAIRange",
                    "Increase AI Range",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic == null)
                            return;

                        float rangeTick = (logic.Definition.Targeting.MaxTargetingRange -
                                           logic.Definition.Targeting.MinTargetingRange) / 10f;
                        if (logic.Settings.AiRange + rangeTick < logic.Definition.Targeting.MaxTargetingRange)
                            logic.Settings.AiRange += rangeTick;
                        else if (logic.Settings.AiRange != logic.Definition.Targeting.MaxTargetingRange)
                            logic.Settings.AiRange = logic.Definition.Targeting.MaxTargetingRange;
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append($"{logic.Settings.AiRange} Range");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Increase.dds"
                    );

                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "DecreaseAIRange",
                    "Decrease AI Range",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic == null)
                            return;

                        float rangeTick = (logic.Definition.Targeting.MaxTargetingRange -
                                           logic.Definition.Targeting.MinTargetingRange) / 10f;
                        if (logic.Settings.AiRange - rangeTick > logic.Definition.Targeting.MinTargetingRange)
                            logic.Settings.AiRange -= rangeTick;
                        else if (logic.Settings.AiRange != logic.Definition.Targeting.MinTargetingRange)
                            logic.Settings.AiRange = logic.Definition.Targeting.MinTargetingRange;
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            sb.Append($"{logic.Settings.AiRange} Range");
                    },
                    @"Textures\GUI\Icons\Actions\Decrease.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleUniqueTargets",
                    "Toggle Prefer Unique",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            logic.Settings.PreferUniqueTargetState = !logic.Settings.PreferUniqueTargetState; // Toggling the value
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                            sb.Append(logic.Settings.PreferUniqueTargetState ? "UNQ ON" : "UNQ OFF");
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetGrids",
                    "Toggle Target Grids",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Grids" option and ensure sync
                            logic.Settings.TargetGridsState = !logic.Settings.TargetGridsState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetGridsState ? "Grid ON" : "Grid OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetLargeGrids",
                    "Toggle Target Large Grids",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Large Grids" option and ensure sync
                            logic.Settings.TargetLargeGridsState = !logic.Settings.TargetLargeGridsState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetLargeGridsState ? "LGrid ON" : "LGrid OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetSmallGrids",
                    "Toggle Target Small Grids",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Small Grids" option and ensure sync
                            logic.Settings.TargetSmallGridsState = !logic.Settings.TargetSmallGridsState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetSmallGridsState ? "SGrid ON" : "SGrid OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetProjectiles",
                    "Toggle Target Projectiles",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the targeting of projectiles and ensure sync
                            logic.Settings.TargetProjectilesState = !logic.Settings.TargetProjectilesState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetProjectilesState ? "Proj. ON" : "Proj. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetCharacters",
                    "Toggle Target Characters",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Characters" option and ensure sync
                            logic.Settings.TargetCharactersState = !logic.Settings.TargetCharactersState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetCharactersState ? "Char. ON" : "Char. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetFriendlies",
                    "Toggle Target Friendlies",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Friendlies" option and ensure sync
                            logic.Settings.TargetFriendliesState = !logic.Settings.TargetFriendliesState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetFriendliesState ? "Fr. ON" : "Fr. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetNeutrals",
                    "Toggle Target Neutrals",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Neutrals" option and ensure sync
                            logic.Settings.TargetNeutralsState = !logic.Settings.TargetNeutralsState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetNeutralsState ? "Neu. ON" : "Neu. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetEnemies",
                    "Toggle Target Enemies",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Enemies" option and ensure sync
                            logic.Settings.TargetEnemiesState = !logic.Settings.TargetEnemiesState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetEnemiesState ? "Enem. ON" : "Enem. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }
            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleTargetUnowned",
                    "Toggle Target Unowned",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Unowned" option and ensure sync
                            logic.Settings.TargetUnownedState = !logic.Settings.TargetUnownedState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.TargetUnownedState ? "Unow. ON" : "Unow. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }

            {
                ControlsHelper.CreateAction<SorterTurretLogic>(
                    "ToggleHUDBarrelIndicator",
                    "Toggle HUD Barrel Indicator",
                    (b) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            // Toggle the "Target Unowned" option and ensure sync
                            logic.Settings.HudBarrelIndicatorState = !logic.Settings.HudBarrelIndicatorState; // Toggling the value
                        }
                    },
                    (b, sb) =>
                    {
                        var logic = b?.GameLogic?.GetAs<SorterTurretLogic>();
                        if (logic != null)
                        {
                            sb.Append(logic.Settings.HudBarrelIndicatorState ? "Ind. ON" : "Ind. OFF");
                        }
                    },
                    @"Textures\GUI\Icons\Actions\Toggle.dds"
                    );
            }

        }
    }
}