using System;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Shared.Definitions;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.HeartApi
{
    internal partial class HeartApiMethods
    {
        private static uint ProjectileSpawn(string definition, Vector3D position, Vector3D direction, IMyEntity owner)
        {
            return ProjectileManager.SpawnProjectile(DefinitionManager.ProjectileDefinitions[definition], position, direction, owner)?.Id ?? uint.MaxValue;
        }
    }
}
