using Heart_Module.Data.Scripts.HeartModule.ErrorHandler;
using Heart_Module.Data.Scripts.HeartModule.ExceptionHandler;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.ProjectileNetworking;
using Heart_Module.Data.Scripts.HeartModule.Projectiles.StandardClasses;
using Heart_Module.Data.Scripts.HeartModule.Weapons;
using Sandbox.Game.GUI.DebugInputComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public partial class ProjectileManager : MySessionComponentBase
    {
        public static ProjectileManager I = new ProjectileManager();
        public ProjectileNetwork Network = new ProjectileNetwork();

        internal Dictionary<uint, Projectile> ActiveProjectiles = new Dictionary<uint, Projectile>();
        private HashSet<Projectile> ProjectilesWithHealth = new HashSet<Projectile>();
        public uint NextId { get; private set; } = 0;
        internal List<Projectile> QueuedCloseProjectiles = new List<Projectile>();
        /// <summary>
        /// Delta for engine ticks; 60tps
        /// </summary>
        public const float DeltaTick = 1 / 60f;
        /// <summary>
        /// Delta for frames; varies
        /// </summary>
        private Stopwatch clockTick = Stopwatch.StartNew();

        ParallelProjectileThread ProjectileThread;

        public int NumProjectiles => ActiveProjectiles.Count;

        public override void LoadData()
        {
            I = this;
            DamageHandler.Load();
            ProjectileThread = new ParallelProjectileThread();
        }

        protected override void UnloadData()
        {
            I = null;
            DamageHandler.Unload();
            ProjectileThread.Close();
        }

        public override void UpdateAfterSimulation()
        {
            if (HeartData.I.IsSuspended) return;

            try
            {
                ProjectileThread.Update();

                foreach (var projectile in QueuedCloseProjectiles)
                {
                    if (projectile == null)
                        continue;
                    ActiveProjectiles.Remove(projectile.Id);
                    ProjectilesWithHealth.Remove(projectile);
                    projectile.OnClose.Invoke(projectile);
                }
                QueuedCloseProjectiles.Clear();

                // Sync stuff
                Network.Update1();

                DamageHandler.Update();

                clockTick.Restart();
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException(ex, typeof(ProjectileManager));
            }
        }

        public override void UpdatingStopped()
        {
            clockTick.Stop();
        }

        public override void Draw() // Called once per frame to avoid jitter
        {
            if (HeartData.I.IsSuspended || MyAPIGateway.Utilities.IsDedicated) // We don't want to needlessly use server CPU time
                return;

            foreach (var projectile in ActiveProjectiles.Values)
                projectile.DrawUpdate(); // Draw delta is always 1/60 because Keen:tm:
        }

        [Obsolete]
        public void UpdateProjectileSync(n_SerializableProjectile projectile)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            if (IsIdAvailable(projectile.Id) && projectile.DefinitionId.HasValue)
            {
                if (projectile.Firer != null)
                {
                    WeaponManager.I.GetWeapon(projectile.Firer.Value)?.MuzzleFlash(true);
                }
                AddProjectile(new Projectile(projectile));
            }
            else
            {
                Projectile p = GetProjectile(projectile.Id);
                if (p != null)
                    p.UpdateFromSerializable(projectile);
                else if (projectile.IsActive ?? false)
                    HeartData.I.Net.SendToServer(new n_ProjectileRequest(projectile.Id));
            }
        }

        public Projectile AddProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, IMyConveyorSorter sorterWep, bool shouldSync = true)
        {
            try
            {
                return AddProjectile(new Projectile(projectileDefinitionId, position, direction, sorterWep), shouldSync);
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException($"Invalid ammo definition ({projectileDefinitionId} of {ProjectileDefinitionManager.DefinitionCount()})", ex, typeof(ProjectileManager));
                return null;
            }
        }

        public Projectile AddProjectile(int projectileDefinitionId, Vector3D position, Vector3D direction, long firer, Vector3D initialVelocity, bool shouldSync = true)
        {
            try
            {
                return AddProjectile(new Projectile(projectileDefinitionId, position, direction, (MyEntity) MyAPIGateway.Entities.GetEntityById(firer), initialVelocity), shouldSync);
            }
            catch (Exception ex)
            {
                SoftHandle.RaiseException($"Invalid ammo definition ({projectileDefinitionId} of {ProjectileDefinitionManager.DefinitionCount()})", ex, typeof(ProjectileManager));
                return null;
            }
        }

        internal Projectile AddProjectile(Projectile projectile, bool shouldSync = true)
        {
            if (projectile == null || projectile.DefinitionId == -1) return null; // Ensure that invalid projectiles don't get added

            projectile.Position -= projectile.InheritedVelocity / 60f; // Because this doesn't run during simulation

            NextId++;
            while (!IsIdAvailable(NextId))
                NextId++;
            projectile.SetId(NextId);
            //HeartLog.Log("SpawnProjectile " + projectile.Id + $" | [{projectile.Definition.Name}] " + projectile.Definition.Networking.NetworkingMode);
            ActiveProjectiles.Add(projectile.Id, projectile);
            if (MyAPIGateway.Session.IsServer && shouldSync)
            {
                switch (projectile.Definition.Networking.NetworkingMode)
                {
                    case Networking.NetworkingModeEnum.FullSync:
                        Network.QueueSync_PP(projectile, 0);
                        break;
                    case Networking.NetworkingModeEnum.FireEvent:
                        Network.QueueSync_FireEvent(projectile);
                        break;
                }
            }
            if (!MyAPIGateway.Utilities.IsDedicated)
                projectile.InitEffects();
            if (projectile.Definition.PhysicalProjectile.Health > 0 && projectile.Definition.PhysicalProjectile.ProjectileSize > 0)
                ProjectilesWithHealth.Add(projectile);

            return projectile;
        }

        public Projectile GetProjectile(uint id) => ActiveProjectiles.GetValueOrDefault(id, null);
        public bool IsIdAvailable(uint id) => !ActiveProjectiles.ContainsKey(id);

        /// <summary>
        /// Populates a list with all projectiles in a sphere.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="projectiles"></param>
        /// <param name="onlyDamageable"></param>
        public void GetProjectilesInSphere(BoundingSphereD sphere, ref List<Projectile> projectiles, bool onlyDamageable = false)
        {
            projectiles.Clear();
            double rangeSq = sphere.Radius * sphere.Radius;
            Vector3D pos = sphere.Center;

            if (onlyDamageable)
            {
                foreach (var projectile in ProjectilesWithHealth)
                    if (Vector3D.DistanceSquared(pos, projectile.Position) < rangeSq)
                        projectiles.Add(projectile);
            }
            else
            {
                foreach (var projectile in ActiveProjectiles.Values)
                    if (Vector3D.DistanceSquared(pos, projectile.Position) < rangeSq)
                        projectiles.Add(projectile);
            }
        }
    }
}
