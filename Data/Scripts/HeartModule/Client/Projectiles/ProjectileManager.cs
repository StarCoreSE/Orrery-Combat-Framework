using System.Collections.Generic;
using Orrery.HeartModule.Shared.Networking;
using Sandbox.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Client.Projectiles
{
    internal class ProjectileManager
    {
        private static ProjectileManager _;

        private Dictionary<uint, HitscanProjectile> _projectiles = new Dictionary<uint, HitscanProjectile>();
        private List<uint> _queuedCloseProjectiles = new List<uint>();

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
            foreach (var projectile in _projectiles.Values)
            {
                if (_queuedCloseProjectiles.Contains(projectile.Id))
                    continue;

                projectile.Update();
                if (Vector3D.DistanceSquared(projectile.Raycast.From, MyAPIGateway.Session.Camera.Position) > HeartData.I.SyncRangeSq)
                    _queuedCloseProjectiles.Add(projectile.Id);
            }

            foreach (var projectileId in _queuedCloseProjectiles)
            {
                _projectiles.GetValueOrDefault(projectileId, null)?.OnClose();
                _projectiles.Remove(projectileId);
            }
            _queuedCloseProjectiles.Clear();
        }

        public void UpdateDraw()
        {
            foreach (var projectile in _projectiles.Values)
            {
                projectile.UpdateDraw();
            }
        }


        public static void NetSpawnProjectile(SerializedSpawnProjectile data)
        {
            var projectile = data.Velocity == Vector3.NegativeInfinity ? new HitscanProjectile(data) : new PhysicalProjectile(data);
            _._projectiles.Add(data.Id, projectile);
        }

        public static void NetUpdateProjectile(SerializedSyncProjectile data)
        {
            if (!_._projectiles.ContainsKey(data.Id))
                return;
            _._projectiles[data.Id].UpdateSync(data);
        }

        public static void NetCloseProjectile(SerializedCloseProjectile data)
        {
            if (_._projectiles.ContainsKey(data.Id))
            {
                _._projectiles[data.Id].Raycast.From = data.Position;
                _._projectiles[data.Id].HasImpacted = data.DidImpact;
            }
            _._queuedCloseProjectiles.Add(data.Id);
        }

        public static HitscanProjectile GetProjectile(uint id)
        {
            return _._projectiles.GetValueOrDefault(id, null);
        }

        public static int ActiveProjectiles => _?._projectiles.Count ?? -1;
    }
}
