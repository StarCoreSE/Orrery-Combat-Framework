using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace Orrery.HeartModule.Client.Projectiles
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class GlobalEffects : MySessionComponentBase
    {
        const float TickRate = 1 / 60f;
        internal static GlobalEffects I;
        List<LineFade> TrailFade = new List<LineFade>(); // Maybe try a Stack var?

        public static void AddLine(Vector3D start, Vector3D end, float fadeTime, float width, Vector4 color, MyStringId texture)
        {
            I?.TrailFade.Add(new LineFade(start, end, fadeTime, width, color, texture));
        }

        public override void LoadData()
        {
            I = this;
        }

        protected override void UnloadData()
        {
            I = null;
        }

        public override void UpdateAfterSimulation()
        {

        }

        public override void Draw()
        {
            UpdateTrailFade(TickRate);
        }

        /// <summary>
        /// Updates trail fade.
        /// </summary>
        private void UpdateTrailFade(float delta)
        {
            foreach (var lineFade in TrailFade.ToList())
            {
                // TODO: Make persistent billboard system. DrawLine is very expensive.
                BoundingSphereD sphere = new BoundingSphereD(lineFade.Start, lineFade.Length);
                if (MyAPIGateway.Session.Camera?.IsInFrustum(ref sphere) ?? false) // Check if line is visible
                {
                    float lifetimePct = lineFade.RemainingTime / lineFade.FadeTime;
                    Vector4 fadedColor = lineFade.Color * (lineFade.FadeTime == 0 ? 1 : lifetimePct);
                    MySimpleObjectDraw.DrawLine(lineFade.Start, lineFade.End, lineFade.Texture, ref fadedColor, lineFade.Width);
                }

                if (!HeartData.I.IsPaused)
                    lineFade.RemainingTime -= delta;
                if (lineFade.RemainingTime <= 0)
                    TrailFade.Remove(lineFade); // surely this will not cause a problem :)
            }
        }

        internal class LineFade
        {
            public Vector3D Start;
            public Vector3D End;
            public float RemainingTime;
            public float FadeTime;
            public double Length;
            public Vector4 Color;
            public float Width;
            public MyStringId Texture;

            public LineFade(Vector3D start, Vector3D end, float fadeTime, float width, Vector4 color, MyStringId texture)
            {
                Start = start;
                End = end;
                FadeTime = fadeTime;
                RemainingTime = fadeTime;
                Length = Vector3D.Distance(Start, End);
                Color = color;
                Width = width;
                Texture = texture;
            }
        }
    }
}
