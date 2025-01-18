using Orrery.HeartModule.Shared.Definitions;
using Orrery.HeartModule.Shared.Logging;
using Sandbox.ModAPI;
using System;
using Orrery.HeartModule.Shared.Weapons;
using Sandbox.Game;
using VRage.ModAPI;
using VRage.Game;
using VRageMath;

namespace Orrery.HeartModule.Client.Weapons
{
    internal class SorterWeaponLogic : SorterWeaponBase
    {
        public MatrixD MuzzleMatrix = MatrixD.Identity;
        public ProjectileDefinitionBase CurrentAmmo => DefinitionManager.ProjectileDefinitions[Definition.Loading.Ammos[Settings.AmmoLoadedIdx]];

        public SorterWeaponLogic(IMyConveyorSorter sorterWep, WeaponDefinitionBase definition, long id) : base(sorterWep, definition, id)
        {
            sorterWep.OnClose += OnClose;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            try
            {
                if (Settings == null)
                {
                    Settings = CreateSettings();
                    Settings.RequestSync();
                }
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }

            try
            {
                Definition.LiveMethods.ClientOnPlace?.Invoke(SorterWep);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(SorterWeaponLogic));
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (MarkedForClose || SorterWep == null)
                    return;

                MuzzleMatrix = CalcMuzzleMatrix(_muzzleIdx);

                // yeah this is a bit stupid but whatever I don't care. if it's an issue will fix.
                if (!HasInventory)
                    SorterWep.ShowInInventory = false;
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(SorterWeaponLogic));
            }
        }

        public void OnClose(IMyEntity ent)
        {
            WeaponManager.RemoveWeapon(Id);
            SorterWep.OnClose -= OnClose;
        }

        public void PlayPreShootSound()
        {
            if (!string.IsNullOrEmpty(Definition.Audio.ShootSound))
                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.PreShootSound, SorterWep.GetPosition());
        }

        private int _muzzleIdx = 0;
        public void OnShoot()
        {
            if (!string.IsNullOrEmpty(Definition.Audio.ShootSound))
                MyVisualScriptLogicProvider.PlaySingleSoundAtPosition(Definition.Audio.ShootSound, SorterWep.GetPosition());

            if (Definition.Visuals.HasShootParticle)
            {
                Vector3D muzzlePos = MuzzleMatrix.Translation;

                MyParticleEffect hitEffect;
                if (MyParticlesManager.TryCreateParticleEffect(Definition.Visuals.ShootParticle, ref MuzzleMatrix, ref muzzlePos, uint.MaxValue, out hitEffect))
                {
                    //MyAPIGateway.Utilities.ShowNotification("Spawned particle at " + hitEffect.WorldMatrix.Translation);
                    //hitEffect.Velocity = SorterWep.CubeGrid.LinearVelocity;

                    if (hitEffect.Loop)
                        hitEffect.Stop();
                }
                else
                {
                    throw new Exception($"Failed to create new muzzle flash particle! RenderId: {SorterWep.Render.GetRenderObjectID()} Effect: {Definition.Visuals.ShootParticle}");
                }
            }

            _muzzleIdx++;
            if (_muzzleIdx >= Definition.Assignments.Muzzles.Length)
                _muzzleIdx = 0;
            MuzzleMatrix = CalcMuzzleMatrix(_muzzleIdx);

            try
            {
                Definition.LiveMethods.ClientOnShoot?.Invoke(SorterWep);
            }
            catch (Exception ex)
            {
                HeartLog.Exception(ex, typeof(SorterWeaponLogic));
            }
        }
    }
}
