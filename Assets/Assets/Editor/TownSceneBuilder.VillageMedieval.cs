using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>Sentuhan fantasi-abad-pertengahan + isekai untuk Desa Pemula: kristal mana, lentera kristal, menara jaga, bendera guild, pohon tua bercahaya, pandai besi.</summary>
public static partial class TownSceneBuilder
{
    static Material[] vCrystalSlots;

    static Material[] VCrystalSlots()
    {
        DungeonKit.Load();
        vCrystalSlots = (Material[])baseSlots.Clone();
        vCrystalSlots[Sl.Glow] = DungeonKit.Crystal;
        return vCrystalSlots;
    }

    static void VMedievalExtras()
    {
        var cs = VCrystalSlots();
        var g = Group("FantasiDesa", tProps);

        // ---- Kristal Mana melayang di atas air mancur ----
        var kr = new GameObject("KristalMana").transform;
        kr.SetParent(g, false);
        kr.position = new Vector3(0f, 7.4f, 0f);
        var kb = new GeoBag();
        kb.Ball(Sl.Glow, Vector3.zero, 1.0f, 2, 4, 1.9f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            kb.Ball(Sl.Glow, new Vector3(Mathf.Sin(a) * 2.2f, Mathf.Sin(i * 1.7f) * 0.9f, Mathf.Cos(a) * 2.2f), 0.26f, 2, 4, 1.7f);
        }
        kb.Emit(kr, "Mesh", cs);
        var sp = kr.gameObject.AddComponent<SpinObject>();
        sp.axis = Vector3.up; sp.degreesPerSecond = 22f;
        var lg = new GameObject("CahayaKristal").transform;
        lg.SetParent(g, false);
        lg.position = new Vector3(0f, 7.4f, 0f);
        var lt = lg.gameObject.AddComponent<Light>();
        lt.type = LightType.Point; lt.range = 30f; lt.intensity = 2.4f; lt.color = new Color(0.45f, 0.8f, 1f);

        // ---- Menara jaga di kedua gerbang + bendera guild ----
        VMapMark("Gerbang Utara (Hutan Pemula)", new Vector2(0f, VR), 5);
        VMapMark("Gerbang Timur (Kota)", new Vector2(VR, 0f), 5);
        VGateTowers(new Vector3(0f, 0f, VR), 0f, cs);
        VGateTowers(new Vector3(VR, 0f, 0f), 90f, cs);

        // ---- Bendera guild di sepanjang jalan utama ----
        var fb = new GeoBag();
        int fi = 0;
        for (int si = 0; si < 2; si++)
        {
            var st = vStreets[si];
            float acc = 8f;
            for (int i = 0; i + 1 < st.Count; i++)
            {
                Vector2 a = st[i], b = st[i + 1];
                acc += (b - a).magnitude;
                if (acc < 19f) continue;
                acc = 0f;
                Vector2 tan = (b - a).normalized, nrm = new Vector2(tan.y, -tan.x);
                Vector2 p = (a + b) / 2f + nrm * ((fi++ % 2 == 0) ? 3.9f : -3.9f);
                if (p.magnitude < 19f || p.magnitude > VR - 6f || !IsFree(p.x, p.y, 0.9f)) continue;
                Reserve(p.x, p.y, 0.9f);
                Vector3 bp = V3(p);
                fb.Cyl(Sl.Wood, bp, 0.12f, 5.2f, 6, 0.1f, true, false);
                fb.Beam(Sl.Wood, bp + new Vector3(0f, 4.9f, 0f), bp + new Vector3(nrm.x * -1.4f * ((fi % 2 == 0) ? 1f : -1f), 4.9f, nrm.y * -1.4f * ((fi % 2 == 0) ? 1f : -1f)), 0.07f, 0.07f);
                Vector3 hang = bp + new Vector3(nrm.x * -1.4f * ((fi % 2 == 0) ? 1f : -1f), 0f, nrm.y * -1.4f * ((fi % 2 == 0) ? 1f : -1f));
                float yawF = Mathf.Atan2(tan.x, tan.y) * Mathf.Rad2Deg;
                fb.Box(fi % 2 == 0 ? Sl.Cloth : Sl.ClothB, hang + new Vector3(0f, 3.8f, 0f), new Vector3(0.12f, 2.2f, 1.1f), yawF);
                fb.Box(Sl.Gold, hang + new Vector3(0f, 4.95f, 0f), new Vector3(0.14f, 0.12f, 1.2f), yawF);
            }
        }
        fb.Emit(g, "BenderaGuild", baseSlots);

        // ---- Pandai besi desa: rumah sungguhan (bisa dimasuki; di dalam: tempa/enchant/repair + toko set perlengkapan) ----
        VSmithyHouse(g);

        // ---- Pohon tua bercahaya di depan gereja ----
        {
            Vector3 cw = VPol(235f, VR - 20f);
            float cy = YawToward(new Vector2(cw.x, cw.z), Vector2.zero);
            Vector3 tp = cw + Quaternion.Euler(0f, cy, 0f) * new Vector3(15f, 0f, 21f);
            var tree = PropKit.Place(PropKit.TreeProto(5), g, "PohonTuaBercahaya", tp, 40f, 1.7f, baseSlots);
            var tc = tree.AddComponent<CapsuleCollider>();
            tc.center = new Vector3(0f, 2f, 0f); tc.radius = 1.0f; tc.height = 4f;
            Reserve(tp.x, tp.z, 5f);
            var gb = new GeoBag();
            var rr = new System.Random(8);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rr.NextDouble() * 360f * Mathf.Deg2Rad, r = 1.6f + (float)rr.NextDouble() * 2.2f, hy = 3.6f + (float)rr.NextDouble() * 3.4f;
                gb.Ball(Sl.Glow, tp + new Vector3(Mathf.Sin(a) * r, hy, Mathf.Cos(a) * r), 0.16f, 2, 5);
            }
            gb.Emit(g, "LenteraPohon", cs);
            AddLight(g, tp + new Vector3(0f, 6f, 0f), 18f, 1.6f);
        }
    }

    static void VSmithyHouse(Transform g)
    {
        if (HouseKit.Defs.Count == 0) HouseKit.BuildAll();
        var def = VDef(5, 3);
        Vector2 pos; float yaw;
        if (!FindHomeSpot(new Vector2(-30f, -12f), def, 22f, 72f, out pos, out yaw)) { Debug.LogWarning("Tidak ada lahan untuk Pandai Besi"); return; }
        var slots = TownMats.Slots(TownMats.Plaster[4 % TownMats.Plaster.Length], TownMats.Roofs[3 % TownMats.Roofs.Length]);
        var go = PropKit.Place(def.proto, g, "RumahPandaiBesi", V3(pos), yaw, 1f, slots);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, def.height / 2f, 0f);
        bc.size = new Vector3(def.w, def.height, def.d);
        ReserveBox(pos.x, pos.y, def.w + 1.5f, def.d + 1.5f, yaw, 0f);
        Reserve(pos.x, pos.y, Mathf.Max(def.w, def.d) / 2f + 1f);
        VMapMark("Pandai Besi & Toko Perlengkapan", pos, 1);
        Vector2 fwd = new Vector2(Mathf.Sin(yaw * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad));
        Vector2 rgt = new Vector2(fwd.y, -fwd.x);
        Vector2 doorPt = pos + fwd * (def.d / 2f + 1.6f);
        Reserve(doorPt.x, doorPt.y, 3.5f);

        var holder = new GameObject("PandaiBesi_Papan").transform;
        holder.SetParent(g, false);
        holder.position = V3(pos);
        holder.rotation = Quaternion.Euler(0f, yaw, 0f);
        float sy = Mathf.Min(def.height * 0.62f, 5.2f);
        Prim(PrimitiveType.Cube, "PlatNama", holder, new Vector3(0f, sy, def.d / 2f + 0.15f), new Vector3(8.6f, 1.4f, 0.3f), UMat("PandaiPlat", new Color(0.16f, 0.1f, 0.06f)), false);
        BuildKit.Text(holder, "PANDAI BESI", new Vector3(0f, sy + 0.1f, def.d / 2f + 0.34f), 180f, 0.16f, new Color(1f, 0.78f, 0.45f));
        BuildKit.Text(holder, "Tempa - Perlengkapan - Reparasi", new Vector3(0f, sy - 0.5f, def.d / 2f + 0.34f), 180f, 0.065f, new Color(0.95f, 0.9f, 0.75f));
        // landasan, tungku bara, dan tong di samping pintu (di luar jalur masuk)
        var db = new GeoBag();
        Vector3 ap = V3(pos + fwd * (def.d / 2f + 1.4f) + rgt * 4.2f);
        db.Box(Sl.Wood, ap + new Vector3(0f, 0.3f, 0f), new Vector3(0.7f, 0.6f, 0.7f), yaw);
        db.Box(Sl.DarkStone, ap + new Vector3(0f, 0.75f, 0f), new Vector3(1.1f, 0.3f, 0.45f), yaw);
        Vector3 fp = V3(pos + fwd * (def.d / 2f + 1.4f) - rgt * 4.2f);
        db.Box(Sl.DarkStone, fp + new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1.0f, 1.0f), yaw);
        db.Box(Sl.Glow, fp + new Vector3(0f, 1.02f, 0f), new Vector3(1.0f, 0.06f, 0.7f), yaw);
        db.Emit(g, "PandaiBesiDekor", baseSlots);
        AddLight(holder, new Vector3(-4.2f, 1.8f, def.d / 2f + 1.4f), 9f, 1.3f);
        AddLight(holder, new Vector3(0f, 3.6f, def.d / 2f + 1.5f), 14f, 1.3f);

        var door = new GameObject("Pintu_PandaiBesi").transform;
        door.SetParent(g, false);
        door.position = V3(doorPt);
        door.rotation = Quaternion.Euler(0f, yaw, 0f);
        var bd = door.gameObject.AddComponent<BuildingDoor>();
        bd.interiorId = "Smithy"; bd.radius = 3.8f; bd.prompt = "Masuk Pandai Besi";
    }

    static void VGateTowers(Vector3 gate, float az, Material[] cs)
    {
        float a = az * Mathf.Deg2Rad;
        Vector3 t = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
        var tb = new GeoBag();
        foreach (float s in new[] { -1f, 1f })
        {
            Vector3 bp = gate + t * (s * 10.5f);
            BuildKit.RoundTower(tb, bp, 3.4f, 13f, Sl.Stone, Sl.DarkStone, Sl.Roof, 6.5f, false, true, Sl.Cloth);
            tb.Box(Sl.Glow, bp + new Vector3(0f, 9f, 0f) + Fwd(az) * 3.1f, new Vector3(0.6f, 1.1f, 0.2f), az);
            Reserve(bp.x, bp.z, 5f);
        }
        var go = tb.Emit(tGates, "MenaraJaga", baseSlots);
        foreach (float s in new[] { -1f, 1f })
        {
            Vector3 bp = gate + t * (s * 10.5f);
            var c = new GameObject("MenaraCol").transform;
            c.SetParent(tGates, false);
            c.position = bp + Vector3.up * 6.5f;
            var cap = c.gameObject.AddComponent<CapsuleCollider>();
            cap.radius = 3.6f; cap.height = 13f;
            AddLight(tGates, bp + new Vector3(0f, 9f, 0f) + Fwd(az) * 3.6f, 12f, 1.3f);
        }
    }
}
