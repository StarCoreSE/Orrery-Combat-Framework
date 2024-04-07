using Heart_Module.Data.Scripts.HeartModule.Weapons.StandardClasses;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using YourName.ModName.Data.Scripts.HeartModule.Weapons.Setup.Adding;


namespace Heart_Module.Data.Scripts.HeartModule.ResourceSystem
{
    internal class WeaponResourceSystem
    {
        private readonly WeaponDefinitionBase _weaponDefinition;
        private readonly SorterWeaponLogic _weaponLogic;
        private Dictionary<string, float> _resources;

        public WeaponResourceSystem(WeaponDefinitionBase weaponDefinition, SorterWeaponLogic weaponLogic)
        {
            _weaponDefinition = weaponDefinition;
            _weaponLogic = weaponLogic;
            _resources = new Dictionary<string, float>();

            // Check if the weapon definition has a resource section
            if (_weaponDefinition.Loading.Resources == null)
            {
                // If the resource section is missing, create an empty dictionary
                return;
            }

            // Initialize resources with maximum storage capacity
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                // Check for invalid resource definitions
                if (string.IsNullOrEmpty(resource.ResourceType))
                {
                    throw new Exception("Invalid resource type defined in the weapon definition.");
                }

                // Check for negative or zero resource storage
                if (resource.ResourceStorage <= 0)
                {
                    throw new Exception($"Invalid resource storage value for {resource.ResourceType}.");
                }

                _resources.Add(resource.ResourceType, resource.ResourceStorage);
            }
        }

        public bool CanShoot()
        {
            // Check if the weapon logic is null or if SorterWep is null
            if (_weaponLogic == null || _weaponLogic.SorterWep == null)
                return false;

            // Check if CubeGrid is null or if CubeGrid.Physics is null
            if (_weaponLogic.SorterWep.CubeGrid == null || _weaponLogic.SorterWep.CubeGrid.Physics == null)
                return false;

            // Check if the loading resources are null
            if (_weaponDefinition.Loading.Resources == null)
                return true;

            // Check if there are enough resources for at least one shot
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                if (!_resources.ContainsKey(resource.ResourceType))
                {
                    // Handle the case where the resource type is not found in the dictionary
                    // This could happen if the resource is not initialized properly
                    // You can log an error or handle it based on your requirements
                    return false;
                }

                if (_resources[resource.ResourceType] < resource.MinResourceBeforeFire)
                {
                    if (MyAPIGateway.Multiplayer.IsServer)
                    {
                        ShowNotification($"Insufficient {resource.ResourceType} to fire. Current {resource.ResourceType} count: {_resources[resource.ResourceType]}", 1000); // Show notification for insufficient resources
                    }
                    return false;
                }
            }
            return true;
        }

        public void ConsumeResources()
        {
            // Check if the block has valid physics
            if (_weaponLogic.SorterWep.CubeGrid.Physics == null)
                return;

            // Check if the loading resources are null
            if (_weaponDefinition.Loading.Resources == null)
                return;

            // Consume resources per shot
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                _resources[resource.ResourceType] -= resource.ResourcePerShot;
                if (_resources[resource.ResourceType] < 0) _resources[resource.ResourceType] = 0;
            }

            // Update resource status after consumption
            //if (MyAPIGateway.Multiplayer.IsServer)
            //{
            //    ShowResourceStatus();
            //}
        }

        public void RegenerateResources(float deltaTime)
        {
            // Check if the block has valid physics
            if (_weaponLogic.SorterWep.CubeGrid.Physics == null)
                return;

            // Check if the loading resources are null
            if (_weaponDefinition.Loading.Resources == null)
                return;

            // Regenerate resources over time
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                _resources[resource.ResourceType] += resource.ResourceGeneration * deltaTime;
                if (_resources[resource.ResourceType] > resource.ResourceStorage)
                    _resources[resource.ResourceType] = resource.ResourceStorage;
            }

            // Update resource status after regeneration
            //if (MyAPIGateway.Multiplayer.IsServer)
            //{
            //    ShowResourceStatus();
            //}
        }

        // Call this method every update tick
        public void Update(float deltaTime)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                RegenerateResources(deltaTime);
            }
        }

        private void ShowResourceStatus()
        {
            // Display the current count of each resource
            foreach (var resource in _weaponDefinition.Loading.Resources)
            {
                ShowNotification($"Current {resource.ResourceType} count: {_resources[resource.ResourceType]}", 1000);
            }
        }

        private void ShowNotification(string message, int duration)
        {
            MyAPIGateway.Utilities.ShowNotification(message, duration);
        }

        public void Unload()
        {
            _resources = null; // Release the dictionary
        }
    }
}
