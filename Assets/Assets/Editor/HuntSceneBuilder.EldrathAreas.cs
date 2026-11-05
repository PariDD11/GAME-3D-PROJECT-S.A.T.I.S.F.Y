using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>Isi khusus tiga area Eldrath: Hutan Perbatasan, Padang Tulang, Lembah Iblis (pola sama dengan VolcanoExtras):
/// landmark tambahan, bahaya (HazardZone), papan arah, lentera jalur, bahan gathering khusus, arena boss, dan cakrawala.</summary>
public static partial class HuntSceneBuilder
{
    static void EAreaExtras()
    {
        if (ea == 0) FrontierExtras();
        else if (ea == 2) BoneExtras();
        else if (ea == 3) DemonExtras();
        if (ea != 1) EHorizon();
        ELore();
    }

    // ------------------------------------------------------------------ pembantu umum
    static Vector2 NearestTrailPt(Vector2[] trail, float z)
    {
        Vector2 best = trail[0]; float bd = 99999f;
        foreach (var p in VWalk(trail, 4f)) { float d = Mathf.Abs(p.y - z); if (d < bd) { bd = d; best = new Vector2(p.x, p.y); } }
        return best;
    }

    static void ELamps(Vector2[] trail, float step, float zMin, float zMax, Color col, System.Action<GeoBag> model, string key)
    {
        var tv = Group("LenteraJalur");
        int n = 0;
        foreach (var pt in VWalk(trail, step))
        {
            n++;
            if (pt.y < zMin || pt.y > zMax) continue;
            float side = n % 2 == 0 ? 1f : -1f;
            Vector2 perp = new Vector2(-Mathf.Sin(pt.z), Mathf.Cos(pt.z)) * 5.8f * side;
            float x = pt.x + perp.x, z = pt.y + perp.y;
            if (GroundY(x, z) < WaterY + 1f || Slope(x, z) > 30f) continue;
            Vector3 wp = G(x, z);
            EKit(key, model, tv, wp, 0f, 1f, true);
            if (n % 3 == 0) ELight(tv, wp + Vector3.up * 3.4f, col, 18f, 1.5f);
        }
    }

    static void EGather(string itemId, Vector3 pos, string label, Color c1, Color c2, int shape, int min, int max, float respawn)
    {
        var n = new GameObject("Node_" + itemId);
        n.transform.SetParent(tNodes, false);
        n.transform.position = pos;
        var vis = new GameObject("Visual");
        vis.transform.SetParent(n.transform, false);
        var m1 = TownMats.Emissive(Mat("EG_" + itemId + "1", c1), c1 * 0.25f);
        var m2 = Mat("EG_" + itemId + "2", c2);
        Prim(PrimitiveType.Sphere, "Dasar", vis.transform, new Vector3(0, 0.12f, 0), new Vector3(0.95f, 0.28f, 0.95f), m2, false);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad, r = i == 0 ? 0f : 0.32f;
            Vector3 p = new Vector3(Mathf.Cos(a) * r, 0.32f + (i == 0 ? 0.2f : 0f), Mathf.Sin(a) * r);
            GameObject o;
            if (shape == 0) o = Prim(PrimitiveType.Capsule, "Bagian", vis.transform, p, new Vector3(0.16f, 0.28f + 0.04f * (i % 3), 0.16f), m1, false);      // tulang/duri
            else if (shape == 1) o = Prim(PrimitiveType.Cylinder, "Bagian", vis.transform, p, new Vector3(0.13f, 0.3f + 0.05f * (i % 3), 0.13f), m1, false);   // kristal
            else o = Prim(PrimitiveType.Sphere, "Bagian", vis.transform, p, new Vector3(0.3f, 0.3f, 0.3f), m1, false);                                       // bongkah
            o.transform.localRotation = Quaternion.Euler(Random.Range(-18f, 18f), 0f, Random.Range(-18f, 18f));
        }
        var gn = n.AddComponent<GatherNode>();
        gn.itemId = itemId; gn.visual = vis; gn.prompt = label; gn.radius = 2.8f;
        gn.minAmount = min; gn.maxAmount = max; gn.respawnSeconds = respawn;
    }

    static void EScatterNodes(string itemId, int count, int[] zones, string label, Color c1, Color c2, int shape, int min, int max, float respawn)
    {
        for (int i = 0, k = 0; i < 600 && k < count; i++)
        {
            var zd = Zones[zones[Random.Range(0, zones.Length)]];
            Vector3? p = FindLand(zd.x, zd.z, zd.r, 25f);
            if (!p.HasValue) continue;
            EGather(itemId, p.Value, label, c1, c2, shape, min, max, respawn); k++;
        }
    }

    static void EHazard(Vector2 c, float r, float dps, string warn, Color col, bool fog)
    {
        if (GroundY(c.x, c.y) < WaterY + 0.5f) return;
        var h = new GameObject("Hazard_" + warn.Substring(0, Mathf.Min(8, warn.Length))).transform;
        h.SetParent(tProps, false);
        h.position = G(c.x, c.y);
        var hz = h.gameObject.AddComponent<HazardZone>();
        hz.radius = r * 0.92f; hz.dps = dps; hz.warn = warn;
        var m = TownMats.Emissive(Mat("EHaz" + (int)(col.r * 9) + (int)(col.g * 9) + (int)(col.b * 9), new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f)), col * 0.5f);
        var d = Prim(PrimitiveType.Cylinder, "Tanda", h, new Vector3(0, 0.12f, 0), new Vector3(r * 2f, 0.03f, r * 2f), m, false);
        if (fog)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 o = Random.insideUnitCircle * r * 0.6f;
                Prim(PrimitiveType.Sphere, "Kabut", h, new Vector3(o.x, 1.1f + i * 0.15f, o.y), new Vector3(r * 0.9f, 1.4f, r * 0.9f), m, false);
            }
        }
        ELight(h, h.position + Vector3.up * 1.2f, col, r * 2.2f, 1.2f, false);
        clearings.Add(new Vector3(c.x, c.y, r + 2f));
    }

    static void EBossRing(Vector2 c, float r, Color glow, string signText, Vector2 signAt)
    {
        var g = Group("ArenaBoss");
        var m = TownMats.Emissive(Mat("EArena" + ea, glow * 0.6f), glow);
        var ring = Prim(PrimitiveType.Cylinder, "CincinRune", g, G(c.x, c.y) + Vector3.up * 0.2f, new Vector3(r * 2f, 0.03f, r * 2f), m, false);
        var inner = Prim(PrimitiveType.Cylinder, "CincinDalam", g, G(c.x, c.y) + Vector3.up * 0.24f, new Vector3(r * 1.84f, 0.03f, r * 1.84f), Mat("EArenaFloor" + ea, new Color(0.08f, 0.07f, 0.08f)), false);
        for (int i = 0; i < 8; i++)
        {
            Vector2 p = Pt(c, i * 45f + 22f, r);
            EKit("EArenaBrazier", VBrazier, g, G(p.x, p.y), 0f, 1.2f, true);
            ELight(g, G(p.x, p.y) + Vector3.up * 4f, glow, 24f, 2f);
        }
        VSign(signAt, signText, Mathf.Max(5.5f, signText.Length * 0.34f));
    }

    // ------------------------------------------------------------------ cakrawala: cincin gunung/bukit jauh yang menyatu dengan kabut
    static void EHorizon()
    {
        Color far, mid;
        if (ea == 0) { far = new Color(0.42f, 0.52f, 0.5f); mid = new Color(0.3f, 0.4f, 0.34f); }
        else if (ea == 2) { far = new Color(0.5f, 0.54f, 0.62f); mid = new Color(0.38f, 0.4f, 0.46f); }
        else { far = new Color(0.2f, 0.07f, 0.12f); mid = new Color(0.14f, 0.05f, 0.09f); }
        var g = Group("Cakrawala");
        var mf = Mat("EHorizonFar" + ea, far);
        var mm = Mat("EHorizonMid" + ea, mid);
        var rnd = new System.Random(900 + ea);
        for (int layer = 0; layer < 2; layer++)
        {
            float R = layer == 0 ? 980f : 790f;
            int n = layer == 0 ? 46 : 38;
            var b = new GeoBag();
            for (int i = 0; i < n; i++)
            {
                float a = (i + (float)rnd.NextDouble() * 0.6f) / n * Mathf.PI * 2f;
                float h = (layer == 0 ? 230f : 140f) * (0.5f + (float)rnd.NextDouble());
                float w = (layer == 0 ? 190f : 130f) * (0.8f + (float)rnd.NextDouble() * 0.7f);
                if (ea == 3) h *= 1.15f;
                b.Cone(Sl.Stone, new Vector3(Mathf.Cos(a) * R, -40f, Mathf.Sin(a) * R), w, h + 40f, ea == 2 ? 6 : 7);
            }
            var slots = new Material[Sl.Count];
            for (int i = 0; i < slots.Length; i++) slots[i] = layer == 0 ? mf : mm;
            var go = b.Emit(g, layer == 0 ? "GunungJauh" : "BukitJauh", slots);
            if (go != null) { foreach (var col in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col); }
        }
        var cam = Camera.main;
        if (cam != null && cam.farClipPlane < 2200f) cam.farClipPlane = 2200f;
    }

    // ================================================================== HUTAN PERBATASAN
    static void FrontierExtras()
    {
        var tv = Group("PerbatasanDekor");
        var main = ETrails(0)[0];
        Color torch = new Color(1f, 0.68f, 0.35f);

        // ---- pos pedagang di jalan raya (karavan, tenda, api unggun)
        {
            Vector2 c = V(60f, -110f);
            clearings.Add(new Vector3(c.x, c.y, 34f));
            ECampSite(c, 4, 12f, "Pedagang");
            for (int i = 0; i < 2; i++)
            {
                Vector2 p = Pt(c, 70f + i * 130f, 22f);
                EKit("EWagon", EWagon, tv, G(p.x, p.y), 30f + i * 80f, 1.1f, true);
            }
            VSign(V(14f, -108f), "POS PEDAGANG >> ISTIRAHAT AMAN", 6.6f);
        }
        // ---- karavan hancur (korban bandit)
        {
            Vector2 c = V(-90f, 110f);
            clearings.Add(new Vector3(c.x, c.y, 24f));
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = Pt(c, i * 120f + 15f, 12f);
                EKit("EWreck", EWreck, tv, G(p.x, p.y), i * 70f, 1.1f, true);
            }
            for (int i = 0; i < 6; i++) { Vector2 p = Pt(c, i * 60f, 7f + (i % 2) * 5f); EKit("ECrate", b => b.Box(Sl.Wood, new Vector3(0, 0.5f, 0), new Vector3(1.1f, 1f, 1.1f), i * 17f), tv, G(p.x, p.y), i * 40f, 1f, true); }
        }
        // ---- kuil hutan (totem serigala di Hutan Serigala)
        {
            Vector2 c = V(-230f, -120f);
            clearings.Add(new Vector3(c.x, c.y, 26f));
            EKit("ETotem", ETotem, tv, G(c.x, c.y), 0f, 1.3f, true);
            for (int i = 0; i < 6; i++) { Vector2 p = Pt(c, i * 60f, 11f); EKit("EStandStone", b => ForestKit.Obelisk(b, 3, 0.3f, 5f), tv, G(p.x, p.y), i * 60f, 1f, true); }
            ELight(tv, G(c.x, c.y) + Vector3.up * 4f, new Color(0.5f, 1f, 0.7f), 26f, 1.8f, false);
        }
        // ---- gerbang tua di jalan utara
        foreach (float zt in new[] { 120f, 330f })
        {
            Vector2 p = NearestTrailPt(main, zt);
            clearings.Add(new Vector3(p.x, p.y, 14f));
            EKit("EFArch" + (int)zt, b => ForestKit.Archway(b, 4, 0.2f), tv, G(p.x, p.y), 0f, 1.2f, true);
        }
        // ---- bahaya: jebakan bandit di kamp bandit & dekat jalan
        EHazard(V(-205f, 128f), 3.2f, 14f, "Jebakan bandit! Hati-hati melangkah.", new Color(0.85f, 0.35f, 0.12f), false);
        EHazard(V(-235f, 150f), 3.2f, 14f, "Jebakan bandit! Hati-hati melangkah.", new Color(0.85f, 0.35f, 0.12f), false);
        EHazard(V(-190f, 160f), 3.2f, 14f, "Jebakan bandit! Hati-hati melangkah.", new Color(0.85f, 0.35f, 0.12f), false);
        EHazard(V(170f, -110f), 8f, 6f, "Rawa berduri menusuk kakimu.", new Color(0.3f, 0.6f, 0.2f), true);
        // ---- lentera + papan arah
        ELamps(main, 30f, -300f, 400f, torch, ELampWood, "EFLamp");
        VSign(V(14f, -300f), "BASE CAMP << SELATAN | ELDRATH UTARA >>", 9f);
        VSign(V(-14f, -176f), "<< HUTAN SERIGALA (Lv 10-14)", 7.4f);
        VSign(V(14f, -76f), "SARANG OGRE >> (BAHAYA)", 6.2f);
        VSign(V(-14f, 80f), "<< KAMP BANDIT", 4.6f);
        VSign(V(14f, 210f), "MENARA PENGAWAS >>", 5.4f);
        VSign(V(8f, 340f), "ASHWATCH | LURUS UTARA", 8f);
        // ---- arena Grumak (sarang ogre)
        EBossRing(V(250f, -90f), 34f, new Color(1f, 0.55f, 0.2f), "SARANG BEHEMOTH: PENGHANCUR HUTAN", V(190f, -90f));
        // ---- bahan gathering khusus
        EScatterNodes("road_pelt", 8, new[] { 1, 2, 3 }, "Ambil Kulit Serigala", new Color(0.5f, 0.45f, 0.4f), new Color(0.25f, 0.2f, 0.16f), 2, 1, 2, 420f);
        EScatterNodes("herb", 22, new[] { 1, 2, 4, 6 }, "Petik Herb", new Color(0.3f, 0.75f, 0.3f), new Color(0.15f, 0.3f, 0.12f), 2, 1, 3, 240f);
        EScatterNodes("mushroom", 14, new[] { 2, 3, 4 }, "Petik Jamur", new Color(0.85f, 0.7f, 0.5f), new Color(0.2f, 0.14f, 0.1f), 2, 1, 2, 240f);
    }

    // ================================================================== PADANG TULANG
    static void BoneExtras()
    {
        var tv = Group("TulangDekor");
        var main = ETrails(2)[0];
        Color ghost = new Color(0.45f, 0.78f, 1f);
        // ---- pemakaman perang: barisan salib batu
        {
            Vector2 c = V(-210f, -40f);
            clearings.Add(new Vector3(c.x, c.y, 50f));
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 8; col++)
                {
                    Vector2 p = c + V((col - 3.5f) * 9f, (row - 2f) * 9f);
                    if (GroundY(p.x, p.y) < WaterY + 1f) continue;
                    int v = (row * 8 + col) % 3;
                    EKit("EGrave" + v, b => EGrave(b, v), tv, G(p.x, p.y), Random.Range(-14f, 14f), 1f, true);
                }
            EKit("EMausoleum", EMausoleum, tv, G(c.x - 34f, c.y), 90f, 1f, true);
            ELight(tv, G(c.x - 30f, c.y) + Vector3.up * 3f, ghost, 26f, 1.6f, false);
            EHazard(c, 24f, 5f, "Kabut makam menyesakkan dadamu.", new Color(0.3f, 0.8f, 0.5f), true);
        }
        // ---- parit tengkorak
        {
            Vector2 c = V(210f, 0f);
            clearings.Add(new Vector3(c.x, c.y, 34f));
            for (int i = 0; i < 14; i++) { Vector2 p = Pt(c, i * 25.7f, 18f + (i % 3) * 3f); EKit("ESkullPile" + i % 3, b => ESkullPile(b, i % 3), tv, G(p.x, p.y), i * 31f, 1.2f, true); }
            for (int i = 0; i < 5; i++) { Vector2 p = Pt(c, i * 72f, 9f); EKit("EImpaled" + i, EStake, tv, G(p.x, p.y), i * 60f, 1f, true); }
            ELight(tv, G(c.x, c.y) + Vector3.up * 2f, ghost, 30f, 1.4f, false);
        }
        // ---- mesin pengepung rusak
        foreach (var sc in new[] { V(-60f, -90f), V(120f, 120f) })
        {
            clearings.Add(new Vector3(sc.x, sc.y, 14f));
            EKit("ESiege", ESiege, tv, G(sc.x, sc.y), Random.Range(0f, 360f), 1.2f, true);
        }
        // ---- panji perang menancap di padang
        for (int i = 0; i < 18; i++)
        {
            Vector2 p = V(Random.Range(-160f, 160f), Random.Range(-250f, 150f));
            if (NearClearing(p.x, p.y) || DistToTrails(p.x, p.y) < 8f || GroundY(p.x, p.y) < WaterY + 1f) continue;
            EKit("EBanner", EBannerPole, tv, G(p.x, p.y), Random.Range(0f, 360f), 1f, true);
        }
        // ---- lentera hantu + papan arah
        ELamps(main, 30f, -300f, 290f, ghost, EGhostLamp, "EBLamp");
        VSign(V(14f, -300f), "BASE CAMP << SELATAN", 6.2f);
        VSign(V(-14f, -176f), "<< PEMAKAMAN PERANG", 6.6f);
        VSign(V(14f, -60f), "PARIT TENGKORAK >>", 6f);
        VSign(V(-14f, 110f), "<< BENTENG RUNTUH", 5.6f);
        VSign(V(14f, 200f), "PADANG HANTU >>", 5.2f);
        VSign(V(8f, 250f), "AWAS: GERBANG CERBERUS", 7.2f);
        // ---- arena Malrek
        EBossRing(V(0f, 300f), 36f, new Color(0.4f, 0.75f, 1f), "MAKAM RAJA MALREK: RAJA TULANG", V(10f, 245f));
        // ---- bahan gathering khusus
        EScatterNodes("ancient_bone", 14, new[] { 1, 2, 3, 5 }, "Gali Tulang Kuno", new Color(0.88f, 0.86f, 0.76f), new Color(0.3f, 0.28f, 0.24f), 0, 1, 2, 420f);
        EScatterNodes("grave_dust", 14, new[] { 2, 4, 5 }, "Kumpulkan Debu Makam", new Color(0.62f, 0.66f, 0.6f), new Color(0.2f, 0.2f, 0.2f), 2, 1, 3, 300f);
        EScatterNodes("ectoplasm", 6, new[] { 5, 2 }, "Tangkap Ektoplasma", new Color(0.55f, 0.85f, 1f), new Color(0.12f, 0.2f, 0.28f), 1, 1, 1, 600f);
    }

    // ================================================================== LEMBAH IBLIS
    static void DemonExtras()
    {
        var tv = Group("IblisDekor");
        var main = ETrails(3)[0];
        Color purple = new Color(0.75f, 0.2f, 0.95f);
        Color fire = new Color(1f, 0.3f, 0.08f);
        // ---- altar ritual dengan pentagram dan sangkar
        {
            Vector2 c = V(-170f, 80f);
            clearings.Add(new Vector3(c.x, c.y, 40f));
            EKit("EPentagram", EPentagram, tv, G(c.x, c.y) + Vector3.up * 0.25f, 0f, 1f, false);
            for (int i = 0; i < 5; i++) { Vector2 p = Pt(c, i * 72f, 26f); EKit("ECage", ECage, tv, G(p.x, p.y), i * 72f, 1f, true); }
            EKit("EDemonStatue", EDemonStatue, tv, G(c.x - 14f, c.y), 90f, 1.4f, true);
            ELight(tv, G(c.x, c.y) + Vector3.up * 3f, purple, 34f, 2.4f, false);
        }
        // ---- jurang iblis: gerbang bertanduk
        {
            Vector2 c = V(190f, 190f);
            clearings.Add(new Vector3(c.x, c.y, 34f));
            EKit("EHornGate", EHornGate, tv, G(c.x - 30f, c.y), 90f, 1.4f, true);
            for (int i = 0; i < 5; i++) { Vector2 p = Pt(c, i * 72f + 20f, 22f); EKit("EDemonSpike", b => EDemonSpike(b, i), tv, G(p.x, p.y), i * 50f, 1.3f, true); }
        }
        // ---- retakan api (bahaya) di dataran retakan
        for (int i = 0, k = 0; i < 200 && k < 9; i++)
        {
            Vector2 p = V(Random.Range(-280f, -90f), Random.Range(150f, 320f));
            if (DistToTrails(p.x, p.y) < 6f || GroundY(p.x, p.y) < WaterY + 1f) continue;
            EHazard(p, 4.2f, 12f, "Retakan api membakar kakimu!", fire, false); k++;
        }
        EHazard(V(120f, -60f), 9f, 6f, "Asap belerang menyengat.", new Color(0.8f, 0.7f, 0.2f), true);
        // ---- lentera ungu + papan arah
        ELamps(main, 28f, -300f, 290f, purple, EDemonLamp, "EDLamp");
        VSign(V(14f, -300f), "BASE CAMP << SELATAN", 6.2f);
        VSign(V(-14f, -170f), "SUNGAI API >> TIMUR", 6f);
        VSign(V(-14f, 60f), "<< ALTAR RITUAL", 5.2f);
        VSign(V(14f, 150f), "JURANG IBLIS >>", 5.2f);
        VSign(V(-14f, 220f), "<< DATARAN RETAKAN (BARA!)", 7f);
        VSign(V(8f, 270f), "AWAS: TAKHTA ZARGOTH", 6.6f);
        // ---- arena Zargoth
        EBossRing(V(0f, 320f), 38f, purple, "TAKHTA ZARGOTH: LORD IBLIS", V(10f, 262f));
        // ---- bahan gathering khusus
        EScatterNodes("hellfire_ash", 16, new[] { 1, 2, 3, 5 }, "Kumpulkan Abu Neraka", new Color(0.35f, 0.18f, 0.18f), new Color(0.1f, 0.05f, 0.05f), 2, 1, 3, 300f);
        EScatterNodes("demon_horn", 8, new[] { 4, 5, 2 }, "Cabut Tanduk Iblis", new Color(0.5f, 0.1f, 0.15f), new Color(0.12f, 0.05f, 0.07f), 0, 1, 2, 480f);
        EScatterNodes("brimstone", 6, new[] { 1, 3 }, "Kumpulkan Belerang", new Color(0.9f, 0.78f, 0.12f), new Color(0.15f, 0.1f, 0.1f), 1, 1, 2, 360f);
    }

    // ------------------------------------------------------------------ model
    static void ELampWood(GeoBag b)
    {
        b.Cyl(Sl.Wood, Vector3.zero, 0.14f, 3.2f, 6, 0.1f, true);
        b.Box(Sl.Wood, new Vector3(0.45f, 3.0f, 0), new Vector3(0.9f, 0.1f, 0.1f));
        b.Ball(Sl.Glow, new Vector3(0.85f, 2.8f, 0), 0.22f, 5, 8, 1.2f);
    }

    static void EGhostLamp(GeoBag b)
    {
        b.Cyl(Sl.Marble, Vector3.zero, 0.4f, 0.5f, 8, 0.3f, true);
        b.Cyl(Sl.DarkStone, new Vector3(0, 0.5f, 0), 0.12f, 2.6f, 6, 0.1f, true);
        b.Ball(Sl.Glow, new Vector3(0, 3.3f, 0), 0.32f, 6, 8, 1.3f);
        b.Cone(Sl.Marble, new Vector3(0, 3.0f, 0), 0.34f, 0.2f, 6);
    }

    static void EDemonLamp(GeoBag b)
    {
        b.Cyl(Sl.DarkStone, Vector3.zero, 0.5f, 0.5f, 6, 0.36f, true);
        b.Cyl(Sl.DarkStone, new Vector3(0, 0.5f, 0), 0.16f, 2.9f, 6, 0.1f, true);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            b.Cone(Sl.DarkStone, new Vector3(Mathf.Sin(a) * 0.28f, 3.3f, Mathf.Cos(a) * 0.28f), 0.08f, 0.55f, 4);
        }
        b.Ball(ForestKit.Crystal, new Vector3(0, 3.5f, 0), 0.3f, 5, 8, 1.4f);
    }

    static void EWagon(GeoBag b)
    {
        b.Box(Sl.Wood, new Vector3(0, 0.9f, 0), new Vector3(2.2f, 0.25f, 4.2f));
        b.Box(Sl.Wood, new Vector3(-1.05f, 1.45f, 0), new Vector3(0.12f, 1.0f, 4.2f));
        b.Box(Sl.Wood, new Vector3(1.05f, 1.45f, 0), new Vector3(0.12f, 1.0f, 4.2f));
        b.Box(Sl.Cloth, new Vector3(0, 2.3f, 0), new Vector3(2.0f, 0.2f, 3.8f));
        for (int i = 0; i < 3; i++) b.Box(Sl.Wood, new Vector3(0, 1.9f, -1.5f + i * 1.5f), new Vector3(2.0f, 0.14f, 0.14f));
        foreach (float sx in new[] { -1f, 1f }) foreach (float z in new[] { -1.4f, 1.4f })
            b.Cyl(Sl.DarkStone, new Vector3(sx * 1.2f, 0.55f, z), 0.55f, 0.12f, 12, 0.55f, true);
        b.Beam(Sl.Wood, new Vector3(0, 0.9f, 2.1f), new Vector3(0, 0.7f, 4.8f), 0.1f, 0.1f);
    }

    static void EWreck(GeoBag b)
    {
        b.Box(Sl.Wood, new Vector3(0, 0.5f, 0), new Vector3(2.2f, 0.2f, 4f), 12f);
        b.Box(Sl.Wood, new Vector3(-1.0f, 0.9f, 0.5f), new Vector3(0.12f, 1.2f, 3f), 18f);
        b.Beam(Sl.Wood, new Vector3(1.0f, 0.5f, -1f), new Vector3(2.6f, 1.6f, -2.4f), 0.1f, 0.1f);
        b.Cyl(Sl.DarkStone, new Vector3(2.2f, 0.4f, 1.8f), 0.55f, 0.12f, 12, 0.55f, true);
        b.Box(Sl.Cloth, new Vector3(0.3f, 0.65f, -0.5f), new Vector3(1.5f, 0.06f, 1.8f), 25f);
    }

    static void ETotem(GeoBag b)
    {
        b.Cyl(Sl.Stone, Vector3.zero, 1.6f, 0.6f, 8, 1.3f, true);
        b.Cyl(Sl.Wood, new Vector3(0, 0.6f, 0), 0.5f, 6.4f, 8, 0.42f, true);
        b.Box(Sl.DarkStone, new Vector3(0, 5.2f, 0.4f), new Vector3(1.4f, 1.0f, 0.8f));
        b.Cone(Sl.DarkStone, new Vector3(-0.5f, 5.7f, 0.4f), 0.22f, 0.7f, 4);
        b.Cone(Sl.DarkStone, new Vector3(0.5f, 5.7f, 0.4f), 0.22f, 0.7f, 4);
        b.Ball(Sl.Glow, new Vector3(-0.3f, 5.3f, 0.82f), 0.12f, 3, 6);
        b.Ball(Sl.Glow, new Vector3(0.3f, 5.3f, 0.82f), 0.12f, 3, 6);
        b.Box(Sl.DarkStone, new Vector3(0, 3.6f, 0.3f), new Vector3(1.8f, 0.5f, 0.4f));
        b.Ball(Sl.Glow, new Vector3(0, 6.9f, 0), 0.4f, 5, 8);
    }

    static void EGrave(GeoBag b, int v)
    {
        b.Box(Sl.Stone, new Vector3(0, 0.12f, 0), new Vector3(1.0f, 0.24f, 2.0f));
        if (v == 0) { b.Box(Sl.Stone, new Vector3(0, 1.0f, -0.8f), new Vector3(0.2f, 1.8f, 0.2f)); b.Box(Sl.Stone, new Vector3(0, 1.5f, -0.8f), new Vector3(0.9f, 0.2f, 0.2f)); }
        else if (v == 1) { b.Box(Sl.Marble, new Vector3(0, 0.8f, -0.9f), new Vector3(0.9f, 1.4f, 0.2f)); b.Ball(Sl.Marble, new Vector3(0, 1.5f, -0.9f), 0.45f, 3, 6, 0.6f); }
        else { b.Cyl(Sl.Metal, new Vector3(0, 0.2f, -0.8f), 0.05f, 1.6f, 5, 0.04f, true); b.Box(Sl.Metal, new Vector3(0, 1.5f, -0.8f), new Vector3(0.7f, 0.12f, 0.05f)); }
        EGraveArt(b, v);
    }

    static void EMausoleum(GeoBag b)
    {
        b.Box(Sl.Stone, new Vector3(0, 1.5f, 0), new Vector3(10, 3f, 8));
        b.Box(Sl.DarkStone, new Vector3(0, 3.4f, 0), new Vector3(11, 0.8f, 9));
        b.Cone(Sl.DarkStone, new Vector3(0, 3.8f, 0), 5.4f, 3.6f, 4);
        b.Box(Sl.DarkStone, new Vector3(0, 1.2f, 4.05f), new Vector3(2.6f, 2.4f, 0.2f));
        foreach (float sx in new[] { -1f, 1f }) b.Cyl(Sl.Marble, new Vector3(sx * 2.4f, 0, 4.4f), 0.4f, 3.2f, 8, 0.36f, true);
        b.Box(Sl.Marble, new Vector3(0, 3.0f, 4.4f), new Vector3(6.2f, 0.4f, 0.6f));
        EMausoleumArt(b);
    }

    static void ESkullPile(GeoBag b, int v)
    {
        int n = 7 + v * 3;
        var r = new System.Random(50 + v);
        for (int i = 0; i < n; i++)
        {
            float a = (float)r.NextDouble() * 6.28f, rad = (float)r.NextDouble() * 1.6f;
            b.Ball(Sl.Marble, new Vector3(Mathf.Cos(a) * rad, 0.3f + (float)r.NextDouble() * 0.9f, Mathf.Sin(a) * rad), 0.32f, 3, 6);
        }
        b.Cyl(Sl.DarkStone, new Vector3(0, -0.1f, 0), 1.9f, 0.4f, 8, 1.4f, true);
    }

    static void EStake(GeoBag b)
    {
        b.Cyl(Sl.Wood, Vector3.zero, 0.1f, 3.6f, 5, 0.05f, true);
        b.Ball(Sl.Marble, new Vector3(0, 3.7f, 0), 0.3f, 3, 6);
        b.Cone(Sl.Wood, new Vector3(0, 3.4f, 0), 0.08f, 0.5f, 4);
    }

    static void ESiege(GeoBag b)
    {
        b.Box(Sl.Wood, new Vector3(0, 0.6f, 0), new Vector3(2.4f, 0.3f, 5f));
        foreach (float sx in new[] { -1f, 1f }) foreach (float z in new[] { -1.8f, 1.8f })
            b.Cyl(Sl.Wood, new Vector3(sx * 1.4f, 0.8f, z), 0.8f, 0.2f, 12, 0.8f, true);
        b.Beam(Sl.Wood, new Vector3(0, 0.9f, -1.5f), new Vector3(0.4f, 4.8f, 1.2f), 0.18f, 0.12f);
        b.Beam(Sl.Wood, new Vector3(-1f, 0.9f, 0.2f), new Vector3(-0.4f, 3.4f, 0.4f), 0.12f, 0.1f);
        b.Ball(Sl.Stone, new Vector3(0.4f, 5.0f, 1.3f), 0.5f, 4, 8);
    }

    static void EBannerPole(GeoBag b)
    {
        b.Cyl(Sl.Wood, Vector3.zero, 0.09f, 4.8f, 5, 0.07f, true);
        b.Box(Sl.Cloth, new Vector3(0.7f, 4.0f, 0), new Vector3(1.4f, 1.6f, 0.05f), 8f);
        b.Beam(Sl.Wood, new Vector3(-0.4f, 0.2f, 0), new Vector3(-1.2f, 0f, 0.4f), 0.08f, 0.04f);
    }

    static void EPentagram(GeoBag b)
    {
        b.Annulus(Sl.Glow, Vector3.zero, 16f, 17f, 0f, 360f, 48, 0.1f);
        b.Annulus(Sl.Glow, Vector3.zero, 11.5f, 12f, 0f, 360f, 40, 0.1f);
        for (int i = 0; i < 5; i++)
        {
            float a1 = (90f + i * 144f) * Mathf.Deg2Rad, a2 = (90f + (i + 1) * 144f) * Mathf.Deg2Rad;
            b.Beam(Sl.Glow, new Vector3(Mathf.Cos(a1) * 15.5f, 0.12f, Mathf.Sin(a1) * 15.5f), new Vector3(Mathf.Cos(a2) * 15.5f, 0.12f, Mathf.Sin(a2) * 15.5f), 0.45f, 0.45f);
        }
        b.Cyl(Sl.DarkStone, new Vector3(0, 0, 0), 2.4f, 1.0f, 8, 2.0f, true);
        b.Ball(Sl.Glow, new Vector3(0, 2.0f, 0), 0.8f, 5, 8);
    }

    static void ECage(GeoBag b)
    {
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            b.Cyl(Sl.Metal, new Vector3(Mathf.Cos(a) * 1.1f, 0f, Mathf.Sin(a) * 1.1f), 0.05f, 3.2f, 4, 0.05f, true);
        }
        b.Cyl(Sl.DarkStone, new Vector3(0, 0, 0), 1.2f, 0.2f, 8, 1.2f, true);
        b.Cyl(Sl.DarkStone, new Vector3(0, 3.2f, 0), 1.2f, 0.2f, 8, 0.3f, true);
        b.Ball(Sl.Marble, new Vector3(0.3f, 0.45f, 0.1f), 0.3f, 3, 6);
        ECageArt(b);
    }

    static void EDemonStatue(GeoBag b)
    {
        b.Box(Sl.DarkStone, new Vector3(0, 0.6f, 0), new Vector3(4, 1.2f, 4));
        b.Cyl(Sl.DarkStone, new Vector3(0, 1.2f, 0), 1.2f, 5f, 8, 0.9f, true);
        b.Ball(Sl.DarkStone, new Vector3(0, 6.6f, 0), 0.9f, 5, 8);
        b.Cone(Sl.DarkStone, new Vector3(-0.7f, 6.9f, 0), 0.2f, 1.6f, 5);
        b.Cone(Sl.DarkStone, new Vector3(0.7f, 6.9f, 0), 0.2f, 1.6f, 5);
        b.Beam(Sl.DarkStone, new Vector3(-1f, 5.4f, 0), new Vector3(-3.6f, 7.2f, 0), 0.5f, 0.1f);
        b.Beam(Sl.DarkStone, new Vector3(1f, 5.4f, 0), new Vector3(3.6f, 7.2f, 0), 0.5f, 0.1f);
        b.Ball(Sl.Glow, new Vector3(-0.3f, 6.7f, 0.8f), 0.12f, 3, 6); b.Ball(Sl.Glow, new Vector3(0.3f, 6.7f, 0.8f), 0.12f, 3, 6);
    }

    static void EHornGate(GeoBag b)
    {
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Cyl(Sl.DarkStone, new Vector3(sx * 7f, 0, 0), 1.6f, 10f, 7, 1.1f, true);
            b.Cone(Sl.DarkStone, new Vector3(sx * 7f, 10f, 0), 1.1f, 6f, 5);
            b.Cone(Sl.Glow, new Vector3(sx * 7f, 15.4f, 0), 0.35f, 2f, 4);
        }
        b.Box(Sl.DarkStone, new Vector3(0, 10.4f, 0), new Vector3(15f, 1.6f, 2.4f));
        b.Ball(Sl.Glow, new Vector3(0, 11.6f, 1.2f), 0.9f, 5, 8);
        EHornGateArt(b);
    }

    static void EDemonSpike(GeoBag b, int v)
    {
        b.Cone(Sl.DarkStone, new Vector3(0, -0.5f, 0), 1.3f + v * 0.1f, 6f + v * 1.2f, 5);
        b.Cone(Sl.DarkStone, new Vector3(1.1f, -0.5f, 0.4f), 0.8f, 3.4f, 5);
        b.Cone(Sl.Glow, new Vector3(0, 4.8f + v * 1.2f, 0), 0.2f, 1f, 4);
        EDemonSpikeArt(b, v);
    }
}
