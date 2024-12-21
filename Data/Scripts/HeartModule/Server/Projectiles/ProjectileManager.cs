using System.Collections.Generic;
using Orrery.HeartModule.Server.Networking;
using Orrery.HeartModule.Shared.Networking;

namespace Orrery.HeartModule.Server.Projectiles
{
    internal class ProjectileManager
    {
        private readonly HashSet<HitscanProjectile> _projectiles = new HashSet<HitscanProjectile>();
        private readonly List<HitscanProjectile> _deadProjectiles = new List<HitscanProjectile>();
        private uint _maxProjectileId = 0;

        public void Update()
        {
            foreach (var projectile in _projectiles)
            {
                projectile.UpdateTick(1/60d);
                if (projectile.Definition.NetworkingDef.DoConstantSync) // TODO: Smart (rate-limited) network syncing
                    ServerNetwork.SendToEveryoneInSync((SerializedSyncProjectile) projectile, projectile.Raycast.From);

                if (!projectile.IsActive)
                    _deadProjectiles.Add(projectile);
            }

            foreach (var deadProjectile in _deadProjectiles)
            {
                ServerNetwork.SendToEveryoneInSync((SerializedCloseProjectile) deadProjectile, deadProjectile.Raycast.From);
                _projectiles.Remove(deadProjectile);
            }

            _deadProjectiles.Clear();
        }

        public void SpawnProjectile(HitscanProjectile projectile)
        {
            projectile.Id = _maxProjectileId++;
            _projectiles.Add(projectile);

            ServerNetwork.SendToEveryoneInSync((SerializedSpawnProjectile) projectile, projectile.Raycast.From);
        }

        public int ActiveProjectiles => _projectiles.Count;
    }
}
