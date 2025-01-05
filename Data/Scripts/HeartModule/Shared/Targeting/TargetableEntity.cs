using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Shared.Utility;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.Targeting
{
    internal class TargetableEntity : ITargetable
    {
        public readonly MyEntity Entity;

        public TargetableEntity(MyEntity entity)
        {
            Entity = entity;
        }

        public TargetableEntity(IMyEntity entity)
        {
            Entity = (MyEntity) entity;
        }

        public static implicit operator TargetableEntity(MyEntity ent) => new TargetableEntity(ent);
        public static implicit operator MyEntity(TargetableEntity ent) => ent.Entity;

        public Vector3D GetPosition()
        {
            return Entity.PositionComp.GetPosition();
        }

        public MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeBlock block)
        {
            return RelationUtils.GetRelationsBetweenBlockAndEntity(block, Entity);
        }

        public MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeGrid grid)
        {
            return RelationUtils.GetRelationsBetweenGridAndEntity(grid, Entity);
        }

        public bool IsClosed()
        {
            return Entity.Closed;
        }
    }

    internal class TargetableProjectile : ITargetable
    {
        public readonly PhysicalProjectile Projectile;

        public TargetableProjectile(PhysicalProjectile entity)
        {
            Projectile = entity;
        }

        public static implicit operator TargetableProjectile(PhysicalProjectile ent) => new TargetableProjectile(ent);
        public static implicit operator PhysicalProjectile(TargetableProjectile ent) => ent.Projectile;

        public Vector3D GetPosition()
        {
            return Projectile.Raycast.From;
        }

        public MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeBlock block)
        {
            return Projectile.Owner == null ? MyRelationsBetweenPlayerAndBlock.NoOwnership : RelationUtils.GetRelationsBetweenBlockAndEntity(block, (MyEntity) Projectile.Owner);
        }

        public MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeGrid grid)
        {
            return Projectile.Owner == null ? MyRelationsBetweenPlayerAndBlock.NoOwnership : RelationUtils.GetRelationsBetweenGridAndEntity(grid, (MyEntity) Projectile.Owner);
        }

        public bool IsClosed()
        {
            return !Projectile.IsActive;
        }
    }

    internal interface ITargetable
    {
        Vector3D GetPosition();
        MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeBlock block);
        MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeGrid grid);
        bool IsClosed();
    }
}
