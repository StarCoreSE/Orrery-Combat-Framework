using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Networking;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using Orrery.HeartModule.Shared.Targeting.Generics;
using Sandbox.Game;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Orrery.HeartModule.Client.Projectiles
{
    internal class HitscanProjectile : IHitscanProjectile
    {
        public readonly uint Id;
        public ProjectileDefinitionBase Definition { get; }
        public IMyEntity Owner { get; }
        public LineD Raycast;

        public Vector3D Position
        {
            get
            {
                return Raycast.From;
            }
            set
            {
                Raycast.From = value;
            }
        }

        public Vector3D Direction
        {
            get
            {
                return Raycast.Direction;
            }
            set
            {
                Raycast.Direction = value;
            }
        }

        public bool IsActive { get; set; } = true;
        public MatrixD ProjectileMatrix = MatrixD.Identity;
        public bool HasImpacted = false;

        #region FX variables
        internal MyEntity ProjectileEntity = new MyEntity();
        internal MyParticleEffect ProjectileEffect;
        internal MyEntity3DSoundEmitter ProjectileSound;
        public bool IsVisible = true;
        public bool HasAudio = true;
        /// <summary>
        /// Limits beam length if the beam impacts a block.
        /// </summary>
        internal float MaxBeamLength = 0;
        #endregion

        

        public HitscanProjectile(SerializedSpawnProjectile data)
        {
            Id = data.Id;
            Definition = DefinitionManager.GetProjectileDefinitionFromId(data.DefinitionId);
            if (data.OwnerId != 0)
                Owner = MyAPIGateway.Entities.GetEntityById(data.OwnerId);
            Raycast = new LineD(data.Position, data.Position + data.Direction);
            ProjectileMatrix = MatrixD.CreateWorld(Raycast.From, Raycast.Direction, Vector3D.Cross(Raycast.Direction, Vector3D.Up)); // TODO: Inherit up vector from firer.

            InitEffects();
        }

        public virtual void Update(double deltaTime = 1/60d)
        {
            MaxBeamLength = Definition.PhysicalProjectileDef.MaxTrajectory;

            UpdateAudio();
        }

        public virtual void UpdateSync(SerializedSyncProjectile data)
        {
            Raycast.From = data.Position;
            Raycast.To = data.Position + data.Direction;
        }

        #region FX

        internal virtual void InitEffects()
        {
            float f = (float)HeartData.I.Random.NextDouble(); // I don't care if this isn't synced.
            IsVisible = f <= Definition.VisualDef.VisibleChance;
            HasAudio = f <= Definition.AudioDef.SoundChance;

            if (IsVisible && Definition.VisualDef.HasModel)
            {
                ProjectileEntity.Init(null, Definition.VisualDef.Model, null, null);
                ProjectileEntity.Render.CastShadows = false;
                ProjectileEntity.IsPreview = true;
                ProjectileEntity.Save = false;
                ProjectileEntity.SyncFlag = false;
                ProjectileEntity.NeedsWorldMatrix = false;
                ProjectileEntity.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
                MyEntities.Add(ProjectileEntity, true);
                ProjectileEntity.WorldMatrix = MatrixD.CreateWorld(Raycast.From, Raycast.Direction, Vector3D.Cross(Raycast.Direction, Vector3D.Up));
            }

            if (HasAudio && Definition.AudioDef.HasTravelSound)
            {
                ProjectileSound = new MyEntity3DSoundEmitter(null);
                ProjectileSound.SetPosition(Raycast.From);
                ProjectileSound.CanPlayLoopSounds = true;
                ProjectileSound.VolumeMultiplier = Definition.AudioDef.TravelVolume;
                ProjectileSound.CustomMaxDistance = Definition.AudioDef.TravelMaxDistance;
                ProjectileSound.PlaySound(Definition.AudioDef.TravelSoundPair, true);
            }
        }

        public virtual void UpdateDraw(double deltaTime = 1/60d)
        {
            if (!IsVisible)
                return;

            ProjectileMatrix.Translation = Raycast.From;
            ProjectileMatrix.Forward = Raycast.Direction;
            ProjectileMatrix.Up = Vector3D.Cross(Raycast.Direction, ProjectileMatrix.Right);

            if (MaxBeamLength > 0 && Definition.VisualDef.HasTrail && !HeartData.I.IsPaused)
                GlobalEffects.AddLine(Raycast.From, Raycast.From + Raycast.Direction * MaxBeamLength, Definition.VisualDef.TrailFadeTime, Definition.VisualDef.TrailWidth, Definition.VisualDef.TrailColor, Definition.VisualDef.TrailTexture);

            if (Definition.VisualDef.HasAttachedParticle && !HeartData.I.IsPaused)
            {
                if (ProjectileEffect == null)
                {
                    if (!MyParticlesManager.TryCreateParticleEffect(Definition.VisualDef.AttachedParticle,
                            ref ProjectileMatrix, ref Vector3D.Zero, uint.MaxValue, out ProjectileEffect))
                        throw new Exception($"Failed to create new projectile particle! Effect: {Definition.VisualDef.AttachedParticle}");
                }

                ProjectileEffect.WorldMatrix = ProjectileMatrix;
            }

            ProjectileEntity.WorldMatrix = ProjectileMatrix;

            if (HasAudio && Definition.AudioDef.HasTravelSound)
            {
                ProjectileSound.SetPosition(Raycast.From);
            }
        }

        internal virtual void UpdateAudio()
        {
            if (!HasAudio || !Definition.AudioDef.HasTravelSound) return;

            ProjectileSound.SetPosition(Raycast.From);
        }

        internal virtual void DrawImpactParticle(Vector3D impactPosition, Vector3D impactNormal) // TODO: Does not work in multiplayer
        {
            if (!IsVisible || !Definition.VisualDef.HasImpactParticle)
                return;

            MatrixD matrix = MatrixD.CreateWorld(impactPosition, impactNormal, Vector3D.CalculatePerpendicularVector(impactNormal));
            MyParticleEffect hitEffect;
            if (MyParticlesManager.TryCreateParticleEffect(Definition.VisualDef.ImpactParticle, ref matrix, ref impactPosition, uint.MaxValue, out hitEffect))
            {
                //MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                //hitEffect.Velocity = av.Hit.HitVelocity;

                if (hitEffect.Loop)
                    hitEffect.Stop();
            }
            else
            {
                throw new Exception($"Failed to create new impact particle! RenderId: {uint.MaxValue} Effect: {Definition.VisualDef.ImpactParticle}");
            }
        }

        internal virtual void PlayImpactAudio(Vector3D impactPosition)
        {
            if (!HasAudio || !Definition.AudioDef.HasImpactSound) return;
            MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.AudioDef.ImpactSound, impactPosition);
        }

        #endregion

        public virtual void OnClose()
        {
            ProjectileEffect?.Close();
            ProjectileEntity?.Close();
            ProjectileSound?.StopSound(true);
            ProjectileSound?.Cleanup();

            if (HasImpacted)
            {
                PlayImpactAudio(Raycast.From);
                DrawImpactParticle(Raycast.From, Raycast.Direction);
            }
        }
    }
}
