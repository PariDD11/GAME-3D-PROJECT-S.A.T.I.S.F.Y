using System.Collections.Generic;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kanon Eldrath (DungeonCity; lihat Docs/LORE_BIBLE.md §8.6). Dipanggil dari BuildDungeonCity() sesudah DBuildOutside() dan sebelum
/// DBuildNpcs(), dan TIDAK memakai UnityEngine.Random, supaya sebaran rumah, pohon, kristal, dan NPC yang sudah ada tidak bergeser.
///
///  1. Garis putih: jarum putih tipis dari mahkota menara sampai awan ("goresan putih lurus dari tanah sampai awan"). (file ini)
///  2. Kantor Pendaki: aula gerbang di sisi utara alun-alun dengan 2.400 goresan 3+3 di dindingnya. (.Kantor.cs, .Goresan.cs)
///  3. Lengkung Menara: tujuh pijakan aus di teras depan lengkung dan Papan Lantai (10-30 menghitung, 31 tanpa cahaya). (.Lengkung.cs)
///  4. Jalan Pengrajin Lilin: toko roti, pengrajin lilin, retakan enam kelopak, anjing kuning kurus. (.Lilin.cs, .Anjing.cs)
///  5. Tali lentera Festival Cahaya Salju di atas alun-alun. (.Festival.cs)
///
/// Tiga benda baca quest (nd_o_goresan, nd_o_lapar, nd_o_lantai) sebelumnya tidak punya penempatan di scene mana pun, sehingga quest
/// nd_k13, nd_k14, nd_k15 tidak bisa selesai. Builder ini memasangnya sebagai LoreObject (EcLore). Tidak ada NPC atau mekanik baru; hanya benda dan teks.
/// Teks tidak memakai "yang ketujuh", Jeda, si Sulung, Lucifer, atau nama dewa.
/// </summary>
public static partial class TownSceneBuilder
{
    static Transform tCanon;

    static void DungeonCanon()
    {
        tCanon = Group("KanonEldrath", tTown);
        EcStroke(tCanon);
        EcKantor(tCanon);
        EcLengkung(tCanon);
        EcLilin(tCanon);
        EcFestival(tCanon);
    }

    // ================================================================== pembantu
    /// <summary>Hash deterministik 0..1 (pengganti UnityEngine.Random).</summary>
    static float EcHash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
            h ^= h >> 15; h *= 0x85EBCA6Bu; h ^= h >> 13; h *= 0xC2B2AE35u; h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }

    /// <summary>Segiempat yang dijamin menghadap 'want' (urutan titik dibalik bila perlu).</summary>
    static void EcQ(GeoBag b, int slot, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 want)
    {
        Vector3 n = Vector3.Cross(p1 - p0, p2 - p0);
        if (Vector3.Dot(n, want) >= 0f) b.Quad(slot, p0, p1, p2, p3);
        else b.Quad(slot, p3, p2, p1, p0);
    }

    /// <summary>Salinan dSl dengan api hangat pada slot Glow (kristal Eldrath biru-langit tidak cocok untuk lilin dan jendela toko).</summary>
    static Material[] EcWarmSlots()
    {
        var s = (Material[])dSl.Clone();
        var fl = UMat("EldApiHangat", new Color(1f, 0.86f, 0.55f, 1f));
        s[Sl.Glow] = TownMats.Emissive(fl, new Color(1.15f, 0.92f, 0.55f, 1f));
        return s;
    }

    static void EcMarker(Transform p, float size)
    {
        Prim(PrimitiveType.Cylinder, "Marker", p, new Vector3(0f, 0.16f, 0f), new Vector3(size, 0.03f, size), mMarker, false);
    }

    static InfoPoint EcInfo(Transform parent, string name, Vector3 world, string prompt, float radius, string title, string message, bool marker)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = world;
        var ip = go.AddComponent<InfoPoint>();
        ip.prompt = prompt; ip.radius = radius; ip.title = title; ip.message = message;
        if (marker) EcMarker(go.transform, Mathf.Min(3.2f, radius * 0.7f));
        return ip;
    }

    /// <summary>Benda baca quest (Kodeks). Nama "LoreObject_id" sama dengan benda runtime StoryWorld, jadi mudah dicari.</summary>
    static LoreObject EcLore(Transform parent, string loreId, Vector3 world)
    {
        var go = new GameObject("LoreObject_" + loreId);
        go.transform.SetParent(parent, false);
        go.transform.position = world;
        var lo = go.AddComponent<LoreObject>();
        lo.loreId = loreId;
        EcMarker(go.transform, 2.4f);
        return lo;
    }

    static GameObject EcLight(Transform parent, string name, Vector3 world, float range, float intensity, Color c, bool flicker)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = world;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = c;
        if (flicker) { var f = go.AddComponent<FlickerLight>(); f.amount = 0.10f; f.speed = 2.2f; }
        return go;
    }

    // ================================================================== 1. garis putih
    /// <summary>
    /// Jarum putih tipis dari mahkota menara (puncak kristal sekitar 124,6 m) sampai 1.600 m. Dari jauh, di balik kabut kota,
    /// hanya garis inilah yang tampak: "begitu tipis sampai mata mengira langit retak". Tidak melempar bayangan.
    /// </summary>
    static void EcStroke(Transform root)
    {
        var slots = (Material[])dSl.Clone();
        var white = UMat("EldGarisPutih", new Color(0.96f, 0.97f, 1f, 1f));
        slots[Sl.Glow] = TownMats.Emissive(white, new Color(0.85f, 0.9f, 1.0f, 1f));
        var b = new GeoBag();
        b.Cyl(Sl.Glow, new Vector3(0f, 118f, 0f), 1.5f, 1500f, 4, 0.22f, false, false);
        var go = b.Emit(root, "GarisPutih", slots);
        if (go != null)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
