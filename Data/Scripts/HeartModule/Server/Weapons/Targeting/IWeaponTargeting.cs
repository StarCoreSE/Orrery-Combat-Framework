using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Server.Weapons.Targeting
{
    internal interface IWeaponTargeting
    {
        Vector3D? TargetPosition { get; }
        SorterWeaponLogic Weapon { get; }
        MyEntity Target { get; }
        void UpdateTargeting();
        void SetTarget(MyEntity target);
    }
}
