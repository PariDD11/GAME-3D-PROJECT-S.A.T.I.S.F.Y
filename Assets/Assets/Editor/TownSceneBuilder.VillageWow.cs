using UnityEngine;
using HunterGuild;

/// <summary>
/// Tata ruang "wow" Desa Pemula: empat gerbang lengkung pualam di mulut alun-alun (utara/timur/selatan/barat) dengan lentera & panji,
/// jalan utama berpohon simetris (sakura & pohon rindang berselang-seling, kembar kiri-kanan), dan delapan taman bunga melingkar
/// mengelilingi alun-alun (di sela empat sumbu dan empat diagonal). Semua bersumbu simetris sehingga denah terbaca rapi dari udara.
/// </summary>
public static partial class TownSceneBuilder
{
    static void VWow()
    {
        var root = Group("TataRuangDesa", tProps);
        VPlazaArches(root);
        VPlazaPlanters(root);
        VAvenues(root);
    }

    static void VPlazaArches(Transform root)
    {
        float[] azs = { 0f, 90f, 180f, 270f };
        for (int i = 0; i < 4; i++)
        {
            float az = azs[i];
            Vector3 c = VPol(az, 19f);
            Vector3 dir = VPol(az, 1f), perp = new Vector3(dir.z, 0f, -dir.x);
            var b = new GeoBag();
            const float half = 5.4f;
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Vector3 pc = c + perp * (sgn * half);
                b.Box(Sl.Marble, pc + Vector3.up * 0.3f, new Vector3(1.7f, 0.6f, 1.7f), az);          // alas
                b.Cyl(Sl.Marble, pc + Vector3.up * 0.6f, 0.62f, 5.2f, 12, 0.5f, true);                 // tiang
                b.Cyl(Sl.Gold, pc + Vector3.up * 1.1f, 0.68f, 0.14f, 12, 0.68f, true);
                b.Cyl(Sl.Gold, pc + Vector3.up * 5.5f, 0.7f, 0.16f, 12, 0.7f, true);
                b.Ball(Sl.Marble, pc + Vector3.up * 6.15f, 0.55f, 3, 8);                                // kepala tiang
                // lentera menggantung di sisi dalam
                Vector3 lp = pc - perp * (sgn * 0.9f) + Vector3.up * 4.3f;
                b.Beam(Sl.Wood, pc + Vector3.up * 4.9f, lp + Vector3.up * 0.5f, 0.07f, 0.07f);
                b.Cyl(Sl.Gold, lp, 0.22f, 0.08f, 8, 0.22f, true);
                b.Ball(Sl.Glow, lp - Vector3.up * 0.1f, 0.24f, 3, 7, 1.2f);
            }
            // balok lintang melengkung (busur dari 7 segmen) + panji
            Vector3 prev = c - perp * half + Vector3.up * 5.5f;
            for (int k = 1; k <= 8; k++)
            {
                float u = k / 8f;
                Vector3 p = c - perp * half + perp * (2f * half * u) + Vector3.up * (5.5f + Mathf.Sin(u * Mathf.PI) * 1.5f);
                b.Beam(Sl.Marble, prev, p, 0.55f, 0.55f);
                prev = p;
            }
            b.Ball(Sl.Gold, c + Vector3.up * 7.35f, 0.34f, 3, 8);
            for (int k = 0; k < 3; k++)                                                                  // panji berumbai
            {
                float u = (k + 1) / 4f;
                Vector3 top = c - perp * half + perp * (2f * half * u) + Vector3.up * (5.4f + Mathf.Sin(u * Mathf.PI) * 1.5f);
                b.Box(k % 2 == 0 ? Sl.Cloth : Sl.ClothB, top - Vector3.up * 0.85f, new Vector3(0.7f, 1.5f, 0.05f), az);
            }
            b.Emit(root, "Gerbang_Lengkung" + i, baseSlots, MeshDir + "/Vil_Arch" + i + ".asset");
            for (int sgn = -1; sgn <= 1; sgn += 2)
                BoxCol(root, "LengkungCol", c + perp * (sgn * half) + Vector3.up * 2.8f, new Vector3(1.4f, 5.6f, 1.4f), new Vector3(0f, az, 0f));
            Reserve(c.x + perp.x * half, c.z + perp.z * half, 1.4f); Reserve(c.x - perp.x * half, c.z - perp.z * half, 1.4f);
        }
    }

    static void VPlazaPlanters(Transform root)
    {
        var b = new GeoBag();
        const float r = 21f;
        for (int s = 0; s < 8; s++)
        {
            float a0 = s * 45f + 14f, a1 = s * 45f + 31f;
            Vector3 mid = VPol((a0 + a1) / 2f, r);
            if (!IsFree(mid.x, mid.z, 3.2f)) continue;
            b.Annulus(Sl.Stone, Vector3.zero, r - 0.75f, r + 0.75f, a0, a1, 6, 0.07f);          // bingkai batu
            b.Annulus(Sl.DarkStone, Vector3.zero, r - 0.55f, r + 0.55f, a0, a1, 6, 0.1f);        // tanah
            for (int k = 0; k <= 6; k++)
            {
                float a = Mathf.Lerp(a0 + 1.2f, a1 - 1.2f, k / 6f);
                Vector3 p = VPol(a, r + ((k % 2) * 2 - 1) * 0.18f);
                b.Ball(k % 3 == 1 ? Sl.Leaf : Sl.Hedge, p + Vector3.up * 0.28f, 0.55f, 3, 7, 0.8f);
                if (k % 2 == 0) b.Ball(k % 4 == 0 ? Sl.Blossom : Sl.Gold, p + Vector3.up * 0.78f, 0.13f, 3, 5);
            }
            Reserve(mid.x, mid.z, 3f);
        }
        b.Emit(root, "TamanAlun", baseSlots, MeshDir + "/Vil_Planters.asset");
    }

    static void VAvenues(Transform root)
    {
        var g = Group("PohonJalanUtama", root);
        // empat sumbu: utara, timur (panjang), selatan, barat (pendek). Pasangan kembar kiri-kanan, berselang-seling sakura / pohon rindang.
        float[] azs = { 0f, 90f, 180f, 270f };
        float[] ends = { 90f, 90f, 56f, 56f };
        int n = 0;
        for (int i = 0; i < 4; i++)
        {
            Vector3 dir = VPol(azs[i], 1f), perp = new Vector3(dir.z, 0f, -dir.x);
            int idx = 0;
            for (float d = 26f; d <= ends[i]; d += 10f, idx++)
            {
                Vector3 pl = dir * d + perp * 7.4f, pr = dir * d - perp * 7.4f;
                if (!IsFree(pl.x, pl.z, 2.4f) || !IsFree(pr.x, pr.z, 2.4f)) continue;      // hanya berpasangan agar tetap simetris
                int kind = idx % 2 == 0 ? 2 : 0;
                foreach (var p in new[] { pl, pr })
                {
                    Reserve(p.x, p.z, 2.4f);
                    float y = VGy(p.x, p.z);
                    var go = PropKit.Place(PropKit.TreeProto(kind), g, kind == 2 ? "SakuraJalan" : "PohonJalan", new Vector3(p.x, y, p.z), (n++ * 47f) % 360f, 1.05f, baseSlots);
                    var cc = go.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.6f; cc.height = 3f;
                }
            }
        }
    }
}
