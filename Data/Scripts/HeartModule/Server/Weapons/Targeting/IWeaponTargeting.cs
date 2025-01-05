using Orrery.HeartModule.Shared.Targeting.Generics;
using VRageMath;

namespace Orrery.HeartModule.Server.Weapons.Targeting
{
    internal interface IWeaponTargeting
    {
        Vector3D? TargetPosition { get; }
        SorterWeaponLogic Weapon { get; }
        ITargetable Target { get; }
        void UpdateTargeting();
        void SetTarget(ITargetable target);
    }
}
