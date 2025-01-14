using System;
using Orrery.HeartModule.Shared.Definitions;
using System.Collections.Generic;
using Orrery.HeartModule.Shared.Networking;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Orrery.HeartModule.Shared.Utility;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Shared.Targeting
{
    public class ProjectileGuidance
    {
        public readonly IPhysicalProjectile Projectile;
        public Vector3D Aimpoint { get; private set; }

        public ITargetable Target { get; private set; }

        private double _age = 0;
        private Queue<GuidanceDef> _stages;

        private PID _currentPid;
        private GuidanceDef _currentStage;
        private bool _stageActive = false;
        /// <summary>
        /// The "forward velocity" of the projectile.
        /// </summary>
        private float _velocity;

        public ProjectileGuidance(IPhysicalProjectile projectile)
        {
            Projectile = projectile;
            if (Projectile.Definition.Guidance.Length == 0)
                throw new Exception("No projectile guidance defined!");

            _stages = new Queue<GuidanceDef>(Projectile.Definition.Guidance);
            _velocity = Projectile.Definition.PhysicalProjectileDef.Velocity; // TODO: Figure out how to get the random velocity in here. Does it matter?
        }

        public ProjectileGuidance(IPhysicalProjectile projectile, ITargetable target) : this(projectile)
        {
            SetTarget(target);
        }

        public void SetTarget(ITargetable target, bool networked = true)
        {
            if (IsTargetAllowed(target))
            {
                Target = target;
                if (networked && MyAPIGateway.Session.IsServer)
                {
                    Server.Networking.ServerNetwork.SendToEveryoneInSync(new SerializedGuidance(this), Projectile.Position);
                    (Projectile as Server.Projectiles.PhysicalProjectile)?.Sync();
                }
            }
        }

        public void Update(double delta)
        {
            if (delta == 0 || !Projectile.IsActive)
                return;

            _age += delta;

            // Cycle to next stage if ready.
            if (_stages.Count > 0 && _stages.Peek().TriggerTime <= _age)
            {
                _currentStage = _stages.Dequeue();
                _currentPid = _currentStage.Pid?.GetPID();
                _stageActive = true;

                // Update velocity without changing initial velocity or gravity
                if (_currentStage.Velocity >= 0)
                {
                    Projectile.Velocity -= Projectile.Direction * _velocity;
                    _velocity = _currentStage.Velocity;
                    Projectile.Velocity += Projectile.Direction * _velocity;
                }

                if (!IsTargetAllowed(Target))
                    Target = null; // TODO retarget

                (Projectile as Server.Projectiles.PhysicalProjectile)?.Sync();
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

            if (Target == null || Target.IsClosed)
                return;

            if (_currentStage.UseAimPrediction)
            {
                Aimpoint = TargetingUtils.InterceptionPoint(Projectile.Position, Vector3D.Zero, Target,
                    (float) Projectile.Velocity.Length()) ?? Target.Position;
            }
            else
                Aimpoint = Target.Position;

            StepDirection(delta);
        }

        /// <summary>
        /// Steps the projectile towards a specified direction, with an optional PID.
        /// </summary>
        /// <param name="delta">Delta time, in seconds.</param>
        private void StepDirection(double delta)
        {
            var targetDir = (Aimpoint - Projectile.Position).Normalized();

            // turnRate and maxGs serve as ABSOLUTE LIMITS (of the absolute value). Set to -1 (or any negative value lol lmao) if you want to disable them.
            double angleDifference = Vector3D.Angle(Projectile.Direction, targetDir);

            Vector3 rotAxis = Vector3.Cross(Projectile.Direction, targetDir);
            rotAxis.Normalize();

            double maxTurnRate = _currentStage.MaxTurnRate >= 0 ? _currentStage.MaxTurnRate : double.MaxValue;

            if (_currentStage.MaxGs > 0)
            {
                double gravityLimited = Math.Tan(_currentStage.MaxGs * 9.81/_velocity); // I swear to god I did the math for this.
                maxTurnRate = Math.Min(gravityLimited, maxTurnRate);
            }

            // DELTATICK YOURSELF *RIGHT FUCKING NOW*
            maxTurnRate *= delta;

            // Check if we even have a PID, then set values according to result.
            double finalAngle;
            if (_currentPid != null)
            {
                // I always want to have an angle of zero, with an offset of zero.
                finalAngle = MathUtils.ClampAbs(_currentPid.Tick(angleDifference, 0, 0, delta), maxTurnRate);
            }
            else
            {
                finalAngle = MathUtils.ClampAbs(angleDifference, maxTurnRate);
            }

            if (finalAngle == 0 || double.IsNaN(finalAngle))
                return;

            Matrix rotationMatrix = Matrix.CreateFromAxisAngle(rotAxis, (float)finalAngle);
            Vector3D prevVelocity = Projectile.Velocity -
                                    Projectile.Direction * _velocity;
            Projectile.Direction = Vector3.Transform(Projectile.Direction, rotationMatrix).Normalized();
            Projectile.Velocity = Vector3.Transform(Projectile.Velocity - prevVelocity, rotationMatrix) + prevVelocity;
        }

        private bool IsTargetAllowed(ITargetable target)
        {
            if (target == null)
                return false;

            if (!_stageActive)
                return true;

            MyRelationsBetweenPlayerAndBlock relations = MyRelationsBetweenPlayerAndBlock.Neutral;
            if (Projectile.Owner is IMyConveyorSorter)
                relations = target.GetRelations(Projectile.Owner as IMyConveyorSorter);

            switch (relations)
            {
                case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                case MyRelationsBetweenPlayerAndBlock.Neutral:
                    return (_currentStage.IFF & IFFEnum.TargetNeutrals) == IFFEnum.TargetNeutrals;
                case MyRelationsBetweenPlayerAndBlock.Owner:
                case MyRelationsBetweenPlayerAndBlock.Friends:
                case MyRelationsBetweenPlayerAndBlock.FactionShare:
                    return (_currentStage.IFF & IFFEnum.TargetFriendlies) == IFFEnum.TargetFriendlies;
                case MyRelationsBetweenPlayerAndBlock.Enemies:
                    return (_currentStage.IFF & IFFEnum.TargetEnemies) == IFFEnum.TargetEnemies;
            }

            return false;
        }
    }
}
