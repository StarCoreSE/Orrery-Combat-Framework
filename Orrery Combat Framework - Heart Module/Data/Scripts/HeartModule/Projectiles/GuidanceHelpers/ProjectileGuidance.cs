using Heart_Module.Data.Scripts.HeartModule.Definitions.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Utility;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles.GuidanceHelpers
{
    public class ProjectileGuidance
    {
        IMyEntity targetEntity;

        Projectile projectile;
        ProjectileDefinitionBase Definition;
        LinkedList<Guidance> stages;
        float time = 0;
        Vector3D randomOffset = Vector3D.Zero;

        PID stagePid;

        public ProjectileGuidance(Projectile projectile)
        {
            this.projectile = projectile;
            Definition = projectile.Definition;
            stages = new LinkedList<Guidance>(Definition.Guidance);

            stagePid = stages.First?.Value.PID?.GetPID();

            // Set projectile velocity
            if ((stages.First?.Value.Velocity ?? -1) != -1)
                projectile.Velocity = stages.First.Value.Velocity;
            else
                projectile.Velocity = projectile.Definition.PhysicalProjectile.Velocity;
        }

        public IMyEntity GetTarget()
        {
            return targetEntity;
        }

        public void SetTarget(IMyEntity target)
        {
            if (IsTargetAllowed(target, stages.First.Value))
                targetEntity = target;
        }

        public void RunGuidance(float delta)
        {
            time += delta;
            if (stages.Count == 0 || stages.First.Value.TriggerTime > time) // Don't run logic if no stage is active
                return;

            Guidance currentStage = stages.First.Value;

            if (currentStage.ActiveDuration == -1) // Keep guiding until next stage
            {
                if ((stages.First.Next?.Value.TriggerTime ?? float.MaxValue) <= time) // Move to next stage
                {
                    NextStage(delta);
                    return;
                }
            }
            else if (currentStage.TriggerTime + currentStage.ActiveDuration < time) // Go to next stage if projectile has ran out of time
            {
                NextStage(delta);
                return;
            }

            if (currentStage.DoRaycast)
                CheckRaycast(currentStage);

            if (targetEntity != null && !targetEntity.Closed) // If target is null, just move forward
            {
                Vector3D leadPos = targetEntity.PositionComp.WorldAABB.Center;
                if (currentStage.UseAimPrediction)
                    leadPos = TargetingHelper.InterceptionPoint(projectile.Position, projectile.InheritedVelocity, targetEntity.PositionComp.WorldAABB.Center, targetEntity.Physics.LinearVelocity, projectile.Velocity) ?? leadPos;
                leadPos += randomOffset;

                // Adjust the call to StepDirection to include the maxGs parameter
                StepDirection((leadPos - projectile.Position).Normalized(), currentStage.MaxTurnRate, currentStage.MaxGs, delta);
            }
        }

        internal void NextStage(float delta)
        {
            stages.RemoveFirst();

            if ((stages.First?.Value.Velocity ?? -1) != -1)
                projectile.Velocity = stages.First.Value.Velocity;
            else
                projectile.Velocity = projectile.Definition.PhysicalProjectile.Velocity;

            if (stages.First == null)
                return;

            randomOffset = Vector3D.Zero;
            if (stages.First.Value.Inaccuracy != 0)
            {
                Vector3D.CreateFromAzimuthAndElevation(HeartData.I.Random.NextDouble() * 2 * Math.PI, HeartData.I.Random.NextDouble() * 2 * Math.PI, out randomOffset);
                randomOffset *= stages.First.Value.Inaccuracy * HeartData.I.Random.NextDouble();
            }

            stagePid = stages.First?.Value.PID?.GetPID();

            //projectile.Definition.LiveMethods.OnGuidanceStage?.Invoke(projectile.Id, stages.First?.Value);
            RunGuidance(delta); // Avoid a tick of delay
        }

        /// <summary>
        /// Steps the projectile towards a specified direction, with an optional PID.
        /// </summary>
        /// <param name="targetDir">Normalized target direction.</param>
        /// <param name="maxTurnRate">Maximum turn rate in radians.</param>
        /// <param name="maxGs">Maximum 'pull' of the missile, in Gs.</param>
        /// <param name="delta">Delta time, in seconds.</param>
        internal void StepDirection(Vector3D targetDir, float maxTurnRate, float maxGs, float delta)
        {
            // turnRate and maxGs serve as ABSOLUTE LIMITS (of the absolute value). Set to -1 (or any negative value lol lmao) if you want to disable them.

            double AngleDifference = Vector3D.Angle(projectile.Direction, targetDir);

            Vector3 RotAxis = Vector3.Cross(projectile.Direction, targetDir);
            RotAxis.Normalize();

            double actualTurnRate = maxTurnRate >= 0 ? maxTurnRate : double.MaxValue;

            if (maxGs >= 0)
            {
                double gravityLimited = Definition.PhysicalProjectile.Velocity / (maxGs * 9.81); // I swear to god I did the math for this, it really is that easy.

                actualTurnRate = Math.Min(gravityLimited, actualTurnRate);
            }

            // DELTATICK YOURSELF *RIGHT FUCKING NOW*
            actualTurnRate *= delta;

            // Check if we even have a PID, then set values according to result.
            double finalAngle;
            if (stagePid != null)
            {
                // I always want to have an angle of zero, with an offset of zero.
                finalAngle = HeartUtils.MinAbs(stagePid.Tick(AngleDifference, 0, 0, delta), actualTurnRate);
            }
            else
            {
                finalAngle = HeartUtils.ClampAbs(AngleDifference, actualTurnRate);
            }


            Matrix RotationMatrix = Matrix.CreateFromAxisAngle(RotAxis, (float)finalAngle);
            projectile.Direction = Vector3.Transform(projectile.Direction, RotationMatrix).Normalized();
        }

        internal void CheckRaycast(Guidance currentstage)
        {
            if (targetEntity == null)
            {
                PreformRaycast(currentstage);
                return;
            }
            double angle = Vector3D.Angle(projectile.Direction, targetEntity.WorldAABB.Center);

            if (angle > currentstage.CastCone)
                PreformRaycast(currentstage);
        }

        /// <summary>
        /// Scans for valid targets within the missile's cone
        /// </summary>
        /// <param name="currentstage"></param>
        internal void PreformRaycast(Guidance currentstage)
        {
            MatrixD frustrumMatrix = MatrixD.CreatePerspectiveFieldOfView(currentstage.CastCone, 1, 50, currentstage.CastDistance);
            frustrumMatrix = MatrixD.Invert(MatrixD.CreateWorld(projectile.Position, projectile.Direction, Vector3D.CalculatePerpendicularVector(projectile.Direction))) * frustrumMatrix;
            BoundingFrustumD frustrum = new BoundingFrustumD(frustrumMatrix);
            BoundingSphereD sphere = new BoundingSphereD(projectile.Position, currentstage.CastDistance);

            foreach (var entity in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere))
            {
                if (!IsTargetAllowed(entity, currentstage))
                    continue;

                if (frustrum.Intersects(entity.WorldAABB))
                {
                    //MyAPIGateway.Utilities.ShowNotification("Hit " + entity.DisplayName, 1000 / 60);
                    targetEntity = entity;
                    break;
                }
            }
        }

        internal bool IsTargetAllowed(IMyEntity target, Guidance? currentStage)
        {
            if (currentStage == null || target == null)
                return false;
            if (projectile.Firer == null)
                return true;
            IMyEntity firer = projectile.Firer;
            if (firer == null || !(firer is IMyCubeBlock))
                return true;

            MyRelationsBetweenPlayerAndBlock relations;

            if (target is IMyCubeGrid)
                relations = HeartUtils.GetRelationsBetweeenGrids(((IMyCubeBlock)firer).CubeGrid, (IMyCubeGrid)target);
            else if (target is IMyPlayer)
                relations = HeartUtils.GetRelationsBetweenGridAndPlayer(((IMyCubeBlock)firer).CubeGrid, ((IMyPlayer)target).IdentityId);
            else
                return true;

            if ((relations == MyRelationsBetweenPlayerAndBlock.NoOwnership || relations == MyRelationsBetweenPlayerAndBlock.Neutral) &&
                (currentStage?.IFF & IFF_Enum.TargetNeutrals) == IFF_Enum.TargetNeutrals)
                return true;
            if ((relations == MyRelationsBetweenPlayerAndBlock.Owner) &&
                (currentStage?.IFF & IFF_Enum.TargetSelf) == IFF_Enum.TargetSelf)
                return true;
            if ((relations == MyRelationsBetweenPlayerAndBlock.Friends) &&
                (currentStage?.IFF & IFF_Enum.TargetFriendlies) == IFF_Enum.TargetFriendlies)
                return true;
            if ((relations == MyRelationsBetweenPlayerAndBlock.Enemies) &&
                (currentStage?.IFF & IFF_Enum.TargetEnemies) == IFF_Enum.TargetEnemies)
                return true;

            return false;
        }
    }
}
