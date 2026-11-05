using UnityEngine;

/// <summary>
/// Enam patung dewa Kuil Kaca (kanon §8.4; Kronik Bab 12-13), dalam bingkai patung: asal = puncak lantai relung, menghadap +Z (ke pusat kuil),
/// tinggi sekitar 5 m. Tanpa nama di teks permainan; dikenali dari bendanya: api, bumi (menunduk), guci air batu, satu kaki, lilin, busur.
///   1 api (tangan terangkat memegang nyala), 2 bumi (gemuk, menunduk menyentuh tanah), 3 air (guci yang tak berhenti mengalir),
///   4 angin (nakal, berdiri di satu kaki, tangan terentang), 5 lilin (berkerudung, dua tangan menangkup lilin kecil), 6 busur (tersenyum).
/// Bahan: badan Sl.Marble, alas Sl.Stone, nyala/air batu Sl.Glow (warna cahaya relungnya).
/// </summary>
public static partial class HuntSceneBuilder
{
    static int dcLimbSeed;

    /// <summary>Tabung bulat meruncing dari a ke c (r0 di a, r1 di c).</summary>
    static void DcLimb(GeoBag b, int slot, Vector3 a, Vector3 c, float r0, float r1)
    {
        ForestKit.Limb(b, slot, a, c, r0, r1, ++dcLimbSeed, 8, 0f);
    }

    /// <summary>Lengan dua ruas + tangan bulat.</summary>
    static void DcArm(GeoBag b, Vector3 sh, Vector3 el, Vector3 hd)
    {
        DcLimb(b, Sl.Marble, sh, el, 0.16f, 0.125f);
        DcLimb(b, Sl.Marble, el, hd, 0.125f, 0.1f);
        b.Ball(Sl.Marble, hd, 0.15f, 4, 8, 1f);
        b.Ball(Sl.Marble, el, 0.15f, 4, 8, 1f);
    }

    /// <summary>Kepala bulat telur + rambut/kerudung; (cx, cy, cz) = pusat kepala.</summary>
    static void DcHead(GeoBag b, Vector3 c, float hoodR)
    {
        b.Cyl(Sl.Marble, c + new Vector3(0f, -0.42f, 0f), 0.17f, 0.24f, 10, 0.15f, false, false);
        b.Ball(Sl.Marble, c, 0.31f, 6, 12, 1.18f);
        b.Ball(Sl.Stone, c + new Vector3(0f, 0.04f, -0.08f), hoodR, 6, 12, 1.1f);
        b.Cone(Sl.Marble, c + new Vector3(0f, -0.06f, 0.26f), 0.05f, 0.14f, 6);
    }

    static void DcStatue(GeoBag b, int god)
    {
        const float yb = 0.28f;
        b.Box(Sl.Stone, new Vector3(0f, 0.14f, 0f), new Vector3(2.8f, 0.28f, 1.7f));
        switch (god)
        {
            case 1: DcApi(b, yb); break;
            case 2: DcBumi(b, yb); break;
            case 3: DcAir(b, yb); break;
            case 4: DcAngin(b, yb); break;
            case 5: DcLilin(b, yb); break;
            default: DcBusur(b, yb); break;
        }
    }

    /// <summary>Badan berjubah panjang: jubah, dada, bahu. waistR = jari-jari pinggang, hemR = jari-jari ujung jubah.</summary>
    static void DcRobe(GeoBag b, float yb, float hemR, float waistR, float chestTop)
    {
        b.Cyl(Sl.Marble, new Vector3(0f, yb, 0f), hemR, 2.55f, 18, waistR, false, false);
        b.Cyl(Sl.Stone, new Vector3(0f, yb, 0f), hemR + 0.03f, 0.13f, 18, hemR + 0.03f, false, false);
        b.Cyl(Sl.Stone, new Vector3(0f, yb + 2.5f, 0f), waistR + 0.04f, 0.1f, 14, waistR + 0.04f, true, false);
        for (int k = -2; k <= 2; k++)                                           // lipatan jubah di sisi depan
        {
            float a = k * 0.5f + 0.07f * (k & 1), sn = Mathf.Sin(a), cs = Mathf.Cos(a);
            b.Beam(Sl.Marble, new Vector3(sn * (hemR + 0.02f), yb + 0.14f, cs * (hemR + 0.02f)), new Vector3(sn * (waistR + 0.02f), yb + 2.42f, cs * (waistR + 0.02f)), 0.07f, 0.06f);
        }
        b.Cyl(Sl.Marble, new Vector3(0f, yb + 2.55f, 0f), waistR, chestTop - yb - 2.55f, 14, 0.55f, true, false);
        b.Ball(Sl.Marble, new Vector3(0f, chestTop, 0f), 0.56f, 5, 12, 0.42f);
    }

    // ------------------------------------------------------------------ 1. api: wajah tegak, satu tangan terangkat memegang nyala
    static void DcApi(GeoBag b, float yb)
    {
        DcRobe(b, yb, 0.80f, 0.42f, 3.83f);
        DcHead(b, new Vector3(0f, 4.32f, 0.03f), 0.34f);
        DcArm(b, new Vector3(0.58f, 3.62f, 0f), new Vector3(0.9f, 3.85f, 0.35f), new Vector3(0.78f, 4.5f, 0.5f));
        DcArm(b, new Vector3(-0.58f, 3.62f, 0f), new Vector3(-0.85f, 3.0f, 0.15f), new Vector3(-0.72f, 2.4f, 0.32f));
        b.Cone(Sl.Glow, new Vector3(0.78f, 4.58f, 0.5f), 0.21f, 0.85f, 8);
        b.Ball(Sl.Glow, new Vector3(0.78f, 4.7f, 0.5f), 0.16f, 4, 8, 1.2f);
    }

    // ------------------------------------------------------------------ 2. bumi: gemuk, menunduk, satu tangan menyentuh lantai
    static void DcBumi(GeoBag b, float yb)
    {
        // berjongkok dan membungkuk dalam; tangan kanan menyentuh alas, tangan kiri bertumpu di lutut
        b.Cyl(Sl.Marble, new Vector3(0f, yb, 0f), 1.15f, 1.35f, 18, 0.8f, false, false);
        b.Cyl(Sl.Stone, new Vector3(0f, yb, 0f), 1.18f, 0.13f, 18, 1.18f, false, false);
        b.PushTRS(new Vector3(0f, 1.5f, -0.1f), Quaternion.Euler(48f, 0f, 0f), 1f);
        b.Ball(Sl.Marble, new Vector3(0f, 0.7f, 0.0f), 0.98f, 6, 14, 0.85f);
        b.Ball(Sl.Marble, new Vector3(0f, 1.3f, 0f), 0.66f, 5, 12, 0.5f);
        DcHead(b, new Vector3(0f, 1.95f, 0.1f), 0.35f);
        b.Pop();
        DcArm(b, new Vector3(0.85f, 2.27f, 0.75f), new Vector3(1.3f, 1.35f, 0.8f), new Vector3(0.95f, 0.34f, 0.8f));
        DcArm(b, new Vector3(-0.85f, 2.27f, 0.75f), new Vector3(-1.15f, 1.7f, 0.5f), new Vector3(-0.75f, 1.2f, 0.35f));
    }

    // ------------------------------------------------------------------ 3. air: guci terbalik yang tak berhenti mengalirkan air batu ke mangkuk
    static void DcAir(GeoBag b, float yb)
    {
        DcRobe(b, yb, 0.80f, 0.42f, 3.83f);
        DcHead(b, new Vector3(0f, 4.32f, 0.03f), 0.34f);
        DcArm(b, new Vector3(-0.58f, 3.62f, 0f), new Vector3(-0.95f, 3.4f, 0.3f), new Vector3(-0.8f, 3.2f, 0.55f));
        DcArm(b, new Vector3(0.58f, 3.62f, 0f), new Vector3(0.75f, 3.1f, 0.38f), new Vector3(0.2f, 3.25f, 0.5f));
        b.Ball(Sl.Stone, new Vector3(-0.8f, 3.6f, 0.55f), 0.36f, 6, 12, 1.12f);
        b.Cyl(Sl.Stone, new Vector3(-0.8f, 3.02f, 0.55f), 0.15f, 0.25f, 10, 0.11f, false, false);
        b.Beam(Sl.Glow, new Vector3(-0.8f, 3.05f, 0.55f), new Vector3(-0.8f, 0.5f, 0.55f), 0.09f, 0.09f, Vector3.forward);
        b.Cyl(Sl.Stone, new Vector3(-0.8f, yb, 0.55f), 0.46f, 0.22f, 14, 0.52f, false, false);
        b.Disc(Sl.Glow, new Vector3(-0.8f, 0f, 0.55f), 0.43f, 14, yb + 0.2f);
    }

    // ------------------------------------------------------------------ 4. angin: nakal, satu kaki, tangan terentang, rambut berkibar
    static void DcAngin(GeoBag b, float yb)
    {
        b.Cyl(Sl.Marble, new Vector3(-0.12f, yb, 0f), 0.23f, 1.75f, 10, 0.18f, false, false);
        DcLimb(b, Sl.Marble, new Vector3(0.18f, 2.15f, 0.05f), new Vector3(0.62f, 2.55f, 0.75f), 0.2f, 0.15f);
        DcLimb(b, Sl.Marble, new Vector3(0.62f, 2.55f, 0.75f), new Vector3(0.5f, 1.75f, 0.9f), 0.15f, 0.11f);
        b.Ball(Sl.Marble, new Vector3(0.5f, 1.72f, 0.95f), 0.17f, 4, 8, 0.7f);
        b.Cyl(Sl.Marble, new Vector3(0f, 1.95f, 0f), 0.68f, 0.85f, 14, 0.36f, false, false);
        b.Cyl(Sl.Marble, new Vector3(0f, 2.8f, 0f), 0.36f, 0.95f, 12, 0.5f, true, false);
        b.Ball(Sl.Marble, new Vector3(0f, 3.75f, 0f), 0.5f, 5, 10, 0.45f);
        b.PushTRS(new Vector3(0f, 4.25f, 0.03f), Quaternion.Euler(0f, 0f, 9f), 1f);
        DcHead(b, Vector3.zero, 0.33f);
        b.Pop();
        DcArm(b, new Vector3(0.5f, 3.62f, 0f), new Vector3(1.0f, 3.82f, 0.1f), new Vector3(1.5f, 4.02f, 0.15f));
        DcArm(b, new Vector3(-0.5f, 3.62f, 0f), new Vector3(-1.0f, 3.82f, 0.1f), new Vector3(-1.5f, 4.02f, 0.15f));
        for (int k = 0; k < 3; k++)                                              // rambut berkibar ke samping
            DcLimb(b, Sl.Stone, new Vector3(0.05f, 4.45f - k * 0.08f, -0.28f), new Vector3(0.9f + k * 0.2f, 4.3f - k * 0.25f, -0.45f), 0.07f, 0.03f);
    }

    // ------------------------------------------------------------------ 5. lilin: berkerudung, dua tangan menangkup lilin kecil (cahaya paling redup)
    static void DcLilin(GeoBag b, float yb)
    {
        DcRobe(b, yb, 0.78f, 0.4f, 3.8f);
        b.Ball(Sl.Stone, new Vector3(0f, 3.85f, -0.06f), 0.72f, 5, 12, 0.5f);
        DcHead(b, new Vector3(0f, 4.27f, 0.1f), 0.43f);
        DcArm(b, new Vector3(0.55f, 3.55f, 0f), new Vector3(0.62f, 3.2f, 0.4f), new Vector3(0.13f, 3.12f, 0.72f));
        DcArm(b, new Vector3(-0.55f, 3.55f, 0f), new Vector3(-0.62f, 3.2f, 0.4f), new Vector3(-0.13f, 3.12f, 0.72f));
        b.Cyl(Sl.Marble, new Vector3(0f, 3.12f, 0.74f), 0.06f, 0.5f, 8, 0.055f, true, false);
        b.Ball(Sl.Glow, new Vector3(0f, 3.72f, 0.74f), 0.085f, 4, 8, 1.6f);
    }

    // ------------------------------------------------------------------ 6. busur: tersenyum, busur tegak di sisi kiri, tabung anak panah di punggung
    static void DcBusur(GeoBag b, float yb)
    {
        DcRobe(b, yb, 0.74f, 0.42f, 3.83f);
        DcHead(b, new Vector3(0.02f, 4.32f, 0.03f), 0.34f);
        DcArm(b, new Vector3(-0.58f, 3.62f, 0f), new Vector3(-0.9f, 3.2f, 0.45f), new Vector3(-0.95f, 2.75f, 0.9f));
        DcArm(b, new Vector3(0.58f, 3.62f, 0f), new Vector3(0.8f, 3.0f, 0.15f), new Vector3(0.7f, 2.4f, 0.28f));
        Vector3 prev = new Vector3(-0.95f, 1.0f, 0.35f);
        for (int i = 1; i <= 8; i++)
        {
            float t = i / 8f;
            Vector3 p = new Vector3(-0.95f, 1.0f + 3.4f * t, 0.35f + 0.55f * Mathf.Sin(t * Mathf.PI));
            b.Beam(Sl.Wood, prev, p, 0.1f, 0.1f, Vector3.right);
            prev = p;
        }
        b.Beam(Sl.Stone, new Vector3(-0.95f, 1.0f, 0.35f), new Vector3(-0.95f, 4.4f, 0.35f), 0.03f, 0.03f, Vector3.forward);
        b.PushTRS(new Vector3(0.3f, 3.3f, -0.55f), Quaternion.Euler(-18f, 0f, -14f), 1f);
        b.Cyl(Sl.Wood, new Vector3(0f, -0.9f, 0f), 0.2f, 1.9f, 8, 0.2f, true, false);
        b.Cyl(Sl.Stone, new Vector3(0f, 1.0f, 0f), 0.2f, 0.12f, 8, 0.2f, true, false);
        b.Pop();
    }
}
