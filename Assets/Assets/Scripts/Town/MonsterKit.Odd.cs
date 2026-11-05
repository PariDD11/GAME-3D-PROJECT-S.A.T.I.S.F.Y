using System;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Bentuk khusus: slime, ayam, kalajengking, golem batu.</summary>
    public static partial class MonsterKit
    {
        static Vector3 P3(float x, float y, float z) { return new Vector3(x, y, z); }

        // ------------------------------------------------------------------ slime
        static readonly float[] SlimeY = { 0.02f, 0.05f, 0.12f, 0.26f, 0.52f, 0.86f, 1.07f, 1.23f };
        static readonly float[] SlimeRad = { 0.12f, 0.58f, 0.78f, 0.86f, 0.68f, 0.38f, 0.17f, 0.03f };

        // titik di permukaan tetes pada ketinggian y, sudut phi (derajat dari depan), menjorok off keluar
        static Vector3 SlimeSurf(float y, float phi, float off)
        {
            float r = SlimeRad[SlimeRad.Length - 1];
            for (int i = 0; i < SlimeY.Length - 1; i++)
                if (y <= SlimeY[i + 1]) { r = Mathf.Lerp(SlimeRad[i], SlimeRad[i + 1], Mathf.InverseLerp(SlimeY[i], SlimeY[i + 1], y)); break; }
            r += off; float a = phi * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r * 0.96f);
        }

        static void SlimeBody(MkPart p)
        {
            Vector3[] pts = new Vector3[SlimeY.Length];
            float[] rz = new float[SlimeY.Length];
            for (int i = 0; i < pts.Length; i++) { pts[i] = P3(i >= 6 ? 0.07f * (i - 5) : 0f, SlimeY[i], i >= 6 ? -0.035f * (i - 5) : 0f); rz[i] = SlimeRad[i] * 0.96f; }
            Tube(p.S, MS.Body, pts, SlimeRad, rz, 16, Vector3.forward, true, true);
            // kilap + wajah kawaii (dipasang di permukaan, menghadap keluar)
            Ell(p.S, MS.Pale, SlimeSurf(0.8f, -30f, 0.012f), P3(0.055f, 0.14f, 0.03f), Quaternion.Euler(0f, -30f, 14f), 3, 8);
            for (int q = -1; q <= 1; q += 2)
            {
                float ph = 19f * q;
                Ell(p.S, MS.Pupil, SlimeSurf(0.55f, ph, 0.0f), P3(0.11f, 0.15f, 0.05f), Quaternion.Euler(0f, ph, 0f), 4, 9);
                Ell(p.S, MS.Pale, SlimeSurf(0.61f, ph + 5f * q, 0.04f), P3(0.04f, 0.05f, 0.02f), Quaternion.Euler(0f, ph, 0f), 3, 6);
                Ell(p.S, MS.Mouth, SlimeSurf(0.38f, 40f * q, 0.0f), P3(0.07f, 0.045f, 0.03f), Quaternion.Euler(0f, 40f * q, 0f), 3, 7);      // pipi merona
            }
            Tube(p.S, MS.Mouth, new[] { SlimeSurf(0.4f, -11f, 0.015f), SlimeSurf(0.35f, 0f, 0.015f), SlimeSurf(0.4f, 11f, 0.015f) }, new[] { 0.016f, 0.016f, 0.016f }, 5, true, true);
        }

        static void Slime(MkCtx c)
        {
            var col = c.col; Color g = col[MS.Body];
            col[MS.Pale] = Mix(g, Color.white, 0.62f); col[MS.Dark] = Dk(g, 0.6f);
            col[MS.Pupil] = new Color(0.06f, 0.08f, 0.1f); col[MS.Mouth] = Mix(g, new Color(0.9f, 0.25f, 0.32f), 0.6f);
            c.New("puddle", null, P3(0f, 0f, 0f), delegate (MkPart pp)
            {
                Ell(pp.S, MS.Body, P3(0f, 0.03f, 0f), P3(1.02f, 0.045f, 0.98f), 3, 16);
                Ell(pp.S, MS.Body, P3(0.92f, 0.05f, 0.34f), P3(0.16f, 0.09f, 0.14f), 3, 8);
                Ell(pp.S, MS.Body, P3(-0.86f, 0.05f, -0.4f), P3(0.13f, 0.08f, 0.12f), 3, 8);
            });
            var body = c.New("body", null, P3(0f, 0.03f, 0f), delegate (MkPart pb) { SlimeBody(pb); });
            Vector3 bs0 = body.t.localScale, bp0 = body.t.localPosition;
            c.rig.Add(delegate (MonsterRig r)
            {
                float hop = Mathf.Abs(Mathf.Sin(r.phase * 0.5f)) * r.spd01;
                float idle = Mathf.Sin(r.time * 2.3f) * 0.035f;
                float sy = 1f + idle + 0.16f * hop - 0.2f * r.wind + 0.22f * r.lunge;
                float sxz = 1f - idle * 0.6f - 0.07f * hop + 0.1f * r.wind - 0.09f * r.lunge;
                body.t.localScale = new Vector3(bs0.x * sxz, bs0.y * sy, bs0.z * sxz);
                body.t.localPosition = bp0 + new Vector3(0f, hop * 0.3f, 0.22f * r.lunge);
            });
        }

        // ------------------------------------------------------------------ ayam
        static void ChickenTorso(MkPart p)
        {
            Ell(p.S, MS.Body, P3(0f, 0.44f, 0.02f), P3(0.2f, 0.21f, 0.28f), Quaternion.Euler(-12f, 0f, 0f), 6, 12);
            Ell(p.S, MS.Pale, P3(0f, 0.37f, 0.17f), P3(0.15f, 0.15f, 0.17f), 4, 10);                          // dada berbulu
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Ell(p.S, MS.Dark, P3(0.2f * sd, 0.46f, -0.02f), P3(0.045f, 0.14f, 0.2f), Quaternion.Euler(8f, 0f, -10f * sd), 4, 9);       // sayap
                Ear(p.S, MS.Dark, P3(0.19f * sd, 0.4f, -0.14f), P3(0.2f * sd, 0.33f, -0.36f), 0.05f, 0.016f, Vector3.up, 3);
            }
            for (int t = -2; t <= 2; t++)                                                                      // ekor: kipas bulu
                Ear(p.S, t % 2 == 0 ? MS.Dark : MS.Body, P3(0f, 0.5f, -0.24f), P3(t * 0.07f, 0.8f - Mathf.Abs(t) * 0.06f, -0.4f - Mathf.Abs(t) * 0.02f), 0.05f, 0.015f, Vector3.right, 4);
        }

        static void ChickenHead(MkPart p)
        {
            Tube(p.S, MS.Body, new[] { P3(0f, 0.52f, 0.16f), P3(0f, 0.66f, 0.27f), P3(0f, 0.76f, 0.31f) }, new[] { 0.09f, 0.07f, 0.06f }, 8, true, true);
            Ell(p.S, MS.Body, P3(0f, 0.8f, 0.33f), P3(0.075f, 0.085f, 0.09f), 5, 10);
            Spike(p.S, MS.Accent, P3(0f, 0.785f, 0.4f), P3(0f, 0.76f, 0.5f), 0.036f, 6);                       // paruh
            Spike(p.S, MS.Accent, P3(0f, 0.775f, 0.4f), P3(0f, 0.745f, 0.48f), 0.028f, 6);
            Ell(p.S, MS.Mouth, P3(0f, 0.89f, 0.31f), P3(0.018f, 0.04f, 0.04f), 3, 7);                          // jengger
            Ell(p.S, MS.Mouth, P3(0f, 0.9f, 0.355f), P3(0.018f, 0.05f, 0.035f), 3, 7);
            Ell(p.S, MS.Mouth, P3(0f, 0.88f, 0.405f), P3(0.018f, 0.035f, 0.03f), 3, 7);
            Ell(p.S, MS.Mouth, P3(0f, 0.7f, 0.4f), P3(0.02f, 0.045f, 0.02f), 3, 7);                            // pial
            for (int sd = -1; sd <= 1; sd += 2) Ell(p.S, MS.Pupil, P3(0.062f * sd, 0.82f, 0.37f), P3(0.017f, 0.02f, 0.017f), 3, 6);
        }

        static void ChickenLeg(MkPart p, float sg)
        {
            Tube(p.S, MS.Accent, new[] { P3(0.08f * sg, 0.3f, 0f), P3(0.08f * sg, 0.05f, 0.02f) }, new[] { 0.024f, 0.018f }, 6, true, true);
            for (int k = -1; k <= 1; k++) Spike(p.S, MS.Accent, P3(0.08f * sg, 0.03f, 0.02f), P3(0.08f * sg + k * 0.045f, 0.0f, 0.15f), 0.014f, 4);
            Spike(p.S, MS.Accent, P3(0.08f * sg, 0.03f, 0.02f), P3(0.08f * sg, 0.0f, -0.07f), 0.012f, 4);
        }

        static void Chicken(MkCtx c)
        {
            var col = c.col; Color w = col[MS.Body];
            col[MS.Dark] = Dk(w, 0.78f); col[MS.Pale] = Mix(w, Color.white, 0.5f); col[MS.Accent] = new Color(0.96f, 0.66f, 0.16f);
            col[MS.Mouth] = new Color(0.84f, 0.16f, 0.14f); col[MS.Pupil] = new Color(0.05f, 0.04f, 0.04f); col[MS.Claw] = new Color(0.9f, 0.58f, 0.15f);
            var torso = c.New("torso", null, P3(0f, 0.42f, 0f), delegate (MkPart pt) { ChickenTorso(pt); });
            var head = c.New("head", torso, P3(0f, 0.55f, 0.2f), delegate (MkPart ph) { ChickenHead(ph); });
            for (int sd = 0; sd < 2; sd++)
            {
                float sg = sd == 0 ? 1f : -1f;
                var leg = c.New("leg" + sd, torso, P3(0.08f * sg, 0.3f, 0f), delegate (MkPart pl) { ChickenLeg(pl, sg); });
                c.rig.Leg(leg.t, null, sd == 0 ? 0f : Mathf.PI, 34f, 0f);
            }
            Vector3 tp0 = torso.t.localPosition, hp0 = head.t.localPosition; Quaternion hq0 = head.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                float nod = Mathf.Sin(r.phase * 2f) * r.spd01;
                float peck = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(r.time * 1.7f)), 10f) * (1f - r.spd01);
                torso.t.localPosition = tp0 + new Vector3(0f, Mathf.Abs(Mathf.Sin(r.phase)) * 0.025f * r.spd01 - 0.04f * r.wind, 0.08f * r.lunge);
                head.t.localPosition = hp0 + new Vector3(0f, 0f, nod * 0.035f + 0.06f * r.lunge);
                head.t.localRotation = hq0 * Quaternion.AngleAxis(peck * 55f + 20f * r.lunge, Vector3.right);
            });
        }

        // ------------------------------------------------------------------ kalajengking
        static void ScorpBody(MkPart p, bool queen, bool gold)
        {
            Ell(p.S, MS.Body, P3(0f, 0.7f, 0.62f), P3(0.6f, 0.31f, 0.68f), Quaternion.Euler(-4f, 0f, 0f), 6, 14);
            Vector3[] bp = { P3(0f, 0.68f, 0.15f), P3(0f, 0.66f, -0.5f), P3(0f, 0.65f, -1.1f), P3(0f, 0.68f, -1.5f) };
            float[] brx = { 0.56f, 0.5f, 0.4f, 0.28f }, bry = { 0.3f, 0.29f, 0.26f, 0.2f };
            TubeBands(p.S, new[] { MS.Dark, MS.Body, MS.Pale, MS.Body }, new[] { 40f, 130f, 230f, 310f, 400f }, bp, brx, bry, 5, Vector3.up, true, true);
            for (int k = 0; k < 5; k++)                                                                          // ruas perut: cincin gelap
            {
                float z = -0.05f - k * 0.3f, f = Mathf.Clamp01((0.15f - z) / 1.65f);
                float rxk = Mathf.Lerp(0.56f, 0.28f, f) + 0.025f, ryk = Mathf.Lerp(0.3f, 0.2f, f) + 0.025f;
                Ell(p.S, MS.Dark, P3(0f, 0.67f, z), P3(rxk, ryk, 0.035f), 3, 14);
                if (queen) Spike(p.S, MS.Accent, P3(0f, 0.67f + ryk * 0.9f, z), P3(0f, 0.67f + ryk * 0.9f + 0.17f, z - 0.07f), 0.05f, 5);
            }
            Tube(p.S, MS.Dark, new[] { P3(0f, 0.99f, 0.3f), P3(0f, 0.985f, 0.7f), P3(0f, 0.95f, 1.1f) }, new[] { 0.06f, 0.06f, 0.05f }, 6, true, true);   // lunas karapas
            EyePair(p, P3(0.12f, 0.99f, 1.0f), 0.05f, P3(0.2f, 0.4f, 1f), 0.4f, 0.6f);
            for (int q = -1; q <= 1; q += 2)
            {
                Ell(p.G, MS.Eye, P3(0.34f * q, 0.9f, 1.02f), P3(0.03f, 0.03f, 0.03f), 3, 6);
                Ell(p.G, MS.Eye, P3(0.27f * q, 0.93f, 1.08f), P3(0.025f, 0.025f, 0.025f), 3, 6);
                Spike(p.S, MS.Bone, P3(0.14f * q, 0.58f, 1.22f), P3(0.05f * q, 0.42f, 1.6f), 0.06f, 5);          // rahang (chelicerae)
            }
            Ell(p.S, MS.Dark, P3(0f, 0.55f, 1.25f), P3(0.17f, 0.1f, 0.12f), 3, 8);
            if (queen) for (int k = -2; k <= 2; k++) Spike(p.S, MS.Accent, P3(k * 0.17f, 0.97f - Mathf.Abs(k) * 0.05f, 0.4f), P3(k * 0.2f, 1.4f - Mathf.Abs(k) * 0.1f, 0.3f), 0.065f, 5);   // mahkota ratu
            if (gold) { Ell(p.G, MS.Ember, P3(0f, 1.0f, 0.05f), P3(0.12f, 0.08f, 0.14f), 4, 9); Ell(p.S, MS.Accent, P3(0f, 0.97f, 0.05f), P3(0.17f, 0.07f, 0.2f), 4, 9); }
        }

        static void ScorpTail(MkPart p, bool queen, bool gold)
        {
            Vector3[] tp = { P3(0f, 0.68f, -1.45f), P3(0f, 0.8f, -1.92f), P3(0f, 1.18f, -2.28f), P3(0f, 1.68f, -2.3f), P3(0f, 2.1f, -2.05f), P3(0f, 2.34f, -1.62f), P3(0f, 2.36f, -1.2f) };
            float[] tr = { 0.22f, 0.2f, 0.18f, 0.16f, 0.14f, 0.12f, 0.1f }, tt = new float[tr.Length];
            for (int i = 0; i < tr.Length; i++) tt[i] = tr[i] * 0.84f;
            Tube(p.S, MS.Body, tp, tt, 10, true, false);
            for (int i = 1; i < tp.Length; i++) Ell(p.S, i % 2 == 0 ? MS.Dark : MS.Body, tp[i], P3(tr[i] * 1.12f, tr[i] * 1.12f, tr[i] * 1.12f), 4, 9);
            Ell(p.S, MS.Pale, P3(0f, 2.37f, -1.08f), P3(0.13f, 0.14f, 0.17f), 4, 9);                             // kantung racun
            Spike(p.S, MS.Claw, P3(0f, 2.4f, -0.98f), P3(0f, 2.1f, -0.7f), 0.08f, 6);                           // sengat
            Ell(p.G, MS.Ember, P3(0f, 2.28f, -0.88f), P3(0.04f, 0.05f, 0.07f), 3, 7);                           // setetes racun
            if (queen) for (int i = 2; i < 6; i++) Spike(p.S, MS.Accent, tp[i] + P3(0f, tr[i], 0f), tp[i] + P3(0f, tr[i] + 0.2f, -0.08f), 0.045f, 4);
        }

        static void ScorpArm(MkPart p, float sg)
        {
            Vector3 sh = P3(0.45f * sg, 0.66f, 1.0f), el = P3(0.98f * sg, 0.74f, 1.48f), wr = P3(0.8f * sg, 0.72f, 1.95f);
            Tube(p.S, MS.Body, new[] { sh, el, wr }, new[] { 0.14f, 0.12f, 0.11f }, 8, true, true);
            Ell(p.S, MS.Dark, el, P3(0.15f, 0.14f, 0.15f), 4, 8);
            Ell(p.S, MS.Pale, P3(0.74f * sg, 0.72f, 2.22f), P3(0.24f, 0.17f, 0.38f), Quaternion.Euler(0f, -10f * sg, 0f), 5, 10);                // telapak capit
            Horn(p.S, MS.Pale, P3(0.62f * sg, 0.72f, 2.5f), P3(0.34f * sg, 0.72f, 3.0f), P3(-0.1f * sg, 0f, 0f), 0.13f, 7);                         // jari tetap
        }

        static void ScorpPinch(MkPart p, float sg)
        {
            Horn(p.S, MS.Pale, P3(0.84f * sg, 0.72f, 2.4f), P3(0.58f * sg, 0.72f, 3.0f), P3(-0.12f * sg, 0f, 0f), 0.12f, 7);                         // jari bergerak
        }

        static void ScorpLeg(MkPart p, float sg, float z0, int idx)
        {
            float fz = z0 + 0.45f - idx * 0.2f;
            Vector3 hp = P3(0.42f * sg, 0.56f, z0), kn = P3(0.95f * sg, 0.95f, z0 + 0.1f), an = P3(1.35f * sg, 0.5f, z0 + 0.28f), ft = P3(1.5f * sg, 0.02f, fz);
            Tube(p.S, MS.Body, new[] { hp, kn, an, ft }, new[] { 0.1f, 0.085f, 0.06f, 0.03f }, 7, true, true);
            Ell(p.S, MS.Dark, kn, P3(0.11f, 0.11f, 0.11f), 3, 8);
            Spike(p.S, MS.Claw, ft, ft + P3(0.04f * sg, -0.02f, 0.1f), 0.03f, 4);
        }

        static void Scorpion(MkCtx c)
        {
            bool queen = c.V("queen"), gold = c.V("gold");
            var col = c.col; Color g = col[MS.Body];
            col[MS.Pale] = Mix(g, new Color(1f, 0.95f, 0.8f), 0.45f); col[MS.Dark] = Dk(g, 0.55f); col[MS.Claw] = Dk(g, 0.3f);
            col[MS.Eye] = queen ? new Color(0.5f, 1f, 0.95f) : new Color(1f, 0.3f, 0.2f);
            col[MS.Ember] = queen ? new Color(0.4f, 1f, 0.8f) : (gold ? new Color(1f, 0.6f, 0.15f) : new Color(1f, 0.35f, 0.2f));
            col[MS.Accent] = gold ? new Color(1f, 0.82f, 0.3f) : (queen ? new Color(0.65f, 0.95f, 0.9f) : new Color(0.9f, 0.7f, 0.3f));
            var body = c.New("body", null, P3(0f, 0.62f, 0.1f), delegate (MkPart pb) { ScorpBody(pb, queen, gold); });
            var tail = c.New("tail", body, P3(0f, 0.68f, -1.45f), delegate (MkPart pt) { ScorpTail(pt, queen, gold); });
            for (int sd = 0; sd < 2; sd++)
            {
                float sg = sd == 0 ? 1f : -1f; int sdc = sd;
                var arm = c.New("arm" + sd, body, P3(0.45f * sg, 0.66f, 1.0f), delegate (MkPart pa) { ScorpArm(pa, sg); });
                var pin = c.New("pin" + sd, arm, P3(0.84f * sg, 0.72f, 2.4f), delegate (MkPart pp) { ScorpPinch(pp, sg); });
                Quaternion aq = arm.t.localRotation, pq = pin.t.localRotation;
                c.rig.Add(delegate (MonsterRig r)
                {
                    float sw = Mathf.Sin(r.phase * 1.3f + (sdc == 0 ? 0f : Mathf.PI)) * 6f * r.spd01;
                    arm.t.localRotation = aq * Quaternion.AngleAxis(-sg * (sw + 6f * r.lunge), Vector3.up) * Quaternion.AngleAxis(-24f * r.wind + 6f * r.lunge, Vector3.right);
                    float open = 12f + 22f * r.wind + Mathf.Sin(r.time * 2.5f + sdc) * 4f;
                    pin.t.localRotation = pq * Quaternion.AngleAxis(sg * open * (1f - r.lunge), Vector3.up);
                });
                for (int i = 0; i < 4; i++)
                {
                    int ic = i; float z0 = 0.72f - i * 0.36f;
                    var leg = c.New("leg" + i + "_" + sd, null, P3(0.42f * sg, 0.56f, z0), delegate (MkPart pl) { ScorpLeg(pl, sg, z0, ic); });
                    Quaternion q0 = leg.t.localRotation; float off = ((i + sd) % 2 == 0) ? 0f : Mathf.PI;
                    c.rig.Add(delegate (MonsterRig r)
                    {
                        float sn = Mathf.Sin(r.phase * 1.3f + off), lift = Mathf.Max(0f, Mathf.Cos(r.phase * 1.3f + off));
                        leg.t.localRotation = q0 * Quaternion.AngleAxis(sn * 20f * r.spd01, Vector3.up) * Quaternion.AngleAxis(sg * lift * 14f * r.spd01, Vector3.forward);
                    });
                }
            }
            Vector3 bp0 = body.t.localPosition; Quaternion bq0 = body.t.localRotation, tq0 = tail.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                body.t.localPosition = bp0 + new Vector3(0f, Mathf.Abs(Mathf.Sin(r.phase * 1.3f)) * 0.03f * r.spd01 - 0.06f * r.wind, 0.3f * r.lunge);
                body.t.localRotation = bq0 * Quaternion.AngleAxis(-6f * r.wind + 8f * r.lunge, Vector3.right) * Quaternion.AngleAxis(Mathf.Sin(r.phase * 1.3f) * 2f * r.spd01, Vector3.forward);
                float idle = Mathf.Sin(r.time * 1.7f) * 3f + Mathf.Sin(r.phase) * 2f * r.spd01;
                tail.t.localRotation = tq0 * Quaternion.AngleAxis(idle - 14f * r.wind + 38f * r.lunge, Vector3.right);
            });
        }

        // ------------------------------------------------------------------ golem batu
        static void Golem(MkCtx c)
        {
            bool magma = c.V("magma"), obs = c.V("obsidian");
            var col = c.col; Color g = col[MS.Body];
            if (magma)                                                  // batu basal hitam, cahaya hanya di retakan
            {
                Color basalt = Mix(g, new Color(0.06f, 0.045f, 0.045f), 0.8f);
                col[MS.Body] = basalt; col[MS.Dark] = Dk(basalt, 0.55f); col[MS.Pale] = Mix(basalt, g, 0.3f);
            }
            else
            {
                col[MS.Dark] = obs ? new Color(0.07f, 0.06f, 0.1f) : Dk(g, 0.6f);
                col[MS.Pale] = obs ? new Color(0.24f, 0.19f, 0.32f) : Mix(g, new Color(1f, 0.97f, 0.88f), 0.32f);
            }
            col[MS.Eye] = magma ? new Color(1f, 0.85f, 0.35f) : (obs ? new Color(0.85f, 0.5f, 1f) : new Color(0.3f, 1f, 0.95f));
            col[MS.Ember] = magma ? new Color(1f, 0.42f, 0.08f) : (obs ? new Color(0.75f, 0.4f, 1f) : new Color(0.3f, 1f, 0.95f));
            col[MS.Accent] = magma ? new Color(1f, 0.55f, 0.1f) : (obs ? new Color(0.7f, 0.4f, 0.95f) : new Color(0.5f, 0.6f, 0.3f));
            var s = new BpSpec { H = 1.9f, W = 2.2f, headK = 1.2f, legK = 0.7f, armK = 1.5f, lean = 9f, neckFwd = 0.12f, shoulderK = 1.35f, waistK = 1.1f, limbK = 1.55f, stride = 22f, kneeBend = 16f, armSwing = 20f, weaponArm = false, bareFeet = true, armWindK = 2.3f };
            s.skin = MS.Dark; s.top = MS.Body; s.arm = MS.Body; s.fore = MS.Dark; s.bottom = MS.Dark; s.glove = MS.Dark; s.trim = MS.Dark;
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H;
                Rock(p.S, MS.Body, P3(0f, L.chestY + 0.02f * H, 0.04f), P3(0.9f, 0.7f, 0.64f), Quaternion.identity, 11, 0.14f, 5, 10);
                Rock(p.S, MS.Dark, P3(0f, L.waistY - 0.02f * H, 0f), P3(0.62f, 0.42f, 0.5f), Quaternion.identity, 12, 0.12f, 4, 8);
                Rock(p.S, MS.Body, P3(0f, L.shY - 0.08f * H, -0.36f), P3(0.8f, 0.55f, 0.42f), Quaternion.Euler(-12f, 0f, 0f), 13, 0.14f, 4, 9);
                for (int k = 0; k < 4; k++) { float x = (k - 1.5f) * 0.3f; Spike(p.S, MS.Pale, P3(x, L.shY, -0.5f), P3(x * 1.2f, L.shY + 0.4f - Mathf.Abs(x) * 0.2f, -0.74f), 0.1f, 5); }
                Ell(p.G, MS.Ember, P3(0f, L.chestY + 0.03f * H, 0.66f), P3(0.2f, 0.2f, 0.08f), 4, 9);                // inti bercahaya
                for (int q = -1; q <= 1; q += 2)
                {
                    Tube(p.G, MS.Ember, new[] { P3(0.14f * q, L.chestY + 0.06f * H, 0.68f), P3(0.3f * q, L.chestY + 0.22f * H, 0.62f), P3(0.34f * q, L.chestY + 0.4f * H, 0.5f) }, new[] { 0.03f, 0.024f, 0.012f }, 4, true, true);
                    Tube(p.G, MS.Ember, new[] { P3(0.12f * q, L.chestY - 0.04f * H, 0.68f), P3(0.26f * q, L.chestY - 0.2f * H, 0.64f), P3(0.2f * q, L.waistY, 0.54f) }, new[] { 0.026f, 0.02f, 0.012f }, 4, true, true);
                }
            };
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                Rock(p.S, MS.Body, hc, P3(r * 1.1f, r * 0.95f, r * 1.0f), Quaternion.identity, 21, 0.1f, 5, 10);
                Rock(p.S, MS.Dark, hc + P3(0f, r * 0.42f, r * 0.55f), P3(r * 1.0f, r * 0.2f, r * 0.5f), Quaternion.identity, 22, 0.08f, 3, 8);         // alis tebal
                Rock(p.S, MS.Dark, hc + P3(0f, -r * 0.6f, r * 0.45f), P3(r * 0.75f, r * 0.34f, r * 0.55f), Quaternion.identity, 23, 0.08f, 3, 8);     // rahang
                for (int q = -1; q <= 1; q += 2) Ell(p.G, MS.Eye, hc + P3(r * 0.42f * q, r * 0.12f, r * 0.92f), P3(r * 0.26f, r * 0.1f, r * 0.1f), Quaternion.Euler(0f, 0f, 12f * q), 3, 8);
                if (magma) for (int q = -1; q <= 1; q += 2) Horn(p.S, MS.Dark, hc + P3(r * 0.7f * q, r * 0.7f, -r * 0.1f), hc + P3(r * 1.3f * q, r * 1.8f, -r * 0.3f), P3(r * 0.3f * q, 0f, 0f), r * 0.28f);
                else if (obs) for (int k = -1; k <= 1; k++) Spike(p.S, MS.Accent, hc + P3(k * r * 0.5f, r * 0.8f, -r * 0.1f), hc + P3(k * r * 0.7f, r * (1.9f - Mathf.Abs(k) * 0.4f), -r * 0.3f), r * 0.22f, 5);
                else Ell(p.S, MS.Accent, hc + P3(r * 0.2f, r * 0.95f, -r * 0.1f), P3(r * 0.5f, r * 0.16f, r * 0.4f), 3, 8);                           // lumut
            };
            s.upperFn = delegate (MkPart p, BpLay L, int sd)
            {
                float sg = sd == 0 ? 1f : -1f; Vector3 sh = L.shoulder[sd];
                Rock(p.S, MS.Body, sh + P3(0.1f * sg, 0.08f, 0f), P3(0.62f, 0.5f, 0.56f), Quaternion.identity, 31 + sd, 0.14f, 4, 9);
                Spike(p.S, MS.Pale, sh + P3(0.2f * sg, 0.4f, 0f), sh + P3(0.4f * sg, 0.95f, -0.1f), 0.15f, 5);
                Rock(p.S, MS.Dark, L.elbow[sd], P3(0.34f, 0.3f, 0.34f), Quaternion.identity, 33 + sd, 0.12f, 3, 8);
                if (magma) Tube(p.G, MS.Ember, new[] { sh + P3(0.2f * sg, -0.1f, 0.3f), L.elbow[sd] + P3(0f, 0.05f, 0.32f) }, new[] { 0.03f, 0.025f }, 4, true, true);
            };
            s.foreFn = delegate (MkPart p, BpLay L, int sd)
            {
                Vector3 h = L.hand[sd];
                Rock(p.S, MS.Dark, h + P3(0f, -0.05f, 0.04f), P3(0.5f, 0.46f, 0.5f), Quaternion.identity, 35 + sd, 0.13f, 4, 9);
                for (int k = -1; k <= 1; k++) Spike(p.S, MS.Pale, h + P3(k * 0.2f, -0.2f, 0.34f), h + P3(k * 0.22f, -0.2f, 0.58f), 0.07f, 4);
            };
            s.thighFn = delegate (MkPart p, BpLay L, int sd)
            {
                float sg = sd == 0 ? 1f : -1f;
                Rock(p.S, MS.Body, L.hip[sd] + P3(0f, -0.1f, 0f), P3(0.5f, 0.42f, 0.46f), Quaternion.identity, 41 + sd, 0.12f, 3, 8);
                Rock(p.S, MS.Dark, L.knee[sd] + P3(0f, 0f, 0.1f), P3(0.38f, 0.34f, 0.36f), Quaternion.identity, 43 + sd, 0.12f, 3, 8);
                if (magma) Tube(p.G, MS.Ember, new[] { L.hip[sd] + P3(0.12f * sg, -0.05f, 0.44f), L.hip[sd] + P3(0.04f * sg, -0.3f, 0.47f), L.knee[sd] + P3(0.1f * sg, 0.06f, 0.4f) }, new[] { 0.032f, 0.026f, 0.016f }, 4, true, true);
            };
            s.shinFn = delegate (MkPart p, BpLay L, int sd)
            {
                Rock(p.S, MS.Dark, L.ankle[sd] + P3(0f, -0.08f, 0.18f), P3(0.46f, 0.2f, 0.58f), Quaternion.identity, 45 + sd, 0.1f, 3, 8);
            };
            Biped(c, s);
        }
    }
}
