using Sandbox.ModAPI;
using VRage;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.HeartApi
{
    internal partial class HeartApiMethods
    {
        private static MyTuple<string, Vector3D, Vector3D, IMyEntity>? ProjectileInfo(uint projectileId)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Server.Projectiles.HitscanProjectile projectile;
                if (!Server.Projectiles.ProjectileManager.TryGetProjectile(projectileId, out projectile))
                    return null;
                return new MyTuple<string, Vector3D, Vector3D, IMyEntity>(projectile.Definition.Name, projectile.Position, projectile.Direction, projectile.Owner);
            }
            else
            {
                Client.Projectiles.HitscanProjectile projectile;
                if (!Client.Projectiles.ProjectileManager.TryGetProjectile(projectileId, out projectile))
                    return null;
                return new MyTuple<string, Vector3D, Vector3D, IMyEntity>(projectile.Definition.Name, projectile.Position, projectile.Direction, projectile.Owner);
            }
        }
    }
}
