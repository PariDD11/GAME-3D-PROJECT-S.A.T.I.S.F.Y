using System.Collections.Generic;
using UnityEngine;

public static partial class NpcKit
{
    public class WeaponModel
    {
        public string id, kind;           // kind: sword, bow, shield, arrow
        public Mesh mesh;
        public Material[] mats;
        public float length;
    }

    static readonly Dictionary<string, WeaponModel> weaponCache = new Dictionary<string, WeaponModel>();

    public static string WeaponKind(string itemId)
    {
        EliteInfo ei;
        if (EliteParse(itemId, out ei)) return EliteKind(ei.job);
        itemId = HunterGuild.GearTiers.VisualBase(itemId);
        switch (itemId)
        {
            case "hunter_blade": case "iron_sword": case "knight_sword": return "sword";
            case "bone_bow": case "hunters_bow": case "long_bow": return "bow";
            case "ember_rod": case "flame_staff": case "inferno_staff": case "oak_staff": case "stone_staff": case "terra_staff":
            case "tide_rod": case "frost_staff": case "abyss_staff": case "breeze_rod": case "gale_staff": case "tempest_staff": case "prayer_rod": case "sun_staff": case "seraph_staff": return "staff";
        }
        return null;
    }

    static Material[] WeaponSlots(string id)
    {
        var a = new Material[Sl.Count];
        var basis = Slots(new Spec());
        for (int i = 0; i < a.Length; i++) a[i] = basis[i];
        a[Sl.Metal] = M("WSteel", C(.78f, .80f, .84f), 0.75f, 0.8f);
        a[Sl.Hair] = M("WBronze", C(.78f, .54f, .30f), 0.6f, 0.7f);          // perunggu
        a[Sl.Eye] = M("WBone", C(.93f, .90f, .80f), 0.15f);                 // tulang
        a[Sl.Lip] = M("WCord", C(.62f, .16f, .16f), 0.1f);                  // lilitan merah
        a[Sl.Leather] = M("WLeather", C(.34f, .22f, .14f), 0.2f);
        a[Sl.Wood] = M("WWoodDark", C(.30f, .20f, .12f), 0.2f);
        a[Sl.WoodLight] = M("WWoodLight", C(.58f, .42f, .25f), 0.25f);
        a[Sl.CoatA] = M("WEmblemRed", C(.65f, .13f, .15f), 0.15f);
        a[Sl.CoatC] = M("WGem", C(.25f, .55f, .95f), 0.9f);
        a[Sl.Dark] = M("WDark", C(.12f, .12f, .14f), 0.4f);
        a[Sl.CoatB] = M("WGemGreen", C(.35f, .8f, .3f), 0.9f);
        return a;
    }

    /// <summary>Model senjata dalam kerangka tangan: pegangan di titik asal, bilah/busur memanjang ke +Y, muka bilah menghadap +Z.</summary>
    public static WeaponModel Weapon(string id)
    {
        WeaponModel w;
        EliteInfo ei;
        if (EliteParse(id, out ei))
        {
            if (weaponCache.TryGetValue(id, out w) && w.mesh != null) return w;
            return EliteWeapon(id, ei);
        }
        id = HunterGuild.GearTiers.VisualBase(id);
        if (weaponCache.TryGetValue(id, out w) && w.mesh != null) return w;
        var b = new GeoBag();
        w = new WeaponModel { id = id };
        switch (id)
        {
            case "hunter_blade": w.kind = "sword"; Sword(b, 0.56f, 0.07f, Sl.Hair, Sl.Eye, Sl.Lip, false); w.length = 0.70f; break;
            case "iron_sword": w.kind = "sword"; Sword(b, 0.78f, 0.07f, Sl.Metal, Sl.Gold, Sl.Leather, false); w.length = 0.95f; break;
            case "knight_sword": w.kind = "sword"; Sword(b, 0.94f, 0.09f, Sl.Metal, Sl.Gold, Sl.CoatA, true); w.length = 1.12f; break;
            case "bone_bow": w.kind = "bow"; Bow(b, Sl.Eye, Sl.Lip, 0.62f); w.length = 1.24f; break;
            case "hunters_bow": w.kind = "bow"; Bow(b, Sl.WoodLight, Sl.Leather, 0.66f); w.length = 1.32f; break;
            case "long_bow": w.kind = "bow"; Bow(b, Sl.Wood, Sl.Gold, 0.74f); w.length = 1.48f; break;
            case "ember_rod": w.kind = "staff"; Staff(b, true, 0); w.length = 1.1f; break;
            case "flame_staff": w.kind = "staff"; Staff(b, true, 1); w.length = 1.35f; break;
            case "inferno_staff": w.kind = "staff"; Staff(b, true, 2); w.length = 1.5f; break;
            case "oak_staff": w.kind = "staff"; Staff(b, false, 0); w.length = 1.1f; break;
            case "stone_staff": w.kind = "staff"; Staff(b, false, 1); w.length = 1.35f; break;
            case "terra_staff": w.kind = "staff"; Staff(b, false, 2); w.length = 1.5f; break;
            case "tide_rod": w.kind = "staff"; Staff(b, false, 0, Sl.Water); w.length = 1.1f; break;
            case "frost_staff": w.kind = "staff"; Staff(b, false, 1, Sl.Water); w.length = 1.35f; break;
            case "abyss_staff": w.kind = "staff"; Staff(b, false, 2, Sl.Water); w.length = 1.5f; break;
            case "breeze_rod": w.kind = "staff"; Staff(b, false, 0, Sl.Glass); w.length = 1.1f; break;
            case "gale_staff": w.kind = "staff"; Staff(b, false, 1, Sl.Glass); w.length = 1.35f; break;
            case "tempest_staff": w.kind = "staff"; Staff(b, false, 2, Sl.Glass); w.length = 1.5f; break;
            case "prayer_rod": w.kind = "staff"; Staff(b, true, 0, Sl.Glow); w.length = 1.1f; break;
            case "sun_staff": w.kind = "staff"; Staff(b, true, 1, Sl.Glow); w.length = 1.35f; break;
            case "seraph_staff": w.kind = "staff"; Staff(b, true, 2, Sl.Glow); w.length = 1.5f; break;
            case "@shield": w.kind = "shield"; Shield(b); w.length = 0.5f; break;
            case "@arrow": w.kind = "arrow"; Arrow(b); w.length = 0.7f; break;
            default: w.kind = "sword"; Sword(b, 0.5f, 0.06f, Sl.Metal, Sl.Gold, Sl.Leather, false); w.length = 0.65f; break;
        }
        int[] used;
        w.mesh = b.ToMesh("W_" + id, null, out used);
        var all = WeaponSlots(id);
        w.mats = new Material[used.Length];
        for (int i = 0; i < used.Length; i++) w.mats[i] = all[used[i]];
        weaponCache[id] = w;
        return w;
    }

    static void TriBoth(GeoBag b, int slot, Vector3 p, Vector3 q, Vector3 r) { b.Tri(slot, p, q, r); b.Tri(slot, p, r, q); }

    static Vector3[] BladeRing(float y, float w, float th)
    {
        return new[] { new Vector3(w, y, 0f), new Vector3(w * 0.30f, y, th), new Vector3(-w * 0.30f, y, th), new Vector3(-w, y, 0f), new Vector3(-w * 0.30f, y, -th), new Vector3(w * 0.30f, y, -th) };
    }

    static void Sword(GeoBag b, float len, float wid, int blade, int guard, int wrap, bool grand)
    {
        float y0 = 0.075f, hw = wid * 0.5f;
        b.Cyl(Sl.Leather, new Vector3(0f, -0.09f, 0f), 0.018f, 0.17f, 8, 0.018f, true, true);                    // gagang
        for (int i = 0; i < 5; i++) b.Cyl(wrap, new Vector3(0f, -0.088f + i * 0.034f, 0f), 0.0196f, 0.012f, 8, 0.0196f, false, false);   // lilitan
        b.Ball(guard, new Vector3(0f, -0.108f, 0f), 0.028f, 3, 8, 0.85f);                                          // pomel
        b.Cyl(guard, new Vector3(0f, -0.097f, 0f), 0.025f, 0.010f, 10, 0.025f, true, true);
        if (grand) b.Ball(Sl.CoatC, new Vector3(0f, -0.118f, 0f), 0.013f, 3, 6);
        float gw = grand ? 0.108f : 0.084f, dr = grand ? 0.024f : 0.013f;                                          // pelindung melengkung
        Sweep(b, guard, new[] { new Vector3(-gw, y0 - dr, 0f), new Vector3(-gw * 0.5f, y0 - 0.004f, 0f), new Vector3(gw * 0.5f, y0 - 0.004f, 0f), new Vector3(gw, y0 - dr, 0f) },
            new[] { 0.008f, 0.013f, 0.013f, 0.008f }, new[] { 0.008f, 0.011f, 0.011f, 0.008f }, Vector3.forward, 8, 3);
        b.Box(guard, new Vector3(0f, y0 + 0.012f, 0f), new Vector3(0.034f, 0.026f, 0.030f));                       // pangkal bilah
        if (grand) b.Ball(Sl.CoatC, new Vector3(0f, y0, 0.017f), 0.015f, 3, 6);
        float yb = y0 + 0.020f, yt = yb + len, th0 = 0.0075f;
        var rs = new List<Vector3[]>();
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f;
            rs.Add(BladeRing(yb + t * (len - wid * 1.7f), hw * (1f - 0.22f * t), th0 * (1f - 0.35f * t)));
        }
        rs.Add(BladeRing(yt - wid * 0.62f, hw * 0.66f, 0.0043f));                                                  // ujung runcing
        rs.Add(BladeRing(yt - wid * 0.14f, hw * 0.20f, 0.0017f));
        rs.Add(BladeRing(yt, 0.0006f, 0.0006f));
        NLoft(b, blade, rs, true, true, false);
        float yA = yb + len * 0.06f, yB = yb + len * 0.80f;
        float zA = th0 * (1f - 0.35f * 0.06f) + 0.0004f, zB = th0 * (1f - 0.35f * 0.80f) + 0.0004f;
        for (int sg = -1; sg <= 1; sg += 2)                                                                        // alur darah di kedua sisi
        {
            b.Beam(Sl.Dark, new Vector3(0f, yA, sg * zA), new Vector3(0f, yB, sg * zB), wid * 0.17f, 0.0012f, Vector3.forward);
            if (grand) b.Beam(Sl.Gold, new Vector3(0f, yA, sg * (zA + 0.0006f)), new Vector3(0f, yB, sg * (zB + 0.0006f)), wid * 0.05f, 0.0012f, Vector3.forward);
        }
    }

    static void Bow(GeoBag b, int limb, int wrap, float half)
    {
        const int m = 7;
        Vector3[] tips = new Vector3[2];
        for (int sg = -1; sg <= 1; sg += 2)
        {
            var pts = new Vector3[m]; var w = new float[m]; var th = new float[m];
            for (int i = 0; i < m; i++)
            {
                float t = i / (float)(m - 1), cu = Mathf.Max(0f, (t - 0.80f) / 0.20f);
                pts[i] = new Vector3(0f, sg * half * t, -0.20f * t * t + 0.045f * cu * cu);                            // lengkung + ujung recurve ke depan
                w[i] = 0.015f * (1f - 0.55f * t); th[i] = 0.021f * (1f - 0.62f * t);
            }
            Sweep(b, limb, pts, w, th, Vector3.right, 8, 3);
            tips[sg > 0 ? 1 : 0] = pts[m - 1];
            b.Ball(wrap == Sl.Gold ? Sl.Gold : Sl.Leather, pts[m - 1], 0.011f, 3, 6);                                  // nock
            b.Ball(wrap == Sl.Gold ? Sl.Gold : Sl.Leather, pts[m - 3] + new Vector3(0f, 0f, 0f), 0.017f - 0.002f * sg * 0f, 3, 6, 0.55f);   // ikat hias
        }
        b.Cyl(wrap, new Vector3(0f, -0.075f, 0f), 0.024f, 0.15f, 10, 0.024f, true, true);                         // pegangan berlilit
        for (int i = 0; i < 3; i++) b.Cyl(Sl.Dark, new Vector3(0f, -0.06f + i * 0.045f, 0f), 0.0255f, 0.008f, 10, 0.0255f, false, false);
        b.Box(Sl.Dark, new Vector3(0f, 0.085f, 0.010f), new Vector3(0.020f, 0.030f, 0.034f));                      // sandaran anak panah
        b.Beam(Sl.EyeW, tips[1], tips[0], 0.006f, 0.006f);                                                         // tali busur
    }

    static Vector3[] HexRing(float y, float r, float rot = 0f)
    {
        var p = new Vector3[6];
        for (int i = 0; i < 6; i++) { float a = rot + i * Mathf.PI / 3f; p[i] = new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r); }
        return p;
    }

    /// <summary>Tongkat sihir: tiang berlilit dengan cakar emas melengkung memegang bola bara (api) atau kristal berfaset; tier 2 megah (halo + serpihan melayang).</summary>
    static void Staff(GeoBag b, bool fire, int tier, int gemSlot = -1)
    {
        int gem = gemSlot >= 0 ? gemSlot : (fire ? Sl.CoatA : Sl.CoatB);
        float len = 0.9f + tier * 0.22f, top = len - 0.05f;
        int claw = tier >= 1 ? Sl.Gold : Sl.Wood;
        Sweep(b, Sl.WoodLight, new[] { new Vector3(0f, -0.25f, 0f), new Vector3(0f, 0.30f, 0.004f), new Vector3(0f, top - 0.2f, -0.004f), new Vector3(0f, top, 0f) },
            new[] { 0.019f, 0.021f, 0.020f, 0.024f }, new[] { 0.019f, 0.021f, 0.020f, 0.024f }, Vector3.right, 8, 4);                  // tiang sedikit meliuk
        for (int i = 0; i < 3; i++) b.Cyl(Sl.Leather, new Vector3(0f, -0.07f + i * 0.045f, 0f), 0.026f, 0.026f, 8, 0.026f, false, false);   // lilitan
        b.Cyl(claw, new Vector3(0f, top - 0.012f, 0f), 0.032f, 0.026f, 8, 0.024f, true, true);                                              // kerah kepala
        b.Ball(Sl.Gold, new Vector3(0f, -0.26f, 0f), 0.02f, 3, 6);                                                                           // ujung bawah
        float oy = top + 0.115f;
        for (int i = 0; i < 4; i++)       // cakar melengkung
        {
            float a = i * 90f * Mathf.Deg2Rad + 0.4f, sa = Mathf.Sin(a), ca = Mathf.Cos(a);
            Sweep(b, claw, new[] { new Vector3(0f, top, 0f), new Vector3(sa * 0.050f, top + 0.045f, ca * 0.050f), new Vector3(sa * 0.062f, top + 0.115f, ca * 0.062f), new Vector3(sa * 0.030f, top + 0.185f, ca * 0.030f) },
                new[] { 0.011f, 0.011f, 0.009f, 0.0024f }, new[] { 0.008f, 0.008f, 0.007f, 0.0024f }, Vector3.up, 6, 2);
        }
        if (fire)
        {
            float r = 0.062f + tier * 0.010f;
            b.Ball(gem, new Vector3(0f, oy, 0f), r, 5, 10);
            b.Cone(Sl.Glow, new Vector3(0f, oy + r * 0.7f, 0f), r * 0.55f, r * 1.25f, 6);                                                    // lidah api
            if (tier >= 1) for (int i = 0; i < 3; i++) { float a = i * 120f * Mathf.Deg2Rad; b.Ball(Sl.Gold, new Vector3(Mathf.Sin(a) * (r + 0.02f), oy + 0.02f, Mathf.Cos(a) * (r + 0.02f)), 0.014f, 3, 5); }
        }
        else
        {
            float h = 0.085f + tier * 0.02f, rw = 0.040f + tier * 0.004f;       // kristal berfaset enam sisi
            var rs = new List<Vector3[]> { HexRing(oy - h * 0.95f, 0.002f), HexRing(oy - h * 0.35f, rw), HexRing(oy + h * 0.30f, rw, 0.2f), HexRing(oy + h * 1.15f, 0.002f, 0.2f) };
            NLoft(b, gem, rs, true, true, false);
            if (tier >= 1) { b.Box(Sl.Stone, new Vector3(0.075f, top + 0.01f, 0f), new Vector3(0.04f, 0.07f, 0.04f), 20f); b.Box(Sl.Stone, new Vector3(-0.075f, top + 0.03f, 0f), new Vector3(0.035f, 0.06f, 0.035f), -25f); }
        }
        if (tier >= 2)       // halo + serpihan melayang
        {
            var pr = new Vector3[11]; var pw = new float[11]; var pt = new float[11];
            for (int i = 0; i < 11; i++) { float a = i / 10f * Mathf.PI * 2f; pr[i] = new Vector3(Mathf.Sin(a) * 0.105f, oy + Mathf.Cos(a) * 0.105f, 0f); pw[i] = 0.007f; pt[i] = 0.007f; }
            Sweep(b, Sl.Gold, pr, pw, pt, Vector3.forward, 6, 2);
            for (int i = 0; i < 3; i++) { float a = i * 120f * Mathf.Deg2Rad + 0.5f; b.Ball(gem, new Vector3(Mathf.Sin(a) * 0.14f, oy + 0.03f * (i - 1), Mathf.Cos(a) * 0.14f), 0.014f, 3, 5); }
        }
    }

    static float ShieldH(float r) { float q = r / 0.22f; return 0.040f * (1f - q * q); }

    static List<Vector3[]> ShieldRings(float r0, float r1, int n, int k, float lift)
    {
        var rs = new List<Vector3[]>();
        for (int i = 0; i <= k; i++)
        {
            float r = Mathf.Lerp(r0, r1, i / (float)k);
            var p = new Vector3[n];
            for (int j = 0; j < n; j++) { float a = j * Mathf.PI * 2f / n; p[j] = new Vector3(Mathf.Sin(a) * r, ShieldH(r) + lift, Mathf.Cos(a) * r); }
            rs.Add(p);
        }
        return rs;
    }

    /// <summary>Perisai bulat cembung: muka menghadap +Y (diputar saat dipasang), pegangan di bawah. Cincin logam luar + bidang bercat dalam (tanpa lapisan tumpang tindih, hemat segitiga),
    /// tepi emas, salib, pusat menonjol, paku keling. Sekitar 900 segitiga.</summary>
    static void Shield(GeoBag b)
    {
        NLoft(b, Sl.Metal, ShieldRings(0.186f, 0.215f, 24, 2, 0f), false, false);                   // cincin logam luar
        NLoft(b, Sl.CoatA, ShieldRings(0.045f, 0.186f, 24, 4, 0f), false, false);                   // bidang bercat
        var back = new List<Vector3[]>();                                                           // punggung cekung
        foreach (float rr in new[] { 0.215f, 0.11f, 0.001f })
        {
            var p = new Vector3[16]; float yy = -0.004f - 0.040f * (1f - rr / 0.215f);
            for (int j = 0; j < 16; j++) { float a = j * Mathf.PI * 2f / 16f; p[j] = new Vector3(Mathf.Sin(a) * rr, yy, Mathf.Cos(a) * rr); }
            back.Add(p);
        }
        NLoft(b, Sl.Leather, back, false, true);
        var rim = new Vector3[19]; var rw = new float[19]; var rt = new float[19];                   // melewati satu putaran penuh: ujung meruncing saling menutup, tanpa celah
        for (int i = 0; i < 19; i++) { float a = i / 16f * Mathf.PI * 2f; rim[i] = new Vector3(Mathf.Sin(a) * 0.218f, 0.002f, Mathf.Cos(a) * 0.218f); rw[i] = 0.011f; rt[i] = 0.013f; }
        Sweep(b, Sl.Gold, rim, rw, rt, Vector3.up, 5, 1);                                           // bingkai emas
        for (int ax = 0; ax < 2; ax++)                                                              // salib emas melengkung mengikuti kubah
        {
            var pts = new Vector3[7]; var pw = new float[7]; var pt = new float[7];
            for (int i = 0; i < 7; i++)
            {
                float u = (i / 6f - 0.5f) * 0.38f;
                pts[i] = ax == 0 ? new Vector3(u, ShieldH(Mathf.Abs(u)) + 0.0055f, 0f) : new Vector3(0f, ShieldH(Mathf.Abs(u)) + 0.0055f, u);
                pw[i] = 0.016f; pt[i] = 0.004f;
            }
            Sweep(b, Sl.Gold, pts, pw, pt, ax == 0 ? Vector3.forward : Vector3.right, 4, 1);
        }
        b.Ball(Sl.Gold, new Vector3(0f, 0.040f, 0f), 0.048f, 3, 10, 0.55f);                         // pusat menonjol
        for (int i = 0; i < 8; i++) { float a = (i + 0.5f) / 8f * Mathf.PI * 2f; b.Ball(Sl.Gold, new Vector3(Mathf.Sin(a) * 0.197f, ShieldH(0.197f) + 0.006f, Mathf.Cos(a) * 0.197f), 0.0085f, 2, 4); }
        b.Box(Sl.Leather, new Vector3(0f, -0.07f, 0f), new Vector3(0.03f, 0.03f, 0.2f));            // pegangan
        b.Box(Sl.Metal, new Vector3(0f, -0.055f, 0.085f), new Vector3(0.036f, 0.012f, 0.03f));
        b.Box(Sl.Metal, new Vector3(0f, -0.055f, -0.085f), new Vector3(0.036f, 0.012f, 0.03f));
    }

    static void Arrow(GeoBag b)
    {
        // panah memanjang ke +Z, titik asal di ujung belakang batang
        b.Beam(Sl.Wood, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0.62f), 0.012f, 0.012f);
        Pyramid(b, Sl.Metal, new Vector3(-0.014f, -0.014f, 0.62f), new Vector3(0.014f, -0.014f, 0.62f), new Vector3(0f, 0.016f, 0.62f), new Vector3(0f, 0f, 0.70f));
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            Vector3 o = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f) * 0.03f;
            b.QuadBoth(Sl.EyeW, new Vector3(0f, 0f, 0.02f), o + new Vector3(0f, 0f, 0.02f), o + new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, 0.1f));
        }
    }
}
