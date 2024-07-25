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
                            remainingDegradedModeTicks = 600;
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
                HeartLog.Log($"OnEntityAdd: Starting for entity {entity?.EntityId}");

                if (entity == null)
                {
                    HeartLog.Log("OnEntityAdd: Entity is null, skipping");
                    return;
                }

                if (HeartData.I == null)
                {
                    HeartLog.Log("OnEntityAdd: HeartData.I is null, skipping");
                    return;
                }

                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    HeartLog.Log($"OnEntityAdd: Entity is a CubeGrid {grid.EntityId}");

                    if (grid.Physics == null)
                    {
                        HeartLog.Log($"OnEntityAdd: Grid {grid.EntityId} has no physics, skipping");
                        return;
                    }

                    if (HeartData.I.OnGridAdd == null)
                    {
                        HeartLog.Log("OnEntityAdd: HeartData.I.OnGridAdd is null, skipping");
                        return;
                    }

                    HeartLog.Log($"OnEntityAdd: Invoking OnGridAdd for grid {grid.EntityId}");
                    HeartData.I.OnGridAdd.Invoke(grid);
                }
                else
                {
                    HeartLog.Log($"OnEntityAdd: Entity {entity.EntityId} is not a CubeGrid, skipping");
                }
            }
            catch (Exception ex)
            {
                HeartLog.Log($"OnEntityAdd: Exception occurred: {ex.Message}");
                HeartLog.Log($"OnEntityAdd: Stack trace: {ex.StackTrace}");
                SoftHandle.RaiseException(ex);
            }
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            try
            {
                HeartLog.Log($"OnEntityRemove: Starting for entity {entity?.EntityId}");

                if (entity == null)
                {
                    HeartLog.Log("OnEntityRemove: Entity is null, skipping");
                    return;
                }

                if (HeartData.I == null)
                {
                    HeartLog.Log("OnEntityRemove: HeartData.I is null, skipping");
                    return;
                }

                var grid = entity as IMyCubeGrid;
                if (grid != null)
                {
                    HeartLog.Log($"OnEntityRemove: Entity is a CubeGrid {grid.EntityId}");

                    if (HeartData.I.OnGridRemove == null)
                    {
                        HeartLog.Log("OnEntityRemove: HeartData.I.OnGridRemove is null, skipping");
                        return;
                    }

                    HeartLog.Log($"OnEntityRemove: Invoking OnGridRemove for grid {grid.EntityId}");
                    HeartData.I.OnGridRemove.Invoke(grid);
                }
                else
                {
                    HeartLog.Log($"OnEntityRemove: Entity {entity.EntityId} is not a CubeGrid, skipping");
                }
            }
            catch (Exception ex)
            {
                HeartLog.Log($"OnEntityRemove: Exception occurred: {ex.Message}");
                HeartLog.Log($"OnEntityRemove: Stack trace: {ex.StackTrace}");
                SoftHandle.RaiseException(ex);
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
