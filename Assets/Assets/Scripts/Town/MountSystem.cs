using UnityEngine;

namespace HunterGuild
{
    /// <summary>Tunggangan: tekan N (D-pad atas di gamepad) untuk memanggil kuda dan menaikinya, tekan lagi untuk turun. Kuda dibuat dari bentuk dasar (placeholder, bisa diganti model).</summary>
    public class MountSystem : MonoBehaviour
    {
        public static bool Mounted;
        public const float SpeedMul = 1.8f;
        static MountSystem inst;
        GameObject horse; Transform[] legs = new Transform[4]; Transform body;
        Vector3 lastPos; float phase, speedSm;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("MountSystem"); DontDestroyOnLoad(go); inst = go.AddComponent<MountSystem>();
        }

        static Transform PlayerT() { var pc = PlayerCombat.I; return pc != null ? pc.transform : null; }

        void Update()
        {
            var pt = PlayerT();
            if (pt == null || horse == null) { if (horse == null) Mounted = false; }
            var ui = TownUI.I;
            bool menu = ui != null && ui.PanelOpen;
            if (pt != null && !menu && TownInput.MountDown()) Toggle(pt);
            if (Mounted && (PlayerController.Swimming || PlayerController.Climbing || (PlayerCombat.I != null && PlayerCombat.I.IsDead)))
            { Mounted = false; if (horse != null) PlaceHorse(pt); }
        }

        void Toggle(Transform pt)
        {
            if (Mounted) { Mounted = false; Place(pt, pt.forward * 1.2f); if (TownUI.I != null) TownUI.I.Toast("Turun dari kuda."); return; }
            if (PlayerController.Swimming || PlayerController.Climbing || PlayerController.Gliding) return;
            if (PlayerController.InFight()) { if (TownUI.I != null) TownUI.I.Toast("Tidak bisa memanggil kuda saat bertarung."); return; }
            if (horse == null) Build();
            Place(pt, Vector3.zero);
            Mounted = true;
            Audio.Sfx("dodge", pt.position);
            CombatFx.Dust(pt.position - Vector3.up * 0.9f, 8);
            if (TownUI.I != null) TownUI.I.Toast("Naik kuda. Kecepatan x" + SpeedMul.ToString("0.0") + ". Tekan " + Bindings.Name(Act.Mount) + " lagi untuk turun.");
        }

        void Place(Transform pt, Vector3 off)
        {
            Vector3 p = pt.position + off; p.y = pt.position.y - 0.9f;
            horse.transform.position = p; horse.transform.rotation = Quaternion.Euler(0f, pt.eulerAngles.y, 0f);
            lastPos = p;
        }
        void PlaceHorse(Transform pt) { if (pt != null) Place(pt, pt.forward * 1.2f); }

        void LateUpdate()
        {
            if (horse == null) return;
            var pt = PlayerT();
            if (Mounted && pt != null)
            {
                Vector3 p = pt.position; p.y -= 0.9f;
                Vector3 d = p - lastPos; d.y = 0f; lastPos = p;
                horse.transform.position = p;
                horse.transform.rotation = Quaternion.Slerp(horse.transform.rotation, Quaternion.Euler(0f, pt.eulerAngles.y, 0f), 1f - Mathf.Exp(-12f * Time.deltaTime));
                float v = d.magnitude / Mathf.Max(Time.deltaTime, 1e-4f);
                speedSm = Mathf.Lerp(speedSm, Mathf.Min(v, 16f), 10f * Time.deltaTime);
            }
            else speedSm = Mathf.Lerp(speedSm, 0f, 10f * Time.deltaTime);
            phase += speedSm * 1.1f * Time.deltaTime;
            float amp = Mathf.Clamp01(speedSm / 6f) * 38f;
            for (int i = 0; i < 4; i++)
            {
                if (legs[i] == null) continue;
                float ph = phase * 6f + (i == 0 || i == 3 ? 0f : Mathf.PI);
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(ph) * amp, 0f, 0f);
            }
            if (body != null) body.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(phase * 6f)) * 0.05f * Mathf.Clamp01(speedSm / 6f), 0f);
        }

        void Build()
        {
            horse = new GameObject("Horse");
            var b = new GameObject("Body").transform; b.SetParent(horse.transform, false); body = b;
            Color brown = new Color(0.46f, 0.29f, 0.16f), dark = new Color(0.2f, 0.12f, 0.07f), red = new Color(0.55f, 0.12f, 0.1f);
            RT.Prim(PrimitiveType.Cube, b, new Vector3(0, 1.0f, 0), new Vector3(0.55f, 0.6f, 1.5f), brown);
            var neck = RT.Prim(PrimitiveType.Cube, b, new Vector3(0, 1.42f, 0.75f), new Vector3(0.28f, 0.7f, 0.3f), brown); neck.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            var head = RT.Prim(PrimitiveType.Cube, b, new Vector3(0, 1.72f, 1.08f), new Vector3(0.26f, 0.3f, 0.58f), brown); head.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
            var mane = RT.Prim(PrimitiveType.Cube, b, new Vector3(0, 1.56f, 0.62f), new Vector3(0.08f, 0.62f, 0.2f), dark); mane.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            var tail = RT.Prim(PrimitiveType.Cube, b, new Vector3(0, 1.1f, -0.86f), new Vector3(0.1f, 0.6f, 0.1f), dark); tail.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            RT.Prim(PrimitiveType.Cube, b, new Vector3(0, 1.34f, -0.05f), new Vector3(0.52f, 0.1f, 0.5f), red);
            float[] xs = { -0.2f, 0.2f, -0.2f, 0.2f }, zs = { 0.55f, 0.55f, -0.55f, -0.55f };
            for (int i = 0; i < 4; i++)
            {
                var piv = new GameObject("Leg" + i).transform; piv.SetParent(horse.transform, false); piv.localPosition = new Vector3(xs[i], 0.8f, zs[i]);
                RT.Prim(PrimitiveType.Cube, piv, new Vector3(0, -0.4f, 0), new Vector3(0.14f, 0.8f, 0.14f), i < 2 ? brown : dark);
                legs[i] = piv;
            }
        }
    }
}
