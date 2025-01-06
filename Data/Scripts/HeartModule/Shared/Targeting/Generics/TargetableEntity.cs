using Orrery.HeartModule.Shared.Utility;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.Targeting.Generics
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
            Entity = (MyEntity)entity;
        }

        public static implicit operator TargetableEntity(MyEntity ent) => new TargetableEntity(ent);
        public static implicit operator MyEntity(TargetableEntity ent) => ent.Entity;

        public Vector3D Position => Entity.PositionComp.GetPosition();
        public Vector3D Velocity => Entity.Physics?.LinearVelocity ?? Vector3.Zero;

        public MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeBlock block)
        {
            return RelationUtils.GetRelationsBetweenBlockAndEntity(block, Entity);
        }

        public MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeGrid grid)
        {
            return RelationUtils.GetRelationsBetweenGridAndEntity(grid, Entity);
        }

        public bool IsClosed => Entity.Closed;
    }

    internal class TargetableProjectile : ITargetable
    {
        public readonly IPhysicalProjectile Projectile;

        public TargetableProjectile(IPhysicalProjectile entity)
        {
            Projectile = entity;
        }

        public Vector3D Position => Projectile.Position;
        public Vector3D Velocity => Projectile.Velocity;

        public MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeBlock block)
        {
            return Projectile.Owner == null ? MyRelationsBetweenPlayerAndBlock.NoOwnership : RelationUtils.GetRelationsBetweenBlockAndEntity(block, (MyEntity)Projectile.Owner);
        }

        public MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeGrid grid)
        {
            return Projectile.Owner == null ? MyRelationsBetweenPlayerAndBlock.NoOwnership : RelationUtils.GetRelationsBetweenGridAndEntity(grid, (MyEntity)Projectile.Owner);
        }

        public bool IsClosed => !Projectile.IsActive;
    }

    public interface ITargetable
    {
        Vector3D Position { get; }
        Vector3D Velocity { get; }
        MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeBlock block);
        MyRelationsBetweenPlayerAndBlock GetRelations(IMyCubeGrid grid);
        bool IsClosed { get; }
    }
}
