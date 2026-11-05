using UnityEngine;

namespace HunterGuild
{
    /// <summary>Efek nyata buff makan: label "Regen HP"/"Regen Stamina" memulihkan terus-menerus selama 3 menit (atau sampai quest berikutnya diterima).</summary>
    public class MealBuff : MonoBehaviour
    {
        public const float Duration = 180f;
        static float left; static float hpRate, stRate;
        static MealBuff inst;
        public static float Left { get { return left; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot() { if (inst != null) return; var go = new GameObject("MealBuff"); DontDestroyOnLoad(go); inst = go.AddComponent<MealBuff>(); }

        public static void Begin(string buff)
        {
            string b = buff.ToLowerInvariant();
            bool big = b.Contains("besar"), small = b.Contains("kecil");
            float mul = big ? 2.2f : small ? 0.5f : 1f;
            hpRate = b.Contains("hp") ? 1.4f * mul : 0f;
            stRate = b.Contains("stamina") ? 4f * mul : 0f;
            left = hpRate > 0f || stRate > 0f ? Duration : 0f;
        }

        void Update()
        {
            var gs = GameState.I;
            if (gs == null || string.IsNullOrEmpty(gs.mealBuff)) { left = 0f; return; }
            if (left <= 0f && hpRate == 0f && stRate == 0f) Begin(gs.mealBuff);          // setelah load save
            if (left <= 0f) return;
            var pc = PlayerCombat.I; if (pc == null || pc.IsDead) return;
            left -= Time.deltaTime;
            if (hpRate > 0f) pc.hp = Mathf.Min(pc.MaxHp, pc.hp + hpRate * Time.deltaTime);
            if (stRate > 0f) pc.stamina = Mathf.Min(pc.MaxStamina, pc.stamina + stRate * Time.deltaTime);
            if (left <= 0f) { left = 0f; hpRate = stRate = 0f; if (TownUI.I != null) TownUI.I.Toast("Efek makanan habis."); }
        }
    }
}
