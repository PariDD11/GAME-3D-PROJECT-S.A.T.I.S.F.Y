using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>Rawa Terkutuk: desa nelayan Kelabu (hidup), desa hantu Merana, gubuk penyihir, lingkar ritual, menara Maelis. Koordinat = koordinat DESAIN.</summary>
public static partial class HuntSceneBuilder
{
    static Transform tFish, tGhost, tWitch, tNecro;

    // =====================================================================
    // Rencana tata letak
    // =====================================================================
    static void PlanSwamp()
    {
        plan.Clear();
        PlanFish();
        PlanGhost();
        PlanWitch();
        PlanRitualAndTower();
        SanitizePlan();
    }

    static void PlanFish()
    {
        Vector2 c = FishC;
        clearings.Add(new Vector3(c.x, c.y, 50f));
        Add("well", c.x, c.y, 0f, 1);

        // rumah panggung di tepi danau (barat) dan di darat (timur)
        float[] angS = { 232, 252, 288, 308 };
        for (int i = 0; i < angS.Length; i++)
        {
            Vector2 p = Pt(c, angS[i], 25f);
            Add("shouse", p.x, p.y, FaceYaw(p, c), 410 + i, 0f, 5.4f + (i % 2) * 0.8f, 4.6f);
        }
        float[] angL = { 20, 62, 150, 200, 340 };
        for (int i = 0; i < angL.Length; i++)
        {
            Vector2 p = Pt(c, angL[i], 27f);
            Add("shouse", p.x, p.y, FaceYaw(p, c), 430 + i, 0f, 5.6f + (i % 3) * 0.7f, 4.8f);
            Vector2 toC = (c - p).normalized;
            trails.Add(new[] { p + toC * 5.5f, c - toC * 10f });
        }
        Vector2 innP = Pt(c, 106, 26);
        Add("sinn", innP.x, innP.y, FaceYaw(innP, c), 450);
        trails.Add(new[] { Local(innP, FaceYaw(innP, c), 0f, 7f), c + (innP - c).normalized * 10f });
        Vector2 sp = Pt(c, 60, 12.5f);
        Add("stall", sp.x, sp.y, FaceYaw(sp, c) + 180f, 3);
        for (int i = 0; i < 6; i++)
        {
            Vector2 lp = Pt(c, 30f + i * 60f, 17.5f);
            Add("lamp", lp.x, lp.y, 0f, i, 0f, 1f);
        }
        Vector2 bp = Pt(c, 160, 15);
        Add("board", bp.x, bp.y, FaceYaw(bp, c), 1);

        // dermaga, perahu, jaring
        Add("dock", -138f, -128f, -90f, 1, 0f, 17f);
        Add("dock", -138f, -72f, -90f, 2, 0f, 14f);
        Add("dock", -196f, -153f, 0f, 3, 0.55f, 12f);
        Add("boat", -152f, -131.5f, -90f, 1);
        Add("boat", -158f, -69.5f, -90f, 2);
        Add("boat", -185f, -92f, 35f, 3);
        Add("boat", -170f, -118f, 80f, 4);
        Add("boat", -215f, -70f, 20f, 5, 0f, 0.45f);
        Vector2 n1 = Pt(c, 250, 15), n2 = Pt(c, 296, 15);
        Add("netrack", n1.x, n1.y, 90f, 1);
        Add("netrack", n2.x, n2.y, 90f, 2);
        Add("crate", -134f, -121f, 20f, 31);
        Add("barrel", -134f, -118f, 0f, 32);
        Add("crate", -134f, -78f, 70f, 33);
        Vector2 cartP = Pt(c, 120, 44);
        Add("cart", cartP.x, cartP.y, 70f, 4);
        for (int i = 0; i < 6; i++)
        {
            Vector2 pp = Pt(c, i * 60f + 25f, 44f + (i % 3) * 5f);
            Add(i % 2 == 0 ? "willow" : "cypress", pp.x, pp.y, i * 70f, 60 + i);
        }
    }

    static void PlanGhost()
    {
        Vector2 m = GhostC;
        clearings.Add(new Vector3(m.x, m.y, 52f));
        Add("well", m.x, m.y, 0f, 2, 0.85f);
        Add("gallows", m.x + 8f, m.y + 6f, 40f, 1);

        for (int i = 0; i < 9; i++)
        {
            Vector2 p = Pt(m, i * 40f + 12f, 26f + (i % 3) * 2.5f);
            if (Mathf.Abs(Mathf.DeltaAngle(i * 40f + 12f, 0f)) < 22f) continue;     // ruang untuk kapel di utara
            float w = 5.2f + (i % 3) * 0.8f, d = 4.6f + (i % 2) * 0.7f;
            Add("rhouse", p.x, p.y, FaceYaw(p, m), 510 + i, Mathf.Min(1f, 0.5f + (i % 5) * 0.12f), w, d, i % 2);
        }
        Vector2 chap = Pt(m, 0, 40);
        Add("chapel", chap.x, chap.y, FaceYaw(chap, m), 31, 0.4f, 0f, 0f, 0, 26f);
        trails.Add(new[] { Local(chap, FaceYaw(chap, m), 0f, 12f), m + (chap - m).normalized * 8f });

        // kuburan (timur)
        Vector2 gy = Pt(m, 95, 40);
        for (int i = 0; i < 20; i++)
        {
            float gx = gy.x + (i % 5 - 2f) * 2.5f + Mathf.Sin(i * 7.3f) * 0.4f;
            float gz = gy.y + (i / 5 - 1.5f) * 2.8f + Mathf.Cos(i * 5.1f) * 0.4f;
            Add("grave", gx, gz, 90f + Mathf.Sin(i * 3.1f) * 18f, 130 + i, 0f, i % 3 == 0 ? 1f : 0f);
        }
        Add("fence", gy.x - 14f, gy.y, 90f, 1, 0.4f, 8f);
        Add("fence", gy.x - 14f, gy.y + 8f, 90f, 2, 0.4f, 8f);
        Add("fence", gy.x - 14f, gy.y - 8f, 90f, 3, 0.5f, 8f);
        Vector2 cr1 = gy + new Vector2(13f, 5f), cr2 = gy + new Vector2(13f, -7f);
        Add("crypt", cr1.x, cr1.y, 270f, 1, 0.3f, 1f);
        Add("crypt", cr2.x, cr2.y, 270f, 2, 0.5f, 1f);

        for (int i = 0; i < 6; i++)
        {
            Vector2 lp = Pt(m, 15f + i * 60f, 15f);
            Add("ghostlamp", lp.x, lp.y, i * 40f, i);
        }
        Vector2 cg1 = Pt(m, 200, 14), cg2 = Pt(m, 320, 20);
        Add("cage", cg1.x, cg1.y, 30f, 1);
        Add("cage", cg2.x, cg2.y, 200f, 2);
        Vector2 cartP = Pt(m, 145, 15);
        Add("cart", cartP.x, cartP.y, 30f, 9, 0.8f);
        Add("bones", m.x - 10f, m.y - 6f, 40f, 4);
        Add("bones", m.x + 14f, m.y + 18f, 200f, 5);
        Vector2 rb = Pt(m, 252, 38);
        Add("ruinboard", rb.x, rb.y, FaceYaw(rb, m), 1);
        for (int k = 0; k < 9; k++)
        {
            Vector2 bp = Pt(m, k * 40f + 5f, 46f + (k % 3) * 4f);
            Add(k % 3 == 0 ? "willow" : "deadtree", bp.x, bp.y, k * 70f, 20 + k);
        }
    }

    static void PlanWitch()
    {
        Vector2 c = WitchC;
        clearings.Add(new Vector3(c.x, c.y, 42f));
        float hutYaw = FaceYaw(c, V(-170f, 120f));
        Add("witchhut", c.x, c.y, hutYaw, 5);
        Vector2 cauld = Local(c, hutYaw, 3.4f, 7.5f);
        Add("cauldron", cauld.x, cauld.y, 0f, 1);
        Add("book", cauld.x + 2.4f, cauld.y - 0.6f, hutYaw, 1);
        for (int k = 0; k < 4; k++)
        {
            Vector2 tp = Pt(c, 35f + k * 90f, 16f);
            Add("totem", tp.x, tp.y, k * 70f, 10 + k);
        }
        for (int k = 0; k < 3; k++)
        {
            Vector2 tp = Pt(c, 120f + k * 60f, 13f);
            Add("tent", tp.x, tp.y, FaceYaw(tp, c), 140 + k, 0f, k % 2);
        }
        Vector2 fire = Local(c, hutYaw, -4f, 11f);
        Add("fire", fire.x, fire.y, 0f, 6);
        for (int k = 0; k < 7; k++)
        {
            Vector2 mp = Pt(c, k * 51f + 8f, 6f + (k % 4) * 3.5f);
            Add(k % 2 == 0 ? "bush" : "mush", mp.x, mp.y, k * 40f, 70 + k);
        }
        Vector2 cg = Pt(c, 270, 22);
        Add("cage", cg.x, cg.y, 90f, 3);
        Add("bones", c.x - 14f, c.y - 6f, 70f, 6);
        Add("bones", c.x + 12f, c.y - 18f, 10f, 7);
        Vector2 sb = Pt(c, 60, 34);
        Add("ruinboard", sb.x, sb.y, FaceYaw(sb, c), 2);
        for (int k = 0; k < 8; k++)
        {
            Vector2 bp = Pt(c, k * 45f + 22f, 40f + (k % 3) * 4f);
            Add(k % 2 == 0 ? "deadtree" : "cypress", bp.x, bp.y, k * 70f, 90 + k);
        }
    }

    static void PlanRitualAndTower()
    {
        // lingkar ritual
        Vector2 r = RitualC;
        clearings.Add(new Vector3(r.x, r.y, 30f));
        Add("ritual", r.x, r.y, 0f, 4, 0f);
        Vector2 t1 = Pt(r, 90, 13), t2 = Pt(r, 270, 13);
        Add("totem", t1.x, t1.y, 90f, 41);
        Add("totem", t2.x, t2.y, 270f, 42);
        Add("bones", r.x + 10f, r.y - 12f, 20f, 8);
        Add("mush", r.x - 9f, r.y + 11f, 0f, 3);
        Add("mush", r.x + 12f, r.y + 9f, 0f, 4);

        // menara Maelis
        Vector2 n = NecroC;
        float yawN = FaceYaw(n, V(60f, 270f));
        clearings.Add(new Vector3(n.x, n.y, 40f));
        Add("spire", n.x, n.y, yawN, 7, 0.15f);
        float[] angC = { 20, 60, 100, 140, 175, 300, 340 };
        for (int i = 0; i < angC.Length; i++)
        {
            Vector2 p = Pt(n, angC[i], 46f + (i % 2) * 5f);
            Add("crypt", p.x, p.y, FaceYaw(p, n), 600 + i, 0.15f + (i % 3) * 0.2f, 1f);
        }
        Vector2 gy = Pt(n, 60, 70);
        for (int i = 0; i < 24; i++)
        {
            float gx = gy.x + (i % 6 - 2.5f) * 2.6f + Mathf.Sin(i * 7.3f) * 0.4f;
            float gz = gy.y + (i / 6 - 1.5f) * 3f + Mathf.Cos(i * 5.1f) * 0.4f;
            Add("grave", gx, gz, 200f + Mathf.Sin(i * 3.1f) * 18f, 700 + i, 0f, i % 3 == 0 ? 1f : 0f);
        }
        float front = Mathf.Atan2(60f - n.x, 270f - n.y) * Mathf.Rad2Deg;
        for (int i = 0; i < 6; i++)
        {
            Vector2 lp = Pt(n, front + (i % 2 == 0 ? -1f : 1f) * (12f + (i / 2) * 5f), 24f + (i / 2) * 9f);
            Add("ghostlamp", lp.x, lp.y, 0f, 20 + i);
        }
        Vector2 cg = Pt(n, front + 30f, 36f);
        Add("cage", cg.x, cg.y, 20f, 4);
        Add("cage", cg.x - 30f, cg.y + 6f, 140f, 5);
        Add("gallows", n.x - 44f, n.y - 18f, 200f, 2);
        for (int k = 0; k < 10; k++)
        {
            Vector2 bp = Pt(n, k * 36f + 4f, 78f + (k % 3) * 5f);
            Add(k % 3 == 0 ? "willow" : "deadtree", bp.x, bp.y, k * 70f, 800 + k);
        }
        Vector2 sbp = Pt(n, front, 52f);
        Add("ruinboard", sbp.x + 5f, sbp.y, FaceYaw(sbp, n), 3);
    }

    // =====================================================================
    // Instansiasi
    // =====================================================================
    /// <summary>mode 0 = di tanah, 1 = di permukaan air, 2 = di tanah atau air (mana yang lebih tinggi, untuk rumah panggung).</summary>
    static GameObject PlaceS(Plc p, string key, System.Action<GeoBag> build, Transform parent, bool collider, float scale = 1f, int mode = 0)
    {
        var proto = PropKit.Get(key, build);
        Vector3 pos = G(p.x, p.z);
        if (mode == 1) pos.y = WaterY; else if (mode == 2) pos.y = Mathf.Max(pos.y, WaterY);
        var go = PropKit.Place(proto, parent, p.kind, pos, p.yaw + (M < 0f ? 180f : 0f), scale, ForestKit.SSlots);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = proto.mesh;
        return go;
    }

    static InfoPoint MakeInfo(GameObject host, string prompt, string title, string message, float radius)
    {
        var ip = host.AddComponent<InfoPoint>();
        ip.prompt = prompt; ip.radius = radius; ip.title = title; ip.message = message;
        return ip;
    }

    static void BuildSwampSites()
    {
        tFish = Group("Desa_Nelayan_Kelabu");
        tGhost = Group("Desa_Merana_Hantu");
        tWitch = Group("Gubuk_Penyihir");
        tNecro = Group("Ritual_dan_Menara");
        if (mFxSmoke == null) EnsureFxMats();
        foreach (var p in plan)
        {
            Transform parent;
            if (Dist(p.x, p.z, FishC.x, FishC.y) < 90f && p.x < -90f && p.x > -260f) parent = tFish;
            else if (Dist(p.x, p.z, GhostC.x, GhostC.y) < 75f) parent = tGhost;
            else if (Dist(p.x, p.z, WitchC.x, WitchC.y) < 60f) parent = tWitch;
            else parent = tNecro;
            int sd = p.seed; float ruin = p.ruin;
            string rk = "_" + (int)(ruin * 100f);
            switch (p.kind)
            {
                case "shouse":
                {
                    float w = p.a, d = p.b;
                    var go = PlaceS(p, "SHouse_" + sd + "_" + (int)(w * 10f) + "_" + (int)(d * 10f), b => ForestKit.StiltHouse(b, sd, w, d, 0f), parent, true, 1f, 2);
                    if (sd % 2 == 0)
                    {
                        var sm = new GameObject("ChimneySmoke").AddComponent<ChimneySmoke>();
                        sm.material = mFxSmoke;
                        sm.transform.SetParent(parent, false);
                        sm.transform.position = go.transform.TransformPoint(new Vector3(w * 0.3f, 1.5f + 2.7f + 2.4f, -d * 0.2f));
                    }
                    break;
                }
                case "sinn": PlaceS(p, "SInn_" + sd, b => ForestKit.StiltInn(b, sd), parent, true, 1f, 2); break;
                case "stall": PlaceS(p, "SStall_" + (sd % 2), b => ForestKit.Stall(b, sd % 2), parent, true); break;
                case "well": PlaceS(p, "SWell" + rk, b => ForestKit.Well(b, sd, ruin), parent, true); break;
                case "cart": PlaceS(p, "SCart" + rk, b => ForestKit.Cart(b, sd, ruin), parent, true); break;
                case "crate": PlaceS(p, "SCrateP_" + (sd % 3), b => ForestKit.Crate(b, sd % 3), parent, true); break;
                case "barrel": PlaceS(p, "SBarrelP", b => ForestKit.Barrel(b), parent, true); break;
                case "netrack": PlaceS(p, "SNet_" + (sd % 2), b => ForestKit.NetRack(b, sd), parent, false); break;
                case "dock": { float len = p.a; float rr = ruin; PlaceS(p, "SDock_" + sd + "_" + (int)len + rk, b => ForestKit.Dock(b, sd, len, rr), parent, true, 1f, 1); break; }
                case "boat": { float sk = p.a; PlaceS(p, "SBoat_" + sd + "_" + (int)(sk * 100f), b => ForestKit.Boat(b, sd, sk), parent, false, 1f, 1); break; }
                case "lamp":
                {
                    var lm = PlaceS(p, "SLamp", b => ForestKit.LanternPost(b, false), parent, false, 1f, 2);
                    AddLight(lm.transform, new Vector3(0.5f, 2.3f, 0f), new Color(1f, 0.74f, 0.4f), 14f, 1.3f, "LampLight");
                    break;
                }
                case "ghostlamp":
                {
                    var lm = PlaceS(p, "SGhostLamp", b => ForestKit.LanternPost(b, true), parent, false);
                    AddLight(lm.transform, new Vector3(0.5f, 2.3f, 0f), new Color(0.4f, 1f, 0.7f), 15f, 1.5f, "SpiritLight");
                    break;
                }
                case "board":
                {
                    var bd = PlaceS(p, "SBoard", b => ForestKit.NoticeBoard(b), parent, false);
                    MakeInfo(bd, "Baca papan pengumuman", "Papan Desa Kelabu",
                        "PERINGATAN UNTUK PARA PENJELAJAH\n\n- Nelayan yang tenggelam di danau kini berjalan lagi di malam hari. Jangan dekati tepi air saat gelap.\n- Desa Merana (timur) ditinggalkan setelah wabah. Kerangka dan hantu berkeliaran di sana.\n- Sekelompok penyihir tinggal di barat laut. Jangan menerima tawaran mereka.\n- Ada cahaya hijau di menara utara. Tidak ada yang kembali dari sana.\n\n- Tetua Gantar", 3f);
                    break;
                }
                case "ruinboard":
                {
                    var bd = PlaceS(p, "SBoardRuin_" + sd, b => ForestKit.NoticeBoard(b), parent, false);
                    string title, msg;
                    if (parent == tGhost) { title = "Papan Merana"; msg = "Papan lapuk ini ditumbuhi lumut. Tulisan kapur yang masih terbaca:\n\n\"... air sumur berubah hijau. Yang minum, tidak bangun lagi. Kami mengubur mereka di timur. Kini kuburan itu pun tak tenang ...\"\n\nBeberapa coretan baru menambahkan: \"PERGI.\""; }
                    else if (parent == tWitch) { title = "Peringatan Sekte"; msg = "Tiang kayu bertuliskan arang dan darah:\n\n\"Wilayah Sekte Kabut. Penyusup menjadi bahan ramuan.\"\n\nTengkorak kecil diikat di bawah papan, menghadap ke jalan."; }
                    else { title = "Gerbang Maelis"; msg = "Pada papan batu tertulis dengan huruf bercahaya hijau:\n\n\"Mereka yang mati belum selesai. Maelis akan menyelesaikannya.\"\n\nUdara di sini dingin dan berbau tanah makam."; }
                    MakeInfo(bd, "Periksa papan", title, msg, 3f);
                    break;
                }
                case "rhouse":
                {
                    float w = p.a, d = p.b; int rf = p.roof; float rr = ruin;
                    PlaceS(p, "SRHouse_" + sd + "_" + (int)(w * 10f) + "_" + (int)(d * 10f) + rk, b => ForestKit.House(b, sd, w, d, 3.0f, rr, rf, false), parent, true);
                    break;
                }
                case "chapel": { float rr = ruin; PlaceS(p, "SChapel" + rk, b => ForestKit.Chapel(b, sd, rr), parent, true); break; }
                case "crypt":
                {
                    float rr = ruin; float sc = p.a > 0.5f ? 1f : 0.8f;
                    var cr = PlaceS(p, "SCrypt_" + sd + rk, b => ForestKit.Crypt(b, sd, rr, 1f), parent, true, sc);
                    AddLight(cr.transform, new Vector3(0, 3.0f, 3.6f), new Color(0.4f, 1f, 0.7f), 9f, 1.0f, "CryptGlow");
                    break;
                }
                case "grave": { bool cross = p.a > 0.5f; PlaceS(p, "SGrave_" + sd + (cross ? "x" : "r"), b => ForestKit.Gravestone(b, sd, cross), parent, false); break; }
                case "fence": { float len = p.a; float rr = ruin; PlaceS(p, "SFence_" + sd + rk, b => ForestKit.Fence(b, sd, len, rr), parent, false); break; }
                case "gallows": PlaceS(p, "SGallows", b => ForestKit.Gallows(b, sd), parent, true); break;
                case "cage": PlaceS(p, "SCage", b => ForestKit.HangingCage(b, sd), parent, false); break;
                case "deadtree": PlaceS(p, "SDeadV_" + (sd % 3), b => ForestKit.SwampDeadTree(b, sd, 1.1f), parent, true); break;
                case "willow": { var go = PlaceS(p, "SWillowV_" + (sd % 3), b => ForestKit.Willow(b, sd % 3 + 20, 1.2f), parent, false); go.AddComponent<CapsuleCollider>().height = 4f; break; }
                case "cypress": { var go = PlaceS(p, "SCypressV_" + (sd % 3), b => ForestKit.BaldCypress(b, sd % 3 + 30, 1.1f), parent, false); go.AddComponent<CapsuleCollider>().height = 8f; break; }
                case "bones": PlaceS(p, "SBonesV_" + (sd % 3), b => ForestKit.BeastSkeleton(b, sd % 3 + 1), parent, false, 1.3f); break;
                case "bush": PlaceS(p, "SBushV_" + (sd % 3), b => ForestKit.Bush(b, sd % 3 + 7, ForestKit.LeafOrange, 1.2f), parent, false); break;
                case "mush": PlaceS(p, "SMushV_" + (sd % 3), b => ForestKit.GlowMushrooms(b, sd % 3), parent, false, 1.6f); break;
                case "witchhut":
                {
                    var hut = PlaceS(p, "SWitchHut", b => ForestKit.WitchHut(b, sd), parent, true);
                    AddLight(hut.transform, new Vector3(0, 2.6f, 3.0f), new Color(0.4f, 1f, 0.6f), 14f, 1.4f, "HutGlow");
                    break;
                }
                case "cauldron":
                {
                    var ca = PlaceS(p, "SCauldron", b => ForestKit.Cauldron(b, sd), parent, true, 1.3f);
                    AddLight(ca.transform, new Vector3(0, 2.0f, 0), new Color(0.4f, 1f, 0.55f), 12f, 1.5f, "BrewGlow");
                    var sm = new GameObject("BrewSmoke").AddComponent<ChimneySmoke>();
                    sm.material = mFxSmoke;
                    sm.transform.SetParent(ca.transform, false);
                    sm.transform.localPosition = new Vector3(0, 1.8f, 0);
                    break;
                }
                case "book":
                {
                    var host = new GameObject("BukuMantra");
                    host.transform.SetParent(parent, false);
                    host.transform.position = G(p.x, p.z);
                    MakeInfo(host, "Periksa buku mantra", "Buku Mantra Sekte Kabut",
                        "Buku tebal dengan sampul kulit lembap terbuka di atas tong. Halaman terakhir:\n\n\"Air desa nelayan sudah dikutuk. Tiga malam lagi mereka akan bangun. Maelis di menara memberi kita kekuatan, dan sebagai gantinya kita memberinya yang mati.\"\n\nTulisan itu diikuti gambar menara dengan bola hijau di puncaknya.", 4.5f);
                    break;
                }
                case "totem": PlaceS(p, "STotem_" + (sd % 4), b => ForestKit.BoneTotem(b, sd), parent, true); break;
                case "tent": { int cs = p.a > 0.5f ? Sl.ClothB : Sl.Cloth; PlaceS(p, "STentB_" + cs, b => ForestKit.Tent(b, sd, cs), parent, true, 1.25f); break; }
                case "fire":
                {
                    var f = PlaceS(p, "SWitchFire", b => ForestKit.Campfire(b, sd), parent, false, 1.3f);
                    AddLight(f.transform, new Vector3(0, 1.4f, 0), new Color(0.6f, 1f, 0.5f), 18f, 1.5f, "FireLight");
                    break;
                }
                case "ritual":
                {
                    var rt = PlaceS(p, "SRitual", b => ForestKit.RitualCircle(b, sd, 0f), parent, false);
                    AddLight(rt.transform, new Vector3(0, 3.0f, 0), new Color(0.4f, 1f, 0.7f), 28f, 1.8f, "RitualGlow");
                    var host = new GameObject("RitualAltar");
                    host.transform.SetParent(parent, false);
                    host.transform.position = G(p.x, p.z);
                    MakeInfo(host, "Periksa altar ritual", "Altar Ritual",
                        "Lingkaran batu berlumut mengelilingi altar hitam. Simbol bintang bercahaya hijau menyala redup di tanah, dan tengkorak di atas altar menatap ke arah menara di utara.\n\nBau belerang dan daging terbakar menggantung di udara.", 7f);
                    break;
                }
                case "spire":
                {
                    float rr = ruin;
                    var sp = PlaceS(p, "SSpire", b => ForestKit.NecroSpire(b, sd, rr), parent, true);
                    AddLight(sp.transform, new Vector3(0, 30f, 0), new Color(0.4f, 1f, 0.65f), 70f, 2.4f, "SpireGlow");
                    AddLight(sp.transform, new Vector3(0, 4f, 12f), new Color(0.4f, 1f, 0.7f), 26f, 1.6f, "GateGlow");
                    var host = new GameObject("GerbangMaelis");
                    host.transform.SetParent(parent, false);
                    host.transform.position = G(p.x, p.z) + sp.transform.forward * 18f;
                    MakeInfo(host, "Periksa gerbang menara", "Menara Maelis",
                        "Menara tulang dan batu hitam menjulang, dikelilingi kristal hijau yang berdenyut seperti jantung. Tulang belulang ditumpuk di sepanjang tangga.\n\nDi balik gerbang, sosok berjubah berdiri membelakangi cahaya. Maelis tidak terburu-buru; dia hanya menunggu seseorang cukup bodoh untuk mendekat.", 6f);
                    break;
                }
                case "smoke": break;
            }
        }
        BuildFishNpcs();
        BuildFishInteractions();
        BuildLakeLife();
        BuildGhostExtras();
    }

    // =====================================================================
    // Danau: teratai, ilalang, perahu terapung
    // =====================================================================
    static void BuildLakeLife()
    {
        var root = Group("LakeLife");
        int lily = 0, cat = 0;
        for (int i = 0; i < 30000 && (lily < 110 || cat < 70); i++)
        {
            float x = Random.Range(-440f, 440f), z = Random.Range(-440f, 420f);
            if (Dist(x, z, Camp.x, Camp.y) < 55f) continue;
            float depth = WaterY - GroundY(x, z);
            if (depth > 0.25f && depth < 1.4f && lily < 110)
            {
                var proto = PropKit.Get("SLily_" + (lily % 4), b => ForestKit.LilyPads(b, lily % 4 + 100));
                var go = PropKit.Place(proto, root, "Lily", new Vector3(M * x, WaterY, M * z), Random.Range(0f, 360f), Random.Range(0.8f, 1.5f), ForestKit.SSlots);
                lily++;
            }
            else if (depth > -0.2f && depth < 0.5f && cat < 70)
            {
                var proto = PropKit.Get("SCattailP_" + (cat % 3), b => ForestKit.Cattail(b, cat % 3 + 200));
                PropKit.Place(proto, root, "Cattail", G(x, z), Random.Range(0f, 360f), Random.Range(1f, 1.7f), ForestKit.SSlots);
                cat++;
            }
        }
    }

    static void BuildGhostExtras()
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 p = Pt(GhostC, i * 60f + 35f, 8f + (i % 3) * 14f);
            var proto = PropKit.Get("SGhostMush", b => ForestKit.GlowMushrooms(b, 5));
            PropKit.Place(proto, tGhost, "GlowShroom", G(p.x, p.y), i * 50f, 1.5f, ForestKit.SSlots);
        }
    }

    // =====================================================================
    // NPC & interaksi Desa Kelabu
    // =====================================================================
    static void BuildFishNpcs()
    {
        tVillagers = Group("Villagers");
        vRng = new System.Random(823);
        Vector2 c = FishC;
        Vector2 innP = Pt(c, 106, 26);
        float innYaw = FaceYaw(innP, c);
        Vector2 innDoor = Local(innP, innYaw, 0f, 7f / 2f + 2.6f);

        VNpc("Ibu Marni", "Pemilik Penginapan", NpcKit.OPelayan, NpcKit.Human, true, innDoor, innYaw + 180f,
            "Masuklah, masuklah! Ikan bakar dan kamar kering, hal paling mewah di rawa ini.|Jangan keluar dekat danau saat tengah malam. Suamiku juga bilang begitu... sebelum dia tenggelam.", null, false);
        VNpc("Pak Darsa", "Pedagang Ikan", NpcKit.OPedagang, NpcKit.Human, false, Pt(c, 60, 10.7f), FaceYaw(Pt(c, 60, 10.7f), c) + 180f,
            "Ikan asap, ramuan, tali, dan lentera! Semua yang kamu perlukan untuk bertahan di rawa.|Kabut makin tebal tiap tahun. Pelangganku makin sedikit, hantunya makin banyak.", null, false);
        Vector2 shr = Pt(c, 340, 18);
        VNpc("Tetua Gantar", "Tetua Desa", NpcKit.OPendeta, NpcKit.Elf, false, shr, FaceYaw(shr, c) + 180f,
            "Sudah tiga musim air danau berbau seperti kuburan. Para penyihir di barat laut meracuninya.|Mereka dipimpin sesuatu di menara utara. Maelis. Dulu dia hanya tabib desa Merana.|Kalau kamu menyingkirkan mereka, Kelabu akan mengingat namamu.", null, false);
        Vector2 gate = c + (V(-10f, -260f) - c).normalized * 42f;
        VNpc("Wira", "Penjaga Desa", NpcKit.OPenjaga, NpcKit.Dwarf, false, gate, FaceYaw(gate, c) + 180f,
            "Berhenti. ...Hunter, ya? Silakan.|Aku jaga pos ini sendirian. Malam hari yang naik dari danau bukan ikan.", null, true);
        VNpc("Tabib Sari", "Tabib", NpcKit.OPendeta, NpcKit.Human, true, Pt(c, 120, 10f), 0f,
            "Teratai rawa bisa menawar demam kabut. Kalau kamu menemukan banyak, bawakan ke aku.|Jangan minum air mentah. Pakai air hujan dari tampungan di Pos.", null, false);

        var dock = VRoute("FishDock", V(-139f, -128f), V(-150f, -128f), V(-154f, -128f), V(-146f, -128f));
        VNpc("Jaka", "Nelayan", NpcKit.OPetani, NpcKit.Human, false, V(-139f, -128f), 0f, "Hari ini tangkapan sepi. Ikan-ikan menjauh dari tengah danau.|Aku tidak mau mendayung ke sana. Ada sesuatu di dalam air, kadang kelihatan wajah.", dock, false);
        var plaza = VRoute("FishPlaza", Pt(c, 0, 18), Pt(c, 70, 18), Pt(c, 140, 18), Pt(c, 210, 18), Pt(c, 280, 18), Pt(c, 340, 18));
        VNpc("Lintang", "Anak Desa", NpcKit.OWarga, NpcKit.Beast, false, Pt(c, 0, 18), 0f, "Aku lihat lampu hijau menari di atas air! Kata Ibu itu hantu, kata Ayah itu gas rawa.|Aku tidak takut. ...Sedikit.", plaza, false);
        var net = VRoute("FishNets", Pt(c, 245, 14), Pt(c, 270, 16), Pt(c, 298, 14));
        VNpc("Nenek Rukmi", "Penjaring", NpcKit.OWarga, NpcKit.Human, true, Pt(c, 245, 14), 0f, "Jaring ini sudah tiga generasi. Dulu penuh ikan, sekarang penuh lumut.|Kalau lihat orang berjalan di dasar danau, jangan sapa.", net, false);
    }

    static void BuildFishInteractions()
    {
        Vector2 c = FishC;
        Vector2 innP = Pt(c, 106, 26);
        float innYaw = FaceYaw(innP, c);
        Vector2 door = Local(innP, innYaw, 0f, 7f / 2f + 2.2f);
        var io = new GameObject("InnDoor");
        io.transform.SetParent(tFish, false);
        io.transform.position = G(door.x, door.y);
        var inn = io.AddComponent<Inn>();
        inn.prompt = "Penginapan Kelabu"; inn.radius = 3.8f;

        Vector2 sp = Pt(c, 60, 10.7f);
        var so = new GameObject("StallShop");
        so.transform.SetParent(tFish, false);
        so.transform.position = G(sp.x, sp.y);
        var shop = so.AddComponent<GeneralShop>();
        shop.prompt = "Lapak Pak Darsa"; shop.radius = 3.4f;

        var wo = new GameObject("VillageWell");
        wo.transform.SetParent(tFish, false);
        wo.transform.position = G(c.x, c.y + 1.6f);
        var ws = wo.AddComponent<WaterSource>();
        ws.prompt = "Sumur Desa"; ws.radius = 3.4f;

        var go = new GameObject("ChapelAltar");
        go.transform.SetParent(tGhost, false);
        Vector2 chap = Pt(GhostC, 0, 40);
        go.transform.position = G(chap.x, chap.y);
        MakeInfo(go, "Periksa altar kapel", "Kapel Merana",
            "Atap kapel bocor dan bangku-bangku ambruk. Di altar, kristal hijau tumbuh di antara lilin yang sudah membusuk. Jendela-jendela bersinar redup seolah masih ada yang berdoa di dalam.\n\nDi lantai ada nama-nama yang ditulis dengan kapur: seluruh penduduk Merana. Satu nama dicoret berkali-kali: Maelis.", 8f);
    }

    // =====================================================================
    // Efek dunia
    // =====================================================================
    static void BuildSwampFx()
    {
        if (mFxSmoke == null) EnsureFxMats();
        if (mFxMist == null) mFxMist = FxMat("FxMist", FxUtil.SoftDot(1.2f));
        var fx = Group("WorldFx");
        var mist = new GameObject("SwampMist").AddComponent<SwampMist>();
        mist.transform.SetParent(fx, false);
        mist.material = mFxMist;
    }
}
