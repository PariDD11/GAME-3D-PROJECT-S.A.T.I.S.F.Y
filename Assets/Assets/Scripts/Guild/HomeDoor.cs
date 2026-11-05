using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>Pintu rumah yang bisa dibeli di kota. Dijual: papan "DIJUAL", tekan E membuka penawaran beli. Dimiliki: masuk ke interior rumah.</summary>
    public class HomeDoor : Interactable
    {
        public string houseId;
        public TextMesh sign;
        public Vector3 center;      // titik tengah bangunan (untuk pratinjau)
        public float viewDist;      // jarak kamera pratinjau
        float next;
        bool lastOwned, init;

        void Start() { Refresh(true); }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.7f;
            Refresh(false);
        }

        void Refresh(bool force)
        {
            var h = PropertyMenus.Find(houseId);
            if (h == null || GameState.I == null) return;
            bool owned = PropertyMenus.Owns(h);
            if (!force && init && owned == lastOwned) return;
            init = true; lastOwned = owned;
            prompt = owned ? "Masuk rumah: " + h.name : "Rumah dijual: " + h.name + " (" + h.price + "g)";
            if (sign != null) sign.text = owned ? "MILIKMU\n" + h.name : "DIJUAL\n" + h.name + "\n" + h.price + " g";
            if (sign != null) sign.color = owned ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.85f, 0.4f);
        }

        public override void Interact()
        {
            var h = PropertyMenus.Find(houseId);
            if (h == null) return;
            if (!PropertyMenus.Owns(h)) { PropertyMenus.OpenOffer(h); return; }
            if (!Application.CanStreamedLevelBeLoaded(InteriorInfo.Scene("Home")))
            {
                if (TownUI.I != null) TownUI.I.Toast("Scene 'Interior_Home' belum dibuat (menu Hunter Guild > Build Interiors).");
                return;
            }
            InteriorRoute.EnterHome(houseId, transform.position, transform.eulerAngles.y, false);
        }
    }

    /// <summary>Di scene Interior_Home: menampilkan furnitur yang dimiliki pemain untuk rumah yang dimasuki.</summary>
    public class HomeInterior : MonoBehaviour
    {
        public Transform furniture;
        public TextMesh title;

        void Start()
        {
            var h = PropertyMenus.Find(InteriorRoute.homeId);
            if (h == null) h = PropertyMenus.OwnedIn(InteriorRoute.returnScene);
            if (title != null) title.text = h != null ? h.name : "Rumahku";
            if (furniture == null) return;
            string bed = "bed0";
            if (h != null)
            {
                if (PropertyMenus.HasFurn(h, "bed3")) bed = "bed3";
                else if (PropertyMenus.HasFurn(h, "bed2")) bed = "bed2";
                else if (PropertyMenus.HasFurn(h, "bed1")) bed = "bed1";
            }
            bool pv = InteriorRoute.preview;
            if (pv && h != null)
            {   // rumah contoh: perabot sesuai kelas rumah
                bed = h.baseComfort >= 12 ? "bed3" : h.baseComfort >= 5 ? "bed2" : "bed1";
                if (title != null) title.text = h.name + "  (contoh)";
            }
            foreach (Transform c in furniture)
            {
                string n = c.name.StartsWith("Furn_") ? c.name.Substring(5) : c.name;
                bool on;
                if (n.StartsWith("bed")) on = n == bed;
                else if (pv && h != null) on = PropertyMenus.ShowroomHas(h, n);
                else on = h != null && PropertyMenus.HasFurn(h, n);
                c.gameObject.SetActive(on);
            }
        }
    }

    /// <summary>Ranjang di rumah sendiri: buka menu rumah (tidur, jual furnitur).</summary>
    public class HomeBed : Interactable
    {
        public override void Interact()
        {
            var h = PropertyMenus.Find(InteriorRoute.homeId);
            if (h == null) h = PropertyMenus.OwnedIn(InteriorRoute.returnScene);
            if (h == null) { if (TownUI.I != null) TownUI.I.Toast("Rumah tidak dikenali."); return; }
            if (InteriorRoute.preview) { PropertyMenus.OpenOffer(h, null, false); return; }
            PropertyMenus.OpenHomeMenu(h);
        }
    }
}
