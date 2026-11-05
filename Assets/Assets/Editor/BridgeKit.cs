using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jembatan kayu prosedural: lantai papan melintang, gelagar, pagar, dan tiang penyangga sampai dasar sungai.
/// Geometri dibangun di koordinat dunia (A dan B = titik permukaan lantai di kedua ujung; boleh miring mengikuti tebing).
/// </summary>
public static class BridgeKit
{
    public static void Build(GeoBag b, Vector3 A, Vector3 B, float width, float floorY, System.Func<float, float, float> groundAt = null)
    {
        Vector3 d = B - A;
        float len = d.magnitude;
        if (len < 1f) return;
        Vector3 dir = d / len;
        Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, flat).normalized;
        float yaw = Mathf.Atan2(-flat.z, flat.x) * Mathf.Rad2Deg;       // sumbu X lokal sejajar 'side' setelah ditambah 90 derajat
        float planksYaw = Mathf.Atan2(side.x, side.z) * Mathf.Rad2Deg;  // untuk Box: kotak lebar searah side

        // papan melintang
        int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.66f));
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            Vector3 p = Vector3.Lerp(A, B, t);
            b.Box((i % 2 == 0) ? Sl.WoodLight : Sl.Wood, p + Vector3.down * 0.08f, new Vector3(width, 0.14f, 0.62f), Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg + 0f);
        }
        // gelagar bawah di kiri-kanan dan tengah
        for (int s = -1; s <= 1; s++)
            b.Beam(Sl.Wood, A + side * (s * (width / 2f - 0.2f)) + Vector3.down * 0.3f, B + side * (s * (width / 2f - 0.2f)) + Vector3.down * 0.3f, 0.26f, 0.3f);
        // pagar: tiang tiap ~2 m, dua palang
        int posts = Mathf.Max(2, Mathf.RoundToInt(len / 2.0f));
        for (int sgn = -1; sgn <= 1; sgn += 2)
        {
            Vector3 off = side * (sgn * (width / 2f - 0.08f));
            for (int i = 0; i <= posts; i++)
            {
                Vector3 p = Vector3.Lerp(A, B, i / (float)posts) + off;
                b.Cyl(Sl.Wood, p, 0.1f, 1.25f, 6, 0.09f, true, false);
            }
            b.Beam(Sl.WoodLight, A + off + Vector3.up * 1.05f, B + off + Vector3.up * 1.05f, 0.1f, 0.12f);
            b.Beam(Sl.WoodLight, A + off + Vector3.up * 0.55f, B + off + Vector3.up * 0.55f, 0.08f, 0.1f);
        }
        // hiasan pagar: bola penutup tiang, tali melengkung di antara tiang, papan bernomor paku
        for (int sgn = -1; sgn <= 1; sgn += 2)
        {
            Vector3 off = side * (sgn * (width / 2f - 0.08f));
            for (int i = 0; i <= posts; i++)
            {
                Vector3 p = Vector3.Lerp(A, B, i / (float)posts) + off;
                b.Ball(Sl.Gold, p + Vector3.up * 1.3f, 0.1f, 3, 6);
                if (i < posts)
                {
                    Vector3 q = Vector3.Lerp(A, B, (i + 1) / (float)posts) + off;
                    Vector3 prevR = p + Vector3.up * 0.8f;
                    for (int k = 1; k <= 4; k++)
                    {
                        float t = k / 4f;
                        Vector3 pr = Vector3.Lerp(p, q, t) + Vector3.up * (0.8f - 0.22f * 4f * t * (1f - t));
                        b.Beam(Sl.Cloth, prevR, pr, 0.05f, 0.05f);
                        prevR = pr;
                    }
                    if (i % 2 == 0) AnimeKit.FlowerBush(b, Vector3.Lerp(p, q, 0.5f) + side * (sgn * 0.35f) + Vector3.down * 0.05f, 0.28f, i % 4 == 0 ? Sl.Blossom : Sl.Gold);
                }
            }
        }
        // lengkungan-X penguat di bawah lantai
        for (int i = 0; i < posts; i += 2)
        {
            Vector3 p0 = Vector3.Lerp(A, B, i / (float)posts), p1 = Vector3.Lerp(A, B, Mathf.Min(posts, i + 2) / (float)posts);
            b.Beam(Sl.WoodLight, p0 - side * (width / 2f - 0.3f) + Vector3.down * 0.35f, p1 + side * (width / 2f - 0.3f) + Vector3.down * 0.9f, 0.12f, 0.12f);
            b.Beam(Sl.WoodLight, p0 + side * (width / 2f - 0.3f) + Vector3.down * 0.35f, p1 - side * (width / 2f - 0.3f) + Vector3.down * 0.9f, 0.12f, 0.12f);
        }
        // pangkal batu di kedua ujung dengan lantai tangga
        foreach (var e2 in new[] { A, B })
        {
            float sgnF = (e2 == A) ? -1f : 1f;
            Vector3 bp = e2 + flat * (sgnF * 0.3f);
            b.Box(Sl.Stone, bp + Vector3.down * 0.35f, new Vector3(width + 0.9f, 0.7f, 1.2f), Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg);
            b.Box(Sl.DarkStone, bp + Vector3.down * 0.05f, new Vector3(width + 1.1f, 0.1f, 1.4f), Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg);
        }
        // tiang penyangga ke dasar, tiap ~6 m (tiap tiang menjulur sampai tanah di bawahnya sendiri, tidak melayang)
        int piers = Mathf.Max(1, Mathf.RoundToInt(len / (groundAt != null ? 4.5f : 6f)));
        for (int i = 0; i <= piers; i++)
        {
            Vector3 p = Vector3.Lerp(A, B, i / (float)piers);
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Vector3 q = p + side * (sgn * (width / 2f - 0.35f));
                float top = q.y - 0.25f;
                float fy = groundAt != null ? groundAt(q.x, q.z) - 0.5f : floorY;
                if (top - fy < 0.3f) continue;
                b.Cyl(Sl.Wood, new Vector3(q.x, fy, q.z), 0.24f, top - fy, 7, 0.22f, true, false);
            }
            if (i > 0 && i < piers)
                b.Beam(Sl.Wood, Vector3.Lerp(A, B, i / (float)piers) + side * (-(width / 2f - 0.35f)) + Vector3.down * 0.45f,
                    Vector3.Lerp(A, B, i / (float)piers) + side * (width / 2f - 0.35f) + Vector3.down * 0.45f, 0.2f, 0.2f);
        }
        // tiang pagar: tiap tiang turun sampai tanah bila deck jauh di atasnya
        if (groundAt != null)
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Vector3 off = side * (sgn * (width / 2f - 0.08f));
                for (int i = 0; i <= posts; i++)
                {
                    Vector3 p = Vector3.Lerp(A, B, i / (float)posts) + off;
                    float gy = groundAt(p.x, p.z) - 0.4f;
                    if (p.y - gy > 0.5f && (i == 0 || i == posts)) b.Cyl(Sl.Wood, new Vector3(p.x, gy, p.z), 0.16f, p.y - gy, 6, 0.14f, true, false);
                }
            }
        // gerbang kecil di tiap ujung: dua tiang tinggi + lentera
        foreach (var end in new[] { A, B })
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                Vector3 p = end + side * (sgn * (width / 2f + 0.15f));
                {
                    float gy = groundAt != null ? Mathf.Min(groundAt(p.x, p.z) - 0.4f, end.y - 0.4f) : end.y - 0.4f;
                    b.Cyl(Sl.Wood, new Vector3(p.x, gy, p.z), 0.15f, (end.y + 1.9f) - gy, 6, 0.13f, true, false);
                }
                BuildKit.Lantern(b, p + Vector3.up * 2.05f);
            }
    }

    /// <summary>Tempatkan jembatan + collider lantai (kotak miring) di bawah parent.</summary>
    public static GameObject Place(Transform parent, string name, Vector3 A, Vector3 B, float width, float floorY, Material[] slots, string meshAsset, System.Func<float, float, float> groundAt = null)
    {
        var bag = new GeoBag();
        Build(bag, A, B, width, floorY, groundAt);
        var root = bag.Emit(parent, name, slots, meshAsset);

        Vector3 d = B - A;
        Vector3 mid = (A + B) / 2f + Vector3.down * 0.12f;
        float horiz = new Vector3(d.x, 0f, d.z).magnitude;
        float pitch = -Mathf.Atan2(d.y, horiz) * Mathf.Rad2Deg;
        float yawDeg = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        var col = new GameObject(name + "_Lantai");
        col.transform.SetParent(parent, false);
        col.transform.position = mid;
        col.transform.rotation = Quaternion.Euler(pitch, yawDeg, 0f);
        col.AddComponent<BoxCollider>().size = new Vector3(width, 0.3f, d.magnitude + 1.2f);
        // pagar sebagai penghalang tipis
        for (int sgn = -1; sgn <= 1; sgn += 2)
        {
            var rail = new GameObject(name + "_Pagar");
            rail.transform.SetParent(parent, false);
            Vector3 side = Vector3.Cross(Vector3.up, new Vector3(d.x, 0f, d.z).normalized).normalized;
            rail.transform.position = mid + side * (sgn * (width / 2f - 0.05f)) + Vector3.up * 0.6f;
            rail.transform.rotation = Quaternion.Euler(pitch, yawDeg, 0f);
            rail.AddComponent<BoxCollider>().size = new Vector3(0.14f, 1.3f, d.magnitude);
        }
        return root;
    }
}
