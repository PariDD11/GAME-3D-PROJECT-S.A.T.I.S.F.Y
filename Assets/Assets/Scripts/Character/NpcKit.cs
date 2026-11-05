using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Karakter 3D low-poly semi realistis (proporsi anime) yang dibuat dari kode: 4 ras (Manusia, Dwarf, Elf, Beastfolk), wajah dengan mata anime,
/// rambut, telinga/ekor, dan pakaian fantasi abad pertengahan. Tiap karakter terdiri dari bagian terpisah (badan, kepala, 2 lengan, 2 kaki, ekor)
/// yang dipasang pada pivot sendiri, sehingga bisa dianimasikan (jalan, bernapas, melambai) oleh NpcAnimator.
/// Bangunan geometri ada di NpcKit.Body.cs dan NpcKit.Outfit.cs.
/// </summary>
public static partial class NpcKit
{
    public const string MeshDir = "Assets/Meshes/Npc";

    // ---- kait untuk kode Editor (agar kelas ini bisa jalan di runtime tanpa UnityEditor) ----
    public static System.Func<string, Color, float, float, Material> EditorMat;     // (nama, warna, smooth, metal)
    public static System.Func<Material[]> EditorBaseSlots;
    public static System.Action EditorEnsureDirs;
    public static Shader RuntimeShader;                                              // dipakai bila tidak ada kait Editor
    static bool UseEditor { get { return Application.isEditor && !Application.isPlaying && EditorMat != null; } }
    static Material[] runtimeBase;

    static Material RuntimeMat(string name, Color c, float smooth, float metal)
    {
        Shader sh = RuntimeShader;
        if (sh == null) sh = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh); m.name = "N_" + name; m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        return m;
    }

    static Material[] RuntimeBase()
    {
        if (runtimeBase != null && runtimeBase[0] != null) return runtimeBase;
        runtimeBase = new Material[Sl.Count];
        var gray = RuntimeMat("BaseGray", new Color(0.7f, 0.68f, 0.64f), 0.1f, 0f);
        for (int i = 0; i < Sl.Count; i++) runtimeBase[i] = gray;
        runtimeBase[Sl.Cloth] = RuntimeMat("BaseCloth", new Color(0.82f, 0.76f, 0.62f), 0.05f, 0f);
        runtimeBase[Sl.ClothB] = RuntimeMat("BaseClothB", new Color(0.7f, 0.25f, 0.22f), 0.05f, 0f);
        var glow = RuntimeMat("BaseGlow", new Color(1f, 0.78f, 0.42f), 0.3f, 0f);
        glow.EnableKeyword("_EMISSION");
        if (glow.HasProperty("_EmissionColor")) glow.SetColor("_EmissionColor", new Color(1.5f, 0.95f, 0.4f));
        runtimeBase[Sl.Glow] = glow;
        return runtimeBase;
    }

    public const int Human = 0, Dwarf = 1, Elf = 2, Beast = 3;
    public static readonly string[] RaceName = { "Manusia", "Dwarf", "Elf", "Beastfolk" };
    public static readonly string[] BeastName = { "Kucing", "Serigala", "Rubah", "Kelinci" };

    // peran / pakaian
    public const int OWarga = 0, OPedagang = 1, OPetani = 2, OBangsawan = 3, OPenjaga = 4, OKsatria = 5, OPendeta = 6, OPenyihir = 7, OHunter = 8,
                     OPandai = 9, OPelayan = 10, OBard = 11, OKoki = 12, OPelajar = 13, OPengembara = 14, OPetugas = 15, Count = 16;
    public static readonly string[] OutfitName =
    {
        "Warga", "Pedagang", "Petani", "Bangsawan", "Penjaga", "Ksatria", "Pendeta", "Penyihir", "Hunter",
        "Pandai Besi", "Pelayan", "Bard", "Koki", "Pelajar", "Pengembara", "Petugas Guild"
    };

    public class Spec
    {
        public int race, outfit, hair, hairCol, skin, eye, cA, cB, cC, beast, beard, hat, prop;
        public int armHead, armBody, armLegs, build;          // 0 tidak ada, 1.. tingkat/gaya armor (lihat NpcKit.Armor.cs)
        public bool female, elite, split;               // split = lengan dua ruas (siku) untuk animasi kombat pemain
        public string name = "NPC", role = "Warga";
        public string Key()
        {
            return race + "_" + (female ? "f" : "m") + "_o" + outfit + "_h" + hair + "c" + hairCol + "_s" + skin + "_e" + eye + "_a" + cA + "b" + cB + "c" + cC + "_k" + beast + "_d" + beard + "_t" + hat + "_p" + prop + "_bd" + build + "_ah" + armHead + "b" + armBody + "l" + armLegs + (split ? "S" : "");
        }
    }

    // ------------------------------------------------------------------ palet
    static readonly Color[][] SkinPal =
    {
        new[] { C(.97f, .81f, .69f), C(.89f, .68f, .52f), C(.74f, .54f, .40f), C(.56f, .39f, .28f) },       // manusia
        new[] { C(.92f, .70f, .57f), C(.80f, .57f, .44f), C(.68f, .47f, .36f) },                              // dwarf
        new[] { C(.98f, .90f, .83f), C(.94f, .85f, .76f), C(.82f, .72f, .60f) },                              // elf
        new[] { C(.96f, .82f, .70f), C(.88f, .68f, .54f), C(.72f, .54f, .42f) },                              // beast
    };
    static readonly Color[] HairPal =
    {
        C(.10f, .08f, .08f), C(.25f, .16f, .10f), C(.42f, .26f, .14f), C(.86f, .70f, .36f), C(.80f, .80f, .84f), C(.70f, .22f, .12f),
        C(.52f, .50f, .48f), C(.95f, .95f, .93f), C(.16f, .22f, .42f), C(.88f, .55f, .66f), C(.30f, .52f, .34f), C(.60f, .45f, .75f)
    };
    static readonly Color[] EyePal =
    {
        C(.32f, .20f, .12f), C(.25f, .55f, .30f), C(.25f, .45f, .80f), C(.55f, .35f, .78f), C(.90f, .70f, .20f), C(.80f, .22f, .20f), C(.20f, .65f, .66f)
    };
    static readonly Color[][] FurPal =
    {
        new[] { C(.86f, .56f, .28f), C(.15f, .14f, .16f), C(.93f, .92f, .88f), C(.55f, .52f, .50f) },       // kucing
        new[] { C(.50f, .50f, .54f), C(.28f, .26f, .28f), C(.78f, .76f, .72f), C(.45f, .34f, .26f) },       // serigala
        new[] { C(.88f, .46f, .16f), C(.92f, .60f, .30f), C(.95f, .92f, .86f), C(.70f, .30f, .12f) },       // rubah
        new[] { C(.95f, .94f, .92f), C(.62f, .48f, .38f), C(.82f, .80f, .84f), C(.35f, .30f, .30f) },       // kelinci
    };
    static readonly Color[] FabricPal =
    {
        C(.93f, .88f, .75f), C(.45f, .30f, .20f), C(.25f, .45f, .28f), C(.18f, .26f, .52f), C(.62f, .15f, .17f), C(.82f, .62f, .20f),
        C(.20f, .50f, .52f), C(.40f, .22f, .46f), C(.46f, .46f, .52f), C(.95f, .95f, .93f), C(.14f, .14f, .17f), C(.46f, .66f, .86f),
        C(.86f, .52f, .56f), C(.46f, .50f, .26f)
    };

    static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }

    // ------------------------------------------------------------------ nama
    static readonly string[][] Names =
    {
        new[] { "Aldric", "Mira", "Tomas", "Elena", "Garrick", "Sera", "Roland", "Lyanna", "Bram", "Isolde", "Edric", "Nora", "Hugo", "Clara", "Marek", "Anya" },
        new[] { "Thorin", "Brunhild", "Gimra", "Dorn", "Hilda", "Balin", "Torva", "Kragg", "Frida", "Durgan", "Magda", "Orik" },
        new[] { "Aelindra", "Faelar", "Lirael", "Thalion", "Sylvara", "Eldrin", "Nimue", "Caladwen", "Erevan", "Ilyana", "Vaelis", "Anariel" },
        new[] { "Miko", "Kiba", "Reina", "Tamaki", "Suzu", "Rin", "Hana", "Koji", "Nyaa", "Fenrir", "Yuzu", "Lupa" },
    };
    public static string RandomName(int race, System.Random rng) { var a = Names[race]; return a[rng.Next(a.Length)]; }

    // ------------------------------------------------------------------ spec acak berdasarkan peran
    public static Spec RandomSpec(int outfit, System.Random rng, int race = -1, int female = -1)
    {
        var s = new Spec();
        s.outfit = outfit;
        s.role = OutfitName[outfit];
        // sebaran ras: manusia 45%, elf 20%, dwarf 15%, beastfolk 20%
        if (race < 0)
        {
            int r = rng.Next(100);
            race = r < 45 ? Human : (r < 65 ? Elf : (r < 80 ? Dwarf : Beast));
            if (outfit == OPandai && rng.Next(100) < 60) race = Dwarf;
            if (outfit == OPenyihir && rng.Next(100) < 50) race = Elf;
        }
        s.race = race;
        s.female = female >= 0 ? female == 1 : rng.Next(2) == 0;
        if (outfit == OPandai && race == Dwarf) s.female = rng.Next(100) < 35;
        s.skin = rng.Next(SkinPal[race].Length);
        s.hairCol = rng.Next(HairPal.Length);
        if (race == Dwarf) s.hairCol = new[] { 0, 1, 2, 5, 6, 7, 5 }[rng.Next(7)];
        s.eye = rng.Next(EyePal.Length);
        s.beast = race == Beast ? rng.Next(4) : 0;
        s.cA = rng.Next(FabricPal.Length); s.cB = rng.Next(FabricPal.Length); s.cC = rng.Next(FabricPal.Length);
        if (s.cB == s.cA) s.cB = (s.cA + 3) % FabricPal.Length;
        if (s.cC == s.cA) s.cC = (s.cA + 5) % FabricPal.Length;
        s.hair = rng.Next(8);
        if (!s.female && (s.hair == 3 || s.hair == 4)) s.hair = rng.Next(3) == 0 ? 0 : 5;
        s.beard = 0;
        if (!s.female && (race == Human || race == Dwarf) && rng.Next(100) < (race == Dwarf ? 90 : 35)) s.beard = race == Dwarf ? 2 + rng.Next(2) : 1 + rng.Next(2);
        if (race == Dwarf && s.female) s.hair = 7;
        if (s.hair == 0 && race != Dwarf) s.hair = 9 + ((s.hairCol * 7 + s.cA * 3 + s.eye) % 3);     // gaya anime baru (tanpa menambah pemakaian rng)
        s.hat = 0; s.prop = 0;
        s.name = RandomName(race, rng);
        PickOutfit(s, rng);
        return s;
    }

    /// <summary>Atur warna kain, topi, dan properti sesuai peran.</summary>
    static void PickOutfit(Spec s, System.Random rng)
    {
        switch (s.outfit)
        {
            case OWarga: s.hat = rng.Next(100) < 25 ? 13 : 0; break;
            case OPedagang: s.hat = s.female ? (rng.Next(2) == 0 ? 2 : 15) : (rng.Next(100) < 60 ? 14 : 2); s.prop = 1; break;                               // kantong besar
            case OPetani: s.hat = 1; s.cA = 0; s.cB = 1; s.prop = 2; break;                                       // topi jerami, garpu
            case OBangsawan: s.hat = s.female ? 7 : (rng.Next(2) == 0 ? 9 : 0); s.cA = s.female ? 7 : 3; s.cB = 5; s.cC = 9; break;
            case OPenjaga: s.hat = 4; s.cA = 4; s.cB = 8; s.prop = 3; s.hair = 0; break;                          // tombak
            case OKsatria: s.hat = 4; s.cA = 3; s.cB = 5; s.cC = 4; s.prop = 4; s.elite = true; break;          // pedang + perisai
            case OPendeta: s.hat = s.female ? 6 : 10; s.cA = 9; s.cB = 5; s.cC = 9; s.prop = 5; break;           // tongkat suci
            case OPenyihir: s.hat = 3; s.cA = rng.Next(2) == 0 ? 7 : 3; s.cB = 10; s.cC = 5; s.prop = 6; break;  // tongkat kristal
            case OHunter: s.hat = rng.Next(2) == 0 ? 5 : 0; s.cA = rng.Next(2) == 0 ? 2 : 13; s.cB = 1; s.cC = 1; s.prop = 7; break;  // busur + tabung panah
            case OPandai: s.hat = 13; s.cA = 1; s.cB = 10; s.prop = 8; break;                                      // palu
            case OPelayan: s.hat = s.female ? 6 : 0; s.cA = 10; s.cB = 9; s.cC = 9; break;
            case OBard: s.hat = 11; s.cA = rng.Next(14); s.cB = 5; s.cC = 4; s.prop = 9; break;                   // kecapi
            case OKoki: s.hat = 12; s.cA = 9; s.cB = 4; s.cC = 9; break;
            case OPelajar: s.hat = 0; s.cA = 3; s.cB = 8; s.cC = 4; s.prop = 10; break;                           // buku
            case OPengembara: s.hat = rng.Next(3) == 0 ? 5 : (rng.Next(2) == 0 ? 15 : 14); s.cA = 13; s.cB = 1; s.cC = 8; s.prop = 11; break; // ransel
            case OPetugas: s.hat = 0; s.cA = 3; s.cB = 9; s.cC = 5; s.prop = 10; break;
        }
    }

    // ------------------------------------------------------------------ material
    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

    static Material M(string name, Color c, float smooth = 0.08f, float metal = 0f)
    {
        Material m;
        if (matCache.TryGetValue(name, out m) && m != null) return m;
        m = UseEditor ? EditorMat(name, c, smooth, metal) : RuntimeMat(name, c, smooth, metal);
        if (name.StartsWith("Skin") || name.StartsWith("Eye") || name.StartsWith("Iris") || name.StartsWith("Blush") || name == "Lip") Lift(m, c, name.StartsWith("Skin") || name.StartsWith("Blush") ? 0.22f : 0.30f);
        matCache[name] = m;
        return m;
    }

    // emisi lembut agar wajah/kulit tidak pernah tenggelam gelap di bayangan
    static void Lift(Material m, Color c, float k)
    {
        if (m == null) return;
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(c.r * k, c.g * k, c.b * k, 1f));
    }

    static string Hex(Color c) { return Mathf.RoundToInt(c.r * 255f).ToString("X2") + Mathf.RoundToInt(c.g * 255f).ToString("X2") + Mathf.RoundToInt(c.b * 255f).ToString("X2"); }

    public static Material[] Slots(Spec s)
    {
        var a = (Material[])(UseEditor && EditorBaseSlots != null ? EditorBaseSlots() : RuntimeBase()).Clone();
        Color sk = SkinPal[s.race][s.skin % SkinPal[s.race].Length];
        a[Sl.SkinS] = M("Skin" + Hex(sk), sk, 0.35f);
        Color hc = s.race == Beast ? FurPal[s.beast][s.skin % 4] : HairPal[s.hairCol % HairPal.Length];
        a[Sl.Hair] = M("Hair" + Hex(hc), hc, 0.5f);
        Color ec = EyePal[s.eye % EyePal.Length];
        a[Sl.Eye] = M("Eye" + Hex(ec), ec, 0.8f);
        Color fur = s.race == Beast ? FurPal[s.beast][s.skin % 4] : hc;
        a[Sl.Fur] = M("Fur" + Hex(fur), fur, 0.15f);
        Color ca = FabricPal[s.cA % FabricPal.Length], cb = FabricPal[s.cB % FabricPal.Length], cc = FabricPal[s.cC % FabricPal.Length];
        a[Sl.CoatA] = M("Fab" + Hex(ca), ca, 0.12f);
        a[Sl.CoatB] = M("Fab" + Hex(cb), cb, 0.12f);
        a[Sl.CoatC] = M("Fab" + Hex(cc), cc, 0.12f);
        a[Sl.Leather] = M("Leather", C(.36f, .24f, .15f), 0.3f);
        a[Sl.EyeW] = M("EyeWhite", C(.98f, .98f, .98f), 0.7f);
        a[Sl.Dark] = M("Dark", C(.06f, .05f, .06f), 0.6f);
        a[Sl.Lip] = M("Lip", s.female ? C(.78f, .36f, .40f) : C(.66f, .40f, .36f), 0.4f);
        a[Sl.Metal] = M("Steel", C(.66f, .68f, .72f), 0.7f, 0.8f);
        a[Sl.Gold] = M("GoldTrim", C(.92f, .72f, .24f), 0.8f, 0.9f);
        a[Sl.Wood] = M("WoodDark", C(.30f, .20f, .12f), 0.2f);
        a[Sl.WoodLight] = M("WoodLight", C(.55f, .40f, .24f), 0.2f);
        a[Sl.Thatch] = M("Straw", C(.86f, .72f, .40f), 0.1f);
        // warna turunan untuk wajah/rambut gaya anime (lihat NpcKit.Anime.cs)
        Color hHi = (hc.r + hc.g + hc.b) < 0.75f ? Mix(hc, C(.46f, .52f, .74f), 0.5f) : Mix(hc, C(1f, .97f, .92f), 0.30f);
        a[SHairHi] = M("HairHi" + Hex(hHi), hHi, 0.5f);
        Color hLo = Shade(hc, 0.62f);
        a[SHairLo] = M("HairLo" + Hex(hLo), hLo, 0.4f);
        Color iHi = Mix(ec, C(1f, 1f, 1f), 0.5f), iLo = Shade(ec, 0.52f);
        a[SIrisHi] = M("IrisHi" + Hex(iHi), iHi, 0.8f);
        a[SIrisLo] = M("IrisLo" + Hex(iLo), iLo, 0.8f);
        Color bl = Mix(sk, C(.95f, .42f, .48f), 0.36f);
        a[SBlush] = M("Blush" + Hex(bl), bl, 0.3f);
        Color sl = new Color(sk.r * 0.88f, sk.g * 0.78f, sk.b * 0.76f, 1f);
        a[SSkinLo] = M("SkinLo" + Hex(sl), sl, 0.3f);
        return a;
    }

    // ------------------------------------------------------------------ arketipe & instance
    public class Archetype
    {
        public string key;
        public Mesh[] meshes = new Mesh[9];
        public int[][] used = new int[9][];
        public Material[] slots;
        public float height, headY, headScale = 1f;
        public Vector3[] pivots = new Vector3[9];       // posisi pivot dunia tiap bagian
    }

    public const int PTorso = 0, PHead = 1, PArmL = 2, PArmR = 3, PLegL = 4, PLegR = 5, PTail = 6, PForeL = 7, PForeR = 8;
    static readonly string[] PartName = { "Torso", "Head", "ArmL", "ArmR", "LegL", "LegR", "Tail", "ForeL", "ForeR" };
    static readonly Dictionary<string, Archetype> cache = new Dictionary<string, Archetype>();

    public static void Reset() { cache.Clear(); matCache.Clear(); }

    static Archetype Get(Spec s)
    {
        string key = s.Key();
        Archetype a;
        if (cache.TryGetValue(key, out a)) return a;
        a = new Archetype { key = key };
        if (UseEditor && EditorEnsureDirs != null) EditorEnsureDirs();
        var cx = new Ctx(s);
        a.slots = Slots(s);
        a.height = cx.TotalHeight + 0.032f; a.headScale = cx.hk;
        a.headY = cx.Y(1.46f);
        for (int p = 0; p < 9; p++)
        {
            if (p == PTail && !(s.race == Beast)) continue;
            if ((p == PForeL || p == PForeR) && !s.split) continue;
            Vector3 pv = cx.Pivot(p);
            a.pivots[p] = pv;
            var b = new GeoBag();
            b.Push(-pv, 0f);
            switch (p)
            {
                case PTorso: BuildTorso(b, cx); break;
                case PHead: BuildHead(b, cx); break;
                case PArmL: BuildArm(b, cx, -1, s.split ? 1 : 0); break;
                case PArmR: BuildArm(b, cx, 1, s.split ? 1 : 0); break;
                case PForeL: BuildArm(b, cx, -1, 2); break;
                case PForeR: BuildArm(b, cx, 1, 2); break;
                case PLegL: BuildLeg(b, cx, -1); break;
                case PLegR: BuildLeg(b, cx, 1); break;
                case PTail: BuildTail(b, cx); break;
            }
            b.Pop();
            int[] used;
            a.meshes[p] = b.ToMesh("N_" + PartName[p] + "_" + key, UseEditor ? MeshDir + "/N_" + PartName[p] + "_" + key.Replace("|", "_") + ".asset" : null, out used);
            a.used[p] = used;
        }
        cache[key] = a;
        return a;
    }

    /// <summary>Pasang satu karakter ke scene. Kembalikan akar (kaki di origin, menghadap +Z).</summary>
    public static GameObject Spawn(Transform parent, string name, Vector3 pos, float yaw, Spec s, bool addCollider = true, bool npc = true)
    {
        if (npc) { var ext = TrySpawnExternal(parent, name, pos, yaw, s, addCollider); if (ext != null) return ext; }
        var a = Get(s);
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        var body = new GameObject("Body").transform;
        body.SetParent(root.transform, false);
        body.localPosition = a.pivots[PTorso];
        var parts = new Transform[9];
        parts[PTorso] = MakePart(body, "Torso", a, PTorso, Vector3.zero);
        parts[PHead] = MakePart(body, "Head", a, PHead, a.pivots[PHead] - a.pivots[PTorso]);
        parts[PHead].localScale = Vector3.one * a.headScale;
        parts[PHead].localPosition += new Vector3(0f, 0.032f, 0f);
        parts[PArmL] = MakePart(body, "ArmL", a, PArmL, a.pivots[PArmL] - a.pivots[PTorso]);
        parts[PArmR] = MakePart(body, "ArmR", a, PArmR, a.pivots[PArmR] - a.pivots[PTorso]);
        parts[PLegL] = MakePart(root.transform, "LegL", a, PLegL, a.pivots[PLegL]);
        parts[PLegR] = MakePart(root.transform, "LegR", a, PLegR, a.pivots[PLegR]);
        if (s.race == Beast) parts[PTail] = MakePart(body, "Tail", a, PTail, a.pivots[PTail] - a.pivots[PTorso]);
        Transform handL = null, handR = null;
        if (s.split)
        {
            parts[PForeL] = MakePart(parts[PArmL], "ForeL", a, PForeL, a.pivots[PForeL] - a.pivots[PArmL]);
            parts[PForeR] = MakePart(parts[PArmR], "ForeR", a, PForeR, a.pivots[PForeR] - a.pivots[PArmR]);
            handL = new GameObject("HandL").transform; handL.SetParent(parts[PForeL], false); handL.localPosition = HandOffset(s, -1);
            handR = new GameObject("HandR").transform; handR.SetParent(parts[PForeR], false); handR.localPosition = HandOffset(s, 1);
        }
        if (addCollider)
        {
            var cc = root.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, a.height * 0.5f, 0f);
            cc.radius = 0.32f;
            cc.height = a.height;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }

        var rig = root.AddComponent<CharacterRig>();
        rig.body = body; rig.head = parts[PHead]; rig.armL = parts[PArmL]; rig.armR = parts[PArmR]; rig.foreL = parts[PForeL]; rig.foreR = parts[PForeR];
        rig.legL = parts[PLegL]; rig.legR = parts[PLegR]; rig.tail = parts[PTail]; rig.handL = handL; rig.handR = handR; rig.height = a.height; rig.scale = a.height / 1.75f;
        if (!npc) return root;

        var an = root.AddComponent<NpcAnimator>();
        an.body = body; an.head = parts[PHead]; an.armL = parts[PArmL]; an.armR = parts[PArmR]; an.legL = parts[PLegL]; an.legR = parts[PLegR]; an.tail = parts[PTail];
        an.scale = a.height / 1.75f;

        var actor = root.AddComponent<NpcActor>();
        actor.displayName = s.name;
        actor.race = s.race;
        actor.outfit = s.outfit;
        actor.role = s.outfit;
        actor.female = s.female;
        actor.beastKind = s.race == Beast ? s.beast : -1;
        actor.height = a.height;
        return root;
    }

    /// <summary>Posisi telapak tangan relatif terhadap pivot siku (lokal lengan bawah).</summary>
    static Vector3 HandOffset(Spec s, int sgn)
    {
        var c = new Ctx(s);
        Vector3 hp = HandPos(c, sgn);
        return hp - ElbowPos(c, sgn);
    }

    static Transform MakePart(Transform parent, string name, Archetype a, int part, Vector3 localPos)
    {
        var g = new GameObject(name).transform;
        g.SetParent(parent, false);
        g.localPosition = localPos;
        if (a.meshes[part] != null)
        {
            var mf = g.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = a.meshes[part];
            var mats = new Material[a.used[part].Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = a.slots[a.used[part][i]];
            g.gameObject.AddComponent<MeshRenderer>().sharedMaterials = mats;
        }
        return g;
    }

    public static string Describe(Spec s)
    {
        string r = RaceName[s.race];
        if (s.race == Beast) r += " (" + BeastName[s.beast] + ")";
        return r;
    }
}
