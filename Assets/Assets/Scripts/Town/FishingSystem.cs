using UnityEngine;

namespace HunterGuild
{
    /// <summary>Memancing: dekat air (3-8 m di depan) muncul "[E] Memancing". Lempar, tunggu gigitan (tekan saat tanda !), lalu mini-game menarik ikan.</summary>
    public class FishingSystem : MonoBehaviour
    {
        public static bool Busy;
        static FishingSystem inst;
        enum St { Idle, Wait, Bite }
        St st; float timer, biteWindow, curWindow = 1.2f; float rareMul = 1f; float diffMul = 1f; string rodName = ""; string baitUsed = ""; Vector3 spot; float surfY; GameObject bobber; float bobT;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { if (inst != null) return; var go = new GameObject("FishingSystem"); DontDestroyOnLoad(go); inst = go.AddComponent<FishingSystem>(); }

        /// <summary>Cari titik air di depan pemain.</summary>
        public static bool FindSpot(Transform pt, out Vector3 p)
        {
            p = Vector3.zero;
            Vector3 f = pt.forward; f.y = 0f; f.Normalize();
            for (float d = 3f; d <= 8f; d += 1f)
            {
                Vector3 q = pt.position + f * d; float sy;
                if (WaterZone.Query(new Vector3(q.x, pt.position.y - 0.9f, q.z), out sy) && sy > pt.position.y - 3f && sy < pt.position.y + 0.8f) { p = new Vector3(q.x, sy, q.z); return true; }
            }
            return false;
        }

        public static bool Available(Transform pt)
        {
            if (Busy || MiniGame.Active || PlayerController.Swimming || PlayerController.Climbing || PlayerController.Gliding || MountSystem.Mounted) return false;
            if (PlayerController.InFight()) return false;
            Vector3 p; return FindSpot(pt, out p);
        }

        public static void Begin(Transform pt) { if (inst == null) Boot(); inst.StartCast(pt); }

        void StartCast(Transform pt)
        {
            Vector3 p; if (!FindSpot(pt, out p)) return;
            spot = p; surfY = p.y; st = St.Wait; Busy = true;
            var bait = FishCook.BestBait(); rareMul = 1f; baitUsed = "";
            float waitMul = 1f;
            if (bait != null && GameState.I.RemoveItem(bait.id, 1)) { rareMul = bait.rareMul; waitMul = bait.waitMul; baitUsed = bait.name + " (sisa " + GameState.I.Count(bait.id) + ")"; }
            var rod = FishCook.BestRod(); rodName = rod.name; diffMul = rod.diffMul; rareMul *= rod.rareMul; curWindow = rod.window;
            timer = Random.Range(2.5f, 7f) * waitMul * rod.waitMul;
            if (bobber == null)
            {
                bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(bobber.GetComponent<Collider>());
                bobber.transform.localScale = Vector3.one * 0.18f;
                bobber.GetComponent<Renderer>().sharedMaterial = RT.NewMat(new Color(0.95f, 0.2f, 0.15f));
            }
            bobber.SetActive(true); bobber.transform.position = spot;
            CombatFx.ShockRing(spot, 1.2f, new Color(0.8f, 0.95f, 1f, 0.8f), 0.4f);
            Audio.Sfx("dodge", spot);
            if (TownUI.I != null) TownUI.I.Toast((baitUsed != "" ? "Umpan: " + baitUsed + ". " : "Tanpa umpan (beli Cacing di Toko Umum). ") + rodName + ". Kail dilempar. Tunggu sampai ada tanda ! lalu tekan " + Bindings.Name(Act.Interact) + ". Esc = batal.");
            if (TownUI.I != null) TownUI.I.Toast("Bisa didapat di sini (" + GameCalendar.Name(GameCalendar.Current) + "): " + FishCook.CatchList(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
        }

        void End() { st = St.Idle; Busy = false; if (bobber != null) bobber.SetActive(false); }

        void Update()
        {
            if (st == St.Idle) return;
            var pc = PlayerCombat.I;
            if (pc == null || pc.IsDead || PlayerController.Swimming || (TownUI.I != null && TownUI.I.PanelOpen) || TownInput.CancelDown() || Vector3.Distance(pc.transform.position, spot) > 12f)
            { if (!MiniGame.Active) End(); return; }
            if (MiniGame.Active) return;
            bobT += Time.deltaTime;
            if (st == St.Wait)
            {
                if (bobber != null) bobber.transform.position = spot + Vector3.up * (Mathf.Sin(bobT * 2f) * 0.02f);
                timer -= Time.deltaTime;
                if (TownInput.InteractDown() && timer > 0.4f) { TownUI.I.Toast("Terlalu cepat, ikannya kabur."); End(); return; }
                if (timer <= 0f)
                {
                    st = St.Bite; biteWindow = curWindow;
                    CombatFx.ShockRing(spot, 1.6f, new Color(1f, 1f, 1f, 0.9f), 0.35f);
                    Audio.Sfx("hit_light", spot);
                    if (TownUI.I != null) TownUI.I.Toast("! ! !  Ikan menggigit, tarik sekarang!");
                }
            }
            else if (st == St.Bite)
            {
                if (bobber != null) bobber.transform.position = spot + Vector3.down * (0.06f + Mathf.Abs(Mathf.Sin(bobT * 18f)) * 0.04f);
                biteWindow -= Time.deltaTime;
                if (TownInput.InteractDown() || TownInput.JumpDown())
                {
                    var fish = FishCook.Roll(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, rareMul);
                    st = St.Idle;
                    var def = ItemDB.Get(fish.id);
                    MiniGame.Reel("Menarik " + (def != null ? def.name : "ikan") + "!", Mathf.Clamp01(fish.diff * diffMul), win =>
                    {
                        End();
                        var gs = GameState.I;
                        if (!win) { TownUI.I.Toast("Talinya putus, ikan lepas."); return; }
                        if (!gs.HasSpace(fish.id, 1)) { TownUI.I.Toast("Tas penuh, ikan dilepas."); return; }
                        gs.AddItem(fish.id, 1); gs.AddStat("fished"); gs.AddStat("fc_" + fish.id); HuntManager.Report("gather", fish.id);
                        Audio.Sfx("gather", pc.transform.position);
                        CombatFx.Burst(spot + Vector3.up * 0.2f, new Color(0.85f, 0.95f, 1f), 12, 0.7f, 2.4f, 0.6f, 0.1f);
                        TownUI.I.Toast("Dapat " + def.name + "!");
                    });
                    Busy = true;
                }
                else if (biteWindow <= 0f) { TownUI.I.Toast("Ikannya lolos. Coba lagi."); End(); }
            }
        }
    }
}
