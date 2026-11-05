using UnityEngine;

namespace HunterGuild
{
    /// <summary>Bentuk manusiawi bersenjata: bandit, pemanah, pemimpin/ksatria.</summary>
    public static partial class MonsterKit
    {
        static void Cape(MkCtx c, BpLay L, int slot, float len, float wid)
        {
            float H = L.s.H, W = L.s.W;
            Vector3 top = new Vector3(0f, L.shY - 0.02f * H, -0.15f * W);
            var cape = c.New("cape", L.torso, top, delegate (MkPart p)
            {
                Tube(p.S, slot, new[] { top, top + new Vector3(0f, -len * 0.5f, -0.1f * W), top + new Vector3(0f, -len, -0.2f * W) }, new[] { 0.27f * W * wid, 0.31f * W * wid, 0.36f * W * wid }, new[] { 0.018f, 0.02f, 0.02f }, 7, Vector3.forward, true, true);
            });
            c.rig.Sway(cape.t, Vector3.right, 3f, 1.7f, 0f, 20f);
        }

        // tudung (kubah belakang+atas, depan terbuka) dan masker bawah wajah
        static void Hood(MkPart p, BpLay L, int slot, int inner, float k = 1.13f)
        {
            Vector3 hc = L.hc; float r = L.hr;
            EllArc(p.S, slot, hc + new Vector3(0f, 0.015f, -0.03f), new Vector3(r * k, r * k * 1.06f, r * k * 1.04f), Quaternion.identity, 6, 14, 58f, 302f, -42f, 90f, inner, 0.95f);
            Tube(p.S, slot, new[] { hc + new Vector3(-r * 0.9f, -r * 1.0f, -r * 0.2f), hc + new Vector3(0f, -r * 1.35f, -r * 0.55f), hc + new Vector3(r * 0.9f, -r * 1.0f, -r * 0.2f) }, new[] { r * 0.28f, r * 0.3f, r * 0.28f }, new[] { r * 0.14f, r * 0.14f, r * 0.14f }, 5, Vector3.forward, true, true);
        }

        static void Mask(MkPart p, BpLay L, int slot)
        {
            Vector3 hc = L.hc; float r = L.hr;
            EllArc(p.S, slot, hc + new Vector3(0f, -r * 0.02f, r * 0.02f), new Vector3(r * 1.0f, r * 1.07f, r * 1.02f), Quaternion.identity, 4, 10, -62f, 62f, -62f, -14f, slot, 0.96f);
        }

        static void Shield(MkPart p, Vector3 h, int face, int rim, int boss, float rad)
        {
            Vector3 ctr = h + new Vector3(-0.02f, 0.08f, 0.17f);
            Ell(p.S, rim, ctr + new Vector3(0f, 0f, -0.012f), new Vector3(rad * 1.04f, rad * 1.04f, 0.05f), 4, 14);
            Ell(p.S, face, ctr + new Vector3(0f, 0f, 0.012f), new Vector3(rad * 0.93f, rad * 0.93f, 0.05f), 4, 14);
            Ell(p.S, boss, ctr + new Vector3(0f, 0f, 0.05f), new Vector3(rad * 0.22f, rad * 0.22f, 0.07f), 3, 8);
        }

        static void Bandit(MkCtx c)
        {
            string sh = c.shape;
            bool archer = sh == "archer", leader = sh == "leader";
            bool knight = c.V("knight") || c.V("warden"), warden = c.V("warden"), gladi = c.V("gladiator"), blade = c.V("blade"), dark = c.V("dark"), bone = c.V("bone"), shadow = c.V("shadow");
            var col = c.col;
            Color cloth = col[MS.Body];
            col[MS.ClothA] = cloth; col[MS.ClothB] = Dk(cloth, 0.7f); col[MS.Leather] = new Color(0.26f, 0.17f, 0.11f);
            col[MS.Eye] = new Color(1f, 0.9f, 0.55f);
            if (knight || dark) { col[MS.Metal] = Mix(cloth, new Color(0.62f, 0.64f, 0.7f), 0.55f); col[MS.Eye] = dark ? new Color(0.85f, 0.4f, 1f) : new Color(1f, 0.35f, 0.25f); }
            if (gladi) { col[MS.Metal] = new Color(0.78f, 0.6f, 0.28f); col[MS.Skin] = new Color(0.72f, 0.52f, 0.4f); }
            if (blade) { col[MS.ClothB] = new Color(0.12f, 0.09f, 0.1f); col[MS.Eye] = new Color(1f, 0.3f, 0.2f); }
            if (bone) { col[MS.Skin] = col[MS.Bone]; col[MS.Eye] = new Color(0.4f, 1f, 0.6f); col[MS.ClothA] = Dk(col[MS.Bone], 0.7f); }
            if (shadow) { col[MS.Skin] = new Color(0.3f, 0.3f, 0.4f); col[MS.Eye] = new Color(0.7f, 0.5f, 1f); col[MS.ClothB] = Dk(cloth, 0.5f); }

            var s = new BpSpec();
            s.top = MS.ClothA; s.bottom = MS.ClothB; s.boot = MS.Leather; s.trim = MS.Leather; s.glove = MS.Leather; s.skin = MS.Skin;
            s.weaponArm = !archer; s.armPose = archer ? -12f : 0f;
            if (leader) { s.W = 1.1f; s.shoulderK = 1.12f; s.H = 1.03f; }
            if (knight) { s.W = warden ? 1.22f : 1.1f; s.H = 1.05f; s.shoulderK = 1.18f; s.top = MS.Metal; s.arm = MS.Metal; s.fore = MS.Metal; s.bottom = MS.Metal; s.boot = MS.Metal; s.glove = MS.Metal; s.trim = MS.Leather; }
            if (gladi) { s.W = 1.12f; s.shoulderK = 1.14f; s.top = MS.Skin; s.arm = MS.Skin; s.fore = MS.Metal; s.bottom = MS.ClothA; s.glove = MS.Leather; s.trim = MS.Metal; }
            if (blade || shadow) { s.top = MS.ClothB; s.arm = MS.ClothB; s.fore = MS.ClothB; s.bottom = MS.ClothB; s.W = 0.96f; }
            if (bone) { s.top = MS.Skin; s.arm = MS.Skin; s.bottom = MS.Skin; s.fore = MS.Skin; s.glove = MS.Skin; s.boot = MS.Skin; s.W = 0.8f; s.armSwing = 30f; }

            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W;
                Vector3[] tp; float[] trx, tr;
                if (knight)
                {
                    // zirah dada penuh + gorget + pelindung bahu bertingkat + rok baja
                    TorsoTube(L, 0.03f, out tp, out trx, out tr);
                    Tube(p.S, MS.Metal, tp, trx, tr, 12, Vector3.forward, true, true);
                    Tube(p.S, MS.Dark, new[] { new Vector3(0f, L.waistY + 0.2f * H, 0f), new Vector3(0f, L.waistY + 0.12f * H, 0.02f) }, new[] { 0.01f, 0.01f }, new[] { 0.01f, 0.01f }, 3, Vector3.forward, false, false);
                    for (int q = -1; q <= 1; q += 2)
                    {
                        Ell(p.S, MS.Metal, new Vector3(L.sx * 0.98f * q, L.shY - 0.01f * H, 0f), new Vector3(0.17f * W, 0.12f * H, 0.16f * W), 4, 10);
                        Ell(p.S, MS.Metal, new Vector3(L.sx * 1.04f * q, L.shY - 0.1f * H, 0f), new Vector3(0.15f * W, 0.07f * H, 0.15f * W), 4, 10);
                        Spike(p.S, MS.Dark, new Vector3(L.sx * 1.1f * q, L.shY + 0.05f * H, 0f), new Vector3(L.sx * 1.2f * q, L.shY + 0.3f * H, -0.04f), 0.05f * W, 5);
                    }
                    Tube(p.S, MS.Metal, new[] { new Vector3(0f, L.hipY + 0.06f * H, 0f), new Vector3(0f, L.hipY - 0.2f * H, 0.01f) }, new[] { 0.24f * W, 0.3f * W }, new[] { 0.18f * W, 0.22f * W }, 10, Vector3.forward, true, true);
                    Ell(p.S, MS.Dark, new Vector3(0f, L.chestY + 0.02f * H, 0.17f * W), new Vector3(0.075f * W, 0.11f * H, 0.03f), 3, 8);    // lambang dada
                }
                else if (gladi)
                {
                    // dada telanjang berotot: pelat dada kulit + sabuk emas + rok kulit
                    TorsoTube(L, 0.012f, out tp, out trx, out tr);
                    TubeBands(p.S, new[] { MS.Leather }, new[] { 55f, 125f }, tp, trx, tr, 4, Vector3.forward, false, false);
                    Tube(p.S, MS.Leather, new[] { new Vector3(0f, L.hipY + 0.05f * H, 0f), new Vector3(0f, L.hipY - 0.17f * H, 0.01f) }, new[] { 0.23f * W, 0.29f * W }, new[] { 0.17f * W, 0.21f * W }, 9, Vector3.forward, true, true);
                    for (int q = -1; q <= 1; q += 2) Ell(p.S, MS.Metal, new Vector3(L.sx * 0.98f * q, L.shY + 0.0f, 0f), new Vector3(0.16f * W, 0.09f * H, 0.14f * W), 4, 10);
                }
                else if (bone)
                {
                    for (int r = 0; r < 4; r++) Ell(p.S, MS.Bone, new Vector3(0f, L.chestY - 0.06f * H + r * 0.075f * H, 0.01f), new Vector3((0.24f - r * 0.015f) * W, 0.022f * H, 0.17f * W), 3, 12);
                }
                else
                {
                    // rompi kulit: dua panel sempit di dada (celah tengah memperlihatkan kemeja) + tali bahu + kantong di pinggang
                    TorsoTube(L, 0.012f, out tp, out trx, out tr);
                    TubeBands(p.S, new[] { MS.Leather }, new[] { 38f, 84f }, tp, trx, tr, 4, Vector3.forward, false, false);
                    TubeBands(p.S, new[] { MS.Leather }, new[] { 96f, 142f }, tp, trx, tr, 4, Vector3.forward, false, false);
                    Ell(p.S, MS.Leather, new Vector3(0.2f * W, L.waistY - 0.035f * H, 0.1f * W), new Vector3(0.075f * W, 0.06f * H, 0.05f * W), 3, 8);
                    Tube(p.S, MS.Leather, new[] { new Vector3(-0.2f * W, L.shY, 0.0f), new Vector3(0.18f * W, L.waistY - 0.02f * H, -0.12f * W) }, new[] { 0.025f, 0.025f }, new[] { 0.012f, 0.012f }, 5, Vector3.forward, true, true);
                    if (leader)
                        for (int q = -1; q <= 1; q += 2) Ell(p.S, MS.Metal, new Vector3(L.sx * 0.98f * q, L.shY + 0.02f * H, 0f), new Vector3(0.15f * W, 0.085f * H, 0.14f * W), 4, 10);
                    if (blade) Tube(p.S, MS.ClothA, new[] { new Vector3(0f, L.waistY - 0.02f * H, 0f), new Vector3(0f, L.waistY + 0.04f * H, 0f) }, new[] { 0.2f, 0.2f }, new[] { 0.14f, 0.14f }, 8, Vector3.forward, true, true);
                }
                if (archer)
                {
                    // tabung anak panah di punggung + bulu panah
                    Tube(p.S, MS.Leather, new[] { new Vector3(0.14f * W, L.waistY + 0.04f * H, -0.2f * W), new Vector3(0.04f * W, L.shY + 0.02f * H, -0.23f * W) }, new[] { 0.07f * W, 0.075f * W }, new[] { 0.07f * W, 0.075f * W }, 7, Vector3.forward, true, true);
                    for (int a = 0; a < 4; a++)
                    {
                        Vector3 b0 = new Vector3(0.03f * W + a * 0.018f, L.shY + 0.02f * H, -0.23f * W + (a - 1.5f) * 0.02f);
                        Spike(p.S, MS.Bone, b0, b0 + new Vector3(-0.05f, 0.2f * H, 0.02f * (a - 1.5f)), 0.012f, 4);
                        Ell(p.S, MS.ClothB, b0 + new Vector3(-0.035f, 0.1f * H, 0f), new Vector3(0.012f, 0.05f, 0.03f), 3, 5);
                    }
                }
            };

            s.headFn = delegate (MkPart p, BpLay L)
            {
                float r = L.hr; Vector3 hc = L.hc;
                if (knight)
                {
                    // helm tertutup: kubah + pelat dagu, celah mata menyala + jambul
                    Ell(p.S, MS.Metal, hc + new Vector3(0f, 0.02f, 0f), new Vector3(r * 1.1f, r * 1.14f, r * 1.12f), 6, 14);
                    Ell(p.S, MS.Metal, hc + new Vector3(0f, -r * 0.55f, r * 0.2f), new Vector3(r * 0.9f, r * 0.62f, r * 0.85f), 4, 12);
                    Tube(p.S, MS.Dark, new[] { hc + new Vector3(-r * 0.78f, r * 0.08f, r * 0.82f), hc + new Vector3(0f, r * 0.04f, r * 1.12f), hc + new Vector3(r * 0.78f, r * 0.08f, r * 0.82f) }, new[] { r * 0.07f, r * 0.07f, r * 0.07f }, new[] { r * 0.14f, r * 0.14f, r * 0.14f }, 5, Vector3.forward, true, true);
                    EyePair(p, hc + new Vector3(r * 0.34f, r * 0.08f, r * 0.96f), r * 0.14f, new Vector3(0.1f, 0f, 1f), 0.4f, 0.5f);
                    Tube(p.S, warden ? MS.Accent : MS.Dark, new[] { hc + new Vector3(0f, r * 1.0f, -r * 0.7f), hc + new Vector3(0f, r * 1.35f, 0f), hc + new Vector3(0f, r * 1.0f, r * 0.7f) }, new[] { r * 0.08f, r * 0.08f, r * 0.08f }, new[] { r * 0.22f, r * 0.3f, r * 0.22f }, 5, Vector3.right, true, true);
                    if (!warden) for (int q = -1; q <= 1; q += 2) Horn(p.S, MS.Bone, hc + new Vector3(r * 0.95f * q, r * 0.45f, 0f), hc + new Vector3(r * 1.7f * q, r * 1.35f, r * 0.15f), new Vector3(r * 0.2f * q, 0f, r * 0.2f), r * 0.2f);
                }
                else if (gladi)
                {
                    HumanHead(p, L, MS.Skin);
                    Ell(p.S, MS.Metal, hc + new Vector3(0f, r * 0.2f, -r * 0.05f), new Vector3(r * 1.06f, r * 0.95f, r * 1.04f), 5, 12);
                    Tube(p.S, MS.ClothB, new[] { hc + new Vector3(0f, r * 1.0f, -r * 0.9f), hc + new Vector3(0f, r * 1.7f, -r * 0.1f), hc + new Vector3(0f, r * 1.2f, r * 0.8f) }, new[] { r * 0.1f, r * 0.1f, r * 0.1f }, new[] { r * 0.35f, r * 0.4f, r * 0.3f }, 5, Vector3.right, true, true);
                    EllArc(p.S, MS.Metal, hc + new Vector3(0f, -r * 0.2f, 0f), new Vector3(r * 1.02f, r * 0.9f, r * 1.03f), Quaternion.identity, 3, 8, 50f, 130f, -50f, 20f);
                }
                else if (bone)
                {
                    // tengkorak: tempurung + rahang + rongga mata menyala
                    Ell(p.S, MS.Bone, hc + new Vector3(0f, r * 0.05f, -r * 0.05f), new Vector3(r * 0.95f, r * 1.0f, r * 1.0f), 6, 12);
                    Ell(p.S, MS.Bone, hc + new Vector3(0f, -r * 0.6f, r * 0.35f), new Vector3(r * 0.62f, r * 0.42f, r * 0.55f), 4, 10);
                    for (int q = -1; q <= 1; q += 2) { Ell(p.S, MS.Pupil, hc + new Vector3(r * 0.36f * q, r * 0.12f, r * 0.82f), new Vector3(r * 0.27f, r * 0.24f, r * 0.14f), 4, 8); }
                    EyePair(p, hc + new Vector3(r * 0.36f, r * 0.12f, r * 0.9f), r * 0.15f, new Vector3(0.1f, 0f, 1f), 0.2f, 0.6f);
                    for (int t = -2; t <= 2; t++) Ell(p.S, MS.Bone, hc + new Vector3(t * r * 0.15f, -r * 0.5f, r * 0.84f), new Vector3(r * 0.055f, r * 0.09f, r * 0.05f), 3, 5);
                    Hood(p, L, MS.ClothA, MS.Dark, 1.2f);
                }
                else
                {
                    HumanHead(p, L, MS.Skin);
                    Hood(p, L, MS.ClothB, MS.Dark);
                    Mask(p, L, blade ? MS.ClothA : MS.Dark);
                    if (leader)
                    {
                        // helm tanduk di atas tudung
                        EllArc(p.S, MS.Metal, hc + new Vector3(0f, r * 0.12f, 0f), new Vector3(r * 1.17f, r * 1.1f, r * 1.14f), Quaternion.identity, 4, 12, 0f, 360f, 15f, 90f, MS.Dark, 0.95f);
                        for (int q = -1; q <= 1; q += 2) Horn(p.S, MS.Bone, hc + new Vector3(r * 1.1f * q, r * 0.55f, 0f), hc + new Vector3(r * 1.75f * q, r * 1.45f, r * 0.1f), new Vector3(r * 0.25f * q, 0f, r * 0.1f), r * 0.17f);
                    }
                }
            };

            // ----- senjata
            s.foreFn = delegate (MkPart p, BpLay L, int side)
            {
                Vector3 h = L.hand[side]; float H = L.s.H;
                if (side == 0 && !archer)
                {
                    if (blade) Sword(p.S, MS.Metal, MS.Leather, MS.ClothA, h, new Vector3(0f, 0.18f, 1f), 1.35f * H, 0.035f);
                    else if (knight || leader && c.def.id != "bandit_leader") Sword(p.S, MS.Metal, MS.Dark, MS.Leather, h, new Vector3(0f, 0.3f, 1f), (warden ? 1.5f : 1.3f) * H, 0.075f);
                    else if (gladi) Sword(p.S, MS.Metal, MS.Accent, MS.Leather, h, new Vector3(0f, 0.25f, 1f), 0.85f * H, 0.065f);
                    else if (bone) Sword(p.S, MS.Metal, MS.Leather, MS.Leather, h, new Vector3(0f, 0.3f, 1f), 0.95f * H, 0.05f);
                    else Sword(p.S, MS.Metal, MS.Leather, MS.Leather, h, new Vector3(0f, 0.3f, 1f), (leader ? 1.15f : 0.9f) * H, leader ? 0.065f : 0.05f);
                }
                if (side == 1 && !archer && (!leader || knight) && !blade && !shadow)
                {
                    if (knight || gladi) Shield(p, h, knight ? MS.Dark : MS.ClothA, MS.Metal, MS.Accent, warden ? 0.36f : 0.3f);
                    else if (!leader) Shield(p, h, MS.Wood, MS.Leather, MS.Metal, 0.26f);
                }
                if (side == 1 && archer)
                {
                    // busur melengkung + tali
                    Vector3 g = h + new Vector3(0f, 0.04f, 0.1f);
                    Tube(p.S, MS.Wood, new[] { g + new Vector3(0f, 0.62f * H, -0.06f), g + new Vector3(0f, 0.28f * H, 0.12f), g + new Vector3(0f, 0f, 0.15f), g + new Vector3(0f, -0.28f * H, 0.12f), g + new Vector3(0f, -0.62f * H, -0.06f) },
                         new[] { 0.012f, 0.022f, 0.028f, 0.022f, 0.012f }, new[] { 0.012f, 0.022f, 0.028f, 0.022f, 0.012f }, 5, Vector3.right, true, true);
                    Tube(p.S, MS.Pale, new[] { g + new Vector3(0f, 0.62f * H, -0.06f), g + new Vector3(0f, -0.62f * H, -0.06f) }, new[] { 0.005f, 0.005f }, new[] { 0.005f, 0.005f }, 3, Vector3.right, false, false);
                }
            };

            var L0 = Biped(c, s);
            if (leader && !knight && !gladi && !blade) Cape(c, L0, MS.ClothB, 1.05f * s.H, 1.15f);
            if (knight || dark) Cape(c, L0, MS.Dark, 1.2f * s.H, 1.25f);
            if (blade) Cape(c, L0, MS.ClothA, 0.55f * s.H, 0.8f);
        }
    }
}
