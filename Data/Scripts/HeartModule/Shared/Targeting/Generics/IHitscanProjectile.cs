using Orrery.HeartModule.Shared.Definitions;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.Targeting.Generics
{
    public interface IHitscanProjectile
    {
        uint Id { get; }
        IMyEntity Owner { get; }
        Vector3D Position { get; set; }
        Vector3D Direction { get; set; }
        bool IsActive { get; set; }
        ProjectileDefinitionBase Definition { get; }
    }
}
