using System;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Berjubah / melayang: penyihir & pemuja, nekromancer & iblis, hantu.</summary>
    public static partial class MonsterKit
    {
        // benda melayang mengitari sumbu tegak di titik centre (tiap benda = satu bagian yang berputar)
        static void Orbit(MkCtx c, Vector3 centre, float rad, float h, int n, float spd, Action<MkPart, Vector3> build)
        {
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n;
                Vector3 pos = centre + new Vector3(Mathf.Sin(a0) * rad, h + Mathf.Sin(i * 2.1f) * 0.12f, Mathf.Cos(a0) * rad);
                Vector3 piv = centre + new Vector3(0f, h, 0f);
                var part = c.New("orb" + i, null, piv, delegate (MkPart p) { build(p, pos); });
                var pt = part.t;
                c.rig.Add(delegate (MonsterRig r) { pt.localRotation = Quaternion.AngleAxis(r.time * spd * Mathf.Rad2Deg, Vector3.up); });
            }
        }

        // jumbai kain di tepi jubah: bilah pipih menjuntai mengelilingi cincin elips (rx, rz) setinggi y
        static void Tatters(GeoBag b, int slot, float rx, float rz, float y, int n, float len, float w, float phase)
        {
            for (int k = 0; k < n; k++)
            {
                float a = phase + k * Mathf.PI * 2f / n, sa = Mathf.Sin(a), ca = Mathf.Cos(a);
                float ln = len * (0.72f + 0.28f * ((k * 7) % 3) * 0.5f);
                Vector3 bp = new Vector3(sa * rx * 0.97f, y, ca * rz * 0.97f), tp = new Vector3(sa * rx * 1.04f, y - ln, ca * rz * 1.04f);
                Ear(b, slot, bp, tp, w, 0.012f, new Vector3(sa, 0f, ca), 3);
            }
        }

        static void SmallSkull(MkPart p, Vector3 pos, float r)
        {
            Ell(p.S, MS.Bone, pos, new Vector3(r, r * 1.05f, r * 1.05f), 4, 9);
            Ell(p.S, MS.Bone, pos + new Vector3(0f, -r * 0.62f, r * 0.3f), new Vector3(r * 0.6f, r * 0.4f, r * 0.55f), 3, 8);
            for (int q = -1; q <= 1; q += 2) Ell(p.S, MS.Pupil, pos + new Vector3(r * 0.38f * q, r * 0.1f, r * 0.82f), new Vector3(r * 0.26f, r * 0.24f, r * 0.14f), 3, 7);
            EyePair(p, pos + new Vector3(r * 0.38f, r * 0.1f, r * 0.88f), r * 0.14f, Vector3.forward, 0.2f, 0.6f);
        }

        static void WitchHat(MkPart p, Vector3 hc, float r, int slot, int band, float bend)
        {
            Ell(p.S, slot, hc + new Vector3(0f, r * 0.9f, 0f), new Vector3(r * 2.0f, r * 0.1f, r * 2.0f), 3, 14);
            Tube(p.S, slot, new[] { hc + new Vector3(0f, r * 0.95f, 0f), hc + new Vector3(0f, r * 1.9f, -r * 0.08f), hc + new Vector3(r * 0.1f * bend, r * 2.9f, -r * 0.3f), hc + new Vector3(r * 0.45f * bend, r * 3.35f, -r * 0.75f) },
                 new[] { r * 0.95f, r * 0.72f, r * 0.4f, r * 0.03f }, new[] { r * 0.95f, r * 0.72f, r * 0.4f, r * 0.03f }, 10, Vector3.forward, false, true);
            Tube(p.S, band, new[] { hc + new Vector3(0f, r * 0.98f, 0f), hc + new Vector3(0f, r * 1.25f, 0f) }, new[] { r * 1.0f, r * 0.94f }, new[] { r * 1.0f, r * 0.94f }, 12, Vector3.forward, false, false);
            Ell(p.S, MS.Accent, hc + new Vector3(0f, r * 1.12f, r * 0.98f), new Vector3(r * 0.2f, r * 0.2f, r * 0.05f), 3, 8);
        }

        // api / bola sihir di tangan
        static void Flame(MkPart p, Vector3 h, int slot, float s)
        {
            Ell(p.G, slot, h + new Vector3(0f, 0.12f * s, 0.05f), new Vector3(0.06f * s, 0.11f * s, 0.06f * s), 4, 8);
            Ell(p.G, slot, h + new Vector3(0.02f * s, 0.26f * s, 0.05f), new Vector3(0.04f * s, 0.12f * s, 0.04f * s), 3, 7);
            Ell(p.G, slot, h + new Vector3(-0.02f * s, 0.38f * s, 0.05f), new Vector3(0.022f * s, 0.08f * s, 0.022f * s), 3, 6);
        }

        static void Staff(MkPart p, Vector3 g, float top, int shaft, bool skull, int orbSlot, float orbR)
        {
            Tube(p.S, shaft, new[] { new Vector3(g.x, 0f, g.z), new Vector3(g.x, top * 0.5f, g.z), new Vector3(g.x, top, g.z) }, new[] { 0.04f, 0.036f, 0.04f }, new[] { 0.04f, 0.036f, 0.04f }, 6, Vector3.forward, true, true);
            Vector3 o = new Vector3(g.x, top + orbR * 0.8f, g.z);
            Ell(p.G, orbSlot, o, new Vector3(orbR, orbR * 1.15f, orbR), 5, 10);
            for (int k = 0; k < 4; k++)
            {
                float a = k * Mathf.PI * 0.5f + 0.4f; Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Tube(p.S, shaft, new[] { new Vector3(g.x, top - 0.04f, g.z), o + d * orbR * 1.5f - new Vector3(0f, orbR * 0.2f, 0f), o + d * orbR * 0.9f + new Vector3(0f, orbR * 1.5f, 0f) }, new[] { 0.028f, 0.02f, 0.008f }, new[] { 0.028f, 0.02f, 0.008f }, 4, Vector3.up, false, true);
            }
            if (skull) SmallSkull(p, new Vector3(g.x, top - 0.12f, g.z), 0.1f);
        }

        // lengan baju lebar menjuntai
        static void BellSleeve(MkPart p, BpLay L, int sd, int slot)
        {
            Vector3 e = L.elbow[sd], w = L.wrist[sd];
            Tube(p.S, slot, new[] { e, Vector3.Lerp(e, w, 0.55f), w + new Vector3(0f, -0.03f, 0.01f) }, new[] { 0.07f, 0.1f, 0.15f }, new[] { 0.07f, 0.1f, 0.15f }, 8, Vector3.forward, false, false);
        }

        // ------------------------------------------------------------------ penyihir & pemuja
        static void Witch(MkCtx c)
        {
            bool fire = c.V("fire"), dark = c.V("dark"), ash = c.V("ash"), cultist = fire || dark || ash, hag = c.V("hag"), abyss = c.V("abyss");
            var col = c.col; Color robe = col[MS.Body];
            col[MS.ClothA] = robe; col[MS.ClothB] = Dk(robe, 0.7f);
            col[MS.Skin] = hag ? new Color(0.55f, 0.66f, 0.42f) : (abyss ? new Color(0.74f, 0.68f, 0.82f) : new Color(0.68f, 0.76f, 0.56f));
            col[MS.Eye] = fire ? new Color(1f, 0.6f, 0.2f) : (ash ? new Color(1f, 0.45f, 0.25f) : (dark || abyss ? new Color(0.85f, 0.45f, 1f) : new Color(0.4f, 1f, 0.55f)));
            col[MS.Ember] = col[MS.Eye]; col[MS.Accent] = abyss ? new Color(0.7f, 0.5f, 0.95f) : new Color(0.92f, 0.74f, 0.26f);
            col[MS.Wood] = new Color(0.32f, 0.22f, 0.13f); col[MS.Hair] = hag ? new Color(0.75f, 0.75f, 0.7f) : new Color(0.14f, 0.1f, 0.14f);
            var s = new BpSpec { H = abyss ? 1.14f : 1.0f, W = 0.95f, headK = 1.05f, robe = 1, robeFlare = abyss ? 1.2f : 1.05f, lean = hag ? 16f : 3f, neckFwd = hag ? 0.1f : 0f, armSwing = 12f, hover = abyss ? 0.05f : 0f };
            s.skin = MS.Skin; s.top = MS.ClothA; s.arm = MS.ClothA; s.fore = MS.ClothA; s.bottom = MS.ClothB; s.trim = MS.Accent; s.glove = MS.Skin;
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                if (cultist || abyss)
                {
                    Ell(p.S, MS.Pupil, hc + new Vector3(0f, 0f, r * 0.12f), new Vector3(r * 0.86f, r * 0.96f, r * 0.9f), 5, 10);            // wajah gelap di balik tudung
                    if (abyss) { Ell(p.S, MS.Skin, hc + new Vector3(0f, -r * 0.55f, r * 0.35f), new Vector3(r * 0.5f, r * 0.4f, r * 0.45f), 4, 9); }
                    EyePair(p, hc + new Vector3(r * 0.34f, r * 0.12f, r * 0.82f), r * 0.17f, new Vector3(0.1f, 0f, 1f), 0.3f, 0.5f);
                    Hood(p, L, MS.ClothA, MS.ClothB, 1.2f);
                    Spike(p.S, MS.ClothA, hc + new Vector3(0f, r * 1.0f, -r * 0.6f), hc + new Vector3(0f, r * 1.45f, -r * 1.25f), r * 0.5f, 6);
                    if (abyss) for (int q = -1; q <= 1; q += 2) Horn(p.S, MS.Dark, hc + new Vector3(r * 0.6f * q, r * 0.85f, -r * 0.1f), hc + new Vector3(r * 1.15f * q, r * 2.0f, -r * 0.3f), new Vector3(r * 0.3f * q, 0f, 0f), r * 0.2f);
                    return;
                }
                HumanHead(p, L, MS.Skin, true, 1.0f);
                Tube(p.S, MS.Skin, new[] { hc + new Vector3(0f, -r * 0.05f, r * 0.95f), hc + new Vector3(0f, -r * 0.25f, r * 1.45f), hc + new Vector3(0f, -r * 0.55f, r * 1.55f) }, new[] { r * 0.13f, r * 0.1f, r * 0.07f }, new[] { r * 0.13f, r * 0.1f, r * 0.07f }, 6, Vector3.right, true, true);   // hidung panjang
                Ell(p.S, MS.Dark, hc + new Vector3(r * 0.04f, -r * 0.42f, r * 1.52f), new Vector3(r * 0.05f, r * 0.05f, r * 0.05f), 3, 6);                                                                                              // kutil
                for (int k = 0; k < 6; k++)
                {
                    float a = (k - 2.5f) * 0.32f; Vector3 b0 = hc + new Vector3(Mathf.Sin(a) * r * 0.95f, r * 0.2f, -r * 0.1f - Mathf.Abs(a) * r * 0.1f);
                    Tube(p.S, MS.Hair, new[] { b0, b0 + new Vector3(Mathf.Sin(a) * r * 0.2f, -r * 0.9f, -r * 0.4f), b0 + new Vector3(Mathf.Sin(a) * r * 0.4f, -r * 2.0f, -r * 0.5f) }, new[] { r * 0.1f, r * 0.08f, r * 0.02f }, new[] { r * 0.1f, r * 0.08f, r * 0.02f }, 4, Vector3.up, true, true);
                }
                WitchHat(p, hc, r, MS.ClothB, MS.Accent, hag ? -1.6f : 1f);
            };
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W;
                Tube(p.S, MS.Accent, new[] { new Vector3(0f, L.waistY - 0.03f * H, 0f), new Vector3(0f, L.waistY + 0.04f * H, 0f) }, new[] { 0.2f * W, 0.2f * W }, new[] { 0.15f * W, 0.15f * W }, 10, Vector3.forward, true, true);
                for (int q = -1; q <= 1; q += 2)
                {
                    Ell(p.S, MS.ClothB, new Vector3(L.sx * 0.95f * q, L.shY - 0.01f * H, 0f), new Vector3(0.14f * W, 0.1f * H, 0.13f * W), 4, 9);
                    if (cultist) Spike(p.S, MS.Accent, new Vector3(L.sx * 1.05f * q, L.shY + 0.05f * H, 0f), new Vector3(L.sx * 1.15f * q, L.shY + 0.25f * H, -0.03f), 0.04f, 5);
                }
                if (hag) { Ell(p.S, MS.Hair, new Vector3(0f, L.shY + 0.02f * H, -0.05f), new Vector3(0.34f * W, 0.1f * H, 0.24f * W), 4, 10); for (int k = 0; k < 5; k++) Spike(p.S, MS.Hair, new Vector3((k - 2) * 0.12f * W, L.shY, -0.15f), new Vector3((k - 2) * 0.14f * W, L.shY - 0.28f * H, -0.2f), 0.05f, 4); }
                if (cultist) { Tube(p.S, MS.Accent, new[] { new Vector3(0f, L.shY - 0.04f * H, 0.15f * W), new Vector3(0f, L.waistY + 0.05f * H, 0.19f * W) }, new[] { 0.035f, 0.035f }, new[] { 0.012f, 0.012f }, 4, Vector3.forward, true, true); }
                Ell(p.S, MS.Accent, new Vector3(0f, L.chestY, 0.17f * W), new Vector3(0.05f, 0.05f, 0.02f), 3, 7);
            };
            s.skirtFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W, fl = L.s.robeFlare;
                Tatters(p.S, MS.ClothB, 0.5f * W * fl, 0.42f * W * fl, 0.1f * H, 14, 0.13f * H, 0.085f * W * fl, 0.2f);
            };
            s.upperFn = delegate (MkPart p, BpLay L, int sd) { };
            s.foreFn = delegate (MkPart p, BpLay L, int sd)
            {
                BellSleeve(p, L, sd, MS.ClothA);
                Vector3 h = L.hand[sd]; float H = L.s.H;
                for (int k = -1; k <= 1; k++) Spike(p.S, MS.Claw, h + new Vector3(k * 0.02f, -0.05f, 0.03f), h + new Vector3(k * 0.025f, -0.12f, 0.05f), 0.01f, 4);
                if (sd == 0)
                {
                    if (fire) { Flame(p, h + new Vector3(0f, 0.02f, 0.04f), MS.Ember, 1.3f); }
                    else if (ash) Flame(p, h + new Vector3(0f, 0.02f, 0.04f), MS.Eye, 1.0f);
                    else if (dark) { Ell(p.G, MS.Ember, h + new Vector3(0f, 0.12f, 0.08f), new Vector3(0.1f, 0.1f, 0.1f), 4, 9); }
                    else Staff(p, h + new Vector3(0.0f, 0f, 0.04f), 2.15f * H, MS.Wood, hag, MS.Ember, hag ? 0.12f : 0.14f);
                }
                if (sd == 1 && hag)
                {
                    Tube(p.S, MS.Metal, new[] { h + new Vector3(0f, 0f, 0.03f), h + new Vector3(0f, -0.2f, 0.03f) }, new[] { 0.008f, 0.008f }, new[] { 0.008f, 0.008f }, 3, Vector3.right, false, false);
                    Ell(p.S, MS.Metal, h + new Vector3(0f, -0.3f, 0.03f), new Vector3(0.08f, 0.1f, 0.08f), 3, 8); Ell(p.G, MS.Ember, h + new Vector3(0f, -0.3f, 0.03f), new Vector3(0.06f, 0.08f, 0.06f), 3, 8);
                }
            };
            var L0 = Biped(c, s);
            if (abyss) Orbit(c, new Vector3(0f, 0f, 0f), 1.1f, 1.6f, 3, 1.5f, delegate (MkPart p, Vector3 pos) { Ell(p.G, MS.Ember, pos, new Vector3(0.12f, 0.12f, 0.12f), 4, 9); Ell(p.S, MS.Dark, pos, new Vector3(0.17f, 0.17f, 0.17f), 3, 8); });
        }

        // ------------------------------------------------------------------ nekromancer & iblis
        static void Necro(MkCtx c)
        {
            bool king = c.V("king"), demon = c.V("demon"), tower = c.V("tower"), echo = c.V("echo");
            var col = c.col; Color robe = col[MS.Body];
            col[MS.ClothA] = robe; col[MS.ClothB] = Dk(robe, 0.65f); col[MS.Skin] = col[MS.Bone]; col[MS.Metal] = new Color(0.5f, 0.5f, 0.54f);
            col[MS.Eye] = demon ? new Color(1f, 0.45f, 0.15f) : (echo ? new Color(0.85f, 0.5f, 1f) : (tower ? new Color(1f, 0.95f, 0.6f) : new Color(0.4f, 1f, 0.6f)));
            col[MS.Ember] = col[MS.Eye]; col[MS.Accent] = new Color(0.95f, 0.78f, 0.26f); col[MS.Wood] = new Color(0.25f, 0.18f, 0.12f);
            if (king) { col[MS.ClothA] = new Color(0.3f, 0.12f, 0.38f); col[MS.ClothB] = new Color(0.18f, 0.07f, 0.24f); }
            if (tower) { col[MS.Metal] = new Color(0.95f, 0.78f, 0.3f); col[MS.ClothB] = Dk(robe, 0.55f); }
            var s = new BpSpec { H = 1.38f, W = 1.05f, headK = 1.02f, robe = 1, robeFlare = 1.18f, armSwing = 10f, hover = echo ? 0.07f : 0.03f };
            s.skin = MS.Bone; s.top = MS.ClothA; s.arm = MS.ClothA; s.fore = MS.ClothA; s.bottom = MS.ClothB; s.trim = MS.Accent; s.glove = MS.Bone;
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                if (tower)
                {
                    Ell(p.S, MS.Metal, hc + new Vector3(0f, r * 0.05f, 0f), new Vector3(r * 1.08f, r * 1.12f, r * 1.1f), 6, 14);
                    Ell(p.S, MS.Metal, hc + new Vector3(0f, -r * 0.55f, r * 0.2f), new Vector3(r * 0.9f, r * 0.6f, r * 0.85f), 4, 12);
                    Tube(p.S, MS.Pupil, new[] { hc + new Vector3(-r * 0.8f, r * 0.1f, r * 0.8f), hc + new Vector3(0f, r * 0.06f, r * 1.12f), hc + new Vector3(r * 0.8f, r * 0.1f, r * 0.8f) }, new[] { r * 0.07f, r * 0.07f, r * 0.07f }, new[] { r * 0.14f, r * 0.14f, r * 0.14f }, 5, Vector3.forward, true, true);
                    EyePair(p, hc + new Vector3(r * 0.34f, r * 0.1f, r * 0.98f), r * 0.14f, new Vector3(0.1f, 0f, 1f), 0.4f, 0.5f);
                    Tube(p.S, MS.Accent, new[] { hc + new Vector3(0f, r * 1.0f, -r * 0.9f), hc + new Vector3(0f, r * 2.0f, -r * 0.6f), hc + new Vector3(0f, r * 1.3f, r * 0.7f) }, new[] { r * 0.09f, r * 0.09f, r * 0.09f }, new[] { r * 0.25f, r * 0.3f, r * 0.2f }, 5, Vector3.right, true, true);
                    for (int q = -1; q <= 1; q += 2) Horn(p.S, MS.Accent, hc + new Vector3(r * 1.0f * q, r * 0.3f, 0f), hc + new Vector3(r * 1.9f * q, r * 1.3f, -r * 0.1f), new Vector3(r * 0.3f * q, 0f, 0f), r * 0.18f);
                    return;
                }
                Ell(p.S, MS.Pupil, hc + new Vector3(0f, 0f, r * 0.1f), new Vector3(r * 0.9f, r * 1.0f, r * 0.95f), 5, 10);
                Ell(p.S, MS.Bone, hc + new Vector3(0f, -r * 0.05f, r * 0.55f), new Vector3(r * 0.7f, r * 0.8f, r * 0.55f), 5, 10);                     // wajah tengkorak di dalam tudung
                Ell(p.S, MS.Bone, hc + new Vector3(0f, -r * 0.62f, r * 0.62f), new Vector3(r * 0.46f, r * 0.3f, r * 0.4f), 4, 8);
                for (int q = -1; q <= 1; q += 2) Ell(p.S, MS.Pupil, hc + new Vector3(r * 0.3f * q, r * 0.1f, r * 0.98f), new Vector3(r * 0.2f, r * 0.2f, r * 0.1f), 3, 7);
                EyePair(p, hc + new Vector3(r * 0.3f, r * 0.1f, r * 1.03f), r * 0.13f, new Vector3(0.1f, 0f, 1f), 0.2f, 0.6f);
                for (int t = -2; t <= 2; t++) Ell(p.S, MS.Bone, hc + new Vector3(t * r * 0.12f, -r * 0.55f, r * 0.98f), new Vector3(r * 0.05f, r * 0.08f, r * 0.04f), 3, 5);
                Hood(p, L, MS.ClothA, MS.ClothB, 1.28f);
                Spike(p.S, MS.ClothA, hc + new Vector3(0f, r * 1.0f, -r * 0.6f), hc + new Vector3(0f, r * 1.5f, -r * 1.4f), r * 0.55f, 6);
                if (king) { Tube(p.S, MS.Accent, new[] { hc + new Vector3(-r * 0.9f, r * 0.62f, r * 0.1f), hc + new Vector3(0f, r * 0.7f, r * 0.95f), hc + new Vector3(r * 0.9f, r * 0.62f, r * 0.1f) }, new[] { r * 0.1f, r * 0.1f, r * 0.1f }, new[] { r * 0.09f, r * 0.09f, r * 0.09f }, 6, Vector3.up, true, true); for (int k = 0; k < 5; k++) { float a = (-80f + k * 40f) * Mathf.Deg2Rad; Vector3 bp = hc + new Vector3(Mathf.Sin(a) * r * 0.95f, r * 0.68f, Mathf.Cos(a) * r * 0.55f); Spike(p.S, MS.Accent, bp, bp + new Vector3(0f, r * (k % 2 == 0 ? 0.8f : 0.55f), 0f), r * 0.1f, 5); } }
                if (demon) for (int q = -1; q <= 1; q += 2) Horn(p.S, MS.Dark, hc + new Vector3(r * 0.7f * q, r * 0.7f, -r * 0.1f), hc + new Vector3(r * 1.5f * q, r * 2.1f, -r * 0.5f), new Vector3(r * 0.4f * q, r * 0.1f, 0f), r * 0.26f);
            };
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W;
                Tube(p.S, MS.Accent, new[] { new Vector3(0f, L.waistY - 0.03f * H, 0f), new Vector3(0f, L.waistY + 0.04f * H, 0f) }, new[] { 0.2f * W, 0.2f * W }, new[] { 0.15f * W, 0.15f * W }, 10, Vector3.forward, true, true);
                SmallSkull(p, new Vector3(0f, L.waistY, 0.2f * W), 0.07f);
                for (int q = -1; q <= 1; q += 2)
                {
                    int ps = tower ? MS.Metal : (king ? MS.Accent : MS.Dark);
                    Ell(p.S, ps, new Vector3(L.sx * 1.0f * q, L.shY + 0.0f, 0f), new Vector3(0.2f * W, 0.12f * H, 0.17f * W), 4, 10);
                    for (int k = 0; k < 3; k++) Spike(p.S, demon ? MS.Dark : MS.Bone, new Vector3(L.sx * (0.9f + k * 0.12f) * q, L.shY + 0.08f * H, (k - 1) * 0.07f), new Vector3(L.sx * (1.0f + k * 0.2f) * q, L.shY + (0.26f - k * 0.03f) * H, (k - 1) * 0.1f - 0.02f), 0.045f * W, 5);
                }
                for (int k = 0; k < 4; k++) Ell(p.S, tower ? MS.Metal : MS.Bone, new Vector3(0f, L.chestY - 0.1f * H + k * 0.07f * H, 0.15f * W), new Vector3((0.15f - k * 0.015f) * W, 0.02f * H, 0.05f), 3, 9);
                if (king) Tube(p.S, MS.Accent, new[] { new Vector3(-0.28f * W, L.shY - 0.02f * H, 0.05f), new Vector3(0f, L.shY - 0.15f * H, 0.2f * W), new Vector3(0.28f * W, L.shY - 0.02f * H, 0.05f) }, new[] { 0.05f, 0.05f, 0.05f }, new[] { 0.03f, 0.03f, 0.03f }, 6, Vector3.up, true, true);
                if (demon || echo) for (int k = 0; k < 4; k++) Ell(p.G, MS.Ember, new Vector3((k - 1.5f) * 0.07f * W, L.chestY + 0.06f * H, 0.17f * W), new Vector3(0.015f, 0.07f, 0.01f), 3, 6);
            };
            s.skirtFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W, fl = L.s.robeFlare;
                Tatters(p.S, MS.ClothB, 0.5f * W * fl, 0.42f * W * fl, 0.1f * H, 16, 0.13f * H, 0.1f * W * fl, 0.1f);
            };
            s.foreFn = delegate (MkPart p, BpLay L, int sd)
            {
                BellSleeve(p, L, sd, MS.ClothA);
                Vector3 h = L.hand[sd]; float H = L.s.H;
                for (int k = -1; k <= 1; k++) Spike(p.S, MS.Bone, h + new Vector3(k * 0.022f, -0.05f, 0.03f), h + new Vector3(k * 0.028f, -0.14f, 0.06f), 0.012f, 4);
                if (sd == 0) Staff(p, h + new Vector3(0f, 0f, 0.05f), 2.2f * H, MS.Wood, true, MS.Ember, tower ? 0.2f : 0.17f);
                if (sd == 1 && !king && !tower) Flame(p, h + new Vector3(0f, 0.02f, 0.05f), MS.Ember, 1.0f);
            };
            var L0 = Biped(c, s);
            if (king || tower || demon) Cape(c, L0, MS.ClothB, 1.35f * s.H, 1.15f);
            if (demon) Wings(c, L0, MS.Dark, MS.ClothB, 1.5f);
            if (!echo && !tower) Orbit(c, new Vector3(0f, 0f, 0f), 1.3f, 1.9f * s.H, 3, 1.3f, delegate (MkPart p, Vector3 pos) { SmallSkull(p, pos, 0.14f); });
            else Orbit(c, new Vector3(0f, 0f, 0f), 1.3f, 1.9f * s.H, 3, 1.3f, delegate (MkPart p, Vector3 pos) { Ell(p.G, MS.Ember, pos, new Vector3(0.14f, 0.14f, 0.14f), 4, 9); Ell(p.S, tower ? MS.Metal : MS.Dark, pos, new Vector3(0.2f, 0.2f, 0.2f), 3, 8); });
        }

        // ------------------------------------------------------------------ hantu
        static void Wraith(MkCtx c)
        {
            bool ash = c.V("ash"), banshee = c.V("banshee");
            var col = c.col; Color g = col[MS.Body];
            col[MS.ClothA] = g; col[MS.ClothB] = Dk(g, 0.7f); col[MS.Pale] = Mix(g, Color.white, 0.5f); col[MS.Skin] = Mix(g, new Color(0.9f, 1f, 0.95f), 0.5f); col[MS.Hair] = Mix(g, Color.white, 0.6f);
            col[MS.Eye] = ash ? new Color(1f, 0.5f, 0.25f) : new Color(0.9f, 1f, 0.8f); col[MS.Ember] = new Color(1f, 0.45f, 0.15f); col[MS.Pupil] = Dk(g, 0.12f);
            var s = new BpSpec { H = 1.1f, W = 0.74f, shoulderK = 0.92f, headK = 1.1f, robe = 2, hover = 0.07f, lean = 9f, neckFwd = 0.05f, armPose = -34f, weaponArm = false, armSwing = 8f, limbK = 0.62f };
            s.skin = MS.Skin; s.top = MS.ClothA; s.arm = MS.ClothA; s.fore = MS.Skin; s.glove = MS.Skin; s.bottom = MS.ClothB; s.trim = MS.Pale;
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                if (!banshee) Ell(p.S, MS.Pupil, hc + new Vector3(0f, 0f, r * 0.1f), new Vector3(r * 0.86f, r * 0.98f, r * 0.9f), 5, 10);
                if (banshee)
                {
                    Ell(p.S, MS.Skin, hc, new Vector3(r * 0.93f, r * 1.04f, r * 0.98f), 5, 12);
                    Ell(p.S, MS.Pupil, hc + new Vector3(0f, -r * 0.58f, r * 0.74f), new Vector3(r * 0.17f, r * 0.27f, r * 0.16f), 4, 8);        // mulut menjerit
                    for (int q = -1; q <= 1; q += 2) Ell(p.S, MS.Pupil, hc + new Vector3(r * 0.38f * q, r * 0.12f, r * 0.84f), new Vector3(r * 0.19f, r * 0.24f, r * 0.12f), 3, 7);
                    EyePair(p, hc + new Vector3(r * 0.38f, r * 0.1f, r * 0.9f), r * 0.12f, new Vector3(0.1f, 0f, 1f), 0.3f, 0.5f);
                    for (int k = 0; k < 9; k++)
                    {
                        float a = (k - 4f) * 0.3f; Vector3 b0 = hc + new Vector3(Mathf.Sin(a) * r * 0.95f, r * 0.6f, -r * 0.1f);
                        Tube(p.S, MS.Hair, new[] { b0, b0 + new Vector3(Mathf.Sin(a) * r * 0.5f, -r * 1.0f, -r * 0.7f), b0 + new Vector3(Mathf.Sin(a) * r * 1.1f, -r * 2.4f, -r * 1.2f), b0 + new Vector3(Mathf.Sin(a) * r * 1.5f, -r * 3.4f, -r * 1.0f) }, new[] { r * 0.14f, r * 0.12f, r * 0.08f, r * 0.02f }, new[] { r * 0.14f, r * 0.12f, r * 0.08f, r * 0.02f }, 4, Vector3.up, true, true);
                    }
                    return;
                }
                EyePair(p, hc + new Vector3(r * 0.32f, r * 0.12f, r * 0.84f), r * 0.2f, new Vector3(0.1f, 0f, 1f), 0.25f, 0.5f);
                Hood(p, L, MS.ClothA, MS.ClothB, 1.3f);
                Spike(p.S, MS.ClothA, hc + new Vector3(0f, r * 1.0f, -r * 0.6f), hc + new Vector3(0f, r * 1.7f, -r * 1.7f), r * 0.6f, 6);
            };
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W;
                for (int q = -1; q <= 1; q += 2) for (int k = 0; k < 3; k++) { Vector3 b0 = new Vector3(L.sx * 0.9f * q + (k - 1) * 0.05f * q, L.shY - 0.04f * H, -0.02f); Tube(p.S, MS.ClothB, new[] { b0, b0 + new Vector3(0.05f * q, -0.3f - k * 0.1f, -0.12f), b0 + new Vector3(0.09f * q, -0.62f - k * 0.1f, -0.2f) }, new[] { 0.06f, 0.05f, 0.012f }, new[] { 0.015f, 0.015f, 0.01f }, 4, Vector3.forward, true, true); }
                if (ash) for (int k = 0; k < 5; k++) Ell(p.G, MS.Ember, new Vector3((k % 3 - 1) * 0.07f * W, L.chestY + (k - 2) * 0.05f * H, 0.17f * W), new Vector3(0.015f, 0.04f, 0.01f), 3, 6);
            };
            s.skirtFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W; float y0 = L.hipY + 0.06f * H;
                Tube(p.S, MS.ClothA, new[] { new Vector3(0f, y0, 0f), new Vector3(0f, y0 * 0.65f, -0.02f), new Vector3(0f, 0.3f * H, -0.12f) }, new[] { 0.21f * W, 0.26f * W, 0.07f * W }, new[] { 0.16f * W, 0.21f * W, 0.07f * W }, 10, Vector3.forward, true, true);
                for (int k = 0; k < 9; k++)
                {
                    float a = k * Mathf.PI * 2f / 9f; float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    Vector3 b0 = new Vector3(sa * 0.2f * W, y0 - 0.04f, ca * 0.16f * W);
                    Tube(p.S, k % 2 == 0 ? MS.ClothA : MS.ClothB, new[] { b0, new Vector3(sa * 0.36f * W, y0 * 0.55f, ca * 0.28f * W - 0.04f), new Vector3(sa * 0.34f * W, 0.32f * H, ca * 0.26f * W - 0.22f - (k % 3) * 0.08f), new Vector3(sa * 0.28f * W, 0.14f * H, ca * 0.22f * W - 0.42f - (k % 3) * 0.1f) },
                         new[] { 0.11f * W, 0.1f * W, 0.07f * W, 0.012f }, new[] { 0.045f, 0.04f, 0.03f, 0.008f }, 5, Vector3.up, true, true);
                }
            };
            s.foreFn = delegate (MkPart p, BpLay L, int sd)
            {
                Vector3 h = L.hand[sd];
                for (int k = -2; k <= 2; k++) Spike(p.S, MS.Pale, h + new Vector3(k * 0.02f, -0.05f, 0.03f), h + new Vector3(k * 0.03f, -0.28f, 0.14f), 0.012f, 4);
                BellSleeve(p, L, sd, MS.ClothA);
            };
            Biped(c, s);
        }
    }
}
