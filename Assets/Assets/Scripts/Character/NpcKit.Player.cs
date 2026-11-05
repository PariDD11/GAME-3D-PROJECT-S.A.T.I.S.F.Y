using System.Collections.Generic;
using UnityEngine;

public static partial class NpcKit
{
    // ---- palet yang dibuka ke UI kustomisasi ----
    public static int SkinCount(int race) { return SkinPal[race].Length; }
    public static Color SkinColor(int race, int i) { var p = SkinPal[race]; return p[((i % p.Length) + p.Length) % p.Length]; }
    public static int HairColorCount { get { return HairPal.Length; } }
    public static Color HairColor(int i) { return HairPal[((i % HairPal.Length) + HairPal.Length) % HairPal.Length]; }
    public static int FabricCount { get { return FabricPal.Length; } }
    public static Color FabricColor(int i) { return FabricPal[((i % FabricPal.Length) + FabricPal.Length) % FabricPal.Length]; }
    public static Color FurColor(int kind, int i) { return FurPal[kind % 4][((i % 4) + 4) % 4]; }

    /// <summary>Spec untuk karakter pemain: gaya pakaian dari 'outfit', warna dan bagian tubuh dari pilihan pemain. hat: -1 = bawaan pakaian, 0 = tanpa topi, selain itu id topi.</summary>
    public static Spec PlayerSpec(string name, int race, bool female, int build, int skin, int hair, int hairCol, int beard, int beast, int outfit, int hat, int cA, int cB, int cC)
    {
        var s = new Spec();
        s.name = name; s.race = race; s.female = female; s.build = Mathf.Clamp(build, 0, 2);
        s.skin = skin; s.hair = hair; s.hairCol = hairCol; s.beard = female ? 0 : beard; s.beast = race == Beast ? ((beast % 4) + 4) % 4 : 0;
        s.outfit = outfit; s.eye = 0;
        s.cA = cA; s.cB = cB; s.cC = cC;
        PickOutfit(s, new System.Random(outfit * 31 + race));
        s.cA = cA; s.cB = cB; s.cC = cC;           // pilihan pemain menimpa warna bawaan
        s.hair = hair; s.prop = 0; s.elite = false; s.split = true;
        if (hat >= 0) s.hat = hat;
        s.role = OutfitName[outfit];
        return s;
    }

    /// <summary>Pasang visual armor sesuai item yang sedang dipakai (id item dari ItemDB).</summary>
    public static void ApplyEquipment(Spec s, string head, string body, string legs)
    {
        s.armHead = s.armBody = s.armLegs = 0;
        head = HunterGuild.GearTiers.VisualBase(head); body = HunterGuild.GearTiers.VisualBase(body); legs = HunterGuild.GearTiers.VisualBase(legs);
        switch (head)
        {
            case "leather_cap": s.armHead = 1; s.hat = 16; break;
            case "iron_cap": s.armHead = 2; s.hat = 4; break;
            case "knight_helm": s.armHead = 3; s.hat = 4; break;
            case "eld_head_0": case "eld_head_1": case "eld_head_2": case "eld_head_3": s.armHead = 4 + (head[9] - '0'); s.hat = 4; break;
        }
        switch (body)
        {
            case "leather_armor": s.armBody = 1; break;
            case "wolfhide_armor": s.armBody = 2; break;
            case "iron_armor": s.armBody = 3; break;
            case "knight_armor": s.armBody = 4; break;
            case "eld_body_0": case "eld_body_1": case "eld_body_2": case "eld_body_3": s.armBody = 5 + (body[9] - '0'); break;
        }
        switch (legs)
        {
            case "leather_boots": s.armLegs = 1; break;
            case "iron_boots": s.armLegs = 2; break;
            case "knight_greaves": s.armLegs = 3; break;
            case "eld_legs_0": case "eld_legs_1": case "eld_legs_2": case "eld_legs_3": s.armLegs = 4 + (legs[9] - '0'); break;
        }
    }
}
