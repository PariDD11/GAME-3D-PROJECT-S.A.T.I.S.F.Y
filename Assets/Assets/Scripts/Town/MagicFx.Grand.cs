using System.Collections;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Lapisan efek megah: gelombang kejut berlapis, pilar cahaya, cincin sihir ganda, percikan membumbung.</summary>
    public static partial class MagicFx
    {
        static IEnumerator Later(float delay, System.Action a)
        {
            yield return new WaitForSeconds(delay);
            if (a != null) a();
        }

        static void After(float delay, System.Action a) { CombatFx.Host.I.StartCoroutine(Later(delay, a)); }

        /// <summary>Pilar cahaya vertikal dengan inti putih.</summary>
        public static void LightPillar(Vector3 pos, float height, float width, Color col, float life)
        {
            CombatFx.Beam(pos, pos + Vector3.up * height, new Color(col.r, col.g, col.b, 0.8f), width, life);
            CombatFx.Beam(pos, pos + Vector3.up * height, new Color(1f, 1f, 1f, 0.95f), width * 0.4f, life * 0.8f);
        }

        /// <summary>Tambahan "megah" untuk setiap dampak elemen: cincin kedua, cincin sihir, bara naik.</summary>
        static void Grand(Vector3 pos, string elem, float radius)
        {
            Color col = ElemCol(elem);
            Color hot, deep; Hot(elem, out hot, out deep);
            float r = Mathf.Max(1.5f, radius);
            After(0.08f, () => CombatFx.ShockRing(pos, r * 1.9f, new Color(hot.r, hot.g, hot.b, 0.7f), 0.5f));
            After(0.18f, () => CombatFx.ShockRing(pos, r * 2.5f, new Color(col.r, col.g, col.b, 0.45f), 0.6f));
            MagicCircle(pos, r * 0.9f, new Color(col.r, col.g, col.b, 0.8f), 0.7f);
            CombatFx.Burst(pos + Vector3.up * 0.3f, hot, 14 + Mathf.RoundToInt(r * 3f), r * 0.5f, 5f + r * 0.6f, 1.1f, 0.14f);
            if (radius >= 5f) LightPillar(pos, 10f + r, 0.35f + r * 0.08f, col, 0.45f);
        }

        /// <summary>Awal skill ultimate di kaki pemain: dua lingkaran sihir, pilar, gelombang, percikan, gempa kamera.</summary>
        public static void UltimateCast(Vector3 feet, Vector3 forward, Color col)
        {
            Color hot = Color.Lerp(col, Color.white, 0.7f);
            MagicCircle(feet, 4.2f, new Color(col.r, col.g, col.b, 0.95f), 1.6f);
            After(0.12f, () => MagicCircle(feet, 2.6f, new Color(hot.r, hot.g, hot.b, 0.95f), 1.4f));
            LightPillar(feet, 22f, 0.9f, col, 0.9f);
            CombatFx.Flash(feet + Vector3.up * 1.2f, col, 16f, 3.5f, 0.6f);
            CombatFx.ShockRing(feet, 7f, new Color(col.r, col.g, col.b, 0.9f), 0.55f);
            After(0.15f, () => CombatFx.ShockRing(feet, 11f, new Color(hot.r, hot.g, hot.b, 0.6f), 0.7f));
            After(0.3f, () => CombatFx.ShockRing(feet, 15f, new Color(col.r, col.g, col.b, 0.4f), 0.8f));
            CombatFx.Burst(feet + Vector3.up * 0.2f, hot, 60, 2.4f, 9f, 1.4f, 0.16f);
            CombatFx.Burst(feet + Vector3.up * 0.2f, col, 40, 4f, 3f, 1.1f, 0.2f);
            ThirdPersonCamera.Shake(0.35f, 0.6f);
        }
    }
}
