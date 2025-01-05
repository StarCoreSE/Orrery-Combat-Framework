using System;
using System.Collections.Generic;
using System.Linq;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Networking;
using Orrery.HeartModule.Shared.Utility;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRage.Noise.Patterns;
using VRageMath;

namespace Orrery.HeartModule.Server.Projectiles
{
    internal class ProjectileManager
    {
        private static ProjectileManager _;

        private readonly HashSet<HitscanProjectile> _projectiles = new HashSet<HitscanProjectile>();
        private readonly List<HitscanProjectile> _deadProjectiles = new List<HitscanProjectile>();
        private uint _maxProjectileId = 0;

        public ProjectileManager()
        {
            _ = this;
        }

        public void Close()
        {
            _ = null;
        }

        public void Update()
        {
            MyAPIGateway.Parallel.ForEach(_projectiles, projectile =>
            {
                projectile.UpdateTick(1/60d);
                if (projectile.Definition.NetworkingDef.DoConstantSync) // TODO: Smart (rate-limited) network syncing
                    ServerNetwork.SendToEveryoneInSync((SerializedSyncProjectile) projectile, projectile.Raycast.From);

                if (!projectile.IsActive || projectile.Definition.PhysicalProjectileDef.IsHitscan)
                    _deadProjectiles.Add(projectile);
            });

            foreach (var deadProjectile in _deadProjectiles)
            {
                ServerNetwork.SendToEveryoneInSync((SerializedCloseProjectile) deadProjectile, deadProjectile.Raycast.From);
                _projectiles.Remove(deadProjectile);
            }

            _deadProjectiles.Clear();
        }

        public static void SpawnProjectile(HitscanProjectile projectile)
        {
            projectile.Id = _._maxProjectileId++;
            _._projectiles.Add(projectile);

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

        public static IEnumerable<PhysicalProjectile> GetProjectilesInSphere(BoundingSphereD sphere, Func<PhysicalProjectile, bool> check = null)
        {
            return _._projectiles.Where(p =>
            {
                var projectile = p as PhysicalProjectile;

                return projectile != null && (check?.Invoke(projectile) ?? true) &&
                       Vector3D.DistanceSquared(projectile.Raycast.From, sphere.Center) <=
                       (projectile.Definition.PhysicalProjectileDef.ProjectileSize + sphere.Radius) *
                       (projectile.Definition.PhysicalProjectileDef.ProjectileSize + sphere.Radius);
            }).Select(p => p as PhysicalProjectile);
        }

        public static IEnumerable<PhysicalProjectile> GetProjectilesInLine(LineD line, Func<PhysicalProjectile, bool> check = null)
        {
            BoundingSphereD sphere = new BoundingSphereD(line.From, line.Length);
            return GetProjectilesInSphere(sphere, check).Where(projectile =>
            {
                if (projectile == null || projectile.Health <= 0)
                    return false;
                sphere.Center = projectile.Raycast.From;
                sphere.Radius = projectile.Definition.PhysicalProjectileDef.ProjectileSize;
                return sphere.RayIntersect(line);
            });
        }
    }
}
