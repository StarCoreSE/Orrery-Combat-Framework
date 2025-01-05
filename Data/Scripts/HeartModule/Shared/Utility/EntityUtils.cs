using System.Collections.Generic;
using VRage.ModAPI;

namespace Orrery.HeartModule.Shared.Utility
{
    public static class EntityUtils
    {
        public static bool IsEntityInHierarchy(this IMyEntity thisEntity, IMyEntity entity)
        {
            return entity != null && (thisEntity == entity || thisEntity.GetTopMostParent().IsEntityChild(entity));
        }

        public static bool IsEntityChild(this IMyEntity thisEntity, IMyEntity entity)
        {
            List<IMyEntity> ents = new List<IMyEntity>(1);
            thisEntity.GetChildren(ents, child => child == entity || child.IsEntityChild(entity));
            return entity != null && (thisEntity == entity || ents.Count != 0);
        }
    }
}
