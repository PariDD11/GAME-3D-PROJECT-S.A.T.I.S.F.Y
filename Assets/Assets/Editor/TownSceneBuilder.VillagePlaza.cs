using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Alun-alun Desa Pemula yang dirombak: lantai batu berpola radial simetris 8 arah, air mancur bertingkat tiga (bak bundar besar,
/// pulau pualam, dua mangkuk bertingkat, pucuk kristal), delapan pancuran dari bibir bak, empat tiang lentera di bibir bak,
/// bunga teratai di air, delapan bangku & delapan sakura yang tersusun simetris.
/// </summary>
public static partial class TownSceneBuilder
{
    static Vector3 PP(float x, float y, float z) { return new Vector3(x, y, z); }

    static void VPlazaAndFountain()
    {
        // ------------------------------------------------------------ lantai alun-alun (pola radial)
        var pz = new GeoBag();
        pz.Disc(Sl.Cobble, Vector3.zero, 15.2f, 56, 0.05f);
        for (int k = 0; k < 8; k++)
        {
            float a0 = k * 45f + 4f, a1 = k * 45f + 41f;
            pz.Annulus(Sl.Pave, Vector3.zero, 8.4f, 13.4f, a0, a1, 6, 0.068f);       // 8 baji batu pipih
        }
        pz.Annulus(Sl.Stone, Vector3.zero, 14.4f, 15.2f, 0f, 360f, 56, 0.12f);         // tepi luar
        pz.Annulus(Sl.DarkStone, Vector3.zero, 13.5f, 13.8f, 0f, 360f, 56, 0.09f);     // garis penghias
        pz.Annulus(Sl.Pave, Vector3.zero, 7.3f, 8.3f, 0f, 360f, 48, 0.08f);            // cincin sekeliling bak
        pz.Emit(tRoads, "Alun2", baseSlots, MeshDir + "/Vil_Plaza.asset");

        // ------------------------------------------------------------ air mancur
        var f = new GeoBag();
        const float R = 6.4f;
        // bak utama
        f.Cyl(Sl.DarkStone, PP(0f, 0f, 0f), R + 0.55f, 0.28f, 32, R + 0.45f, true);
        f.Cyl(Sl.Marble, PP(0f, 0.28f, 0f), R + 0.25f, 0.82f, 32, R + 0.2f, false);
        f.Annulus(Sl.Marble, Vector3.zero, R - 0.7f, R + 0.45f, 0f, 360f, 32, 1.14f);        // bibir bak
        f.Cyl(Sl.Gold, PP(0f, 1.14f, 0f), R + 0.47f, 0.06f, 32, R + 0.47f, true);            // garis emas di bibir
        f.Disc(Sl.Marble, Vector3.zero, R - 0.6f, 32, 0.34f);                                // dasar bak
        f.Disc(Sl.Water, Vector3.zero, R - 0.65f, 32, 0.9f);                                 // permukaan air
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            // panel ukir di dinding luar (16 buah, simetris)
            f.Box(Sl.PlasterB, PP(Mathf.Sin(a) * (R + 0.4f), 0.7f, Mathf.Cos(a) * (R + 0.4f)), new Vector3(1.5f, 0.5f, 0.1f), a * Mathf.Rad2Deg);
            f.Ball(Sl.Gold, PP(Mathf.Sin(a) * (R + 0.48f), 0.7f, Mathf.Cos(a) * (R + 0.48f)), 0.1f, 3, 5);
            f.Ball(Sl.Marble, PP(Mathf.Sin(a) * (R + 0.1f), 1.2f, Mathf.Cos(a) * (R + 0.1f)), 0.2f, 3, 6, 0.9f);   // manik di bibir
        }
        // empat tiang lentera di bibir bak (arah diagonal)
        for (int k = 0; k < 4; k++)
        {
            float a = (45f + 90f * k) * Mathf.Deg2Rad;
            Vector3 pc = PP(Mathf.Sin(a) * (R + 0.1f), 1.14f, Mathf.Cos(a) * (R + 0.1f));
            f.Cyl(Sl.Marble, pc, 0.42f, 0.5f, 8, 0.34f, true);
            f.Cyl(Sl.Stone, pc + PP(0f, 0.5f, 0f), 0.2f, 2.1f, 8, 0.17f, true);
            f.Cyl(Sl.Gold, pc + PP(0f, 2.6f, 0f), 0.3f, 0.12f, 8, 0.3f, true);
            f.Ball(Sl.Glow, pc + PP(0f, 2.95f, 0f), 0.3f, 3, 7, 1.2f);
            f.Cone(Sl.Gold, pc + PP(0f, 3.2f, 0f), 0.34f, 0.5f, 8);
        }
        // pulau pualam di tengah + tangga kecil di empat sisi
        f.Cyl(Sl.Marble, PP(0f, 0.3f, 0f), 2.9f, 0.9f, 24, 2.7f, true);
        f.Annulus(Sl.Gold, Vector3.zero, 2.55f, 2.7f, 0f, 360f, 24, 1.21f);
        // tingkat 1: kolom + mangkuk lebar
        f.Cyl(Sl.Marble, PP(0f, 1.2f, 0f), 0.85f, 1.3f, 16, 0.55f, true);
        f.Cyl(Sl.Marble, PP(0f, 2.5f, 0f), 0.62f, 0.55f, 16, 2.45f, true);                  // mangkuk melebar ke atas
        f.Annulus(Sl.Gold, Vector3.zero, 2.3f, 2.45f, 0f, 360f, 24, 3.05f);
        f.Disc(Sl.Water, Vector3.zero, 2.3f, 24, 2.98f);
        // tingkat 2: kolom + mangkuk sedang
        f.Cyl(Sl.Marble, PP(0f, 3.05f, 0f), 0.4f, 1.15f, 12, 0.3f, true);
        f.Cyl(Sl.Marble, PP(0f, 4.15f, 0f), 0.34f, 0.42f, 14, 1.35f, true);
        f.Annulus(Sl.Gold, Vector3.zero, 1.22f, 1.35f, 0f, 360f, 20, 4.57f);
        f.Disc(Sl.Water, Vector3.zero, 1.22f, 20, 4.5f);
        // pucuk: kolom ramping + cincin emas + kristal
        f.Cyl(Sl.Marble, PP(0f, 4.5f, 0f), 0.22f, 0.85f, 10, 0.12f, true);
        f.Cyl(Sl.Gold, PP(0f, 5.0f, 0f), 0.34f, 0.08f, 12, 0.34f, true);
        f.Ball(Sl.Glow, PP(0f, 5.55f, 0f), 0.34f, 3, 8, 1.7f);
        // tirai air jatuh dari mangkuk besar ke pulau (lembaran air tipis, bukan batang) + percikan melengkung berupa tetesan kecil
        f.Cyl(Sl.Water, PP(0f, 1.25f, 0f), 2.66f, 1.74f, 32, 2.44f, false, false);
        f.Cyl(Sl.Water, PP(0f, 3.0f, 0f), 1.55f, 1.55f, 24, 1.32f, false, false);               // tirai dari mangkuk kecil ke mangkuk besar
        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            Vector3 s = PP(Mathf.Sin(a) * (R - 0.55f), 1.3f, Mathf.Cos(a) * (R - 0.55f));
            Vector3 e = PP(Mathf.Sin(a) * 3.3f, 1.0f, Mathf.Cos(a) * 3.3f);
            for (int t = 0; t <= 9; t++)
            {
                float u = t / 9f;
                Vector3 p = Vector3.Lerp(s, e, u); p.y += 1.1f * 4f * u * (1f - u);
                f.Ball(Sl.Water, p, 0.075f * (1f - 0.35f * u), 2, 5);
            }
            f.Ball(Sl.Marble, PP(Mathf.Sin(a) * (R - 0.2f), 1.25f, Mathf.Cos(a) * (R - 0.2f)), 0.16f, 3, 5);      // mulut pancuran di bibir bak
        }
        // pancuran vertikal di tengah (semburan naik dari pucuk)
        for (int t = 0; t < 12; t++) f.Ball(Sl.Water, PP(0f, 5.95f + t * 0.12f, 0f), 0.09f * (1f - t * 0.05f), 2, 5);
        // teratai & daun di air (8 kembar)
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * Mathf.PI * 2f / 8f;
            Vector3 lp = PP(Mathf.Sin(a) * 4.9f, 0.9f, Mathf.Cos(a) * 4.9f);
            f.Disc(Sl.Leaf, new Vector3(lp.x, 0f, lp.z), 0.55f, 9, 0.93f);
            f.Ball(Sl.Blossom, lp + PP(0f, 0.12f, 0f), 0.17f, 3, 6, 0.7f);
        }
        f.Emit(tProps, "AirMancur", baseSlots, MeshDir + "/Vil_Fountain.asset");
        // collider: segi-delapan (dua kotak, satu diputar 45 derajat) setinggi bibir bak
        var fc = new GameObject("AirMancurCol").transform;
        fc.SetParent(tProps, false);
        foreach (float rot in new[] { 0f, 45f })
        {
            var cg = new GameObject("Col").transform; cg.SetParent(fc, false);
            cg.localRotation = Quaternion.Euler(0f, rot, 0f);
            var bc = cg.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.8f, 0f); bc.size = new Vector3(2f * (R + 0.4f) * 0.924f, 1.6f, 2f * (R + 0.4f) * 0.924f);
        }
        Reserve(0f, 0f, 17f);
        VMapMark("Alun-alun & Kristal", Vector2.zero, 6);

        // ------------------------------------------------------------ bangku (8), sakura (8) simetris
        var bench = PropKit.Get("V_Bench", PropKit.Bench);
        for (int i = 0; i < 8; i++)
        {
            float az = i * 45f + 22.5f;
            Vector3 p = VPol(az, 10.2f);
            var bo = PropKit.Place(bench, tProps, "Bangku", p, az + 180f, 1f, baseSlots);
            BoxCol(bo.transform, "BangkuCol", new Vector3(0f, 0.5f, 0f), new Vector3(1.9f, 1f, 0.6f), Vector3.zero);
            Vector3 tp = VPol(az, 13.8f);
            var sak = PropKit.Place(PropKit.TreeProto(2), tProps, "SakuraAlun", tp, i * 45f, 1.05f, baseSlots);
            var sc = sak.AddComponent<CapsuleCollider>();
            sc.center = new Vector3(0f, 1.5f, 0f); sc.radius = 0.6f; sc.height = 3f;
        }
    }
}
