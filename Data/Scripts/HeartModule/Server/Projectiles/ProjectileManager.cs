using System.Collections.Generic;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Networking;
using Orrery.HeartModule.Shared.Utility;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Projectiles
{
    internal class ProjectileManager
    {
        private static ProjectileManager _;

        private readonly HashSet<HitscanProjectile> _projectiles = new HashSet<HitscanProjectile>();
        private readonly HashSet<HitscanProjectile> _projectilesWithHealth = new HashSet<HitscanProjectile>();
        private readonly HashSet<HitscanProjectile> _deadProjectiles = new HashSet<HitscanProjectile>();
        private uint _maxProjectileId = 0;

        public ProjectileManager()
        {
            _ = this;
        }

        public void Close()
        {
            _ = null;
        }

        public void UpdateBeforeSimulation()
        {
            MyAPIGateway.Parallel.ForEach(_projectiles, projectile =>
            {
                projectile.UpdateTick(1/60d);
                if (projectile.Definition.NetworkingDef.DoConstantSync) // TODO: Smart (rate-limited) network syncing
                    ServerNetwork.SendToEveryoneInSync((SerializedSyncProjectile) projectile, projectile.Raycast.From);
            });
        }

        public void UpdateAfterSimulation()
        {
            MyAPIGateway.Parallel.ForEach(_projectiles, projectile =>
            {
                projectile.UpdateAfterTick(1/60d);

                if (!projectile.IsActive || projectile.Definition.PhysicalProjectileDef.IsHitscan)
                    _deadProjectiles.Add(projectile);
            });

            foreach (var deadProjectile in _deadProjectiles)
            {
                ServerNetwork.SendToEveryoneInSync((SerializedCloseProjectile) deadProjectile, deadProjectile.Raycast.From);
                _projectiles.Remove(deadProjectile);
                _projectilesWithHealth.Remove(deadProjectile);
            }

            _deadProjectiles.Clear();
        }

        public static void SpawnProjectile(HitscanProjectile projectile)
        {
            projectile.Id = _._maxProjectileId++;
            _._projectiles.Add(projectile);

            if (projectile.Definition.PhysicalProjectileDef.Health > 0)
                _._projectilesWithHealth.Add(projectile);

            ServerNetwork.SendToEveryoneInSync((SerializedSpawnProjectile) projectile, projectile.Raycast.From);
        }

        public static HitscanProjectile SpawnProjectile(ProjectileDefinitionBase definition, Vector3D position, Vector3D direction, IMyEntity owner = null)
        {
            HitscanProjectile projectile;
            if (definition.PhysicalProjectileDef.IsHitscan)
                projectile = new HitscanProjectile(definition, position, direction, owner);
            else
                projectile = new PhysicalProjectile(definition, position, direction, owner);

            SpawnProjectile(projectile);
            return projectile;
        }

        public static int ActiveProjectiles => _._projectiles.Count;

        public static void CloseProjectile(HitscanProjectile projectile)
        {
            projectile.IsActive = false;
            _._deadProjectiles.Add(projectile);
        }

        public static void GetProjectilesInSphere(BoundingSphereD sphere, ref HashSet<PhysicalProjectile> projectiles)
        {
            projectiles.Clear();
            if (_._projectilesWithHealth.Count == 0)
                return;

            foreach (var p in _._projectilesWithHealth)
            {
                var projectile = p as PhysicalProjectile;

                if (projectile != null &&
                    Vector3D.DistanceSquared(projectile.Raycast.From, sphere.Center) <=
                    (projectile.Definition.PhysicalProjectileDef.ProjectileSize + sphere.Radius) *
                    (projectile.Definition.PhysicalProjectileDef.ProjectileSize + sphere.Radius))
                    projectiles.Add(projectile);
            }
        }

        public static void GetProjectilesInLine(LineD line, ref HashSet<PhysicalProjectile> projectiles)
        {
            projectiles.Clear();
            if (_._projectilesWithHealth.Count == 0)
                return;

            foreach (var p in _._projectilesWithHealth)
            {
                var projectile = p as PhysicalProjectile;

                if (projectile != null &&
                    Vector3D.DistanceSquared(projectile.Raycast.From, line.From) <= (projectile.Definition.PhysicalProjectileDef.ProjectileSize + line.Length) * (projectile.Definition.PhysicalProjectileDef.ProjectileSize + line.Length) &&
                    projectile.CollisionSphere.RayIntersect(line)
                    )
                    projectiles.Add(projectile);
            }
        }
    }
}
