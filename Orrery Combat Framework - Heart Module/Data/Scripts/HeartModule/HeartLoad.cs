﻿using Heart_Module.Data.Scripts.HeartModule.Definitions;
using Heart_Module.Data.Scripts.HeartModule.Definitions.ApiHandler;
using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using RichHudFramework.Client;
using Sandbox.ModAPI;
using System;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Adding;
using Heart_Module.Data.Scripts.HeartModule.Weapons.Setup.Hiding;

namespace Heart_Module.Data.Scripts.HeartModule
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation, priority: int.MaxValue)]
    internal class HeartLoad : MySessionComponentBase
    {
        private static HeartLoad I;

        CriticalHandle handle;
        ApiSender apiSender;
        DefinitionReciever definitionReciever;
        CommandHandler commands;
        int remainingDegradedModeTicks = 30;

        public override void LoadData()
        {
            I = this;
            HeartData.I = new HeartData();
            HeartLog.Log($"Start loading core...");

            handle = new CriticalHandle();
            handle.LoadData();

            try
            {
                HeartData.I.Net.LoadData();

                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    RichHudClient.Init("HeartModule", () => { }, () => { });
                    HeartLog.Log($"Loaded RichHudClient");
                }
                else
                    HeartLog.Log($"Skipped loading RichHudClient");

                WeaponDefinitionManager.I = new WeaponDefinitionManager();
                ProjectileDefinitionManager.I = new ProjectileDefinitionManager();
                HeartLog.Log($"Initialized DefinitionManagers");

                definitionReciever = new DefinitionReciever();
                definitionReciever.LoadData();

                apiSender = new ApiSender();
                apiSender.LoadData();

                commands = new CommandHandler();
                commands.Init();

                HeartData.I.IsSuspended = false;
                HeartLog.Log($"Finished loading core.");
            }
            catch (Exception ex)
            {
                CriticalHandle.ThrowCriticalException(ex, typeof(HeartLoad));
            }
        }

        public override void UpdateAfterSimulation()
        {
            // This has the power to shut down the server. Afaik the only way to do this is throwing an exception. Yeah.
            handle.Update();
            byte[] a = BitConverter.GetBytes(1.2f);
            try
            {
                if (HeartData.I.IsSuspended)
                    return;
                HeartData.I.IsPaused = false;

                if (!HeartData.I.IsLoaded) // Definitions can load after blocks do :)
                {
                    if (MyAPIGateway.Entities != null)
                    {
                        HeartLog.Log("UpdateAfterSimulation: Adding entity event handlers");
                        MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
                        MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;

                        MyAPIGateway.Entities.GetEntities(null, ent =>
                        {
                            OnEntityAdd(ent);
                            return false;
                        });
                    }
                    else
                    {
                        HeartLog.Log("UpdateAfterSimulation: MyAPIGateway.Entities is null, skipping event handler setup");
                    }

                    HeartData.I.IsLoaded = true;

                    HideSorterControls.DoOnce();
                    SorterWeaponTerminalControls.DoOnce(ModContext);
                }

                if (!MyAPIGateway.Utilities.IsDedicated && HeartData.I.SteamId == 0)
                    HeartData.I.SteamId = MyAPIGateway.Session?.Player?.SteamUserId ?? 0;

                HeartData.I.Net.Update(); // Update network stats

                if (MyAPIGateway.Session.IsServer) // Get players
                {
                    HeartData.I.Players.Clear(); // KEEN DOESN'T. CLEAR. THE LIST. AUTOMATICALLY. AUGH. -aristeas
                    MyAPIGateway.Multiplayer.Players.GetPlayers(HeartData.I.Players);
                }

                if (MyAPIGateway.Physics.SimulationRatio < 0.7 && !HeartData.I.IsPaused) // Set degraded mode
                {
                    if (!HeartData.I.DegradedMode)
                    {
                        if (remainingDegradedModeTicks >= 60) // Wait 300 ticks before engaging degraded mode
                        {
                            HeartData.I.DegradedMode = true;
                            if (MyAPIGateway.Session.IsServer)
                                MyAPIGateway.Utilities.SendMessage("[OCF] Entering degraded mode!");
                            MyAPIGateway.Utilities.ShowMessage("[OCF]", "Entering client degraded mode!");
                            remainingDegradedModeTicks = 60; //end it after this amount
                        }
                        else
                            remainingDegradedModeTicks++;
                    }
                }
                else if (MyAPIGateway.Physics.SimulationRatio > 0.87)
                {
                    if (remainingDegradedModeTicks <= 0 && HeartData.I.DegradedMode)
                    {
                        HeartData.I.DegradedMode = false;
                        if (MyAPIGateway.Session.IsServer)
                            MyAPIGateway.Utilities.SendMessage("[OCF] Exiting degraded mode.");
                        MyAPIGateway.Utilities.ShowMessage("[OCF]", "Exiting client degraded mode.");
                        remainingDegradedModeTicks = 0;
                    }
                    else if (remainingDegradedModeTicks > 0)
                        remainingDegradedModeTicks--;
                }
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex);
            }
        }

        public override void UpdatingStopped()
        {
            HeartData.I.IsPaused = true;
        }

        protected override void UnloadData()
        {
            commands.Close();

            handle.UnloadData();
            HeartData.I.Net.UnloadData();
            HeartLog.Log($"Unloaded HeartNetwork");

            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;

            definitionReciever.UnloadData();
            WeaponDefinitionManager.I = null;
            ProjectileDefinitionManager.I = null;
            HeartLog.Log($"Closed DefinitionManagers");

            apiSender.UnloadData();

            HeartLog.Log($"Closing core, log finishes here.");
            HeartData.I.Log.Close();
            HeartData.I = null;

            I = null;
        }

        private void OnEntityAdd(IMyEntity entity)
        {
            try
            {
                // Ensure the entity is not null
                if (entity == null)
                    return;

                // Try to cast to IMyCubeGrid, return if it's not a grid
                var grid = entity as IMyCubeGrid;
                if (grid == null)
                    return;

                // Skip grids without physics (ghost/projection grids)
                if (grid.Physics == null)
                    return;

                // Only proceed if HeartData.I.OnGridAdd is not null
                if (HeartData.I.OnGridAdd != null)
                {
                    // Invoke the OnGridAdd action for valid CubeGrid
                    HeartData.I.OnGridAdd.Invoke(grid);
                }
            }
            catch (Exception ex)
            {
                HeartLog.Log($"OnEntityAdd: Exception occurred: {ex.Message}");
                SoftHandle.RaiseException(ex); // Handle the exception with your defined SoftHandle mechanism
            }
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            try
            {
                // Ensure the entity is not null
                if (entity == null)
                    return;

                // Try to cast to IMyCubeGrid, return if it's not a grid
                var grid = entity as IMyCubeGrid;
                if (grid == null)
                    return;

                // Skip grids without physics (ghost/projection grids)
                if (grid.Physics == null)
                    return;

                // Only proceed if HeartData.I.OnGridRemove is not null
                if (HeartData.I.OnGridRemove != null)
                {
                    // Invoke the OnGridRemove action for valid CubeGrid
                    HeartData.I.OnGridRemove.Invoke(grid);
                }
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex); // Handle the exception with your defined SoftHandle mechanism
            }
        }

        public static void ResetDefinitions()
        {
            WeaponDefinitionManager.ClearDefinitions();

            ProjectileDefinitionManager.ClearDefinitions();

            // Re-request definitions
            I.definitionReciever.UnloadData();
            I.definitionReciever.LoadData();
        }
    }
}
