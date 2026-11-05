using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Zona di ujung jalan keluar desa/hutan pemula. Melewatinya memuat scene tujuan dengan fade.
    /// minLevel &gt; 0: pemain di bawah level itu ditahan penjaga (Ashwatch butuh Level 5).
    /// </summary>
    public class VillageExit : MonoBehaviour
    {
        public string destScene = "";
        public string destSpawn = "";
        public int minLevel;
        public string deniedText = "Penjaga: \"Jalan ini belum aman untukmu.\"";
        public Vector3 size = new Vector3(14f, 10f, 4f);

        Transform player;
        bool inside;
        float cooldown;

        void Update()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                player = p.transform;
            }
            if (ScreenFader.I != null && ScreenFader.I.Busy) return;
            if (TownUI.I != null && TownUI.I.PanelOpen) return;

            Vector3 d = transform.InverseTransformPoint(player.position);
            bool now = Mathf.Abs(d.x) < size.x / 2f && Mathf.Abs(d.z) < size.z / 2f && Mathf.Abs(d.y) < size.y;
            if (now && !inside && Time.time > cooldown)
            {
                cooldown = Time.time + 1.5f;
                OnEnter();
            }
            inside = now;
        }

        void OnEnter()
        {
            var gs = GameState.I;
            if (gs == null) return;
            if (minLevel > 0 && gs.level < minLevel)
            {
                if (TownUI.I != null) TownUI.I.Toast(deniedText);
                Audio.Sfx("error");
                GateGuard.PushBack(player, transform.position, 5f);
                return;
            }
            if (minLevel > 0 && destScene == StarterConfig.CityScene && gs.requireJobChoice && !gs.jobChosen)
            {
                if (TownUI.I != null) TownUI.I.Toast("Penjaga: \"Pilih job dulu. Temui Veteran Brann di desa.\"");
                Audio.Sfx("error");
                GateGuard.PushBack(player, transform.position, 5f);
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(destScene))
            {
                if (TownUI.I != null) TownUI.I.Toast("Scene '" + destScene + "' belum dibuat (menu Hunter Guild).");
                GateGuard.PushBack(player, transform.position, 5f);
                return;
            }
            SceneRouter.GoToSpawn(destScene, destSpawn);
        }
    }
}
