using System.Collections.Generic;
using System.Linq;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Networking;
using VRage.ModAPI;
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
            foreach (var projectile in _projectiles)
            {
                projectile.UpdateTick(1/60d);
                if (projectile.Definition.NetworkingDef.DoConstantSync) // TODO: Smart (rate-limited) network syncing
                    ServerNetwork.SendToEveryoneInSync((SerializedSyncProjectile) projectile, projectile.Raycast.From);

                if (!projectile.IsActive || projectile.Definition.PhysicalProjectileDef.IsHitscan)
                    _deadProjectiles.Add(projectile);
            }

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
            _._deadProjectiles.Add(projectile);
        }

        public static IEnumerable<PhysicalProjectile> GetProjectilesInSphere(BoundingSphereD sphere)
        {
            return _._projectiles.Where(p =>
                p is PhysicalProjectile && ((PhysicalProjectile) p).Health > 0 && Vector3D.DistanceSquared(p.Raycast.From, sphere.Center) <=
                (p.Definition.PhysicalProjectileDef.ProjectileSize + sphere.Radius) *
                (p.Definition.PhysicalProjectileDef.ProjectileSize + sphere.Radius)).Select(p => p as PhysicalProjectile);
        }

        public static IEnumerable<PhysicalProjectile> GetProjectilesInLine(LineD line)
        {
            RayD ray = new RayD(line.From, line.Direction);
            BoundingSphereD sphere = new BoundingSphereD();
            return _._projectiles.Where(p =>
            {
                var projectile = p as PhysicalProjectile;
                if (projectile == null || projectile.Health <= 0)
                    return false;
                sphere.Center = p.Raycast.From;
                sphere.Radius = p.Definition.PhysicalProjectileDef.ProjectileSize;
                return (sphere.Intersects(ray) ?? double.MaxValue) <= line.Length;
            }).Select(p => p as PhysicalProjectile);
        }
    }
}
