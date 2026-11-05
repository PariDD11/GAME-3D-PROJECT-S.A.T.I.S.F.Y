using System;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Berkaki empat lainnya: babi hutan, banteng, Cerberus, ternak (domba, kambing, sapi), kadal.</summary>
    public static partial class MonsterKit
    {
        // bulu tengkuk/punggung: bilah pipih berjajar dari a ke z
        static void Mane(GeoBag b, int slot, Vector3 a, Vector3 z, int n, float h, float w, float lean, int seed)
        {
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : i / (float)(n - 1);
                Vector3 bp = Vector3.Lerp(a, z, t);
                float hh = h * (0.75f + 0.5f * Hash(i, 3, seed)) * (1f - 0.25f * t);
                Ear(b, slot, bp, bp + new Vector3(0f, hh, -lean * (0.6f + 0.4f * Hash(i, 5, seed))), w, 0.03f, Vector3.right);
            }
        }

        // ------------------------------------------------------------------ babi hutan
        static void BoarTorso(MkPart p, bool pollen)
        {
            Vector3[] bp = { P3(0f, 1.08f, 0.95f), P3(0f, 1.12f, 0.4f), P3(0f, 1.04f, -0.35f), P3(0f, 0.98f, -0.95f) };
            float[] brx = { 0.5f, 0.62f, 0.58f, 0.48f }, bry = { 0.5f, 0.56f, 0.52f, 0.46f };
            TubeBands(p.S, new[] { MS.Dark, MS.Body, MS.Pale, MS.Body }, new[] { 50f, 130f, 230f, 310f, 410f }, bp, brx, bry, 5, Vector3.up, true, true);
            Ell(p.S, MS.Dark, P3(0f, 1.42f, 0.55f), P3(0.44f, 0.3f, 0.48f), 5, 10);                   // punuk bahu
            Ell(p.S, MS.Body, P3(0f, 1.05f, -0.8f), P3(0.5f, 0.48f, 0.42f), 5, 10);                   // pantat
            Mane(p.S, MS.Dark, P3(0f, 1.58f, 0.95f), P3(0f, 1.72f, 0.45f), 4, 0.3f, 0.07f, 0.16f, 5);
            Mane(p.S, MS.Dark, P3(0f, 1.7f, 0.3f), P3(0f, 1.44f, -0.95f), 8, 0.3f, 0.07f, 0.16f, 6);
            if (pollen)
                for (int i = 0; i < 7; i++)                                                           // serbuk sari & kuncup di punggung
                {
                    float z = 0.7f - i * 0.28f;
                    Ell(p.S, i % 2 == 0 ? MS.Accent : MS.Pale, P3((i % 3 - 1) * 0.16f, 1.58f - i * 0.02f, z), P3(0.09f, 0.08f, 0.09f), 3, 7);
                }
        }

        static void BoarHead(MkPart p)
        {
            Tube(p.S, MS.Body, new[] { P3(0f, 1.18f, 0.75f), P3(0f, 1.2f, 1.15f), P3(0f, 1.13f, 1.5f) }, new[] { 0.42f, 0.38f, 0.33f }, new[] { 0.44f, 0.4f, 0.34f }, 9, Vector3.up, true, true);
            Ell(p.S, MS.Body, P3(0f, 1.12f, 1.55f), P3(0.38f, 0.38f, 0.42f), 6, 12);
            TubeBands(p.S, new[] { MS.Body, MS.Pale }, new[] { -25f, 205f, 335f }, new[] { P3(0f, 1.1f, 1.8f), P3(0f, 1.02f, 2.1f), P3(0f, 0.96f, 2.35f) },
                      new[] { 0.27f, 0.23f, 0.2f }, new[] { 0.24f, 0.2f, 0.17f }, 5, Vector3.up, true, true);
            Ell(p.S, MS.Claw, P3(0f, 0.95f, 2.4f), P3(0.19f, 0.15f, 0.07f), 4, 9);                    // moncong
            for (int q = -1; q <= 1; q += 2)
            {
                Ell(p.S, MS.Pupil, P3(0.07f * q, 0.97f, 2.45f), P3(0.03f, 0.03f, 0.02f), 3, 6);       // lubang hidung
                Horn(p.S, MS.Bone, P3(0.16f * q, 0.9f, 2.0f), P3(0.36f * q, 1.3f, 2.22f), P3(0.1f * q, 0.04f, 0.2f), 0.055f);   // taring
                Ear(p.S, MS.Body, P3(0.28f * q, 1.42f, 1.42f), P3(0.5f * q, 1.64f, 1.25f), 0.12f, 0.04f, Vector3.forward);
                Ear(p.S, MS.Dark, P3(0.29f * q, 1.43f, 1.46f), P3(0.45f * q, 1.6f, 1.33f), 0.07f, 0.02f, Vector3.forward);
            }
            Tube(p.S, MS.Dark, new[] { P3(-0.3f, 1.36f, 1.8f), P3(0f, 1.4f, 1.9f), P3(0.3f, 1.36f, 1.8f) }, new[] { 0.05f, 0.05f, 0.05f }, 5, true, true);   // alis garang
            EyePair(p, P3(0.2f, 1.27f, 1.86f), 0.05f, P3(0.5f, 0f, 1f), 0.5f, 0.6f);
        }

        static void BoarJaw(MkPart p)
        {
            Tube(p.S, MS.Pale, new[] { P3(0f, 0.92f, 1.8f), P3(0f, 0.86f, 2.05f), P3(0f, 0.84f, 2.3f) }, new[] { 0.19f, 0.15f, 0.12f }, new[] { 0.09f, 0.07f, 0.06f }, 7, Vector3.up, true, true);
            Tube(p.S, MS.Mouth, new[] { P3(0f, 0.96f, 1.9f), P3(0f, 0.93f, 2.25f) }, new[] { 0.1f, 0.07f }, new[] { 0.015f, 0.012f }, 5, Vector3.up, true, true);
        }

        static void Boar(MkCtx c)
        {
            bool pollen = c.V("pollen");
            var col = c.col; Color g = col[MS.Body];
            col[MS.Dark] = Dk(g, pollen ? 0.68f : 0.5f); col[MS.Pale] = Mix(g, new Color(0.95f, 0.88f, 0.75f), pollen ? 0.55f : 0.35f);
            col[MS.Bone] = new Color(0.96f, 0.93f, 0.82f); col[MS.Eye] = new Color(1f, 0.35f, 0.15f); col[MS.Claw] = new Color(0.14f, 0.1f, 0.09f);
            col[MS.Accent] = new Color(1f, 0.88f, 0.3f);
            var torso = c.New("torso", null, P3(0f, 1.05f, 0.05f), delegate (MkPart p) { BoarTorso(p, pollen); });
            var head = c.New("head", torso, P3(0f, 1.2f, 1.0f), delegate (MkPart p) { BoarHead(p); });
            var jaw = c.New("jaw", head, P3(0f, 0.92f, 1.8f), delegate (MkPart p) { BoarJaw(p); });
            var tail = c.New("tail", torso, P3(0f, 1.25f, -1.0f), delegate (MkPart p)
            {
                Tube(p.S, MS.Dark, new[] { P3(0f, 1.25f, -1.0f), P3(0f, 1.45f, -1.28f), P3(0.07f, 1.6f, -1.2f), P3(0.1f, 1.55f, -1.1f) }, new[] { 0.06f, 0.045f, 0.035f, 0.02f }, 6, true, true);
                Ell(p.S, MS.Dark, P3(0.1f, 1.55f, -1.1f), P3(0.05f, 0.07f, 0.05f), 3, 6);
            });
            Vector3 sh = P3(0.36f, 0.95f, 0.72f), el = P3(0.36f, 0.58f, 0.68f), wr = P3(0.36f, 0.26f, 0.74f), fp = P3(0.36f, 0.07f, 0.76f);
            Vector3 hb = P3(0.34f, 0.95f, -0.64f), kn = P3(0.34f, 0.55f, -0.5f), hk = P3(0.33f, 0.28f, -0.74f), bpw = P3(0.33f, 0.07f, -0.7f);
            QLegs(c, torso, "legFL", 1f, sh, el, wr, fp, 0.2f, 0.13f, 0.1f, MS.Body, MS.Claw, 0f, 30f, 24f, false, 0.12f, true);
            QLegs(c, torso, "legFR", -1f, sh, el, wr, fp, 0.2f, 0.13f, 0.1f, MS.Body, MS.Claw, Mathf.PI, 30f, 24f, false, 0.12f, true);
            QLegs(c, torso, "legBL", 1f, hb, kn, hk, bpw, 0.22f, 0.13f, 0.095f, MS.Body, MS.Claw, Mathf.PI, 27f, 38f, true, 0.12f, true);
            QLegs(c, torso, "legBR", -1f, hb, kn, hk, bpw, 0.22f, 0.13f, 0.095f, MS.Body, MS.Claw, 0f, 27f, 38f, true, 0.12f, true);
            Vector3 tp0 = torso.t.localPosition; Quaternion tq0 = torso.t.localRotation, hq0 = head.t.localRotation, jq0 = jaw.t.localRotation, tlq0 = tail.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                float bob = Mathf.Abs(Mathf.Sin(r.phase * 0.5f)) * 0.05f * r.spd01;
                torso.t.localPosition = tp0 + new Vector3(0f, bob - 0.1f * r.wind, 0.4f * r.lunge - 0.14f * r.wind);
                torso.t.localRotation = tq0 * Quaternion.AngleAxis(8f * r.wind - 6f * r.lunge, Vector3.right);
                head.t.localRotation = hq0 * Quaternion.AngleAxis(14f * r.wind + 18f * r.lunge + Mathf.Sin(r.phase) * 3f * r.spd01, Vector3.right);
                jaw.t.localRotation = jq0 * Quaternion.AngleAxis(6f * r.wind + 28f * r.lunge, Vector3.right);
                tail.t.localRotation = tlq0 * Quaternion.AngleAxis(Mathf.Sin(r.time * 3f) * 10f, Vector3.up);
            });
        }

        // ------------------------------------------------------------------ banteng raksasa
        static void BullTorso(MkPart p)
        {
            Vector3[] bp = { P3(0f, 1.45f, 1.0f), P3(0f, 1.5f, 0.4f), P3(0f, 1.35f, -0.5f), P3(0f, 1.3f, -1.2f) };
            float[] brx = { 0.7f, 0.82f, 0.74f, 0.6f }, bry = { 0.78f, 0.9f, 0.76f, 0.66f };
            TubeBands(p.S, new[] { MS.Dark, MS.Body, MS.Pale, MS.Body }, new[] { 50f, 130f, 230f, 310f, 410f }, bp, brx, bry, 5, Vector3.up, true, true);
            Ell(p.S, MS.Dark, P3(0f, 2.1f, 0.65f), P3(0.58f, 0.44f, 0.62f), 5, 11);                   // punuk
            Ell(p.S, MS.Body, P3(0f, 1.35f, -1.0f), P3(0.62f, 0.62f, 0.5f), 5, 10);                   // pantat
            Ell(p.S, MS.Body, P3(0f, 1.4f, 0.95f), P3(0.72f, 0.78f, 0.56f), 5, 11);                   // dada tebal
            Ell(p.S, MS.Dark, P3(0f, 0.98f, 1.15f), P3(0.3f, 0.36f, 0.4f), 4, 9);                     // gelambir
            Mane(p.S, MS.Dark, P3(0f, 2.3f, 1.0f), P3(0f, 2.0f, -0.4f), 9, 0.3f, 0.08f, 0.18f, 9);
        }

        static void BullHead(MkPart p)
        {
            Tube(p.S, MS.Body, new[] { P3(0f, 1.75f, 0.85f), P3(0f, 1.72f, 1.3f), P3(0f, 1.55f, 1.68f) }, new[] { 0.76f, 0.68f, 0.58f }, new[] { 0.74f, 0.66f, 0.57f }, 10, Vector3.up, true, true);
            Ell(p.S, MS.Body, P3(0f, 1.42f, 1.82f), P3(0.56f, 0.58f, 0.64f), 6, 12);
            TubeBands(p.S, new[] { MS.Pale, MS.Pale }, new[] { 0f, 180f, 360f }, new[] { P3(0f, 1.3f, 2.1f), P3(0f, 1.22f, 2.3f), P3(0f, 1.17f, 2.44f) },
                      new[] { 0.3f, 0.27f, 0.22f }, new[] { 0.26f, 0.23f, 0.19f }, 5, Vector3.up, true, false);
            Ell(p.S, MS.Pale, P3(0f, 1.16f, 2.44f), P3(0.22f, 0.19f, 0.13f), 4, 10);                  // ujung moncong membulat
            Ell(p.S, MS.Mouth, P3(0f, 1.15f, 2.55f), P3(0.19f, 0.13f, 0.05f), 4, 10);                 // cermin hidung
            Ell(p.S, MS.Dark, P3(0f, 1.62f, 1.55f), P3(0.34f, 0.2f, 0.34f), 4, 9);                    // jambul dahi
            for (int q = -1; q <= 1; q += 2)
            {
                Ell(p.S, MS.Pupil, P3(0.09f * q, 1.14f, 2.58f), P3(0.04f, 0.045f, 0.02f), 3, 6);
                // tanduk bulan sabit: keluar dulu, lalu melengkung naik
                Tube(p.S, MS.Bone, new[] { P3(0.44f * q, 1.72f, 1.6f), P3(0.8f * q, 1.76f, 1.56f), P3(1.06f * q, 1.98f, 1.68f), P3(1.1f * q, 2.36f, 1.98f) }, new[] { 0.16f, 0.13f, 0.09f, 0.012f }, 8, false, true);
                Ell(p.S, MS.Dark, P3(0.48f * q, 1.72f, 1.6f), P3(0.19f, 0.19f, 0.14f), 3, 8);
                Ear(p.S, MS.Dark, P3(0.5f * q, 1.6f, 1.5f), P3(0.95f * q, 1.52f, 1.36f), 0.14f, 0.045f, Vector3.up);
            }
            Tube(p.S, MS.Dark, new[] { P3(-0.42f, 1.7f, 2.0f), P3(0f, 1.76f, 2.12f), P3(0.42f, 1.7f, 2.0f) }, new[] { 0.07f, 0.07f, 0.07f }, 5, true, true);   // alis berat
            EyePair(p, P3(0.3f, 1.6f, 2.04f), 0.065f, P3(0.5f, 0f, 1f), 0.5f, 0.6f);
        }

        static void BullJaw(MkPart p)
        {
            Tube(p.S, MS.Pale, new[] { P3(0f, 1.2f, 2.0f), P3(0f, 1.1f, 2.3f), P3(0f, 1.06f, 2.5f) }, new[] { 0.24f, 0.2f, 0.17f }, new[] { 0.1f, 0.08f, 0.07f }, 7, Vector3.up, true, true);
        }

        static void Bull(MkCtx c)
        {
            var col = c.col; Color g = col[MS.Body];
            col[MS.Dark] = Dk(g, 0.6f); col[MS.Pale] = Mix(g, new Color(0.85f, 0.75f, 0.6f), 0.42f); col[MS.Bone] = new Color(0.95f, 0.92f, 0.8f);
            col[MS.Eye] = new Color(1f, 0.3f, 0.12f); col[MS.Claw] = new Color(0.13f, 0.1f, 0.09f); col[MS.Mouth] = new Color(0.72f, 0.4f, 0.4f);
            var torso = c.New("torso", null, P3(0f, 1.45f, 0.1f), delegate (MkPart p) { BullTorso(p); });
            var head = c.New("head", torso, P3(0f, 1.75f, 1.0f), delegate (MkPart p) { BullHead(p); });
            head.t.localScale = Vector3.one * 1.12f;
            var jaw = c.New("jaw", head, P3(0f, 1.2f, 2.0f), delegate (MkPart p) { BullJaw(p); });
            var tail = c.New("tail", torso, P3(0f, 1.5f, -1.25f), delegate (MkPart p)
            {
                Tube(p.S, MS.Body, new[] { P3(0f, 1.5f, -1.25f), P3(0f, 1.0f, -1.5f), P3(0f, 0.5f, -1.5f) }, new[] { 0.08f, 0.06f, 0.05f }, 6, true, true);
                Ell(p.S, MS.Dark, P3(0f, 0.36f, -1.5f), P3(0.1f, 0.22f, 0.1f), 4, 8);
            });
            Vector3 sh = P3(0.5f, 1.25f, 0.8f), el = P3(0.5f, 0.72f, 0.8f), wr = P3(0.5f, 0.34f, 0.84f), fp = P3(0.5f, 0.1f, 0.86f);
            Vector3 hb = P3(0.48f, 1.2f, -0.9f), kn = P3(0.48f, 0.7f, -0.75f), hk = P3(0.46f, 0.36f, -1.0f), bpw = P3(0.46f, 0.1f, -0.95f);
            QLegs(c, torso, "legFL", 1f, sh, el, wr, fp, 0.28f, 0.18f, 0.15f, MS.Body, MS.Claw, 0f, 28f, 22f, false, 0.16f, true);
            QLegs(c, torso, "legFR", -1f, sh, el, wr, fp, 0.28f, 0.18f, 0.15f, MS.Body, MS.Claw, Mathf.PI, 28f, 22f, false, 0.16f, true);
            QLegs(c, torso, "legBL", 1f, hb, kn, hk, bpw, 0.3f, 0.18f, 0.14f, MS.Body, MS.Claw, Mathf.PI, 25f, 34f, true, 0.16f, true);
            QLegs(c, torso, "legBR", -1f, hb, kn, hk, bpw, 0.3f, 0.18f, 0.14f, MS.Body, MS.Claw, 0f, 25f, 34f, true, 0.16f, true);
            Vector3 tp0 = torso.t.localPosition; Quaternion tq0 = torso.t.localRotation, hq0 = head.t.localRotation, jq0 = jaw.t.localRotation, tlq0 = tail.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                float bob = Mathf.Abs(Mathf.Sin(r.phase * 0.5f)) * 0.06f * r.spd01;
                torso.t.localPosition = tp0 + new Vector3(0f, bob - 0.14f * r.wind, 0.5f * r.lunge - 0.2f * r.wind);
                torso.t.localRotation = tq0 * Quaternion.AngleAxis(7f * r.wind - 5f * r.lunge, Vector3.right);
                head.t.localRotation = hq0 * Quaternion.AngleAxis(16f * r.wind + 20f * r.lunge + Mathf.Sin(r.phase) * 2f * r.spd01, Vector3.right);
                jaw.t.localRotation = jq0 * Quaternion.AngleAxis(4f * r.wind + 22f * r.lunge, Vector3.right);
                tail.t.localRotation = tlq0 * Quaternion.AngleAxis(Mathf.Sin(r.time * 2.5f) * 9f, Vector3.up);
            });
        }

        // ------------------------------------------------------------------ Cerberus (tiga kepala)
        static void CerbHead(MkPart p, Vector3 o)
        {
            Tube(p.S, MS.Body, new[] { o + P3(0f, -0.05f, -0.1f), o + P3(0f, 0.2f, 0.25f), o + P3(0f, 0.4f, 0.5f) }, new[] { 0.24f, 0.21f, 0.19f }, 8, true, true);
            Ell(p.S, MS.Body, o + P3(0f, 0.5f, 0.68f), P3(0.24f, 0.22f, 0.27f), 5, 11);
            TubeBands(p.S, new[] { MS.Body, MS.Pale }, new[] { -25f, 205f, 335f }, new[] { o + P3(0f, 0.46f, 0.86f), o + P3(0f, 0.4f, 1.05f), o + P3(0f, 0.36f, 1.25f) },
                      new[] { 0.15f, 0.12f, 0.085f }, new[] { 0.13f, 0.105f, 0.075f }, 5, Vector3.up, true, true);
            Ell(p.S, MS.Claw, o + P3(0f, 0.37f, 1.28f), P3(0.06f, 0.045f, 0.045f), 3, 7);
            for (int q = -1; q <= 1; q += 2)
            {
                Ear(p.S, MS.Dark, o + P3(0.13f * q, 0.66f, 0.6f), o + P3(0.2f * q, 0.98f, 0.5f), 0.09f, 0.035f, Vector3.forward);
                Spike(p.S, MS.Bone, o + P3(0.045f * q, 0.34f, 1.18f), o + P3(0.05f * q, 0.24f, 1.2f), 0.02f, 4);
                Horn(p.S, MS.Dark, o + P3(0.1f * q, 0.68f, 0.62f), o + P3(0.2f * q, 0.95f, 0.38f), P3(0f, 0.06f, 0.1f), 0.04f);
            }
            Tube(p.S, MS.Dark, new[] { o + P3(-0.17f, 0.66f, 0.84f), o + P3(0f, 0.68f, 0.94f), o + P3(0.17f, 0.66f, 0.84f) }, new[] { 0.025f, 0.025f, 0.025f }, 5, true, true);
            EyePair(p, o + P3(0.12f, 0.58f, 0.9f), 0.05f, P3(0.4f, 0f, 1f));
        }

        static void CerbJaw(MkPart p, Vector3 o)
        {
            Tube(p.S, MS.Pale, new[] { o + P3(0f, 0.34f, 0.85f), o + P3(0f, 0.3f, 1.05f), o + P3(0f, 0.29f, 1.2f) }, new[] { 0.1f, 0.075f, 0.05f }, new[] { 0.055f, 0.045f, 0.035f }, 7, Vector3.up, true, true);
            Tube(p.G, MS.Ember, new[] { o + P3(0f, 0.38f, 0.92f), o + P3(0f, 0.355f, 1.15f) }, new[] { 0.06f, 0.035f }, new[] { 0.012f, 0.01f }, 5, Vector3.up, true, true);
            for (int q = -1; q <= 1; q += 2) Spike(p.S, MS.Bone, o + P3(0.05f * q, 0.31f, 1.12f), o + P3(0.055f * q, 0.4f, 1.1f), 0.02f, 4);
        }

        static void Cerberus(MkCtx c)
        {
            var col = c.col; Color g = col[MS.Body];
            col[MS.Dark] = Dk(g, 0.45f); col[MS.Pale] = Mix(g, new Color(0.55f, 0.25f, 0.2f), 0.55f); col[MS.Bone] = new Color(0.93f, 0.9f, 0.8f);
            col[MS.Eye] = new Color(1f, 0.82f, 0.25f); col[MS.Ember] = new Color(1f, 0.38f, 0.1f); col[MS.Claw] = new Color(0.12f, 0.09f, 0.09f);
            var torso = c.New("torso", null, P3(0f, 1.15f, 0.05f), delegate (MkPart p)
            {
                Vector3[] bp = { P3(0f, 1.2f, 0.9f), P3(0f, 1.2f, 0.3f), P3(0f, 1.12f, -0.4f), P3(0f, 1.15f, -0.9f) };
                float[] brx = { 0.5f, 0.55f, 0.5f, 0.44f }, bry = { 0.58f, 0.6f, 0.5f, 0.5f };
                TubeBands(p.S, new[] { MS.Dark, MS.Body, MS.Pale, MS.Body }, new[] { 50f, 130f, 230f, 310f, 410f }, bp, brx, bry, 5, Vector3.up, true, true);
                Ell(p.S, MS.Body, P3(0f, 1.2f, 0.75f), P3(0.5f, 0.55f, 0.4f), 5, 10);              // dada
                for (int i = 0; i < 9; i++)                                                          // surai api di punggung
                {
                    float z = 0.85f - i * 0.2f, y = 1.78f - i * 0.04f, h = 0.3f - i * 0.012f;
                    Ear(p.S, MS.Dark, P3(0f, y - 0.05f, z), P3(0f, y + h, z - 0.18f), 0.07f, 0.03f, Vector3.right);
                    Spike(p.G, MS.Ember, P3(0f, y, z + 0.02f), P3(0f, y + h * 1.7f, z - 0.12f), 0.045f, 4);
                }
            });
            Vector3[] hp = { P3(0f, 1.3f, 0.85f), P3(0.4f, 1.22f, 0.78f), P3(-0.4f, 1.22f, 0.78f) };
            float[] hyaw = { 0f, 28f, -28f }, hsc = { 1.2f, 1.05f, 1.05f };
            var heads = new MkPart[3]; var jaws = new MkPart[3]; var hq0 = new Quaternion[3]; var jq0 = new Quaternion[3];
            for (int i = 0; i < 3; i++)
            {
                Vector3 o = hp[i];
                heads[i] = c.New("head" + i, torso, o, delegate (MkPart p) { CerbHead(p, o); });
                jaws[i] = c.New("jaw" + i, heads[i], o + P3(0f, 0.36f, 0.9f), delegate (MkPart p) { CerbJaw(p, o); });
                heads[i].t.localRotation = Quaternion.Euler(0f, hyaw[i], 0f); heads[i].t.localScale = Vector3.one * hsc[i];
                hq0[i] = heads[i].t.localRotation; jq0[i] = jaws[i].t.localRotation;
            }
            var tail = c.New("tail", torso, P3(0f, 1.25f, -0.88f), delegate (MkPart p)
            {
                Tube(p.S, MS.Body, new[] { P3(0f, 1.25f, -0.88f), P3(0f, 1.3f, -1.4f), P3(0f, 1.15f, -1.95f) }, new[] { 0.1f, 0.12f, 0.06f }, 8, true, true);
                for (int k = 0; k < 4; k++) Spike(p.G, MS.Ember, P3(0f, 1.3f - k * 0.04f, -1.35f - k * 0.18f), P3(0f, 1.5f - k * 0.04f, -1.5f - k * 0.2f), 0.04f, 4);
            });
            Vector3 sh = P3(0.34f, 1.0f, 0.65f), el = P3(0.34f, 0.6f, 0.58f), wr = P3(0.34f, 0.25f, 0.66f), fp = P3(0.34f, 0.07f, 0.68f);
            Vector3 hb = P3(0.34f, 1.0f, -0.62f), kn = P3(0.34f, 0.58f, -0.4f), hk = P3(0.33f, 0.3f, -0.76f), bpw = P3(0.33f, 0.07f, -0.7f);
            QLegs(c, torso, "legFL", 1f, sh, el, wr, fp, 0.2f, 0.13f, 0.1f, MS.Body, MS.Dark, 0f, 32f, 30f, false, 0.16f);
            QLegs(c, torso, "legFR", -1f, sh, el, wr, fp, 0.2f, 0.13f, 0.1f, MS.Body, MS.Dark, Mathf.PI, 32f, 30f, false, 0.16f);
            QLegs(c, torso, "legBL", 1f, hb, kn, hk, bpw, 0.22f, 0.12f, 0.095f, MS.Body, MS.Dark, Mathf.PI, 29f, 42f, true, 0.16f);
            QLegs(c, torso, "legBR", -1f, hb, kn, hk, bpw, 0.22f, 0.12f, 0.095f, MS.Body, MS.Dark, 0f, 29f, 42f, true, 0.16f);
            Vector3 tp0 = torso.t.localPosition; Quaternion tq0 = torso.t.localRotation, tlq0 = tail.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                float bob = Mathf.Abs(Mathf.Sin(r.phase * 0.5f)) * 0.05f * r.spd01;
                torso.t.localPosition = tp0 + new Vector3(0f, bob - 0.1f * r.wind, 0.4f * r.lunge - 0.12f * r.wind);
                torso.t.localRotation = tq0 * Quaternion.AngleAxis(8f * r.wind - 7f * r.lunge, Vector3.right);
                tail.t.localRotation = tlq0 * Quaternion.AngleAxis(Mathf.Sin(r.time * 2.4f) * 9f, Vector3.up);
                for (int h = 0; h < 3; h++)
                {
                    float ph = h * 2.1f;
                    heads[h].t.localRotation = hq0[h] * Quaternion.AngleAxis(-10f * r.wind + 18f * r.lunge + Mathf.Sin(r.time * 1.4f + ph) * 4f + Mathf.Sin(r.phase + ph) * 3f * r.spd01, Vector3.right) * Quaternion.AngleAxis(Mathf.Sin(r.time * 0.9f + ph) * 5f, Vector3.up);
                    jaws[h].t.localRotation = jq0[h] * Quaternion.AngleAxis(8f * r.wind + 36f * r.lunge + (1f + Mathf.Sin(r.time * 2.6f + ph)) * 2f, Vector3.right);
                }
            });
        }

        // ------------------------------------------------------------------ domba
        static void SheepBody(MkPart p)
        {
            Ell(p.S, MS.Body, P3(0f, 0.68f, 0f), P3(0.4f, 0.36f, 0.5f), 5, 11);
            for (int i = 0; i < 20; i++)                                                              // gumpalan wol
            {
                float a = i * 2.399f, y = 0.5f + 0.34f * Hash(i, 1, 7), rr = 0.18f + 0.08f * Hash(i, 2, 7);
                Ell(p.S, i % 4 == 0 ? MS.Pale : MS.Body, P3(Mathf.Sin(a) * 0.3f, y, Mathf.Cos(a) * 0.42f - 0.02f), P3(rr, rr, rr), 4, 9);
            }
            Ell(p.S, MS.Pale, P3(0f, 0.66f, -0.5f), P3(0.17f, 0.16f, 0.15f), 4, 8);                  // ekor wol
            Ell(p.S, MS.Pale, P3(0f, 0.97f, 0.38f), P3(0.2f, 0.13f, 0.17f), 4, 8);                   // jambul
        }

        static void SheepHead(MkPart p)
        {
            Tube(p.S, MS.Dark, new[] { P3(0f, 0.74f, 0.4f), P3(0f, 0.8f, 0.56f) }, new[] { 0.1f, 0.09f }, 6, true, true);
            Ell(p.S, MS.Dark, P3(0f, 0.78f, 0.68f), P3(0.14f, 0.16f, 0.2f), 5, 10);
            Ell(p.S, MS.Dark, P3(0f, 0.7f, 0.82f), P3(0.095f, 0.09f, 0.11f), 4, 8);
            Ell(p.S, MS.Claw, P3(0f, 0.68f, 0.92f), P3(0.04f, 0.03f, 0.03f), 3, 6);
            for (int q = -1; q <= 1; q += 2)
            {
                Ear(p.S, MS.Dark, P3(0.12f * q, 0.85f, 0.62f), P3(0.34f * q, 0.78f, 0.58f), 0.07f, 0.025f, Vector3.up);
                Ell(p.S, MS.Bone, P3(0.095f * q, 0.83f, 0.78f), P3(0.04f, 0.045f, 0.03f), 3, 6);
                Ell(p.S, MS.Pupil, P3(0.1f * q, 0.83f, 0.805f), P3(0.02f, 0.03f, 0.015f), 3, 6);
            }
        }

        static void Sheep(MkCtx c)
        {
            var col = c.col; Color g = col[MS.Body];
            col[MS.Pale] = Mix(g, Color.white, 0.55f); col[MS.Dark] = new Color(0.24f, 0.21f, 0.2f); col[MS.Claw] = new Color(0.12f, 0.1f, 0.1f);
            col[MS.Bone] = new Color(0.95f, 0.93f, 0.85f); col[MS.Pupil] = new Color(0.05f, 0.04f, 0.04f);
            var torso = c.New("torso", null, P3(0f, 0.68f, 0f), delegate (MkPart p) { SheepBody(p); });
            var head = c.New("head", torso, P3(0f, 0.76f, 0.42f), delegate (MkPart p) { SheepHead(p); });
            Vector3 sh = P3(0.2f, 0.45f, 0.3f), el = P3(0.2f, 0.28f, 0.3f), wr = P3(0.2f, 0.14f, 0.32f), fp = P3(0.2f, 0.04f, 0.33f);
            Vector3 hb = P3(0.2f, 0.45f, -0.3f), kn = P3(0.2f, 0.28f, -0.25f), hk = P3(0.2f, 0.14f, -0.34f), bpw = P3(0.2f, 0.04f, -0.32f);
            QLegs(c, torso, "legFL", 1f, sh, el, wr, fp, 0.075f, 0.05f, 0.04f, MS.Dark, MS.Claw, 0f, 28f, 18f, false, 0.05f, true);
            QLegs(c, torso, "legFR", -1f, sh, el, wr, fp, 0.075f, 0.05f, 0.04f, MS.Dark, MS.Claw, Mathf.PI, 28f, 18f, false, 0.05f, true);
            QLegs(c, torso, "legBL", 1f, hb, kn, hk, bpw, 0.08f, 0.05f, 0.04f, MS.Dark, MS.Claw, Mathf.PI, 26f, 24f, false, 0.05f, true);
            QLegs(c, torso, "legBR", -1f, hb, kn, hk, bpw, 0.08f, 0.05f, 0.04f, MS.Dark, MS.Claw, 0f, 26f, 24f, false, 0.05f, true);
            Vector3 tp0 = torso.t.localPosition; Quaternion hq0 = head.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                torso.t.localPosition = tp0 + new Vector3(0f, Mathf.Abs(Mathf.Sin(r.phase * 0.5f)) * 0.03f * r.spd01 - 0.03f * r.wind, 0.1f * r.lunge);
                float graze = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(r.time * 0.7f)), 6f) * (1f - r.spd01);
                head.t.localRotation = hq0 * Quaternion.AngleAxis(graze * 38f + 14f * r.lunge + Mathf.Sin(r.phase) * 3f * r.spd01, Vector3.right);
            });
        }

        // ------------------------------------------------------------------ kambing
        static void GoatBody(MkPart p)
        {
            Vector3[] bp = { P3(0f, 0.84f, 0.42f), P3(0f, 0.82f, 0f), P3(0f, 0.78f, -0.45f) };
            TubeBands(p.S, new[] { MS.Body, MS.Body, MS.Pale, MS.Body }, new[] { 50f, 130f, 230f, 310f, 410f }, bp, new[] { 0.2f, 0.23f, 0.2f }, new[] { 0.24f, 0.25f, 0.22f }, 5, Vector3.up, true, true);
            Mane(p.S, MS.Dark, P3(0f, 1.05f, 0.4f), P3(0f, 1.0f, -0.4f), 7, 0.1f, 0.04f, 0.05f, 4);
            Tube(p.S, MS.Body, new[] { P3(0f, 0.78f, -0.45f), P3(0f, 0.95f, -0.58f) }, new[] { 0.05f, 0.03f }, 5, true, true);   // ekor pendek tegak
        }

        static void GoatHead(MkPart p)
        {
            Tube(p.S, MS.Body, new[] { P3(0f, 0.86f, 0.4f), P3(0f, 1.08f, 0.58f), P3(0f, 1.2f, 0.7f) }, new[] { 0.13f, 0.1f, 0.085f }, 7, true, true);
            Ell(p.S, MS.Body, P3(0f, 1.22f, 0.76f), P3(0.09f, 0.11f, 0.14f), 5, 9);
            Ell(p.S, MS.Pale, P3(0f, 1.14f, 0.9f), P3(0.065f, 0.07f, 0.1f), 4, 8);
            Ell(p.S, MS.Claw, P3(0f, 1.15f, 0.99f), P3(0.035f, 0.025f, 0.025f), 3, 6);
            Spike(p.S, MS.Pale, P3(0f, 1.07f, 0.88f), P3(0f, 0.86f, 0.84f), 0.04f, 5);                                              // jenggot
            for (int q = -1; q <= 1; q += 2)
            {
                Horn(p.S, MS.Bone, P3(0.05f * q, 1.3f, 0.7f), P3(0.11f * q, 1.55f, 0.42f), P3(0.04f * q, 0.1f, 0.03f), 0.038f);
                Ear(p.S, MS.Body, P3(0.09f * q, 1.22f, 0.7f), P3(0.27f * q, 1.2f, 0.62f), 0.05f, 0.02f, Vector3.up);
                Ell(p.S, MS.Bone, P3(0.075f * q, 1.25f, 0.84f), P3(0.03f, 0.03f, 0.025f), 3, 6);
                Ell(p.S, MS.Pupil, P3(0.08f * q, 1.25f, 0.86f), P3(0.025f, 0.012f, 0.012f), 3, 6);                           // pupil mendatar
            }
        }

        static void Goat(MkCtx c)
        {
            var col = c.col; Color g = col[MS.Body];
            col[MS.Pale] = Mix(g, new Color(0.96f, 0.92f, 0.84f), 0.6f); col[MS.Dark] = Dk(g, 0.55f); col[MS.Claw] = new Color(0.14f, 0.12f, 0.11f);
            col[MS.Bone] = new Color(0.9f, 0.86f, 0.74f); col[MS.Pupil] = new Color(0.05f, 0.04f, 0.04f);
            var torso = c.New("torso", null, P3(0f, 0.8f, 0f), delegate (MkPart p) { GoatBody(p); });
            var head = c.New("head", torso, P3(0f, 0.9f, 0.45f), delegate (MkPart p) { GoatHead(p); });
            Vector3 sh = P3(0.15f, 0.62f, 0.32f), el = P3(0.15f, 0.36f, 0.3f), wr = P3(0.15f, 0.17f, 0.32f), fp = P3(0.15f, 0.04f, 0.33f);
            Vector3 hb = P3(0.15f, 0.62f, -0.34f), kn = P3(0.15f, 0.38f, -0.26f), hk = P3(0.15f, 0.18f, -0.4f), bpw = P3(0.15f, 0.04f, -0.37f);
            QLegs(c, torso, "legFL", 1f, sh, el, wr, fp, 0.075f, 0.05f, 0.04f, MS.Body, MS.Claw, 0f, 32f, 20f, false, 0.05f, true);
            QLegs(c, torso, "legFR", -1f, sh, el, wr, fp, 0.075f, 0.05f, 0.04f, MS.Body, MS.Claw, Mathf.PI, 32f, 20f, false, 0.05f, true);
            QLegs(c, torso, "legBL", 1f, hb, kn, hk, bpw, 0.085f, 0.05f, 0.04f, MS.Body, MS.Claw, Mathf.PI, 30f, 30f, true, 0.05f, true);
            QLegs(c, torso, "legBR", -1f, hb, kn, hk, bpw, 0.085f, 0.05f, 0.04f, MS.Body, MS.Claw, 0f, 30f, 30f, true, 0.05f, true);
            Vector3 tp0 = torso.t.localPosition; Quaternion hq0 = head.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                torso.t.localPosition = tp0 + new Vector3(0f, Mathf.Abs(Mathf.Sin(r.phase * 0.5f)) * 0.035f * r.spd01 - 0.03f * r.wind, 0.1f * r.lunge);
                head.t.localRotation = hq0 * Quaternion.AngleAxis(Mathf.Sin(r.time * 0.9f) * 6f + 16f * r.lunge + Mathf.Sin(r.phase) * 4f * r.spd01, Vector3.right);
            });
        }

        // ------------------------------------------------------------------ sapi
        static void CowBody(MkPart p)
        {
            Vector3[] bp = { P3(0f, 1.02f, 0.7f), P3(0f, 1.06f, 0.1f), P3(0f, 1.0f, -0.7f) };
            TubeBands(p.S, new[] { MS.Body, MS.Body, MS.Pale, MS.Body }, new[] { 50f, 130f, 230f, 310f, 410f }, bp, new[] { 0.4f, 0.47f, 0.42f }, new[] { 0.45f, 0.5f, 0.45f }, 6, Vector3.up, true, true);
            Ell(p.S, MS.Dark, P3(0.4f, 1.12f, 0.25f), P3(0.07f, 0.26f, 0.3f), Quaternion.Euler(0f, 0f, -10f), 3, 8);                  // bercak
            Ell(p.S, MS.Dark, P3(-0.38f, 1.0f, -0.35f), P3(0.07f, 0.22f, 0.26f), Quaternion.Euler(0f, 0f, 10f), 3, 8);
            Ell(p.S, MS.Dark, P3(0.1f, 1.5f, -0.2f), P3(0.24f, 0.05f, 0.28f), 3, 8);
            Ell(p.S, MS.Mouth, P3(0f, 0.66f, -0.5f), P3(0.14f, 0.1f, 0.15f), 4, 8);                                                    // ambing
            Tube(p.S, MS.Body, new[] { P3(0f, 1.1f, -0.9f), P3(0f, 0.8f, -1.0f), P3(0f, 0.4f, -0.98f) }, new[] { 0.04f, 0.035f, 0.03f }, 5, true, true);
            Ell(p.S, MS.Dark, P3(0f, 0.32f, -0.98f), P3(0.06f, 0.14f, 0.06f), 3, 7);
        }

        static void CowHead(MkPart p)
        {
            Tube(p.S, MS.Body, new[] { P3(0f, 1.15f, 0.75f), P3(0f, 1.15f, 1.0f), P3(0f, 1.05f, 1.2f) }, new[] { 0.26f, 0.23f, 0.2f }, 7, true, true);
            Ell(p.S, MS.Body, P3(0f, 1.06f, 1.2f), P3(0.2f, 0.22f, 0.3f), 5, 10);
            Ell(p.S, MS.Mouth, P3(0f, 0.96f, 1.42f), P3(0.16f, 0.12f, 0.12f), 4, 9);
            Ell(p.S, MS.Claw, P3(0f, 0.97f, 1.52f), P3(0.1f, 0.05f, 0.025f), 3, 7);
            Ell(p.S, MS.Dark, P3(-0.1f, 1.2f, 1.22f), P3(0.1f, 0.12f, 0.12f), 3, 7);                                                   // bercak mata
            for (int q = -1; q <= 1; q += 2)
            {
                Horn(p.S, MS.Bone, P3(0.14f * q, 1.26f, 1.1f), P3(0.26f * q, 1.46f, 1.12f), P3(0.04f * q, 0.06f, 0f), 0.04f);
                Ear(p.S, MS.Body, P3(0.2f * q, 1.14f, 1.05f), P3(0.42f * q, 1.08f, 0.98f), 0.09f, 0.03f, Vector3.up);
                Ell(p.S, MS.Bone, P3(0.12f * q, 1.14f, 1.38f), P3(0.04f, 0.045f, 0.03f), 3, 6);
                Ell(p.S, MS.Pupil, P3(0.125f * q, 1.14f, 1.405f), P3(0.025f, 0.032f, 0.015f), 3, 6);
            }
        }

        static void Cow(MkCtx c)
        {
            var col = c.col; Color g = col[MS.Body];
            col[MS.Dark] = new Color(0.2f, 0.17f, 0.15f); col[MS.Pale] = Mix(g, new Color(0.9f, 0.8f, 0.7f), 0.5f); col[MS.Mouth] = new Color(0.93f, 0.72f, 0.7f);
            col[MS.Bone] = new Color(0.95f, 0.92f, 0.84f); col[MS.Claw] = new Color(0.26f, 0.22f, 0.2f); col[MS.Pupil] = new Color(0.05f, 0.04f, 0.04f);
            var torso = c.New("torso", null, P3(0f, 1.0f, 0f), delegate (MkPart p) { CowBody(p); });
            var head = c.New("head", torso, P3(0f, 1.15f, 0.8f), delegate (MkPart p) { CowHead(p); });
            head.t.localScale = Vector3.one * 1.22f;
            Vector3 sh = P3(0.26f, 0.8f, 0.55f), el = P3(0.26f, 0.48f, 0.52f), wr = P3(0.26f, 0.2f, 0.55f), fp = P3(0.26f, 0.05f, 0.56f);
            Vector3 hb = P3(0.27f, 0.8f, -0.58f), kn = P3(0.27f, 0.5f, -0.45f), hk = P3(0.26f, 0.22f, -0.62f), bpw = P3(0.26f, 0.05f, -0.58f);
            QLegs(c, torso, "legFL", 1f, sh, el, wr, fp, 0.13f, 0.085f, 0.07f, MS.Body, MS.Claw, 0f, 26f, 18f, false, 0.08f, true);
            QLegs(c, torso, "legFR", -1f, sh, el, wr, fp, 0.13f, 0.085f, 0.07f, MS.Body, MS.Claw, Mathf.PI, 26f, 18f, false, 0.08f, true);
            QLegs(c, torso, "legBL", 1f, hb, kn, hk, bpw, 0.15f, 0.085f, 0.07f, MS.Body, MS.Claw, Mathf.PI, 24f, 26f, true, 0.08f, true);
            QLegs(c, torso, "legBR", -1f, hb, kn, hk, bpw, 0.15f, 0.085f, 0.07f, MS.Body, MS.Claw, 0f, 24f, 26f, true, 0.08f, true);
            Vector3 tp0 = torso.t.localPosition; Quaternion hq0 = head.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                torso.t.localPosition = tp0 + new Vector3(0f, Mathf.Abs(Mathf.Sin(r.phase * 0.5f)) * 0.03f * r.spd01 - 0.03f * r.wind, 0.1f * r.lunge);
                float graze = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(r.time * 0.55f)), 6f) * (1f - r.spd01);
                head.t.localRotation = hq0 * Quaternion.AngleAxis(graze * 32f + 14f * r.lunge + Mathf.Sin(r.phase) * 3f * r.spd01, Vector3.right);
            });
        }

        // ------------------------------------------------------------------ kadal (pasir, lava, Ignarok)
        static void LizTorso(MkPart p, bool lava, bool boss)
        {
            Vector3[] bp = { P3(0f, 0.56f, 0.72f), P3(0f, 0.57f, 0.1f), P3(0f, 0.53f, -0.55f) };
            TubeBands(p.S, new[] { MS.Dark, MS.Body, MS.Pale, MS.Body }, new[] { 50f, 130f, 230f, 310f, 410f }, bp, new[] { 0.3f, 0.35f, 0.3f }, new[] { 0.22f, 0.25f, 0.21f }, 5, Vector3.up, true, true);
            Mane(p.S, MS.Dark, P3(0f, 0.78f, 0.7f), P3(0f, 0.74f, -0.5f), boss ? 9 : 7, boss ? 0.2f : 0.13f, 0.05f, 0.05f, 12);
            if (lava || boss)
            {
                for (int i = 0; i < 7; i++) Spike(p.G, MS.Ember, P3(0f, 0.8f, 0.6f - i * 0.17f), P3(0f, 0.8f + (boss ? 0.26f : 0.17f), 0.55f - i * 0.17f), 0.035f, 4);
                for (int q = -1; q <= 1; q += 2)
                    Tube(p.G, MS.Ember, new[] { P3(0.15f * q, 0.76f, 0.5f), P3(0.24f * q, 0.7f, 0.1f), P3(0.17f * q, 0.72f, -0.4f) }, new[] { 0.022f, 0.02f, 0.015f }, 4, true, true);
            }
            if (boss) for (int q = -1; q <= 1; q += 2) for (int k = 0; k < 3; k++) Spike(p.S, MS.Dark, P3(0.26f * q, 0.66f, 0.45f - k * 0.4f), P3(0.45f * q, 0.9f, 0.4f - k * 0.4f), 0.07f, 5);
        }

        static void LizHead(MkPart p, bool lava, bool boss)
        {
            Tube(p.S, MS.Body, new[] { P3(0f, 0.58f, 0.6f), P3(0f, 0.63f, 0.9f), P3(0f, 0.66f, 1.08f) }, new[] { 0.22f, 0.19f, 0.17f }, new[] { 0.18f, 0.16f, 0.15f }, 8, Vector3.up, true, true);
            Ell(p.S, MS.Body, P3(0f, 0.67f, 1.2f), P3(0.2f, 0.15f, 0.26f), 5, 10);
            TubeBands(p.S, new[] { MS.Body, MS.Pale }, new[] { -25f, 205f, 335f }, new[] { P3(0f, 0.66f, 1.35f), P3(0f, 0.62f, 1.55f), P3(0f, 0.6f, 1.74f) }, new[] { 0.13f, 0.105f, 0.08f }, new[] { 0.1f, 0.085f, 0.065f }, 5, Vector3.up, true, true);
            Ell(p.S, MS.Pupil, P3(0f, 0.62f, 1.77f), P3(0.045f, 0.03f, 0.03f), 3, 6);
            for (int q = -1; q <= 1; q += 2)
            {
                Ell(p.S, MS.Body, P3(0.14f * q, 0.76f, 1.2f), P3(0.07f, 0.07f, 0.08f), 3, 8);                                          // tonjolan mata
                Spike(p.S, MS.Dark, P3(0.18f * q, 0.7f, 0.98f), P3(0.34f * q, 0.8f, 0.8f), 0.05f, 5);                                 // jumbai leher
                Spike(p.S, MS.Dark, P3(0.17f * q, 0.66f, 1.0f), P3(0.32f * q, 0.62f, 0.78f), 0.045f, 5);
                if (boss) Horn(p.S, MS.Dark, P3(0.12f * q, 0.8f, 1.1f), P3(0.3f * q, 1.12f, 0.74f), P3(0.06f * q, 0.08f, 0.04f), 0.07f);
            }
            if (boss)
            {
                for (int k = 0; k < 4; k++) Spike(p.S, MS.Dark, P3(0f, 0.8f, 1.0f - k * 0.1f), P3(0f, 1.02f - k * 0.03f, 0.84f - k * 0.1f), 0.05f, 5);
                for (int k = -3; k <= 3; k++)                                                                                          // kipas leher menyala
                {
                    Vector3 fb = P3(k * 0.045f, 0.76f, 1.0f), ft = P3(k * 0.2f, 1.28f - Mathf.Abs(k) * 0.07f, 0.72f);
                    Ear(p.S, MS.Dark, fb, ft, 0.07f, 0.025f, Vector3.forward);
                    Spike(p.G, MS.Ember, ft, ft + (ft - fb).normalized * 0.12f, 0.03f, 4);
                }
            }
            EyePair(p, P3(0.14f, 0.77f, 1.24f), 0.045f, P3(0.4f, 0.1f, 1f));
        }

        static void LizJaw(MkPart p, bool lava, bool boss)
        {
            Tube(p.S, MS.Pale, new[] { P3(0f, 0.6f, 1.2f), P3(0f, 0.55f, 1.45f), P3(0f, 0.54f, 1.7f) }, new[] { 0.11f, 0.085f, 0.06f }, new[] { 0.06f, 0.05f, 0.04f }, 7, Vector3.up, true, true);
            Tube(p.G, MS.Ember, new[] { P3(0f, 0.63f, 1.3f), P3(0f, 0.6f, 1.62f) }, new[] { 0.07f, 0.04f }, new[] { 0.012f, 0.01f }, 5, Vector3.up, true, true);
            if (lava || boss) for (int q = -1; q <= 1; q += 2) Spike(p.S, MS.Bone, P3(0.05f * q, 0.57f, 1.62f), P3(0.055f * q, 0.66f, 1.6f), 0.02f, 4);
        }

        static void Lizard(MkCtx c)
        {
            bool lava = c.V("lava"), boss = c.V("boss");
            var col = c.col; Color g = col[MS.Body];
            if (lava || boss)
            {
                Color ash = Mix(g, new Color(0.08f, 0.06f, 0.06f), boss ? 0.55f : 0.7f);
                col[MS.Body] = ash; col[MS.Dark] = Dk(ash, 0.5f); col[MS.Pale] = Mix(ash, g, 0.45f);
                col[MS.Eye] = new Color(1f, 0.9f, 0.3f); col[MS.Ember] = new Color(1f, 0.42f, 0.08f);
            }
            else { col[MS.Dark] = Dk(g, 0.62f); col[MS.Pale] = Mix(g, new Color(1f, 0.95f, 0.8f), 0.5f); col[MS.Eye] = new Color(1f, 0.82f, 0.25f); }
            col[MS.Bone] = new Color(0.93f, 0.9f, 0.8f); col[MS.Claw] = new Color(0.16f, 0.13f, 0.11f); col[MS.Pupil] = new Color(0.05f, 0.04f, 0.04f);
            var torso = c.New("torso", null, P3(0f, 0.55f, 0.1f), delegate (MkPart p) { LizTorso(p, lava, boss); });
            var head = c.New("head", torso, P3(0f, 0.62f, 0.8f), delegate (MkPart p) { LizHead(p, lava, boss); });
            var jaw = c.New("jaw", head, P3(0f, 0.6f, 1.2f), delegate (MkPart p) { LizJaw(p, lava, boss); });
            var tail = c.New("tail", torso, P3(0f, 0.53f, -0.55f), delegate (MkPart p)
            {
                Tube(p.S, MS.Body, new[] { P3(0f, 0.53f, -0.55f), P3(0f, 0.45f, -1.2f), P3(0f, 0.36f, -1.85f), P3(0.1f, 0.28f, -2.5f) }, new[] { 0.22f, 0.16f, 0.1f, 0.02f }, new[] { 0.17f, 0.13f, 0.08f, 0.02f }, 8, Vector3.up, true, true);
                Mane(p.S, MS.Dark, P3(0f, 0.7f, -0.6f), P3(0.03f, 0.4f, -2.2f), 8, 0.11f, 0.045f, 0.05f, 14);
                if (boss) { Ell(p.S, MS.Dark, P3(0.1f, 0.3f, -2.45f), P3(0.16f, 0.14f, 0.2f), 4, 8); for (int k = 0; k < 4; k++) Spike(p.S, MS.Bone, P3(0.1f, 0.3f, -2.45f), P3(0.1f + (k - 1.5f) * 0.14f, 0.3f + (k % 2 == 0 ? 0.16f : -0.12f), -2.55f), 0.05f, 4); }
                if (lava) for (int k = 0; k < 5; k++) Spike(p.G, MS.Ember, P3(0.0f, 0.62f - k * 0.05f, -0.8f - k * 0.3f), P3(0f, 0.78f - k * 0.05f, -0.86f - k * 0.3f), 0.03f, 4);
            });
            Vector3 sh = P3(0.3f, 0.5f, 0.55f), el = P3(0.62f, 0.34f, 0.48f), wr = P3(0.55f, 0.12f, 0.7f), fp = P3(0.56f, 0.04f, 0.78f);
            Vector3 hb = P3(0.3f, 0.5f, -0.4f), kn = P3(0.66f, 0.36f, -0.5f), hk = P3(0.6f, 0.12f, -0.3f), bpw = P3(0.62f, 0.04f, -0.22f);
            QLegs(c, torso, "legFL", 1f, sh, el, wr, fp, 0.1f, 0.075f, 0.055f, MS.Body, MS.Dark, 0f, 30f, 30f, false, 0.1f);
            QLegs(c, torso, "legFR", -1f, sh, el, wr, fp, 0.1f, 0.075f, 0.055f, MS.Body, MS.Dark, Mathf.PI, 30f, 30f, false, 0.1f);
            QLegs(c, torso, "legBL", 1f, hb, kn, hk, bpw, 0.12f, 0.08f, 0.055f, MS.Body, MS.Dark, Mathf.PI, 27f, 34f, true, 0.1f);
            QLegs(c, torso, "legBR", -1f, hb, kn, hk, bpw, 0.12f, 0.08f, 0.055f, MS.Body, MS.Dark, 0f, 27f, 34f, true, 0.1f);
            Vector3 tp0 = torso.t.localPosition; Quaternion tq0 = torso.t.localRotation, hq0 = head.t.localRotation, jq0 = jaw.t.localRotation, tlq0 = tail.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                float wig = Mathf.Sin(r.phase) * 5f * r.spd01;
                torso.t.localPosition = tp0 + new Vector3(0f, Mathf.Abs(Mathf.Sin(r.phase)) * 0.025f * r.spd01 - 0.06f * r.wind, 0.35f * r.lunge - 0.1f * r.wind);
                torso.t.localRotation = tq0 * Quaternion.AngleAxis(wig, Vector3.up) * Quaternion.AngleAxis(6f * r.wind - 6f * r.lunge, Vector3.right);
                head.t.localRotation = hq0 * Quaternion.AngleAxis(-wig * 0.8f - 8f * r.wind + 16f * r.lunge, Vector3.up) * Quaternion.AngleAxis(-10f * r.wind + 12f * r.lunge, Vector3.right);
                jaw.t.localRotation = jq0 * Quaternion.AngleAxis(6f * r.wind + 34f * r.lunge + (1f + Mathf.Sin(r.time * 2.8f)) * 1.5f, Vector3.right);
                tail.t.localRotation = tlq0 * Quaternion.AngleAxis(-wig * 2.2f + Mathf.Sin(r.time * 1.6f) * 6f, Vector3.up);
            });
        }
    }
}
