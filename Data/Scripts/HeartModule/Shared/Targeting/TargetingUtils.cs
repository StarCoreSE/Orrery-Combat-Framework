using System;
using Orrery.HeartModule.Server.Projectiles;
using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Targeting.Generics;
using VRageMath;

namespace Orrery.HeartModule.Shared.Targeting
{
    internal static class TargetingUtils
    {
        public static Vector3D? InterceptionPoint(Vector3D startPos, Vector3D startVel, ITargetable target, ProjectileDefinitionBase def)
        {
            if (def == null || target == null)
                return null;
            if (def.PhysicalProjectileDef.IsHitscan)
                return target.Position - target.Velocity / 60f; // Because this doesn't run during simulation, offset velocity

            return InterceptionPoint(startPos, startVel, target, def.PhysicalProjectileDef.Velocity);
        }

        public static Vector3D? InterceptionPoint(Vector3D startPos, Vector3D startVel, ITargetable target, float projectileVelocity)
        {
            if (target == null)
                return null;

            return InterceptionPoint(startPos, startVel, target.Position, target.Velocity, projectileVelocity);
        }

        public static Vector3D? InterceptionPoint(Vector3D startPos, Vector3D startVel, Vector3D targetPos, Vector3D targetVel, float projectileSpeed)
        {
            Vector3D relativeVelocity = targetVel - startVel;

            try
            {
                double t = TimeOfInterception(targetPos - startPos, relativeVelocity, projectileSpeed);
                if (t == -1)
                    return null;

                // Calculate interception point
                Vector3D interceptionPoint = targetPos + relativeVelocity * t;

                return interceptionPoint;
            }
            catch
            {
                return null;
            }
        }

        public static double TimeOfInterception(Vector3 relativePosition, Vector3 relativeVelocity, float projectileSpeed) // Adapted from Bunny83 on the Unity forums https://discussions.unity.com/t/how-to-calculate-the-point-of-intercept-in-3d-space/22540
        {
            double velocitySquared = relativeVelocity.LengthSquared();
            if (velocitySquared < double.Epsilon)
                return 0;

            double a = velocitySquared - projectileSpeed * projectileSpeed;
            if (Math.Abs(a) < double.Epsilon)
            {
                double t = -relativePosition.LengthSquared() / (2 * Vector3D.Dot(relativeVelocity, relativePosition));
                return t > 0 ? t : -1;
            }

            double b = 2 * Vector3D.Dot(relativeVelocity, relativePosition);
            double c = relativePosition.LengthSquared();
            double determinant = b * b - 4 * a * c;

            if (determinant > 0) // Two solutions
            {
                double t1 = (-b + Math.Sqrt(determinant)) / (2 * a);
                double t2 = (-b - Math.Sqrt(determinant)) / (2 * a);
                if (t1 > 0)
                {
                    if (t2 > 0)
                        return t1 < t2 ? t1 : t2;
                    return t1;
                }
                return t2 > 0 ? t2 : -1;
            }
            else if (determinant < 0) // No solutions
                return -1;

            double solution = -b / (2 * a); // One solution
            return solution > 0 ? solution : -1;
        }
    }
}
