using VRageMath;

namespace Orrery.HeartModule.Shared.Utility
{
    /// <summary>
    /// Single-dimensional PID system.
    /// </summary>
    public class PID
    {
        public double kIntegral = 1;
        public double kDerivative = 1;
        public double kProportional = 1;

        private double p_error = 0;
        private double p_integral = 0;

        public PID() { }

        public PID(double kProportional, double kIntegral, double kDerivative)
        {
            this.kIntegral = kIntegral;
            this.kDerivative = kDerivative;
            this.kProportional = kProportional;
        }

        public double Tick(double current, double desired, double bias, double delta)
        {
            double error = desired - current;
            double integral = p_integral + error * delta;
            double derivative = (error - p_error) / delta;

            p_error = error;
            p_integral = integral;

            return kProportional * error + kIntegral * integral + kDerivative * derivative + bias;
        }
    }

    /// <summary>
    /// Three-dimensional PID system.
    /// </summary>
    public class VectorPID
    {
        public double kIntegral
        {
            get
            {
                return x.kIntegral;
            }
            set
            {
                x.kIntegral = value;
                y.kIntegral = value;
                z.kIntegral = value;
            }
        }
        public double kDerivative
        {
            get
            {
                return x.kDerivative;
            }
            set
            {
                x.kDerivative = value;
                y.kDerivative = value;
                z.kDerivative = value;
            }
        }
        public double kProportional
        {
            get
            {
                return x.kProportional;
            }
            set
            {
                x.kProportional = value;
                y.kProportional = value;
                z.kProportional = value;
            }
        }

        private PID x = new PID();
        private PID y = new PID();
        private PID z = new PID();

        public VectorPID(double kProportional, double kIntegral, double kDerivative)
        {
            this.kIntegral = kIntegral;
            this.kDerivative = kDerivative;
            this.kProportional = kProportional;
        }

        public Vector3D Tick(Vector3D current, Vector3D desired, Vector3D bias, double delta)
        {
            return new Vector3D(
                x.Tick(current.X, desired.X, bias.X, delta),
                y.Tick(current.Y, desired.Y, bias.Y, delta),
                z.Tick(current.Z, desired.Z, bias.Z, delta)
                );
        }
    }
}
