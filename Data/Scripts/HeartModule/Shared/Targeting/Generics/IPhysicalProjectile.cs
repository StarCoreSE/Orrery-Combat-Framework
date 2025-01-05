using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.Targeting.Generics
{
    public interface IPhysicalProjectile : IHitscanProjectile
    {
        Vector3D Velocity { get; set; }
    }
}
