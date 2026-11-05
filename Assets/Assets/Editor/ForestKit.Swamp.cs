using System;
using UnityEditor;
using UnityEngine;

/// <summary>Palet &amp; tekstur prosedural untuk Rawa Terkutuk (kayu basah, lumpur, gambut, lumut, batu nisan berlumut, cahaya hantu). Slot sama dengan kit hutan.</summary>
public static partial class ForestKit
{
    public static Material[] SSlots;
    public static Texture2D sMudT, sPeatT, sMossT, sGrassT, sAshT, sPathT, sRockT;
    static bool sLoaded;

    public static void ResetSwamp() { sLoaded = false; SSlots = null; }

    // ------------------------------------------------------------------ tekstur objek
    static Texture2D TexWetWood()
    {
        return Make("s_wetwood", 256, (u, v) =>
        {
            float grain = VN(u, v, 4, 60, 601), n = FBM(u, v, 5, 4, 602);
            float plank = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 6f));
            float t = 0.30f + (grain - 0.5f) * 0.18f + (n - 0.5f) * 0.2f;
            if (plank < 0.05f) t *= 0.55f;
            float algae = Mathf.Clamp01((FBM(u, v, 4, 3, 603) - 0.55f) * 3f);
            return Cl(t * (1f - algae * 0.35f), t * (0.92f + algae * 0.25f), t * (0.78f - algae * 0.1f));
        });
    }

    static Texture2D TexRotPlaster()
    {
        return Make("s_rotplaster", 256, (u, v) =>
        {
            float n = FBM(u, v, 6, 5, 611), f = VN(u, v, 80, 80, 612);
            float t = 0.46f + (n - 0.5f) * 0.3f + (f - 0.5f) * 0.05f;
            float damp = Mathf.Clamp01((FBM(u, v, 3, 3, 613) - 0.45f) * 2.5f);
            return Cl(t * (0.9f - damp * 0.3f), t * (0.92f - damp * 0.05f), t * (0.82f - damp * 0.25f));
        });
    }

    static Texture2D TexSlimeStone()
    {
        return Make("s_slimestone", 256, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 6, 621, out f1, out f2, out id);
            float t = 0.36f + (Hash(id, 5, 621) - 0.5f) * 0.12f + (FBM(u, v, 12, 4, 622) - 0.5f) * 0.14f;
            if (f2 - f1 < 0.035f) t *= 0.5f;
            float moss = Mathf.Clamp01((FBM(u, v, 5, 3, 623) - 0.5f) * 2.4f);
            return Cl(t * (1f - moss * 0.4f), t * (1f - moss * 0.05f), t * (0.95f - moss * 0.45f));
        });
    }

    static Texture2D TexBogBark()
    {
        return Make("s_bogbark", 256, (u, v) =>
        {
            float ridge = Mathf.Abs(Mathf.Sin((u + FBM(u, v, 3, 3, 631) * 0.15f) * Mathf.PI * 14f));
            float t = 0.22f + ridge * 0.14f + (FBM(u, v, 10, 4, 632) - 0.5f) * 0.12f;
            float moss = Mathf.Clamp01((FBM(u, v, 4, 3, 633) - 0.55f) * 2.5f);
            return Cl(t * (1f - moss * 0.4f), t * (1f + moss * 0.15f), t * (0.9f - moss * 0.5f));
        });
    }

    static Texture2D TexBogLeaf()
    {
        return Make("s_bogleaf", 256, (u, v) =>
        {
            float n = FBM(u, v, 8, 4, 641), c = VN(u, v, 40, 40, 642);
            float t = 0.5f + (n - 0.5f) * 0.4f + (c - 0.5f) * 0.12f;
            return Cl(t * 0.62f, t * 0.82f, t * 0.42f);
        });
    }

    static Texture2D TexBeardMoss()
    {
        return Make("s_beardmoss", 256, (u, v) =>
        {
            float f = VN(u, v, 90, 8, 651), n = FBM(u, v, 6, 3, 652);
            float t = 0.5f + (f - 0.5f) * 0.35f + (n - 0.5f) * 0.2f;
            return Cl(t * 0.66f, t * 0.8f, t * 0.62f);
        });
    }

    static Texture2D TexRotThatch()
    {
        return Make("s_rotthatch", 256, (u, v) =>
        {
            float f = VN(u, v, 100, 10, 661), n = FBM(u, v, 6, 3, 662);
            float t = 0.3f + (f - 0.5f) * 0.22f + (n - 0.5f) * 0.16f;
            return Cl(t * 0.95f, t * 0.9f, t * 0.62f);
        });
    }

    static Texture2D TexSlate()
    {
        return Make("s_slate", 256, (u, v) =>
        {
            const int rows = 9;
            float pv = v * rows; int row = Mathf.FloorToInt(pv); float fv = pv - row;
            float pu = u * 6f + (row % 2) * 0.5f; int col = Mathf.FloorToInt(pu); float fu = pu - col;
            float h = Hash(col + row * 5, row, 671);
            float t = 0.28f + (h - 0.5f) * 0.1f + (FBM(u, v, 10, 3, 672) - 0.5f) * 0.1f;
            if (fv < 0.07f || fu < 0.04f) t *= 0.5f;
            float moss = Mathf.Clamp01((FBM(u, v, 4, 3, 673) - 0.52f) * 3f);
            return Cl(t * (0.9f - moss * 0.3f), t * (1f + moss * 0.1f), t * (1.05f - moss * 0.4f));
        });
    }

    static Texture2D TexTatter()
    {
        return Make("s_tatter", 256, (u, v) =>
        {
            float w = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 60f)) * 0.05f + Mathf.Abs(Mathf.Sin(v * Mathf.PI * 60f)) * 0.05f;
            float t = 0.42f + w + (FBM(u, v, 6, 4, 681) - 0.5f) * 0.3f;
            return Cl(t * 0.8f, t * 0.9f, t * 0.82f);
        });
    }

    // ------------------------------------------------------------------ tekstur terrain
    static Texture2D TexSMud()
    {
        return Make("s_tmud", 512, (u, v) =>
        {
            float n = FBM(u, v, 5, 5, 701), s = VN(u, v, 180, 180, 702);
            float wet = Mathf.Clamp01((FBM(u, v, 3, 3, 703) - 0.45f) * 2f);
            float t = 0.30f + (n - 0.5f) * 0.18f + (s - 0.5f) * 0.04f;
            return Cl(t * (1f - wet * 0.25f), t * (0.86f - wet * 0.1f), t * (0.64f + wet * 0.05f));
        });
    }

    static Texture2D TexSPeat()
    {
        return Make("s_tpeat", 512, (u, v) =>
        {
            float n = FBM(u, v, 6, 5, 711), s = VN(u, v, 200, 200, 712), root = VN(u, v, 40, 14, 713);
            float t = 0.17f + (n - 0.5f) * 0.12f + (s - 0.5f) * 0.05f + (root - 0.5f) * 0.03f;
            return Cl(t * 1.1f, t * 0.88f, t * 0.7f);
        });
    }

    static Texture2D TexSMoss()
    {
        return Make("s_tmoss", 512, (u, v) =>
        {
            float n = FBM(u, v, 8, 5, 721), s = VN(u, v, 210, 210, 722);
            float t = 0.5f + (n - 0.5f) * 0.28f + (s - 0.5f) * 0.1f;
            return Cl(t * 0.30f, t * 0.50f, t * 0.17f);
        });
    }

    static Texture2D TexSGrass()
    {
        return Make("s_tgrass", 512, (u, v) =>
        {
            float n = FBM(u, v, 6, 5, 731), blade = VN(u, v, 170, 80, 732), fine = VN(u, v, 210, 210, 733);
            float t = 0.5f + (n - 0.5f) * 0.25f + (blade - 0.5f) * 0.12f + (fine - 0.5f) * 0.06f;
            return Cl(t * 0.34f, t * 0.46f, t * 0.2f);
        });
    }

    static Texture2D TexSAsh()
    {
        return Make("s_tash", 512, (u, v) =>
        {
            float n = FBM(u, v, 6, 5, 741), s = VN(u, v, 200, 200, 742);
            float bone = Mathf.Clamp01((VN(u, v, 60, 60, 743) - 0.86f) * 6f);
            float t = 0.26f + (n - 0.5f) * 0.12f + (s - 0.5f) * 0.05f + bone * 0.35f;
            return Cl(t * 0.92f, t * 0.95f, t);
        });
    }

    static Texture2D TexSPath()
    {
        return Make("s_tpath", 512, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 40, 751, out f1, out f2, out id);
            float t = 0.34f + (Hash(id, 3, 751) - 0.5f) * 0.12f - f1 * 0.2f + (FBM(u, v, 10, 4, 752) - 0.5f) * 0.1f;
            return Cl(t * 0.95f, t * 0.88f, t * 0.74f);
        });
    }

    static Texture2D TexSRock()
    {
        return Make("s_trock", 512, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 9, 761, out f1, out f2, out id);
            float t = 0.3f + (FBM(u, v, 12, 5, 762) - 0.5f) * 0.18f + (Hash(id, 2, 761) - 0.5f) * 0.06f;
            if (f2 - f1 < 0.03f) t *= 0.6f;
            return Cl(t * 0.9f, t * 0.96f, t);
        });
    }

    /// <summary>Rumput rawa / alang-alang gelap (billboard detail).</summary>
    public static Texture2D TexBogTuft()
    {
        return Make("s_bogtuft", 128, (u, v) =>
        {
            float cov = 0f, shade = 0f;
            for (int i = 0; i < 14; i++)
            {
                float x0 = 0.1f + 0.8f * Hash(i, 1, 771);
                float lean = (Hash(i, 2, 771) - 0.5f) * 0.6f;
                float hgt = 0.55f + 0.43f * Hash(i, 3, 771);
                float w = 0.02f + 0.016f * Hash(i, 4, 771);
                float yy = 1f - v;
                if (yy > hgt) continue;
                float tt = yy / hgt;
                float cx = x0 + lean * tt * tt;
                float half = w * (1f - tt * 0.9f);
                if (Mathf.Abs(u - cx) < half) { cov = 1f; shade = Mathf.Max(shade, 0.5f + 0.5f * tt); }
            }
            return new Color(0.34f * shade, 0.5f * shade, 0.2f * shade, cov);
        }, true);
    }

    // ------------------------------------------------------------------ material
    public static void LoadSwamp()
    {
        if (sLoaded && SSlots != null) return;
        Load();
        var wet = TexWetWood(); var plaster = TexRotPlaster(); var slime = TexSlimeStone(); var bogBark = TexBogBark(); var bogLeaf = TexBogLeaf();
        var beard = TexBeardMoss(); var thatch = TexRotThatch(); var slate = TexSlate(); var tatter = TexTatter();
        sMudT = TexSMud(); sPeatT = TexSPeat(); sMossT = TexSMoss(); sGrassT = TexSGrass(); sAshT = TexSAsh(); sPathT = TexSPath(); sRockT = TexSRock();

        SSlots = (Material[])Slots.Clone();
        Func<string, Color, Texture2D, float, float, Material> D = (n, c, t, tile, sm) => TownMats.Mk("S_" + n, c, t, tile, sm, 0f);

        SSlots[Sl.Plaster] = D("RotPlaster", Cl(1f, 1f, 1f), plaster, 2.4f, 0.03f);
        SSlots[Sl.Stone] = D("SlimeStone", Cl(1f, 1f, 1f), slime, 2.2f, 0.12f);
        SSlots[Sl.DarkStone] = D("BlackStone", Cl(0.7f, 0.74f, 0.8f), slime, 2.4f, 0.14f);
        SSlots[Sl.Wood] = D("WetWood", Cl(1f, 1f, 1f), wet, 1.8f, 0.1f);
        SSlots[Sl.Roof] = D("MossSlate", Cl(1f, 1f, 1f), slate, 2.2f, 0.08f);
        SSlots[Sl.RoofB] = SSlots[Sl.Roof];
        SSlots[Sl.Thatch] = D("RotThatch", Cl(1f, 1f, 1f), thatch, 1.4f, 0.02f);
        SSlots[Sl.Cloth] = D("TatterGreen", Cl(0.7f, 0.85f, 0.7f), tatter, 1.2f, 0.03f);
        SSlots[Sl.ClothB] = D("TatterViolet", Cl(0.62f, 0.4f, 0.8f), tatter, 1.2f, 0.03f);
        SSlots[Sl.Marble] = D("BoneWhite", Cl(0.82f, 0.82f, 0.74f), TexAged("s_bone", 781, 0.78f, false), 1.6f, 0.05f);
        SSlots[Sl.Dirt] = D("Mud", Cl(1f, 1f, 1f), sMudT, 4f, 0.08f);
        SSlots[Sl.Cobble] = D("SlimeCobble", Cl(0.8f, 0.9f, 0.85f), slime, 3f, 0.1f);

        SSlots[Bark] = D("BogBark", Cl(1f, 1f, 1f), bogBark, 2f, 0.05f);
        SSlots[BarkBirch] = D("GreyBark", Cl(0.62f, 0.66f, 0.6f), bogBark, 2f, 0.05f);
        SSlots[BarkDead] = D("BlackBark", Cl(0.5f, 0.48f, 0.5f), bogBark, 2f, 0.03f);
        SSlots[LeafGreen] = D("BogLeaf", Cl(0.8f, 0.9f, 0.7f), bogLeaf, 1.8f, 0.08f);
        SSlots[LeafPine] = D("CypressLeaf", Cl(0.5f, 0.7f, 0.5f), bogLeaf, 1.6f, 0.06f);
        SSlots[LeafYellow] = D("SickLeaf", Cl(0.9f, 0.9f, 0.35f), bogLeaf, 1.7f, 0.06f);
        SSlots[LeafOrange] = D("BogBloom", Cl(0.8f, 0.5f, 0.75f), bogLeaf, 1.7f, 0.1f);
        SSlots[LeafRed] = D("BogBerry", Cl(0.55f, 0.1f, 0.25f), bogLeaf, 1.7f, 0.1f);
        SSlots[Moss] = D("SlimeMoss", Cl(0.7f, 1f, 0.6f), TexMossObj(), 1.5f, 0.1f);
        SSlots[Rock] = D("WetRock", Cl(0.6f, 0.66f, 0.64f), TexRockObj(), 3f, 0.14f);
        SSlots[Ivy] = D("BeardMoss", Cl(1f, 1f, 1f), beard, 1.2f, 0.04f);
        SSlots[Weathered] = D("GreyWood", Cl(0.7f, 0.72f, 0.68f), wet, 2f, 0.05f);
        SSlots[Grime] = D("Mildew", Cl(0.7f, 0.78f, 0.66f), plaster, 3f, 0.04f);
        SSlots[OldRoof] = D("OldSlate", Cl(0.7f, 0.78f, 0.7f), slate, 2f, 0.05f);
        SSlots[OldThatch] = D("OldRot", Cl(0.6f, 0.62f, 0.5f), thatch, 2f, 0.02f);
        SSlots[Ash] = D("Soot", Cl(0.1f, 0.1f, 0.11f), bogBark, 2f, 0.02f);
        SSlots[Crystal] = TownMats.Emissive(D("SpiritGlow", Cl(0.45f, 1f, 0.7f), null, 1f, 0.85f), Cl(0.35f, 1.5f, 0.9f));
        sLoaded = true;
    }
}
