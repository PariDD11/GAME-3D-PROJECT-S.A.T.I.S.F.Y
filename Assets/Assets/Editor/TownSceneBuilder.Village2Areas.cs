using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HunterGuild;

/// <summary>Kampung Tepi Kali: area terbuka (hijau desa, ladang, padang, kebun apel, makam, lapangan latihan), lampu, pohon, NPC, papan denah.</summary>
public static partial class TownSceneBuilder
{
    static Vector3 V2Pt(float x, float z, float dy = 0f) { return new Vector3(x, V2.H(x, z) + dy, z); }

    static List<Vector3> V2Poly(params float[] xz)
    {
        var l = new List<Vector3>();
        for (int i = 0; i + 1 < xz.Length; i += 2) l.Add(V2Pt(xz[i], xz[i + 1]));
        return l;
    }

    /// <summary>Collider pagar: satu kotak per ruas.</summary>
    static void V2FenceCols(Transform g, IList<Vector3> pts, float h)
    {
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], d = pts[i + 1];
            Vector3 dir = d - a; dir.y = 0f;
            if (dir.magnitude < 0.3f) continue;
            BoxCol(g, "PagarCol", (a + d) / 2f + Vector3.up * (h * 0.5f), new Vector3(dir.magnitude, h, 0.3f), new Vector3(0f, VYawOf(dir.normalized), 0f));
        }
    }

    static GameObject V2PlaceProp(string key, System.Action<GeoBag> build, Transform parent, string name, Vector3 pos, float yaw, float scale = 1f)
    {
        return PropKit.Place(PropKit.Get(key, build), parent, name, pos, yaw, scale, baseSlots);
    }

    static Transform v2AnchorRoot;

    /// <summary>Titik jangkar kawanan ternak. FarmAnimals (runtime) membaca objek "Ternak_&lt;id&gt;" di bawah "TernakAnchors" untuk menentukan tempat kawanan muncul.</summary>
    static void V2Anchor(string id, float x, float z)
    {
        if (v2AnchorRoot == null) v2AnchorRoot = Group("TernakAnchors", tProps);
        var a = new GameObject("Ternak_" + id).transform;
        a.SetParent(v2AnchorRoot, false);
        a.position = V2Pt(x, z);
    }

    static void V2Areas()
    {
        v2AnchorRoot = null;
        V2Green();
        V2Fields();
        V2Pasture();
        V2Orchard();
        V2Graveyard();
        V2TrainingYard();
        V2Stream();
        V2Notice();
    }

    // =====================================================================
    // Hijau desa "Simpang Beringin": beringin tua, sumur, batu pendiri, bangku, tiang rambu
    // =====================================================================
    static void V2Green()
    {
        var g = Group("HijauDesa", tNoble);
        // beringin tua
        Vector3 op = V2Pt(V2.Oak.x, V2.Oak.y);
        var oak = PropKit.Place(vTreeProtos[0], g, "PohonBeringinTua", op, 35f, 2.3f, ForestKit.Slots);
        var oc = oak.AddComponent<CapsuleCollider>(); oc.center = new Vector3(0f, 1.5f, 0f); oc.radius = 0.55f; oc.height = 3f;
        Reserve(op.x, op.z, 7f);
        // bangku melingkar di bawah beringin
        var bb = new GeoBag();
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 p = V2Pt(op.x + Mathf.Sin(a) * 4.6f, op.z + Mathf.Cos(a) * 4.6f);
            bb.Push(p, i * 90f + 45f + 180f); V2Kit.Bench(bb); bb.Pop();
        }
        bb.Emit(g, "BangkuBeringin", baseSlots);
        // sumur desa
        Vector3 wp = V2Pt(V2.Well.x, V2.Well.y);
        var well = V2PlaceProp("V2_Well", V2Kit.Well, g, "SumurDesa", wp, 20f);
        BoxCol(well.transform, "SumurCol", new Vector3(0f, 0.8f, 0f), new Vector3(2.8f, 1.6f, 2.8f), Vector3.zero);
        Reserve(wp.x, wp.z, 3.5f);
        VMapMark("Sumur & Hijau Desa", V2.Well, 6);
        var wi = VPoint<InfoPoint>(g, "InteractPoint_Sumur", V2Pt(V2.Well.x + 1.4f, V2.Well.y - 2.8f), "Lihat Sumur Desa", 3.4f);
        wi.title = "Sumur Desa";
        wi.message = "Sumur tua di tengah hijau desa. Airnya dingin sepanjang tahun, katanya mengalir dari mata air yang sama dengan Kali Embun.\n\nPara petani mengisi ember di sini sebelum berangkat ke ladang.";
        // batu pendiri
        Vector3 fp = V2Pt(V2.Founder.x, V2.Founder.y);
        var fs = V2PlaceProp("V2_Founder", V2Kit.FoundersStone, g, "BatuPendiri", fp, 200f);
        BoxCol(fs.transform, "BatuCol", new Vector3(0f, 0.9f, 0f), new Vector3(1.6f, 1.8f, 1.2f), Vector3.zero);
        Reserve(fp.x, fp.z, 2.5f);
        var fi = VPoint<InfoPoint>(g, "InteractPoint_BatuPendiri", V2Pt(V2.Founder.x + 1f, V2.Founder.y - 2.2f), "Baca Batu Pendiri", 3.4f);
        fi.title = "Batu Pendiri Desa";
        fi.message = "KAMPUNG TEPI KALI\nDidirikan oleh keluarga petani dan para Hunter pertama yang menetap di tepi Kali Embun.\n\n\"Kami datang untuk berladang, tinggal untuk saling menjaga.\"\n\nSeseorang pernah mengukir sebuah nama di sudut batu ini, lalu menggosoknya hingga hilang. Tak seorang pun ingat siapa.";
        // tiang rambu di simpang
        var sp = new GeoBag();
        V2Kit.SignPost(sp, new[] { -90f, 0f, 180f, 90f }, new[] { 3.2f, 2.8f, 2.4f, 2.0f });
        var sgo = sp.Emit(g, "TiangRambu", baseSlots);
        Vector3 sp0 = V2Pt(6f, 22f);
        sgo.transform.position = sp0;
        string[] lab = { "Hutan Pemula", "Ashwatch Lv5+", "Gereja & Makam", "Latihan & Kali" };
        float[] yw = { -90f, 0f, 180f, 90f }, yy = { 3.2f, 2.8f, 2.4f, 2.0f };
        for (int i = 0; i < 4; i++)
        {
            Quaternion q = Quaternion.Euler(0f, yw[i], 0f);
            Vector3 c = sp0 + q * new Vector3(0.95f, yy[i], 0f);
            BuildKit.Text(g, lab[i], c + q * new Vector3(0f, 0f, 0.05f), yw[i] + 180f, 0.031f, new Color(0.2f, 0.11f, 0.05f));
            BuildKit.Text(g, lab[i], c + q * new Vector3(0f, 0f, -0.05f), yw[i], 0.031f, new Color(0.2f, 0.11f, 0.05f));
        }
        BoxCol(g, "TiangRambuCol", sp0 + Vector3.up * 1.6f, new Vector3(0.5f, 3.2f, 0.5f), Vector3.zero);
        Reserve(sp0.x, sp0.z, 2.5f);
        // gerobak & tong di tepi hijau
        var cb = new GeoBag();
        Vector3 cp = V2Pt(-10f, 34f);
        cb.Push(cp, -30f); V2Kit.Cart(cb); cb.Pop();
        Vector3 hp = V2Pt(-16f, 32f);
        cb.Push(hp, 0f); V2Kit.Haystack(cb); cb.Pop();
        cb.Emit(g, "GerobakJerami", baseSlots);
        BoxCol(g, "GerobakCol", cp + Vector3.up * 0.8f, new Vector3(2.6f, 1.6f, 1.6f), new Vector3(0f, -30f, 0f));
        BoxCol(g, "JeramiCol", hp + Vector3.up * 1.2f, new Vector3(3f, 2.4f, 3f), Vector3.zero);
        Reserve(cp.x, cp.z, 3f); Reserve(hp.x, hp.z, 3f);
    }

    // =====================================================================
    // Papan pengumuman dekat titik mulai
    // =====================================================================
    static void V2Notice()
    {
        Vector3 bp = V2Pt(V2.Notice.x, V2.Notice.y);
        float yaw = Mathf.Atan2(V2.Spawn.x - V2.Notice.x, V2.Spawn.y - V2.Notice.y) * Mathf.Rad2Deg;
        var go = V2PlaceProp("V2_Notice", V2Kit.NoticeBoard, tProps, "PapanDesa", bp, yaw);
        string face = "DESA PEMULA\nUtara: Hutan Pemula\nTimur: Kota (Lv 5)";
        BuildKit.Text(go.transform, face, new Vector3(0f, 1.95f, 0.12f), 180f, 0.036f, Color.white);
        BoxCol(go.transform, "PapanCol", new Vector3(0f, 1.5f, 0f), new Vector3(3.2f, 3f, 0.5f), Vector3.zero);
        Reserve(bp.x, bp.z, 3.2f);
        var info = VPoint<InfoPoint>(tProps, "InteractPoint_Papan", bp + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 2.0f), "Baca papan pengumuman");
        info.title = "Papan Desa Pemula";
        info.message = "Selamat datang di Kampung Tepi Kali, calon Hunter!\n\n" +
            "1. Bicaralah dengan Veteran Brann (rumah panjang di barat laut hijau desa); dia membimbingmu dari Level 1 sampai 5 dan membantumu memilih job.\n" +
            "2. LATIHAN: lapangan pedang, memanah, dan sihir ada di seberang Kali Embun (selatan), lewat jembatan batu atau titian kayu.\n" +
            "3. FASILITAS: Penginapan & Toko Bram dekat hijau desa, Pandai Besi di sisi jalan, Kantor Properti (rumah dijual: 3 rumah), Gereja di bukit barat laut dengan makam di belakangnya.\n" +
            "4. JALAN UTARA: Hutan Pemula, tempat berburu Slime & Goblin sampai Level 5.\n" +
            "5. JALAN TIMUR: Ashwatch. Gerbangnya baru terbuka untuk Hunter Level 5 ke atas.\n\n" +
            "Penginapan untuk tidur dan menyimpan, Tabib Mira menyembuhkan gratis.";
    }

    // =====================================================================
    // Ladang (gandum, sayur, kubis, petak bajakan)
    // =====================================================================
    static void V2Fields()
    {
        var g = Group("Ladang", tProps);
        var fenceB = new GeoBag(); var cropB = new GeoBag(); var groundB = new GeoBag();
        var marked = new HashSet<int>();
        foreach (var l in V2.Lots)
        {
            if (l.kind != "field") continue;
            float y = l.py + 0.035f;
            Vector2 c0 = V2.ToWorld(l, -l.w * 0.5f, -l.d * 0.5f), c1 = V2.ToWorld(l, -l.w * 0.5f, l.d * 0.5f), c2 = V2.ToWorld(l, l.w * 0.5f, l.d * 0.5f), c3 = V2.ToWorld(l, l.w * 0.5f, -l.d * 0.5f);
            Vector2 rowDir = (c3 - c0).normalized;
            groundB.Patch(l.v == 3 ? Sl.FieldB : Sl.Field, c0, c1, c2, c3, y, rowDir);
            cropB.Push(new Vector3(l.x, l.py, l.z), l.yaw);
            int seed = (int)(l.x * 3f + l.z);
            if (l.v == 0)                  // gandum masak: barisan sepanjang X
            {
                int rows = Mathf.Max(3, (int)((l.d - 1.6f) / 0.75f));
                for (int r = 0; r < rows; r++)
                {
                    float z = -l.d * 0.5f + 0.8f + r * (l.d - 1.6f) / (rows - 1);
                    float h = 0.85f + 0.2f * V2Kit.Rnd(seed + r, 3);
                    cropB.Box(Sl.Leaf, new Vector3(0f, 0.25f, z), new Vector3(l.w - 1.4f, 0.5f, 0.42f));
                    cropB.Box(Sl.Thatch, new Vector3(0f, 0.25f + h * 0.55f, z), new Vector3(l.w - 1.5f, h * 0.7f, 0.36f));
                }
            }
            else if (l.v == 1)             // sayur: guludan + bola hijau
            {
                int rows = Mathf.Max(2, (int)((l.d - 1.4f) / 1.1f));
                for (int r = 0; r < rows; r++)
                {
                    float z = -l.d * 0.5f + 0.9f + r * (l.d - 1.8f) / Mathf.Max(1, rows - 1);
                    cropB.Box(Sl.Dirt, new Vector3(0f, 0.12f, z), new Vector3(l.w - 1.4f, 0.24f, 0.55f));
                    int k = Mathf.Max(3, (int)((l.w - 2f) / 0.7f));
                    for (int i = 0; i < k; i++)
                        cropB.Ball((i + r) % 4 == 0 ? Sl.Blossom : Sl.Hedge, new Vector3(-(l.w - 2f) * 0.5f + i * (l.w - 2f) / (k - 1), 0.42f, z), 0.2f + 0.05f * V2Kit.Rnd(seed + i, r), 3, 6, 0.9f);
                }
            }
            else if (l.v == 2)             // kubis
            {
                int cols = Mathf.Max(3, (int)((l.w - 1.6f) / 1.1f)), rows = Mathf.Max(2, (int)((l.d - 1.4f) / 1.1f));
                for (int r = 0; r < rows; r++)
                    for (int i = 0; i < cols; i++)
                        cropB.Ball(Sl.Leaf, new Vector3(-(l.w - 1.8f) * 0.5f + i * (l.w - 1.8f) / Mathf.Max(1, cols - 1), 0.3f, -(l.d - 1.6f) * 0.5f + r * (l.d - 1.6f) / Mathf.Max(1, rows - 1)), 0.34f, 3, 7, 0.85f);
            }
            else                           // petak bajakan: alur melintang
            {
                int fr = Mathf.Max(4, (int)((l.w - 0.8f) / 0.6f));
                for (int i = 0; i < fr; i++)
                    cropB.Box(i % 2 == 0 ? Sl.Field : Sl.Dirt, new Vector3(-(l.w - 0.8f) * 0.5f + i * (l.w - 0.8f) / (fr - 1), 0.07f, 0f), new Vector3(0.3f, 0.14f, l.d - 0.8f));
            }
            cropB.Pop();
            // pagar tiga sisi (sisi terdekat jalan dibiarkan terbuka)
            float[] sx = { -1f, 1f, 0f, 0f }, sz = { 0f, 0f, -1f, 1f };
            int open = 0; float best = 1e9f;
            for (int s = 0; s < 4; s++)
            {
                Vector2 mid = V2.ToWorld(l, sx[s] * (l.w * 0.5f + 1f), sz[s] * (l.d * 0.5f + 1f));
                float dd = 1e9f; foreach (var r in V2.Roads) dd = Mathf.Min(dd, V2.DistPoly(mid.x, mid.y, r.pts));
                if (dd < best) { best = dd; open = s; }
            }
            Vector2[] cor = { V2.ToWorld(l, -l.w * 0.5f - 1f, -l.d * 0.5f - 1f), V2.ToWorld(l, -l.w * 0.5f - 1f, l.d * 0.5f + 1f), V2.ToWorld(l, l.w * 0.5f + 1f, l.d * 0.5f + 1f), V2.ToWorld(l, l.w * 0.5f + 1f, -l.d * 0.5f - 1f) };
            int[][] sides = { new[] { 0, 1 }, new[] { 3, 2 }, new[] { 0, 3 }, new[] { 1, 2 } };
            for (int s = 0; s < 4; s++)
            {
                if (s == open) continue;
                var pts = new List<Vector3> { V2Pt(cor[sides[s][0]].x, cor[sides[s][0]].y), V2Pt(cor[sides[s][1]].x, cor[sides[s][1]].y) };
                V2Kit.RailFence(fenceB, pts, 1.05f, 2);
                V2FenceCols(g, pts, 1.0f);
            }
            ReserveBox(l.x, l.z, l.w + 2f, l.d + 2f, l.yaw, 0f);
            Reserve(l.x, l.z, Mathf.Max(l.w, l.d) * 0.5f + 1.5f);
            if (l.v != 3 && marked.Add(l.v))
                VMapMark(l.v == 0 ? "Ladang Gandum" : (l.v == 2 ? "Kebun Kubis" : "Kebun Sayur"), new Vector2(l.x, l.z), 4);
            if (l.id == "field1" || l.id == "field4")
            {
                Vector3 sc = V2Pt(l.x, l.z);
                V2PlaceProp("V2_Scarecrow", V2Kit.Scarecrow, g, "Orang2an", sc, 20f);
            }
        }
        groundB.Emit(g, "TanahLadang", baseSlots);
        cropB.Emit(g, "Tanaman", baseSlots);
        fenceB.Emit(g, "PagarLadang", baseSlots);
        // tumpukan jerami dan gerobak di dekat ladang gandum
        var hb = new GeoBag();
        foreach (var hp in new[] { new Vector2(104.5f, -22f), new Vector2(104f, -30f), new Vector2(75.5f, -43f) })
        {
            Vector3 p = V2Pt(hp.x, hp.y); hb.Push(p, hp.x * 7f); V2Kit.Haystack(hb); hb.Pop();
            BoxCol(g, "JeramiCol", p + Vector3.up * 1.2f, new Vector3(3f, 2.4f, 3f), Vector3.zero);
            Reserve(p.x, p.z, 2.6f);
        }
        Vector3 cp = V2Pt(58f, -44f); hb.Push(cp, 70f); V2Kit.Cart(hb); hb.Pop();
        hb.Emit(g, "JeramiGerobak", baseSlots);
        BoxCol(g, "GerobakCol", cp + Vector3.up * 0.8f, new Vector3(2.6f, 1.6f, 1.6f), new Vector3(0f, 70f, 0f));
    }

    // =====================================================================
    // Padang ternak (timur laut) di sekitar lumbung besar
    // =====================================================================
    static void V2Pasture()
    {
        var g = Group("PadangTernak", tProps);
        var pts = V2Poly(72f, 38f, 94f, 38f, 97f, 56f, 91f, 74f, 76f, 86f, 56f, 90f, 47f, 82f, 47f, 38f, 62f, 38f);
        var fb = new GeoBag();
        V2Kit.RailFence(fb, pts, 1.15f, 2);
        fb.Emit(g, "PagarPadang", baseSlots);
        V2FenceCols(g, pts, 1.1f);
        var ab = new GeoBag();
        // palung air dan jerami (ternak hidup muncul dari sistem FarmAnimals di titik jangkar "Ternak_*", lihat V2Anchor)
        foreach (var tp in new[] { new Vector2(84f, 52f), new Vector2(85f, 73f), new Vector2(57f, 56f) })
        {
            Vector3 p = V2Pt(tp.x, tp.y); ab.Push(p, 90f); V2Kit.Trough(ab); ab.Pop();
            BoxCol(g, "PalungCol", p + Vector3.up * 0.35f, new Vector3(0.8f, 0.7f, 2.4f), Vector3.zero);
            Reserve(p.x, p.z, 2f);
        }
        Vector3 hp = V2Pt(79f, 82f); ab.Push(hp, 10f); V2Kit.Haystack(ab); ab.Pop();
        ab.Emit(g, "PalungJerami", baseSlots);
        BoxCol(g, "JeramiCol", hp + Vector3.up * 1.2f, new Vector3(3f, 2.4f, 3f), Vector3.zero);
        V2Anchor("cow", 84f, 50f); V2Anchor("cow", 88f, 60f);
        V2Anchor("sheep", 55f, 50f); V2Anchor("sheep", 54f, 64f);
        V2Anchor("goat", 58f, 80f); V2Anchor("goat", 90f, 48f);
        V2Anchor("chicken", 60f, -38f); V2Anchor("chicken", 42f, -2f); V2Anchor("chicken", -10f, 24f);
        ReserveBox(72f, 64f, 52f, 54f, 0f, 0f);
        VMapMark("Padang Ternak", new Vector2(72f, 62f), 4);
    }

    // =====================================================================
    // Kebun apel (barat laut, di bawah lereng hutan)
    // =====================================================================
    static void V2Orchard()
    {
        var g = Group("KebunApel", tProps);
        int n = 0;
        // dua baris berselang-seling di kaki lereng barat laut. Batas barat/utara mengikuti kaki tebing (tinggi tanah
        // dicek dengan V2.H: pagar tidak boleh memanjat dinding tebing seperti denah lama).
        for (int row = 0; row < 2; row++)
        {
            int cnt = row == 0 ? 6 : 5;
            for (int i = 0; i < cnt; i++)
            {
                float x = (row == 0 ? -64f : -56.8f) + i * 7.2f, z = row == 0 ? 95f : 101.5f;
                Vector3 p = V2Pt(x, z);
                var go = PropKit.Place(PropKit.TreeProto(5), g, "PohonApel", p, n * 53f, 1f + 0.12f * (n % 3), baseSlots);
                var cc = go.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.45f; cc.height = 3f;
                Reserve(x, z, 2.6f);
                n++;
            }
        }
        var hb = new GeoBag();
        V2Kit.HedgeRun(hb, V2Poly(-68f, 89f, -68f, 98f, -62f, 103f, -52f, 106f, -38f, 107.5f, -26f, 106f), 1.2f, 1.0f, 3);
        Vector3 cp = V2Pt(-34f, 89.5f); hb.Push(cp, 200f); V2Kit.Cart(hb); hb.Pop();
        Vector3 kp = V2Pt(-30.5f, 90.5f);
        hb.Push(kp, 0f); V2Kit.Crate(hb, Vector3.zero, 0.9f, 20f); V2Kit.Barrel(hb, new Vector3(1.4f, 0f, 0.2f)); hb.Pop();
        Reserve(cp.x, cp.z, 2.4f); Reserve(kp.x, kp.z, 1.6f);
        hb.Emit(g, "PagarKebun", baseSlots);
        BoxCol(g, "GerobakCol", cp + Vector3.up * 0.8f, new Vector3(2.6f, 1.6f, 1.6f), new Vector3(0f, 200f, 0f));
        VMapMark("Kebun Apel", new Vector2(-46f, 98.5f), 4);
    }

    // =====================================================================
    // Makam desa di belakang gereja
    // =====================================================================
    static void V2Graveyard()
    {
        V2.Lot gl = V2.Lots.Find(q => q.kind == "graveyard");
        if (gl == null) return;
        var g = Group("MakamDesa", tHouses);
        var b = new GeoBag();
        // tembok batu kering dengan celah di sisi timur (dekat gereja)
        float x0 = gl.x - gl.w * 0.5f, x1 = gl.x + gl.w * 0.5f, z0 = gl.z - gl.d * 0.5f, z1 = gl.z + gl.d * 0.5f;
        V2Kit.DryWall(b, V2Poly(x1, gl.z - 2f, x1, z0, x0, z0, x0, z1, x1, z1, x1, gl.z + 2f), 1.0f, 0.5f, 11);
        // nisan: 5 baris x 4 kolom
        int gi = 0;
        var cols = new List<Vector3>();
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
            {
                float gx = x0 + 3.6f + c * 4.0f, gz = z0 + 3.2f + r * 4.4f;
                if (Mathf.Abs(gx - gl.x) < 3.2f && Mathf.Abs(gz - gl.z) < 3.4f) continue;     // ruang patung tengah
                float jit = V2Kit.Rnd(gi, 5) - 0.5f;
                Vector3 p = V2Pt(gx + jit * 0.6f, gz + jit * 0.5f);
                b.Push(p, 90f + jit * 16f);
                V2Kit.Grave(b, gi % 5, gi + 3);
                b.Pop();
                if (gi % 2 == 0) cols.Add(p);
                gi++;
            }
        // patung ksatria penjaga di tengah
        Vector3 cp = V2Pt(gl.x, gl.z);
        b.Cyl(Sl.Stone, cp, 1.5f, 0.8f, 12, 1.3f, true);
        PropKit.KnightStatue(b, cp + new Vector3(0f, 0.8f, 0f), 90f);
        // papan nama di gerbang
        Vector3 sg = V2Pt(x1 + 0.6f, gl.z - 3.2f);
        b.Cyl(Sl.Wood, sg + new Vector3(0f, 0f, -1.5f), 0.08f, 2.6f, 6, 0.07f, true, false);
        b.Cyl(Sl.Wood, sg + new Vector3(0f, 0f, 1.5f), 0.08f, 2.6f, 6, 0.07f, true, false);
        b.Push(sg + new Vector3(0f, 0f, 0f), 90f); V2Kit.SignBoard(b, 0f, 2.2f, 0f, 3.0f, 0.8f); b.Pop();
        b.Emit(g, "Mesh", baseSlots);
        BuildKit.Text(g, "MAKAM DESA", sg + new Vector3(0.1f, 2.2f, 0f), -90f, 0.05f, new Color(0.95f, 0.92f, 0.82f));
        BuildKit.Text(g, "MAKAM DESA", sg + new Vector3(-0.1f, 2.2f, 0f), 90f, 0.05f, new Color(0.95f, 0.92f, 0.82f));
        for (int i = 0; i < cols.Count; i++) BoxCol(g, "NisanCol", cols[i] + new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 1.2f, 0.8f), Vector3.zero);
        BoxCol(g, "PatungCol", cp + Vector3.up * 1.5f, new Vector3(2.4f, 3f, 2.4f), Vector3.zero);
        V2FenceCols(g, V2Poly(x1, gl.z - 2f, x1, z0, x0, z0, x0, z1, x1, z1, x1, gl.z + 2f), 1.0f);
        ReserveBox(gl.x, gl.z, gl.w + 4f, gl.d + 4f, 0f, 0f);
        VMapMark("Makam Desa", new Vector2(gl.x, gl.z), 3);
        // sipres + lentera kristal biru
        var cyp = PropKit.TreeProto(3);
        Vector2[] tp = { new Vector2(x0 + 1.2f, z0 + 1.4f), new Vector2(x0 + 1.2f, z1 - 1.4f), new Vector2(x1 - 1.2f, z0 + 1.4f), new Vector2(x1 - 1.2f, z1 - 1.4f), new Vector2(gl.x, z0 + 1.2f), new Vector2(gl.x, z1 - 1.2f) };
        for (int i = 0; i < tp.Length; i++) PropKit.Place(cyp, g, "Sipres", V2Pt(tp[i].x, tp[i].y), i * 53f, 1.15f + (i % 3) * 0.1f, baseSlots);
        var csl = VCrystalSlots();
        var clamp = PropKit.Get("D_CrystalLamp", DungeonKit.CrystalLamp);
        foreach (var lp in new[] { new Vector2(x1 - 2.4f, gl.z - 4f), new Vector2(x1 - 2.4f, gl.z + 4f), new Vector2(gl.x, z0 + 3f), new Vector2(x0 + 3f, gl.z) })
        {
            var go = PropKit.Place(clamp, g, "LenteraMakam", V2Pt(lp.x, lp.y), 0f, 1f, csl);
            VBlueLight(go.transform, new Vector3(0f, 3.6f, 0f));
        }
        var ipt = VPoint<InfoPoint>(g, "InteractPoint_Makam", V2Pt(x1 + 2.4f, gl.z - 3.2f), "Baca papan Makam Desa", 3.6f);
        ipt.title = "Makam Desa Pemula";
        ipt.message = "MAKAM DESA PEMULA\n\nPemakaman tua di punggung bukit, tepat di belakang gereja. Di sini beristirahat penduduk kampung turun-temurun dan beberapa Hunter muda yang tak kembali dari Hutan Pemula.\n\n\"Yang pergi dengan berani, pulang dalam ingatan.\"\n\nDi tengah berdiri patung ksatria penjaga.";
        var keeper = VNpc("NPC_JuruKunci", "Pak Bram Tua", "Juru Kunci", "Peziarah", V2Pt(x1 - 3.4f, gl.z + 3.4f), false);
        keeper.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        var kip = VAttach<InfoPoint>(keeper, "Bicara: Juru Kunci", 3.4f);
        kip.title = "Juru Kunci Makam";
        kip.message = "Jangan khawatir, Nak. Mereka yang tidur di sini tidak mengganggu yang hidup.\n\nKadang kabut turun dari bukit di malam hari dan lentera kristal berpendar sendiri. Itu hanya doa lama yang belum selesai.\n\nAda satu nisan di sudut barat yang tak bernama. Aku merawatnya juga. Seseorang harus.";
    }

    // =====================================================================
    // Lapangan latihan Hunter (seberang kali, selatan)
    // =====================================================================
    static void V2TrainingYard()
    {
        var g = Group("KompleksPelatihan", tProps);
        var cs = VCrystalSlots();
        float deskZ = -73.5f;
        V2SwordRing(g, new Vector3(-66f, 0f, -88f));
        V2Archery(g, -45f, -77f);
        V2MagicCircle(g, new Vector3(-26f, 0f, -88f), cs);
        var b = new GeoBag();
        Vector3 toRoad = new Vector3(0f, 0f, 1f);
        VStation(g, b, V2Pt(-66f, deskZ), toRoad, "sword", "Pedang");
        VStation(g, b, V2Pt(-56f, deskZ), toRoad, "archery", "Memanah");
        VStation(g, b, V2Pt(-26f, deskZ), toRoad, "magic", "Sihir");
        // dekor: spanduk, lentera, bangku (slot biasa: Glow = cahaya lentera hangat, bukan kristal)
        VBannerPole(b, V2Pt(-74f, -77f), 3.8f, Sl.Cloth, 0f);
        VBannerPole(b, V2Pt(-18f, -77f), 3.8f, Sl.ClothB, 180f);
        VLanternPost(b, V2Pt(-61f, deskZ - 1f)); VLanternPost(b, V2Pt(-31f, deskZ - 1f)); VLanternPost(b, V2Pt(-46f, deskZ - 1f));
        VBench(b, V2Pt(-36f, -78f), 0f); VBench(b, V2Pt(-71f, -97f), 90f);
        b.Emit(g, "DekorPelatihan", baseSlots);
        VMapMark("Latihan Pedang", new Vector2(-66f, -88f), 4);
        VMapMark("Lapangan Memanah", new Vector2(-45f, -88f), 4);
        VMapMark("Uji Sihir", new Vector2(-26f, -88f), 4);
        ReserveBox(-46f, -87f, 62f, 32f, 0f, 0f);
    }

    static void V2SwordRing(Transform g, Vector3 c)
    {
        c.y = V2.H(c.x, c.z);
        var fb = new GeoBag();
        var pts = new List<Vector3>();
        for (int i = 0; i < 17; i++)
        {
            float a = (25f + i * 19.375f) * Mathf.Deg2Rad;          // celah di sisi utara (jalan masuk dari pos latihan)
            pts.Add(V2Pt(c.x + Mathf.Sin(a) * 8.5f, c.z + Mathf.Cos(a) * 8.5f));
        }
        V2Kit.RailFence(fb, pts, 1.3f, 2);
        // GeoBag.Disc/Annulus memakai tinggi ABSOLUT: tambahkan c.y
        fb.Disc(Sl.Dirt, c, 8.0f, 32, c.y + 0.04f);
        fb.Annulus(Sl.Stone, c, 3.0f, 3.3f, 0f, 360f, 32, c.y + 0.07f);          // cincin tanding
        fb.Annulus(Sl.Gold, c, 2.45f, 2.6f, 0f, 360f, 32, c.y + 0.08f);
        fb.Box(Sl.Stone, c + new Vector3(0f, 0.08f, 0f), new Vector3(5.0f, 0.03f, 0.12f));
        fb.Box(Sl.Stone, c + new Vector3(0f, 0.08f, 0f), new Vector3(0.12f, 0.03f, 5.0f));
        // dua rak senjata di dalam pagar, menghadap ke tengah
        foreach (Vector2 rk in new[] { new Vector2(-5.0f, 4.4f), new Vector2(5.6f, 3.2f) })
        {
            Vector3 rp = c + new Vector3(rk.x, 0f, rk.y);
            fb.Push(rp, Mathf.Atan2(-rk.x, -rk.y) * Mathf.Rad2Deg); V2Kit.WeaponRack(fb); fb.Pop();
            BoxCol(g, "RakCol", rp + Vector3.up * 1f, new Vector3(2.2f, 2f, 0.8f), new Vector3(0f, Mathf.Atan2(-rk.x, -rk.y) * Mathf.Rad2Deg, 0f));
        }
        fb.Emit(g, "LapanganPedang", baseSlots);
        V2FenceCols(g, pts, 1.3f);
        foreach (float da in new[] { 100f, 140f, 180f, 220f, 260f }) VDummy(g, V2Pt(c.x + Mathf.Sin(da * Mathf.Deg2Rad) * 5.6f, c.z + Mathf.Cos(da * Mathf.Deg2Rad) * 5.6f));
        var trainer = VNpc("NPC_Pelatih", "Pelatih Garr", "Pelatih Pedang", "Hunter", V2Pt(c.x + 1.8f, c.z - 0.2f), true);
        var tip = VAttach<InfoPoint>(trainer, "Bicara: Pelatih Garr", 3.4f);
        tip.title = "Pelatih Garr";
        tip.message = "Pukul boneka jerami itu, Nak. Tidak akan membalas.\n\n" +
            "KONTROL\n- WASD: jalan, Shift: lari, Ctrl: guling menghindar, Spasi: lompat\n- Klik kiri / J: serangan (kombo 3 kali; pukulan ketiga paling kuat)\n- Klik kanan: menahan serangan\n- E: bicara / ambil / buka\n\n" +
            "Pedang cocok untuk yang berani maju. Menahan serangan di saat yang tepat membuat lawan terbuka.\n" +
            "Di Level 5 kau memilih job (Swordsman, Archer, Penyihir Api, atau Penyihir Tanah) dan keterampilan baru terbuka.";
        Reserve(c.x, c.z, 11f);
    }

    static void V2Archery(Transform g, float cx, float zS)
    {
        var b = new GeoBag();
        float y0 = V2.H(cx, zS - 12f);
        b.Patch(Sl.Dirt, new Vector2(cx - 5.5f, -100f), new Vector2(cx - 5.5f, zS + 2f), new Vector2(cx + 5.5f, zS + 2f), new Vector2(cx + 5.5f, -100f), y0 + 0.04f, new Vector2(0f, 1f));
        b.Box(Sl.Marble, new Vector3(cx, y0 + 0.07f, zS), new Vector3(11f, 0.1f, 0.5f));
        var left = V2Poly(cx - 6f, zS + 2f, cx - 6f, -101f); var right = V2Poly(cx + 6f, zS + 2f, cx + 6f, -101f);
        V2Kit.RailFence(b, left, 1.2f, 2); V2Kit.RailFence(b, right, 1.2f, 2);
        float[] dz = { -83f, -89f, -95f };
        string[] dl = { "6", "12", "18" };
        for (int i = 0; i < 3; i++)
        {
            float x = cx + (i - 1) * 3.2f;
            b.Push(V2Pt(x, dz[i]), 0f); V2Kit.ArcheryTarget(b); b.Pop();
            b.Box(Sl.Marble, new Vector3(cx, y0 + 0.06f, dz[i] + 1.8f), new Vector3(11f, 0.06f, 0.14f));          // garis jarak
            Vector3 sp = V2Pt(cx - 5.1f, dz[i] + 1.8f);
            b.Cyl(Sl.Wood, sp, 0.07f, 1.2f, 6, 0.06f, true, false);
            b.Box(Sl.WoodLight, sp + new Vector3(0f, 1.35f, 0f), new Vector3(0.7f, 0.5f, 0.06f), 0f);
            BuildKit.Text(g, dl[i], sp + new Vector3(0f, 1.35f, 0.05f), 180f, 0.05f, new Color(1f, 0.92f, 0.6f));
            VDummy(g, V2Pt(x, dz[i] + 1.1f));
        }
        for (int i = -1; i <= 1; i++) b.Box(Sl.Thatch, new Vector3(cx + i * 3.6f, y0 + 1.2f, -100.2f), new Vector3(3.4f, 2.4f, 1.4f));
        b.Push(V2Pt(cx - 8.4f, zS - 0.5f), 0f); V2Kit.WeaponRack(b); b.Pop();
        b.Emit(g, "LapanganMemanah", baseSlots);
        V2FenceCols(g, left, 1.2f); V2FenceCols(g, right, 1.2f);
        BoxCol(g, "DindingJerami", new Vector3(cx, y0 + 1.2f, -100.2f), new Vector3(11f, 2.4f, 1.4f), Vector3.zero);
        var ar = VNpc("NPC_Pemanah", "Elowen", "Instruktur Panah", "Hunter", V2Pt(cx + 7.8f, zS - 0.5f), false);
        var ip = VAttach<InfoPoint>(ar, "Bicara: Elowen", 3.4f);
        ip.title = "Elowen, Pemanah";
        ip.message = "Berdiri di belakang garis putih. Tarik napas, bidik, lepaskan.\n\n" +
            "- Panah terbang lurus; bidik sedikit di atas sasaran yang jauh.\n- Tiga sasaran ada di jarak 6, 12, dan 18 langkah.\n- Pemanah (job Archer, Level 5) menembak dari jauh sambil bergerak: menghindar dulu, lalu menembak.\n\nBoneka di depan tiap sasaran tidak membalas. Pakailah sepuasnya.";
        Reserve(cx, -88f, 14f);
    }

    static void V2MagicCircle(Transform g, Vector3 c, Material[] cs)
    {
        c.y = V2.H(c.x, c.z);
        var b = new GeoBag();
        b.Push(c + new Vector3(0f, 0.05f, 0f), 0f);
        V2Kit.RuneCircle(b, 8f);                   // lantai batu, garis rune (Glow = kristal), 8 menhir, altar tengah
        b.Pop();
        b.Emit(g, "AreaSihir", cs);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad + 0.2f;
            Vector3 mp = V2Pt(c.x + Mathf.Sin(a) * 8f, c.z + Mathf.Cos(a) * 8f);
            BoxCol(g, "MenhirCol", mp + Vector3.up * 1.2f, new Vector3(1.1f, 2.4f, 1.1f), Vector3.zero);
        }
        BoxCol(g, "AltarCol", c + new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 2.4f), Vector3.zero);
        // kristal uji melayang di atas altar + cahaya biru
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
        // sasaran sihir: boneka di busur selatan lingkaran, masing-masing di atas alas marmer
        var pg = new GeoBag();
        for (int i = 0; i < 5; i++)
        {
            float a = (150f + i * 15f) * Mathf.Deg2Rad;
            Vector3 p = V2Pt(c.x + Mathf.Sin(a) * 12.5f, c.z + Mathf.Cos(a) * 12.5f);
            VDummy(g, p);
            pg.Cyl(Sl.Marble, p, 0.9f, 0.18f, 8, 0.9f, true);
        }
        pg.Emit(g, "AlasSasaran", cs);
        var mage = VNpc("NPC_Penyihir", "Lyra", "Instruktur Sihir", "Peziarah", V2Pt(c.x + 3.6f, c.z + 3.6f), false);
        var ip = VAttach<InfoPoint>(mage, "Bicara: Lyra", 3.4f);
        ip.title = "Lyra, Penyihir";
        ip.message = "Berdirilah di dalam lingkaran rune dan arahkan sihirmu ke boneka-boneka di seberang.\n\n" +
            "- Sihir api membakar dan meluas; sihir tanah menahan dan menghancurkan dari jauh.\n- Mana pulih sendiri bila kau berhenti merapal beberapa saat.\n- Job Penyihir Api dan Penyihir Tanah terbuka di Level 5.\n\nKristal di tengah lingkaran bergetar bila ada yang bisa memakai sihir. Kau mungkin salah satunya.";
        var kp = VPoint<InfoPoint>(g, "InteractPoint_KristalUji", c + new Vector3(0f, 0f, -2.6f), "Sentuh Kristal Uji", 3.2f);
        kp.title = "Kristal Uji Sihir";
        kp.message = "Kristal berdenyut lembut dan memantulkan warna biru.\n\n\"Bakat sihir sejati terlihat saat kau memilih jalanmu di Level 5,\" tulis ukiran di altar.\n\nBerlatihlah dulu di lingkaran ini.";
        Reserve(c.x, c.z, 20f);
    }

    // =====================================================================
    // Kali Embun: batu tepi, alang-alang
    // =====================================================================
    static void V2Stream()
    {
        var g = Group("TepiKali", tProps);
        var rng = new System.Random(77);
        var rb = new GeoBag();
        float total = V2.Len(V2.Stream);
        int rocks = 0;
        for (int i = 0; i < 400 && rocks < 70; i++)
        {
            Vector2 tg; Vector2 p = V2.At(V2.Stream, (float)rng.NextDouble() * total, out tg);
            Vector2 nr = new Vector2(tg.y, -tg.x);
            float hw = V2.StreamHalfWidth(p.x, p.y);
            float off = ((rng.NextDouble() < 0.5) ? -1f : 1f) * (hw * (1.0f + (float)rng.NextDouble() * 0.9f));
            Vector2 q = p + nr * off;
            float y = V2.H(q.x, q.y);
            if (y < -1.9f || y > 0.9f || V2OnBridge(q) || Mathf.Abs(q.x) > 225f || Mathf.Abs(q.y) > 225f) continue;
            int v = rng.Next(0, 4);
            float s = 0.5f + (float)rng.NextDouble() * 0.9f;
            var proto = PropKit.Get("Tut_Boulder" + v, bb => ForestKit.Boulder(bb, 50 + v * 7, 1f, v % 2 == 0));
            PropKit.Place(proto, g, "BatuKali", new Vector3(q.x, y - 0.12f * s, q.y), (float)rng.NextDouble() * 360f, s, ForestKit.Slots);
            rocks++;
        }
        int clumps = 0;
        for (int i = 0; i < 600 && clumps < 150; i++)
        {
            Vector2 tg; Vector2 p = V2.At(V2.Stream, (float)rng.NextDouble() * total, out tg);
            Vector2 nr = new Vector2(tg.y, -tg.x);
            float hw = V2.StreamHalfWidth(p.x, p.y);
            float off = ((rng.NextDouble() < 0.5) ? -1f : 1f) * (hw * (1.05f + (float)rng.NextDouble() * 0.55f));
            Vector2 q = p + nr * off;
            float y = V2.H(q.x, q.y);
            if (y < -1.75f || y > -0.2f || V2OnBridge(q) || Mathf.Abs(q.x) > 225f || Mathf.Abs(q.y) > 225f) continue;
            for (int k = 0; k < 6; k++)
            {
                float ox = (float)(rng.NextDouble() - 0.5) * 0.9f, oz = (float)(rng.NextDouble() - 0.5) * 0.9f;
                float h = 1.0f + (float)rng.NextDouble() * 0.9f;
                rb.Cone(k % 3 == 0 ? Sl.Hedge : Sl.Leaf, new Vector3(q.x + ox, y, q.y + oz), 0.045f, h, 3);
            }
            clumps++;
        }
        rb.Emit(g, "AlangAlang", baseSlots);
    }

    // =====================================================================
    // Lampu tiang
    // =====================================================================
    static void V2PlaceLantern(float x, float z, float yaw, bool light)
    {
        var g = tProps.Find("LampuDesa") != null ? tProps.Find("LampuDesa") : Group("LampuDesa", tProps);
        var go = V2PlaceProp("V2_Lantern", V2Kit.Lantern, g, "LampuDesa", V2Pt(x, z), yaw);
        if (light) AddLight(go.transform, new Vector3(0.55f, 2.8f, 0f), 15f, 1.5f);
        Reserve(x, z, 0.8f);
    }

    static void V2Lamps()
    {
        // sepanjang jalan utama & jalan timur: berselang-seling kiri-kanan, lentera menjorok ke jalan
        for (int ri = 0; ri < 2; ri++)
        {
            var r = V2.Roads[ri];
            float total = V2.Len(r.pts);
            int side = 1;
            for (float d = 14f; d < total - 20f; d += 24f)
            {
                Vector2 tg; Vector2 p = V2.At(r.pts, d, out tg);
                Vector2 nr = new Vector2(tg.y, -tg.x);
                Vector2 q = p + nr * side * (r.width * 0.5f + 1.1f);
                side = -side;
                if (V2OnBridge(q) || V2.DistPoly(q.x, q.y, V2.Stream) < 8f) continue;
                if (Vector2.Distance(q, V2.GateN) < 12f || Vector2.Distance(q, V2.GateE) < 12f) continue;
                if (q.y > 150f || q.x > 150f) continue;
                if (!IsFree(q.x, q.y, 0.8f)) continue;
                Vector2 toRoad = (p - q).normalized;          // arah dari lampu ke tengah jalan
                V2PlaceLantern(q.x, q.y, Mathf.Atan2(-toRoad.y, toRoad.x) * Mathf.Rad2Deg, true);
            }
        }
        // di sekitar fasilitas utama
        foreach (var l in V2.Lots)
        {
            if (l.id != "inn" && l.id != "smithy" && l.id != "elder" && l.id != "office" && l.id != "chapel") continue;
            float sgn = l.id == "elder" || l.id == "chapel" ? -1f : 1f;
            Vector2 q = V2.ToWorld(l, sgn * (l.w * 0.5f + 2.6f), l.d * 0.5f + 2.4f);
            if (!IsFree(q.x, q.y, 0.8f)) continue;
            V2PlaceLantern(q.x, q.y, l.yaw + (sgn > 0f ? 180f : 0f), true);
        }
    }

    // =====================================================================
    // Pohon & batu (hutan di tebing sekeliling cekungan, beberapa pohon penanda di dalam)
    // =====================================================================
    static void V2Trees()
    {
        var g = Group("Pohon", tProps);
        // sakura di bukit gereja (nuansa anime)
        foreach (var sp in new[] { new Vector2(-40f, 76f), new Vector2(-45f, 58f), new Vector2(-50f, 84f) })
        {
            var go = PropKit.Place(PropKit.TreeProto(2), g, "PohonSakura", V2Pt(sp.x, sp.y), sp.x * 7f, 1.25f, baseSlots);
            var cc = go.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.5f; cc.height = 3f;
            Reserve(sp.x, sp.y, 3f);
        }
        // birch tepi kali (di luar air)
        var rng = new System.Random(31);
        float total = V2.Len(V2.Stream);
        int bk = 0;
        for (int i = 0; i < 300 && bk < 26; i++)
        {
            Vector2 tg; Vector2 p = V2.At(V2.Stream, 20f + (float)rng.NextDouble() * (total - 40f), out tg);
            Vector2 nr = new Vector2(tg.y, -tg.x);
            float off = ((rng.NextDouble() < 0.5) ? -1f : 1f) * (9f + (float)rng.NextDouble() * 5f);
            Vector2 q = p + nr * off;
            if (Mathf.Abs(q.x) > 215f || Mathf.Abs(q.y) > 215f) continue;
            float y = V2.H(q.x, q.y);
            if (y < 0.2f || VSlopeDeg(q.x, q.y) > 25f || V2OnBridge(q)) continue;
            bool near = false; foreach (var r in V2.Roads) if (V2.DistPoly(q.x, q.y, r.pts) < r.width * 0.5f + 3.2f) { near = true; break; }
            if (near || !IsFree(q.x, q.y, 3f)) continue;
            Reserve(q.x, q.y, 3f);
            var go = PropKit.Place(vTreeProtos[2], g, "PohonBirch", new Vector3(q.x, y - 0.1f, q.y), (float)rng.NextDouble() * 360f, 1f + (float)rng.NextDouble() * 0.4f, ForestKit.Slots);
            var cc = go.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.4f; cc.height = 3f;
            bk++;
        }
        // hutan di lereng: cincin rapat di tepi cekungan (bd 0.92-1.3), lalu cincin jarang sampai punggung bukit
        int placed = 0;
        for (int i = 0; i < 30000 && placed < 1100; i++)
        {
            float th = Random.value * Mathf.PI * 2f;
            float bd = i % 4 == 3 ? Random.Range(1.3f, 1.85f) : Random.Range(0.92f, 1.3f);
            float x = V2.Basin.x + Mathf.Sin(th) * V2.BasinRx * bd, z = V2.Basin.y + Mathf.Cos(th) * V2.BasinRz * bd;
            if (Mathf.Abs(x) > 228f || Mathf.Abs(z) > 228f) continue;
            float y = V2.H(x, z);
            if (y < 0.5f || VSlopeDeg(x, z) > 33f) continue;
            bool near = false;
            foreach (var r in V2.Roads) if (V2.DistPoly(x, z, r.pts) < r.width * 0.5f + 7f) { near = true; break; }
            if (near || V2.DistPoly(x, z, V2.Stream) < 12f) continue;
            if (!IsFree(x, z, 3.2f)) continue;
            Reserve(x, z, 3.2f);
            VPlaceTree(g, x, z, 1f);
            placed++;
        }
        VScatterBoulders(Group("Batu", tProps), 70, 230f, (x, z) =>
        {
            float bd = Mathf.Sqrt(((x - V2.Basin.x) / V2.BasinRx) * ((x - V2.Basin.x) / V2.BasinRx) + ((z - V2.Basin.y) / V2.BasinRz) * ((z - V2.Basin.y) / V2.BasinRz));
            if (bd < 0.95f || V2.DistPoly(x, z, V2.Stream) < 10f) return false;
            foreach (var r in V2.Roads) if (V2.DistPoly(x, z, r.pts) < r.width * 0.5f + 8f) return false;
            return true;
        });
        VBoundary(234f, vMerged);
    }

    // =====================================================================
    // Warga yang berkeliling
    // =====================================================================
    /// <summary>Titik rute di sepanjang garis tengah jalan V2.Roads[ri], dari titik terdekat ke a sampai titik terdekat ke b; lateral = geser ke kanan arah jalan.</summary>
    static List<Vector2> V2RoadRoute(int ri, Vector2 a, Vector2 b, float step, float lateral = 0f)
    {
        var pts = V2.Roads[ri].pts;
        float total = V2.Len(pts), da = 0f, db = 0f, ba = 1e9f, bb = 1e9f;
        for (float d = 0f; d <= total; d += 0.5f)
        {
            Vector2 tg; Vector2 p = V2.At(pts, d, out tg);
            float ka = (p - a).sqrMagnitude, kb = (p - b).sqrMagnitude;
            if (ka < ba) { ba = ka; da = d; }
            if (kb < bb) { bb = kb; db = d; }
        }
        var list = new List<Vector2>();
        int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(db - da) / step) + 1);
        for (int i = 0; i < n; i++)
        {
            Vector2 tg; Vector2 p = V2.At(pts, Mathf.Lerp(da, db, i / (float)(n - 1)), out tg);
            list.Add(p + new Vector2(tg.y, -tg.x) * lateral);
        }
        return list;
    }

    static void V2Npcs()
    {
        // hijau desa & beringin: berkeliling pelan
        Npcs("Warga", 3, RoutePts("Hijau", ArcPts(new Vector2(-4f, 21f), 8f, 0f, 360f, 14)));
        Npcs("Pelajar", 1, RoutePts("Beringin", ArcPts(new Vector2(-14f, 15f), 6.4f, 0f, 360f, 12)));
        // jalan-jalan desa (mengikuti garis tengah jalan, sedikit bergeser agar tidak berhimpit)
        Npcs("Pedagang", 1, RoutePts("Pasar", V2RoadRoute(0, V(1f, -4f), V(2f, 16f), 6f, 3.2f)));
        Npcs("Warga", 2, RoutePts("JalanUtara", V2RoadRoute(0, V(2f, 24f), V(0f, 120f), 7f, 0.9f)));
        Npcs("Hunter", 2, RoutePts("JalanTimur", V2RoadRoute(1, V(8f, 17f), V(120f, -6f), 7f, -0.9f)));
        Npcs("Petani", 2, RoutePts("JalanLadang", V2RoadRoute(1, V(62f, 4f), V(112f, -6f), 6f, 0.9f)));
        Npcs("Warga", 1, RoutePts("JalurKincir", V2RoadRoute(3, V(-3f, -10f), V(-58f, -30f), 6f, 0.8f)));
        Npcs("Peziarah", 1, RoutePts("JalurGereja", V2RoadRoute(2, V(-4f, 52f), V(-40f, 66f), 6f, 0.8f)));
        Npcs("Petani", 1, RoutePts("KebunApel", Line(V(-62f, 98.2f), V(-30f, 98.2f), 6f)));
        Npcs("Pelajar", 1, RoutePts("SeberangKali", V2RoadRoute(7, V(2f, -64f), V(-19f, -79f), 6f, 0.8f)));
    }

    // =====================================================================
    // Papan denah desa (di dekat titik mulai)
    // =====================================================================
    static int V2KindOrder(int k)
    {
        switch (k) { case 6: return 0; case 5: return 1; case 0: return 2; case 1: return 3; case 2: return 4; case 3: return 5; default: return 6; }
    }

    static Texture2D V2RenderMap(out List<string> legend)
    {
        mpx = new Color32[MS * MS];
        mHalf = 150f;
        mScale = MS / (mHalf * 2f);
        var rng = new System.Random(5);
        Color paper = new Color(0.86f, 0.77f, 0.58f), ink = new Color(0.25f, 0.16f, 0.09f);
        for (int y = 0; y < MS; y++) for (int x = 0; x < MS; x++)
        {
            float n = ((x * 7 + y * 13) % 17) / 17f * 0.04f + (float)rng.NextDouble() * 0.03f;
            float edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(MS - 1 - x, MS - 1 - y)) / 60f;
            float dark = Mathf.Clamp01(1f - edge) * 0.35f;
            mpx[y * MS + x] = Color.Lerp(new Color(paper.r - n, paper.g - n, paper.b - n), new Color(0.45f, 0.32f, 0.18f), dark);
        }
        // cekungan desa (hijau); di luarnya lereng hutan (kertas)
        Color grass = new Color(0.62f, 0.74f, 0.45f);
        for (int y = 0; y < MS; y += 2) for (int x = 0; x < MS; x += 2)
        {
            float wx = x / mScale - mHalf, wz = y / mScale - mHalf;
            float ex = (wx - V2.Basin.x) / V2.BasinRx, ez = (wz - V2.Basin.y) / V2.BasinRz;
            if (ex * ex + ez * ez < 0.93f * 0.93f) { MPut(x, y, grass); MPut(x + 1, y, grass); MPut(x, y + 1, grass); MPut(x + 1, y + 1, grass); }
        }
        // kali
        for (int k = 0; k + 1 < V2.Stream.Count; k++) MLine(M2P(V2.Stream[k]), M2P(V2.Stream[k + 1]), 6f * mScale, new Color(0.45f, 0.68f, 0.85f));
        // jalan
        foreach (var r in V2.Roads)
            for (int k = 0; k + 1 < r.pts.Count; k++) MLine(M2P(r.pts[k]), M2P(r.pts[k + 1]), r.width * mScale + 3f, ink * 0.8f);
        foreach (var r in V2.Roads)
            for (int k = 0; k + 1 < r.pts.Count; k++) MLine(M2P(r.pts[k]), M2P(r.pts[k + 1]), r.width * mScale, new Color(0.93f, 0.86f, 0.68f));
        // jembatan (geladak melintang di atas kali)
        foreach (var br in V2.Bridges)
        {
            Vector2 dir = new Vector2(Mathf.Sin(br.yaw * Mathf.Deg2Rad), Mathf.Cos(br.yaw * Mathf.Deg2Rad));
            MLine(M2P(br.c - dir * (br.L * 0.5f)), M2P(br.c + dir * (br.L * 0.5f)), br.W * mScale + 5f, ink);
            MLine(M2P(br.c - dir * (br.L * 0.5f)), M2P(br.c + dir * (br.L * 0.5f)), br.W * mScale, br.stone ? new Color(0.78f, 0.76f, 0.7f) : new Color(0.72f, 0.55f, 0.36f));
        }
        // area (ladang, padang, kebun, makam, lapangan)
        foreach (var l in V2.Lots)
        {
            Color c;
            switch (l.kind)
            {
                case "field": c = new Color(0.82f, 0.7f, 0.3f); break;
                case "pasture": c = new Color(0.5f, 0.68f, 0.38f); break;
                case "orchard": c = new Color(0.9f, 0.62f, 0.62f); break;
                case "graveyard": c = new Color(0.7f, 0.7f, 0.68f); break;
                case "yard": c = new Color(0.78f, 0.62f, 0.4f); break;
                default: continue;
            }
            bool sw = Mathf.Abs(Mathf.Sin(l.yaw * Mathf.Deg2Rad)) > 0.7f;
            float k = l.kind == "pasture" ? 0.85f : 1f;
            MRect(M2P(new Vector2(l.x, l.z)), (sw ? l.d : l.w) * 0.5f * mScale * k, (sw ? l.w : l.d) * 0.5f * mScale * k, c);
        }
        // bangunan
        foreach (var l in V2.Lots)
        {
            if (l.kind == "field" || l.kind == "pasture" || l.kind == "orchard" || l.kind == "graveyard" || l.kind == "yard" || l.kind == "bridgepad" || l.kind == "stall") continue;
            bool sw = Mathf.Abs(Mathf.Sin(l.yaw * Mathf.Deg2Rad)) > 0.7f;
            MRect(M2P(new Vector2(l.x, l.z)), Mathf.Max(3f, (sw ? l.d : l.w) * 0.5f * mScale), Mathf.Max(3f, (sw ? l.w : l.d) * 0.5f * mScale), new Color(0.55f, 0.35f, 0.25f));
        }
        // bingkai persegi
        MRect(new Vector2(MS * 0.5f, 5f), MS * 0.5f, 5f, ink); MRect(new Vector2(MS * 0.5f, MS - 6f), MS * 0.5f, 5f, ink);
        MRect(new Vector2(5f, MS * 0.5f), 5f, MS * 0.5f, ink); MRect(new Vector2(MS - 6f, MS * 0.5f), 5f, MS * 0.5f, ink);
        // penanda bernomor (urut: pusat, gerbang, sipil, toko, dijual, religi, ladang/latihan)
        var items = new List<VMapItem>(vMap);
        var order = new Dictionary<VMapItem, int>();
        for (int i = 0; i < items.Count; i++) order[items[i]] = i;
        items.Sort((a, b) =>
        {
            int d = V2KindOrder(a.kind).CompareTo(V2KindOrder(b.kind));
            return d != 0 ? d : order[a].CompareTo(order[b]);
        });
        legend = new List<string>();
        int num = 0;
        foreach (var it in items)
        {
            num++;
            Vector2 p = M2P(it.pos);
            Color c = MKindColor(it.kind);
            float r = it.kind == 6 ? 22f : 17f;
            if (it.kind == 2) MRect(p, r, r, ink); else MDisc(p, r + 3f, ink);
            if (it.kind == 2) MRect(p, r - 3f, r - 3f, c); else MDisc(p, r, c);
            MNumber(num, p, num > 9 ? 4 : 5, ink);
            legend.Add(num + ". " + it.name);
        }
        // mata angin: anak panah utara + huruf N
        Vector2 na = new Vector2(MS - 90f, MS - 120f);
        MLine(na + new Vector2(0f, -40f), na + new Vector2(0f, 40f), 7f, ink);
        for (int i = 0; i < 18; i++) MLine(na + new Vector2(0f, 46f), na + new Vector2(-14f + i * 1.6f, 22f), 3f, ink);
        MGlyphDraw(10, na + new Vector2(-12f, 96f), 8, ink);
        var tex = new Texture2D(MS, MS, TextureFormat.RGBA32, false);
        tex.SetPixels32(mpx);
        tex.Apply();
        return tex;
    }

    /// <summary>Teks rata-kiri (TextMesh) yang terbaca dari sisi +Z papan; kiri teks berada di localPos.</summary>
    static void V2LeftText(Transform parent, string text, Vector3 localPos, float charSize, Color color)
    {
        var go = BuildKit.Text(parent, text, localPos, 180f, charSize, color);
        var tm = go.GetComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleLeft;
        tm.alignment = TextAlignment.Left;
    }

    static void V2BuildMapBoard()
    {
        List<string> legend;
        var tex = V2RenderMap(out legend);
        if (!AssetDatabase.IsValidFolder("Assets/Textures")) AssetDatabase.CreateFolder("Assets", "Textures");
        string tp = AssetDatabase.GenerateUniqueAssetPath("Assets/Textures/DenahKampungTepiKali.png");
        File.WriteAllBytes(tp, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(tp, ImportAssetOptions.ForceSynchronousImport);
        var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
        if (ti != null) { ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
        var tx = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        Shader sh = GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find("Unlit/Texture");
        if (sh == null) sh = Shader.Find("Unlit/Texture");
        var mat = new Material(sh);
        mat.mainTexture = tx;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tx);
        mat.name = "DenahKampungTepiKali";
        HeightBake.NewAsset(mat, MatDir + "/DenahKampungTepiKali.mat");

        // papan 9.4 m: peta di kiri (dari sudut pandang pembaca), legenda dua kolom di kanan. Muka baca = +Z lokal.
        Vector3 pos = V2Pt(-10f, -29f);
        var h = new GameObject("PapanDenahDesa").transform;
        h.SetParent(tProps, false);
        h.position = pos;
        h.rotation = Quaternion.Euler(0f, 90f, 0f);            // muka menghadap timur (ke jalan utama dan titik mulai)
        var b = new GeoBag();
        b.Cyl(Sl.Wood, new Vector3(-4.5f, 0f, 0f), 0.16f, 5.4f, 6, 0.14f, true, false);
        b.Cyl(Sl.Wood, new Vector3(4.5f, 0f, 0f), 0.16f, 5.4f, 6, 0.14f, true, false);
        b.Box(Sl.Wood, new Vector3(0f, 2.9f, -0.08f), new Vector3(9.4f, 4.4f, 0.14f));
        b.Box(Sl.WoodLight, new Vector3(0f, 2.9f, 0.02f), new Vector3(9.0f, 4.0f, 0.1f));
        b.Box(Sl.Roof, new Vector3(0f, 5.3f, 0f), new Vector3(10.0f, 0.18f, 1.3f));
        b.Emit(h, "Mesh", baseSlots);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "PetaDenah";
        Object.DestroyImmediate(quad.GetComponent<MeshCollider>());
        quad.transform.SetParent(h, false);
        quad.transform.localPosition = new Vector3(2.45f, 2.75f, 0.09f);
        quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        quad.transform.localScale = new Vector3(3.6f, 3.6f, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = mat;
        BuildKit.Text(h, "DENAH KAMPUNG TEPI KALI", new Vector3(-1.95f, 4.55f, 0.14f), 180f, 0.04f, new Color(1f, 0.9f, 0.55f));
        int perCol = (legend.Count + 1) / 2;
        float cs = Mathf.Min(0.021f, 3.2f / (7.4f * perCol));          // tinggi baris ~7.4 x charSize
        var colA = legend.GetRange(0, perCol);
        var colB = legend.GetRange(perCol, legend.Count - perCol);
        V2LeftText(h, string.Join("\n", colA.ToArray()), new Vector3(0.35f, 2.6f, 0.14f), cs, new Color(1f, 0.96f, 0.82f));
        V2LeftText(h, string.Join("\n", colB.ToArray()), new Vector3(-1.75f, 2.6f, 0.14f), cs, new Color(1f, 0.96f, 0.82f));
        BoxCol(h, "Col", new Vector3(0f, 2.7f, 0f), new Vector3(9.4f, 5.4f, 0.5f), Vector3.zero);
        AddLight(h, new Vector3(0f, 3.4f, 2.4f), 10f, 1.0f);
        Reserve(pos.x, pos.z, 5.5f);
        var ip = VPoint<InfoPoint>(h, "InteractPoint_Denah", h.TransformPoint(new Vector3(0f, 0f, 2.6f)), "Lihat Denah Kampung", 4f);
        ip.title = "Denah Kampung Tepi Kali";
        ip.message = "DENAH KAMPUNG TEPI KALI\n\n" + string.Join("\n", legend.ToArray()) +
            "\n\nKuning = rumah dijual. Biru = bangunan desa. Oranye = toko. Putih = gereja & makam. Hijau = ladang, ternak & latihan. Merah = gerbang.\nGaris biru = Kali Embun. Utara ada di atas peta.";
    }
}
