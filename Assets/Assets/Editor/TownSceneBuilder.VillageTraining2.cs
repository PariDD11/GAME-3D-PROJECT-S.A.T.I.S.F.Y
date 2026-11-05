using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Pemanis + fungsi Kompleks Pelatihan: spanduk, lentera, bangku, jerami, tong air, papan jarak, dan
/// POS LATIHAN TERUKUR (TrainingStation) di tiap lapangan lengkap dengan papan skor 3D.
/// </summary>
public static partial class TownSceneBuilder
{
    // yaw agar TextMesh / papan (sisi baca = -Z lokal) menghadap arah f
    static float VFaceYaw(Vector3 f) { return Mathf.Atan2(-f.x, -f.z) * Mathf.Rad2Deg; }

    static void VBannerPole(GeoBag b, Vector3 p, float h, int cloth, float yaw)
    {
        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
        b.Cyl(Sl.Wood, p, 0.09f, h, 6, 0.06f, true, false);
        b.Box(Sl.Wood, p + q * new Vector3(0.5f, h - 0.2f, 0f), new Vector3(1.05f, 0.07f, 0.07f), yaw);
        b.Box(cloth, p + q * new Vector3(0.5f, h - 1.15f, 0f), new Vector3(0.85f, 1.7f, 0.04f), yaw);
        b.Box(Sl.Gold, p + q * new Vector3(0.5f, h - 1.9f, 0f), new Vector3(0.85f, 0.1f, 0.06f), yaw);
        b.Ball(Sl.Gold, p + new Vector3(0f, h + 0.08f, 0f), 0.12f, 2, 4);
    }

    static void VLanternPost(GeoBag b, Vector3 p)
    {
        b.Cyl(Sl.Wood, p, 0.08f, 2.5f, 6, 0.06f, true, false);
        b.Box(Sl.Wood, p + new Vector3(0f, 2.55f, 0f), new Vector3(0.34f, 0.06f, 0.34f));
        b.Ball(Sl.Glow, p + new Vector3(0f, 2.28f, 0f), 0.17f, 2, 4, 1.25f);
    }

    static void VBench(GeoBag b, Vector3 p, float yaw)
    {
        Quaternion q = Quaternion.Euler(0f, yaw, 0f);
        b.Box(Sl.WoodLight, p + new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 0.1f, 0.5f), yaw);
        b.Box(Sl.Wood, p + q * new Vector3(-0.75f, 0.25f, 0f), new Vector3(0.1f, 0.5f, 0.45f), yaw);
        b.Box(Sl.Wood, p + q * new Vector3(0.75f, 0.25f, 0f), new Vector3(0.1f, 0.5f, 0.45f), yaw);
    }

    static void VHay(GeoBag b, Vector3 p, float yaw)
    {
        b.Box(Sl.Thatch, p + new Vector3(0f, 0.3f, 0f), new Vector3(1.1f, 0.6f, 0.6f), yaw);
    }

    static void VBarrel(GeoBag b, Vector3 p)
    {
        b.Cyl(Sl.Wood, p, 0.38f, 0.95f, 8, 0.34f, true, false);
        b.Cyl(Sl.Metal, p + new Vector3(0f, 0.25f, 0f), 0.395f, 0.06f, 8, 0.395f, false, false);
        b.Cyl(Sl.Metal, p + new Vector3(0f, 0.65f, 0f), 0.375f, 0.06f, 8, 0.375f, false, false);
    }

    /// <summary>Pos latihan + papan skor 3D. face = arah tempat pemain berdiri/membaca papan (dari papan).</summary>
    static TrainingStation VStation(Transform g, GeoBag b, Vector3 pos, Vector3 face, string kind, string label)
    {
        face.y = 0f; face.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, face).normalized;       // sumbu lebar papan
        Vector3 bp = pos - face * 1.7f;                                    // papan di belakang pos (dari sisi pembaca)
        float yaw = VFaceYaw(face);
        b.Cyl(Sl.Wood, bp - right * 1.35f, 0.12f, 3.2f, 6, 0.1f, true, false);
        b.Cyl(Sl.Wood, bp + right * 1.35f, 0.12f, 3.2f, 6, 0.1f, true, false);
        b.Box(Sl.WoodLight, bp + new Vector3(0f, 2.25f, 0f), new Vector3(2.9f, 1.5f, 0.12f), yaw);
        b.Box(Sl.Gold, bp + new Vector3(0f, 3.05f, 0f) + face * 0.02f, new Vector3(2.9f, 0.08f, 0.14f), yaw);
        b.Box(Sl.Roof, bp + new Vector3(0f, 3.35f, 0f), new Vector3(3.3f, 0.12f, 0.9f), yaw);
        BoxCol(g, "PapanSkorCol", bp + Vector3.up * 1.6f, new Vector3(3.0f, 3.2f, 0.4f), new Vector3(0f, yaw, 0f));
        // meja kecil + tong air + peti perban (pemulihan gratis lewat menu pos)
        VBarrel(b, pos + right * 1.7f + face * 0.3f);
        b.Box(Sl.Wood, pos - right * 1.7f + face * 0.3f + new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.6f, 0.55f), yaw);
        b.Box(Sl.Cloth, pos - right * 1.7f + face * 0.3f + new Vector3(0f, 0.63f, 0f), new Vector3(0.82f, 0.08f, 0.57f), yaw);

        var st = VPoint<TrainingStation>(g, "Pos_Latihan_" + kind, pos, "Latihan Terukur: " + label, 3.6f);
        st.kind = kind; st.title = "Pos Latihan";
        var tg = BuildKit.Text(st.transform, "PAPAN SKOR\n" + label, (bp + new Vector3(0f, 2.3f, 0f) + face * 0.09f) - pos, yaw, 0.036f, new Color(1f, 0.92f, 0.6f));
        tg.name = "Skor";
        return st;
    }

    static Vector3 VRing(Vector3 c, float deg, float r) { float a = deg * Mathf.Deg2Rad; return c + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * r; }

    static void VTrainingExtras(Transform g)
    {
        var b = new GeoBag();
        const float deskZ = -57.8f;            // barisan pos latihan sejajar di depan kompleks (sisi jalan), menghadap jalan
        Vector3 toRoad = new Vector3(0f, 0f, 1f);
        // ---------- lapangan pedang ----------
        Vector3 c = new Vector3(-44f, 0f, -73f);
        b.Annulus(Sl.Gold, c, 2.45f, 2.6f, 0f, 360f, 32, 0.07f);
        b.Box(Sl.Stone, c + new Vector3(0f, 0.06f, 0f), new Vector3(5.0f, 0.03f, 0.12f));
        b.Box(Sl.Stone, c + new Vector3(0f, 0.06f, 0f), new Vector3(0.12f, 0.03f, 5.0f));
        // dua spanduk simetris di sisi belakang, dua lentera mengapit pintu masuk (celah timur laut)
        foreach (float ba in new[] { 135f, 225f }) { Vector3 p = VRing(c, ba, 9.4f); VBannerPole(b, p, 3.8f, Sl.Cloth, VFaceYaw(c - p) + 90f); }
        VLanternPost(b, VRing(c, 4f, 9.9f)); VLanternPost(b, VRing(c, 56f, 9.9f));
        VBench(b, VRing(c, 180f, 10.2f), 180f);
        VStation(g, b, new Vector3(c.x, 0f, deskZ), toRoad, "sword", "Pedang");

        // ---------- lapangan memanah ----------
        float cx = -24f, zS = -61f;
        VBannerPole(b, new Vector3(cx - 5.2f, 0f, zS + 0.8f), 3.8f, Sl.Cloth, 0f);
        VBannerPole(b, new Vector3(cx + 5.2f, 0f, zS + 0.8f), 3.8f, Sl.Cloth, 180f);
        VLanternPost(b, new Vector3(cx - 5.4f, 0f, zS - 3.2f));
        VLanternPost(b, new Vector3(cx + 5.4f, 0f, zS - 3.2f));
        float[] dz = { -67f, -73f, -79f };
        string[] dl = { "6", "12", "18" };
        for (int i = 0; i < dz.Length; i++)
        {
            Vector3 sp = new Vector3(cx - 5.1f, 0f, dz[i] + 1.4f);
            b.Cyl(Sl.Wood, sp, 0.07f, 1.2f, 6, 0.06f, true, false);
            b.Box(Sl.WoodLight, sp + new Vector3(0f, 1.35f, 0f), new Vector3(0.7f, 0.5f, 0.06f), 180f);
            BuildKit.Text(g, dl[i], sp + new Vector3(0f, 1.35f, 0.05f), 180f, 0.05f, new Color(1f, 0.92f, 0.6f));
        }
        VStation(g, b, new Vector3(cx, 0f, deskZ), toRoad, "archery", "Memanah");

        // ---------- area sihir ----------
        Vector3 m = new Vector3(-6f, 0f, -72f);
        for (int i = 0; i < 3; i++)
        {
            float a = (i * 120f + 20f) * Mathf.Deg2Rad;
            Vector3 p = m + new Vector3(Mathf.Sin(a) * 2.3f, 2.7f + 0.25f * i, Mathf.Cos(a) * 2.3f);
            b.Ball(Sl.Glow, p, 0.2f, 2, 4, 1.3f);                                  // batu rune melayang
        }
        VLanternPost(b, VRing(m, 0f, 10.2f)); VLanternPost(b, VRing(m, 180f, 10.2f));
        VStation(g, b, new Vector3(m.x, 0f, deskZ), toRoad, "magic", "Sihir");

        b.Emit(g, "DekorPelatihan", baseSlots);
    }
}
