using UnityEngine;
using HunterGuild;

/// <summary>Siluet Desa Pemula di kejauhan, di luar celah lembah Hutan Pemula (arah gerbang "Jalan pulang").</summary>
public static partial class TownSceneBuilder
{
    static void VForestVillageBackdrop()
    {
        // Desa Pemula asli: pagar bulat radius VR=70, gerbang utara di (0, +70) dengan jalan menuju hutan.
        // Di sini pusat desa ditaruh di z = -200 - VR sehingga gerbang utaranya jatuh tepat di ujung lembah (z=-200).
        var root = Group("SiluetDesa", tProps);
        float gy = vH(0f, -168f) - 0.05f;
        Vector3 C = new Vector3(0f, gy, -200f - VR);
        var ground = UMat("SiluetTanah", new Color(0.36f, 0.46f, 0.26f));
        Prim(PrimitiveType.Cube, "TanahSiluet", root, new Vector3(0f, gy - 0.5f, -168f - 260f), new Vector3(900f, 1f, 520f), ground, false);
        var dirt = UMat("SiluetJalan", new Color(0.40f, 0.31f, 0.20f));
        Prim(PrimitiveType.Cube, "JalanSiluet", root, new Vector3(0f, gy + 0.04f, -168f - 100f), new Vector3(6.4f, 0.1f, 200f), dirt, false);
        var r = new System.Random(77);
        System.Func<double, double, float> R = (a2, c2) => (float)(a2 + r.NextDouble() * (c2 - a2));

        // pagar glondongan runcing mengikuti lingkaran desa (sisi yang menghadap hutan), celah 5.2 m di gerbang
        var pb = new GeoBag();
        for (float ang = -105f; ang <= 105f; ang += 0.55f * 180f / (Mathf.PI * VR))
        {
            float a2 = ang * Mathf.Deg2Rad;
            Vector3 p = C + new Vector3(Mathf.Sin(a2), 0f, Mathf.Cos(a2)) * VR;
            if (Mathf.Abs(p.x) < 5.2f && p.z > C.z) continue;
            float h = 4.3f + R(0, 0.9);
            pb.Cyl(Sl.Wood, p, 0.33f, h, 7, 0.3f, false, false);
            pb.Cone(Sl.WoodLight, p + Vector3.up * h, 0.32f, 0.6f, 7);
        }
        Vector3 gate = new Vector3(0f, gy, -200f);
        VGateFrame(pb, gate, 0f, 5.2f);
        pb.Emit(root, "PagarSiluet", baseSlots);
        VSignBoth(root, gate, 0f, "GERBANG UTARA\nHutan Pemula (Lv 0-5)", 0.12f);

        // rumah HouseKit asli di sepanjang jalan utara desa (kiri-kanan), menghadap jalan
        int[] kinds = { 0, 3, 3, 5, 1, 2 };
        float[] zs = { -222f, -240f, -258f, -276f, -294f, -312f, -330f, -348f, -366f };
        foreach (float z in zs)
            for (int side = -1; side <= 1; side += 2)
            {
                var def = VDef(kinds[r.Next(kinds.Length)], r.Next(40));
                float x = side * (3.6f + def.d / 2f + 3f + R(0, 2));
                int v = r.Next(8);
                var slots = TownMats.Slots(TownMats.Plaster[v % TownMats.Plaster.Length], TownMats.Roofs[(v + 1) % TownMats.Roofs.Length]);
                if (def.kind == 3 || def.kind == 5) slots[Sl.Roof] = TownMats.Thatch;
                // bagian depan rumah (+Z lokal) menghadap jalan
                float yaw = side < 0 ? 90f : -90f;
                PropKit.Place(def.proto, root, "RumahSiluet", new Vector3(x, gy, z + R(-2, 2)), yaw, 1f, slots);
            }
        // gereja di barat daya (menara dan atap) sebagai penanda
        var cb = new GeoBag();
        Vector3 cc = new Vector3(-48f, gy, -300f);
        cb.Box(Sl.Plaster, cc + new Vector3(0f, 3.6f, 0f), new Vector3(10f, 6f, 20f));
        cb.Gable(Sl.Roof, Sl.Plaster, Sl.Wood, cc + new Vector3(0f, 6.6f, 0f), 10f, 20f, 4.2f, 0.6f, false);
        cb.Box(Sl.Stone, cc + new Vector3(0f, 8.6f, 11.2f), new Vector3(6.2f, 16f, 6.2f));
        cb.Cone(Sl.Roof, cc + new Vector3(0f, 19.4f, 11.2f), 4.6f, 9f, 4);
        cb.Box(Sl.Gold, cc + new Vector3(0f, 29.4f, 11.2f), new Vector3(0.25f, 2.4f, 0.25f));
        cb.Emit(root, "GerejaSiluet", baseSlots);
        // pohon desa di belakang
        for (int i = 0; i < 10; i++)
        {
            float a2 = R(-60, 60) * Mathf.Deg2Rad, rr = R(18, 55);
            Vector3 q = C + new Vector3(Mathf.Sin(a2), 0f, -Mathf.Cos(a2)) * rr;
            PropKit.Place(PropKit.TreeProto(r.Next(3)), root, "PohonSiluet", new Vector3(q.x, gy, q.z), R(0, 360), R(1.0, 1.4), baseSlots);
        }
    }

    /// <summary>Siluet Hutan Pemula di balik gerbang utara Desa Pemula: bukit berlapis + hutan cemara.</summary>
    static void VVillageForestBackdrop()
    {
        var root = Group("SiluetHutanPemula", tProps);
        float gy = vH(0f, 185f) - 0.05f;
        var ground = UMat("SiluetHutanTanah", new Color(0.18f, 0.30f, 0.20f));
        Prim(PrimitiveType.Cube, "TanahHutanSiluet", root, new Vector3(0f, gy - 0.5f, 190f + 300f), new Vector3(1400f, 1f, 600f), ground, false);
        var b = new GeoBag();
        var r = new System.Random(91);
        System.Func<double, double, float> R = (a, c) => (float)(a + r.NextDouble() * (c - a));
        // bukit berlapis (makin jauh makin tinggi)
        for (int layer = 0; layer < 3; layer++)
        {
            float z = 300f + layer * 150f;
            for (int i = -4; i <= 4; i++)
                b.Cone(Sl.Grass, new Vector3(i * 150f + R(-40, 40) + layer * 40f, gy - 2f, z + R(-20, 20)), R(140, 200), 70f + layer * 45f + R(0, 25), 14);
        }
        // hutan cemara lebat di atas bukit
        for (int layer = 0; layer < 4; layer++)
        {
            float z = 235f + layer * 90f;
            int n = 34;
            for (int i = 0; i < n; i++)
            {
                float x = -520f + i * (1040f / n) + R(-14, 14);
                float h = R(26, 44) + layer * 10f, rad = h * 0.26f;
                float y = gy + (layer + 1) * 12f;
                b.Cyl(Sl.Wood, new Vector3(x, y, z), 1.4f, h * 0.25f, 6, 1.2f, true);
                b.Cone(Sl.Leaf, new Vector3(x, y + h * 0.18f, z), rad, h * 0.55f, 8);
                b.Cone(Sl.Leaf, new Vector3(x, y + h * 0.45f, z), rad * 0.75f, h * 0.55f, 8);
            }
        }
        // jalur setapak/gerbang hutan: celah terang di tengah + kabut asap pondok
        b.Cyl(Sl.DarkStone, new Vector3(18f, gy + 24f, 268f), 1.2f, 18f, 8, 1.2f, true);
        b.Emit(root, "HutanSiluet", baseSlots);
    }
}
