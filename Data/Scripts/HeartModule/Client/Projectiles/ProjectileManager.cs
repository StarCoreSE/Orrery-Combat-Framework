using System.Collections.Generic;
using Orrery.HeartModule.Shared.Networking;

namespace Orrery.HeartModule.Client.Projectiles
{
    internal class ProjectileManager
    {
        private static ProjectileManager _;

        private Dictionary<ushort, HitscanProjectile> _projectiles = new Dictionary<ushort, HitscanProjectile>();

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

        }

        public void UpdateDraw()
        {

        }


        public static void NetSpawnProjectile(SerializedSpawnProjectile data)
        {

        }

        public static void NetUpdateProjectile(SerializedSyncProjectile data)
        {

        }

        public static void NetCloseProjectile(SerializedCloseProjectile data)
        {

        }
    }
}
