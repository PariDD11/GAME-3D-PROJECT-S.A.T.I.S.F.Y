using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>Gurun Zahar: tata letak desa Zafir (hidup), desa Sahr (terbengkalai), kuil, bandit, dan efek dunia. Koordinat = koordinat DESAIN.</summary>
public static partial class HuntSceneBuilder
{
    static Transform tZafir, tSahr, tTemples;
    static Material mFxSand;

    // =====================================================================
    // Rencana tata letak
    // =====================================================================
    static void PlanDesert()
    {
        plan.Clear();
        PlanZafir();
        PlanSahr();
        PlanTemples();
        SanitizePlan();
    }

    static void PlanZafir()
    {
        Vector2 c = ZafirC;
        clearings.Add(new Vector3(c.x, c.y, 54f));
        Add("well", c.x, c.y, 0f, 1);

        float[] ang = { 15, 55, 95, 135, 165, 300, 340 };
        float[] rad = { 26, 28, 25, 27, 26, 27, 25 };
        for (int i = 0; i < ang.Length; i++)
        {
            Vector2 p = Pt(c, ang[i], rad[i]);
            float w = 5.4f + (i % 3) * 0.8f, d = 4.6f + (i % 2) * 0.7f;
            float yaw = FaceYaw(p, c);
            Add("dhouse", p.x, p.y, yaw, 210 + i, 0f, w, d, i % 3 == 0 ? 2 : 1);
            Vector2 toC = (c - p).normalized;
            trails.Add(new[] { p + toC * (d / 2f + 2.6f), c - toC * 10f });
        }
        Vector2 innP = Pt(c, 205, 24);
        float innYaw = FaceYaw(innP, c);
        Add("dinn", innP.x, innP.y, innYaw, 240, 0f, 11f, 8f);
        trails.Add(new[] { Local(innP, innYaw, 0f, 8f), c + (innP - c).normalized * 10f });
        Vector2 oven = Local(innP, innYaw, 11f * 0.25f, -8f / 2f - 1f);
        Add("smoke", oven.x, oven.y, 0f, 0, 0f, 1.6f);

        foreach (float a in new float[] { 60, 120, 240, 300 })
        {
            Vector2 sp = Pt(c, a, 12.5f);
            Add("dstall", sp.x, sp.y, FaceYaw(sp, c) + 180f, (int)a);
        }
        for (int i = 0; i < 6; i++)
        {
            Vector2 lp = Pt(c, 30f + i * 60f, 17.5f);
            Add("brazier", lp.x, lp.y, 0f, i, 0f, i % 2 == 0 ? 1f : 0f);
        }
        Vector2 shr = Pt(c, 330, 44);
        Add("dshrine", shr.x, shr.y, FaceYaw(shr, c), 250);
        trails.Add(new[] { Local(shr, FaceYaw(shr, c), 0f, 5f), c + (shr - c).normalized * 10f });
        Vector2 bp = Pt(c, 350, 15);
        Add("board", bp.x, bp.y, FaceYaw(bp, c), 1);

        Vector2 f1 = Pt(c, 272, 37), f2 = Pt(c, 296, 42);
        Add("crops", f1.x, f1.y, 90f, 1, 0f, 9f, 6f, 1);
        Add("crops", f2.x, f2.y, 60f, 2, 0f, 8f, 6f, 1);
        Vector2 cartP = Pt(c, 90, 44);
        Add("cart", cartP.x, cartP.y, 80f, 4);
        for (int k = 0; k < 3; k++)
        {
            Vector2 jp = Local(innP, innYaw, -6.5f + k * 1.2f, 5.8f);
            Add(k == 1 ? "crate" : "jars", jp.x, jp.y, k * 40f, 30 + k);
        }
        // palem di sekeliling desa dan kolam
        float[] pa = { 0, 40, 80, 120, 160, 200, 250, 285, 320 };
        for (int i = 0; i < pa.Length; i++)
        {
            Vector2 pp = Pt(c, pa[i], 38f + (i % 3) * 5f);
            if (Dist(pp.x, pp.y, Oases[0].x, Oases[0].y) < 38f) continue;
            Add("palm", pp.x, pp.y, pa[i] * 3f, 60 + i);
        }
        Vector2 lakeShore = V(Oases[0].x + 36f, Oases[0].y + 12f);
        Add("reeds", lakeShore.x, lakeShore.y, 20f, 5);
        Add("reeds", lakeShore.x + 8f, lakeShore.y + 14f, 80f, 6);
    }

    static void PlanSahr()
    {
        Vector2 m = SahrC;
        clearings.Add(new Vector3(m.x, m.y, 50f));
        Add("well", m.x, m.y, 0f, 2, 0.9f);

        Vector2 chap = Pt(m, 15, 18);
        Add("dshrine", chap.x, chap.y, FaceYaw(chap, m), 251, 0.85f);
        Add("crystal", chap.x, chap.y, 0f, 5, 0f, 1f);

        float[] ang = { 70, 110, 160, 205, 250, 300, 345 };
        float[] rad = { 26, 24, 29, 25, 27, 25, 28 };
        for (int i = 0; i < ang.Length; i++)
        {
            Vector2 p = Pt(m, ang[i], rad[i]);
            float w = 5.4f + (i % 3) * 0.8f, d = 4.6f + (i % 2) * 0.7f;
            Add("dhouse", p.x, p.y, FaceYaw(p, m), 310 + i, Mathf.Min(1f, 0.5f + (i % 4) * 0.14f), w, d, i % 3 == 0 ? 2 : 1);
        }
        Vector2 cart = Pt(m, 40, 10);
        Add("cart", cart.x, cart.y, 30f, 9, 0.8f);

        // kuburan (barat laut)
        Vector2 gy = Pt(m, 285, 24);
        for (int i = 0; i < 12; i++)
        {
            float gx = gy.x + (i % 4 - 1.5f) * 2.4f + Mathf.Sin(i * 7.3f) * 0.4f;
            float gz = gy.y + (i / 4 - 1f) * 2.7f + Mathf.Cos(i * 5.1f) * 0.4f;
            Add("grave", gx, gz, 180f + Mathf.Sin(i * 3.1f) * 18f, 130 + i, 0f, i % 3 == 0 ? 1f : 0f);
        }
        Add("deadtree", gy.x + 6f, gy.y - 4f, 40f, 3);
        Add("deadtree", gy.x - 7f, gy.y + 3f, 120f, 4);

        // sarang bandit
        Vector2 fire = m + new Vector2(8f, -4f);
        Add("fire", fire.x, fire.y, 0f, 6);
        for (int k = 0; k < 4; k++)
        {
            Vector2 tp = Pt(fire, 30f + k * 90f, 7f);
            Add("tent", tp.x, tp.y, FaceYaw(tp, fire), 140 + k, 0f, k % 2);
        }
        Vector2 b1 = m + new Vector2(-3f, 9f), b2 = m + new Vector2(15f, 7f);
        Add("banner", b1.x, b1.y, 0f, 1);
        Add("banner", b2.x, b2.y, 0f, 2);
        for (int k = 0; k < 5; k++)
        {
            Vector2 cp = fire + new Vector2(-2.2f + k * 1.2f, 6.4f + (k % 2) * 0.8f);
            Add(k % 2 == 0 ? "crate" : "barrel", cp.x, cp.y, k * 30f, 50 + k);
        }
        Vector2 dirIn = (m - V(170f, -135f)).normalized;
        Vector2 entry = m - dirIn * 31f;
        float bYaw = Mathf.Atan2(dirIn.x, dirIn.y) * Mathf.Rad2Deg + 90f;
        Vector2 perp = new Vector2(dirIn.y, -dirIn.x);
        Add("barricade", entry.x + perp.x * 5.2f, entry.y + perp.y * 5.2f, bYaw, 1, 0f, 6f);
        Add("barricade", entry.x - perp.x * 5.2f, entry.y - perp.y * 5.2f, bYaw, 2, 0f, 6f);
        Add("banner", entry.x + perp.x * 8f, entry.y + perp.y * 8f, 0f, 3);
        Add("bones", m.x + 28f, m.y + 14f, 40f, 4);
        Add("bones", m.x - 34f, m.y - 6f, 200f, 5);
        Add("ruinboard", chap.x + 3f, chap.y - 7f, FaceYaw(chap, m), 1);
        for (int k = 0; k < 8; k++)
        {
            Vector2 bp = Pt(m, k * 45f + 10f, 42f + (k % 3) * 4f);
            Add(k % 3 == 0 ? "deadtree" : "boulder", bp.x, bp.y, k * 70f, 20 + k, 0f, 1.2f + (k % 3) * 0.5f);
        }
    }

    static void PlanTemples()
    {
        // --- Lembah Kuil: kuil agung, ziggurat, patung raja, jalan obelisk ---
        Add("temple", TempleC.x, TempleC.y, 180f, 7, 0.25f, 0f, 0f, 0, 36f);
        Add("ziggurat", 110f, 150f, 180f, 8, 0.35f, 0f, 0f, 0, 32f);
        Add("colossus", 20f, 40f, 180f, 3, 0.0f, 0f, 0f, 0, 10f);
        Add("colossus", 60f, 40f, 180f, 4, 0.8f, 0f, 0f, 0, 10f);
        Add("archway", 40f, -30f, 0f, 2, 0.0f, 0f, 0f, 0, 6f);
        foreach (float oz in new float[] { -5f, 22f })
        {
            Add("obelisk", 24f, oz, 0f, (int)oz + 3, oz > 0 ? 0.7f : 0f, 0f, 0f, 0, 3f);
            Add("obelisk", 56f, oz, 0f, (int)oz + 7, 0f, 0f, 0f, 0, 3f);
        }
        foreach (float bz in new float[] { 8f, 36f, 58f })
        {
            Add("brazier", 32f, bz, 0f, (int)bz, 0f, 1f);
            Add("brazier", 48f, bz, 0f, (int)bz + 1, 0f, 1f);
        }
        Add("crystal", 8f, 80f, 0f, 3, 0f, 1f);

        // --- Kuil Barat: reruntuhan parah dekat kolam ---
        Add("temple", WestTempleC.x, WestTempleC.y, FaceYaw(WestTempleC, V(-165f, -60f)) , 17, 0.8f, 0f, 0f, 0, 36f);
        Vector2 ws = V(-300f, 175f);
        Add("dshrine", ws.x, ws.y, FaceYaw(ws, WestTempleC), 252, 0.9f);
        Add("obelisk", -240f, 235f, 0f, 21, 0.9f, 0f, 0f, 0, 3f);
        Add("obelisk", -300f, 225f, 0f, 22, 0.9f, 0f, 0f, 0, 3f);
        Add("bones", -250f, 160f, 60f, 6);
        for (int i = 0; i < 4; i++)
        {
            Vector2 pp = Pt(V(Oases[2].x, Oases[2].y), i * 90f + 20f, 20f);
            Add("palm", pp.x, pp.y, i * 70f, 80 + i);
        }
        Add("reeds", Oases[2].x + 10f, Oases[2].y - 8f, 30f, 7);

        // --- Kuil Bayangan: kompleks terkutuk bercahaya kristal biru ---
        Add("temple", ShadowC.x, ShadowC.y, 180f, 27, 0.55f, 0f, 0f, 0, 36f);
        Add("colossus", ShadowC.x - 28f, ShadowC.y - 36f, 160f, 5, 0.85f, 0f, 0f, 0, 10f);
        Add("colossus", ShadowC.x + 28f, ShadowC.y - 36f, 200f, 6, 0.0f, 0f, 0f, 0, 10f);
        Add("obelisk", ShadowC.x - 14f, ShadowC.y - 52f, 0f, 31, 0.0f, 0f, 0f, 0, 3f);
        Add("obelisk", ShadowC.x + 14f, ShadowC.y - 52f, 0f, 32, 0.8f, 0f, 0f, 0, 3f);
        foreach (float sx in new float[] { -1f, 1f })
            Add("crystal", ShadowC.x + sx * 22f, ShadowC.y - 12f, 0f, 9 + (int)sx, 0f, 1f);
        Add("crystal", ShadowC.x, ShadowC.y + 24f, 0f, 14, 0f, 1f);
        Add("bones", ShadowC.x + 40f, ShadowC.y - 25f, 20f, 8);

        // reruntuhan tersebar di luar kompleks
        Add("archway", -20f, 200f, 20f, 9, 0.8f, 0f, 0f, 0, 6f);
        Add("domeshrine", 180f, -20f, 70f, 41, 0.8f, 0f, 0f, 0, 8f);
        Add("obelisk", 240f, 70f, 0f, 41, 0.9f, 0f, 0f, 0, 3f);
        Add("domeshrine", -120f, 140f, 20f, 42, 0.9f, 0f, 0f, 0, 8f);
    }

    // =====================================================================
    // Instansiasi
    // =====================================================================
    static GameObject PlaceD(Plc p, string key, System.Action<GeoBag> build, Transform parent, bool collider, float scale = 1f)
    {
        var proto = PropKit.Get(key, build);
        var go = PropKit.Place(proto, parent, p.kind, G(p.x, p.z), p.yaw + (M < 0f ? 180f : 0f), scale, ForestKit.DSlots);
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = proto.mesh;
        return go;
    }

    static Light AddLight(Transform parent, Vector3 localPos, Color c, float range, float intensity, string name = "Light")
    {
        var l = new GameObject(name).AddComponent<Light>();
        l.transform.SetParent(parent, false);
        l.transform.localPosition = localPos;
        l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = c;
        return l;
    }

    static void BuildDesertSites()
    {
        tZafir = Group("Desa_Oasis_Zafir");
        tSahr = Group("Desa_Sahr_Terbengkalai");
        tTemples = Group("Kuil_Kuil");
        if (mFxSmoke == null) EnsureFxMats();
        foreach (var p in plan)
        {
            Transform parent;
            if (Dist(p.x, p.z, ZafirC.x, ZafirC.y) < 70f) parent = tZafir;
            else if (Dist(p.x, p.z, SahrC.x, SahrC.y) < 70f) parent = tSahr;
            else parent = tTemples;
            int sd = p.seed; float ruin = p.ruin;
            string rk = "_" + (int)(ruin * 100f);
            switch (p.kind)
            {
                case "dhouse":
                {
                    float w = p.a, d = p.b; int fl = p.roof;
                    PlaceD(p, "DHouse_" + sd + "_" + (int)(w * 10f) + "_" + (int)(d * 10f) + rk + "_" + fl, b => ForestKit.DHouse(b, sd, w, d, ruin, fl, true), parent, true);
                    break;
                }
                case "dinn": PlaceD(p, "DInn_" + sd, b => ForestKit.DInn(b, sd), parent, true); break;
                case "dstall": PlaceD(p, "DStall_" + (sd % 2), b => ForestKit.DStall(b, sd % 2), parent, true); break;
                case "dshrine": PlaceD(p, "DShrine_" + sd + rk, b => ForestKit.DomeShrine(b, sd, ruin), parent, true); break;
                case "domeshrine": PlaceD(p, "DShrineX_" + sd + rk, b => ForestKit.DomeShrine(b, sd, ruin), parent, true); break;
                case "well": PlaceD(p, "DWell" + rk, b => ForestKit.Well(b, sd, ruin), parent, true); break;
                case "cart": PlaceD(p, "DCart" + rk, b => ForestKit.Cart(b, sd, ruin), parent, true); break;
                case "crate": PlaceD(p, "DCrateP_" + (sd % 3), b => ForestKit.Crate(b, sd % 3), parent, true); break;
                case "barrel": PlaceD(p, "DBarrelP", b => ForestKit.Barrel(b), parent, true); break;
                case "jars": PlaceD(p, "DJarsP_" + (sd % 4), b => ForestKit.Jars(b, sd % 4), parent, false, 1.1f); break;
                case "crops": { float w = p.a, d = p.b; int kind = p.roof; PlaceD(p, "DCrops_" + kind, b => ForestKit.CropRows(b, sd, w, d, kind), parent, false); break; }
                case "palm": { var go = PlaceD(p, "DPalmP_" + (sd % 6), b => ForestKit.Palm(b, sd % 6 + 20, 1f + (sd % 3) * 0.1f, 1f), parent, false); go.AddComponent<CapsuleCollider>().height = 4f; break; }
                case "reeds": PlaceD(p, "DReeds_" + (sd % 3), b => ForestKit.Reeds(b, sd % 3), parent, false, 1.6f); break;
                case "brazier":
                {
                    var br = PlaceD(p, "DBrazier", b => ForestKit.Brazier(b, 1), parent, true);
                    if (p.a > 0.5f) AddLight(br.transform, new Vector3(0, 1.8f, 0), new Color(1f, 0.62f, 0.3f), 15f, 1.2f, "BrazierLight");
                    break;
                }
                case "board":
                {
                    var bd = PlaceD(p, "DBoard", b => ForestKit.NoticeBoard(b), parent, false);
                    var ip = bd.AddComponent<InfoPoint>();
                    ip.prompt = "Baca papan pengumuman"; ip.radius = 3f; ip.title = "Papan Oasis Zafir";
                    ip.message = "PERINGATAN UNTUK PARA PENJELAJAH\n\n- Bandit menduduki Desa Sahr (timur laut jalan kafilah) dan sering menyergap di jalan. Jumlah mereka banyak.\n- Penjaga kuil kuno (mumi) bangkit di Lembah Kuil. Jangan masuk malam hari.\n- Skorpion raksasa terlihat di timur Lembah Kuil. Golem batu di Ngarai Merah.\n- Air Oasis gratis. Penginapan dan lapak buka kapan saja.\n\n- Tetua Salim";
                    break;
                }
                case "ruinboard":
                {
                    var bd = PlaceD(p, "DBoardRuin", b => ForestKit.NoticeBoard(b), parent, false);
                    var ip = bd.AddComponent<InfoPoint>();
                    ip.prompt = "Periksa papan lapuk"; ip.radius = 3f; ip.title = "Papan Sahr";
                    ip.message = "Papan kayu ini nyaris habis dimakan pasir. Satu goresan masih terbaca:\n\n\"... sumur mengering. Kami pergi ke Zafir. Siapa pun yang membaca, jangan menunggu badai berikutnya ...\"\n\nBendera merah baru dipaku di bawahnya. Bandit tinggal di sini sekarang.";
                    break;
                }
                case "tent": { int cs = p.a > 0.5f ? Sl.ClothB : Sl.Cloth; PlaceD(p, "DTentB_" + cs, b => ForestKit.Tent(b, sd, cs), parent, true, 1.25f); break; }
                case "fire":
                {
                    var f = PlaceD(p, "DBanditFire", b => ForestKit.Campfire(b, sd), parent, false, 1.3f);
                    AddLight(f.transform, new Vector3(0, 1.4f, 0), new Color(1f, 0.55f, 0.22f), 20f, 1.7f, "FireLight");
                    var sm = new GameObject("FireSmoke").AddComponent<ChimneySmoke>();
                    sm.material = mFxSmoke;
                    sm.transform.SetParent(f.transform, false);
                    sm.transform.localPosition = new Vector3(0, 1.4f, 0);
                    break;
                }
                case "barricade": { float len = p.a; PlaceD(p, "DBarricade_" + sd + "_" + (int)len, b => ForestKit.Barricade(b, sd, len), parent, true); break; }
                case "banner": PlaceD(p, "DBanner_" + sd, b => ForestKit.BannerPole(b, sd), parent, false); break;
                case "grave": { bool cross = p.a > 0.5f; PlaceD(p, "DGrave_" + sd + (cross ? "x" : "r"), b => ForestKit.Gravestone(b, sd, cross), parent, false); break; }
                case "deadtree": PlaceD(p, "DDeadV_" + (sd % 3), b => ForestKit.DeadTree(b, sd, 1.1f), parent, true); break;
                case "boulder": PlaceD(p, "DBoulderV_" + (sd % 4), b => ForestKit.Boulder(b, sd % 4, 1f, true), parent, p.a > 1.3f, p.a); break;
                case "bones": PlaceD(p, "DBonesV_" + (sd % 3), b => ForestKit.BeastSkeleton(b, sd % 3 + 1), parent, false, 1.3f); break;
                case "crystal":
                {
                    var cr = PlaceD(p, "DCrystalV_" + sd, b => ForestKit.CrystalCluster(b, sd), parent, false, p.a > 0.5f ? 1.8f : 1.2f);
                    if (p.a > 0.5f) AddLight(cr.transform, new Vector3(0, 2.0f, 0), new Color(0.35f, 0.95f, 0.9f), 20f, 1.5f, "CrystalLight");
                    break;
                }
                case "obelisk": { float rr = ruin; PlaceD(p, "DObelisk_" + sd + rk, b => ForestKit.Obelisk(b, sd, rr), parent, true); break; }
                case "archway": { float rr = ruin; PlaceD(p, "DArchway_" + sd + rk, b => ForestKit.Archway(b, sd, rr), parent, true); break; }
                case "colossus":
                {
                    float rr = ruin;
                    var co = PlaceD(p, "DColossus_" + sd + rk, b => ForestKit.Colossus(b, sd, rr), parent, true);
                    if (rr < 0.5f) AddLight(co.transform, new Vector3(0, 11.2f, 2.2f), new Color(0.3f, 0.95f, 0.9f), 9f, 1.0f, "EyeGlow");
                    break;
                }
                case "ziggurat": { float rr = ruin; PlaceD(p, "DZig_" + sd + rk, b => ForestKit.Ziggurat(b, sd, rr), parent, true); break; }
                case "temple":
                {
                    float rr = ruin;
                    var tp = PlaceD(p, "DTemple_" + sd + rk, b => ForestKit.Temple(b, sd, rr), parent, true);
                    AddLight(tp.transform, new Vector3(0, 4.8f, -16.8f), new Color(0.35f, 0.95f, 0.9f), 24f, 1.8f, "SanctumGlow");
                    AddLight(tp.transform, new Vector3(0, 4f, 12f), new Color(1f, 0.7f, 0.4f), 18f, 0.9f, "CourtLight");
                    break;
                }
                case "smoke":
                {
                    var sm = new GameObject("OvenSmoke").AddComponent<ChimneySmoke>();
                    sm.material = mFxSmoke;
                    sm.transform.SetParent(parent, false);
                    sm.transform.position = G(p.x, p.z) + Vector3.up * p.a;
                    break;
                }
            }
        }
        BuildZafirNpcs();
        BuildZafirInteractions();
        BuildSahrAndTempleExtras();
    }

    // =====================================================================
    // NPC & interaksi desa Zafir
    // =====================================================================
    static void BuildZafirNpcs()
    {
        tVillagers = Group("Villagers");
        vRng = new System.Random(717);
        Vector2 c = ZafirC;
        Vector2 innP = Pt(c, 205, 24);
        float innYaw = FaceYaw(innP, c);
        Vector2 innDoor = Local(innP, innYaw, ForestKit.DoorX(240, 11f) + 1.4f, 8f / 2f + 1.7f);

        VNpc("Ibu Zahra", "Pemilik Penginapan", NpcKit.OPelayan, NpcKit.Human, true, innDoor, innYaw + 180f,
            "Selamat datang di Zafir! Kamar sejuk, teh mint, dan roti panas.|Jangan berkemah di Sahr. Anak-anak buahku melihat api unggun bandit di sana tiap malam.", null, false);
        Vector2 sp = Pt(c, 60, 12.5f);
        VNpc("Pak Karim", "Pedagang Kafilah", NpcKit.OPedagang, NpcKit.Human, false, Pt(c, 60, 10.7f), FaceYaw(Pt(c, 60, 10.7f), c) + 180f,
            "Rempah, kain, ramuan! Semua dari seberang gurun.|Kafilah kami dua kali dirampok di jalan ke Sahr. Aku mulai berpikir untuk berhenti berdagang.", null, false);
        Vector2 shr = Pt(c, 330, 44);
        VNpc("Tetua Salim", "Tetua Oasis", NpcKit.OPendeta, NpcKit.Elf, false, Local(shr, FaceYaw(shr, c), 0f, 6f), FaceYaw(shr, c) + 180f,
            "Dulu Sahr adalah desa adik kami. Sumurnya mengering, lalu bandit datang.|Kuil-kuil kuno di utara dijaga mumi. Jangan ambil apa pun dari altar mereka tanpa izin.|Cahaya biru di Kuil Bayangan... sudah lama aku curiga itu bukan cahaya biasa.", null, false);
        Vector2 gate = c + (V(0f, -300f) - c).normalized * 42f;
        VNpc("Rashid", "Penjaga Oasis", NpcKit.OPenjaga, NpcKit.Dwarf, false, gate, FaceYaw(gate, c) + 180f,
            "Berhenti. ...Oh, Hunter. Silakan.|Aku tidak berani meninggalkan pos. Kalau bandit turun ke Zafir, aku satu-satunya yang berdiri di depan sumur kami.", null, true);
        Vector2 sh2 = Pt(c, 200, 16);
        VNpc("Tabib Nuri", "Tabib", NpcKit.OPendeta, NpcKit.Human, true, Pt(c, 120, 10f), 0f,
            "Buah kaktus bagus untuk demam. Kalau kamu menemukan banyak, bawakan ke Zafir.|Skorpion raksasa sengatnya bisa mengeringkan darah. Jangan dekati tanpa penawar.", null, false);

        var plaza = VRoute("ZafirPlaza", Pt(c, 0, 18), Pt(c, 60, 18), Pt(c, 120, 18), Pt(c, 180, 18), Pt(c, 240, 18), Pt(c, 300, 18));
        VNpc("Dalia", "Petani", NpcKit.OPetani, NpcKit.Human, true, Pt(c, 0, 18), 0f, "Airnya manis. Itu rahasia oasis ini.|Hati-hati kalau pergi jauh. Badai pasir datang tanpa aba-aba.", plaza, false);
        var plaza2 = VRoute("ZafirPlaza2", Pt(c, 150, 15), Pt(c, 210, 15), Pt(c, 270, 15), Pt(c, 330, 15));
        VNpc("Amir", "Anak Desa", NpcKit.OWarga, NpcKit.Beast, false, Pt(c, 150, 15), 0f, "Aku pernah lihat patung raksasa duduk di Lembah Kuil! Matanya menyala biru!|Kata Ibu, jangan lewat jalan ke Sahr.", plaza2, false);
        var field = VRoute("ZafirField", Pt(c, 266, 36), Pt(c, 280, 40), Pt(c, 296, 44));
        VNpc("Nenek Layla", "Petani", NpcKit.OWarga, NpcKit.Human, true, Pt(c, 266, 36), 0f, "Kurma tahun ini manis. Kalau bandit tidak mencurinya.|Dulu di Sahr ada pasar besar. Sekarang hanya pasir dan hantu.", field, false);
        var shore = VRoute("ZafirShore", V(Oases[0].x + 34f, Oases[0].y + 20f), V(Oases[0].x + 36f, Oases[0].y + 2f), V(Oases[0].x + 30f, Oases[0].y - 14f));
        VNpc("Umar", "Penggembala", NpcKit.OPetani, NpcKit.Human, false, V(Oases[0].x + 34f, Oases[0].y + 20f), 0f, "Kambingku suka minum di sini. Aku suka mendengar anginnya.", shore, false);
    }

    static void BuildZafirInteractions()
    {
        Vector2 c = ZafirC;
        Vector2 innP = Pt(c, 205, 24);
        float innYaw = FaceYaw(innP, c);
        Vector2 door = Local(innP, innYaw, ForestKit.DoorX(240, 11f), 8f / 2f + 2.2f);
        var io = new GameObject("InnDoor");
        io.transform.SetParent(tZafir, false);
        io.transform.position = G(door.x, door.y);
        var inn = io.AddComponent<Inn>();
        inn.prompt = "Penginapan Oasis"; inn.radius = 3.6f;

        Vector2 sp = Pt(c, 60, 10.7f);
        var so = new GameObject("StallShop");
        so.transform.SetParent(tZafir, false);
        so.transform.position = G(sp.x, sp.y);
        var shop = so.AddComponent<GeneralShop>();
        shop.prompt = "Lapak Pak Karim"; shop.radius = 3.4f;

        var wo = new GameObject("VillageWell");
        wo.transform.SetParent(tZafir, false);
        wo.transform.position = G(c.x, c.y + 1.6f);
        var ws = wo.AddComponent<WaterSource>();
        ws.prompt = "Sumur Desa"; ws.radius = 3.4f;

        Vector2 chap = Pt(SahrC, 15, 18);
        var co = new GameObject("RuinAltar");
        co.transform.SetParent(tSahr, false);
        co.transform.position = G(chap.x, chap.y);
        var ip = co.AddComponent<InfoPoint>();
        ip.prompt = "Periksa kapel retak"; ip.radius = 5f; ip.title = "Kapel Sahr";
        ip.message = "Kubah kapel runtuh dan separuh terkubur pasir. Kristal biru berdenyut lemah di altar, seolah masih mengingat doa-doa lama.\n\nDi lantai tampak bekas api unggun dan botol kosong: para bandit bermalam di sini.";

        Vector2 tp = TempleC;
        var ta = new GameObject("TempleAltar");
        ta.transform.SetParent(tTemples, false);
        ta.transform.position = G(tp.x, tp.y - 18f);
        var tip = ta.AddComponent<InfoPoint>();
        tip.prompt = "Periksa prasasti"; tip.radius = 6f; tip.title = "Prasasti Kuil Agung";
        tip.message = "Huruf-huruf kuno bercahaya samar:\n\n\"Siapa yang membangunkan penjaga, ia mewarisi kutuk raja. Batu akan berdiri, perban akan berjalan.\"\n\nDi kejauhan terdengar langkah berat. Para penjaga tidak suka diganggu.";
    }

    static void BuildSahrAndTempleExtras()
    {
        // tumbleweed yang tersangkut di desa terbengkalai, dan jejak roda kafilah
        for (int i = 0; i < 9; i++)
        {
            Vector2 p = Pt(SahrC, i * 40f + 5f, 12f + (i % 3) * 14f);
            var proto = PropKit.Get("DTumbleP", b => ForestKit.Tumbleweed(b, 5));
            PropKit.Place(proto, tSahr, "Tumbleweed", G(p.x, p.y), i * 50f, 1f, ForestKit.DSlots);
        }
    }

    // =====================================================================
    // Bandit
    // =====================================================================
    static void BuildDesertBandits()
    {
        var g = Group("BanditEvents");
        MakeAmbush(g, "Ambush_JalanKafilah", V(165f, -135f), 20f, 0.7f, 6, 3, false, 130, "Penyergapan di jalan kafilah!");
        MakeAmbush(g, "Ambush_SarangSahr", SahrC + V(-4f, -2f), 30f, 1f, 6, 3, true, 320, "Sarang bandit Sahr!");
        // bandit tetap di sarang (juga memenuhi quest Basmi Bandit Gurun)
        var root = Group("BanditCamp");
        SpawnRing(root, "bandit", SahrC + V(8f, -4f), 12f, 6, 15f);
        SpawnRing(root, "bandit_archer", SahrC + V(4f, 2f), 18f, 2, 60f);
    }

    // =====================================================================
    // Efek dunia
    // =====================================================================
    static void BuildDesertFx()
    {
        if (mFxSmoke == null) EnsureFxMats();
        if (mFxSand == null) mFxSand = FxMat("FxSand", FxUtil.SoftDot(1.4f));
        var fx = Group("WorldFx");
        var sand = new GameObject("BlowingSand").AddComponent<BlowingSand>();
        sand.transform.SetParent(fx, false);
        sand.material = mFxSand;
    }
}
