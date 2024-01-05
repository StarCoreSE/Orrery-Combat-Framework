﻿using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
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


        private List<DamageEvent> DamageEvents = new List<DamageEvent>();
        private void m_Update()
        {
            foreach (var damageEvent in DamageEvents)
            {
                switch (damageEvent.Type)
                {
                    case DamageEvent.DamageEntType.Grid:
                        m_GridDamageHandler((IMyCubeGrid) damageEvent.Entity, damageEvent);
                        break;
                    case DamageEvent.DamageEntType.Character:
                        m_CharacterDamageHandler((IMyCharacter) damageEvent.Entity, damageEvent);
                        break;
                    case DamageEvent.DamageEntType.Projectile:
                        m_ProjectileDamageHandler((Projectile) damageEvent.Entity, damageEvent);
                        break;
                }
            }
            DamageEvents.Clear();
        }

        private void m_QueueEvent(DamageEvent damageEvent)
        {
            DamageEvents.Add(damageEvent);
        }

        private void m_GridDamageHandler(IMyCubeGrid Entity, DamageEvent DamageEvent)
        {
            //MyDamageType.Bullet
            Vector3I? HitPos = Entity.RayCastBlocks(DamageEvent.Projectile.Position, DamageEvent.Projectile.NextMoveStep);
            if (HitPos != null)
            {
                IMySlimBlock block = Entity.GetCubeBlock(HitPos.Value);
                float damageMult = block.FatBlock == null ? DamageEvent.Projectile.Definition.Damage.SlimBlockDamageMod : DamageEvent.Projectile.Definition.Damage.FatBlockDamageMod;

                block.DoDamage(DamageEvent.Projectile.Definition.Damage.BaseDamage * damageMult, MyDamageType.Bullet, MyAPIGateway.Utilities.IsDedicated);

                if (DamageEvent.Projectile.Definition.Damage.AreaDamage != 0 && DamageEvent.Projectile.Definition.Damage.AreaRadius > 0)
                {
                    BoundingSphereD damageArea = new BoundingSphereD(DamageEvent.Projectile.Position, DamageEvent.Projectile.Definition.Damage.AreaRadius);
                    List<IMySlimBlock> AoEBlocks = Entity.GetBlocksInsideSphere(ref damageArea);

                    foreach (var ablock in AoEBlocks)
                    {
                        damageMult = ablock.FatBlock == null ? DamageEvent.Projectile.Definition.Damage.SlimBlockDamageMod : DamageEvent.Projectile.Definition.Damage.FatBlockDamageMod;
                        ablock.DoDamage(DamageEvent.Projectile.Definition.Damage.AreaDamage * damageMult, MyDamageType.Explosion, MyAPIGateway.Utilities.IsDedicated);
                    }
                }
            }
        }

        private void m_CharacterDamageHandler(IMyCharacter Entity, DamageEvent DamageEvent)
        {
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

        internal DamageEvent(object Entity, DamageEntType type, Projectile projectile)
        {
            this.Entity = Entity;
            Type = type;
            Projectile = projectile;
        }

        public enum DamageEntType
        {
            Grid,
            Character,
            Projectile
        }
    }
}