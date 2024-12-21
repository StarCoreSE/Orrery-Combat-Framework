using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Networking;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Client.Projectiles
{
    internal class HitscanProjectile
    {
        public readonly uint Id;
        public readonly ProjectileDefinitionBase Definition;
        public readonly IMyEntity Owner;
        public LineD Raycast;

        public HitscanProjectile(SerializedSpawnProjectile data)
        {
            Id = data.Id;
            Definition = DefinitionManager.GetProjectileDefinitionFromId(data.DefinitionId);
            if (data.OwnerId != 0)
                Owner = MyAPIGateway.Entities.GetEntityById(data.OwnerId);
            Raycast = new LineD(data.Position, data.Position + data.Direction);
        }

        public virtual void Update(double deltaTime = 1/60d)
        {

        }

        public virtual void UpdateDraw(double deltaTime = 1/60d)
        {

        }

        public virtual void UpdateSync(SerializedSyncProjectile data)
        {
            Raycast.From = data.Position;
            Raycast.To = data.Position + data.Direction;
        }
    }
}
