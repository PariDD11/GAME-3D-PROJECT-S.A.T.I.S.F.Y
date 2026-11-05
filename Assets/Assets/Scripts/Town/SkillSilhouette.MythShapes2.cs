using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Seni makhluk mitologi (bagian 2): burung, manusia-hewan, ular/hydra/ouroboros, kura-kura, kraken, naga, wyvern, tetramorph, maw.</summary>
    public static partial class SkillSilhouette
    {
        // ------------------------------------------------------------------ burung
        static void MyBirdOne(List<Vector2[]> l, MySpec sp, float flap, float roar, Vector2 off, float scl, bool main)
        {
            var t = new List<Vector2[]>();
            int f0 = myFills.Count;
            string p = sp.prof;
            float span = 9.5f, rx = 2.6f, ry = 1.05f;
            switch (p)
            {
                case "roc": span = 12f; rx = 3.0f; ry = 1.25f; break;
                case "ziz": span = 13.5f; rx = 3.2f; ry = 1.35f; break;
                case "raven": span = 7.2f; rx = 2.2f; ry = 0.95f; break;
                case "heron": span = 7.6f; rx = 2.2f; ry = 0.8f; break;
                case "thunder": span = 10.5f; rx = 2.7f; ry = 1.15f; break;
                case "simurgh": span = 9.5f; rx = 2.7f; ry = 1.05f; break;
                case "anzu": span = 9.2f; rx = 2.8f; ry = 1.15f; break;
                case "crow3": span = 7.6f; rx = 2.2f; ry = 1.0f; break;
                case "flock": span = 6.2f; rx = 2.0f; ry = 0.85f; break;
            }
            float fl = flap * 24f;
            // sayap belakang (lebih redup) lalu badan lalu sayap depan
            myShade = 0.6f; MyWingF(t, V(0.2f, 0.5f), span * 0.9f, fl * 0.8f - 8f, 0.3f, false); myShade = 1f;
            // badan
            MyBlob(t, new[] { V(-rx * 1.05f, 0.1f), V(-rx * 0.5f, ry * 0.95f), V(rx * 0.3f, ry * 1.05f), V(rx * 0.95f, ry * 0.6f), V(rx * 1.0f, -ry * 0.1f), V(rx * 0.5f, -ry * 0.95f), V(-rx * 0.4f, -ry * 0.9f) }, true, 4);
            // dada berbulu
            for (int i = 0; i < 5; i++) { float x = -rx * 0.2f + i * 0.45f; MyLeaf(t, V(x, ry * 0.6f), V(x + 0.15f, -ry * 0.85f), 0.45f, 0.1f, false); }
            // leher dan kepala
            Vector2 hc = V(rx + 0.3f, 0.75f);
            if (p == "heron")
            {
                var neck = new[] { V(rx * 0.8f, 0.2f), V(rx + 0.9f, 1.4f), V(rx + 0.5f, 2.9f), V(rx + 1.4f, 3.7f) };
                MyRibbon(t, neck, 0.7f, 0.35f, 0.1f, true, 4);
                hc = neck[3];
                MyLeaf(t, hc + V(0.1f, 0f), hc + V(2.9f, -0.4f - roar * 0.3f), 0.38f, 0f, true);
                MyLeaf(t, hc + V(0.1f, -0.1f), hc + V(2.6f, -0.5f - roar * 0.5f), 0.26f, 0f, false);
                MyDisc(t, hc + V(0.4f, 0.2f), 0.12f);
                myHead = hc + V(2.8f, -0.4f);
            }
            else if (p == "anzu")
            {
                MyRibbon(t, new[] { V(rx * 0.7f, 0.3f), V(rx + 0.4f, 0.8f), V(rx + 0.8f, 1.3f) }, 1.0f, 0.8f, 0f, true, 3);
                MyHead(t, V(rx + 0.6f, 1.1f), "feline", 1.6f, roar, 0.0f, true, 0.4f);
                Vector2 c = V(rx + 1.1f, 1.7f);
                for (int i = 0; i < 12; i++) { float a = Mathf.Lerp(-110f, 110f, i / 11f) * Mathf.Deg2Rad; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); MyLeaf(t, c + d * 1.0f, c + d * (2.1f + (i % 2) * 0.4f), 0.5f, 0.1f, i % 3 == 0); }
            }
            else
            {
                float bl = p == "raven" ? 1.6f : 2.0f;
                MyRibbon(t, new[] { V(rx * 0.7f, 0.3f), V(rx + 0.4f, 0.65f), hc }, 1.0f, 0.75f, 0f, true, 3);
                MyHead(t, hc + V(-0.4f, -0.1f), "eagle", 1.3f, roar, -0.05f, false, 0f);
                myHead = hc + V(bl * 0.7f, -0.1f);
            }
            if (sp.Has("crest") || p == "heron" || p == "simurgh")
                for (int i = 0; i < 4; i++) MyLeaf(t, hc + V(-0.1f - i * 0.12f, 0.55f), hc + V(-0.9f - i * 0.5f, 1.7f + i * 0.35f + Mathf.Sin(flap * 3f + i) * 0.1f), 0.28f, 0.2f, false);
            if (sp.Has("crown")) MyBlob(t, new[] { hc + V(-0.5f, 0.7f), hc + V(-0.5f, 1.7f), hc + V(-0.1f, 1.1f), hc + V(0.25f, 1.9f), hc + V(0.6f, 1.1f), hc + V(1.0f, 1.6f), hc + V(0.9f, 0.7f) }, true, 2);
            // sayap depan (terang)
            MyWingF(t, V(0.5f, 0.7f), span, fl, 0.1f, true);
            if (p == "garuda" || sp.Has("sun"))
            {
                Vector2 c = V(-0.8f, 2.4f); MyDisc(t, c, 1.7f);
                for (int i = 0; i < 14; i++) { float a = i * Mathf.PI * 2f / 14f + flap * 0.1f; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); MyLeaf(t, c + d * 1.95f, c + d * (3.0f + (i % 2) * 0.55f), 0.38f, 0f, false); }
            }
            // ekor
            if (p == "simurgh" || sp.Has("plumes"))
                for (int j = 0; j < 6; j++)
                {
                    var pl = new Vector2[5];
                    for (int k = 0; k < 5; k++) { float u = k / 4f; pl[k] = V(-rx * 0.9f - u * (7.5f + j * 0.5f), -0.1f - u * (0.5f + j * 0.35f) + Mathf.Sin(u * 5f + flap * 2f + j) * 0.55f * u); }
                    MyRibbon(t, pl, 0.4f, 0.05f, 0.3f, true, 3);
                    MyDisc(t, pl[4], 0.36f, true);
                }
            else if (p == "raven" || p == "flock")
                for (int j = -1; j <= 1; j++) MyLeaf(t, V(-rx * 0.9f, 0f), V(-rx - 3.6f, j * 0.9f - 0.1f), 0.8f, 0.05f, true);
            else
                for (int j = -2; j <= 2; j++) MyLeaf(t, V(-rx * 0.9f, 0f), V(-rx - 4.2f, j * 1.0f - 0.2f), 0.85f, 0.04f, j % 2 == 0);
            // kaki dan cakar
            int legs = p == "crow3" ? 3 : 2;
            for (int i = 0; i < legs; i++)
            {
                float x = 0.9f - i * 0.55f, ln = p == "heron" ? 4.0f : 1.7f;
                MyRibbon(t, new[] { V(x, -ry + 0.1f), V(x + 0.1f, -ry - ln * 0.55f), V(x + 0.5f, -ry - ln) }, 0.4f, 0.2f, 0.1f, true, 3);
                for (int k = 0; k < 3; k++) MyLeaf(t, V(x + 0.5f, -ry - ln), V(x + 0.9f + k * 0.25f, -ry - ln - 0.25f - k * 0.2f), 0.14f, 0.2f, false);
            }
            if (sp.Has("bolt"))
            {
                for (int i = 0; i < 5; i++) MyBolt(t, V(-0.5f - i * 0.3f, 0.6f), V(-1.5f - i * 1.4f, 1f + span * 0.8f * (1f - i * 0.12f)), 0.6f, i + (int)(flap * 4f + 8f));
                MyBolt(t, hc, hc + V(2.4f, 2.8f), 0.5f, (int)(flap * 4f + 20f));
            }
            if (sp.Has("fire")) for (int i = 0; i < 7; i++) MyFlame(t, V(-rx + i * 0.8f, ry * 0.6f), 1.6f, flap * 4f, i);
            foreach (var pl in t)
            {
                var q = new Vector2[pl.Length];
                for (int i = 0; i < pl.Length; i++) q[i] = off + pl[i] * scl;
                l.Add(q);
            }
            for (int i = f0; i < myFills.Count; i++)
            {
                var fp = myFills[i]; var q = new Vector2[fp.p.Length];
                for (int k = 0; k < q.Length; k++) q[k] = off + fp.p[k] * scl;
                myFills[i] = new MyFill { p = q, strip = fp.strip, kind = fp.kind, ink = fp.ink, sh = fp.sh };
            }
            if (main) { myHead = off + myHead * scl; myTop = off + V(0f, span * 0.9f) * scl; }
        }

        static List<Vector2[]> MyBird(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            float flap = Mathf.Sin(ph);
            if (sp.prof == "flock")
            {
                MyBirdOne(l, sp, flap, roar, V(0f, 0f), 0.85f, true);
                Vector2 h0 = myHead, t0 = myTop;
                MyBirdOne(l, sp, Mathf.Sin(ph + 1.3f), 0f, V(-6f, 4f), 0.5f, false);
                MyBirdOne(l, sp, Mathf.Sin(ph + 2.4f), 0f, V(-7f, -3.4f), 0.45f, false);
                myHead = h0; myTop = t0;
            }
            else MyBirdOne(l, sp, flap, roar, V(0f, 0f), 1f, true);
            return l;
        }

        // ------------------------------------------------------------------ manusia-hewan (Minotaur, Anubis, Tengu)
        static List<Vector2[]> MyHuman(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            string p = sp.prof;
            float k = p == "minotaur" ? 1.28f : 1f;
            float legH = 3.4f, sh = legH + 3.6f;
            float bob = Mathf.Sin(ph * 0.5f) * 0.25f;
            // kaki
            myShade = 0.55f; MyLeg(l, V(-0.4f, legH), legH, ph + Mathf.PI, 1.4f, legH * 0.25f, 0.95f * k, 1f, false, 1.7f); myShade = 1f;
            MyLeg(l, V(0.4f, legH), legH, ph, 1.4f, legH * 0.25f, 1.0f * k, 1f, false, 1.7f);
            // lengan belakang
            myShade = 0.6f; MyRibbon(l, new[] { V(-1.0f * k, sh - 0.2f), V(-1.8f * k, sh - 1.9f), V(-1.4f * k, sh - 3.5f + bob) }, 0.95f * k, 0.55f * k, 0.15f, true, 4); myShade = 1f;
            // cawat
            MyBlob(l, new[] { V(-0.95f * k, legH + 0.4f), V(0.95f * k, legH + 0.4f), V(1.1f * k, legH - 1.4f), V(0.3f, legH - 2.2f), V(-0.6f, legH - 1.8f), V(-1.1f * k, legH - 1.1f) }, true, 3);
            // badan
            MyBlob(l, new[] { V(-0.8f * k, legH + 0.2f), V(-1.2f * k, legH + 1.7f), V(-1.7f * k, sh - 0.5f), V(-1.0f * k, sh + 0.4f), V(0.1f, sh + 0.5f), V(1.1f * k, sh + 0.35f), V(1.7f * k, sh - 0.5f), V(1.25f * k, legH + 1.6f), V(0.8f * k, legH + 0.2f), V(0f, legH - 0.1f) }, true, 4);
            l.Add(new[] { V(0.25f, sh - 0.4f), V(0.35f, legH + 0.7f) });
            l.Add(MyArc(V(0.5f * k, sh - 0.6f), 0.7f * k, 190f, 330f, 5)); l.Add(MyArc(V(-0.5f * k, sh - 0.6f), 0.7f * k, 215f, 355f, 5));
            Vector2 hc = V(0.55f, sh + 1.3f);
            Vector2 weapon = V(2f, sh);
            Vector2 hand = V(2.7f * k, sh - 0.6f + bob);
            MyRibbon(l, new[] { V(1.0f * k, sh - 0.2f), V(2.3f * k, sh - 1.4f), hand }, 1.0f * k, 0.6f * k, 0.15f, true, 4);
            MyRibbon(l, new[] { V(0.1f, sh + 0.2f), V(0.3f, sh + 0.7f), hc + V(-0.35f, -0.5f) }, 1.3f * k, 1.0f * k, 0f, true, 3);
            if (p == "minotaur")
            {
                MyHead(l, hc + V(-0.55f, -0.4f), "bovine", 1.5f, roar, -0.12f, false, 0f);
                Vector2 hb = hc + V(0.0f, 0.3f);
                for (int s = 0; s < 2; s++) { Vector2 b0 = hb + V(-0.35f * s, 0f); MyRibbon(l, new[] { b0, b0 + V(-0.15f, 0.7f), b0 + V(0.8f + s * 0.1f, 1.6f), b0 + V(1.5f, 1.9f) }, 0.5f, 0.04f, 0f, true, 4); }
                l.Add(MyCircle(hc + V(1.2f, -0.95f), 0.28f, 0.38f, 8));
                MyRibbon(l, new[] { hand + V(-0.5f, -2.4f), hand, hand + V(0.5f, 5.4f) }, 0.34f, 0.3f, 0f, true, 3);
                Vector2 T = hand + V(0.5f, 4.3f);
                MyBlob(l, new[] { T + V(0.2f, -2.3f), T + V(1.7f, -3.0f), T + V(3.2f, -1.8f), T + V(3.3f, 0.5f), T + V(1.8f, 1.5f), T + V(0.2f, 0.9f) }, true, 4);
                MyBlob(l, new[] { T + V(-0.2f, -2.0f), T + V(-1.4f, -2.5f), T + V(-2.4f, -1.0f), T + V(-2.2f, 0.7f), T + V(-1.0f, 1.0f), T + V(-0.2f, 0.7f) }, true, 4);
                l.Add(MyArc(T + V(0.4f, -0.6f), 2.2f, -50f, 40f, 6));
                MyLeaf(l, T + V(0.1f, 1.2f), T + V(0.2f, 2.4f), 0.3f, 0f, true);
                weapon = T + V(2.0f, -0.6f);
                MyBlob(l, new[] { V(-2.0f * k, legH + 0.6f), V(-2.5f, sh + 1.0f), V(-3.5f, sh + 1.4f), V(-4.8f, sh + 0.2f), V(-4.3f, legH + 0.8f) }, true, 3);
                myHead = hc + V(1.1f, -0.5f);
            }
            else if (p == "anubis")
            {
                MyHead(l, hc + V(-0.6f, -0.5f), "canine", 1.6f, roar, 0.05f, true, 1.6f);
                MyBlob(l, new[] { hc + V(-0.9f, 0.7f), hc + V(-1.9f, 0.0f), hc + V(-2.1f, -1.9f), hc + V(-1.0f, -1.3f), hc + V(-0.6f, -0.2f) }, true, 3);
                MyBlob(l, new[] { V(-1.6f, sh - 0.2f), V(-1.0f, sh + 0.5f), V(0.2f, sh + 0.7f), V(1.4f, sh + 0.5f), V(2.0f, sh - 0.2f), V(1.2f, sh - 1.2f), V(0.2f, sh - 1.5f), V(-1.0f, sh - 1.2f) }, true, 3);
                Vector2 top = hand + V(0.2f, 5.4f);
                MyRibbon(l, new[] { hand + V(-0.1f, -2.4f), hand, top }, 0.28f, 0.22f, 0f, true, 3);
                MyBlob(l, new[] { top + V(-0.5f, 0.1f), top + V(-0.3f, 1.2f), top + V(0f, 1.6f), top + V(0.3f, 1.2f), top + V(0.5f, 0.1f) }, true, 3);
                MyDisc(l, top + V(0f, 0.5f), 0.42f);
                MyRibbon(l, new[] { top + V(0.3f, -0.4f), top + V(-1.2f, -0.2f), top + V(-2.6f, -0.9f) }, 0.28f, 0.1f, 0f, true, 3);
                MyBlob(l, new[] { top + V(-2.4f, -0.9f), top + V(-3.3f, -2.3f), top + V(-1.9f, -2.2f) }, true, 3);
                MyRibbon(l, new[] { top + V(0.3f, -0.4f), top + V(1.3f, -0.3f), top + V(2.5f, -0.9f) }, 0.28f, 0.1f, 0f, true, 3);
                MyBlob(l, new[] { top + V(2.3f, -0.9f), top + V(3.1f, -2.3f), top + V(1.7f, -2.2f) }, true, 3);
                MyRibbon(l, new[] { V(-1.2f, sh - 0.5f), V(-2.4f, sh - 2.4f), V(-2.7f, legH * 0.35f) }, 1.2f, 0.2f, 0.1f, true, 4);
                weapon = top;
                myHead = hc + V(1.7f, -0.55f);
            }
            else // tengu
            {
                MyBlob(l, new[] { hc + V(-0.8f, 0.0f), hc + V(-0.4f, 0.9f), hc + V(0.5f, 1.05f), hc + V(1.0f, 0.3f), hc + V(0.8f, -0.7f), hc + V(-0.2f, -0.95f) }, true, 4);
                MyLeaf(l, hc + V(0.8f, 0.1f), hc + V(3.1f, -0.2f - roar * 0.2f), 0.55f, 0.04f, true);
                MyBlob(l, new[] { hc + V(-0.7f, 0.8f), hc + V(-0.1f, 1.9f), hc + V(0.6f, 1.9f), hc + V(0.9f, 1.0f), hc + V(0.1f, 0.9f) }, true, 3);
                MyDisc(l, hc + V(0.45f, 0.3f), 0.1f, false);
                MyWingF(l, V(-wid(k) * 0.5f, sh - 0.3f), 9.0f, Mathf.Sin(ph * 0.6f) * 16f, 0.5f);
                MyWingF(l, V(-wid(k) * 0.3f, sh - 0.5f), 7.0f, Mathf.Sin(ph * 0.6f + 0.8f) * 16f - 8f, 0.6f, false);
                Vector2 fc = hand + V(0f, 1.6f);
                for (int i = 0; i < 9; i++) { float a = Mathf.Lerp(-15f, 195f, i / 8f) * Mathf.Deg2Rad; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); MyLeaf(l, fc, fc + d * 2.6f, 0.95f, 0.04f, i % 2 == 0); }
                weapon = hand + V(0f, 4.2f);
                myHead = hc + V(3.0f, -0.2f);
            }
            myTop = V(0f, sh + 3f);
            if (sp.Has("flame")) for (int i = 0; i < 5; i++) MyFlame(l, V(-1.4f + i * 0.7f, sh + 0.2f), 1.7f, ph, i);
            if (sp.Has("bolt")) MyBolt(l, weapon, weapon + V(2f, 3f), 0.5f, (int)(ph * 3f));
            return l;
        }

        static float wid(float k) { return 1.35f * k; }

        // ------------------------------------------------------------------ kepala ular generik
        static void MySnakeHead(List<Vector2[]> l, Vector2 o, float sc, float tilt, float roar, bool crest)
        {
            var U = new[] { V(-0.5f, 0.35f), V(0.1f, 0.65f), V(0.9f, 0.5f), V(1.6f, 0.2f), V(1.75f, 0.0f), V(1.5f, -0.1f), V(0.6f, -0.12f), V(-0.4f, -0.1f) };
            var J = new[] { V(0.0f, -0.1f), V(1.4f, -0.12f), V(1.6f, -0.2f), V(1.2f, -0.4f), V(0.3f, -0.5f), V(-0.4f, -0.3f) };
            var jaw = new Vector2[J.Length];
            for (int i = 0; i < J.Length; i++) jaw[i] = MyRot(J[i], V(0f, -0.1f), -roar * 0.6f);
            MyBlob(l, MyXf(U, o, tilt, sc), true, 4);
            MyBlob(l, MyXf(jaw, o, tilt, sc), true, 4);
            MyDisc(l, o + MyRot(V(0.75f, 0.32f) * sc, Vector2.zero, tilt), 0.08f * sc, false);
            for (int i = 0; i < 2; i++) { Vector2 a = o + MyRot(V(1.35f + i * 0.2f, -0.1f) * sc, Vector2.zero, tilt); MyLeaf(l, a, a + MyRot(V(0.03f, -0.4f - roar * 0.3f) * sc, Vector2.zero, tilt), 0.1f * sc, 0f, false); }
            if (crest) for (int i = 0; i < 4; i++) { Vector2 a = o + MyRot(V(-0.2f - i * 0.12f, 0.5f) * sc, Vector2.zero, tilt); MyLeaf(l, a, a + MyRot(V(-0.8f, 0.4f + i * 0.2f) * sc, Vector2.zero, tilt), 0.25f * sc, 0.2f, false); }
            myHead = o + MyRot(V(1.7f, 0f) * sc, Vector2.zero, tilt);
        }

        static List<Vector2[]> MyHydra(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            // kaki pendek dan tubuh kolam-rawa
            for (int i = 0; i < 4; i++) MyLeg(l, V(-2.6f + i * 1.7f, 2.2f), 2.2f, ph + i * 1.6f, 0.9f, 0.35f, 0.8f, i % 2 == 0 ? 1f : -1f, false, 1.1f);
            MyRibbon(l, new[] { V(-3.8f, 2.8f), V(-6.0f, 2.1f + Mathf.Sin(ph) * 0.3f), V(-8.2f, 1.0f), V(-10.0f, 0.4f) }, 2.4f, 0.1f, 0.2f, true, 4);
            MyBlob(l, new[] { V(-4.4f, 2.2f), V(-3.6f, 3.8f), V(-1.2f, 4.6f), V(2.0f, 4.4f), V(4.2f, 3.2f), V(4.0f, 1.6f), V(1.0f, 1.2f), V(-2.4f, 1.3f) }, true, 4);
            for (int i = 0; i < 6; i++) l.Add(MyArc(V(-3.0f + i * 1.3f, 3.8f), 0.55f, 200f, 340f, 4));
            int n = 5; Vector2 best = V(0f, 0f); float bestH = -1f;
            for (int k = 0; k < n; k++)
            {
                float dirA = Mathf.Lerp(150f, 25f, k / (float)(n - 1)) * Mathf.Deg2Rad, len = 8.5f + (k == 2 ? 3.5f : 0f) - Mathf.Abs(k - 2) * 0.6f;
                Vector2 b0 = V(Mathf.Lerp(-2.0f, 2.6f, k / 4f), 3.9f);
                var neck = new Vector2[7];
                for (int i = 0; i < 7; i++)
                {
                    float u = i / 6f, sway = Mathf.Sin(u * 4.5f + ph * 0.8f + k * 1.3f) * 1.2f * u;
                    neck[i] = b0 + V(Mathf.Cos(dirA) * len * u - Mathf.Sin(dirA) * sway, Mathf.Sin(dirA) * len * u + Mathf.Cos(dirA) * sway * 0.3f + u * u * 1.3f);
                }
                MyRibbon(l, neck, 1.5f, 0.85f, 0.05f, true, 5);
                for (int i = 1; i < 6; i++) { Vector2 c = MyCR(neck[Mathf.Max(0, i - 1)], neck[i], neck[i + 1], neck[Mathf.Min(6, i + 2)], 0.0f); l.Add(MyArc(c, 0.45f, 210f, 330f, 3)); }
                Vector2 tip = neck[6], pre = neck[5]; float tilt = Mathf.Atan2(tip.y - pre.y, tip.x - pre.x) * 0.8f;
                MySnakeHead(l, tip, 1.5f, tilt, roar * (k == 2 ? 1f : 0.5f) + Mathf.Sin(ph + k) * 0.15f, k == 2);
                if (tip.y > bestH) { bestH = tip.y; best = myHead; }
            }
            myHead = best; myTop = V(0f, 14f);
            return l;
        }

        static List<Vector2[]> MyOuroboros(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            Vector2 c = V(0f, 6.5f); float R = 5.6f;
            const int n = 28;
            var cen = new Vector2[n];
            for (int i = 0; i < n; i++) { float u = i / (float)(n - 1), a = 0.55f + u * 5.35f; float r = R + Mathf.Sin(u * 14f - ph) * 0.18f; cen[i] = c + V(Mathf.Cos(a) * r, Mathf.Sin(a) * r); }
            // badan: lebar membesar ke arah kepala (ekor menipis)
            MyRibbon(l, cen, 0.45f, 1.9f, 0f, true, 3);
            for (int i = 2; i < n - 1; i += 2) { float u = i / (float)(n - 1), a = 0.55f + u * 5.35f; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); float w = Mathf.Lerp(0.45f, 1.9f, u) * 0.5f; l.Add(new[] { cen[i] + d * w, cen[i] - d * w }); }
            for (int i = 3; i < n - 2; i += 3) { float u = i / (float)(n - 1), a = 0.55f + u * 5.35f; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); MyLeaf(l, cen[i] + d * Mathf.Lerp(0.2f, 0.95f, u), cen[i] + d * (Mathf.Lerp(0.2f, 0.95f, u) + 0.8f), 0.5f, 0f, false); }
            // kepala di sisi kanan menggigit ekor
            Vector2 hp = cen[n - 1];
            MySnakeHead(l, hp + V(0.2f, -0.3f), 2.3f, 3.14159f + 0.55f - 0.0f, 0.3f + Mathf.Abs(Mathf.Sin(ph * 0.3f)) * 0.15f, true);
            // segel bintang di tengah
            for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f + ph * 0.1f, b = (i + 2) * Mathf.PI / 3f + ph * 0.1f; l.Add(new[] { c + V(Mathf.Cos(a), Mathf.Sin(a)) * 3.2f, c + V(Mathf.Cos(b), Mathf.Sin(b)) * 3.2f }); }
            l.Add(MyCircle(c, 2.2f, 2.2f, 18)); l.Add(MyCircle(c, 3.3f, 3.3f, 20));
            MyDisc(l, c, 0.7f);
            myHead = cen[n - 1] + V(-0.4f, 0.9f); myTop = c + V(0f, R + 2f);
            return l;
        }

        static List<Vector2[]> MyMidgard(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            const int n = 14;
            var sp1 = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float s = i / (float)(n - 1), x = -s * 30f;
                float arch = Mathf.Abs(Mathf.Sin(s * Mathf.PI * 3.5f - ph * 0.12f));
                float neckLift = Mathf.Pow(Mathf.Clamp01(1f - s / 0.2f), 2f) * 9f;
                sp1[i] = V(x, 1.2f + arch * 5.5f * Mathf.Clamp01(s * 6f) + neckLift);
            }
            MyRibbon(l, sp1, 3.0f, 0.5f, 0.1f, true, 6);
            var cs = MySpline(sp1, 6, false);
            for (int i = 4; i < cs.Length - 6; i += 4)
            {
                float u = i / (float)(cs.Length - 1), w = Mathf.Lerp(3.0f, 0.5f, u) * 0.5f;
                l.Add(new[] { cs[i] + V(0f, w), cs[i] + V(0.1f, -w) });
                if (i % 8 == 0) MyLeaf(l, cs[i] + V(0f, w), cs[i] + V(-0.5f, w + 1.0f), 0.55f, 0f, true);
            }
            Vector2 hp = sp1[0] + V(0.6f, 0.6f);
            MySnakeHead(l, hp, 3.2f, -0.05f, 0.4f + roar * 0.7f, true);
            MyRibbon(l, new[] { hp + V(0f, 1.2f), hp + V(-1.3f, 3.0f), hp + V(-2.7f, 3.4f) }, 0.6f, 0.05f, 0f, true, 3);
            MyRibbon(l, new[] { hp + V(0.6f, 1.2f), hp + V(-0.3f, 3.4f), hp + V(-1.5f, 4.2f) }, 0.6f, 0.05f, 0f, true, 3);
            l.Add(MyZig(myHead, myHead + V(2.0f, -0.4f), 4, 0.3f, ph));
            myTop = hp + V(0f, 5f);
            return l;
        }

        // ------------------------------------------------------------------ kura-kura
        static List<Vector2[]> MyTurtle(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            string p = sp.prof;
            // kaki
            for (int i = 0; i < 4; i++) { float x = -3.0f + i * 2.0f; MyBlob(l, new[] { V(x - 0.9f, 2.2f), V(x + 0.9f, 2.2f), V(x + 1.0f + Mathf.Sin(ph + i * 1.6f) * 0.3f, 0.5f), V(x + 1.3f, 0.0f), V(x - 0.8f, 0.0f), V(x - 1.0f, 0.8f) }, true, 3); }
            // cangkang berkubah
            var sh = new List<Vector2> { V(-5.2f, 2.0f) };
            for (int i = 0; i <= 10; i++) { float a = Mathf.Lerp(172f, 8f, i / 10f) * Mathf.Deg2Rad; sh.Add(V(Mathf.Cos(a) * 5.2f, 2.0f + Mathf.Sin(a) * 3.7f)); }
            sh.Add(V(5.2f, 2.0f));
            var shp = MySpline(sh.ToArray(), 3, true); MyAddPoly(shp);
            for (int i = -3; i <= 3; i++) { float xf = i * 1.5f; float hh = 3.7f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(xf / 5.2f, 2f))); l.Add(new[] { V(xf, 2.0f), V(xf * 0.85f, 2.0f + hh * 0.98f) }); }
            for (int r = 1; r <= 2; r++) l.Add(MyArc(V(0f, 2.0f), 5.2f * (1f - r * 0.28f), 12f, 168f, 12));
            // kepala dan ekor
            MyRibbon(l, new[] { V(4.6f, 2.8f), V(5.8f, 3.0f), V(6.6f, 3.0f) }, 1.4f, 1.0f, 0f, true, 3);
            if (p != "xuanwu") MyHead(l, V(6.2f, 2.6f), "canine", 1.1f, roar * 0.8f, 0.05f, false, 0f);
            MyRibbon(l, new[] { V(-5.0f, 1.6f), V(-6.5f, 1.2f), V(-7.6f, 0.7f) }, 0.9f, 0.1f, 0f, true, 3);
            if (p == "xuanwu")
            {
                var sn = new Vector2[8];
                for (int i = 0; i < 8; i++) { float u = i / 7f; sn[i] = V(-4.2f + u * 8.6f, 4.8f + Mathf.Sin(u * Mathf.PI) * 3.2f + Mathf.Sin(u * 8f + ph) * 0.45f); }
                MyRibbon(l, sn, 0.9f, 0.9f, 0.1f, true, 5);
                MySnakeHead(l, sn[7] + V(0.2f, 0.1f), 1.5f, -0.3f, roar * 0.9f + 0.15f, false);
                MyHead(l, V(6.2f, 2.6f), "canine", 1.0f, 0f, 0.05f, false, 0f);
                myHead = sn[7] + V(1.9f, -0.3f);
            }
            else
            {
                myHead = V(8.0f, 2.7f);
                if (p == "akupara")
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -3.2f + i * 1.6f, y0 = 2.0f + 3.6f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(x / 5.2f, 2f))) - 0.2f, h = 2.4f + Mathf.Abs(Mathf.Sin(i * 1.9f)) * 2.4f;
                        MyLeaf(l, V(x, y0), V(x + 0.1f, y0 + h), 1.6f, 0f, true);
                        if (i == 2) { MyLeaf(l, V(x, y0 + h), V(x, y0 + h + 1.6f), 0.7f, 0f, true); }
                    }
                    MyDisc(l, V(0f, 11.2f), 0.4f, true);
                }
                else if (p == "zaratan")
                {
                    for (int i = 0; i < 6; i++)
                    {
                        float x = -3.6f + i * 1.45f, y0 = 2.0f + 3.6f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow(x / 5.2f, 2f))) - 0.2f, h = 1.8f + Mathf.Abs(Mathf.Sin(i * 2.7f)) * 1.6f;
                        MyRibbon(l, new[] { V(x, y0), V(x + Mathf.Sin(i) * 0.2f, y0 + h) }, 0.28f, 0.15f, 0f, true, 3);
                        MyBlob(l, new[] { V(x - 0.8f, y0 + h + 0.2f), V(x - 0.5f, y0 + h + 1.4f), V(x + 0.3f, y0 + h + 1.8f), V(x + 0.9f, y0 + h + 0.8f), V(x + 0.5f, y0 + h) }, true, 3);
                    }
                }
            }
            myTop = V(0f, 7.5f);
            return l;
        }

        // ------------------------------------------------------------------ Kraken
        static List<Vector2[]> MyKraken(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            Vector2 c = V(0f, 8.0f);
            // tentakel besar
            for (int k = 0; k < 8; k++)
            {
                float ang = Mathf.Lerp(200f, 340f, k / 7f) * Mathf.Deg2Rad;
                float lenK = 9f + (k % 2) * 2f;
                var tc = new Vector2[6];
                for (int i = 0; i < 6; i++)
                {
                    float u = i / 5f;
                    float sx = Mathf.Cos(ang) * (3.0f + u * lenK) + Mathf.Sin(u * 5f + ph * 0.9f + k) * 1.1f * u;
                    float sy = Mathf.Sin(ang) * (1.0f + u * lenK * 0.6f) - u * u * 1.5f + 3.2f;
                    tc[i] = V(sx, Mathf.Max(0.1f, sy + 3.0f * (1f - u)));
                }
                MyRibbon(l, tc, 1.9f, 0.1f, 0f, true, 4);
                var cs = MySpline(tc, 4, false);
                for (int i = 3; i < cs.Length - 3; i += 3) MyDisc(l, cs[i] + V(0f, 0.02f), 0.2f * (1f - i / (float)cs.Length), false);
            }
            // mantel bergelombang
            MyBlob(l, new[] { c + V(-3.8f, -1.4f), c + V(-3.2f, 2.4f), c + V(-1.0f, 5.0f), c + V(1.8f, 5.3f), c + V(3.8f, 2.4f), c + V(4.0f, -1.4f), c + V(1.6f, -3.2f), c + V(-1.6f, -3.2f) }, true, 4);
            for (int i = 0; i < 6; i++) l.Add(MyArc(c + V(-2.5f + i, 0.6f), 1.6f, 70f, 120f, 3));
            // kepala dengan paruh dan mata
            MyBlob(l, new[] { c + V(1.0f, -1.2f), c + V(2.6f, -0.2f), c + V(4.0f, -1.8f), c + V(3.0f, -3.3f), c + V(1.4f, -3.0f) }, true, 3);
            MyDisc(l, c + V(2.4f, -1.1f), 0.55f, true); MyDisc(l, c + V(2.4f, -1.1f), 0.18f, false);
            MyLeaf(l, c + V(3.4f, -2.5f), c + V(4.8f, -3.7f - roar * 0.5f), 0.9f, 0.1f, true);
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI * 2f / 8f; l.Add(new[] { c + V(2.4f, -1.1f) + V(Mathf.Cos(a), Mathf.Sin(a)) * 0.7f, c + V(2.4f, -1.1f) + V(Mathf.Cos(a), Mathf.Sin(a)) * 1.0f }); }
            myHead = c + V(4.4f, -3.2f); myTop = V(0f, 14f);
            return l;
        }

        // ------------------------------------------------------------------ Maw (mulut kosmik / pusaran bergigi)
        static List<Vector2[]> MyMaw(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            Vector2 c = V(0f, 7.0f);
            for (int arm = 0; arm < 5; arm++)
            {
                var a = new Vector2[12];
                for (int i = 0; i < 12; i++) { float u = i / 11f, ang = arm * Mathf.PI * 2f / 5f + u * 4.6f - ph * 0.5f, r = 0.8f + u * 7.2f; a[i] = c + V(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r * 0.9f); }
                MyRibbon(l, a, 0.1f, 1.7f, 0f, true, 4);
            }
            var ring = MyCircle(c, 7.4f, 6.8f, 28); l.Add(ring);
            l.Add(MyCircle(c, 6.0f, 5.5f, 28));
            for (int i = 0; i < 20; i++)
            {
                float ang = i * Mathf.PI * 2f / 20f + ph * 0.08f; Vector2 d = V(Mathf.Cos(ang), Mathf.Sin(ang) * 0.92f);
                MyLeaf(l, c + d * 7.2f, c + d * 4.6f, 0.9f, 0f, true);
            }
            for (int i = 0; i < 5; i++) { float ang = i * 1.3f + ph * 0.2f; l.Add(MyZig(c + V(Mathf.Cos(ang), Mathf.Sin(ang)) * 7.8f, c + V(Mathf.Cos(ang), Mathf.Sin(ang)) * 10.5f, 5, 0.5f, i + ph)); }
            MyDisc(l, c, 1.3f);
            myHead = c; myTop = c + V(0f, 10f);
            return l;
        }

        // ------------------------------------------------------------------ naga & wyvern (melayang berlekuk)
        static Vector2[] MyDragonSpine(float ph, int n, float len, float amp, float yBase)
        {
            var s = new Vector2[n];
            for (int i = 0; i < n; i++) { float u = i / (float)(n - 1); s[i] = V(-u * len, yBase + amp * Mathf.Sin(u * 7f - ph) * Mathf.Min(1f, u * 3f) - u * 0.7f); }
            return s;
        }

        static List<Vector2[]> MyDragon(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            string p = sp.prof;
            var sx = MyDragonSpine(ph, 9, 17f, 1.7f, 3.4f);
            // cakar belakang & depan
            MyLeg(l, V(-5.5f, 3.2f), 3.0f, ph + 1f, 1.2f, 0.7f, 1.0f, -1f, false, 1.5f);
            MyLeg(l, V(1.0f, 3.4f), 3.0f, ph, 1.2f, 0.7f, 0.9f, 1f, false, 1.4f);
            MyRibbon(l, sx, 3.0f, 0.15f, 0.2f, true, 5);
            MyBlob(l, new[] { V(-2.4f, 3.0f), V(-1.8f, 5.0f), V(0.2f, 5.7f), V(1.9f, 4.7f), V(1.7f, 2.7f), V(0f, 1.8f), V(-1.6f, 2.0f) }, true, 4);
            var cs = MySpline(sx, 5, false);
            for (int i = 3; i < cs.Length - 2; i += 3)
            {
                float u = i / (float)(cs.Length - 1), w = Mathf.Lerp(3.0f, 0.15f, u) * 0.5f;
                l.Add(new[] { cs[i] + V(0f, -w * 0.9f), cs[i] + V(0.1f, -w * 0.1f) });   // perut berlapis
                MyLeaf(l, cs[i] + V(0f, w), cs[i] + V(-0.4f, w + 0.9f * (1f - u * 0.6f)), 0.5f, 0f, i % 6 == 0);
            }
            Vector2 sh = V(-2.8f, 5.0f);
            Vector2 neckB = V(0.6f, 4.2f);
            MyRibbon(l, new[] { neckB, V(1.8f, 5.6f), V(3.0f, 6.0f), V(4.2f, 5.4f) }, 2.4f, 1.5f, 0.1f, true, 5);
            MyHead(l, V(4.2f, 5.35f), "feline", 2.3f, roar, -0.12f, true, 0f);
            Vector2 hb = V(4.2f, 5.35f) + V(0.3f, 1.0f);
            MyRibbon(l, new[] { hb, hb + V(-1.2f, 1.6f), hb + V(-2.6f, 1.7f) }, 0.5f, 0.04f, 0f, true, 3);
            MyRibbon(l, new[] { hb + V(0.5f, 0f), hb + V(-0.6f, 1.9f), hb + V(-1.8f, 2.7f) }, 0.5f, 0.04f, 0f, true, 3);
            for (int i = 0; i < 2; i++) { Vector2 w = V(5.6f + i * 0.4f, 5.0f); MyLeaf(l, w, w + V(1.2f, -1.0f - i * 0.6f + Mathf.Sin(ph + i) * 0.2f), 0.18f, 0.1f, false); }
            if (p == "typhon")
            {
                MyBatWing(l, sh, 9.5f, 7.5f, Mathf.Sin(ph * 0.6f)); MyBatWing(l, sh + V(-1.5f, -0.3f), 8f, 6.2f, Mathf.Sin(ph * 0.6f + 0.7f));
                for (int k = 0; k < 2; k++)
                {
                    Vector2 b0 = V(0.3f - k * 0.6f, 5.1f + k * 0.2f);
                    var neck = new[] { b0, b0 + V(0.9f, 2.0f + k), b0 + V(2.0f + k * 0.4f, 3.6f + k), b0 + V(3.4f + k * 0.5f, 4.0f + k * 1.2f) };
                    MyRibbon(l, neck, 1.2f, 0.8f, 0.05f, true, 4);
                    MySnakeHead(l, neck[3], 1.5f, 0.3f - k * 0.3f, 0.4f, false);
                }
                for (int i = 0; i < 6; i++) MyFlame(l, V(-i * 2.6f, 5.4f - i * 0.2f), 1.8f, ph, i);
            }
            else if (p == "nidhogg")
            {
                for (int i = 0; i < 6; i++) { float x = -i * 2.4f; MyLeaf(l, V(x, 5.0f - i * 0.1f), V(x - 0.7f, 7.2f + (i % 2) * 1.0f), 0.9f, 0.05f, true); }
                for (int i = 0; i < 5; i++) MyLeaf(l, V(5.2f + i * 0.3f, 4.7f), V(5.3f + i * 0.3f, 3.5f - (i % 2) * 0.4f), 0.2f, 0f, false);
                MyBatWing(l, sh, 8.5f, 6f, Mathf.Sin(ph * 0.6f));
                for (int i = 0; i < 5; i++) MyLeaf(l, V(-i * 3.0f, 2.3f), V(-i * 3.0f - 1.3f, 1.2f + Mathf.Sin(i) * 0.3f), 0.5f, 0f, false);
            }
            else if (p == "fafnir")
            {
                MyDisc(l, V(-0.8f, 3.7f), 0.8f);
                for (int i = 0; i < 6; i++) MyBlob(l, new[] { V(-1.0f + i * 0.7f, 0.1f), V(-0.5f + i * 0.7f, 0.5f), V(0.1f + i * 0.7f, 0.1f), V(-0.4f + i * 0.7f, -0.1f) }, true, 2);
                MyBatWing(l, sh, 7f, 5.5f, Mathf.Sin(ph * 0.6f));
                for (int i = 0; i < 7; i++) MyFlame(l, V(-i * 2.4f, 5.2f - i * 0.2f), 1.5f, ph, i);
            }
            else // bahamut
            {
                MyWingF(l, sh + V(0f, 0.3f), 11.0f, Mathf.Sin(ph * 0.6f) * 16f, 0.3f);
                MyWingF(l, sh + V(-0.8f, 0.1f), 9.0f, Mathf.Sin(ph * 0.6f + 0.6f) * 16f - 8f, 0.5f, false);
                Vector2 hc2 = V(-4.0f, 5.6f);
                l.Add(MyCircle(hc2, 7.0f, 7.0f, 32)); l.Add(MyCircle(hc2, 6.2f, 6.2f, 32));
                for (int i = 0; i < 16; i++) { float a = i * Mathf.PI * 2f / 16f + ph * 0.05f; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); MyLeaf(l, hc2 + d * 7.1f, hc2 + d * (8.4f + (i % 2) * 0.5f), 0.4f, 0f, false); }
                MyDisc(l, V(1.4f, 3.3f), 0.9f);
            }
            myHead = V(7.2f, 5.0f); myTop = V(-3f, 10f);
            return l;
        }

        static List<Vector2[]> MyWyvern(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            var sx = MyDragonSpine(ph, 8, 12f, 1.3f, 3.0f);
            MyLeg(l, V(-3.6f, 2.9f), 2.8f, ph + 0.6f, 1.2f, 0.8f, 1.0f, -1f, false, 1.5f);
            MyLeg(l, V(-2.8f, 2.9f), 2.8f, ph + 2.6f, 1.2f, 0.8f, 0.8f, -1f, false, 1.5f);
            MyRibbon(l, sx, 2.6f, 0.15f, 0.2f, true, 5);
            MyBlob(l, new[] { V(-2.0f, 2.8f), V(-1.5f, 4.4f), V(0.1f, 5.0f), V(1.5f, 4.2f), V(1.4f, 2.5f), V(0f, 1.8f), V(-1.4f, 1.9f) }, true, 4);
            Vector2 tip = sx[7];
            MyBlob(l, new[] { tip + V(0.1f, 0f), tip + V(-1.0f, 1.2f), tip + V(-2.3f, 0f), tip + V(-1.0f, -1.2f) }, true, 3);
            Vector2 sh = V(-1.6f, 4.2f);
            MyBatWing(l, sh, 9f, 6.8f, Mathf.Sin(ph * 0.6f)); MyBatWing(l, sh + V(-1.0f, -0.2f), 7.5f, 5.6f, Mathf.Sin(ph * 0.6f + 0.7f));
            MyRibbon(l, new[] { V(-0.2f, 3.8f), V(0.8f, 4.8f), V(1.6f, 4.9f) }, 1.8f, 1.2f, 0.1f, true, 4);
            MyHead(l, V(1.6f, 4.7f), "feline", 1.9f, roar, -0.1f, true, 0f);
            MyRibbon(l, new[] { V(1.8f, 5.6f), V(0.8f, 6.7f), V(-0.4f, 6.9f) }, 0.4f, 0.04f, 0f, true, 3);
            for (int i = 0; i < 5; i++) { float u = i / 4f; MyLeaf(l, V(1.0f - u * 9f, 4.4f - u * 1.0f + 0.7f), V(0.8f - u * 9f, 5.5f - u * 1.0f), 0.5f, 0f, i % 2 == 0); }
            myHead = V(4.3f, 4.5f); myTop = V(-2f, 10f);
            return l;
        }

        // ------------------------------------------------------------------ Tetramorph: roda bermata dengan empat wajah dan sayap
        static List<Vector2[]> MyTetramorph(MySpec sp, float ph, float roar)
        {
            var l = new List<Vector2[]>();
            Vector2 c = V(0f, 7.0f);
            MyWingF(l, c + V(0f, 3.0f), 10.5f, Mathf.Sin(ph * 0.6f) * 14f, 0.1f);
            MyWingF(l, c + V(0f, 2.8f), 8.5f, -Mathf.Sin(ph * 0.6f) * 14f - 8f, -0.2f, false);
            var o1 = MyCircle(c, 4.1f, 4.1f, 32); l.Add(o1);
            l.Add(MyCircle(c, 3.3f, 3.3f, 32));
            for (int i = 0; i < 14; i++) { float a = i * Mathf.PI * 2f / 14f + ph * 0.15f; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); MyDisc(l, c + d * 3.7f, 0.36f, true); MyDisc(l, c + d * 3.7f, 0.12f, false); }
            for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f + ph * 0.05f; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); l.Add(new[] { c + d * 1.2f, c + d * 3.3f }); }
            MyDisc(l, c, 1.2f); MyDisc(l, c, 0.5f, false);
            // empat wajah: atas manusia, kanan singa, bawah banteng, kiri elang
            MyHumanFace(l, c + V(0f, 5.7f), 0.95f, 0f, sp);
            MyHead(l, c + V(4.3f, -0.4f), "feline", 1.5f, roar, 0f, true, 0.3f);
            MyHead(l, c + V(0.4f, -4.5f), "bovine", 1.3f, 0f, -1.57f, false, 0f);
            MyHead(l, c + V(-4.5f, -0.1f), "eagle", 1.3f, 0f, 3.14159f, false, 0f);
            for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6f + ph * 0.3f; Vector2 d = V(Mathf.Cos(a), Mathf.Sin(a)); MyLeaf(l, c + d * 6.3f, c + d * (7.3f + (i % 2) * 0.8f), 0.4f, 0f, false); }
            myHead = c + V(7.2f, -0.4f); myTop = c + V(0f, 9f);
            return l;
        }

        // ------------------------------------------------------------------ dispatcher
        static List<Vector2[]> MythShape(MySpec sp, float ph, float roar)
        {
            myTop = V(0f, 8f); myHead = V(4f, 3f); myFills.Clear();
            switch (sp.bas)
            {
                case "q": return MyQuad(sp, ph, roar);
                case "b": return MyBird(sp, ph, roar);
                case "h": return MyHuman(sp, ph, roar);
                case "y": return MyHydra(sp, ph, roar);
                case "o": return MyOuroboros(sp, ph, roar);
                case "s": return MyMidgard(sp, ph, roar);
                case "t": return MyTurtle(sp, ph, roar);
                case "k": return MyKraken(sp, ph, roar);
                case "m": return MyMaw(sp, ph, roar);
                case "d": return MyDragon(sp, ph, roar);
                case "w": return MyWyvern(sp, ph, roar);
                case "r": return MyTetramorph(sp, ph, roar);
            }
            return MyQuad(sp, ph, roar);
        }
    }
}
