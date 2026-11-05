using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Stealth: tombol Mengendap (U, bisa diganti) = jongkok pelan. Jarak monster mendeteksimu mengecil (mengendap x0.4, diam x0.7, dari belakang x0.6, malam x0.85).
    /// Serangan pertama ke monster yang belum menyadarimu saat mengendap = serangan diam-diam (damage x3, dari belakang x4, boss x1.5).
    /// </summary>
    public class Stealth : MonoBehaviour
    {
        public static bool Sneaking;
        static Stealth inst;
        Vector3 last; float speed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { if (inst != null) return; var go = new GameObject("Stealth"); DontDestroyOnLoad(go); inst = go.AddComponent<Stealth>(); }

        /// <summary>Pengali jarak deteksi untuk monster m terhadap pemain.</summary>
        public static float DetectMul(Monster m)
        {
            float k = 1f;
            if (Sneaking) k *= 0.4f;
            if (inst != null && inst.speed < 0.3f) k *= 0.7f;
            var pc = PlayerCombat.I;
            if (m != null && pc != null)
            {
                Vector3 to = pc.transform.position - m.transform.position; to.y = 0f;
                if (to.sqrMagnitude > 0.01f && Vector3.Angle(m.transform.forward, to) > 110f) k *= 0.6f;
            }
            var gs = GameState.I; if (gs != null && (gs.timeOfDay < 6f || gs.timeOfDay > 19f)) k *= 0.85f;
            return Mathf.Max(0.12f, k);
        }

        /// <summary>Pengali damage serangan diam-diam (1 = bukan). Dipanggil Monster.TakeDamage sebelum monster bereaksi.</summary>
        public static float SneakAttackMul(Monster m)
        {
            if (!Sneaking || m == null || m.InCombat) return 1f;
            var pc = PlayerCombat.I; if (pc == null) return 1f;
            Vector3 to = pc.transform.position - m.transform.position; to.y = 0f;
            bool behind = to.sqrMagnitude > 0.01f && Vector3.Angle(m.transform.forward, to) > 110f;
            if (m.isBoss) return 1.5f;
            return behind ? 4f : 3f;
        }

        void Update()
        {
            var pc = PlayerCombat.I; var pl = pc != null ? pc.transform : null;
            if (pl != null) { Vector3 d = pl.position - last; d.y = 0f; speed = Mathf.Lerp(speed, d.magnitude / Mathf.Max(Time.deltaTime, 1e-4f), 8f * Time.deltaTime); last = pl.position; }
            var ui = TownUI.I; bool menu = (ui != null && ui.PanelOpen) || MiniGame.Active || Cutscene.Active;
            if (!menu && TownInput.SneakDown()) { Sneaking = !Sneaking; if (ui != null) ui.Toast(Sneaking ? "Mengendap: monster lebih sulit melihatmu, serangan pertama jadi serangan diam-diam." : "Berhenti mengendap."); }
            if (Sneaking && (PlayerController.Swimming || PlayerController.Climbing || PlayerController.Gliding || MountSystem.Mounted || (pc != null && pc.IsDead) || TownInput.Sprint())) Sneaking = false;
        }

        static GUIStyle st;
        void OnGUI()
        {
            if (!Sneaking || Cutscene.Active) return;
            if (st == null) st = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            // monster terdekat yang belum waspada: tampilkan jarak amannya
            var pc = PlayerCombat.I; string line = Loc.T("MENGENDAP");
            if (pc != null)
            {
                Monster best = null; float bd = 80f;
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead || m.InCombat || m.Def == null || m.Def.passive || m.Def.aggro <= 0f) continue;
                    float d = Vector3.Distance(m.transform.position, pc.transform.position); if (d < bd) { bd = d; best = m; }
                }
                if (best != null)
                {
                    float safe = best.Def.aggro * DetectMul(best);
                    line += "   " + best.Name + ": " + Mathf.RoundToInt(bd) + " m  (" + Loc.T("terdeteksi di bawah") + " " + Mathf.RoundToInt(safe) + " m)";
                    GUI.color = bd < safe ? new Color(1f, 0.4f, 0.35f) : new Color(0.6f, 1f, 0.6f);
                }
            }
            var r = new Rect(Screen.width * 0.5f - 300, Screen.height * 0.84f, 600, 28);
            var o = GUI.color; GUI.color = new Color(0, 0, 0, 0.8f); GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), line, st);
            GUI.color = o == Color.white ? new Color(0.8f, 0.9f, 1f) : o; GUI.Label(r, line, st); GUI.color = Color.white;
        }
    }
}
