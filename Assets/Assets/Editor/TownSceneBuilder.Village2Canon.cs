using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// n82 - kanon Desa Pemula (LORE_BIBLE 8.2). Dipanggil V2Canon() setelah V2Scatter (urutan acak desa tidak bergeser) dan memakai IsFree supaya tidak menimpa pohon/semak sebaran.
///  - Garis garam di tiap ambang: bagian dari kit rumah (V2Kit.SaltAtDoor), bukan dari file ini.
///  - Beringin tua: tali lingkar setinggi bahu dengan tujuh helai kain; enam terikat, satu terlepas di antara akar.
///  - Rumah Basra (lot c15): meja di depan pintu dengan dua mangkuk sup bawang (satu mengepul, satu ditutup dan selalu dingin) + rak untaian bawang.
///  - Tali merah melintang di antara dua pohon di ujung utara ngarai (batas Hutan Pemula) + jejak sepatu anak yang berhenti di bawah tali. Tanpa collider.
/// Teks InfoPoint memakai register rakyat: tidak menyebut "yang ketujuh", Jeda, Sulung, atau Lucifer.
/// </summary>
public static partial class TownSceneBuilder
{
    /// <summary>Letak pintu (X lokal) tiap lot, diisi V2Lots; dibaca V2Basra.</summary>
    static readonly Dictionary<string, float> v2LotDoor = new Dictionary<string, float>();

    static void V2Canon()
    {
        V2BanyanTies();
        V2Basra();
        V2RedRope();
    }

    /// <summary>Salinan palet desa dengan warna kain pudar di slot yang tidak dipakai geometri kain (hanya berlaku untuk mesh yang di-Emit dengan palet ini).</summary>
    static Material[] V2ClothSlots()
    {
        var s = (Material[])baseSlots.Clone();
        s[Sl.Cloth] = UMat("V2_Ikat_MerahPudar", new Color(0.66f, 0.22f, 0.17f));         // tali merah dan helai kainnya (pudar oleh hujan)
        s[Sl.ClothB] = UMat("V2_Ikat_MerahBaru", new Color(0.80f, 0.12f, 0.09f));          // simpul yang selalu baru
        s[Sl.CoatA] = UMat("V2_Ikat_Jingga", new Color(0.80f, 0.32f, 0.22f));
        s[Sl.CoatB] = UMat("V2_Ikat_Biru", new Color(0.40f, 0.58f, 0.78f));
        s[Sl.CoatC] = UMat("V2_Ikat_Kuning", new Color(0.88f, 0.70f, 0.28f));
        s[Sl.Lip] = UMat("V2_Ikat_Mawar", new Color(0.84f, 0.52f, 0.58f));
        s[Sl.Blossom] = UMat("V2_Ikat_Hijau", new Color(0.50f, 0.66f, 0.38f));
        s[Sl.Marble] = UMat("V2_Ikat_Kapas", new Color(0.88f, 0.84f, 0.72f));              // helai yang terlepas
        s[Sl.Dark] = UMat("V2_Jejak_Lumpur", new Color(0.20f, 0.14f, 0.10f));               // jejak sepatu anak di tanah lembap
        return s;
    }

    /// <summary>Helai kain menggantung pada bidang singgung silinder: titik atas di (radius r, sudut ang), lebar w, panjang len, ujung bawah runcing, sedikit berayun.</summary>
    static void V2HangStrip(GeoBag b, int slot, float ang, float r, float yTop, float w, float len, float sway, float outSwing)
    {
        float a = ang * Mathf.Deg2Rad;
        Vector3 rad = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
        Vector3 tan = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
        Vector3 top = rad * r + Vector3.up * yTop;
        float hw = w * 0.5f;
        Vector3 tl = top - tan * hw, tr = top + tan * hw;
        Vector3 mid = top + Vector3.down * (len * 0.72f) + rad * (outSwing * 0.6f) + tan * (sway * 0.6f);
        Vector3 ml = mid - tan * (hw * 0.95f), mr = mid + tan * (hw * 0.95f);
        Vector3 tip = top + Vector3.down * len + rad * outSwing + tan * sway;
        b.QuadBoth(slot, tl, tr, mr, ml);
        b.Tri(slot, ml, mr, tip);
        b.Tri(slot, mr, ml, tip);
    }

    // =====================================================================
    // Beringin tua: tali lingkar + tujuh helai kain (enam terikat, satu terlepas)
    // Batang beringin (skala 2,3) berjari-jari 1,5-1,65 m pada tinggi 1,3-2,2 m (diukur dari mesh); di atas 2,2 m mengerucut dan bercabang.
    // =====================================================================
    static void V2BanyanTies()
    {
        var g = Group("KanonDesa_Beringin", tProps);
        Vector3 op = V2Pt(V2.Oak.x, V2.Oak.y);
        const float rBand = 1.6f, yHoop = 2.15f;
        var b = new GeoBag();
        b.Push(op, 0f);
        // tali lingkar (dua gulungan tipis) + simpul
        b.Cyl(Sl.Wood, new Vector3(0f, yHoop - 0.07f, 0f), rBand + 0.01f, 0.09f, 20, rBand + 0.01f, false, false);
        b.Cyl(Sl.WoodLight, new Vector3(0f, yHoop + 0.04f, 0f), rBand + 0.02f, 0.06f, 20, rBand + 0.02f, false, false);

        // arah dari beringin ke titik mulai: helai yang terlepas dihadapkan ke sana supaya terlihat dari jalan
        float faceSpawn = Mathf.Atan2(V2.Spawn.x - V2.Oak.x, V2.Spawn.y - V2.Oak.y) * Mathf.Rad2Deg;
        int[] slot = { Sl.CoatA, Sl.CoatB, Sl.CoatC, Sl.Blossom, Sl.Lip, Sl.Cloth, Sl.Marble };
        float[] len = { 0.95f, 0.80f, 1.00f, 0.85f, 0.90f, 0.75f };
        for (int i = 0; i < 7; i++)
        {
            float ang = faceSpawn + (i - 6) * (360f / 7f);
            float ar = ang * Mathf.Deg2Rad;
            Vector3 rad = new Vector3(Mathf.Sin(ar), 0f, Mathf.Cos(ar));
            if (i < 6)
            {
                float jit = (V2Kit.Rnd(9, i) - 0.5f);
                // kain menjuntai dari tali lingkar dan menyusuri tonjolan akar di bawahnya (batang melebar ke bawah), jadi bagian bawah helai bergeser ke luar
                V2HangStrip(b, slot[i], ang, rBand + 0.07f, yHoop - 0.02f, 0.46f, len[i], jit * 0.10f, 0.34f + 0.10f * V2Kit.Rnd(10, i));
                b.Ball(slot[i], rad * (rBand + 0.09f) + Vector3.up * (yHoop - 0.01f), 0.10f, 3, 6);        // simpul rapi
            }
            else
            {
                // simpul yang terlepas: dua ujung tali menjuntai, kosong
                Vector3 tan = new Vector3(Mathf.Cos(ar), 0f, -Mathf.Sin(ar));
                b.Beam(Sl.Wood, rad * (rBand + 0.04f) + Vector3.up * (yHoop - 0.03f) - tan * 0.04f, rad * (rBand + 0.12f) + Vector3.up * (yHoop - 0.40f) - tan * 0.10f, 0.03f, 0.03f);
                b.Beam(Sl.Wood, rad * (rBand + 0.04f) + Vector3.up * (yHoop - 0.03f) + tan * 0.04f, rad * (rBand + 0.10f) + Vector3.up * (yHoop - 0.30f) + tan * 0.12f, 0.03f, 0.03f);
                // helainya tergeletak di antara akar, agak jauh dari batang
                var pts = new List<Vector2>();
                float ground = 0f; int cnt = 0;
                var raw = new List<Vector2>();
                for (int k = 0; k <= 6; k++)
                {
                    float t = k / 6f;
                    float aa = (ang - 14f + t * 30f) * Mathf.Deg2Rad;
                    float rr = 2.95f + 0.9f * t + 0.18f * Mathf.Sin(t * 5.2f);
                    raw.Add(new Vector2(Mathf.Sin(aa) * rr, Mathf.Cos(aa) * rr));
                    ground += V2.H(op.x + raw[k].x, op.z + raw[k].y) - op.y; cnt++;
                }
                float gy = ground / cnt + 0.045f;
                foreach (var q in raw) pts.Add(q);
                b.Ribbon(slot[6], pts, 0.26f, gy);
                b.Ball(slot[6], new Vector3(raw[0].x, gy + 0.03f, raw[0].y), 0.07f, 3, 5, 0.7f);              // sisa simpul di ujung kain
            }
        }
        b.Pop();
        var go = b.Emit(g, "IkatKainBeringin", V2ClothSlots());
        go.transform.position = Vector3.zero;

        // titik baca, di sisi selatan beringin (antara dua bangku)
        float ia = (faceSpawn + 38f) * Mathf.Deg2Rad;
        Vector3 ip = V2Pt(op.x + Mathf.Sin(ia) * 3.5f, op.z + Mathf.Cos(ia) * 3.5f);
        var info = VPoint<InfoPoint>(g, "InteractPoint_Beringin", ip, "Lihat kain di batang beringin", 3.8f);
        info.title = "Beringin Tua";
        info.message = "Beringin tertua di Desa Pemula, lebih tua dari rumah mana pun di kampung. Seutas tali melingkari batangnya setinggi bahu, dan dari tali itu tergantung tujuh helai kain berwarna pudar.\n\n" +
                       "Enam helai terikat dengan simpul yang rapi. Satu helai lagi selalu tergeletak di antara akar, dan simpulnya terurai. Setiap tahun baru semua simpul diikat ulang, dan setiap tahun yang satu itu terlepas lagi sebelum musim berganti. Orang tua melarang anak-anak memungutnya.";
    }

    // =====================================================================
    // Rumah Basra (lot c15): meja dengan dua mangkuk sup bawang + rak untaian bawang
    // =====================================================================
    static void V2Basra()
    {
        V2.Lot l = null;
        foreach (var q in V2.Lots) if (q.id == "c15") { l = q; break; }
        float dx;
        if (l == null || !v2LotDoor.TryGetValue("c15", out dx)) return;
        var g = Group("KanonDesa_RumahBasra", tProps);
        float side = dx >= 0f ? 1f : -1f;                         // sisi pintu: halaman kebun ada di sisi sebaliknya (V2Yard)
        float front = l.d * 0.5f;

        var os = (Material[])baseSlots.Clone();
        os[Sl.CoatC] = UMat("V2_Bawang_Kuning", new Color(0.80f, 0.60f, 0.30f));
        os[Sl.CoatA] = UMat("V2_Bawang_Merah", new Color(0.58f, 0.24f, 0.28f));

        var b = new GeoBag();
        b.Push(new Vector3(l.x, l.py, l.z), l.yaw);

        // ---- meja + dua mangkuk + dua bangku
        float tx = dx + side * 2.1f, tz = front + 1.7f;
        b.Push(new Vector3(tx, 0f, tz), -12f * side);
        b.Box(Sl.WoodLight, new Vector3(0f, 0.80f, 0f), new Vector3(1.2f, 0.06f, 0.74f));
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                b.Box(Sl.Wood, new Vector3(sx * 0.52f, 0.39f, sz * 0.30f), new Vector3(0.07f, 0.78f, 0.07f));
        b.Box(Sl.Wood, new Vector3(0f, 0.22f, 0f), new Vector3(1.0f, 0.035f, 0.05f));
        // mangkuk hangat: sup bawang keemasan di permukaan
        b.Cyl(Sl.Wood, new Vector3(-0.28f, 0.83f, 0.03f), 0.075f, 0.09f, 10, 0.16f, true, false, Sl.Thatch);
        b.Beam(Sl.Wood, new Vector3(-0.26f, 0.93f, 0.02f), new Vector3(-0.02f, 1.0f, -0.12f), 0.025f, 0.02f);                // sendok bersandar
        // mangkuk dingin: ditutup piring kecil dan selembar kain lipat
        b.Cyl(Sl.Wood, new Vector3(0.28f, 0.83f, -0.02f), 0.075f, 0.09f, 10, 0.16f, true, false, Sl.Wood);
        b.Cyl(Sl.WoodLight, new Vector3(0.28f, 0.925f, -0.02f), 0.19f, 0.022f, 10, 0.19f, true);
        b.Box(Sl.PlasterB, new Vector3(0.28f, 0.95f, -0.02f), new Vector3(0.24f, 0.018f, 0.20f), 14f);
        b.Pop();
        // bangku: satu menghadap rumah dan agak ditarik keluar, satu rapat di bawah meja
        b.Cyl(Sl.Wood, new Vector3(tx - 0.15f * side, 0f, tz - 0.95f), 0.21f, 0.43f, 8, 0.19f, true);
        b.Cyl(Sl.Wood, new Vector3(tx + 0.25f * side, 0f, tz + 0.62f), 0.21f, 0.43f, 8, 0.19f, true);

        // ---- rak jemuran untaian bawang (sisi kebun dari pintu)
        float rx = dx - side * 2.4f, rz = front + 1.9f;
        b.Push(new Vector3(rx, 0f, rz), 6f);
        V2Kit.Post(b, Sl.Wood, new Vector3(-0.9f, 0f, 0f), 1.95f, 0.055f);
        V2Kit.Post(b, Sl.Wood, new Vector3(0.9f, 0f, 0f), 1.95f, 0.055f);
        b.Beam(Sl.Wood, new Vector3(-0.98f, 1.9f, 0f), new Vector3(0.98f, 1.9f, 0f), 0.07f, 0.07f);
        for (int k = 0; k < 6; k++)
        {
            float x = -0.68f + k * 0.27f;
            float drop = 0.78f + 0.18f * V2Kit.Rnd(41, k);
            b.Beam(Sl.Thatch, new Vector3(x, 1.88f, 0f), new Vector3(x, 1.88f - drop, 0f), 0.03f, 0.03f);
            int bulbs = 4 + (k % 3);
            for (int j = 0; j < bulbs; j++)
            {
                float yy = 1.76f - j * (drop - 0.14f) / bulbs;
                float off = (j % 2 == 0 ? 1f : -1f) * 0.035f;
                b.Ball(((j + k) % 3 == 0) ? Sl.CoatA : Sl.CoatC, new Vector3(x + off, yy, 0.02f * off / 0.035f), 0.085f + 0.02f * V2Kit.Rnd(43 + k, j), 3, 6, 1.0f);
            }
            b.Cyl(Sl.Thatch, new Vector3(x, 1.88f - drop - 0.1f, 0f), 0.01f, 0.12f, 5, 0.045f, true);                       // jumbai daun kering di ujung (meruncing ke bawah)
        }
        b.Pop();
        b.Pop();
        b.Emit(g, "MejaDanRakBawang", os);

        // collider kecil: meja saja (bangku dan rak dilewati)
        Vector2 tw = V2.ToWorld(l, tx, tz);
        BoxCol(g, "MejaCol", new Vector3(tw.x, V2.H(tw.x, tw.y) + 0.45f, tw.y), new Vector3(1.2f, 0.9f, 0.74f), new Vector3(0f, l.yaw - 12f * side, 0f));
        Reserve(tw.x, tw.y, 1.8f);
        Vector2 rw = V2.ToWorld(l, rx, rz);
        Reserve(rw.x, rw.y, 1.5f);

        // titik baca: di depan meja, tidak menutupi jalan ke pintu
        Vector3 ip = V2Local(l, dx + side * 1.0f, front + 3.4f);
        var info = VPoint<InfoPoint>(g, "InteractPoint_RumahBasra", ip, "Lihat meja di depan rumah Basra", 3.6f);
        info.title = "Meja di Depan Rumah Basra";
        info.message = "Meja kayu kecil di depan pintu rumah Basra. Bau bawang goreng dan kaldu tercium sampai ke jalan, dan untaian bawang kering bergantung di rak sebelah pintu.\n\n" +
                       "Di atas meja ada dua mangkuk sup bawang yang diletakkan berhadapan. Satu masih mengepul. Satunya ditutup piring kecil dan selembar kain, dan kata para tetangga isinya selalu sudah dingin, padahal Basra mengisinya pada saat yang sama. " +
                       "Tak seorang pun pernah melihat mangkuk itu dimakan, dan tak seorang pun berani bertanya untuk siapa.";
    }

    // =====================================================================
    // Tali merah di batas hutan (ujung utara ngarai) + jejak sepatu anak
    // =====================================================================
    static bool V2FreeNear(ref float x, ref float z, float r)
    {
        float[] dz = { 0f, 1.2f, -1.2f, 2.4f, -2.4f, 3.6f, -3.6f };
        for (int i = 0; i < dz.Length; i++)
            if (IsFree(x, z + dz[i], r)) { z += dz[i]; return true; }
        return false;
    }

    /// <summary>Helai kain bergantung pada tali: badan persegi lalu dua ujung berbentuk ekor layang (takik V), sedikit berayun.</summary>
    static void V2RopeStrip(GeoBag b, int slot, Vector3 p, Vector3 along, float hw, float len, Vector3 sway)
    {
        Vector3 tl = p - along * hw, tr = p + along * hw;
        Vector3 ml = tl + Vector3.down * (len * 0.72f) + sway * 0.6f, mr = tr + Vector3.down * (len * 0.72f) + sway * 0.6f;
        Vector3 notch = (ml + mr) * 0.5f + Vector3.down * (len * 0.12f) + sway * 0.2f;
        Vector3 lt = ml + Vector3.down * (len * 0.28f) + sway * 0.4f, rt = mr + Vector3.down * (len * 0.28f) + sway * 0.4f;
        b.QuadBoth(slot, tl, tr, mr, ml);
        b.Tri(slot, ml, notch, lt); b.Tri(slot, lt, notch, ml);
        b.Tri(slot, notch, mr, rt); b.Tri(slot, rt, mr, notch);
    }

    static void V2RedRope()
    {
        const float z0 = 226f;
        var g = Group("KanonDesa_TaliMerah", tProps);
        // kaki lereng ngarai di kedua sisi jalan: titik tambat pertama yang naik 0,25 m dari dasar
        float floor = V2.H(0f, z0);
        float xa = 4.2f;
        for (float x = 3.4f; x < 7.2f; x += 0.1f)
            if (V2.H(-x, z0) > floor + 0.25f && V2.H(x, z0) > floor + 0.25f) { xa = x; break; }
        float xw = -xa, zw = z0, xe = xa, ze = z0;
        V2FreeNear(ref xw, ref zw, 1.6f);
        V2FreeNear(ref xe, ref ze, 1.6f);
        Vector3 pw = V2Pt(xw, zw), pe = V2Pt(xe, ze);
        var treeW = PropKit.Place(vTreeProtos[0], g, "PohonTali_Barat", pw + Vector3.down * 0.1f, 25f, 0.85f, ForestKit.Slots);
        var treeE = PropKit.Place(vTreeProtos[0], g, "PohonTali_Timur", pe + Vector3.down * 0.1f, 205f, 0.78f, ForestKit.Slots);
        foreach (var t in new[] { treeW, treeE })
        {
            var cc = t.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.5f; cc.height = 3f;
        }
        Reserve(pw.x, pw.z, 2.2f); Reserve(pe.x, pe.z, 2.2f);

        var rs = V2ClothSlots();
        var b = new GeoBag();
        const float tie = 1.05f, rTrunk = 0.6f;
        Vector3 dir = pe - pw; dir.y = 0f; dir.Normalize();
        Vector3 A = pw + dir * rTrunk + Vector3.up * tie;
        Vector3 B = pe - dir * rTrunk + Vector3.up * tie;
        float span = new Vector2(B.x - A.x, B.z - A.z).magnitude;
        float sag = 0.22f + 0.012f * span;
        const int N = 14;
        var rope = new Vector3[N + 1];
        for (int i = 0; i <= N; i++)
        {
            float t = i / (float)N;
            Vector3 p = Vector3.Lerp(A, B, t);
            p.y = Mathf.Lerp(A.y, B.y, t) - sag * 4f * t * (1f - t);
            float gy = V2.H(p.x, p.z);
            if (p.y < gy + 0.8f) p.y = gy + 0.8f;                  // jangan melorot ke tanah
            rope[i] = p;
        }
        for (int i = 0; i < N; i++) b.Beam(Sl.Cloth, rope[i], rope[i + 1], 0.06f, 0.06f);
        // helai kain pudar bergantung pada tali
        float[] st = { 0.17f, 0.31f, 0.46f, 0.60f, 0.74f, 0.88f };
        for (int i = 0; i < st.Length; i++)
        {
            float f = st[i] * N; int k = Mathf.Min(N - 1, (int)f); float u = f - k;
            Vector3 p = Vector3.Lerp(rope[k], rope[k + 1], u);
            float lenS = 0.42f + 0.30f * V2Kit.Rnd(61, i);
            Vector3 along = (rope[k + 1] - rope[k]); along.y = 0f; along.Normalize();
            Vector3 nrm = new Vector3(along.z, 0f, -along.x);               // bidang kain menghadap sepanjang jalan
            float hw = 0.07f + 0.03f * V2Kit.Rnd(62, i);
            Vector3 sw = along * ((V2Kit.Rnd(63, i) - 0.5f) * 0.14f) + nrm * 0.06f;
            V2RopeStrip(b, Sl.Cloth, p, along, hw, lenS, sw);
        }
        // simpul baru di tiap batang: pita merah menyala melingkari batang + dua ujung menjuntai
        Vector3[] tp = { pw, pe };
        Vector3[] ends = { A, B };
        for (int e = 0; e < 2; e++)
        {
            Vector3 c = tp[e] + Vector3.up * (tie - 0.12f);
            b.Cyl(Sl.ClothB, c, rTrunk + 0.03f, 0.24f, 12, rTrunk + 0.03f, false, false);
            b.Ball(Sl.ClothB, ends[e], 0.1f, 3, 6);
            Vector3 d2 = (e == 0 ? dir : -dir);
            Vector3 n2 = new Vector3(d2.z, 0f, -d2.x);
            b.QuadBoth(Sl.ClothB, ends[e] - n2 * 0.04f, ends[e] + n2 * 0.04f, ends[e] + n2 * 0.05f + Vector3.down * 0.55f, ends[e] - n2 * 0.05f + Vector3.down * 0.55f);
            b.QuadBoth(Sl.ClothB, ends[e] + d2 * 0.05f - n2 * 0.03f, ends[e] + d2 * 0.05f + n2 * 0.03f, ends[e] + d2 * 0.10f + n2 * 0.03f + Vector3.down * 0.42f, ends[e] + d2 * 0.10f - n2 * 0.03f + Vector3.down * 0.42f);
        }

        // jejak sepatu anak: lurus dari arah desa, berhenti tepat di bawah tali
        int nPrint = 24;
        float zs = z0 - 14f, ze2 = z0 - 1.05f;
        for (int i = 0; i < nPrint; i++)
        {
            float z = zs + (ze2 - zs) * i / (nPrint - 1);
            float x = 1.15f + 0.30f * Mathf.Sin(z * 0.37f);
            float slope = 0.30f * 0.37f * Mathf.Cos(z * 0.37f);
            float yaw = Mathf.Atan(slope) * Mathf.Rad2Deg;
            float sd = (i % 2 == 0) ? -1f : 1f;
            float px = x + sd * 0.11f, pz = z;
            float gy = V2.H(px, pz) + 0.085f;
            float yr = yaw * Mathf.Deg2Rad;
            b.Box(Sl.Dark, new Vector3(px, gy, pz), new Vector3(0.085f, 0.014f, 0.16f), yaw);
            b.Box(Sl.Dark, new Vector3(px - Mathf.Sin(yr) * 0.115f, gy, pz - Mathf.Cos(yr) * 0.115f), new Vector3(0.068f, 0.014f, 0.05f), yaw);
        }
        b.Emit(g, "TaliMerahDanJejak", rs);

        var info = VPoint<InfoPoint>(g, "InteractPoint_TaliMerah", V2Pt(-1.6f, z0 - 2.6f), "Lihat tali merah", 3.8f);
        info.title = "Tali Merah di Batas Hutan";
        info.message = "Seutas tali merah melintang di antara dua pohon, setinggi pinggang. Kainnya pudar oleh hujan, tetapi simpul di kedua batang selalu tampak baru.\n\n" +
                       "Di jalan sebelum tali ada jejak sepatu kecil, berjajar lurus dari arah desa. Jejak itu berhenti tepat di bawah tali. Di seberangnya, tanah yang sama lembapnya tidak menyimpan satu pun jejak.";
    }
}
