using System;
using UnityEditor;
using UnityEngine;

/// <summary>Palet &amp; tekstur prosedural untuk Gurun Zahar (batu pasir, bata lumpur, palem, kaktus, pasir). Memakai slot yang sama dengan kit hutan.</summary>
public static partial class ForestKit
{
    public static Material[] DSlots;
    public static Texture2D dSandT, dDuneT, dRockT, dCrackT, dGravelT, dDryGrassT, dOasisT;
    static bool dLoaded;

    public static void ResetDesert() { dLoaded = false; DSlots = null; }

    // ------------------------------------------------------------------ tekstur objek
    static Texture2D TexAdobe()
    {
        return Make("d_adobe", 256, (u, v) =>
        {
            float n = FBM(u, v, 5, 5, 301), f = VN(u, v, 70, 70, 302);
            float trowel = VN(u + VN(u, v, 3, 3, 304) * 0.1f, v, 3, 14, 303);
            float t = 0.74f + (n - 0.5f) * 0.28f + (f - 0.5f) * 0.05f + (trowel - 0.5f) * 0.06f;
            float crack = Mathf.Abs(VN(u, v, 9, 9, 305) - 0.5f);
            if (crack < 0.012f) t *= 0.7f;
            return Cl(t * 1.0f, t * 0.86f, t * 0.66f);
        });
    }

    static Texture2D TexSandstone()
    {
        return Make("d_sandstone", 256, (u, v) =>
        {
            const int rows = 6;
            float pv = v * rows; int row = Mathf.FloorToInt(pv); float fv = pv - row;
            float pu = u * 3f + (row % 2) * 0.5f; int col = Mathf.FloorToInt(pu); float fu = pu - col;
            float h = Hash(col + row * 7, row, 311);
            float strata = Mathf.Sin((v + FBM(u, v, 4, 3, 312) * 0.25f) * Mathf.PI * 34f) * 0.035f;
            float t = 0.66f + (h - 0.5f) * 0.16f + strata + (FBM(u, v, 14, 4, 313) - 0.5f) * 0.18f;
            if (fv < 0.045f || fu < 0.03f || fu > 0.97f) t *= 0.58f;
            float wear = Mathf.Clamp01((FBM(u, v, 4, 3, 314) - 0.6f) * 3f);
            t *= 1f - wear * 0.14f;
            return Cl(t * 1.0f, t * 0.82f, t * 0.58f);
        });
    }

    static Texture2D TexBasalt()
    {
        return Make("d_basalt", 256, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 5, 321, out f1, out f2, out id);
            float t = 0.34f + (Hash(id, 4, 321) - 0.5f) * 0.1f + (FBM(u, v, 12, 4, 322) - 0.5f) * 0.14f;
            if (f2 - f1 < 0.03f) t *= 0.55f;
            return Cl(t * 1.0f, t * 0.9f, t * 0.82f);
        });
    }

    static Texture2D TexSandRock()
    {
        return Make("d_sandrock", 256, (u, v) =>
        {
            float strata = Mathf.Sin((v + FBM(u, v, 3, 3, 331) * 0.4f) * Mathf.PI * 22f);
            float band = Mathf.Sin((v + FBM(u, v, 3, 3, 332) * 0.3f) * Mathf.PI * 7f);
            float t = 0.62f + strata * 0.06f + band * 0.07f + (FBM(u, v, 12, 4, 333) - 0.5f) * 0.2f;
            float red = 0.5f + band * 0.5f;
            return Cl(t * (1.0f + red * 0.1f), t * (0.74f - red * 0.06f), t * (0.52f - red * 0.1f));
        });
    }

    static Texture2D TexDrift()
    {
        return Make("d_drift", 256, (u, v) =>
        {
            float ripple = Mathf.Sin((u * 0.6f + v + FBM(u, v, 4, 3, 341) * 0.35f) * Mathf.PI * 26f) * 0.035f;
            float t = 0.86f + ripple + (FBM(u, v, 20, 3, 342) - 0.5f) * 0.08f;
            return Cl(t, t * 0.88f, t * 0.66f);
        });
    }

    static Texture2D TexPalmBark()
    {
        return Make("d_palmbark", 256, (u, v) =>
        {
            float ring = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 26f + FBM(u, v, 6, 2, 351) * 2f));
            float t = 0.40f + ring * 0.2f + (FBM(u, v, 14, 3, 352) - 0.5f) * 0.16f;
            if (ring < 0.12f) t *= 0.6f;
            return Cl(t * 0.95f, t * 0.8f, t * 0.62f);
        });
    }

    static Texture2D TexFrond()
    {
        return Make("d_frond", 256, (u, v) =>
        {
            float vein = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 30f)) * 0.14f;
            float t = 0.62f + vein + (FBM(u, v, 6, 3, 361) - 0.5f) * 0.18f;
            return Cl(t * 0.8f, t, t * 0.55f);
        });
    }

    static Texture2D TexCactus()
    {
        return Make("d_cactus", 256, (u, v) =>
        {
            float rib = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 16f));
            float t = 0.5f + rib * 0.2f + (FBM(u, v, 8, 3, 371) - 0.5f) * 0.1f;
            float spine = Hash((int)(u * 90), (int)(v * 120), 372) > 0.965f ? 0.35f : 0f;
            return Cl(t * 0.6f + spine, t * 0.88f + spine, t * 0.58f + spine * 0.7f);
        });
    }

    static Texture2D TexDry()
    {
        return Make("d_dry", 256, (u, v) =>
        {
            float f = VN(u, v, 120, 40, 381), n = FBM(u, v, 8, 4, 382);
            float t = 0.5f + (f - 0.5f) * 0.3f + (n - 0.5f) * 0.25f;
            return Cl(t * 0.95f, t * 0.82f, t * 0.46f);
        });
    }

    static Texture2D TexReed()
    {
        return Make("d_reed", 256, (u, v) =>
        {
            float f = VN(u, v, 70, 6, 391), n = FBM(u, v, 6, 3, 392);
            float t = 0.55f + (f - 0.5f) * 0.3f + (n - 0.5f) * 0.14f;
            return Cl(t * 0.95f, t * 0.78f, t * 0.45f);
        });
    }

    static Texture2D TexRug()
    {
        return Make("d_rug", 256, (u, v) =>
        {
            float stripe = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 9f));
            float diamond = Mathf.Abs(Mathf.Sin((u + v) * Mathf.PI * 6f)) * Mathf.Abs(Mathf.Sin((u - v) * Mathf.PI * 6f));
            float t = 0.55f + stripe * 0.2f + diamond * 0.22f + (FBM(u, v, 14, 2, 401) - 0.5f) * 0.1f;
            return Cl(t * 1.0f, t * 0.5f, t * 0.36f);
        });
    }

    // ------------------------------------------------------------------ tekstur terrain
    static Texture2D TexDSand()
    {
        return Make("d_tsand", 512, (u, v) =>
        {
            float ripple = Mathf.Sin((u * 0.4f + v + FBM(u, v, 5, 3, 411) * 0.5f) * Mathf.PI * 40f) * 0.03f;
            float t = 0.80f + ripple + (FBM(u, v, 10, 4, 412) - 0.5f) * 0.1f + (VN(u, v, 200, 200, 413) - 0.5f) * 0.05f;
            return Cl(t, t * 0.86f, t * 0.62f);
        });
    }

    static Texture2D TexDDune()
    {
        return Make("d_tdune", 512, (u, v) =>
        {
            float ripple = Mathf.Sin((u * 0.9f + v * 0.4f + FBM(u, v, 4, 3, 421) * 0.7f) * Mathf.PI * 30f) * 0.045f;
            float t = 0.74f + ripple + (FBM(u, v, 8, 4, 422) - 0.5f) * 0.12f + (VN(u, v, 220, 220, 423) - 0.5f) * 0.04f;
            return Cl(t * 1.0f, t * 0.76f, t * 0.46f);
        });
    }

    static Texture2D TexDRock()
    {
        return Make("d_trock", 512, (u, v) =>
        {
            float band = Mathf.Sin((v + FBM(u, v, 3, 3, 431) * 0.5f) * Mathf.PI * 9f);
            float fine = Mathf.Sin((v + FBM(u, v, 5, 3, 432) * 0.2f) * Mathf.PI * 40f) * 0.04f;
            float f1, f2; int id;
            Voro(u, v, 9, 433, out f1, out f2, out id);
            float t = 0.48f + band * 0.09f + fine + (FBM(u, v, 14, 5, 434) - 0.5f) * 0.18f;
            if (f2 - f1 < 0.03f) t *= 0.7f;
            float red = 0.5f + band * 0.5f;
            return Cl(t * (1.0f + red * 0.14f), t * (0.68f - red * 0.1f), t * (0.46f - red * 0.14f));
        });
    }

    static Texture2D TexDCrack()
    {
        return Make("d_tcrack", 512, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 11, 441, out f1, out f2, out id);
            float t = 0.62f + (Hash(id, 6, 441) - 0.5f) * 0.08f + (FBM(u, v, 12, 4, 442) - 0.5f) * 0.12f;
            float edge = Mathf.SmoothStep(0.0f, 0.06f, f2 - f1);
            t *= 0.5f + 0.5f * edge;
            return Cl(t * 0.96f, t * 0.78f, t * 0.58f);
        });
    }

    static Texture2D TexDGravel()
    {
        return Make("d_tgravel", 512, (u, v) =>
        {
            float f1, f2; int id;
            Voro(u, v, 70, 451, out f1, out f2, out id);
            float t = 0.42f + (Hash(id, 8, 451) - 0.5f) * 0.3f - f1 * 0.4f + (FBM(u, v, 12, 3, 452) - 0.5f) * 0.1f;
            return Cl(t * 0.98f, t * 0.86f, t * 0.7f);
        });
    }

    static Texture2D TexDDryGrass()
    {
        return Make("d_tdry", 512, (u, v) =>
        {
            float n = FBM(u, v, 6, 5, 461), blade = VN(u, v, 170, 80, 462), fine = VN(u, v, 210, 210, 463);
            float t = 0.5f + (n - 0.5f) * 0.25f + (blade - 0.5f) * 0.12f + (fine - 0.5f) * 0.06f;
            return Cl(t * 0.95f, t * 0.82f, t * 0.4f);
        });
    }

    static Texture2D TexDOasis()
    {
        return Make("d_toasis", 512, (u, v) =>
        {
            float n = FBM(u, v, 8, 5, 471), blade = VN(u, v, 170, 70, 472), fine = VN(u, v, 200, 200, 473);
            float r = 0.18f + n * 0.10f + (blade - 0.5f) * 0.05f;
            float g = 0.36f + n * 0.20f + (blade - 0.5f) * 0.09f + (fine - 0.5f) * 0.05f;
            float b = 0.12f + n * 0.04f;
            return Cl(r, g, b);
        });
    }

    /// <summary>Rumput kering / semak gurun (billboard detail).</summary>
    public static Texture2D TexDryTuft()
    {
        return Make("d_drytuft", 128, (u, v) =>
        {
            float cov = 0f, shade = 0f;
            for (int i = 0; i < 12; i++)
            {
                float x0 = 0.12f + 0.76f * Hash(i, 1, 481);
                float lean = (Hash(i, 2, 481) - 0.5f) * 0.7f;
                float hgt = 0.5f + 0.48f * Hash(i, 3, 481);
                float w = 0.022f + 0.016f * Hash(i, 4, 481);
                float yy = 1f - v;
                if (yy > hgt) continue;
                float tt = yy / hgt;
                float cx = x0 + lean * tt * tt;
                float half = w * (1f - tt * 0.9f);
                if (Mathf.Abs(u - cx) < half) { cov = 1f; shade = Mathf.Max(shade, 0.55f + 0.45f * tt); }
            }
            return new Color(0.62f * shade + 0.1f, 0.52f * shade + 0.08f, 0.22f * shade, cov);
        }, true);
    }

    // ------------------------------------------------------------------ material
    public static void LoadDesert()
    {
        if (dLoaded && DSlots != null) return;
        Load();
        var adobe = TexAdobe(); var sand = TexSandstone(); var basalt = TexBasalt(); var srock = TexSandRock(); var drift = TexDrift();
        var palmBark = TexPalmBark(); var frond = TexFrond(); var cactus = TexCactus(); var dry = TexDry(); var reed = TexReed(); var rug = TexRug();
        dSandT = TexDSand(); dDuneT = TexDDune(); dRockT = TexDRock(); dCrackT = TexDCrack(); dGravelT = TexDGravel(); dDryGrassT = TexDDryGrass(); dOasisT = TexDOasis();

        DSlots = (Material[])Slots.Clone();
        Func<string, Color, Texture2D, float, float, Material> D = (n, c, t, tile, sm) => TownMats.Mk("D_" + n, c, t, tile, sm, 0f);

        DSlots[Sl.Plaster] = D("Adobe", Cl(1f, 0.97f, 0.9f), adobe, 2.4f, 0.03f);
        DSlots[Sl.Stone] = D("Sandstone", Cl(1f, 0.97f, 0.92f), sand, 2.2f, 0.05f);
        DSlots[Sl.DarkStone] = D("Basalt", Cl(1f, 1f, 1f), basalt, 2.4f, 0.12f);
        DSlots[Sl.Wood] = D("DryWood", Cl(0.82f, 0.62f, 0.42f), TexWeathered(), 1.8f, 0.03f);
        DSlots[Sl.Roof] = D("Terracotta", Cl(0.9f, 0.52f, 0.34f), adobe, 2.4f, 0.1f);
        DSlots[Sl.RoofB] = DSlots[Sl.Roof];
        DSlots[Sl.Thatch] = D("ReedMat", Cl(1f, 0.95f, 0.8f), reed, 1.4f, 0.02f);
        DSlots[Sl.Cloth] = D("ClothTerra", Cl(1f, 0.9f, 0.82f), rug, 1.2f, 0.03f);
        DSlots[Sl.ClothB] = D("ClothIndigo", Cl(0.55f, 0.65f, 1f), rug, 1.2f, 0.03f);
        DSlots[Sl.Marble] = D("Bone", Cl(0.92f, 0.9f, 0.82f), TexAged("d_bone", 501, 0.82f, false), 1.6f, 0.05f);
        DSlots[Sl.Dirt] = D("DryDirt", Cl(1f, 0.95f, 0.85f), dCrackT, 4f, 0.02f);
        DSlots[Sl.Cobble] = D("Flagstone", Cl(1f, 0.95f, 0.88f), sand, 3f, 0.06f);

        DSlots[Bark] = D("PalmBark", Cl(1f, 1f, 1f), palmBark, 2f, 0.04f);
        DSlots[BarkBirch] = D("PalePole", Cl(0.95f, 0.9f, 0.82f), TexWeathered(), 2f, 0.03f);
        DSlots[BarkDead] = D("Bleached", Cl(1f, 0.96f, 0.9f), TexWeathered(), 2f, 0.03f);
        DSlots[LeafGreen] = D("Frond", Cl(0.78f, 0.9f, 0.5f), frond, 1.4f, 0.12f);
        DSlots[LeafPine] = D("Cactus", Cl(0.88f, 1f, 0.82f), cactus, 1.5f, 0.1f);
        DSlots[LeafYellow] = D("DryBrush", Cl(0.95f, 0.88f, 0.55f), dry, 1.5f, 0.02f);
        DSlots[LeafOrange] = D("BloomOrange", Cl(0.95f, 0.5f, 0.15f), dry, 1.5f, 0.08f);
        DSlots[LeafRed] = D("BloomRed", Cl(0.8f, 0.16f, 0.14f), dry, 1.5f, 0.08f);
        DSlots[Moss] = D("SandDrift", Cl(1f, 0.96f, 0.84f), drift, 2.6f, 0.02f);
        DSlots[Rock] = D("RedSandRock", Cl(1f, 0.95f, 0.9f), srock, 2.6f, 0.05f);
        DSlots[Ivy] = D("DesertVine", Cl(0.5f, 0.64f, 0.34f), frond, 1.2f, 0.1f);
        DSlots[Weathered] = D("Bleached2", Cl(0.95f, 0.9f, 0.82f), TexWeathered(), 2f, 0.03f);
        DSlots[Grime] = D("SunPlaster", Cl(0.94f, 0.85f, 0.7f), TexAged("d_grime2", 511, 0.78f, true), 3f, 0.03f);
        DSlots[OldRoof] = D("OldAdobeRoof", Cl(0.82f, 0.68f, 0.52f), adobe, 2.4f, 0.03f);
        DSlots[OldThatch] = D("OldReed", Cl(0.8f, 0.72f, 0.52f), reed, 1.4f, 0.02f);
        DSlots[Ash] = D("Soot", Cl(0.18f, 0.15f, 0.12f), basalt, 2f, 0.02f);
        DSlots[Crystal] = TownMats.Emissive(D("SpiritStone", Cl(0.3f, 0.95f, 0.9f), null, 1f, 0.85f), Cl(0.2f, 1.2f, 1.1f));
        dLoaded = true;
    }
}
