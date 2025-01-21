using System;
using System.Collections.Generic;
using System.Linq;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Orrery.HeartModule.Shared.Weapons.Settings;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;
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

        #region Weapons

        private static byte[] GetWeaponSettings(IMyConveyorSorter sorterWep)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                var weapon = Server.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId);
                return weapon == null ? null : MyAPIGateway.Utilities.SerializeToBinary(weapon.Settings);
            }
            else
            {
                var weapon = Client.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId);
                return weapon == null ? null : MyAPIGateway.Utilities.SerializeToBinary(weapon.Settings);
            }
        }

        private static void SetWeaponSettings(IMyConveyorSorter sorterWep, byte[] serialized)
        {
            if (serialized == null || serialized.Length == 0) return;
            var settings = MyAPIGateway.Utilities.SerializeFromBinary<WeaponSettings>(serialized);

            if (MyAPIGateway.Session.IsServer)
            {
                var weapon = Server.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId);
                if (weapon == null) return;
                weapon.Settings = settings;
                weapon.Settings.Sync();
            }
            else
            {
                var weapon = Client.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId);
                if (weapon == null) return;
                weapon.Settings = settings;
                weapon.Settings.Sync();
            }
        }

        private static IEnumerable<IMyConveyorSorter> GetGridWeapons(IMyCubeGrid grid)
        {
            return grid.GetFatBlocks<IMyConveyorSorter>().Where(block => DefinitionManager.WeaponDefinitions.Values.Any(dictDefinition => dictDefinition.Assignments.BlockSubtype == block.BlockDefinition.SubtypeName));
        }

        private static bool HasWeapon(IMyConveyorSorter block)
        {
            return DefinitionManager.WeaponDefinitions.Values.Any(dictDefinition => dictDefinition.Assignments.BlockSubtype == block.BlockDefinition.SubtypeName);
        }

        private static void RegisterOnWeaponAdd(Action<IMyConveyorSorter> action)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Server.Weapons.WeaponManager.OnWeaponAdd += action;
            }
            else
            {
                Client.Weapons.WeaponManager.OnWeaponAdd += action;
            }
        }

        private static void UnregisterOnWeaponAdd(Action<IMyConveyorSorter> action)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Server.Weapons.WeaponManager.OnWeaponAdd -= action;
            }
            else
            {
                Client.Weapons.WeaponManager.OnWeaponAdd -= action;
            }
        }

        private static void RegisterOnWeaponClose(Action<IMyConveyorSorter> action)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Server.Weapons.WeaponManager.OnWeaponClose += action;
            }
            else
            {
                Client.Weapons.WeaponManager.OnWeaponClose += action;
            }
        }

        private static void UnregisterOnWeaponClose(Action<IMyConveyorSorter> action)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Server.Weapons.WeaponManager.OnWeaponClose -= action;
            }
            else
            {
                Client.Weapons.WeaponManager.OnWeaponClose -= action;
            }
        }

        private static MyTuple<IMyEntity, uint?>? GetWeaponTarget(IMyConveyorSorter sorterWep)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                var target = (Server.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId) as Server.Weapons.SorterSmartLogic)?.Targeting?.Target;
                return target == null ? null : (MyTuple<IMyEntity, uint?>?) new MyTuple<IMyEntity, uint?>((target as TargetableEntity)?.Entity,
                    (target as TargetableProjectile)?.Projectile?.Id);
            }
            else
            {
                var target = (Client.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId) as Client.Weapons.SorterSmartLogic)?.Target;
                return target == null ? null : (MyTuple<IMyEntity, uint?>?) new MyTuple<IMyEntity, uint?>((target as TargetableEntity)?.Entity,
                    (target as TargetableProjectile)?.Projectile?.Id);
            }
        }

        private static Vector3D? GetWeaponAimpoint(IMyConveyorSorter sorterWep)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                var target = (Server.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId) as Server.Weapons.SorterSmartLogic)?.Targeting;
                return target?.TargetPosition;
            }
            else
            {
                var target = (Client.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId) as Client.Weapons.SorterTurretLogic);
                return target?.GetTargetPosition(target.Target);
            }
        }

        private static MatrixD GetWeaponBarrelMatrix(IMyConveyorSorter sorterWep)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                return Server.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId).MuzzleMatrix;
            }
            else
            {
                return Client.Weapons.WeaponManager.GetWeapon(sorterWep.EntityId).MuzzleMatrix;
            }
        }

        #endregion
    }
}
