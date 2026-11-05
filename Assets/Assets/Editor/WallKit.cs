using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiga tembok kota konsentris. Tier 0 = Tembok Luar (r 330, mengelilingi sektor pertanian), tier 1 = Tembok Dalam (r 215, kota & perekonomian),
/// tier 2 = Tembok Tengah (r 100, kediaman bangsawan). Tiap cincin punya 4 gerbang (utara, timur, selatan, barat).
/// Azimut a: 0 = utara (+Z), 90 = timur (+X), 180 = selatan (-Z), 270 = barat (-X).
/// </summary>
public static class WallKit
{
    public static readonly float[] R = { 330f, 215f, 100f };
    public static readonly float[] H = { 14f, 18f, 21f };
    public static readonly float[] Thick = { 5.5f, 6.5f, 6.5f };
    public static readonly float[] TowerGap = { 36f, 34f, 30f };
    public static readonly float[] TowerR = { 4.6f, 4.8f, 4.6f };
    public static readonly float[] GateAz = { 0f, 90f, 180f, 270f };
    public static readonly string[] DirName = { "Utara", "Timur", "Selatan", "Barat" };

    public static Vector2 Out(float azDeg)
    {
        float a = azDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(a), Mathf.Cos(a));
    }

    /// <summary>Titik di cincin pada azimut tertentu (x,z).</summary>
    public static Vector2 OnRing(int tier, float azDeg) { return Out(azDeg) * R[tier]; }

    /// <summary>Membangun 4 busur tembok di antara 4 gerbang (gerbang sendiri dibangun GateKit).</summary>
    public static void BuildRing(Transform parent, int tier, Material[] mats, int arcMask = 15)
    {
        var spec = GateKit.SpecFor(tier);
        float r = R[tier], h = H[tier], th = Thick[tier];
        float delta = Mathf.Atan2(spec.tX, r) * Mathf.Rad2Deg;
        var ringRoot = new GameObject("Tembok_" + GateKit.TierName[tier]).transform;
        ringRoot.SetParent(parent, false);

        for (int k = 0; k < 4; k++)
        {
            if ((arcMask & (1 << k)) == 0) continue;
            float a0 = GateAz[k], a1 = k == 3 ? 360f : GateAz[k + 1];
            var arc = new GameObject("Busur_" + DirName[k] + "-" + DirName[(k + 1) % 4]).transform;
            arc.SetParent(ringRoot, false);

            // simpul poligon: tengah menara gerbang -> titik di lingkaran -> tengah menara gerbang berikutnya
            var nodes = new List<Vector2>();
            nodes.Add(Out(a0) * r + new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), -Mathf.Sin(a0 * Mathf.Deg2Rad)) * spec.tX);
            float start = a0 + delta, end = a1 - delta;
            float arcLen = (end - start) * Mathf.Deg2Rad * r;
            int segs = Mathf.Max(4, Mathf.RoundToInt(arcLen / 9f));
            for (int i = 1; i < segs; i++) nodes.Add(OnRing(tier, Mathf.Lerp(start, end, i / (float)segs)));
            nodes.Add(Out(a1) * r - new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), -Mathf.Sin(a1 * Mathf.Deg2Rad)) * spec.tX);

            var bag = new GeoBag();
            int accent = tier == 2 ? Sl.Gold : Sl.DarkStone;
            for (int i = 0; i + 1 < nodes.Count; i++)
            {
                Vector2 A = nodes[i], B = nodes[i + 1];
                Vector2 d = B - A;
                float len = d.magnitude;
                if (len < 0.05f) continue;
                Vector2 m = (A + B) / 2f;
                float yaw = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                Vector2 rad = m.normalized;
                Vector3 M3 = new Vector3(m.x, 0f, m.y);

                bag.Box(Sl.Stone, M3 + new Vector3(0f, h / 2f, 0f), new Vector3(th, h, len + 0.5f), yaw);
                bag.Box(Sl.DarkStone, M3 + new Vector3(0f, 1.0f, 0f), new Vector3(th + 1.2f, 2.0f, len + 0.5f), yaw);
                bag.Box(Sl.DarkStone, M3 + new Vector3(0f, h - 3.2f, 0f), new Vector3(th + 0.3f, 0.5f, len + 0.5f), yaw);
                bag.Box(Sl.DarkStone, M3 + new Vector3(0f, h, 0f), new Vector3(th + 1.0f, 0.6f, len + 0.5f), yaw);
                if (tier == 2) bag.Box(accent, M3 + new Vector3(0f, h + 0.34f, 0f), new Vector3(th + 1.04f, 0.14f, len + 0.5f), yaw);

                // pagar rendah sisi dalam
                Vector3 inner = M3 - new Vector3(rad.x, 0f, rad.y) * (th / 2f - 0.1f);
                bag.BoxB(Sl.Stone, inner + new Vector3(0f, h + 0.3f, 0f), new Vector3(0.55f, 0.9f, len + 0.5f), yaw);

                // gerigi (merlon) sisi luar
                Vector3 outer = M3 + new Vector3(rad.x, 0f, rad.y) * (th / 2f + 0.15f);
                int nm = Mathf.Max(1, Mathf.FloorToInt(len / 3.4f));
                Vector3 dir3 = new Vector3(d.x, 0f, d.y).normalized;
                for (int j = 0; j < nm; j++)
                {
                    float t = (j + 0.5f) / nm - 0.5f;
                    bag.BoxB(Sl.Stone, outer + dir3 * (t * len) + new Vector3(0f, h + 0.3f, 0f), new Vector3(0.9f, 1.6f, 1.9f), yaw);
                }

                // penopang di sisi dalam
                if (i % 2 == 0 && i > 0 && i < nodes.Count - 2)
                {
                    Vector3 bp = M3 - new Vector3(rad.x, 0f, rad.y) * (th / 2f + 0.9f);
                    bag.BoxB(Sl.Stone, bp, new Vector3(1.5f, h - 3.5f, 2.0f), yaw);
                    bag.BoxB(Sl.DarkStone, bp + new Vector3(0f, h - 3.5f, 0f), new Vector3(1.8f, 0.4f, 2.3f), yaw);
                }

                FantasyKit.RampartDetail(bag, M3, yaw, th, h, len, new Vector3(rad.x, 0f, rad.y), tier, i + k * 3);

                // collider per segmen
                var col = new GameObject("Col");
                col.transform.SetParent(arc, false);
                col.transform.localPosition = M3 + new Vector3(0f, h / 2f, 0f);
                col.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                col.AddComponent<BoxCollider>().size = new Vector3(th, h, len + 0.4f);
            }

            // menara perantara
            int nt = Mathf.Max(0, Mathf.RoundToInt(arcLen / TowerGap[tier]) - 1);
            for (int i = 1; i <= nt; i++)
            {
                float az = Mathf.Lerp(start, end, i / (float)(nt + 1));
                Vector2 p = OnRing(tier, az) + Out(az) * 0.6f;
                bool alt = (i + k) % 2 == 0;
                BuildKit.RoundTower(bag, new Vector3(p.x, 0f, p.y), TowerR[tier], h + 5.5f, Sl.Stone, Sl.DarkStone, alt ? Sl.Roof : Sl.RoofB, 12f, false, true, Sl.Cloth);
                var tc = new GameObject("ColTower");
                tc.transform.SetParent(arc, false);
                tc.transform.localPosition = new Vector3(p.x, (h + 5.5f) / 2f, p.y);
                var cap = tc.AddComponent<CapsuleCollider>();
                cap.radius = TowerR[tier] + 0.4f;
                cap.height = h + 5.5f;
            }

            bag.Emit(arc, "Mesh", mats);
        }
    }
}
