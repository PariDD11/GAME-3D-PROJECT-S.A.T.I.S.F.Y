using System;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Parameter badan dua kaki (manusia, goblin, troll, rangka, zombi, mumi). Semua ukuran dalam meter pada H = W = 1 (orang dewasa ±1,95 m).</summary>
    public sealed class BpSpec
    {
        public float H = 1f, W = 1f;                 // skala tinggi / lebar badan dan anggota gerak
        public float headK = 1f;                      // skala kepala (anime: sedikit besar)
        public float legK = 1f, armK = 1f;            // panjang kaki / lengan relatif
        public float lean = 0f, neckFwd = 0f;         // badan condong ke depan (derajat), kepala maju (m)
        public float shoulderK = 1f, waistK = 1f, limbK = 1f;      // limbK: ketebalan kaki & lengan (rangka tipis)
        public int skin = MS.Skin, top = MS.ClothA, bottom = MS.Leather, boot = MS.Leather, glove = MS.Skin, trim = MS.Leather;
        public int arm = -1, fore = MS.Skin;          // arm < 0 = ikut top
        public float stride = 34f, kneeBend = 42f, armSwing = 26f, armPose = 0f;   // armPose: sudut dasar lengan atas (derajat; negatif = ke depan)
        public bool weaponArm = true;                 // lengan +X memegang senjata: tidak berayun, ikut ancang-ancang & tebasan
        public bool bareFeet;                         // kaki tanpa sepatu (rangka, zombi, mumi)
        public bool hollow;                           // tanpa badan padat (rangka): torsoFn menggambar seluruh tulang
        public int robe;                              // 0 = berkaki, 1 = jubah panjang (rok), 2 = bagian bawah dibuat sendiri lewat skirtFn
        public float robeFlare = 1f, hover = 0f;      // pelebaran rok; amplitudo melayang naik-turun (meter)
        public float armWindK = 1f;                   // pengali angkatan lengan non-senjata saat ancang-ancang/serangan (golem membanting dua tangan)
        public Action<MkPart, BpLay> torsoFn, headFn, skirtFn;
        public Action<MkPart, BpLay, int> upperFn, foreFn, thighFn, shinFn;       // sisi: 0 = +X, 1 = -X
    }

    public sealed class BpLay
    {
        public BpSpec s;
        public float hipY, waistY, chestY, shY, neckY, hr, hx, sx;
        public Vector3 hc;                                                      // pusat kepala
        public Vector3[] shoulder = new Vector3[2], elbow = new Vector3[2], wrist = new Vector3[2], hand = new Vector3[2];
        public Vector3[] hip = new Vector3[2], knee = new Vector3[2], ankle = new Vector3[2];
        public MkPart torso, head;
        public MkPart[] upper = new MkPart[2], fore = new MkPart[2], thigh = new MkPart[2], shin = new MkPart[2];
    }

    public static partial class MonsterKit
    {
        /// <summary>Kepala manusia gaya anime: tengkorak, rahang, hidung kecil, telinga, mata bercahaya. Dipakai langsung atau sebagai dasar tudung/helm.</summary>
        public static void HumanHead(MkPart p, BpLay L, int skin, bool ears = true, float eyeR = 1f, bool eyes = true)
        {
            Vector3 hc = L.hc; float r = L.hr;
            Ell(p.S, skin, hc, new Vector3(r * 0.93f, r * 1.04f, r * 0.98f), 6, 12);
            Ell(p.S, skin, hc + new Vector3(0f, -r * 0.55f, r * 0.24f), new Vector3(r * 0.64f, r * 0.5f, r * 0.62f), 5, 10);
            Ell(p.S, skin, hc + new Vector3(0f, -r * 0.14f, r * 1.0f), new Vector3(r * 0.1f, r * 0.15f, r * 0.13f), 4, 7);
            if (ears) for (int s = -1; s <= 1; s += 2) Ell(p.S, skin, hc + new Vector3(r * 0.94f * s, -r * 0.05f, -r * 0.06f), new Vector3(r * 0.1f, r * 0.22f, r * 0.13f), 4, 7);
            if (eyes) EyePair(p, hc + new Vector3(r * 0.38f, r * 0.08f, r * 0.84f), r * 0.17f * eyeR, new Vector3(0.12f, 0f, 1f));
        }

        /// <summary>Bilah pipih: a -> a + dir * len. Lebar w tegak lurus hint; tebal th searah hint. Ujung runcing.</summary>
        public static void Blade(GeoBag b, int slot, Vector3 a, Vector3 dir, float len, float w, float th, Vector3 hint)
        {
            dir = dir.normalized;
            Tube(b, slot, new[] { a, a + dir * (len * 0.5f), a + dir * len }, new[] { w, w * 0.96f, w * 0.1f }, new[] { th, th, th * 0.35f }, 4, hint, false, true);
        }

        /// <summary>Pedang di tangan h: gagang, pelindung, bilah. dir = arah bilah.</summary>
        public static void Sword(GeoBag b, int sBlade, int sGuard, int sGrip, Vector3 h, Vector3 dir, float len, float w = 0.055f)
        {
            dir = dir.normalized; Vector3 side = Vector3.right;
            Tube(b, sGrip, new[] { h - dir * 0.1f, h + dir * 0.07f }, new[] { 0.024f, 0.022f }, new[] { 0.024f, 0.022f }, 6, Vector3.up, true, true);
            Ell(b, sGuard, h - dir * 0.115f, new Vector3(0.034f, 0.034f, 0.034f), 3, 6);
            Tube(b, sGuard, new[] { h + dir * 0.075f - side * 0.11f, h + dir * 0.075f + side * 0.11f }, new[] { 0.02f, 0.02f }, new[] { 0.026f, 0.026f }, 5, Vector3.up, true, true);
            Blade(b, sBlade, h + dir * 0.085f, dir, len, w, 0.012f, side);
        }

        /// <summary>Titik/radius tabung badan (sama dengan yang dipakai Biped) + selisih d: untuk lapisan baju zirah/rompi yang menempel pas.</summary>
        public static void TorsoTube(BpLay L, float d, out Vector3[] pts, out float[] rx, out float[] ry)
        {
            var s = L.s; float H = s.H, W = s.W;
            pts = new[] { new Vector3(0f, L.hipY + 0.03f * H, 0f), new Vector3(0f, L.waistY, 0f), new Vector3(0f, L.chestY, 0.012f * H), new Vector3(0f, L.shY - 0.02f * H, 0f) };
            rx = new[] { 0.19f * W + d, 0.18f * W * s.waistK + d, 0.255f * W + d, 0.25f * W * s.shoulderK + d };
            ry = new[] { 0.13f * W + d, 0.12f * W * s.waistK + d, 0.17f * W + d, 0.15f * W + d };
        }

        /// <summary>Badan dua kaki umum. Menghasilkan tata letak + bagian beranimasi (kaki berayun, lengan berayun, badan naik-turun, ancang-ancang serang).</summary>
        public static BpLay Biped(MkCtx c, BpSpec s)
        {
            var L = new BpLay { s = s };
            float H = s.H, W = s.W;
            L.hipY = 0.95f * H * s.legK; L.waistY = L.hipY + 0.1f * H; L.chestY = L.hipY + 0.46f * H; L.shY = L.hipY + 0.64f * H; L.neckY = L.hipY + 0.78f * H;
            L.hr = 0.165f * s.headK * H;
            L.hx = 0.15f * W; L.sx = 0.35f * W * s.shoulderK;
            L.hc = new Vector3(0f, L.neckY + L.hr * 0.9f, s.neckFwd);
            int armSlot = s.arm >= 0 ? s.arm : s.top;
            float LW = W * s.limbK;

            // ------------------------------ kaki (atau rok)
            for (int sd = 0; sd < (s.robe == 0 ? 2 : 0); sd++)
            {
                float sg = sd == 0 ? 1f : -1f;
                Vector3 hp = new Vector3(L.hx * sg, L.hipY, 0f), kn = new Vector3(L.hx * sg * 1.04f, L.hipY * 0.53f, 0.05f * H), an = new Vector3(L.hx * sg * 1.02f, 0.12f * H, -0.02f * H);
                L.hip[sd] = hp; L.knee[sd] = kn; L.ankle[sd] = an;
                int sdc = sd; Vector3 hpc = hp, knc = kn, anc = an;
                var th = c.New("thigh" + sd, null, hp, delegate (MkPart p)
                {
                    Tube(p.S, s.bottom, new[] { hpc + new Vector3(0f, 0.02f * H, 0f), Vector3.Lerp(hpc, knc, 0.45f), knc }, new[] { 0.125f * LW, 0.112f * LW, 0.083f * LW }, new[] { 0.125f * LW, 0.112f * LW, 0.083f * LW }, 8, Vector3.forward, true, true);
                    if (s.thighFn != null) s.thighFn(p, L, sdc);
                });
                var sh = c.New("shin" + sd, th, kn, delegate (MkPart p)
                {
                    Tube(p.S, s.bottom, new[] { knc, Vector3.Lerp(knc, anc, 0.5f), anc + new Vector3(0f, 0.02f * H, 0f) }, new[] { 0.085f * LW, 0.07f * LW, 0.06f * LW }, new[] { 0.085f * LW, 0.07f * LW, 0.06f * LW }, 8, Vector3.forward, true, true);
                    int fs = s.bareFeet ? s.skin : s.boot;
                    Ell(p.S, fs, anc + new Vector3(0f, -0.045f * H, 0.1f * H), new Vector3(0.082f * LW, 0.07f * H, 0.17f * H), 4, 9);
                    if (!s.bareFeet) Tube(p.S, s.boot, new[] { anc + new Vector3(0f, 0.17f * H, 0f), anc + new Vector3(0f, 0.01f * H, 0f) }, new[] { 0.078f * LW, 0.07f * LW }, new[] { 0.078f * LW, 0.07f * LW }, 8, Vector3.forward, true, false);
                    if (s.shinFn != null) s.shinFn(p, L, sdc);
                });
                L.thigh[sd] = th; L.shin[sd] = sh;
                c.rig.Leg(th.t, sh.t, sd == 0 ? 0f : Mathf.PI, s.stride, s.kneeBend);
            }

            if (s.robe != 0)
            {
                float hv = s.hover;
                var skirt = c.New("skirt", null, new Vector3(0f, L.hipY, 0f), delegate (MkPart p)
                {
                    if (s.robe == 1)
                    {
                        float fl = s.robeFlare;
                        Tube(p.S, s.bottom, new[] { new Vector3(0f, L.hipY + 0.08f * H, 0f), new Vector3(0f, L.hipY * 0.55f, 0f), new Vector3(0f, 0.04f * H, 0f) },
                             new[] { 0.22f * W, 0.31f * W * fl, 0.5f * W * fl }, new[] { 0.17f * W, 0.25f * W * fl, 0.42f * W * fl }, 12, Vector3.forward, true, false);
                        Tube(p.S, s.trim, new[] { new Vector3(0f, 0.01f * H, 0f), new Vector3(0f, 0.09f * H, 0f) }, new[] { 0.505f * W * fl, 0.49f * W * fl }, new[] { 0.425f * W * fl, 0.41f * W * fl }, 12, Vector3.forward, false, false);
                    }
                    if (s.skirtFn != null) s.skirtFn(p, L);
                });
                Quaternion sq0 = skirt.t.localRotation; Vector3 sp0 = skirt.t.localPosition;
                c.rig.Add(delegate (MonsterRig r)
                {
                    skirt.t.localRotation = sq0 * Quaternion.AngleAxis(Mathf.Sin(r.phase) * 4f * r.spd01 + Mathf.Sin(r.time * 1.3f) * 0.8f, Vector3.right);
                    skirt.t.localPosition = sp0 + new Vector3(0f, hv > 0f ? Mathf.Sin(r.time * 1.9f) * hv : 0f, 0f);
                });
            }

            // ------------------------------ badan
            var torso = c.New("torso", null, new Vector3(0f, L.hipY, 0f), delegate (MkPart p)
            {
                if (!s.hollow)
                {
                Ell(p.S, s.bottom, new Vector3(0f, L.hipY + 0.02f * H, 0f), new Vector3(0.2f * W, 0.13f * H, 0.145f * W), 5, 10);
                Tube(p.S, s.top, new[] { new Vector3(0f, L.hipY + 0.03f * H, 0f), new Vector3(0f, L.waistY, 0f), new Vector3(0f, L.chestY, 0.012f * H), new Vector3(0f, L.shY - 0.02f * H, 0f) },
                     new[] { 0.19f * W, 0.18f * W * s.waistK, 0.255f * W, 0.25f * W * s.shoulderK }, new[] { 0.13f * W, 0.12f * W * s.waistK, 0.17f * W, 0.15f * W }, 10, Vector3.forward, true, true);
                Tube(p.S, s.skin, new[] { new Vector3(0f, L.shY - 0.04f * H, 0f), new Vector3(0f, L.neckY + 0.05f * H, s.neckFwd * 0.5f) }, new[] { 0.058f * W, 0.052f * W }, new[] { 0.058f * W, 0.052f * W }, 7, Vector3.forward, true, true);
                for (int q = -1; q <= 1; q += 2) Ell(p.S, armSlot, new Vector3(L.sx * 0.9f * q, L.shY - 0.03f * H, 0f), new Vector3(0.105f * W, 0.09f * H, 0.105f * W), 4, 8);
                Tube(p.S, s.trim, new[] { new Vector3(0f, L.waistY - 0.035f * H, 0f), new Vector3(0f, L.waistY + 0.03f * H, 0f) }, new[] { 0.195f * W * s.waistK, 0.195f * W * s.waistK }, new[] { 0.135f * W * s.waistK, 0.135f * W * s.waistK }, 10, Vector3.forward, true, true);
                }
                if (s.torsoFn != null) s.torsoFn(p, L);
            });
            L.torso = torso;
            if (s.lean != 0f) torso.t.localRotation = Quaternion.AngleAxis(s.lean, Vector3.right);

            // ------------------------------ kepala
            var head = c.New("head", torso, new Vector3(0f, L.neckY, s.neckFwd * 0.5f), delegate (MkPart p)
            {
                if (s.headFn != null) s.headFn(p, L); else HumanHead(p, L, s.skin);
            });
            L.head = head;

            // ------------------------------ lengan
            for (int sd = 0; sd < 2; sd++)
            {
                float sg = sd == 0 ? 1f : -1f;
                Vector3 sh0 = new Vector3(L.sx * sg, L.shY - 0.03f * H, 0f);
                Vector3 el = sh0 + new Vector3(0.04f * W * sg, -0.29f * H * s.armK, 0.02f * H);
                Vector3 wr = el + new Vector3(0.0f, -0.27f * H * s.armK, 0.11f * H * s.armK);
                L.shoulder[sd] = sh0; L.elbow[sd] = el; L.wrist[sd] = wr; L.hand[sd] = wr + new Vector3(0f, -0.045f * H, 0.03f * H);
                int sdc = sd; Vector3 shc = sh0, elc = el, wrc = wr;
                var up = c.New("upper" + sd, torso, sh0, delegate (MkPart p)
                {
                    Tube(p.S, armSlot, new[] { shc, Vector3.Lerp(shc, elc, 0.5f), elc }, new[] { 0.072f * LW, 0.064f * LW, 0.057f * LW }, new[] { 0.072f * LW, 0.064f * LW, 0.057f * LW }, 8, Vector3.forward, true, true);
                    if (s.upperFn != null) s.upperFn(p, L, sdc);
                });
                var fo = c.New("fore" + sd, up, el, delegate (MkPart p)
                {
                    Tube(p.S, s.fore, new[] { elc, Vector3.Lerp(elc, wrc, 0.5f), wrc }, new[] { 0.057f * LW, 0.052f * LW, 0.045f * LW }, new[] { 0.057f * LW, 0.052f * LW, 0.045f * LW }, 8, Vector3.forward, true, true);
                    Ell(p.S, s.glove, wrc + new Vector3(0f, -0.04f * H, 0.02f * H), new Vector3(0.052f * LW, 0.065f * H, 0.048f * LW), 4, 8);
                    if (s.foreFn != null) s.foreFn(p, L, sdc);
                });
                L.upper[sd] = up; L.fore[sd] = fo;
            }

            // ------------------------------ gerak
            Vector3 tp0 = torso.t.localPosition; Quaternion tq0 = torso.t.localRotation, hq0 = head.t.localRotation;
            var u0 = new Quaternion[] { L.upper[0].t.localRotation, L.upper[1].t.localRotation };
            var f0 = new Quaternion[] { L.fore[0].t.localRotation, L.fore[1].t.localRotation };
            c.rig.Add(delegate (MonsterRig r)
            {
                float bob = Mathf.Abs(Mathf.Sin(r.phase)) * 0.035f * H * r.spd01;
                torso.t.localPosition = tp0 + new Vector3(0f, bob - 0.05f * H * r.wind + (s.hover > 0f ? Mathf.Sin(r.time * 1.9f) * s.hover : 0f), 0.1f * H * r.lunge);
                torso.t.localRotation = tq0 * Quaternion.AngleAxis(Mathf.Sin(r.phase) * 5f * r.spd01, Vector3.up) * Quaternion.AngleAxis(-10f * r.wind + 14f * r.lunge, Vector3.right);
                head.t.localRotation = hq0 * Quaternion.AngleAxis(8f * r.wind - 8f * r.lunge, Vector3.right);
                for (int sd = 0; sd < 2; sd++)
                {
                    float ph = sd == 0 ? Mathf.PI : 0f;
                    bool wp = s.weaponArm && sd == 0;
                    float sw = Mathf.Sin(r.phase + ph) * s.armSwing * r.spd01 * (wp ? 0.35f : 1f);
                    float raise = wp ? -118f * r.wind + 78f * r.lunge : (-52f * r.wind + 36f * r.lunge) * s.armWindK;
                    L.upper[sd].t.localRotation = u0[sd] * Quaternion.AngleAxis(s.armPose + sw + raise, Vector3.right);
                    float elbow = -Mathf.Max(0f, Mathf.Cos(r.phase + ph)) * 26f * r.spd01 + (wp ? -22f * r.wind - 12f * r.lunge : -10f * r.wind);
                    L.fore[sd].t.localRotation = f0[sd] * Quaternion.AngleAxis(elbow, Vector3.right);
                }
            });
            return L;
        }
    }
}
