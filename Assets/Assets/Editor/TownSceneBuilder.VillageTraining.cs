using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>Kompleks Pelatihan Hunter Desa Pemula: lapangan pedang, lapangan memanah, dan area uji sihir.</summary>
public static partial class TownSceneBuilder
{
    static void VTrainingComplex()
    {
        var g = Group("KompleksPelatihan", tProps);
        VSwordYard(g, new Vector3(-44f, 0f, -73f));
        VArcheryRange(g, -24f);
        VMagicArea(g, new Vector3(-6f, 0f, -72f));
        VTrainingExtras(g);
        Vector3 bp = new Vector3(-30f, 0f, -52f);
        // (papan besar dihapus: tiap pos latihan sudah berlabel; pandangan ke kompleks tidak lagi tertutup)
        Reserve(bp.x, bp.z, 5.5f); Reserve(bp.x - 3.5f, bp.z, 2f); Reserve(bp.x + 3.5f, bp.z, 2f);
        ReserveBox(-28f, -70f, 62f, 34f, 0f, 0f);
        VMapMark("Latihan Pedang", new Vector3(-44f, 0f, -73f), 4);
        VMapMark("Lapangan Memanah", new Vector3(-24f, 0f, -72f), 4);
        VMapMark("Uji Sihir", new Vector3(-6f, 0f, -72f), 4);
    }

    static void VDummy(Transform parent, Vector3 pos)
    {
        var m = new GameObject("Monster_dummy");
        m.transform.SetParent(parent, false);
        m.transform.position = pos;
        m.AddComponent<Monster>().defId = "dummy";
    }

    // ---------------------------------------------------------------- pedang
    static void VSwordYard(Transform g, Vector3 c)
    {
        var fb = new GeoBag();
        var pts = new List<Vector3>();
        for (int i = 0; i < 17; i++)
        {
            float a = (50f + i * 20f) * Mathf.Deg2Rad;       // celah di sisi timur laut untuk masuk
            pts.Add(c + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 8.5f);
        }
        PropKit.Fence(fb, pts, 1.3f);
        fb.Disc(Sl.Dirt, c, 8.0f, 32, 0.03f);
        fb.Annulus(Sl.Stone, c, 3.0f, 3.3f, 0f, 360f, 32, 0.05f);       // cincin tanding
        // rak senjata
        for (int r = 0; r < 2; r++)
        {
            Vector3 rp = c + new Vector3(r == 0 ? -6.4f : -3.2f, 0f, 5.6f - r * 0.4f);
            fb.Box(Sl.Wood, rp + new Vector3(0f, 0.9f, 0f), new Vector3(2.0f, 0.12f, 0.5f));
            fb.Box(Sl.Wood, rp + new Vector3(-0.9f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 0.4f));
            fb.Box(Sl.Wood, rp + new Vector3(0.9f, 0.45f, 0f), new Vector3(0.12f, 0.9f, 0.4f));
            for (int k = 0; k < 4; k++) fb.Beam(Sl.Metal, rp + new Vector3(-0.7f + k * 0.45f, 0.95f, 0f), rp + new Vector3(-0.7f + k * 0.45f + 0.1f, 2.0f, 0.1f), 0.05f, 0.05f);
        }
        TrainingArt.SwordYard(fb, c);
        fb.Emit(g, "LapanganPedang", baseSlots);
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], d = pts[i + 1];
            BoxCol(g, "PagarCol", (a + d) / 2f + Vector3.up * 0.65f, new Vector3((d - a).magnitude, 1.3f, 0.3f), new Vector3(0f, VYawOf((d - a).normalized), 0f));
        }
        foreach (float da in new[] { 100f, 140f, 180f, 220f, 260f }) VDummy(g, VRing(c, da, 5.6f));
        var trainer = VNpc("NPC_Pelatih", "Pelatih Garr", "Pelatih Pedang", "Hunter", c + new Vector3(1.8f, 0f, -0.2f), true);
        var tip = VAttach<InfoPoint>(trainer, "Bicara: Pelatih Garr", 3.4f);
        tip.title = "Pelatih Garr";
        tip.message = "Pukul boneka jerami itu, Nak. Tidak akan membalas.\n\n" +
            "KONTROL\n- WASD: jalan, Shift: lari, Ctrl: guling menghindar, Spasi: lompat\n- Klik kiri / J: serangan (kombo 3 kali; pukulan ketiga paling kuat)\n- Klik kanan: menahan serangan\n- E: bicara / ambil / buka\n\n" +
            "Pedang cocok untuk yang berani maju. Menahan serangan di saat yang tepat membuat lawan terbuka.\n" +
            "Di Level 5 kau memilih job (Swordsman, Archer, Penyihir Api, atau Penyihir Tanah) dan keterampilan baru terbuka.";
        Reserve(c.x, c.z, 11f);
    }

    // ---------------------------------------------------------------- panah
    static void VArcheryRange(Transform g, float cx)
    {
        var b = new GeoBag();
        float zS = -61f;           // garis tembak
        // lantai lapangan + garis tembak
        b.Patch(Sl.Dirt, new Vector2(cx - 5.5f, -85f), new Vector2(cx - 5.5f, zS + 2f), new Vector2(cx + 5.5f, zS + 2f), new Vector2(cx + 5.5f, -85f), 0.03f, new Vector2(0f, 1f));
        b.Box(Sl.Marble, new Vector3(cx, 0.05f, zS), new Vector3(11f, 0.1f, 0.5f));
        // pagar samping
        var left = new List<Vector3> { new Vector3(cx - 6f, 0f, zS + 2f), new Vector3(cx - 6f, 0f, -86f) };
        var right = new List<Vector3> { new Vector3(cx + 6f, 0f, zS + 2f), new Vector3(cx + 6f, 0f, -86f) };
        PropKit.Fence(b, left, 1.2f); PropKit.Fence(b, right, 1.2f);
        // penanda jarak
        float[] dist = { -67f, -73f, -79f };
        for (int i = 0; i < dist.Length; i++)
        {
            float z = dist[i];
            float x = cx + (i - 1) * 3.2f;
            // tiang + target bulat bersusun (menghadap utara/pemanah)
            b.Cyl(Sl.Wood, new Vector3(x - 0.9f, 0f, z), 0.1f, 1.8f, 6, 0.1f, true, false);
            b.Cyl(Sl.Wood, new Vector3(x + 0.9f, 0f, z), 0.1f, 1.8f, 6, 0.1f, true, false);
            b.Box(Sl.Wood, new Vector3(x, 1.8f, z - 0.05f), new Vector3(2.4f, 2.4f, 0.1f));
            b.Box(Sl.Cloth, new Vector3(x, 1.8f, z + 0.03f), new Vector3(2.1f, 2.1f, 0.06f));
            b.Box(Sl.Plaster, new Vector3(x, 1.8f, z + 0.06f), new Vector3(1.6f, 1.6f, 0.06f));
            b.Box(Sl.Cloth, new Vector3(x, 1.8f, z + 0.09f), new Vector3(1.1f, 1.1f, 0.06f));
            b.Box(Sl.Gold, new Vector3(x, 1.8f, z + 0.12f), new Vector3(0.5f, 0.5f, 0.06f));
            b.Box(Sl.Marble, new Vector3(cx, 0.04f, z + 1.4f), new Vector3(11f, 0.06f, 0.14f));          // garis jarak
        }
        // dinding jerami di belakang
        for (int i = -1; i <= 1; i++) b.Box(Sl.Thatch, new Vector3(cx + i * 3.6f, 1.2f, -84f), new Vector3(3.4f, 2.4f, 1.4f));
        // tempat busur di garis tembak
        Vector3 rp = new Vector3(cx - 8.5f, 0f, zS - 0.5f);
        b.Box(Sl.Wood, rp + new Vector3(0f, 0.9f, 0f), new Vector3(0.4f, 1.8f, 2.4f));
        for (int k = 0; k < 3; k++) b.Beam(Sl.WoodLight, rp + new Vector3(0.2f, 0.4f, -0.8f + k * 0.8f), rp + new Vector3(0.2f, 1.7f, -0.8f + k * 0.8f), 0.05f, 0.05f);
        TrainingArt.Archery(b, cx, zS);
        b.Emit(g, "LapanganMemanah", baseSlots);
        BoxCol(g, "PagarKiriCol", new Vector3(cx - 6f, 0.6f, -72.5f), new Vector3(0.3f, 1.2f, 27f), Vector3.zero);
        BoxCol(g, "PagarKananCol", new Vector3(cx + 6f, 0.6f, -72.5f), new Vector3(0.3f, 1.2f, 27f), Vector3.zero);
        BoxCol(g, "DindingJerami", new Vector3(cx, 1.2f, -84f), new Vector3(11f, 2.4f, 1.4f), Vector3.zero);
        // boneka di depan setiap target (agar panah memberi umpan balik)
        for (int i = 0; i < dist.Length; i++) VDummy(g, new Vector3(cx + (i - 1) * 3.2f, 0f, dist[i] + 1.0f));
        // papan nama + instruktur
        var ar = VNpc("NPC_Pemanah", "Elowen", "Instruktur Panah", "Hunter", new Vector3(cx + 7.8f, 0f, zS - 0.5f), false);
        var ip = VAttach<InfoPoint>(ar, "Bicara: Elowen", 3.4f);
        ip.title = "Elowen, Pemanah";
        ip.message = "Berdiri di belakang garis putih. Tarik napas, bidik, lepaskan.\n\n" +
            "- Panah terbang lurus; bidik sedikit di atas sasaran yang jauh.\n- Tiga sasaran ada di jarak 6, 12, dan 18 langkah.\n- Pemanah (job Archer, Level 5) menembak dari jauh sambil bergerak: menghindar dulu, lalu menembak.\n\nBoneka di depan tiap sasaran tidak membalas. Pakailah sepuasnya.";
        Reserve(cx, -72f, 14f);
    }

    // ---------------------------------------------------------------- sihir
    static void VMagicArea(Transform g, Vector3 c)
    {
        var cs = VCrystalSlots();
        var b = new GeoBag();
        b.Disc(Sl.Marble, c, 8.2f, 40, 0.06f);
        b.Annulus(Sl.Glow, c, 7.4f, 7.8f, 0f, 360f, 40, 0.09f);
        b.Annulus(Sl.Glow, c, 3.6f, 3.9f, 0f, 360f, 36, 0.09f);
        // garis rune bintang (6 lengan)
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            b.Beam(Sl.Glow, c + new Vector3(Mathf.Sin(a) * 3.9f, 0.1f, Mathf.Cos(a) * 3.9f), c + new Vector3(Mathf.Sin(a) * 7.4f, 0.1f, Mathf.Cos(a) * 7.4f), 0.18f, 0.03f);
        }
        // enam pilar kristal di tepi lingkaran
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 30f) * Mathf.Deg2Rad;
            Vector3 p = c + new Vector3(Mathf.Sin(a) * 8.6f, 0f, Mathf.Cos(a) * 8.6f);
            b.Cyl(Sl.Marble, p, 0.7f, 1.2f, 8, 0.55f, true);
            b.Cone(Sl.Glow, p + Vector3.up * 1.2f, 0.4f, 1.5f, 6);
            b.Ball(Sl.Glow, p + new Vector3(0f, 3.0f, 0f), 0.2f, 3, 6, 1.5f);
        }
        // altar uji di tengah
        b.Cyl(Sl.Marble, c, 1.2f, 0.9f, 10, 1.0f, true);
        b.Cyl(Sl.Stone, c + Vector3.up * 0.9f, 0.9f, 0.25f, 10, 0.9f, true);
        TrainingArt.Magic(b, c);
        b.Emit(g, "AreaSihir", cs);
        var kr = new GameObject("KristalUji").transform;
        kr.SetParent(g, false);
        kr.position = c + new Vector3(0f, 2.3f, 0f);
        var kb = new GeoBag();
        kb.Ball(Sl.Glow, Vector3.zero, 0.55f, 2, 4, 1.9f);
        kb.Emit(kr, "Mesh", cs);
        var sp = kr.gameObject.AddComponent<SpinObject>(); sp.axis = Vector3.up; sp.degreesPerSecond = 40f;
        var lg = new GameObject("CahayaSihir").transform;
        lg.SetParent(g, false); lg.position = c + new Vector3(0f, 3.0f, 0f);
        var lt = lg.gameObject.AddComponent<Light>();
        lt.type = LightType.Point; lt.range = 22f; lt.intensity = 2.0f; lt.color = new Color(0.55f, 0.8f, 1f);
        BoxCol(g, "AltarCol", c + new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 2.4f), Vector3.zero);
        // sasaran sihir: boneka di busur depan lingkaran (busur tenggara)
        for (int i = 0; i < 5; i++)
        {
            float a = (110f + i * 20f) * Mathf.Deg2Rad;           // busur tenggara: tidak menabrak pagar lapangan panah, jauh dari pagar desa
            Vector3 p = c + new Vector3(Mathf.Sin(a) * 12.5f, 0f, Mathf.Cos(a) * 12.5f);
            VDummy(g, p);
            var pg = new GeoBag();
            pg.Cyl(Sl.Marble, p + new Vector3(0f, 0f, 0f), 0.9f, 0.18f, 8, 0.9f, true);
            pg.Emit(g, "AlasSasaran", cs);
        }
        var mage = VNpc("NPC_Penyihir", "Lyra", "Instruktur Sihir", "Peziarah", c + new Vector3(5.2f, 0f, 5.2f), false);
        var ip = VAttach<InfoPoint>(mage, "Bicara: Lyra", 3.4f);
        ip.title = "Lyra, Penyihir";
        ip.message = "Berdirilah di dalam lingkaran rune dan arahkan sihirmu ke boneka-boneka di seberang.\n\n" +
            "- Sihir api membakar dan meluas; sihir tanah menahan dan menghancurkan dari jauh.\n- Mana pulih sendiri bila kau berhenti merapal beberapa saat.\n- Job Penyihir Api dan Penyihir Tanah terbuka di Level 5.\n\nKristal di tengah lingkaran bergetar bila ada yang bisa memakai sihir. Kau mungkin salah satunya.";
        var kp = VPoint<InfoPoint>(g, "InteractPoint_KristalUji", c + new Vector3(0f, 0f, -2.2f), "Sentuh Kristal Uji", 3.2f);
        kp.title = "Kristal Uji Sihir";
        kp.message = "Kristal berdenyut lembut dan memantulkan warna biru.\n\n\"Bakat sihir sejati terlihat saat kau memilih jalanmu di Level 5,\" tulis ukiran di altar.\n\nBerlatihlah dulu di lingkaran ini.";
        Reserve(c.x, c.z, 20f);
    }
}
