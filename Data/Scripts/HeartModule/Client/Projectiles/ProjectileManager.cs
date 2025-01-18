using System;
using System.Collections.Generic;
using Orrery.HeartModule.Client.Weapons;
using Orrery.HeartModule.Shared.Logging;
using Orrery.HeartModule.Shared.Networking;
using Orrery.HeartModule.Shared.Utility;
using Sandbox.ModAPI;
using VRage.Game.Entity;
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
            foreach (var projectileId in _queuedCloseProjectiles)
            {
                _projectiles.GetValueOrDefault(projectileId, null)?.OnClose();
                _projectiles.Remove(projectileId);
            }
            _queuedCloseProjectiles.Clear();

            foreach (var projectile in _projectiles.Values)
            {
                projectile.Update();
                if (projectile.Definition.PhysicalProjectileDef.IsHitscan || Vector3D.DistanceSquared(projectile.Position, MyAPIGateway.Session.Camera.Position) > HeartData.I.SyncRangeSq)
                    _queuedCloseProjectiles.Add(projectile.Id);
            }

            MyAPIGateway.Utilities.ShowNotification($"Client: {ActiveProjectiles}", 1000/60);
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

            if (projectile.Owner is IMyConveyorSorter)
                WeaponManager.GetWeapon(projectile.Owner.EntityId)?.OnShoot();

            try
            {
                projectile.Definition.LiveMethods.ClientOnSpawn?.Invoke(projectile.Id, projectile.Owner);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(ProjectileManager));
            }
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
                _._projectiles[data.Id].Position = data.Position;
                _._projectiles[data.Id].HasImpacted = data.DidImpact;
            }
            _._queuedCloseProjectiles.Add(data.Id);
        }

        public static HitscanProjectile GetProjectile(uint id)
        {
            return _._projectiles.GetValueOrDefault(id, null);
        }

        public static int ActiveProjectiles => _?._projectiles.Count ?? -1;

        public static bool TryGetProjectile(uint id, out HitscanProjectile projectile) => _._projectiles.TryGetValue(id, out projectile);
    }
}
