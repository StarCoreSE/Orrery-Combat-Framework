using System;
using Orrery.HeartModule.Shared.Definitions;
using System.Collections.Generic;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Orrery.HeartModule.Shared.Utility;
using Sandbox.Game.EntityComponents;
using VRageMath;

namespace Orrery.HeartModule.Shared.Targeting
{
    public class ProjectileGuidance
    {
        public readonly IPhysicalProjectile Projectile;
        public Vector3D Aimpoint { get; private set; }

        private ITargetable _target;

        private double _age = 0;
        private Queue<GuidanceDef> _stages;

        private PID _currentPid;
        private GuidanceDef _currentStage;
        private bool _stageActive = true;

        public ProjectileGuidance(IPhysicalProjectile projectile)
        {
            Projectile = projectile;
            if (Projectile.Definition.Guidance.Length == 0)
                throw new Exception("No projectile guidance defined!");

            _stages = new Queue<GuidanceDef>(Projectile.Definition.Guidance);
            _currentStage = _stages.Dequeue();
            _currentPid = _stages.Peek().Pid?.GetPID();
        }

        public ProjectileGuidance(IPhysicalProjectile projectile, ITargetable target) : this(projectile)
        {
            SetTarget(target);
        }

        public void SetTarget(ITargetable target)
        {
            _target = target;
        }

        public void Update(double delta)
        {
            _age += delta;

            // Cycle to next stage if ready.
            if (_stages.Count > 0 && _stages.Peek().TriggerTime <= _age)
            {
                _currentStage = _stages.Dequeue();
                _currentPid = _currentStage.Pid?.GetPID();
                _stageActive = true;
            }

            // If the current stage has a defined (>0) active duration, remove it when past the defined time.
            if (_stageActive && _currentStage.ActiveDuration <= 0 && _currentStage.TriggerTime + _currentStage.ActiveDuration > _age)
            {
                _stageActive = false;
                _currentPid = null;
            }

            if (!_stageActive)
                return;

            // TODO: Raycasting

            if (_target == null || _target.IsClosed())
                return;

            if (_currentStage.UseAimPrediction)
            {
                Aimpoint = TargetingUtils.InterceptionPoint(Projectile.Position, Vector3D.Zero, _target,
                    (float) Projectile.Velocity.Length()) ?? _target.Position;
            }
            else
                Aimpoint = _target.Position;

            StepDirection(delta);
        }

        /// <summary>
        /// Steps the projectile towards a specified direction, with an optional PID.
        /// </summary>
        /// <param name="targetDir">Normalized target direction.</param>
        /// <param name="maxTurnRate">Maximum turn rate in radians.</param>
        /// <param name="maxGs">Maximum 'pull' of the missile, in Gs.</param>
        /// <param name="delta">Delta time, in seconds.</param>
        private void StepDirection(double delta)
        {
            var targetDir = (Aimpoint - Projectile.Direction).Normalized();

            // turnRate and maxGs serve as ABSOLUTE LIMITS (of the absolute value). Set to -1 (or any negative value lol lmao) if you want to disable them.
            double angleDifference = Vector3D.Angle(Projectile.Direction, targetDir);

            Vector3 rotAxis = Vector3.Cross(Projectile.Direction, targetDir);
            rotAxis.Normalize();

            double actualTurnRate = _currentStage.MaxTurnRate >= 0 ? _currentStage.MaxTurnRate : double.MaxValue;

            if (_currentStage.MaxGs >= 0)
            {
                double gravityLimited = Projectile.Definition.PhysicalProjectileDef.Velocity / (_currentStage.MaxGs * 9.81); // I swear to god I did the math for this, it really is that easy.

                actualTurnRate = Math.Min(gravityLimited, actualTurnRate);
            }

            // DELTATICK YOURSELF *RIGHT FUCKING NOW*
            actualTurnRate *= delta;

            // Check if we even have a PID, then set values according to result.
            double finalAngle;
            if (_currentPid != null)
            {
                // I always want to have an angle of zero, with an offset of zero.
                finalAngle = MathUtils.MinAbs(_currentPid.Tick(angleDifference, 0, 0, delta), actualTurnRate);
            }
            else
            {
                finalAngle = MathUtils.ClampAbs(angleDifference, actualTurnRate);
            }


            Matrix rotationMatrix = Matrix.CreateFromAxisAngle(rotAxis, (float)finalAngle);
            Vector3D prevVelocity = Projectile.Velocity -
                                    Projectile.Direction * Projectile.Definition.PhysicalProjectileDef.Velocity;
            Projectile.Direction = Vector3.Transform(Projectile.Direction, rotationMatrix).Normalized();
            Projectile.Velocity = Vector3.Transform(Projectile.Velocity - prevVelocity, rotationMatrix) + prevVelocity;
        }
    }
}
