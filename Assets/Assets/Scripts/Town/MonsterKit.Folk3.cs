using UnityEngine;

namespace HunterGuild
{
    /// <summary>Goblin/imp, troll/ogre, zombi, rangka, mumi, boneka latihan.</summary>
    public static partial class MonsterKit
    {
        // sayap kelelawar: tulang + selaput; kepak lewat Sway pada sumbu depan
        static void Wings(MkCtx c, BpLay L, int bone, int mem, float span)
        {
            float H = L.s.H;
            for (int sd = 0; sd < 2; sd++)
            {
                float sg = sd == 0 ? 1f : -1f;
                Vector3 piv = new Vector3(0.1f * sg, L.shY - 0.06f * H, -0.12f);
                var wing = c.New("wing" + sd, L.torso, piv, delegate (MkPart p)
                {
                    Vector3 a = piv, w1 = a + new Vector3(0.55f * sg, 0.42f, -0.18f) * span;
                    Vector3 t1 = w1 + new Vector3(0.5f * sg, 0.2f, -0.12f) * span, t2 = w1 + new Vector3(0.6f * sg, -0.22f, -0.3f) * span, t3 = w1 + new Vector3(0.3f * sg, -0.6f, -0.36f) * span, lo = a + new Vector3(0.03f * sg, -0.6f, -0.12f) * span;
                    p.S.QuadBoth(mem, a, w1, t1, t1);
                    Vector3[] fp = { t1, t2, t3, lo };                      // selaput melengkung ke dalam di antara ujung jari (tepi bergerigi)
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 mid = Vector3.Lerp(Vector3.Lerp(fp[i], fp[i + 1], 0.5f), a, 0.2f);
                        p.S.QuadBoth(mem, a, fp[i], mid, mid); p.S.QuadBoth(mem, a, mid, fp[i + 1], fp[i + 1]);
                    }
                    Tube(p.S, bone, new[] { a, w1 }, new[] { 0.032f, 0.022f }, new[] { 0.032f, 0.022f }, 5, Vector3.up, true, true);
                    foreach (Vector3 t in new[] { t1, t2, t3 }) Tube(p.S, bone, new[] { w1, t }, new[] { 0.02f, 0.008f }, new[] { 0.02f, 0.008f }, 4, Vector3.up, true, true);
                });
                c.rig.Sway(wing.t, sd == 0 ? Vector3.forward : Vector3.back, 9f, 5.5f, sd * 0.2f, 22f);
            }
        }

        static void Goblin(MkCtx c)
        {
            bool imp = c.V("imp") || c.V("ember"), ember = c.V("ember"), king = c.V("king");
            var col = c.col; Color skin = col[MS.Body];
            col[MS.Skin] = skin; col[MS.ClothA] = imp ? Dk(skin, 0.6f) : new Color(0.4f, 0.28f, 0.18f); col[MS.ClothB] = Dk(col[MS.ClothA], 0.7f);
            col[MS.Eye] = ember ? new Color(1f, 0.95f, 0.5f) : (imp ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.85f, 0.2f));
            col[MS.Bone] = new Color(0.9f, 0.87f, 0.75f); col[MS.Hair] = Dk(skin, 0.5f);
            if (ember) col[MS.Ember] = new Color(1f, 0.45f, 0.1f);
            var s = new BpSpec { H = imp ? 0.78f : 0.86f, W = 0.9f, headK = 1.6f, legK = 0.78f, armK = 1.2f, lean = imp ? 10f : 17f, neckFwd = 0.07f, limbK = 0.9f };
            s.skin = MS.Skin; s.top = imp ? MS.Skin : MS.ClothA; s.arm = MS.Skin; s.fore = MS.Skin; s.bottom = MS.Skin; s.boot = MS.Skin; s.bareFeet = true; s.glove = MS.Skin; s.trim = MS.Leather;
            if (king) { s.H = 0.92f; s.W = 1.1f; s.top = MS.Skin; }
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                Ell(p.S, MS.Skin, hc, new Vector3(r * 1.02f, r * 0.95f, r * 0.98f), 6, 12);
                Ell(p.S, MS.Skin, hc + new Vector3(0f, -r * 0.5f, r * 0.38f), new Vector3(r * 0.62f, r * 0.42f, r * 0.55f), 5, 10);                  // dagu maju
                Tube(p.S, MS.Skin, new[] { hc + new Vector3(0f, r * 0.05f, r * 0.85f), hc + new Vector3(0f, -r * 0.08f, r * 1.2f), hc + new Vector3(0f, -r * 0.34f, r * 1.36f) }, new[] { r * 0.17f, r * 0.15f, r * 0.1f }, new[] { r * 0.17f, r * 0.15f, r * 0.1f }, 6, Vector3.right, true, true);  // hidung bengkok besar
                for (int q = -1; q <= 1; q += 2)
                {
                    Ear(p.S, MS.Skin, hc + new Vector3(r * 0.92f * q, r * 0.0f, -r * 0.05f), hc + new Vector3(r * 2.6f * q, r * 0.6f, -r * 0.55f), r * 0.5f, r * 0.07f, Vector3.forward, 6);
                    Tube(p.S, MS.Dark, new[] { hc + new Vector3(r * 0.12f * q, r * 0.5f, r * 0.9f), hc + new Vector3(r * 0.5f * q, r * 0.42f, r * 0.8f), hc + new Vector3(r * 0.85f * q, r * 0.5f, r * 0.45f) }, new[] { r * 0.1f, r * 0.1f, r * 0.1f }, new[] { r * 0.09f, r * 0.09f, r * 0.09f }, 5, Vector3.up, true, true);  // alis marah
                    Spike(p.S, MS.Bone, hc + new Vector3(r * 0.3f * q, -r * 0.72f, r * 0.85f), hc + new Vector3(r * 0.32f * q, -r * 0.2f, r * 0.9f), r * 0.1f, 5);   // taring bawah
                }
                EyePair(p, hc + new Vector3(r * 0.4f, r * 0.12f, r * 0.82f), r * 0.24f, new Vector3(0.12f, 0f, 1f), 0.55f, 0.6f);
                if (imp) for (int q = -1; q <= 1; q += 2) Horn(p.S, MS.Bone, hc + new Vector3(r * 0.5f * q, r * 0.85f, r * 0.1f), hc + new Vector3(r * 0.95f * q, r * 1.85f, -r * 0.2f), new Vector3(r * 0.25f * q, r * 0.1f, -r * 0.2f), r * 0.2f);
                if (king)
                {
                    Tube(p.S, MS.Accent, new[] { hc + new Vector3(-r * 0.78f, r * 0.72f, 0f), hc + new Vector3(0f, r * 0.85f, r * 0.78f), hc + new Vector3(r * 0.78f, r * 0.72f, 0f) }, new[] { r * 0.1f, r * 0.1f, r * 0.1f }, new[] { r * 0.1f, r * 0.1f, r * 0.1f }, 6, Vector3.up, true, true);
                    for (int k = 0; k < 5; k++) { float a = (-80f + k * 40f) * Mathf.Deg2Rad; Vector3 bp = hc + new Vector3(Mathf.Sin(a) * r * 0.8f, r * 0.8f, Mathf.Cos(a) * r * 0.8f); Spike(p.S, MS.Accent, bp, bp + new Vector3(0f, r * 0.55f, 0f), r * 0.1f, 5); }
                }
            };
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W;
                if (!imp)
                {
                    Tube(p.S, MS.ClothB, new[] { new Vector3(0f, L.hipY + 0.06f * H, 0f), new Vector3(0f, L.hipY - 0.26f * H, 0.03f) }, new[] { 0.23f * W, 0.27f * W }, new[] { 0.17f * W, 0.2f * W }, 9, Vector3.forward, true, true);
                    Ell(p.S, MS.Leather, new Vector3(0.2f * W, L.hipY - 0.05f * H, 0.1f * W), new Vector3(0.07f, 0.09f, 0.06f), 3, 7);
                }
                else
                {
                    for (int q = -1; q <= 1; q += 2) Spike(p.S, MS.Dark, new Vector3(0.1f * q, L.shY - 0.04f, -0.12f), new Vector3(0.14f * q, L.shY + 0.12f, -0.2f), 0.04f, 5);
                    Ell(p.S, MS.Pale, new Vector3(0f, L.chestY - 0.05f * H, 0.15f * W), new Vector3(0.14f * W, 0.2f * H, 0.06f), 4, 8);
                    if (ember) for (int k = 0; k < 3; k++) Ell(p.G, MS.Ember, new Vector3(0f, L.chestY + (k - 1) * 0.1f * H, 0.19f * W), new Vector3(0.05f, 0.012f, 0.012f), 3, 6);
                }
                if (king)
                {
                    for (int q = -1; q <= 1; q += 2) { Ell(p.S, MS.Bone, new Vector3(L.sx * q, L.shY + 0.03f, 0f), new Vector3(0.16f * W, 0.1f, 0.15f * W), 4, 8); Spike(p.S, MS.Bone, new Vector3(L.sx * 1.05f * q, L.shY + 0.1f, 0f), new Vector3(L.sx * 1.15f * q, L.shY + 0.3f, 0f), 0.04f, 5); }
                    Ruff(p.S, MS.Hair, new Vector3(0f, L.shY - 0.02f, 0.02f), Vector3.up, 0.22f * W, 10, 0.16f, 0.08f, 0.04f, 0.0f, 0f);
                }
            };
            s.foreFn = delegate (MkPart p, BpLay L, int side)
            {
                Vector3 h = L.hand[side];
                for (int k = -1; k <= 1; k++) Spike(p.S, MS.Claw, h + new Vector3(k * 0.022f, -0.05f, 0.03f), h + new Vector3(k * 0.026f, -0.11f, 0.06f), 0.012f, 4);
                if (side == 0)
                {
                    if (king) { Blade(p.S, MS.Metal, h + new Vector3(0f, 0.02f, 0.02f), new Vector3(0f, 0.35f, 1f), 0.8f, 0.11f, 0.02f, Vector3.right); Tube(p.S, MS.Leather, new[] { h - new Vector3(0f, 0f, 0.1f), h + new Vector3(0f, 0.05f, 0.08f) }, new[] { 0.025f, 0.025f }, new[] { 0.025f, 0.025f }, 5, Vector3.up, true, true); }
                    else if (!imp) { Blade(p.S, MS.Metal, h + new Vector3(0f, 0.03f, 0.04f), new Vector3(0f, 0.3f, 1f), 0.42f, 0.045f, 0.012f, Vector3.right); Tube(p.S, MS.Leather, new[] { h - new Vector3(0f, 0f, 0.07f), h + new Vector3(0f, 0.03f, 0.06f) }, new[] { 0.02f, 0.02f }, new[] { 0.02f, 0.02f }, 5, Vector3.up, true, true); }
                }
            };
            var L0 = Biped(c, s);
            if (imp)
            {
                Wings(c, L0, MS.Dark, ember ? MS.Body : MS.Pale, 0.8f);
                float H = s.H; Vector3 tp0 = new Vector3(0f, L0.hipY + 0.02f * H, -0.12f);
                var tail = c.New("tail", L0.torso, tp0, delegate (MkPart p)
                {
                    Tube(p.S, MS.Skin, new[] { tp0, tp0 + new Vector3(0f, -0.12f, -0.3f), tp0 + new Vector3(0f, 0.0f, -0.62f), tp0 + new Vector3(0f, 0.2f, -0.78f) }, new[] { 0.045f, 0.035f, 0.028f, 0.02f }, new[] { 0.045f, 0.035f, 0.028f, 0.02f }, 6, Vector3.up, true, true);
                    Spike(p.S, MS.Dark, tp0 + new Vector3(0f, 0.2f, -0.78f), tp0 + new Vector3(0f, 0.36f, -0.84f), 0.06f, 5);
                    Ell(p.S, MS.Dark, tp0 + new Vector3(0f, 0.2f, -0.78f), new Vector3(0.015f, 0.09f, 0.06f), 3, 6);
                });
                c.rig.Sway(tail.t, Vector3.up, 14f, 2.6f, 0f, 16f);
            }
        }

        // ------------------------------------------------------------------ troll / ogre
        static void Troll(MkCtx c)
        {
            bool moss = c.V("moss"), ogre = c.V("ogre") || c.V("chief"), chief = c.V("chief"), pumpkin = c.V("pumpkin"), demon = c.V("demon"), stone = c.V("stone");
            var col = c.col; Color skin = col[MS.Body];
            col[MS.Skin] = skin; col[MS.ClothA] = Dk(skin, 0.75f); col[MS.ClothB] = new Color(0.36f, 0.25f, 0.16f);
            col[MS.Eye] = demon ? new Color(1f, 0.5f, 0.1f) : new Color(1f, 0.85f, 0.2f); col[MS.Bone] = new Color(0.9f, 0.87f, 0.76f);
            col[MS.Hair] = new Color(0.3f, 0.5f, 0.22f);       // lumut
            if (stone) { col[MS.Skin] = Mix(skin, new Color(0.5f, 0.5f, 0.52f), 0.4f); col[MS.Rock] = Dk(col[MS.Skin], 0.8f); col[MS.Ember] = new Color(1f, 0.6f, 0.2f); }
            if (pumpkin) { col[MS.Skin] = new Color(0.78f, 0.6f, 0.4f); col[MS.ClothA] = new Color(0.3f, 0.22f, 0.16f); col[MS.ClothB] = new Color(0.25f, 0.2f, 0.4f); col[MS.Accent] = new Color(0.35f, 0.55f, 0.2f); col[MS.Eye] = new Color(1f, 0.8f, 0.2f); }
            var s = new BpSpec { H = 1.3f, W = 1.55f, headK = 1.0f, legK = 0.75f, armK = 1.2f, lean = 14f, neckFwd = 0.1f, shoulderK = 1.0f, limbK = 1.0f, waistK = 1.15f, stride = 28f, kneeBend = 30f };
            s.skin = MS.Skin; s.top = MS.Skin; s.arm = MS.Skin; s.fore = MS.Skin; s.bottom = ogre || pumpkin ? MS.ClothB : MS.Skin; s.boot = MS.Skin; s.bareFeet = true; s.glove = MS.Skin; s.trim = MS.Leather;
            if (pumpkin) { s.top = MS.ClothA; s.arm = MS.ClothA; s.bottom = MS.ClothB; }
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                if (pumpkin)
                {
                    // kepala labu: belahan menggembung + ukiran wajah menyala + batang
                    for (int k = -2; k <= 2; k++) Ell(p.S, MS.Body, hc + new Vector3(k * r * 0.38f, 0f, 0f), new Vector3(r * 0.62f, r * 1.25f, r * 1.3f), 5, 9);
                    Tube(p.S, MS.Accent, new[] { hc + new Vector3(0f, r * 1.1f, 0f), hc + new Vector3(r * 0.1f, r * 1.7f, -r * 0.05f), hc + new Vector3(r * 0.35f, r * 1.95f, -r * 0.1f) }, new[] { r * 0.2f, r * 0.15f, r * 0.1f }, new[] { r * 0.2f, r * 0.15f, r * 0.1f }, 6, Vector3.up, true, true);
                    EyePair(p, hc + new Vector3(r * 0.5f, r * 0.25f, r * 1.12f), r * 0.28f, new Vector3(0.2f, 0f, 1f), 0.2f, 0.4f);
                    Ell(p.G, MS.Ember, hc + new Vector3(0f, -r * 0.45f, r * 1.1f), new Vector3(r * 0.7f, r * 0.16f, r * 0.12f), 3, 10);
                    for (int k = -2; k <= 2; k++) Spike(p.S, MS.Dark, hc + new Vector3(k * r * 0.28f, -r * 0.3f, r * 1.12f), hc + new Vector3(k * r * 0.28f, -r * 0.62f * (k % 2 == 0 ? 1f : 0.7f), r * 1.12f), r * 0.09f, 4);
                    return;
                }
                Ell(p.S, MS.Skin, hc + new Vector3(0f, r * 0.05f, -r * 0.05f), new Vector3(r * 1.0f, r * 0.92f, r * 1.0f), 6, 12);
                Ell(p.S, MS.Skin, hc + new Vector3(0f, -r * 0.5f, r * 0.45f), new Vector3(r * 0.95f, r * 0.55f, r * 0.82f), 5, 12);            // rahang berat
                Ell(p.S, MS.Skin, hc + new Vector3(0f, -r * 0.1f, r * 1.0f), new Vector3(r * 0.28f, r * 0.2f, r * 0.22f), 4, 8);               // hidung
                Tube(p.S, stone ? MS.Rock : MS.ClothA, new[] { hc + new Vector3(-r * 0.95f, r * 0.3f, r * 0.55f), hc + new Vector3(0f, r * 0.34f, r * 0.98f), hc + new Vector3(r * 0.95f, r * 0.3f, r * 0.55f) }, new[] { r * 0.12f, r * 0.12f, r * 0.12f }, new[] { r * 0.13f, r * 0.13f, r * 0.13f }, 6, Vector3.up, true, true);   // alis tebal
                EyePair(p, hc + new Vector3(r * 0.38f, r * 0.14f, r * 0.82f), r * 0.14f, new Vector3(0.12f, 0f, 1f), 0.5f, 0.6f);
                for (int q = -1; q <= 1; q += 2)
                {
                    Horn(p.S, MS.Bone, hc + new Vector3(r * 0.55f * q, -r * 0.55f, r * 0.9f), hc + new Vector3(r * 0.7f * q, r * 0.2f, r * 1.1f), new Vector3(r * 0.1f * q, 0f, r * 0.1f), r * 0.16f);   // taring
                    Ear(p.S, MS.Skin, hc + new Vector3(r * 0.95f * q, 0f, -r * 0.1f), hc + new Vector3(r * 1.5f * q, r * 0.35f, -r * 0.25f), r * 0.28f, r * 0.07f, Vector3.forward, 5);
                }
                if (demon || chief) for (int q = -1; q <= 1; q += 2) Horn(p.S, demon ? MS.Dark : MS.Bone, hc + new Vector3(r * 0.5f * q, r * 0.75f, 0f), hc + new Vector3(r * 1.0f * q, r * 1.9f, -r * 0.2f), new Vector3(r * 0.3f * q, r * 0.1f, 0f), r * 0.24f);
                if (moss) { Ell(p.S, MS.Hair, hc + new Vector3(-r * 0.2f, r * 0.9f, -r * 0.2f), new Vector3(r * 0.7f, r * 0.2f, r * 0.6f), 4, 9); }
                if (stone) for (int k = 0; k < 4; k++) Rock(p.S, MS.Rock, hc + new Vector3((k - 1.5f) * r * 0.4f, r * 0.85f, -r * 0.1f), new Vector3(r * 0.22f, r * 0.3f, r * 0.22f), Quaternion.identity, 11 + k, 0.2f);
            };
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W;
                Ell(p.S, MS.Skin, new Vector3(0f, L.waistY + 0.05f * H, 0.17f * W), new Vector3(0.3f * W, 0.27f * H, 0.24f * W), 5, 10);             // perut buncit
                Ell(p.S, MS.Skin, new Vector3(0f, L.chestY + 0.02f * H, -0.02f * W), new Vector3(0.28f * W, 0.2f * H, 0.19f * W), 5, 10);
                if (pumpkin)
                {
                    // overall lusuh + jerami
                    Tube(p.S, MS.ClothB, new[] { new Vector3(0f, L.hipY + 0.05f * H, 0f), new Vector3(0f, L.shY - 0.1f * H, 0f) }, new[] { 0.3f * W, 0.34f * W }, new[] { 0.22f * W, 0.2f * W }, 10, Vector3.forward, true, true);
                    for (int k = 0; k < 9; k++) { float a = k * 0.7f; Vector3 b0 = new Vector3(Mathf.Sin(a) * 0.25f * W, L.shY - 0.03f * H, Mathf.Cos(a) * 0.2f * W); Spike(p.S, MS.Accent, b0, b0 + new Vector3(Mathf.Sin(a) * 0.12f, 0.16f, -0.05f), 0.02f, 4); }
                }
                else
                {
                    Tube(p.S, MS.Leather, new[] { new Vector3(0f, L.waistY - 0.03f * H, 0f), new Vector3(0f, L.waistY + 0.05f * H, 0f) }, new[] { 0.32f * W, 0.32f * W }, new[] { 0.28f * W, 0.28f * W }, 10, Vector3.forward, true, true);
                    if (ogre) Tube(p.S, MS.ClothB, new[] { new Vector3(0f, L.hipY + 0.02f * H, 0.02f), new Vector3(0f, L.hipY - 0.3f * H, 0.04f) }, new[] { 0.3f * W, 0.33f * W }, new[] { 0.24f * W, 0.27f * W }, 10, Vector3.forward, true, true);
                }
                for (int q = -1; q <= 1; q += 2)
                {
                    if (moss) Ell(p.S, MS.Hair, new Vector3(L.sx * 0.95f * q, L.shY + 0.07f * H, -0.02f), new Vector3(0.2f * W, 0.07f * H, 0.17f * W), 4, 9);
                    if (stone) { Rock(p.S, MS.Rock, new Vector3(L.sx * 1.0f * q, L.shY + 0.04f * H, 0f), new Vector3(0.2f * W, 0.15f * H, 0.17f * W), Quaternion.identity, 5 + q, 0.22f); for (int k = 0; k < 3; k++) Ell(p.G, MS.Ember, new Vector3((0.08f + k * 0.08f) * q * W, L.chestY - 0.05f * H + k * 0.05f, 0.2f * W), new Vector3(0.02f, 0.02f, 0.01f), 3, 5); }
                    if (demon) { Spike(p.S, MS.Dark, new Vector3(L.sx * 1.1f * q, L.shY + 0.04f * H, 0f), new Vector3(L.sx * 1.25f * q, L.shY + 0.36f * H, -0.06f), 0.07f * W, 5); Ell(p.G, MS.Ember, new Vector3(0.1f * q * W, L.chestY, 0.19f * W), new Vector3(0.025f, 0.12f, 0.012f), 3, 6); }
                    if (chief) { Ell(p.S, MS.Bone, new Vector3(L.sx * 1.0f * q, L.shY + 0.03f * H, 0f), new Vector3(0.18f * W, 0.1f * H, 0.16f * W), 4, 9); }
                }
                if (moss) { for (int k = 0; k < 3; k++) { Vector3 b0 = new Vector3((k - 1) * 0.18f * W, L.chestY + 0.05f * H, -0.22f * W); Tube(p.S, MS.Hair, new[] { b0, b0 + new Vector3(0f, -0.2f, -0.03f), b0 + new Vector3(0.02f, -0.42f, -0.02f) }, new[] { 0.035f, 0.03f, 0.012f }, new[] { 0.035f, 0.03f, 0.012f }, 5, Vector3.up, true, true); } Ell(p.S, MS.ClothB, new Vector3(-0.1f * W, L.chestY + 0.15f * H, -0.24f * W), new Vector3(0.09f, 0.04f, 0.09f), 3, 7); Ell(p.S, MS.Bone, new Vector3(-0.1f * W, L.chestY + 0.11f * H, -0.24f * W), new Vector3(0.025f, 0.05f, 0.025f), 3, 6); }
                if (chief) Tube(p.S, MS.Bone, new[] { new Vector3(-0.2f * W, L.shY - 0.02f * H, 0.1f * W), new Vector3(0f, L.shY - 0.14f * H, 0.2f * W), new Vector3(0.2f * W, L.shY - 0.02f * H, 0.1f * W) }, new[] { 0.025f, 0.025f, 0.025f }, new[] { 0.025f, 0.025f, 0.025f }, 5, Vector3.up, true, true);
            };
            s.foreFn = delegate (MkPart p, BpLay L, int side)
            {
                Vector3 h = L.hand[side];
                if (side != 0) return;
                if (pumpkin)
                {
                    // sabit raksasa
                    Tube(p.S, MS.Wood, new[] { h - new Vector3(0f, 0.15f, 0.2f), h + new Vector3(0f, 0.5f, 0.55f), h + new Vector3(0f, 1.05f, 0.95f) }, new[] { 0.045f, 0.04f, 0.035f }, new[] { 0.045f, 0.04f, 0.035f }, 6, Vector3.right, true, true);
                    Tube(p.S, MS.Metal, new[] { h + new Vector3(0f, 1.05f, 0.95f), h + new Vector3(0f, 1.32f, 1.32f), h + new Vector3(0f, 1.22f, 1.75f), h + new Vector3(0f, 0.95f, 1.98f) }, new[] { 0.08f, 0.07f, 0.05f, 0.012f }, new[] { 0.015f, 0.015f, 0.012f, 0.008f }, 5, Vector3.right, true, true);
                    return;
                }
                Vector3 d = new Vector3(0f, 0.5f, 1f).normalized; float k = chief || demon ? 1.2f : 1f;
                Tube(p.S, MS.Wood, new[] { h - d * 0.25f, h + d * 0.55f * k, h + d * 1.35f * k }, new[] { 0.06f, 0.1f * k, 0.22f * k }, new[] { 0.06f, 0.1f * k, 0.22f * k }, 8, Vector3.up, true, true);
                for (int a = 0; a < 7; a++)
                {
                    float ang = a * 0.9f; Vector3 side1 = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f); Vector3 cpt = h + d * 1.2f * k;
                    Spike(p.S, demon ? MS.Dark : MS.Metal, cpt + side1 * 0.18f * k, cpt + side1 * 0.34f * k, 0.045f, 4);
                }
            };
            var L0 = Biped(c, s);
        }

        // ------------------------------------------------------------------ zombi
        static void Zombie(MkCtx c)
        {
            var col = c.col; Color skin = col[MS.Body];
            col[MS.Skin] = skin; col[MS.ClothA] = new Color(0.42f, 0.38f, 0.3f); col[MS.ClothB] = new Color(0.32f, 0.29f, 0.24f); col[MS.Hair] = new Color(0.2f, 0.45f, 0.22f);
            col[MS.Eye] = new Color(0.95f, 0.9f, 0.4f);
            var s = new BpSpec { H = 0.99f, W = 0.95f, headK = 1.05f, lean = 13f, neckFwd = 0.1f, armPose = -78f, weaponArm = false, limbK = 0.95f, stride = 24f, kneeBend = 30f, armSwing = 8f };
            s.skin = MS.Skin; s.top = MS.ClothA; s.arm = MS.ClothA; s.fore = MS.Skin; s.bottom = MS.ClothB; s.boot = MS.Skin; s.bareFeet = true; s.glove = MS.Skin; s.trim = MS.Leather;
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                Ell(p.S, MS.Skin, hc, new Vector3(r * 0.93f, r * 1.04f, r * 0.98f), 6, 12);
                Ell(p.S, MS.Pupil, hc + new Vector3(0f, -r * 0.55f, r * 0.55f), new Vector3(r * 0.34f, r * 0.34f, r * 0.2f), 4, 8);                // mulut menganga
                Ell(p.S, MS.Skin, hc + new Vector3(0f, -r * 0.92f, r * 0.2f), new Vector3(r * 0.58f, r * 0.3f, r * 0.55f), 4, 9);                      // rahang turun
                for (int t = -1; t <= 1; t++) Spike(p.S, MS.Bone, hc + new Vector3(t * r * 0.17f, -r * 0.44f, r * 0.72f), hc + new Vector3(t * r * 0.17f, -r * 0.66f, r * 0.76f), r * 0.06f, 4);
                for (int q = -1; q <= 1; q += 2) { Ell(p.S, MS.Dark, hc + new Vector3(r * 0.38f * q, r * 0.1f, r * 0.84f), new Vector3(r * 0.24f, r * 0.2f, r * 0.12f), 4, 8); Ell(p.S, MS.Skin, hc + new Vector3(r * 0.93f * q, -r * 0.05f, -r * 0.06f), new Vector3(r * 0.1f, r * 0.2f, r * 0.12f), 3, 6); }
                EyePair(p, hc + new Vector3(r * 0.38f, r * 0.1f, r * 0.9f), r * 0.11f, new Vector3(0.1f, 0f, 1f), 0.3f, 0.5f);
                for (int k = 0; k < 7; k++)
                {
                    float a = (k - 3f) * 0.4f; Vector3 b0 = hc + new Vector3(Mathf.Sin(a) * r * 0.9f, r * 0.75f, -r * 0.2f + Mathf.Cos(a) * r * 0.3f);
                    Tube(p.S, MS.Hair, new[] { b0, b0 + new Vector3(Mathf.Sin(a) * r * 0.25f, -r * 0.5f, -r * 0.5f), b0 + new Vector3(Mathf.Sin(a) * r * 0.3f, -r * 1.5f, -r * 0.6f) }, new[] { r * 0.07f, r * 0.06f, r * 0.02f }, new[] { r * 0.07f, r * 0.06f, r * 0.02f }, 4, Vector3.up, true, true);
                }
            };
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W;
                for (int k = 0; k < 5; k++)
                {
                    float x = (k - 2) * 0.095f * W; Vector3 b0 = new Vector3(x, L.waistY - 0.02f * H, 0.1f * W + (k % 2) * 0.05f);
                    Tube(p.S, MS.ClothA, new[] { b0, b0 + new Vector3(0f, -0.2f * H - (k % 3) * 0.05f, 0.03f) }, new[] { 0.045f, 0.02f }, new[] { 0.02f, 0.012f }, 4, Vector3.forward, true, true);
                }
                for (int q = -1; q <= 1; q += 2) for (int k = 0; k < 3; k++) { Vector3 b0 = new Vector3(L.sx * 0.9f * q + (k - 1) * 0.05f, L.shY - 0.04f * H, 0f); Tube(p.S, MS.Hair, new[] { b0, b0 + new Vector3(0.03f * q, -0.25f - k * 0.1f, -0.12f), b0 + new Vector3(0.05f * q, -0.5f - k * 0.1f, -0.15f) }, new[] { 0.03f, 0.028f, 0.01f }, new[] { 0.03f, 0.028f, 0.01f }, 4, Vector3.up, true, true); }
                Ell(p.S, MS.Bone, new Vector3(0.13f * W, L.chestY + 0.1f * H, 0.17f * W), new Vector3(0.05f, 0.035f, 0.025f), 3, 6);      // remis menempel
            };
            Biped(c, s);
        }

        // ------------------------------------------------------------------ rangka
        static void Skeleton(MkCtx c, bool bow)
        {
            bool king = c.V("king"), knight = c.V("knight");
            var col = c.col; Color bone = col[MS.Body];
            col[MS.Bone] = bone; col[MS.Skin] = bone; col[MS.Dark] = Dk(bone, 0.45f); col[MS.Eye] = new Color(0.4f, 1f, 0.6f);
            col[MS.Metal] = new Color(0.5f, 0.5f, 0.52f); col[MS.Leather] = new Color(0.3f, 0.22f, 0.15f);
            if (king) { col[MS.Accent] = new Color(0.95f, 0.78f, 0.25f); col[MS.ClothB] = new Color(0.35f, 0.12f, 0.45f); col[MS.Eye] = new Color(0.85f, 0.4f, 1f); }
            if (knight) { col[MS.Metal] = new Color(0.36f, 0.4f, 0.5f); col[MS.ClothB] = new Color(0.14f, 0.14f, 0.2f); col[MS.Eye] = new Color(0.4f, 0.8f, 1f); }
            var s = new BpSpec { H = 1.0f, W = 0.72f, headK = 1.0f, limbK = 0.46f, stride = 38f, kneeBend = 48f, armSwing = 30f, hollow = true };
            s.skin = MS.Bone; s.top = MS.Bone; s.arm = MS.Bone; s.fore = MS.Bone; s.bottom = MS.Bone; s.boot = MS.Bone; s.bareFeet = true; s.glove = MS.Bone; s.trim = MS.Bone;
            if (bow) s.weaponArm = false;
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                Ell(p.S, MS.Bone, hc + new Vector3(0f, r * 0.08f, -r * 0.06f), new Vector3(r * 0.92f, r * 1.0f, r * 1.0f), 6, 12);
                Ell(p.S, MS.Bone, hc + new Vector3(0f, -r * 0.62f, r * 0.34f), new Vector3(r * 0.6f, r * 0.4f, r * 0.55f), 4, 10);
                Ell(p.S, MS.Dark, hc + new Vector3(0f, -r * 0.14f, r * 0.96f), new Vector3(r * 0.1f, r * 0.16f, r * 0.08f), 3, 6);               // lubang hidung
                for (int q = -1; q <= 1; q += 2) Ell(p.S, MS.Pupil, hc + new Vector3(r * 0.38f * q, r * 0.1f, r * 0.82f), new Vector3(r * 0.27f, r * 0.25f, r * 0.14f), 4, 8);
                EyePair(p, hc + new Vector3(r * 0.38f, r * 0.1f, r * 0.9f), r * 0.14f, new Vector3(0.1f, 0f, 1f), 0.2f, 0.6f);
                for (int t = -2; t <= 2; t++) Ell(p.S, MS.Bone, hc + new Vector3(t * r * 0.15f, -r * 0.5f, r * 0.86f), new Vector3(r * 0.06f, r * 0.1f, r * 0.05f), 3, 5);
                Tube(p.S, MS.Dark, new[] { hc + new Vector3(-r * 0.5f, -r * 0.52f, r * 0.84f), hc + new Vector3(r * 0.5f, -r * 0.52f, r * 0.84f) }, new[] { r * 0.012f, r * 0.012f }, new[] { r * 0.012f, r * 0.012f }, 3, Vector3.up, false, false);
                if (king)
                {
                    Tube(p.S, MS.Accent, new[] { hc + new Vector3(-r * 0.9f, r * 0.62f, -r * 0.05f), hc + new Vector3(0f, r * 0.72f, r * 0.9f), hc + new Vector3(r * 0.9f, r * 0.62f, -r * 0.05f) }, new[] { r * 0.11f, r * 0.11f, r * 0.11f }, new[] { r * 0.09f, r * 0.09f, r * 0.09f }, 6, Vector3.up, true, true);
                    for (int k = 0; k < 7; k++) { float a = (-135f + k * 45f) * Mathf.Deg2Rad; Vector3 bp = hc + new Vector3(Mathf.Sin(a) * r * 0.92f, r * 0.66f, Mathf.Cos(a) * r * 0.92f); Spike(p.S, MS.Accent, bp, bp + new Vector3(0f, r * (k % 2 == 0 ? 0.8f : 0.55f), 0f), r * 0.1f, 5); }
                }
                if (knight)
                {
                    Ell(p.S, MS.Metal, hc + new Vector3(0f, r * 0.3f, -r * 0.1f), new Vector3(r * 1.06f, r * 0.85f, r * 1.1f), 5, 12);
                    Tube(p.S, MS.Dark, new[] { hc + new Vector3(0f, r * 1.1f, -r * 0.8f), hc + new Vector3(0f, r * 1.5f, -r * 0.1f), hc + new Vector3(0f, r * 1.1f, r * 0.7f) }, new[] { r * 0.07f, r * 0.07f, r * 0.07f }, new[] { r * 0.2f, r * 0.28f, r * 0.2f }, 5, Vector3.right, true, true);
                }
            };
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W;
                Tube(p.S, MS.Bone, new[] { new Vector3(0f, L.hipY + 0.05f * H, -0.04f * W), new Vector3(0f, L.chestY, -0.07f * W), new Vector3(0f, L.shY, -0.05f * W), new Vector3(0f, L.neckY + 0.04f * H, 0f) }, new[] { 0.032f, 0.03f, 0.03f, 0.026f }, new[] { 0.032f, 0.03f, 0.03f, 0.026f }, 6, Vector3.forward, true, true);
                for (int k = 0; k < 6; k++)
                {
                    float y = L.chestY - 0.17f * H + k * 0.062f * H; float wk = 1f - Mathf.Abs(k - 2.4f) * 0.07f;
                    EllArc(p.S, MS.Bone, new Vector3(0f, y, 0.0f), new Vector3(0.235f * W * wk, 0.06f * H, 0.17f * W * wk), Quaternion.AngleAxis(-8f, Vector3.right), 2, 12, -148f, 148f, -9f, 9f, MS.Bone, 0.9f);
                }
                Tube(p.S, MS.Bone, new[] { new Vector3(0f, L.chestY + 0.15f * H, 0.17f * W), new Vector3(0f, L.chestY - 0.13f * H, 0.19f * W) }, new[] { 0.028f, 0.02f }, new[] { 0.014f, 0.01f }, 5, Vector3.forward, true, true);
                Tube(p.S, MS.Bone, new[] { new Vector3(-L.sx * 0.95f, L.shY - 0.02f * H, 0f), new Vector3(L.sx * 0.95f, L.shY - 0.02f * H, 0f) }, new[] { 0.026f, 0.026f }, new[] { 0.026f, 0.026f }, 5, Vector3.up, true, true);
                Ell(p.S, MS.Bone, new Vector3(0f, L.hipY + 0.02f * H, 0f), new Vector3(0.2f * W, 0.09f * H, 0.12f * W), 4, 10);
                for (int q = -1; q <= 1; q += 2) Ell(p.S, MS.Bone, new Vector3(L.sx * 0.98f * q, L.shY - 0.02f * H, 0f), new Vector3(0.06f, 0.06f, 0.06f), 3, 7);
                if (bow) Tube(p.S, MS.Leather, new[] { new Vector3(0.1f * W, L.waistY + 0.1f * H, -0.12f * W), new Vector3(0.0f, L.shY + 0.06f * H, -0.16f * W) }, new[] { 0.06f, 0.065f }, new[] { 0.06f, 0.065f }, 6, Vector3.forward, true, true);
                if (king || knight) { Tube(p.S, MS.Dark, new[] { new Vector3(0f, L.waistY - 0.02f * H, 0f), new Vector3(0f, L.hipY - 0.25f * H, 0.02f) }, new[] { 0.2f * W, 0.25f * W }, new[] { 0.15f * W, 0.19f * W }, 8, Vector3.forward, true, true); }
                if (knight) for (int q = -1; q <= 1; q += 2) { Ell(p.S, MS.Metal, new Vector3(L.sx * 1.0f * q, L.shY + 0.0f, 0f), new Vector3(0.17f * W, 0.1f * H, 0.15f * W), 4, 9); }
                if (king) for (int q = -1; q <= 1; q += 2) { Ell(p.S, MS.Accent, new Vector3(L.sx * 1.0f * q, L.shY + 0.0f, 0f), new Vector3(0.15f * W, 0.08f * H, 0.14f * W), 4, 9); }
            };
            s.shinFn = delegate (MkPart p, BpLay L, int sd) { Ell(p.S, MS.Bone, L.knee[sd], new Vector3(0.062f, 0.062f, 0.062f), 3, 7); };
            s.foreFn = delegate (MkPart p, BpLay L, int side)
            {
                Vector3 h = L.hand[side]; float H = L.s.H;
                Ell(p.S, MS.Bone, L.elbow[side], new Vector3(0.05f, 0.05f, 0.05f), 3, 7);
                for (int k = -1; k <= 1; k++) Spike(p.S, MS.Bone, h + new Vector3(k * 0.02f, -0.04f, 0.03f), h + new Vector3(k * 0.022f, -0.1f, 0.05f), 0.01f, 4);
                if (side == 0 && !bow)
                {
                    if (king || knight) Sword(p.S, MS.Metal, MS.Dark, MS.Leather, h, new Vector3(0f, 0.3f, 1f), (king ? 1.5f : 1.35f) * H, 0.075f);
                    else Sword(p.S, MS.Metal, MS.Leather, MS.Leather, h, new Vector3(0f, 0.3f, 1f), 0.95f * H, 0.05f);
                }
                if (side == 1 && !bow && !king) Shield(p, h, MS.Wood, MS.Metal, MS.Metal, knight ? 0.32f : 0.27f);
                if (side == 1 && bow)
                {
                    Vector3 g = h + new Vector3(0f, 0.04f, 0.1f);
                    Tube(p.S, MS.Wood, new[] { g + new Vector3(0f, 0.62f * H, -0.06f), g + new Vector3(0f, 0.28f * H, 0.12f), g + new Vector3(0f, 0f, 0.15f), g + new Vector3(0f, -0.28f * H, 0.12f), g + new Vector3(0f, -0.62f * H, -0.06f) },
                         new[] { 0.012f, 0.022f, 0.028f, 0.022f, 0.012f }, new[] { 0.012f, 0.022f, 0.028f, 0.022f, 0.012f }, 5, Vector3.right, true, true);
                    Tube(p.S, MS.Pale, new[] { g + new Vector3(0f, 0.62f * H, -0.06f), g + new Vector3(0f, -0.62f * H, -0.06f) }, new[] { 0.005f, 0.005f }, new[] { 0.005f, 0.005f }, 3, Vector3.right, false, false);
                }
            };
            var L0 = Biped(c, s);
            if (king || knight) Cape(c, L0, MS.ClothB, 1.15f, 1.2f);
        }

        // ------------------------------------------------------------------ mumi
        static void Mummy(MkCtx c)
        {
            bool pharaoh = c.V("pharaoh");
            var col = c.col; Color wrap = col[MS.Body];
            col[MS.ClothA] = wrap; col[MS.ClothB] = Dk(wrap, 0.8f); col[MS.Skin] = Dk(wrap, 0.55f); col[MS.Eye] = pharaoh ? new Color(0.5f, 1f, 0.9f) : new Color(0.4f, 1f, 0.9f);
            col[MS.Pale] = Mix(wrap, new Color(1f, 1f, 0.95f), 0.4f);
            if (pharaoh) { col[MS.Accent] = new Color(0.95f, 0.78f, 0.25f); col[MS.Pupil] = new Color(0.1f, 0.2f, 0.5f); }
            var s = new BpSpec { H = 1.02f, W = 1.0f, headK = 1.0f, lean = 8f, neckFwd = 0.06f, armPose = -52f, weaponArm = false, stride = 26f, kneeBend = 26f, armSwing = 10f };
            s.skin = MS.ClothB; s.top = MS.ClothA; s.arm = MS.ClothA; s.fore = MS.ClothA; s.bottom = MS.ClothB; s.boot = MS.ClothA; s.glove = MS.ClothB; s.trim = MS.Pale;
            if (pharaoh) { s.W = 1.12f; s.H = 1.06f; s.shoulderK = 1.1f; }
            s.headFn = delegate (MkPart p, BpLay L)
            {
                Vector3 hc = L.hc; float r = L.hr;
                Ell(p.S, MS.ClothA, hc, new Vector3(r * 0.97f, r * 1.07f, r * 1.02f), 6, 12);
                Ell(p.S, MS.ClothA, hc + new Vector3(0f, -r * 0.55f, r * 0.2f), new Vector3(r * 0.66f, r * 0.5f, r * 0.62f), 5, 10);
                for (int k = 0; k < 4; k++) Tube(p.S, k % 2 == 0 ? MS.Pale : MS.ClothB, new[] { hc + new Vector3(0f, -r * 0.6f + k * r * 0.5f, 0f), hc + new Vector3(0f, -r * 0.55f + k * r * 0.5f, 0.0f) }, new[] { r * (0.9f - Mathf.Abs(k - 1.5f) * 0.05f), r * 0.9f }, new[] { r * 0.98f, r * 0.98f }, 10, Vector3.forward, true, true);
                Ell(p.S, MS.Pupil, hc + new Vector3(0f, r * 0.1f, r * 0.9f), new Vector3(r * 0.62f, r * 0.17f, r * 0.12f), 3, 8);                   // celah di perban
                EyePair(p, hc + new Vector3(r * 0.3f, r * 0.1f, r * 0.96f), r * 0.14f, new Vector3(0.1f, 0f, 1f), 0.3f, 0.5f);
                for (int k = 0; k < 3; k++) Tube(p.S, MS.Pale, new[] { hc + new Vector3(r * (0.7f + k * 0.12f), -r * 0.2f, -r * 0.1f), hc + new Vector3(r * (1.0f + k * 0.2f), -r * 0.9f - k * r * 0.4f, -r * 0.3f), hc + new Vector3(r * (0.95f + k * 0.25f), -r * 1.6f - k * r * 0.5f, -r * 0.25f) }, new[] { r * 0.1f, r * 0.09f, r * 0.03f }, new[] { r * 0.03f, r * 0.03f, r * 0.02f }, 4, Vector3.up, true, true);
                if (pharaoh)
                {
                    // nemes: kain bergaris di sisi kepala + dahi emas + ular (uraeus)
                    for (int q = -1; q <= 1; q += 2)
                    {
                        Tube(p.S, MS.Accent, new[] { hc + new Vector3(r * 0.95f * q, r * 0.35f, r * 0.1f), hc + new Vector3(r * 1.25f * q, -r * 0.5f, r * 0.2f), hc + new Vector3(r * 1.3f * q, -r * 1.3f, r * 0.3f) }, new[] { r * 0.35f, r * 0.4f, r * 0.45f }, new[] { r * 0.14f, r * 0.14f, r * 0.14f }, 6, Vector3.forward, true, true);
                        Tube(p.S, MS.Pupil, new[] { hc + new Vector3(r * 1.0f * q, r * 0.25f, r * 0.35f), hc + new Vector3(r * 1.3f * q, -r * 0.6f, r * 0.45f) }, new[] { r * 0.05f, r * 0.05f }, new[] { r * 0.05f, r * 0.05f }, 4, Vector3.forward, false, false);
                    }
                    EllArc(p.S, MS.Accent, hc + new Vector3(0f, r * 0.1f, -r * 0.05f), new Vector3(r * 1.12f, r * 1.22f, r * 1.1f), Quaternion.identity, 4, 14, 55f, 305f, 10f, 80f, MS.Dark, 0.95f);
                    Tube(p.S, MS.Accent, new[] { hc + new Vector3(0f, r * 0.62f, r * 0.95f), hc + new Vector3(0f, r * 1.05f, r * 1.1f), hc + new Vector3(0f, r * 1.3f, r * 1.0f) }, new[] { r * 0.1f, r * 0.08f, r * 0.05f }, new[] { r * 0.1f, r * 0.08f, r * 0.05f }, 5, Vector3.right, true, true);
                    Tube(p.S, MS.Accent, new[] { hc + new Vector3(0f, -r * 1.2f, r * 0.45f), hc + new Vector3(0f, -r * 1.9f, r * 0.55f) }, new[] { r * 0.12f, r * 0.08f }, new[] { r * 0.06f, r * 0.05f }, 5, Vector3.up, true, true);   // janggut palsu
                }
            };
            s.torsoFn = delegate (MkPart p, BpLay L)
            {
                float H = L.s.H, W = L.s.W; Vector3[] tp; float[] trx, tr;
                TorsoTube(L, 0.012f, out tp, out trx, out tr);
                for (int k = 0; k < 7; k++)
                {
                    float t = (k + 0.5f) / 7f; int seg = Mathf.Min(2, (int)(t * 3f)); float y = Mathf.Lerp(tp[0].y, tp[3].y, t);
                    float rx = Mathf.Lerp(trx[0], trx[3], t) + 0.0f, rz = Mathf.Lerp(tr[0], tr[3], t);
                    float k2 = (t > 0.35f && t < 0.75f) ? 1.35f : 1f;
                    Tube(p.S, k % 2 == 0 ? MS.Pale : MS.ClothB, new[] { new Vector3(0f, y - 0.02f, 0f), new Vector3(0f, y + 0.02f, 0f) }, new[] { rx * 1.0f * k2 * 0.8f + 0.02f, rx * 0.8f * k2 + 0.02f }, new[] { rz * 0.8f * k2 + 0.04f, rz * 0.8f * k2 + 0.04f }, 9, Vector3.forward, false, false);
                }
                for (int k = 0; k < 4; k++)
                {
                    Vector3 b0 = new Vector3((k - 1.5f) * 0.12f * W, L.waistY + 0.1f * H, 0.15f * W);
                    Tube(p.S, MS.Pale, new[] { b0, b0 + new Vector3(0.02f, -0.3f - k * 0.08f, 0.04f), b0 + new Vector3(0.04f, -0.6f - k * 0.1f, 0.02f) }, new[] { 0.05f, 0.045f, 0.02f }, new[] { 0.012f, 0.012f, 0.01f }, 5, Vector3.forward, true, true);
                }
                if (pharaoh)
                {
                    Tube(p.S, MS.Accent, new[] { new Vector3(-0.26f * W, L.shY - 0.02f * H, 0.04f * W), new Vector3(0f, L.shY - 0.2f * H, 0.19f * W), new Vector3(0.26f * W, L.shY - 0.02f * H, 0.04f * W) }, new[] { 0.06f, 0.06f, 0.06f }, new[] { 0.03f, 0.03f, 0.03f }, 6, Vector3.up, true, true);
                    Ell(p.S, MS.Accent, new Vector3(0f, L.shY - 0.2f * H, 0.2f * W), new Vector3(0.08f, 0.08f, 0.03f), 3, 8);
                    Tube(p.S, MS.Accent, new[] { new Vector3(0f, L.waistY - 0.02f * H, 0f), new Vector3(0f, L.waistY + 0.05f * H, 0f) }, new[] { 0.2f * W, 0.2f * W }, new[] { 0.15f * W, 0.15f * W }, 10, Vector3.forward, true, true);
                }
            };
            s.upperFn = delegate (MkPart p, BpLay L, int sd) { for (int k = 0; k < 3; k++) { Vector3 e = Vector3.Lerp(L.shoulder[sd], L.elbow[sd], 0.25f + k * 0.28f); Tube(p.S, k % 2 == 0 ? MS.Pale : MS.ClothB, new[] { e - new Vector3(0f, 0.025f, 0f), e + new Vector3(0f, 0.025f, 0f) }, new[] { 0.075f, 0.075f }, new[] { 0.075f, 0.075f }, 7, Vector3.forward, false, false); } };
            s.foreFn = delegate (MkPart p, BpLay L, int sd)
            {
                for (int k = 0; k < 3; k++) { Vector3 e = Vector3.Lerp(L.elbow[sd], L.wrist[sd], 0.2f + k * 0.3f); Tube(p.S, k % 2 == 1 ? MS.Pale : MS.ClothB, new[] { e - new Vector3(0f, 0.025f, 0.01f), e + new Vector3(0f, 0.025f, 0.01f) }, new[] { 0.062f, 0.062f }, new[] { 0.062f, 0.062f }, 7, Vector3.forward, false, false); }
                Vector3 w = L.wrist[sd]; Tube(p.S, MS.Pale, new[] { w, w + new Vector3(0.03f * (sd == 0 ? 1 : -1), -0.34f, 0.05f), w + new Vector3(0.05f * (sd == 0 ? 1 : -1), -0.62f, 0.02f) }, new[] { 0.035f, 0.03f, 0.012f }, new[] { 0.01f, 0.01f, 0.008f }, 4, Vector3.forward, true, true);
            };
            s.thighFn = delegate (MkPart p, BpLay L, int sd) { for (int k = 0; k < 3; k++) { Vector3 e = Vector3.Lerp(L.hip[sd], L.knee[sd], 0.2f + k * 0.3f); Tube(p.S, k % 2 == 0 ? MS.Pale : MS.ClothB, new[] { e - new Vector3(0f, 0.025f, 0f), e + new Vector3(0f, 0.025f, 0f) }, new[] { 0.128f, 0.128f }, new[] { 0.128f, 0.128f }, 8, Vector3.forward, false, false); } };
            Biped(c, s);
        }

        // ------------------------------------------------------------------ boneka latihan
        static void Dummy(MkCtx c)
        {
            var col = c.col;
            col[MS.Wood] = new Color(0.42f, 0.28f, 0.15f); col[MS.ClothA] = new Color(0.84f, 0.72f, 0.4f); col[MS.ClothB] = new Color(0.72f, 0.2f, 0.18f); col[MS.Leather] = new Color(0.3f, 0.2f, 0.12f);
            var root = c.New("dummy", null, new Vector3(0f, 0f, 0f), delegate (MkPart p)
            {
                Tube(p.S, MS.Wood, new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 1.1f, 0f), new Vector3(0f, 1.9f, 0f) }, new[] { 0.1f, 0.09f, 0.08f }, new[] { 0.1f, 0.09f, 0.08f }, 8, Vector3.forward, true, true);
                Tube(p.S, MS.Wood, new[] { new Vector3(-0.1f, 0.06f, -0.55f), new Vector3(0.1f, 0.06f, 0.55f) }, new[] { 0.07f, 0.07f }, new[] { 0.07f, 0.07f }, 6, Vector3.up, true, true);
                Tube(p.S, MS.Wood, new[] { new Vector3(-0.55f, 0.06f, -0.1f), new Vector3(0.55f, 0.06f, 0.1f) }, new[] { 0.07f, 0.07f }, new[] { 0.07f, 0.07f }, 6, Vector3.up, true, true);
                Tube(p.S, MS.Wood, new[] { new Vector3(-0.72f, 1.5f, 0f), new Vector3(0f, 1.5f, 0f), new Vector3(0.72f, 1.5f, 0f) }, new[] { 0.06f, 0.06f, 0.06f }, new[] { 0.06f, 0.06f, 0.06f }, 6, Vector3.up, true, true);
                Tube(p.S, MS.ClothA, new[] { new Vector3(0f, 0.95f, 0f), new Vector3(0f, 1.2f, 0f), new Vector3(0f, 1.45f, 0f) }, new[] { 0.2f, 0.32f, 0.17f }, new[] { 0.17f, 0.26f, 0.15f }, 10, Vector3.forward, true, true);
                Tube(p.S, MS.Wood, new[] { new Vector3(0f, 1.5f, 0f), new Vector3(0f, 1.7f, 0f) }, new[] { 0.06f, 0.06f }, new[] { 0.06f, 0.06f }, 6, Vector3.forward, true, true);
                Ell(p.S, MS.ClothA, new Vector3(0f, 1.95f, 0f), new Vector3(0.25f, 0.27f, 0.25f), 5, 10);
                Tube(p.S, MS.ClothB, new[] { new Vector3(-0.3f, 1.22f, 0.1f), new Vector3(0f, 1.2f, 0.27f), new Vector3(0.3f, 1.22f, 0.1f) }, new[] { 0.04f, 0.04f, 0.04f }, new[] { 0.03f, 0.03f, 0.03f }, 5, Vector3.up, true, true);
                Tube(p.S, MS.ClothB, new[] { new Vector3(-0.23f, 1.93f, 0.05f), new Vector3(0f, 1.95f, 0.24f), new Vector3(0.23f, 1.93f, 0.05f) }, new[] { 0.04f, 0.04f, 0.04f }, new[] { 0.025f, 0.025f, 0.025f }, 5, Vector3.up, true, true);
                for (int q = -1; q <= 1; q += 2) { Ell(p.S, MS.Pupil, new Vector3(0.09f * q, 2.0f, 0.22f), new Vector3(0.03f, 0.03f, 0.02f), 3, 6); for (int k = 0; k < 3; k++) { Vector3 b0 = new Vector3(0.7f * q, 1.5f, 0f); Spike(p.S, MS.ClothA, b0 + new Vector3(0f, -0.02f * k, 0.03f * (k - 1)), b0 + new Vector3(0.22f * q, -0.12f - 0.03f * k, 0.1f * (k - 1)), 0.03f, 4); } }
                Tube(p.S, MS.Leather, new[] { new Vector3(0f, 1.43f, 0.0f), new Vector3(0f, 1.5f, 0.0f) }, new[] { 0.19f, 0.19f }, new[] { 0.16f, 0.16f }, 8, Vector3.forward, true, true);
            });
        }
    }
}
