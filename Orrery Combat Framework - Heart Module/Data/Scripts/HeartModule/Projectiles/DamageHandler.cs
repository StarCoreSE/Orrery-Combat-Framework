﻿using Sandbox.ModAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Heart_Module.Data.Scripts.HeartModule.Projectiles
{
    public class DamageHandler
    {
        private static DamageHandler I;

        public static void Load()
        {
            I = new DamageHandler();
        }

        public static void Unload()
        {
            I = null;
        }

        public static void Update()
        {
            I?.m_Update();
        }

        public static void QueueEvent(DamageEvent damageEvent)
        {
            I?.m_QueueEvent(damageEvent);
        }


        private ConcurrentQueue<DamageEvent> DamageEvents = new ConcurrentQueue<DamageEvent>();
        private void m_Update()
        {
            DamageEvent damageEvent = null;
            while (DamageEvents.TryDequeue(out damageEvent))
            {
                switch (damageEvent.Type)
                {
                    case DamageEvent.DamageEntType.Grid:
                        m_GridDamageHandler((IMyCubeGrid)damageEvent.Entity, damageEvent);
                        break;
                    case DamageEvent.DamageEntType.Character:
                        m_CharacterDamageHandler((IMyCharacter)damageEvent.Entity, damageEvent);
                        break;
                    case DamageEvent.DamageEntType.Projectile:
                        m_ProjectileDamageHandler((Projectile)damageEvent.Entity, damageEvent);
                        break;
                }
            }
        }

        private void m_QueueEvent(DamageEvent damageEvent)
        {
            DamageEvents.Enqueue(damageEvent);
        }

        /// <summary>
        /// Finds the closest impactee.
        /// </summary>
        /// <param name="Entity"></param>
        /// <param name="Projectile"></param>
        /// <returns></returns>
        public static IMySlimBlock GetCollider(IMyCubeGrid Entity, Vector3D HitPos, Vector3D Normal)
        {
            Vector3I? HitBlock = Entity?.RayCastBlocks(HitPos, HitPos + Normal);
            if (HitBlock != null)
            {
                return Entity.GetCubeBlock(HitBlock.Value);
            }

            return null;
        }

        private void m_GridDamageHandler(IMyCubeGrid Entity, DamageEvent DamageEvent)
        {
            IMySlimBlock block = GetCollider(Entity, DamageEvent.HitPosition, DamageEvent.HitNormal);

            if (block != null)
            {
                Entity.Physics?.ApplyImpulse(DamageEvent.Projectile.Direction * DamageEvent.Projectile.Definition.Ungrouped.Impulse, DamageEvent.HitPosition);
                float damageMult = block.FatBlock == null ? DamageEvent.Projectile.Definition.Damage.SlimBlockDamageMod : DamageEvent.Projectile.Definition.Damage.FatBlockDamageMod;

                block.DoDamage(DamageEvent.Projectile.Definition.Damage.BaseDamage * damageMult, MyDamageType.Bullet, MyAPIGateway.Utilities.IsDedicated);

                if (DamageEvent.Projectile.Definition.Damage.AreaDamage != 0 && DamageEvent.Projectile.Definition.Damage.AreaRadius > 0)
                {
                    BoundingSphereD damageArea = new BoundingSphereD(DamageEvent.HitPosition, DamageEvent.Projectile.Definition.Damage.AreaRadius);
                    List<IMySlimBlock> AoEBlocks = Entity.GetBlocksInsideSphere(ref damageArea);

                    foreach (var ablock in AoEBlocks)
                    {
                        float distMult = Vector3.Distance(ablock.Position, block.Position) / DamageEvent.Projectile.Definition.Damage.AreaRadius; // Do less damage at max radius
                        damageMult = ablock.FatBlock == null ? DamageEvent.Projectile.Definition.Damage.SlimBlockDamageMod : DamageEvent.Projectile.Definition.Damage.FatBlockDamageMod;
                        ablock.DoDamage(DamageEvent.Projectile.Definition.Damage.AreaDamage * damageMult * distMult, MyDamageType.Explosion, MyAPIGateway.Utilities.IsDedicated);
                    }
                }
            }
        }

        private void m_CharacterDamageHandler(IMyCharacter Entity, DamageEvent DamageEvent)
        {
            Entity.Physics?.ApplyImpulse(DamageEvent.Projectile.Direction * DamageEvent.Projectile.Definition.Ungrouped.Impulse, DamageEvent.Projectile.Position);
            Entity.DoDamage(DamageEvent.Projectile.Definition.Damage.BaseDamage, MyDamageType.Bullet, MyAPIGateway.Utilities.IsDedicated);
        }

        private void m_ProjectileDamageHandler(Projectile Entity, DamageEvent DamageEvent)
        {

        }
    }

    public class DamageEvent
    {
        internal DamageEntType Type;
        internal float Modifier;
        internal Projectile Projectile;
        internal object Entity;
        internal Vector3D HitPosition;
        internal Vector3D HitNormal;

        internal DamageEvent(object Entity, DamageEntType type, Projectile projectile, Vector3D hitPosition, Vector3D hitNormal)
        {
            this.Entity = Entity;
            Type = type;
            Projectile = projectile;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
        }

        public enum DamageEntType
        {
            Grid,
            Character,
            Projectile
        }
    }
}
