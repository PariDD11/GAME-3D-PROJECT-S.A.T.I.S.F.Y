using UnityEngine;

/// <summary>
/// Dua belas sapu dan jejak dua belas orang di sekeliling Kuil Kaca (kanon §8.4, usulan):
///  - sapu panjang bersandar pada tonggak batu setinggi pinggang, melingkari kuil; SEMUANYA condong ke dalam, ke arah kuil, tak ada yang
///    bersandar ke dinding;
///  - jejak kaki berputar mengitari dinding (tiap orang dengan langkah dan ukuran telapak sendiri), tetapi tak ada satu pun yang naik ke
///    tangga atau masuk; lantai di dalam tetap bersih.
/// Semua dalam koordinat dunia, satu mesh untuk sapu dan satu untuk jejak; tanpa collider (tidak menghalangi jalan).
/// </summary>
public static partial class HuntSceneBuilder
{
    const float DcBroomR = 16.35f;        // jarak pangkal sapu dari pusat kuil
    const float DcPostR = 15.4f;          // jarak tonggak sandaran (lebih dekat ke kuil daripada pangkal sapu)

    static void DcRing(Transform root)
    {
        var g = new GameObject("SapuDanJejak").transform;
        g.SetParent(root, false);
        DcBrooms(g);
        DcPrints(g);
        DcInfo(g, "InteractPoint_SapuDanJejak", DcPolar(14.9f, 345f), "Periksa sapu dan jejak kaki", 4.2f, "Dua Belas Sapu",
            "Dua belas sapu panjang bersandar pada tonggak batu, melingkari kuil. Gagangnya licin oleh telapak tangan yang sama selama berabad-abad. Semuanya condong ke dalam, ke arah kuil; tak satu pun bersandar ke dinding.\n\n" +
            "Di pasir antara sapu dan dinding, jejak kaki dua belas orang berputar mengitari kuil, berulang-ulang, tiap pasang dengan langkah dan ukuran sendiri. Tak ada satu jejak pun yang naik ke tangga dan masuk. Namun lantai di dalam bersih sempurna.");
    }

    // ------------------------------------------------------------------ sapu
    static void DcBrooms(Transform parent)
    {
        var b = new GeoBag();
        const float postH = 1.3f;
        for (int k = 0; k < 12; k++)
        {
            float ang = 15f + 30f * k + (DcHash(k, 3) - 0.5f) * 3f;                // 15, 45, ... 345 derajat dari pintu: tak ada yang tepat di depan tangga
            Vector3 pb = DcPolar(DcBroomR, ang), pp = DcPolar(DcPostR, ang);
            Vector3 inward = pp - pb; inward.y = 0f; inward.Normalize();
            float yaw = DcYawOf(inward);

            // tonggak batu: alas terkubur, tutup tipis di atasnya
            b.BoxB(Sl.Stone, new Vector3(pp.x, pp.y - 0.25f, pp.z), new Vector3(0.30f, postH + 0.25f, 0.30f), yaw);
            b.Box(Sl.Marble, new Vector3(pp.x, pp.y + postH + 0.04f, pp.z), new Vector3(0.40f, 0.08f, 0.40f), yaw);
            ForestKit.Blob(b, ForestKit.Rock, new Vector3(pb.x, pb.y + 0.04f, pb.z), new Vector3(0.42f, 0.2f, 0.36f), k + 40, 0.3f, 3, 8, 0.5f);

            // gagang menyentuh tepi luar tutup tonggak lalu terus ke atas dan ke dalam
            Vector3 contact = new Vector3(pp.x - inward.x * 0.2f, pp.y + postH + 0.12f, pp.z - inward.z * 0.2f);
            Vector3 d = (contact - pb).normalized;
            b.Beam(Sl.Wood, pb - d * 0.3f, pb + d * 3.45f, 0.075f, 0.075f);
            Vector3 top = pb + d * 3.45f;
            // kepala sapu: seikat lidi membuka ke atas, diikat kain
            b.PushTRS(top - d * 0.05f, Quaternion.FromToRotation(Vector3.up, d), 1f);
            b.Cyl(Sl.Thatch, Vector3.zero, 0.035f, 0.78f, 8, 0.28f, true, false);
            b.Cyl(Sl.Cloth, new Vector3(0f, 0.07f, 0f), 0.08f, 0.1f, 8, 0.08f, true, false);
            b.Pop();
        }
        b.Emit(parent, "Sapu_12", ForestKit.DSlots);
    }

    // ------------------------------------------------------------------ jejak kaki
    static void DcPrints(Transform parent)
    {
        var b = new GeoBag();
        const int rim = 10;
        for (int w = 0; w < 12; w++)
        {
            float lane = 13.75f + 0.3f * (w % 4);
            float stride = 0.60f + 0.14f * DcHash(w, 11);                          // tiap orang berlangkah lain
            float len = 0.23f + 0.10f * DcHash(w, 13);
            float wid = len * (0.40f + 0.06f * DcHash(w, 17));
            float a0 = 30f * w + 9f * DcHash(w, 19);
            float span = 150f + 20f * DcHash(w, 29);
            int count = Mathf.FloorToInt(span * Mathf.Deg2Rad * lane / stride);
            for (int j = 0; j < count; j++)
            {
                float ang = a0 + j * stride / lane * Mathf.Rad2Deg;                // berjalan searah jarum jam mengitari kuil
                float side = (j % 2 == 0) ? -1f : 1f;
                float r = lane + side * 0.16f + (DcHash(w * 131 + j, 23) - 0.5f) * 0.08f;
                float a = ang * Mathf.Deg2Rad;
                float lx = Mathf.Sin(a) * r, lz = Mathf.Cos(a) * r;
                if (lz > 10.5f && Mathf.Abs(lx) < 3.3f) continue;                  // tangga dan alas pintu tetap bersih: tak ada jejak naik
                Vector3 fwd = dcRot * Quaternion.Euler(0f, (DcHash(w * 131 + j, 31) - 0.5f) * 16f, 0f) * new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
                Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
                Vector3 c = dcC + dcRot * new Vector3(lx, 0f, lz);
                var rimP = new Vector3[rim];
                for (int i = 0; i < rim; i++)
                {
                    float t = i / (float)rim * Mathf.PI * 2f;
                    float px = wid * 0.5f * Mathf.Cos(t) * (0.8f + 0.2f * Mathf.Sin(t)), pz = len * 0.5f * Mathf.Sin(t);
                    Vector3 p = c + right * px + fwd * pz;
                    p.y = GroundYW(p.x, p.z) + 0.06f;
                    rimP[i] = p;
                }
                Vector3 ctr = c; ctr.y = GroundYW(c.x, c.z) + 0.06f;
                for (int i = 0; i < rim; i++) DcT3(b, Sl.Dirt, ctr, rimP[i], rimP[(i + 1) % rim], Vector3.up);
            }
        }
        var slots = (Material[])ForestKit.DSlots.Clone();
        slots[Sl.Dirt] = DcMat("GurunJejakKaki", new Color(0.50f, 0.40f, 0.28f, 1f), 0.04f);
        b.Emit(parent, "JejakKaki_12", slots);
    }
}
