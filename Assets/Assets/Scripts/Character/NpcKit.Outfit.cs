using System.Collections.Generic;
using UnityEngine;

public static partial class NpcKit
{
    /// <summary>Parameter pakaian yang dipakai bersama oleh badan, lengan, dan kaki.</summary>
    class Def
    {
        public int pantsSlot = -1; public float pantsEnd = 0.12f, pantsLoose = 0f;
        public int sockSlot = -1; public float sockTop = 0.4f;
        public int bootSlot = Sl.Leather; public float bootTop = 0.26f; public bool greaves;
        public float sleeveEnd = -1f; public int sleeveSlot = Sl.CoatA; public bool sleeveBell, puffSleeve; public int cuffSlot = -1;
        public int gloveSlot = -1; public bool vambrace; public int pauldron; public int vambraceSlot = Sl.Metal;
    }

    static Def MakeDef(Ctx c)
    {
        var d = new Def();
        bool f = c.fem;
        d.pantsSlot = Sl.CoatB;
        switch (c.s.outfit)
        {
            case OWarga:
                if (f) { d.pantsSlot = -1; d.sockSlot = Sl.CoatC; d.sockTop = 0.42f; d.sleeveEnd = 1.12f; }
                else { d.sleeveEnd = 0.92f; d.cuffSlot = -1; d.pantsLoose = 0.004f; }
                break;
            case OPedagang:
                d.sleeveEnd = 0.90f; d.cuffSlot = Sl.CoatC; d.sleeveSlot = Sl.CoatB; d.bootTop = 0.40f; d.pantsSlot = f ? -1 : Sl.CoatC;
                if (f) { d.sockSlot = Sl.CoatC; d.sockTop = 0.30f; }
                break;
            case OPetani:
                d.sleeveEnd = 1.10f; d.bootTop = 0.30f; d.pantsLoose = 0.008f; if (f) { d.pantsSlot = -1; d.sockSlot = Sl.CoatC; d.sockTop = 0.40f; }
                break;
            case OBangsawan:
                d.sleeveEnd = 0.90f; d.cuffSlot = Sl.Gold; d.pantsSlot = f ? -1 : Sl.CoatC; d.bootTop = 0.42f; d.puffSleeve = f; d.sleeveBell = !f;
                break;
            case OPenjaga:
                d.sleeveEnd = 0.95f; d.sleeveSlot = Sl.CoatB; d.vambrace = true; d.pauldron = 1; d.pantsSlot = Sl.CoatB; d.bootTop = 0.40f; d.gloveSlot = Sl.Leather;
                break;
            case OKsatria:
                d.sleeveEnd = 0.92f; d.sleeveSlot = Sl.Metal; d.vambrace = true; d.pauldron = 2; d.greaves = true; d.gloveSlot = Sl.Metal; d.pantsSlot = Sl.CoatB; d.bootTop = 0.34f; d.bootSlot = Sl.Metal;
                break;
            case OPendeta:
                d.pantsSlot = -1; d.sleeveEnd = 0.88f; d.sleeveBell = true; d.cuffSlot = Sl.Gold; d.bootTop = 0.16f;
                break;
            case OPenyihir:
                d.pantsSlot = -1; d.sleeveEnd = 0.86f; d.sleeveBell = true; d.cuffSlot = Sl.CoatC; d.bootTop = 0.2f;
                break;
            case OHunter:
                d.sleeveEnd = 1.12f; d.sleeveSlot = Sl.CoatA; d.vambrace = true; d.gloveSlot = Sl.Leather; d.bootTop = 0.46f; d.pantsSlot = Sl.CoatB; d.pauldron = 1;
                break;
            case OPandai:
                d.sleeveEnd = -1f; d.vambrace = true; d.gloveSlot = Sl.Leather; d.bootTop = 0.34f; d.pantsSlot = Sl.CoatB; d.pantsLoose = 0.006f;
                break;
            case OPelayan:
                if (f) { d.pantsSlot = -1; d.sockSlot = Sl.CoatB; d.sockTop = 0.34f; d.sleeveEnd = 1.10f; d.sleeveSlot = Sl.CoatB; d.puffSleeve = true; d.bootTop = 0.16f; d.bootSlot = Sl.Dark; }
                else { d.sleeveEnd = 0.90f; d.sleeveSlot = Sl.CoatA; d.cuffSlot = Sl.CoatB; d.pantsSlot = Sl.CoatA; d.bootTop = 0.16f; d.bootSlot = Sl.Dark; }
                break;
            case OBard:
                d.sleeveEnd = 0.92f; d.sleeveSlot = Sl.CoatB; d.cuffSlot = Sl.CoatC; d.pantsSlot = Sl.CoatC; d.bootTop = 0.48f; d.sleeveBell = true;
                if (f) { d.pantsSlot = -1; d.sockSlot = Sl.CoatC; d.sockTop = 0.35f; }
                break;
            case OKoki:
                d.sleeveEnd = 1.08f; d.pantsSlot = Sl.CoatB; d.bootTop = 0.18f; d.bootSlot = Sl.Dark; if (f) { d.pantsSlot = -1; d.sockSlot = Sl.CoatA; d.sockTop = 0.4f; }
                break;
            case OPelajar:
            case OPetugas:
                d.sleeveEnd = 0.88f; d.cuffSlot = Sl.CoatA; d.sleeveSlot = Sl.CoatA; d.bootTop = 0.18f; d.bootSlot = Sl.Dark; d.pantsSlot = Sl.CoatB;
                if (f) { d.pantsSlot = -1; d.sockSlot = Sl.EyeW; d.sockTop = 0.38f; }
                break;
            case OPengembara:
                d.sleeveEnd = 0.94f; d.sleeveSlot = Sl.CoatA; d.gloveSlot = Sl.Leather; d.bootTop = 0.40f; d.pantsSlot = Sl.CoatB;
                break;
        }
        if (c.s.outfit != OKsatria && c.s.outfit != OPenjaga && !f && c.s.race == Dwarf) d.pantsLoose += 0.006f;
        ApplyArmorDef(c, d);
        return d;
    }

    // =====================================================================
    // Pakaian badan
    // =====================================================================
    static float FrontZ(Ctx c, float y, float grow = 0f)
    {
        float[] p = c.Prof(y);
        return p[2] + (p[1] + grow) * Mathf.Lerp(1f, c.W, 0.6f);
    }

    static void Belt(GeoBag b, Ctx c, float y, int slot, float grow = 0.020f)
    {
        var rs = new List<Vector3[]> { c.TRing(y - 0.03f, grow, 0f), c.TRing(y + 0.03f, grow, 0f) };
        NLoft(b, slot, rs, false, false);
        float z = FrontZ(c, y, grow);
        b.Box(Sl.Gold, new Vector3(0f, c.Y(y), z + 0.005f), new Vector3(0.062f, 0.056f, 0.012f));          // gesper berbingkai
        b.Box(Sl.Dark, new Vector3(0f, c.Y(y), z + 0.0105f), new Vector3(0.036f, 0.030f, 0.012f));
        b.Box(Sl.Gold, new Vector3(0f, c.Y(y), z + 0.0125f), new Vector3(0.010f, 0.034f, 0.012f));
        b.Box(slot, new Vector3(0.06f, c.Y(y) - 0.05f, z + 0.004f), new Vector3(0.026f, 0.10f, 0.010f), -4f);       // ujung sabuk menjuntai
    }

    static void Trim(GeoBag b, Ctx c, int slot, float y, float grow, float flare)
    {
        var rs = new List<Vector3[]> { c.TRing(y, grow + 0.004f, flare), c.TRing(y + 0.03f, grow + 0.004f, flare) };
        NLoft(b, slot, rs, false, false);
    }

    static void Buttons(GeoBag b, Ctx c, float y0, float y1, int n, int slot = Sl.Gold, float grow = 0.014f)
    {
        for (int i = 0; i < n; i++)
        {
            float y = Mathf.Lerp(y0, y1, n == 1 ? 0.5f : i / (float)(n - 1));
            b.Box(slot, new Vector3(0f, c.Y(y), FrontZ(c, y, grow) + 0.004f), new Vector3(0.016f, 0.016f, 0.01f));
        }
    }

    static void Collar(GeoBag b, Ctx c, int slot, float grow, float h = 0.07f, float open = 0.03f)
    {
        var rs = new List<Vector3[]> { c.TRing(1.40f, grow, 0f), c.TRing(1.40f + h, grow + open, 0f) };
        NLoft(b, slot, rs, false, false);
    }

    /// <summary>Set zirah tingkat 4+ (kecuali set Tulang) membawa jubah sendiri; jubah pakaian dasar dilewati supaya tidak bertumpuk.</summary>
    static bool ArmorCape(Spec s) { return s.armBody >= 4 && s.armBody != 7; }

    static void Garments(GeoBag b, Ctx c)
    {
        Spec s = c.s; bool f = c.fem;
        int A = Sl.CoatA, B = Sl.CoatB, Cc = Sl.CoatC;
        lastFlare = 0f;
        switch (s.outfit)
        {
            case OWarga:
                if (f)
                {
                    TorsoLoft(b, c, A, 1.00f, 1.42f, 0.012f, 0f);                                    // korset/atasan
                    TorsoLoft(b, c, B, 0.38f, 1.00f, 0.014f, 0.55f, false, false, 0.06f);            // rok
                    Trim(b, c, Cc, 0.38f, 0.014f, 0.55f);
                    Belt(b, c, 1.02f, Sl.Leather, 0.016f);
                    if (s.cA % 2 == 0) Apron(b, c, Sl.EyeW, 1.28f, 0.34f, 0.13f, 0.03f);
                    Collar(b, c, Sl.EyeW, 0.012f, 0.05f, 0.015f);
                }
                else
                {
                    TorsoLoft(b, c, A, 0.60f, 1.42f, 0.012f, 0.1f);
                    Belt(b, c, 0.98f, Sl.Leather);
                    Collar(b, c, B, 0.014f, 0.05f, 0.02f);
                    Buttons(b, c, 1.14f, 1.36f, 3);
                    b.Box(Sl.Leather, new Vector3(0.17f * c.W, c.Y(0.92f), FrontZ(c, 0.92f, 0.02f) - 0.02f), new Vector3(0.09f, 0.1f, 0.05f));
                }
                break;
            case OPedagang:
                if (f)
                {
                    TorsoLoft(b, c, B, 0.80f, 1.43f, 0.010f, 0f);
                    TorsoLoft(b, c, A, 0.30f, 0.98f, 0.016f, 0.8f, false, false, 0.06f);
                    Trim(b, c, Sl.Gold, 0.30f, 0.016f, 0.8f);
                    Belt(b, c, 1.00f, Sl.Leather, 0.02f);
                    TorsoLoft(b, c, A, 1.00f, 1.34f, 0.022f, 0f);                                    // rompi
                }
                else
                {
                    TorsoLoft(b, c, B, 0.66f, 1.43f, 0.010f, 0.05f);
                    TorsoLoft(b, c, A, 0.72f, 1.36f, 0.022f, 0.06f);                                 // rompi
                    Belt(b, c, 0.98f, Sl.Leather, 0.03f);
                    Buttons(b, c, 1.04f, 1.32f, 4, Sl.Gold, 0.026f);
                }
                Collar(b, c, Cc, 0.018f, 0.1f, 0.05f);                                               // syal
                b.Box(Cc, new Vector3(0.05f, c.Y(1.22f), FrontZ(c, 1.3f, 0.03f)), new Vector3(0.07f, 0.26f, 0.014f));
                // kantong uang
                b.Ball(Sl.Leather, new Vector3(0.20f * c.W, c.Y(0.86f), 0.04f), 0.055f, 4, 7, 1.1f);
                b.Box(Sl.Gold, new Vector3(0.20f * c.W, c.Y(0.93f), 0.04f), new Vector3(0.03f, 0.02f, 0.03f));
                break;
            case OPetani:
                if (f)
                {
                    TorsoLoft(b, c, A, 0.34f, 1.42f, 0.012f, 0.6f, false, false, 0.06f);
                    Apron(b, c, Sl.EyeW, 1.26f, 0.30f, 0.14f, 0.026f);
                    Belt(b, c, 1.02f, Sl.Leather, 0.018f);
                }
                else
                {
                    TorsoLoft(b, c, A, 0.70f, 1.42f, 0.012f, 0.06f);
                    TorsoLoft(b, c, B, 0.78f, 1.26f, 0.022f, 0f);                                    // dada celana kodok
                    Apron(b, c, B, 1.26f, 0.80f, 0.13f, 0.026f);
                    Belt(b, c, 0.98f, Sl.Leather, 0.03f);
                    Buttons(b, c, 1.2f, 1.24f, 2, Sl.Gold, 0.03f);
                }
                b.Box(B, new Vector3(0.0f, c.Y(0.90f), FrontZ(c, 0.9f, 0.03f) + 0.002f), new Vector3(0.09f, 0.08f, 0.01f));
                break;
            case OBangsawan:
                if (f)
                {
                    TorsoLoft(b, c, A, 0.98f, 1.43f, 0.010f, 0f);                                    // korset
                    TorsoLoft(b, c, A, 0.06f, 0.99f, 0.012f, 0.70f, false, false, 0.07f);          // gaun
                    Trim(b, c, Sl.Gold, 0.06f, 0.012f, 0.70f);
                    Trim(b, c, Sl.Gold, 0.99f, 0.012f, 0f);
                    TorsoLoft(b, c, B, 0.40f, 0.99f, 0.020f, 0.62f, false, false, 0.07f);          // rok luar terbuka
                    Collar(b, c, Sl.EyeW, 0.012f, 0.06f, 0.05f);
                    b.Ball(Sl.Glow, new Vector3(0f, c.Y(1.28f), FrontZ(c, 1.28f, 0.02f)), 0.018f, 3, 6);
                    Buttons(b, c, 1.04f, 1.2f, 3, Sl.Gold, 0.014f);
                }
                else
                {
                    TorsoLoft(b, c, A, 0.34f, 1.43f, 0.020f, 0.25f, false, false, 0.06f);            // mantel panjang
                    Trim(b, c, Sl.Gold, 0.34f, 0.020f, 0.25f);
                    TorsoLoft(b, c, B, 1.2f, 1.40f, 0.030f, 0f);                                     // rompi bordir
                    Belt(b, c, 0.98f, Sl.Gold, 0.034f);
                    Collar(b, c, Cc, 0.026f, 0.1f, 0.05f);
                    Buttons(b, c, 0.5f, 1.36f, 6, Sl.Gold, 0.022f);
                    Cape(b, c, Cc, 0.45f, 0.08f, false);
                }
                break;
            case OPenjaga:
                TorsoLoft(b, c, Sl.Metal, 0.95f, 1.42f, 0.018f, 0f);                                 // rompi rantai
                TorsoLoft(b, c, B, 0.70f, 0.96f, 0.014f, 0.15f);
                Tabard(b, c, A, 1.34f, 0.46f);
                Belt(b, c, 0.98f, Sl.Leather, 0.03f);
                Collar(b, c, Sl.Metal, 0.02f, 0.05f, 0.02f);
                b.Box(Sl.Metal, new Vector3(0.20f * c.W, c.Y(0.88f), 0.0f), new Vector3(0.03f, 0.04f, 0.18f));     // sarung pedang pendek
                break;
            case OKsatria:
                TorsoLoft(b, c, Sl.Metal, 0.92f, 1.43f, 0.028f, 0f);                                 // pelat dada
                b.Box(Sl.Gold, new Vector3(0f, c.Y(1.22f), FrontZ(c, 1.22f, 0.03f) + 0.004f), new Vector3(0.05f, 0.30f, 0.01f));
                Trim(b, c, Sl.Gold, 0.92f, 0.028f, 0f);
                Trim(b, c, Sl.Gold, 1.38f, 0.028f, 0f);
                TorsoLoft(b, c, B, 0.60f, 0.93f, 0.020f, 0.2f);                                      // kain di bawah pelat
                for (int i = 0; i < 7; i++)                                                           // tasset
                {
                    float a = (i - 3) / 3f * 1.1f;
                    Vector3 top = new Vector3(Mathf.Sin(a) * 0.17f * c.W * 1.1f, c.Y(0.90f), Mathf.Cos(a) * 0.11f);
                    b.Box(Sl.Metal, top + new Vector3(0f, c.Y(-0.08f), Mathf.Cos(a) * 0.012f), new Vector3(0.07f, c.Y(0.20f), 0.015f), a * 57.3f);
                }
                Belt(b, c, 0.93f, Sl.Leather, 0.04f);
                if (!ArmorCape(s)) Cape(b, c, A, 0.28f, 0.10f, false);
                Collar(b, c, Sl.Metal, 0.03f, 0.06f, 0.02f);
                break;
            case OPendeta:
                TorsoLoft(b, c, A, 0.05f, 1.43f, 0.016f, 0.5f, false, false, 0.07f);
                Trim(b, c, Sl.Gold, 0.05f, 0.016f, 0.5f);
                Belt(b, c, 1.02f, Cc, 0.026f);
                // stola emas di depan dan belakang: menempel pada jubah
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int bk = 0; bk < 2; bk++)
                    {
                        float cx = sx * 0.082f * c.W;
                        Panel(b, c, Sl.Gold, bk == 1, pu => 1.405f, pu => 0.09f, py => cx - 0.022f, py => cx + 0.022f, 0.027f, lastFlare, 1, 22);
                    }
                Collar(b, c, A, 0.026f, 0.06f, 0.03f);
                // liontin salib
                b.Box(Sl.Gold, new Vector3(0f, c.Y(1.26f), FrontZ(c, 1.26f, 0.03f) + 0.01f), new Vector3(0.012f, 0.06f, 0.008f));
                b.Box(Sl.Gold, new Vector3(0f, c.Y(1.275f), FrontZ(c, 1.26f, 0.03f) + 0.01f), new Vector3(0.04f, 0.012f, 0.008f));
                break;
            case OPenyihir:
                TorsoLoft(b, c, A, 0.10f, 1.43f, 0.016f, 0.45f, false, false, 0.07f);
                Trim(b, c, Cc, 0.10f, 0.016f, 0.45f);
                Belt(b, c, 1.00f, Sl.Leather, 0.026f);
                Cape(b, c, B, 0.20f, 0.12f, true);
                Mantle(b, c, Cc, 1.17f, 0.325f, Sl.Gold);
                b.Box(Sl.Leather, new Vector3(0.20f * c.W, c.Y(0.90f), 0.03f), new Vector3(0.07f, 0.09f, 0.07f));      // kantong ramuan
                b.Ball(Sl.Glow, new Vector3(-0.17f * c.W, c.Y(0.92f), 0.05f), 0.02f, 3, 6);
                b.Ball(Sl.Gold, new Vector3(0f, c.Y(1.33f), FrontZ(c, 1.33f, 0.03f)), 0.02f, 3, 6);
                break;
            case OHunter:
                TorsoLoft(b, c, A, 0.72f, 1.40f, 0.016f, 0.12f);                                     // tunik hijau
                Vest(b, c, Sl.Leather, Sl.Dark, 1.38f, 0.92f, 0.150f, 0.028f);                       // rompi kulit
                Belt(b, c, 0.96f, Sl.Leather, 0.036f);
                Collar(b, c, B, 0.022f, 0.1f, 0.04f);
                if (!ArmorCape(s)) Cape(b, c, B, 1.04f, 0.0f, true);
                b.Box(Sl.Leather, new Vector3(-0.19f * c.W, c.Y(0.88f), 0.03f), new Vector3(0.07f, 0.1f, 0.07f));      // kantong sabuk
                break;
            case OPandai:
                TorsoLoft(b, c, A, 0.80f, 1.40f, 0.008f, 0f, false, false);                            // singlet
                Apron(b, c, Sl.Leather, 1.30f, 0.34f, 0.17f, 0.034f);                                 // celemek kulit
                Belt(b, c, 0.96f, Sl.Leather, 0.04f);
                b.Box(Sl.Metal, new Vector3(0.0f, c.Y(0.62f), FrontZ(c, 0.82f, 0.04f) + 0.08f), new Vector3(0.12f, 0.03f, 0.02f));
                break;
            case OPelayan:
                if (f)
                {
                    TorsoLoft(b, c, A, 0.30f, 1.42f, 0.012f, 0.9f, false, false, 0.06f);
                    Apron(b, c, B, 1.20f, 0.28f, 0.14f, 0.030f);
                    Trim(b, c, B, 0.30f, 0.012f, 0.9f);
                    Collar(b, c, B, 0.014f, 0.05f, 0.02f);
                    b.Beam(B, new Vector3(-0.05f, c.Y(1.36f), FrontZ(c, 1.36f, 0.02f) + 0.01f), new Vector3(0.05f, c.Y(1.36f), FrontZ(c, 1.36f, 0.02f) + 0.01f), 0.02f, 0.03f);   // pita
                }
                else
                {
                    TorsoLoft(b, c, A, 0.30f, 1.42f, 0.018f, 0.2f, false, false, 0.06f);             // jas ekor
                    TorsoLoft(b, c, B, 0.98f, 1.36f, 0.026f, 0f);
                    Collar(b, c, B, 0.02f, 0.06f, 0.02f);
                    b.Box(Sl.Dark, new Vector3(0f, c.Y(1.26f), FrontZ(c, 1.26f, 0.03f) + 0.004f), new Vector3(0.025f, 0.12f, 0.01f));
                    Buttons(b, c, 0.98f, 1.14f, 2, Sl.Gold, 0.028f);
                }
                break;
            case OBard:
                TorsoLoft(b, c, A, f ? 0.50f : 0.62f, 1.42f, 0.016f, f ? 0.4f : 0.18f, false, false, 0.05f);
                TorsoLoft(b, c, B, 0.98f, 1.40f, 0.024f, 0f);
                Trim(b, c, Sl.Gold, f ? 0.50f : 0.62f, 0.016f, f ? 0.4f : 0.18f);
                Belt(b, c, 0.98f, Cc, 0.036f);
                Collar(b, c, Cc, 0.022f, 0.07f, 0.04f);
                Buttons(b, c, 1.06f, 1.34f, 5, Sl.Gold, 0.028f);
                b.Beam(Cc, new Vector3(-0.17f * c.W, c.Y(1.38f), 0.0f), new Vector3(0.17f * c.W, c.Y(0.80f), 0.0f), 0.04f, 0.02f, Vector3.forward);   // selempang
                break;
            case OKoki:
                TorsoLoft(b, c, A, 0.62f, 1.42f, 0.014f, 0.08f);
                Buttons(b, c, 1.0f, 1.34f, 4, Sl.Gold, 0.016f);
                Apron(b, c, B, 1.10f, 0.28f, 0.16f, 0.030f);
                Collar(b, c, B, 0.022f, 0.05f, 0.03f);
                Belt(b, c, 0.98f, Sl.Leather, 0.03f);
                break;
            case OPelajar:
            case OPetugas:
                if (f)
                {
                    TorsoLoft(b, c, A, 0.84f, 1.42f, 0.016f, 0f);                                    // blazer
                    TorsoLoft(b, c, B, 0.42f, 0.88f, 0.018f, 0.5f, false, false, 0.06f);             // rok lipit
                    Trim(b, c, Sl.Gold, 0.42f, 0.018f, 0.5f);
                }
                else
                {
                    TorsoLoft(b, c, A, 0.60f, 1.42f, 0.016f, 0.05f);
                }
                Collar(b, c, Sl.EyeW, 0.020f, 0.05f, 0.03f);
                b.Box(Cc, new Vector3(0f, c.Y(1.24f), FrontZ(c, 1.24f, 0.026f) + 0.004f), new Vector3(0.03f, 0.2f, 0.01f));      // dasi
                Buttons(b, c, 0.96f, 1.12f, 2, Sl.Gold, 0.02f);
                b.Box(Sl.Gold, new Vector3(-0.1f * c.W, c.Y(1.26f), FrontZ(c, 1.26f, 0.02f) + 0.003f), new Vector3(0.04f, 0.04f, 0.008f), 20f);   // lencana
                break;
            case OPengembara:
                TorsoLoft(b, c, A, 0.62f, 1.42f, 0.014f, 0.1f);
                Belt(b, c, 0.98f, Sl.Leather, 0.03f);
                Cape(b, c, B, 0.30f, 0.10f, true);
                b.Box(Sl.Leather, new Vector3(0.20f * c.W, c.Y(0.90f), 0.03f), new Vector3(0.07f, 0.09f, 0.07f));
                b.Beam(Sl.Leather, new Vector3(-0.17f * c.W, c.Y(1.38f), 0.0f), new Vector3(0.17f * c.W, c.Y(0.84f), 0.0f), 0.034f, 0.02f, Vector3.forward);
                break;
        }
        if (s.armBody > 0) ArmorBody(b, c);
    }

    // =====================================================================
    // Properti di badan dan tangan
    // =====================================================================
    static void Props(GeoBag b, Ctx c)
    {
        Spec s = c.s;
        float bz = -0.115f * Mathf.Lerp(1f, c.W, 0.6f) - 0.03f;
        switch (s.prop)
        {
            case 1:      // bungkusan dagangan di punggung
                b.Ball(Sl.CoatC, new Vector3(0f, c.Y(1.12f), bz - 0.08f), 0.17f, 5, 8, 0.9f);
                b.Box(Sl.Leather, new Vector3(0f, c.Y(1.24f), bz - 0.08f), new Vector3(0.34f, 0.025f, 0.02f), 0f);
                break;
            case 4:      // pedang di pinggang
                b.Box(Sl.Leather, new Vector3(-0.21f * c.W, c.Y(0.78f), -0.02f), new Vector3(0.05f, c.Y(0.70f), 0.05f), 0f);
                b.Box(Sl.Gold, new Vector3(-0.21f * c.W, c.Y(1.14f), -0.02f), new Vector3(0.06f, 0.025f, 0.08f));
                b.Box(Sl.Metal, new Vector3(-0.21f * c.W, c.Y(1.20f), -0.02f), new Vector3(0.025f, c.Y(0.12f), 0.03f));
                b.Ball(Sl.Gold, new Vector3(-0.21f * c.W, c.Y(1.27f), -0.02f), 0.02f, 3, 6);
                break;
            case 7:      // tabung panah + busur di punggung
                b.Cyl(Sl.Leather, new Vector3(0.07f, c.Y(0.95f), bz - 0.04f), 0.06f, c.Y(0.5f), 8, 0.055f, true, true);
                for (int i = 0; i < 4; i++)
                    b.Box(Sl.CoatC, new Vector3(0.05f + i * 0.015f, c.Y(1.5f), bz - 0.04f + (i % 2) * 0.02f), new Vector3(0.012f, c.Y(0.1f), 0.012f));
                b.Beam(Sl.Wood, new Vector3(-0.08f, c.Y(0.70f), bz - 0.05f), new Vector3(-0.10f, c.Y(1.62f), bz - 0.05f), 0.03f, 0.03f);
                b.Beam(Sl.EyeW, new Vector3(-0.08f, c.Y(0.72f), bz - 0.045f), new Vector3(-0.10f, c.Y(1.60f), bz - 0.045f), 0.006f, 0.006f);
                break;
            case 8:      // palu di sabuk
                b.Box(Sl.Wood, new Vector3(-0.2f * c.W, c.Y(0.78f), 0.0f), new Vector3(0.03f, c.Y(0.34f), 0.03f), 0f);
                b.Box(Sl.Metal, new Vector3(-0.2f * c.W, c.Y(0.96f), 0.0f), new Vector3(0.09f, 0.06f, 0.06f));
                break;
            case 9:      // kecapi di punggung
                b.Ball(Sl.WoodLight, new Vector3(0f, c.Y(1.02f), bz - 0.04f), 0.15f, 5, 8, 1.1f);
                b.Beam(Sl.Wood, new Vector3(0f, c.Y(1.10f), bz - 0.04f), new Vector3(0.0f, c.Y(1.66f), bz - 0.06f), 0.04f, 0.03f);
                b.Ball(Sl.Gold, new Vector3(0f, c.Y(1.68f), bz - 0.06f), 0.025f, 3, 6);
                break;
            case 11:     // ransel pengembara
                b.BoxB(Sl.Leather, new Vector3(0f, c.Y(0.92f), bz - 0.10f), new Vector3(0.30f * c.W, c.Y(0.46f), 0.18f));
                b.BoxB(Sl.CoatB, new Vector3(0f, c.Y(1.30f), bz - 0.10f), new Vector3(0.26f * c.W, 0.06f, 0.17f));
                b.Cyl(Sl.CoatC, new Vector3(0f, c.Y(0.86f), bz - 0.09f), 0.07f, 0.3f, 8, 0.07f, true, true);        // gulungan tidur
                break;
        }
    }

    static Vector3[] Offs(Vector3[] r, Vector3 o)
    {
        var q = new Vector3[r.Length];
        for (int i = 0; i < r.Length; i++) q[i] = r[i] + o;
        return q;
    }

    static void ArmProps(GeoBag b, Ctx c, int sgn, Vector3 hand)
    {
        Spec s = c.s;
        if (sgn == 1)
        {
            switch (s.prop)
            {
                case 2:  // garpu jerami: tiga mata melengkung + selongsong
                {
                    float ty = c.Y(1.74f);
                    b.Cyl(Sl.Wood, new Vector3(hand.x, 0f, hand.z), 0.017f, ty, 8, 0.015f, true, true);
                    b.Cyl(Sl.Metal, new Vector3(hand.x, ty - 0.02f, hand.z), 0.024f, 0.05f, 8, 0.02f, true, true);
                    for (int i = -1; i <= 1; i++)
                    {
                        float ox = i * 0.042f;
                        Sweep(b, Sl.Metal, new[] { new Vector3(hand.x + ox * 0.5f, ty + 0.02f, hand.z), new Vector3(hand.x + ox, ty + 0.07f, hand.z), new Vector3(hand.x + ox * 1.1f, ty + 0.15f, hand.z), new Vector3(hand.x + ox * 1.05f, ty + 0.23f, hand.z) },
                            new[] { 0.007f, 0.006f, 0.005f, 0.0012f }, new[] { 0.007f, 0.006f, 0.005f, 0.0012f }, Vector3.forward, 6, 2);
                    }
                    break;
                }
                case 3:  // tombak: bilah daun, selongsong, rumbai, panji kecil
                {
                    float top = c.Y(1.90f);
                    b.Cyl(Sl.Wood, new Vector3(hand.x, 0f, hand.z), 0.018f, top, 8, 0.016f, true, true);
                    b.Cyl(Sl.Metal, new Vector3(hand.x, top - 0.05f, hand.z), 0.024f, 0.10f, 8, 0.018f, true, true);
                    b.Cyl(Sl.Gold, new Vector3(hand.x, top - 0.012f, hand.z), 0.026f, 0.014f, 8, 0.026f, false, false);
                    float[] ty = { 0f, 0.05f, 0.11f, 0.18f, 0.25f, 0.30f }, tw = { 0.010f, 0.026f, 0.032f, 0.022f, 0.010f, 0.0006f }, tt = { 0.007f, 0.009f, 0.008f, 0.006f, 0.004f, 0.0006f };
                    var rs = new List<Vector3[]>();
                    for (int i = 0; i < ty.Length; i++) rs.Add(Offs(BladeRing(top + 0.05f + ty[i], tw[i], tt[i]), new Vector3(hand.x, 0f, hand.z)));
                    NLoft(b, Sl.Metal, rs, true, true, false);
                    b.QuadBoth(Sl.CoatA, new Vector3(hand.x, top - 0.07f, hand.z), new Vector3(hand.x + 0.09f, top - 0.085f, hand.z + 0.01f), new Vector3(hand.x + 0.08f, top - 0.15f, hand.z + 0.01f), new Vector3(hand.x, top - 0.14f, hand.z));
                    break;
                }
                case 4:  // pedang terhunus menunjuk ke bawah di sisi
                {
                    b.PushTRS(new Vector3(hand.x, hand.y + 0.02f, hand.z + 0.01f), Quaternion.Euler(0f, 0f, 180f), 0.9f);
                    Sword(b, 0.62f, 0.07f, Sl.Metal, Sl.Gold, Sl.Leather, false);
                    b.Pop();
                    break;
                }
                case 5:  // tongkat suci / 6: tongkat penyihir: memakai model Staff, ditambah tiang sampai tanah
                case 6:
                {
                    const float lift = 0.16f;                                                   // tangan memegang agak di bawah kepala tongkat; kepala jadi lebih tinggi dari kepala orang
                    b.PushTRS(new Vector3(hand.x, hand.y, hand.z), Quaternion.Euler(-5f, 0f, 0f), 1f);   // condong sedikit ke badan: kaki tongkat maju, tidak tertelan jubah
                    b.PushTRS(new Vector3(0f, lift, 0f), Quaternion.identity, 1f);
                    Staff(b, s.prop == 5, 0, Sl.Glow);
                    b.Pop();
                    b.Cyl(Sl.WoodLight, new Vector3(0f, -hand.y - 0.02f, 0f), 0.02f, hand.y + lift - 0.28f + 0.02f + 0.004f, 8, 0.02f, true, true);
                    b.Pop();
                    break;
                }
                case 10: // buku bersampul dengan jepitan emas
                {
                    Vector3 bp = new Vector3(hand.x, hand.y + 0.02f, hand.z + 0.06f);
                    b.Box(Sl.CoatC, bp, new Vector3(0.034f, 0.21f, 0.155f));
                    b.Box(Sl.EyeW, bp + new Vector3(-0.0035f, 0f, 0.004f), new Vector3(0.030f, 0.196f, 0.150f));
                    b.Box(Sl.Gold, bp + new Vector3(0.0185f, 0.07f, 0.06f), new Vector3(0.004f, 0.045f, 0.016f));
                    for (int k = -1; k <= 1; k += 2) b.Box(Sl.Gold, bp + new Vector3(0.0185f, k * 0.1f, 0.07f), new Vector3(0.004f, 0.012f, 0.012f));
                    break;
                }
            }
        }
        else if (s.prop == 4)    // perisai bulat
        {
            b.PushTRS(new Vector3(hand.x - 0.046f, hand.y + 0.06f, hand.z + 0.038f), Quaternion.Euler(0f, 40f, 90f), 0.85f);       // muka perisai menghadap depan-luar, pegangan tepat di tangan
            Shield(b);
            b.Pop();
        }
    }
}
