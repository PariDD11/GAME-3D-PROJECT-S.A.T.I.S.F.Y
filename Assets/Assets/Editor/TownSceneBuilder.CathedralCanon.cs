using UnityEngine;
using HunterGuild;

/// <summary>
/// n89 (Docs/LORE_BIBLE.md 8.9): lapisan kanon Katedral Ashwatch di scene Kota. Dipanggil dari T2BuildTown() sesudah T2LoreProps().
/// (1) retak setipis rambut di muka Katedral ada di geometri (T2Kit.CathedralCracks, Town2Kit.CathedralCanon.cs);
/// (2) CathedralBell: lonceng berdentang enam kali tiap fajar dan senja, dipasang di atas menara depan;
/// (3) dua InfoPoint di pelataran depan (kiri tangga: retak; kanan tangga: lonceng), berregister rakyat (tanpa Jeda, yang ketujuh, Sulung, nama dewa).
/// Tanpa UnityEngine.Random.
/// </summary>
public static partial class TownSceneBuilder
{
    static InfoPoint T2CcInfo(Transform root, string name, Vector3 local, string prompt, string title, string message)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = local;
        var ip = go.AddComponent<InfoPoint>();
        ip.prompt = prompt; ip.radius = 3.4f; ip.title = title; ip.message = message;
        return ip;
    }

    public static string T2CcReport = "";

    static void T2CathedralCanon()
    {
        var go = GameObject.Find("Cathedral");
        if (go == null) { Debug.LogWarning("[Kota n89] Katedral tidak ditemukan; lapisan kanon dilewati."); T2CcReport = "TIDAK ADA KATEDRAL"; return; }
        var root = go.transform;

        // pelataran depan: sejajar InteractPoint bawaan Katedral, digeser ke kiri dan kanan tangga
        var ip0 = root.Find("InteractPoint");
        float zc = ip0 != null ? ip0.localPosition.z : 37f;

        var bell = new GameObject("Lonceng");
        bell.transform.SetParent(root, false);
        bell.transform.localPosition = new Vector3(0f, 34f, 24f);
        bell.AddComponent<CathedralBell>();

        T2CcInfo(root, "InteractPoint_RetakKatedral", new Vector3(-8.5f, 0f, zc), "Periksa Retakan", "Retak di Muka Katedral",
            "Garis-garis hitam setipis rambut merayap di batu putih muka Katedral, dari kaki tembok sampai setinggi tiga lantai. Tukang batu sudah menambalnya tiga kali sejak musim dingin; tiap pagi retak itu ada lagi, sehitam dan setipis semula.\n\nWarga menyarankan agar tak menaruh telapak tangan di dinding itu menjelang subuh. Tak seorang pun mau bilang apa sebabnya.");
        T2CcInfo(root, "InteractPoint_LoncengKatedral", new Vector3(8.5f, 0f, zc), "Periksa Menara Lonceng", "Lonceng Katedral",
            "Lonceng Katedral berdentang tiap fajar dan senja. Enam kali, sama panjang, lalu kota diam: pedagang menahan timbangan, penjaga berhenti melangkah, ibu-ibu menunggu sampai tujuh tarikan napas sebelum bicara lagi.\n\nAnak-anak diajari kebiasaan itu sebelum diajari berhitung. Yang baru datang ke kota biasanya tertawa pada hari pertama, dan berhenti tertawa pada hari ketiga.");

        T2CcReport = "KATEDRAL zc=" + zc.ToString("0.0") + " | Lonceng=1 | InfoPoint=2";
        Debug.Log("[Kota n89] " + T2CcReport);
    }
}
