using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// n43: struktur khusus scene hunt. Arena Ujian & Menara Eldrath dulu hanya kubus/silinder polos: sekarang visualnya dibangun dari GeoBag
/// (fasad bata berelief, lengkung, tribun bertumpu, patung, gerbang berterali, mozaik lantai, lampu gantung). Primitif lama tetap ada sebagai
/// collider (renderer dimatikan) supaya perilaku fisik tidak berubah. Juga detail tambahan untuk kuil/gua Vulkanik dan kit Eldrath.
/// </summary>
public static partial class HuntSceneBuilder
{
    static Vector3 SV(float x, float y, float z) { return new Vector3(x, y, z); }

    static Material[] StructSlots(Color stone, Color dark, Color marble, Color sand, string key)
    {
        ForestKit.Load();
        var s = (Material[])ForestKit.Slots.Clone();
        s[Sl.Stone] = Mat(key + "Stone", stone);
        s[Sl.DarkStone] = Mat(key + "Dark", dark);
        s[Sl.Marble] = Mat(key + "Marble", marble);
        s[Sl.Dirt] = Mat(key + "Sand", sand);
        return s;
    }

    static GameObject SKit(Material[] slots, string key, System.Action<GeoBag> build, Transform parent, Vector3 pos, float yaw)
    {
        var proto = PropKit.Get("S_" + key, build);
        return PropKit.Place(proto, parent, key, pos, yaw, 1f, slots);
    }

    static void HideRenderers(Transform root, params string[] prefixes)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            foreach (var p in prefixes)
                if (r.gameObject.name.StartsWith(p)) { r.enabled = false; break; }
    }

    // ================================================================== ARENA UJIAN
    static void ArenaVisual(Transform root)
    {
        var slots = StructSlots(new Color(0.72f, 0.68f, 0.6f), new Color(0.4f, 0.37f, 0.35f), new Color(0.9f, 0.87f, 0.8f), new Color(0.78f, 0.67f, 0.46f), "ArenaV");
        slots[Sl.Cloth] = Mat("ArenaVRed", new Color(0.72f, 0.12f, 0.12f));
        slots[Sl.ClothB] = Mat("ArenaVBlue", new Color(0.15f, 0.27f, 0.68f));
        HideRenderers(root, "Wall", "Seat", "Pillar", "Banner", "GateL", "GateR", "GateTop");
        SKit(slots, "ArenaFacade", ArenaFacadeKit, root, Vector3.zero, 0f);
        SKit(slots, "ArenaTiers", ArenaTiersKit, root, Vector3.zero, 0f);
        SKit(slots, "ArenaFloor", ArenaFloorKit, root, Vector3.zero, 0f);
    }

    static void ArenaFacadeKit(GeoBag b)
    {
        float R = ArenaR; int n = 32;
        float chord = 2f * Mathf.PI * (R + 1.2f) / n;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            Vector3 dir = SV(Mathf.Cos(a), 0f, Mathf.Sin(a));
            float yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            bool gate = Mathf.Abs(Mathf.Abs(dir.z) - 1f) < 0.12f && Mathf.Abs(dir.x) < 0.2f;
            b.Push(dir * (R + 0.1f), yaw);
            FantasyKit.Wall(b, Sl.Stone, SV(0, 0, -1.2f), SV(chord + 0.3f, 4.4f, 2.4f), 1);
            b.Box(Sl.DarkStone, SV(0, 0.45f, 0.1f), SV(chord + 0.35f, 0.9f, 0.3f));
            b.Box(Sl.Marble, SV(0, 4.5f, 0.1f), SV(chord + 0.4f, 0.35f, 0.7f));
            b.Box(Sl.Gold, SV(0, 4.3f, 0.42f), SV(chord * 0.9f, 0.1f, 0.06f));
            if (i % 2 == 0) b.Box(Sl.Stone, SV(0, 5.15f, -0.2f), SV(chord * 0.58f, 0.95f, 0.7f));
            if (!gate)
            {
                BuildKit.ArchWindow(b, SV(0, 1.1f, 0.02f), chord * 0.4f, 2.7f, Sl.Gold);
                b.Box(Sl.Marble, SV(0, 1.05f, 0.22f), SV(chord * 0.5f, 0.12f, 0.3f));
                if (i % 4 == 2) BuildKit.Banner(b, SV(0, 4.1f, 0.2f), 1.2f, 3.4f, (i / 4) % 2 == 0 ? Sl.Cloth : Sl.ClothB, true);
            }
            b.Pop();
        }
        for (int i = 0; i < n; i += 4)
        {
            float a2 = (i + 0.5f) / n * Mathf.PI * 2f;
            Vector3 d2 = SV(Mathf.Cos(a2), 0f, Mathf.Sin(a2));
            Vector3 p = d2 * (R + 0.3f);
            BuildKit.Column(b, p, 0.6f, 5f, Sl.Marble);
            float yaw2 = Mathf.Atan2(-d2.x, -d2.z) * Mathf.Rad2Deg;
            if (i % 8 == 0) PropKit.KnightStatue(b, p + SV(0, 5.3f, 0), yaw2);
            b.Cyl(Sl.Gold, p + SV(0, 4.9f, 0) - d2 * 0.9f, 0.18f, 0.12f, 8, 0.28f, true);
            b.Ball(Sl.Glow, p + SV(0, 5.15f, 0) - d2 * 0.9f, 0.2f, 4, 6);
        }
        // dua gerbang berterali: utara & selatan
        foreach (float sz in new[] { -1f, 1f })
        {
            b.Push(SV(0, 0, sz * (R + 0.4f)), sz > 0 ? 180f : 0f);
            foreach (float sx in new[] { -1f, 1f })
            {
                BuildKit.Column(b, SV(sx * 4.4f, 0, 0), 0.8f, 7.2f, Sl.Marble);
                b.Cone(Sl.Gold, SV(sx * 4.4f, 7.5f, 0), 0.45f, 1.1f, 6);
            }
            b.Box(Sl.Marble, SV(0, 7.7f, 0), SV(10.4f, 0.9f, 1.6f));
            b.Box(Sl.Gold, SV(0, 8.4f, 0.1f), SV(10.8f, 0.3f, 1.8f));
            b.Box(Sl.Cloth, SV(0, 7.7f, 0.82f), SV(3.0f, 0.55f, 0.06f));
            for (int k = -4; k <= 4; k++) b.Box(Sl.Metal, SV(k * 0.8f, 3.2f, 0.3f), SV(0.1f, 6.4f, 0.1f));
            for (int k = 0; k < 4; k++) b.Box(Sl.Metal, SV(0, 0.9f + k * 1.6f, 0.3f), SV(7.2f, 0.12f, 0.12f));
            for (int k = -4; k <= 4; k++) b.Cone(Sl.Metal, SV(k * 0.8f, 6.2f, 0.3f), 0.07f, 0.35f, 4);
            b.Pop();
        }
    }

    static void ArenaTiersKit(GeoBag b)
    {
        float R = ArenaR; int n = 32;
        float chord = 2f * Mathf.PI * (R + 1.2f) / n;
        for (int r = 0; r < 4; r++)
        {
            float rr = R + 3.4f + r * 2.2f;
            float H = 5.0f + r * 1.5f;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 dir = SV(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
                float c2 = chord * (1f + r * 0.09f);
                b.Push(dir * rr, yaw);
                b.BoxB(r % 2 == 0 ? Sl.Stone : Sl.DarkStone, SV(0, 0, 0), SV(c2 + 0.05f, H, 2.2f));
                b.Box(Sl.WoodLight, SV(0, H + 0.06f, 0.3f), SV(c2 * 0.94f, 0.12f, 1.3f));
                b.Box(Sl.Wood, SV(0, H + 0.4f, -0.4f), SV(c2 * 0.94f, 0.12f, 0.12f));
                if (i % 8 == 0) b.Box(Sl.Cloth, SV(0, H + 0.02f, 0.4f), SV(c2 * 0.5f, 0.05f, 1.5f));
                b.Pop();
            }
        }
        // tangga lorong tiap 8 segmen
        for (int i = 0; i < n; i += 8)
        {
            float a = (i + 0.5f) / n * Mathf.PI * 2f;
            Vector3 dir = SV(Mathf.Cos(a), 0f, Mathf.Sin(a));
            float yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            for (int r = 0; r < 4; r++)
            {
                b.Push(dir * (R + 3.4f + r * 2.2f - 0.9f), yaw);
                b.Box(Sl.Gold, SV(0, 5.0f + r * 1.5f + 0.1f, 0f), SV(1.4f, 0.1f, 1.0f));
                b.Pop();
            }
        }
        // balkon kehormatan di utara
        b.Push(SV(0, 0, R + 3.4f), 180f);
        b.Box(Sl.Marble, SV(0, 5.5f, 0.2f), SV(11f, 0.5f, 3.4f));
        for (int k = -5; k <= 5; k++) b.Cyl(Sl.Marble, SV(k * 1.0f, 5.75f, 1.6f), 0.12f, 1.0f, 6, 0.1f, true);
        b.Box(Sl.Gold, SV(0, 6.8f, 1.6f), SV(11.2f, 0.16f, 0.3f));
        b.Pop();
    }

    static void ArenaFloorKit(GeoBag b)
    {
        float R = ArenaR;
        b.Annulus(Sl.Gold, SV(0, 0, 0), R * 0.9f - 0.3f, R * 0.9f, 0f, 360f, 64, 0.075f);
        b.Annulus(Sl.Stone, SV(0, 0, 0), R * 0.9f - 1.6f, R * 0.9f - 1.0f, 0f, 360f, 64, 0.072f);
        b.Annulus(Sl.Gold, SV(0, 0, 0), R * 0.35f - 0.25f, R * 0.35f, 0f, 360f, 48, 0.078f);
        for (int k = 0; k < 8; k++)
        {
            b.Push(SV(0, 0, 0), k * 22.5f);
            b.Box(Sl.Stone, SV(0, 0.08f, 0), SV(0.5f, 0.03f, R * 0.8f));
            b.Pop();
        }
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            b.Cone(Sl.Gold, SV(Mathf.Sin(a) * R * 0.35f, 0.06f, Mathf.Cos(a) * R * 0.35f), 0.55f, 0.04f, 4);
        }
        b.Disc(Sl.Marble, SV(0, 0, 0), 2.6f, 32, 0.08f);
        b.Annulus(Sl.Gold, SV(0, 0, 0), 2.3f, 2.6f, 0f, 360f, 32, 0.085f);
    }

    // ================================================================== MENARA ELDRATH
    static void TowerVisual(Transform root)
    {
        var slots = StructSlots(new Color(0.4f, 0.39f, 0.45f), new Color(0.2f, 0.2f, 0.25f), new Color(0.62f, 0.6f, 0.68f), new Color(0.3f, 0.3f, 0.33f), "TowerV");
        slots[Sl.ClothB] = Mat("TowerVCloth", new Color(0.35f, 0.15f, 0.55f));
        HideRenderers(root, "Wall", "Pillar");
        SKit(slots, "TowerHall", TowerHallKit, root, Vector3.zero, 0f);
        SKit(slots, "TowerFloorArt", TowerFloorKit, root, Vector3.zero, 0f);
        SKit(slots, "TowerVault", TowerVaultKit, root, Vector3.zero, 0f);
    }

    static void TowerHallKit(GeoBag b)
    {
        float R = TowerR; int n = 28;
        float chord = 2f * Mathf.PI * (R + 1.5f) / n;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            Vector3 dir = SV(Mathf.Cos(a), 0f, Mathf.Sin(a));
            float yaw = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg;
            b.Push(dir * (R + 0.05f), yaw);
            FantasyKit.Wall(b, Sl.Stone, SV(0, 0, -1.5f), SV(chord + 0.4f, 16f, 3f), 1);
            b.Box(Sl.DarkStone, SV(0, 0.6f, 0.12f), SV(chord + 0.4f, 1.2f, 0.4f));
            for (int k = 1; k <= 4; k++) b.Box(Sl.DarkStone, SV(0, k * 3.2f, 0.04f), SV(chord + 0.4f, 0.16f, 0.1f));
            b.Box(Sl.Marble, SV(0, 15.6f, 0.2f), SV(chord + 0.5f, 0.8f, 0.7f));
            b.Box(Sl.Gold, SV(0, 15.0f, 0.1f), SV(chord * 0.9f, 0.12f, 0.06f));
            for (int k = 0; k < 4; k++) b.Box(Sl.Marble, SV(-chord * 0.35f + k * chord * 0.23f, 15.1f, 0.3f), SV(0.3f, 0.5f, 0.35f));
            if (i % 2 == 0)
            {
                BuildKit.ArchWindow(b, SV(0, 2.6f, 0.02f), 2.6f, 8.4f, Sl.Gold);
                b.Box(Sl.Marble, SV(0, 2.5f, 0.3f), SV(3.4f, 0.2f, 0.5f));
            }
            else
            {
                BuildKit.Banner(b, SV(0, 13.2f, 0.12f), 2.0f, 8f, Sl.ClothB, true);
            }
            b.Pop();
        }
        for (int i = 0; i < n; i++)
        {
            float a2 = (i + 0.5f) / n * Mathf.PI * 2f;
            Vector3 d2 = SV(Mathf.Cos(a2), 0f, Mathf.Sin(a2));
            Vector3 p = d2 * (R - 0.4f);
            BuildKit.Column(b, p, 0.95f, 15f, Sl.Marble);
            if (i % 2 == 0)
            {
                Vector3 tp = d2 * (R - 1.7f);
                b.Cyl(Sl.DarkStone, tp + SV(0, 4.2f, 0), 0.4f, 0.25f, 8, 0.55f, true);
                b.Ball(Sl.Glow, tp + SV(0, 4.95f, 0), 0.33f, 4, 8, 1.2f);
            }
        }
    }

    static void TowerFloorKit(GeoBag b)
    {
        float R = TowerR;
        b.Annulus(Sl.Gold, SV(0, 0, 0), 9.0f, 9.35f, 0f, 360f, 64, 0.09f);
        b.Annulus(Sl.Gold, SV(0, 0, 0), 18.0f, 18.4f, 0f, 360f, 72, 0.09f);
        b.Annulus(Sl.Marble, SV(0, 0, 0), R - 2.5f, R - 1.2f, 0f, 360f, 96, 0.085f);
        b.Annulus(Sl.DarkStone, SV(0, 0, 0), 9.4f, 17.9f, 0f, 360f, 72, 0.082f);
        for (int k = 0; k < 12; k++)
        {
            b.Push(SV(0, 0, 0), k * 30f);
            b.Box(Sl.Marble, SV(0, 0.1f, 13.5f), SV(0.35f, 0.04f, 8.4f));
            b.Cone(Sl.Gold, SV(0, 0.1f, 18.8f), 0.9f, 0.04f, 4);
            b.Pop();
        }
        b.Disc(Sl.Marble, SV(0, 0, 0), 4.2f, 40, 0.1f);
        b.Annulus(Sl.Gold, SV(0, 0, 0), 3.8f, 4.2f, 0f, 360f, 40, 0.105f);
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            b.Cone(Sl.Glow, SV(Mathf.Sin(a) * 6.5f, 0.1f, Mathf.Cos(a) * 6.5f), 0.35f, 0.05f, 5);
        }
    }

    static void TowerVaultKit(GeoBag b)
    {
        float R = TowerR;
        // rusuk kubah di bawah langit-langit + lampu gantung
        for (int k = 0; k < 16; k++)
        {
            float a = k * 22.5f * Mathf.Deg2Rad;
            b.Beam(Sl.DarkStone, SV(0, 16.4f, 0), SV(Mathf.Sin(a) * (R + 1f), 16.4f, Mathf.Cos(a) * (R + 1f)), 0.6f, 0.5f, Vector3.up);
        }
        b.Annulus(Sl.Gold, SV(0, 0, 0), R * 0.5f - 0.2f, R * 0.5f + 0.2f, 0f, 360f, 64, 16.35f);
        b.Ball(Sl.Gold, SV(0, 16.4f, 0), 1.2f, 4, 10, 0.6f);
        b.Beam(Sl.Metal, SV(0, 16.2f, 0), SV(0, 11.5f, 0), 0.12f, 0.12f);
        b.Cyl(Sl.Gold, SV(0, 11.2f, 0), 0.5f, 0.3f, 10, 0.3f, true);
        for (int k = 0; k < 12; k++)
        {
            float a = k * 30f * Mathf.Deg2Rad;
            Vector3 p = SV(Mathf.Sin(a) * 2.6f, 11.5f, Mathf.Cos(a) * 2.6f);
            b.Beam(Sl.Gold, SV(0, 11.4f, 0), p, 0.1f, 0.1f);
            b.Cyl(Sl.Gold, p + SV(0, -0.3f, 0), 0.12f, 0.35f, 6, 0.1f, true);
            b.Ball(Sl.Glow, p + SV(0, 0.2f, 0), 0.22f, 4, 6, 1.2f);
        }
    }

    // ================================================================== VOLKANIK
    static void VTempleArt(GeoBag b)
    {
        // atap pelana gelap + paku obsidian di tepi, relief rune menyala, gargoyle, kuali api
        foreach (float sx in new[] { -1f, 1f })
        {
            for (int k = 0; k < 7; k++)
            {
                float z = 13.5f + k * 1.7f;
                b.Cone(Sl.Glass, SV(sx * 17.5f, 14f, z), 0.55f, 2.4f + (k % 3) * 0.6f, 5);
            }
            b.Box(Sl.Marble, SV(sx * 17.5f, 14.1f, 18f), SV(2.9f, 0.4f, 12f));
            for (int k = 0; k < 6; k++)
                b.Box(Sl.Glow, SV(sx * 16.28f, 4f + k * 1.6f, 14.5f + (k % 2) * 6f), SV(0.05f, 0.9f, 0.5f));
            foreach (float z in new[] { 2f, 8f, 14f })
            {
                b.Cyl(Sl.DarkStone, SV(sx * 13f, 11.8f, z), 0.9f, 0.5f, 8, 1.2f, true);
                b.Ball(Sl.Glow, SV(sx * 13f, 13f, z), 0.7f, 5, 8, 1.3f);
                b.Cyl(Sl.Marble, SV(sx * 13f, 0.3f, z), 1.55f, 0.5f, 8, 1.45f, true);
                for (int g = 0; g < 8; g++)
                {
                    float a = g * 45f * Mathf.Deg2Rad;
                    b.Beam(Sl.Stone, SV(sx * 13f + Mathf.Sin(a) * 1.18f, 1.2f, z + Mathf.Cos(a) * 1.18f), SV(sx * 13f + Mathf.Sin(a) * 0.98f, 9.6f, z + Mathf.Cos(a) * 0.98f), 0.1f, 0.1f, Vector3.up);
                }
            }
        }
        // gargoyle di dinding depan
        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 p = SV(sx * 6.2f, 11.2f, 24.4f);
            b.Box(Sl.DarkStone, p, SV(1.0f, 0.9f, 1.4f));
            b.Ball(Sl.DarkStone, p + SV(0, 0.3f, 0.9f), 0.5f, 4, 7);
            b.Cone(Sl.Glass, p + SV(sx * 0.4f, 0.8f, 0.8f), 0.12f, 0.6f, 4);
            b.Cone(Sl.Glass, p + SV(-sx * 0.4f, 0.8f, 0.8f), 0.12f, 0.6f, 4);
            b.Ball(Sl.Glow, p + SV(0.15f, 0.4f, 1.35f), 0.08f, 3, 5);
            b.Ball(Sl.Glow, p + SV(-0.15f, 0.4f, 1.35f), 0.08f, 3, 5);
        }
        // lengkung gerbang api dengan rune
        for (int k = 0; k < 9; k++)
        {
            float a = Mathf.PI * k / 8f;
            b.Beam(Sl.Gold, SV(-3.9f * Mathf.Cos(a), 7.6f + Mathf.Sin(a) * 2.2f, 24.6f), SV(-3.9f * Mathf.Cos(a + Mathf.PI / 8f), 7.6f + Mathf.Sin(a + Mathf.PI / 8f) * 2.2f, 24.6f), 0.35f, 0.3f, Vector3.forward);
        }
        for (int k = 0; k < 5; k++) b.Box(Sl.Glow, SV(-3f + k * 1.5f, 8.4f, 24.45f), SV(0.12f, 0.5f + (k % 2) * 0.3f, 0.06f));
        // tangga depan altar
        b.Stairs(Sl.DarkStone, SV(0, 0.3f, 12.2f), 6f, 5, 0.22f, 0.5f, 180f);
        // pelat api menyala di lantai
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f * Mathf.Deg2Rad;
            b.Cone(Sl.Glow, SV(Mathf.Sin(a) * 3.6f, 0.36f, 5 + Mathf.Cos(a) * 3.6f), 0.28f, 0.04f, 4);
        }
        b.Annulus(Sl.Gold, SV(0, 0, 5), 4.3f, 4.5f, 0f, 360f, 36, 0.37f);
    }

    static void VCaveArt(GeoBag b)
    {
        // batuan bergumpal agar tidak terlihat seperti kerucut + stalaktit + kristal magma
        for (int k = 0; k < 10; k++)
        {
            float a = k * 36f * Mathf.Deg2Rad;
            ForestKit.Blob(b, k % 2 == 0 ? Sl.DarkStone : Sl.Stone, SV(Mathf.Sin(a) * 14f, 1.5f + (k % 3), Mathf.Cos(a) * 8f), SV(4f + (k % 3), 3.2f + (k % 4), 4f), 70 + k, 0.35f, 4, 8, 0.2f);
        }
        for (int k = 0; k < 7; k++)
            b.Cone(Sl.DarkStone, SV(-3.6f + k * 1.2f, 8.6f - (k % 2) * 0.3f, 14.0f), 0.28f, -1.4f - (k % 3) * 0.4f, 5);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Cone(Sl.Glow, SV(sx * 6.5f, 0, 15.5f), 0.4f, 2.2f, 5);
            b.Cone(Sl.Glow, SV(sx * 7.2f, 0, 15f), 0.28f, 1.4f, 5);
            for (int k = 0; k < 4; k++) b.Box(Sl.Glow, SV(sx * 4.2f, 1.2f + k * 1.7f, 14.9f), SV(0.05f, 0.7f, 0.5f));
        }
        for (int k = 0; k < 6; k++) b.Box(Sl.DarkStone, SV(-2.5f + k * 1.0f, 0.1f + k * 0.0f, 16.6f + (k % 2) * 0.3f), SV(0.9f, 0.2f, 1.0f), k * 11f);
    }

    static void VBrazierArt(GeoBag b)
    {
        for (int k = 0; k < 3; k++)
        {
            float a = k * 120f * Mathf.Deg2Rad;
            b.Beam(Sl.DarkStone, SV(Mathf.Sin(a) * 0.9f, 2.3f, Mathf.Cos(a) * 0.9f), SV(Mathf.Sin(a) * 1.1f, 0.05f, Mathf.Cos(a) * 1.1f), 0.2f, 0.18f, Vector3.up);
            b.Cone(Sl.Glass, SV(Mathf.Sin(a) * 0.85f, 2.9f, Mathf.Cos(a) * 0.85f), 0.1f, 0.7f, 4);
        }
        b.Annulus(Sl.Gold, SV(0, 0, 0), 0.82f, 0.95f, 0f, 360f, 16, 2.92f);
        for (int k = 0; k < 6; k++)
        {
            float a = k * 60f * Mathf.Deg2Rad;
            b.Ball(Sl.Glow, SV(Mathf.Sin(a) * 0.3f, 3.0f, Mathf.Cos(a) * 0.3f), 0.28f, 3, 6, 1.4f);
        }
        b.Cone(Sl.Glow, SV(0, 3.2f, 0), 0.45f, 1.6f, 6);
    }

    static void VLanternArt(GeoBag b)
    {
        for (int k = 0; k < 4; k++)
        {
            float a = k * 90f * Mathf.Deg2Rad;
            b.Beam(Sl.Metal, SV(Mathf.Sin(a) * 0.3f, 3.3f, Mathf.Cos(a) * 0.3f), SV(Mathf.Sin(a) * 0.3f, 4.1f, Mathf.Cos(a) * 0.3f), 0.05f, 0.05f, Vector3.up);
        }
        b.Cone(Sl.DarkStone, SV(0, 4.1f, 0), 0.5f, 0.55f, 6);
        b.Ball(Sl.Gold, SV(0, 4.7f, 0), 0.1f, 3, 6);
        b.Cyl(Sl.DarkStone, SV(0, 0.5f, 0), 0.28f, 0.4f, 6, 0.2f, true);
    }

    static void VVentArt(GeoBag b)
    {
        for (int k = 0; k < 9; k++)
        {
            float a = k * 40f * Mathf.Deg2Rad;
            ForestKit.Blob(b, Sl.DarkStone, SV(Mathf.Sin(a) * 2.6f, 0.45f, Mathf.Cos(a) * 2.6f), SV(0.9f, 0.7f, 0.9f), 90 + k, 0.35f, 3, 7, 0.3f);
        }
        b.Annulus(Sl.Glow, SV(0, 0, 0), 1.5f, 2.0f, 0f, 360f, 20, 0.94f);
    }

    // ================================================================== ELDRATH (kota/area)
    static void EGraveArt(GeoBag b, int v)
    {
        for (int k = 0; k < 6; k++) b.Box(Sl.Stone, SV(-0.45f + (k % 2) * 0.9f, 0.18f, -0.9f + (k / 2) * 0.9f), SV(0.14f, 0.3f, 0.14f));
        b.Box(Sl.Stone, SV(0, 0.04f, -0.9f), SV(1.3f, 0.12f, 0.5f));
        ForestKit.Blob(b, ForestKit.Moss, SV(0.2f, 0.08f, 0.4f), SV(0.5f, 0.12f, 0.6f), 40 + v, 0.3f, 3, 6, 0.3f);
        if (v == 1) for (int k = 0; k < 3; k++) b.Ball(Sl.Blossom, SV(-0.25f + k * 0.25f, 0.3f, 0.3f), 0.07f, 3, 5);
        if (v == 0) b.Ball(Sl.Stone, SV(0, 1.75f, -0.8f), 0.14f, 3, 6);
    }

    static void EMausoleumArt(GeoBag b)
    {
        // fasad: pilaster sudut, pintu lengkung, pedimen, tangga, patung penjaga, rune
        b.Stairs(Sl.Stone, SV(0, 0, 4.9f), 6f, 4, 0.25f, 0.45f, 0f);
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(Sl.Marble, SV(sx * 5.1f, 1.5f, 4.1f), SV(0.5f, 3f, 0.5f));
            b.Box(Sl.DarkStone, SV(sx * 5.1f, 3.15f, 4.1f), SV(0.7f, 0.3f, 0.7f));
            b.Ball(Sl.Glow, SV(sx * 3.4f, 2.1f, 4.4f), 0.2f, 3, 6);
            b.Cone(Sl.Marble, SV(sx * 5.1f, 3.3f, 4.1f), 0.28f, 1.1f, 4);
        }
        for (int k = 0; k < 9; k++)
        {
            float a = Mathf.PI * k / 8f;
            float a2 = Mathf.PI * (k + 1) / 8f;
            b.Beam(Sl.Marble, SV(-1.5f * Mathf.Cos(a), 2.4f + Mathf.Sin(a) * 1.0f, 4.3f), SV(-1.5f * Mathf.Cos(a2), 2.4f + Mathf.Sin(a2) * 1.0f, 4.3f), 0.25f, 0.3f, Vector3.forward);
        }
        b.Gable(Sl.DarkStone, Sl.Stone, Sl.Wood, SV(0, 3.2f, 4.6f), 6.8f, 1.2f, 1.2f, 0.2f, true);
        for (int k = 0; k < 5; k++) b.Box(Sl.Glow, SV(-1.2f + k * 0.6f, 2.95f, 4.95f), SV(0.1f, 0.3f, 0.04f));
        for (int k = 0; k < 4; k++) b.Cone(Sl.DarkStone, SV(-4.5f + k * 3f, 3.3f, -3.6f), 0.4f, 1.3f, 4);
    }

    static void EHornGateArt(GeoBag b)
    {
        foreach (float sx in new[] { -1f, 1f })
        {
            for (int k = 0; k < 6; k++) b.Cyl(Sl.DarkStone, SV(sx * 7f, k * 1.7f, 0), 1.7f - k * 0.08f, 0.35f, 7, 1.65f - k * 0.08f, false);
            for (int k = 0; k < 5; k++) b.Box(Sl.Glow, SV(sx * 5.5f, 1.5f + k * 1.8f, 0.4f), SV(0.08f, 0.7f, 0.06f));
            b.Cone(Sl.DarkStone, SV(sx * 8.1f, 6f, 0.2f), 0.5f, 3.4f, 5);
            b.Cone(Sl.DarkStone, SV(sx * 5.9f, 6.5f, -0.2f), 0.45f, 3f, 5);
            ForestKit.Blob(b, Sl.DarkStone, SV(sx * 7f, 0.4f, 0), SV(2.6f, 0.7f, 2.6f), 100 + (int)sx, 0.3f, 3, 8, 0.4f);
        }
        for (int k = -6; k <= 6; k++) b.Cone(Sl.DarkStone, SV(k * 1.1f, 11.2f, 0.2f), 0.3f, 1.5f + (k % 2 == 0 ? 0.8f : 0f), 4);
        for (int k = -3; k <= 3; k++) b.Cone(Sl.DarkStone, SV(k * 1.7f, 10.2f, 1.2f), 0.2f, -1.0f, 4);
        b.Annulus(Sl.Glow, SV(0, 0, 0), 0.6f, 1.2f, 0f, 360f, 16, 0.05f);
    }

    static void EDemonSpikeArt(GeoBag b, int v)
    {
        for (int k = 0; k < 5; k++)
        {
            float a = (k * 72f + v * 20f) * Mathf.Deg2Rad;
            b.Cone(Sl.DarkStone, SV(Mathf.Sin(a) * 1.3f, -0.4f, Mathf.Cos(a) * 1.3f), 0.35f, 1.6f + (k % 3) * 0.7f, 4);
        }
        ForestKit.Blob(b, Sl.DarkStone, SV(0, 0, 0), SV(1.9f, 0.5f, 1.9f), 120 + v, 0.3f, 3, 8, 0.4f);
        for (int k = 0; k < 3; k++) b.Box(Sl.Glow, SV(0.2f * k, 1.0f + k * 1.4f, 1.1f), SV(0.06f, 0.5f, 0.04f));
    }

    static void ECageArt(GeoBag b)
    {
        for (int k = 0; k < 3; k++)
        {
            float a = k * 120f * Mathf.Deg2Rad;
            b.Ball(Sl.Marble, SV(Mathf.Sin(a) * 0.6f, 0.15f, Mathf.Cos(a) * 0.6f), 0.15f, 3, 6, 0.8f);
        }
        b.Beam(Sl.Metal, SV(0, 3.3f, 0), SV(0, 4.4f, 0), 0.06f, 0.06f);
        b.Annulus(Sl.Metal, SV(0, 0, 0), 0.1f, 0.18f, 0f, 360f, 8, 4.4f);
    }
}
