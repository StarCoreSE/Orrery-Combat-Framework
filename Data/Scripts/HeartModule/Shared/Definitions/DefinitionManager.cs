using System.Collections.Generic;
using System.Linq;
using System.Net;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using VRage.Game;

namespace Orrery.HeartModule.Shared.Definitions
{
    internal static class DefinitionManager
    {
        internal static DefinitionApi DefinitionApi;
        public static Dictionary<string, WeaponDefinitionBase> WeaponDefinitions;
        public static Dictionary<string, ProjectileDefinitionBase> ProjectileDefinitions;
        public static Dictionary<ushort, string> ProjectileDefinitionIds; // TODO: Sync this from server to client.
        internal static ushort MaxDefinitionId = 0;

        public static void LoadData()
        {
            WeaponDefinitions = new Dictionary<string, WeaponDefinitionBase>();
            ProjectileDefinitions = new Dictionary<string, ProjectileDefinitionBase>();
            ProjectileDefinitionIds = new Dictionary<ushort, string>();
            MaxDefinitionId = 0;

            DefinitionApi = new DefinitionApi();
            DefinitionApi.Init(MasterSession.I.ModContext, OnApiReady);

            HeartLog.Info("[DefinitionManager] Initialized.");
        }

        public static void UnloadData()
        {
            DefinitionApi.UnregisterOnUpdate<WeaponDefinitionBase>(OnWeaponDefinitionUpdate);
            DefinitionApi.UnregisterOnUpdate<ProjectileDefinitionBase>(OnProjectileDefinitionUpdate);
            HeartLog.Info("[DefinitionManager] DefinitionApi unregistered definition update actions.");

            DefinitionApi.UnloadData();
            DefinitionApi = null;
            HeartLog.Info("[DefinitionManager] DefinitionApi closed.");

            WeaponDefinitions = null;
            ProjectileDefinitions = null;
            ProjectileDefinitionIds = null;
            HeartLog.Info("[DefinitionManager] DefinitionManager closed.");
        }

        public static ushort? GetId(this ProjectileDefinitionBase definition) =>
            GetProjectileDefinitionId(definition.Name);

        public static ushort? GetProjectileDefinitionId(string name)
        {
            foreach (var kvp in ProjectileDefinitionIds)
                if (kvp.Value == name)
                    return kvp.Key;
            return null;
        }

        public static ProjectileDefinitionBase GetProjectileDefinitionFromId(ushort id)
        {
            foreach (var kvp in ProjectileDefinitionIds)
                if (kvp.Key == id)
                    return ProjectileDefinitions.GetValueOrDefault(kvp.Value, null);
            
            return null;
        }

        private static void OnApiReady()
        {
            DefinitionApi.RegisterOnUpdate<WeaponDefinitionBase>(OnWeaponDefinitionUpdate);
            DefinitionApi.RegisterOnUpdate<ProjectileDefinitionBase>(OnProjectileDefinitionUpdate);
            HeartLog.Info("[DefinitionManager] Registered definition update actions.");

            // Retrieve existing definition data
            foreach (string definitionId in DefinitionApi.GetDefinitionsOfType<WeaponDefinitionBase>())
                OnWeaponDefinitionUpdate(definitionId, 0);
            foreach (string definitionId in DefinitionApi.GetDefinitionsOfType<ProjectileDefinitionBase>())
                OnProjectileDefinitionUpdate(definitionId, 0);
            HeartLog.Info($"[DefinitionManager] Registered {WeaponDefinitions.Count} existing weapon definitions and {ProjectileDefinitions.Count} existing projectile definitions.");
        }

        private static void OnWeaponDefinitionUpdate(string definitionId, int updateType)
        {
            // We're caching data because getting it from the API is inefficient.
            switch (updateType)
            {
                case 0:
                    WeaponDefinitions[definitionId] = DefinitionApi.GetDefinition<WeaponDefinitionBase>(definitionId);
                    HeartLog.Info("Registered new weapon definition " + definitionId);
                    if (!MyAPIGateway.Utilities.IsDedicated)
                        Client.Interface.BlockCategoryManager.RegisterFromDefinition(WeaponDefinitions[definitionId]);
                    break;
                case 1:
                    WeaponDefinitions.Remove(definitionId); // TODO cleanup existing turrets/projectiles
                    HeartLog.Info("Unregistered weapon definition " + definitionId);
                    break;
                case 2:
                    // TODO tie into definitions
                    // var delegates = DefinitionApi.GetDelegates<WeaponDefinitionBase>(definitionId);
                    break;
            }
        }

        private static void OnProjectileDefinitionUpdate(string definitionId, int updateType)
        {
            // We're caching data because getting it from the API is inefficient.
            switch (updateType)
            {
                case 0:
                    ProjectileDefinitions[definitionId] = DefinitionApi.GetDefinition<ProjectileDefinitionBase>(definitionId);
                    ProjectileDefinitionIds[MaxDefinitionId++] = definitionId;

                    HeartLog.Info($"Registered new projectile definition {definitionId} (internal ID {MaxDefinitionId})");
                    break;
                case 1:
                    ProjectileDefinitions.Remove(definitionId); // TODO cleanup existing turrets/projectiles
                    HeartLog.Info($"Unregistered projectile definition {definitionId} (internal ID {MaxDefinitionId})");
                    break;
                case 2:
                    // TODO tie into definitions
                    // var delegates = DefinitionApi.GetDelegates<ProjectileDefinitionBase>(definitionId);
                    break;
            }
        }
    }
}
