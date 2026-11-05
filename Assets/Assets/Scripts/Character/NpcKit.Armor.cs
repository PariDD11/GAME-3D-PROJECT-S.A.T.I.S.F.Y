using System.Collections.Generic;
using UnityEngine;

public static partial class NpcKit
{
    // Armor badan: 1 jerkin kulit, 2 kulit serigala (berbulu), 3 baju zirah besi, 4 zirah ksatria (emas + tabard + jubah)
    // Armor kepala: 1 topi kulit, 2 helm besi, 3 helm ksatria berjambul.   Armor kaki: 1 bot kulit, 2 bot besi + greaves, 3 greaves ksatria.
    static void ArmorBody(GeoBag b, Ctx c)
    {
        Spec s = c.s; int t = s.armBody;
        if (t <= 2)
        {
            Vest(b, c, Sl.Leather, Sl.Dark, 1.39f, 0.78f, 0.165f, 0.032f);                         // jerkin kulit berpanel
            TorsoLoft(b, c, Sl.CoatC, 0.70f, 0.80f, 0.034f, 0.18f, false, false, 0.05f);       // rumbai bawah
            Belt(b, c, 0.96f, Sl.Leather, 0.046f);
            b.Beam(Sl.Dark, new Vector3(-0.15f * c.W, c.Y(1.38f), FrontZ(c, 1.3f, 0.034f)), new Vector3(0.15f * c.W, c.Y(1.0f), FrontZ(c, 1.0f, 0.034f)), 0.026f, 0.012f, Vector3.forward);
            for (int i = 0; i < 3; i++) b.Box(Sl.Gold, new Vector3(0f, c.Y(1.14f - i * 0.1f), FrontZ(c, 1.14f - i * 0.1f, 0.034f) + 0.004f), new Vector3(0.02f, 0.02f, 0.008f));
            Collar(b, c, t == 2 ? Sl.Fur : Sl.Leather, 0.04f, t == 2 ? 0.12f : 0.07f, t == 2 ? 0.08f : 0.03f);
            if (t == 2)
            {
                Trim(b, c, Sl.Fur, 0.70f, 0.036f, 0.18f);
                TorsoLoft(b, c, Sl.Fur, 1.0f, 1.04f, 0.044f, 0f);
                b.Box(Sl.EyeW, new Vector3(0f, c.Y(1.2f), FrontZ(c, 1.2f, 0.036f) + 0.004f), new Vector3(0.05f, 0.05f, 0.008f), 45f);   // taring
            }
        }
        else
        {
            TorsoLoft(b, c, Sl.Metal, 0.98f, 1.42f, 0.040f, 0f, false, false, 0.04f);          // pelat dada
            for (int i = 0; i < 3; i++) TorsoLoft(b, c, Sl.Metal, 0.74f + i * 0.08f, 0.82f + i * 0.08f, 0.046f - i * 0.003f, 0f);   // pelat perut
            TorsoLoft(b, c, Sl.Metal, 0.50f, 0.82f, 0.040f, 0.35f, false, false, 0.05f);        // rok pelat
            Trim(b, c, Sl.Gold, 0.98f, 0.042f, 0f);
            Trim(b, c, Sl.Gold, 0.50f, 0.042f, 0.35f);
            Belt(b, c, 0.84f, Sl.Leather, 0.052f);
            b.Box(Sl.Gold, new Vector3(0f, c.Y(1.20f), FrontZ(c, 1.2f, 0.042f) + 0.004f), new Vector3(0.012f, 0.22f, 0.01f));      // rusuk dada
            Collar(b, c, Sl.Metal, 0.044f, 0.09f, 0.03f);
            // detail pelat: tonjolan dada, garis sambungan, paku keling
            float pk = c.fem ? 0.088f : 0.076f;
            for (int sx = -1; sx <= 1; sx += 2)
                b.Ball(Sl.Metal, new Vector3(sx * 0.074f * c.W, c.Y(1.27f), FrontZ(c, 1.27f, 0.04f) - 0.040f), pk, 5, 10, 0.78f);
            Trim(b, c, Sl.Dark, 1.15f, 0.043f, 0f);
            Trim(b, c, Sl.Dark, 0.88f, 0.047f, 0f);
            SeamBeams(b, c, Sl.Dark, 0.50f, 0.82f, 0.044f, 0.35f, 10, 0.31f);
            for (int i = 0; i < 9; i++)
            {
                float ra = (i - 4) / 4f * 1.15f;
                b.Ball(Sl.Gold, c.TPt(0.995f, ra, 0.046f, 0f), 0.0105f, 3, 5);
            }
            if (t >= 5)      // set area Eldrath: 5 Perbatasan (bulu + tanduk), 6 Bara (garis magma), 7 Tulang (rusuk), 8 Iblis (duri + mata)
            {
                int area = t - 5;
                foreach (float sx in new[] { -1f, 1f })
                {
                    float px = sx * 0.19f * c.W;
                    if (area == 0) { b.Beam(Sl.EyeW, new Vector3(px, c.Y(1.40f), 0f), new Vector3(px * 1.5f, c.Y(1.60f), 0f), 0.04f, 0.012f, Vector3.forward); b.Ball(Sl.Fur, new Vector3(px, c.Y(1.38f), 0f), 0.07f, 3, 6); }
                    else if (area == 1) { b.Box(Sl.Glow, new Vector3(sx * 0.07f * c.W, c.Y(1.22f), FrontZ(c, 1.2f, 0.044f) + 0.006f), new Vector3(0.012f, 0.30f, 0.008f), sx * 12f); b.Cone(Sl.DarkStone, new Vector3(px, c.Y(1.40f), 0f), 0.04f, 0.10f, 5); }
                    else if (area == 2) { for (int k = 0; k < 4; k++) b.Box(Sl.EyeW, new Vector3(sx * 0.07f * c.W, c.Y(1.30f - k * 0.08f), FrontZ(c, 1.3f - k * 0.08f, 0.044f) + 0.006f), new Vector3(0.10f, 0.02f, 0.008f), sx * 15f); }
                    else { b.Cone(Sl.DarkStone, new Vector3(px, c.Y(1.40f), 0f), 0.035f, 0.16f, 5); b.Ball(Sl.Glow, new Vector3(sx * 0.05f * c.W, c.Y(1.25f), FrontZ(c, 1.25f, 0.044f) + 0.006f), 0.02f, 3, 5); }
                }
                if (area == 0) { Trim(b, c, Sl.Fur, 0.98f, 0.05f, 0f); Cape(b, c, Sl.Leather, 0.30f, 0.12f, false); }
                if (area == 1) Cape(b, c, Sl.DarkStone, 0.32f, 0.10f, false);
                if (area == 2) Tabard(b, c, Sl.EyeW, 1.0f, 0.30f, 0.074f, 0.12f);
                if (area == 3) { Cape(b, c, Sl.DarkStone, 0.18f, 0.14f, false); Tabard(b, c, Sl.Glow, 1.0f, 0.34f, 0.074f, 0.12f); }
            }
            if (t == 4)
            {
                Tabard(b, c, Sl.CoatA, 1.0f, 0.24f, 0.074f, 0.12f);
                Cape(b, c, Sl.CoatC, 0.20f, 0.12f, false);
            }
        }
    }

    static void ApplyArmorDef(Ctx c, Def d)
    {
        Spec s = c.s;
        if (s.armBody > 0)
        {
            int t = s.armBody;
            d.vambrace = true;
            d.pauldron = t >= 3 ? 2 : 1;
            d.gloveSlot = t >= 3 ? Sl.Metal : Sl.Leather;
            d.vambraceSlot = t >= 3 ? Sl.Metal : Sl.Leather;
            if (d.sleeveEnd < 0f || d.sleeveEnd > 1.0f) d.sleeveEnd = 1.0f;
            d.sleeveSlot = t >= 3 ? Sl.CoatB : Sl.CoatA;
            if (d.cuffSlot >= 0 && t >= 3) d.cuffSlot = Sl.Gold;
        }
        if (s.armLegs > 0)
        {
            int t = s.armLegs;
            if (t == 1) { d.bootSlot = Sl.Leather; d.bootTop = 0.52f; }
            else if (t >= 4) { d.greaves = true; d.bootSlot = t == 4 ? Sl.Leather : t == 5 ? Sl.DarkStone : t == 6 ? Sl.EyeW : Sl.DarkStone; d.bootTop = 0.30f; }
            else { d.greaves = true; d.bootSlot = Sl.Metal; d.bootTop = 0.34f; }
            if (d.pantsSlot < 0) d.pantsSlot = Sl.CoatB;
        }
    }
}
