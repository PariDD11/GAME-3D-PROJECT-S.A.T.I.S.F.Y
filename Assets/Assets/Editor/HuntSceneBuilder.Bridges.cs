using System.Collections.Generic;
using UnityEngine;

/// <summary>Jembatan kayu di Hutan Lerai: di setiap tempat jalur setapak memotong air (sungai, anak sungai, danau) yang lebih panjang dari beberapa meter.</summary>
public static partial class HuntSceneBuilder
{
    static void BuildBridges()
    {
        var root = Group("Bridges");
        int made = 0;
        foreach (var tr in trails)
        {
            var cum = new List<float> { 0f };
            for (int i = 1; i < tr.Length; i++) cum.Add(cum[i - 1] + (tr[i] - tr[i - 1]).magnitude);
            float total = cum[cum.Count - 1];
            System.Func<float, Vector3> at = d =>
            {
                d = Mathf.Clamp(d, 0f, total);
                int i = 1;
                while (i < cum.Count - 1 && cum[i] < d) i++;
                float t = (d - cum[i - 1]) / Mathf.Max(0.0001f, cum[i] - cum[i - 1]);
                Vector2 q = Vector2.Lerp(tr[i - 1], tr[i], t);
                return new Vector3(M * q.x, 0f, M * q.y);
            };
            float dd = 0f;
            while (dd < total)
            {
                Vector3 p = at(dd);
                if (GroundYW(p.x, p.z) >= WaterY - 0.1f) { dd += 1f; continue; }
                float s = dd;
                while (dd < total && GroundYW(at(dd).x, at(dd).z) < WaterY - 0.1f) dd += 1f;
                float e = dd;
                if (e - s < 2.5f) continue;                       // genangan kecil: tidak perlu jembatan
                float a = Mathf.Max(0f, s - 5f), b = Mathf.Min(total, e + 5f);
                Vector3 A = at(a), B = at(b), mid = at((s + e) / 2f);
                A.y = GroundYW(A.x, A.z) + 0.3f;
                B.y = GroundYW(B.x, B.z) + 0.3f;
                float floorY = Mathf.Min(GroundYW(mid.x, mid.z), WaterY) - 0.6f;
                made++;
                BridgeKit.Place(root, "Jembatan_" + made, A, B, 4.2f, floorY, ForestKit.Slots, MeshDir + "/Bridge_Lerai_" + made + ".asset", (x, z) => GroundYW(x, z));
                dd = b + 1f;
            }
        }
        Debug.Log("Jembatan Hutan Lerai: " + made);
    }
}
