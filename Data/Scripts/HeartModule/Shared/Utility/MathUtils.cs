using System;

namespace Orrery.HeartModule.Shared.Utility
{
    public static class MathUtils
    {
        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static double ClampAbs(double value, double absMax) => Clamp(value, -absMax, absMax);

        public static double MinAbs(double value1, double value2)
        {
            if (Math.Abs(value1) < Math.Abs(value2))
                return value1;
            return value2;
        }

        public static double MaxAbs(double value1, double value2)
        {
            if (Math.Abs(value1) > Math.Abs(value2))
                return value1;
            return value2;
        }

        public static double LimitRotationSpeed(double currentAngle, double targetAngle, double maxRotationSpeed)
        {
            // https://yal.cc/angular-rotations-explained/
            // It should NOT HAVE BEEN THAT HARD
            // I (aristeas) AM REALLY STUPID

            var diff = NormalizeAngle(targetAngle - currentAngle);

            // clamp rotations by speed:
            if (diff < -maxRotationSpeed) return currentAngle - maxRotationSpeed;
            if (diff > maxRotationSpeed) return currentAngle + maxRotationSpeed;
            // if difference within speed, rotation's done:
            return targetAngle;
        }

        public static double NormalizeAngle(double angleRads, double limit = Math.PI)
        {
            if (angleRads > limit)
                return (angleRads % limit) - limit;
            if (angleRads < -limit)
                return (angleRads % limit) + limit;
            return angleRads;
        }
    }
}
