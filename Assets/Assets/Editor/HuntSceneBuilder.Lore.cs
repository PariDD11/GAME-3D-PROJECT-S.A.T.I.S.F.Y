using UnityEngine;
using HunterGuild;

/// <summary>Prasasti lore (LoreStone) di tiap area Eldrath: satu di base camp, satu dekat sarang boss.</summary>
public static partial class HuntSceneBuilder
{
    static void ELore()
    {
        string camp, boss; Vector2 bossAt;
        switch (ea)
        {
            case 0: camp = "kelahiran_monster"; boss = "wadah_behemoth"; bossAt = V(186f, -78f); break;
            case 1: camp = "tujuh_dewa"; boss = "wadah_naga"; bossAt = V(-6f, 230f); break;
            case 2: camp = "dark_mana"; boss = "wadah_cerberus"; bossAt = V(-7f, 252f); break;
            default: camp = "cemburu"; boss = "wadah_demon"; bossAt = V(-7f, 268f); break;
        }
        LoreStonePlace(V(ECamp.x + 22f, ECamp.y + 16f), camp);
        LoreStonePlace(bossAt, boss);
    }

    static void LoreStonePlace(Vector2 d, string loreId)
    {
        if (GroundY(d.x, d.y) < WaterY + 0.5f) return;
        var g = new GameObject("Prasasti_" + loreId).transform;
        g.SetParent(tProps, false);
        g.position = G(d.x, d.y);
        Color glow = ea == 3 ? new Color(0.8f, 0.3f, 1f) : ea == 2 ? new Color(0.5f, 0.85f, 1f) : ea == 1 ? new Color(1f, 0.55f, 0.2f) : new Color(0.7f, 1f, 0.7f);
        var stone = Mat("PrasastiStone", new Color(0.32f, 0.31f, 0.33f));
        var rune = TownMats.Emissive(Mat("PrasastiRune" + ea, glow * 0.6f), glow * 1.4f);
        Prim(PrimitiveType.Cube, "Alas", g, new Vector3(0, 0.2f, 0), new Vector3(2.4f, 0.4f, 1.8f), stone, true);
        Prim(PrimitiveType.Cube, "Batu", g, new Vector3(0, 1.7f, 0), new Vector3(1.5f, 2.6f, 0.5f), stone, true);
        var top = Prim(PrimitiveType.Cube, "Puncak", g, new Vector3(0, 3.1f, 0), new Vector3(1.1f, 0.5f, 0.45f), stone, false);
        top.transform.localRotation = Quaternion.Euler(0, 0, 8f);
        for (int i = 0; i < 4; i++) Prim(PrimitiveType.Cube, "Rune", g, new Vector3(0, 1.2f + i * 0.55f, 0.27f), new Vector3(0.7f - (i % 2) * 0.25f, 0.08f, 0.04f), rune, false);
        ELight(g, g.position + Vector3.up * 2.4f, glow, 12f, 1.5f, false);
        var st = g.gameObject.AddComponent<LoreStone>();
        st.loreId = loreId; st.prompt = "Baca prasasti kuno"; st.radius = 3.4f;
        clearings.Add(new Vector3(d.x, d.y, 6f));
    }
}
