using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Titik interaksi Guild (resepsionis, penilai, guild master, pelatih, pengawas ujian) dulu berupa lingkaran di lantai dekat meja.
    /// Sekarang dipasang ke NPC-nya sendiri: pemain berbicara dengan orangnya, bukan mejanya. Berjalan otomatis di scene lama tanpa rebuild.
    /// </summary>
    public class GuildNpcBind : MonoBehaviour
    {
        static readonly string[] Names = { "InteractPoint_Reception", "InteractPoint_Appraiser", "InteractPoint_GuildMaster", "InteractPoint_Trainer", "InteractPoint_Referee" };
        int tries;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded += (s, m) => Spawn();
            Spawn();
        }

        static void Spawn()
        {
            if (FindFirstObjectByType<GuildNpcBind>() != null) return;
            var go = new GameObject("GuildNpcBind");
            go.AddComponent<GuildNpcBind>();
        }

        void Update()
        {
            Bind();
            if (++tries > 5) Destroy(gameObject);
        }

        // penjual di interior/desa: titik toko dipindah ke NPC penjual terdekat (<= 6 m) bila ada; bangunan luar tanpa NPC dibiarkan
        static bool ShopNear(Interactable it)
        {
            if (it.GetComponentInChildren<Renderer>() != null || it.gameObject.GetComponent<Renderer>() != null) return false;   // bangunan landmark: jangan dicuri NPC lewat
            foreach (var a in FindObjectsByType<NpcActor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (Vector3.Distance(a.transform.position, it.transform.position) <= 6f) return true;
            return false;
        }

        static void Bind()
        {
            NpcActor[] npcs = null;
            foreach (var it in Interactable.All)
            {
                if (it == null || it.GetComponentInParent<NpcActor>() != null) continue;
                string n = it.gameObject.name; bool hit = false;
                foreach (var s in Names) if (n == s) { hit = true; break; }
                if (!hit && (it is GeneralShop || it is Blacksmith || it is Inn || it is Library || it is AppraiserDesk)) hit = ShopNear(it);
                if (!hit) continue;
                if (npcs == null) npcs = FindObjectsByType<NpcActor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                NpcActor best = null; float bd = (it is GeneralShop || it is Blacksmith || it is Inn || it is Library || it is AppraiserDesk) && !it.gameObject.name.StartsWith("InteractPoint_") ? 6f : 9f;
                foreach (var a in npcs)
                {
                    float d = Vector3.Distance(a.transform.position, it.transform.position);
                    if (d < bd) { bd = d; best = a; }
                }
                if (best == null) continue;
                it.transform.SetParent(best.transform, false);
                it.transform.localPosition = Vector3.zero;
                var mk = it.transform.Find("Marker"); if (mk != null) Destroy(mk.gameObject);   // lingkaran lantai dihapus
                it.speakerName = best.displayName;
            }
        }
    }
}
