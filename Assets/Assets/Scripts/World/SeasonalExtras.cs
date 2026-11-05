using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// Otomatis (tanpa rebuild scene): (1) monster musiman muncul di area perburuan; (2) Gubernur Roderic Hale berdiri di dekat Guild Ashwatch.
    /// </summary>
    public class SeasonalExtras : MonoBehaviour
    {
        static bool made;
        string lastScene = "";
        float t;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (made) return; made = true;
            var go = new GameObject("SeasonalExtras");
            DontDestroyOnLoad(go);
            go.AddComponent<SeasonalExtras>();
        }

        void Update()
        {
            string sn = SceneManager.GetActiveScene().name;
            if (sn != lastScene) { lastScene = sn; t = 2.5f; return; }
            if (t <= 0f) return;
            t -= Time.deltaTime;
            if (t > 0f) return;
            if (GameState.I == null) return;
            SpawnGovernor();
            SpawnSeasonal();
        }

        static Vector3 Ground(Vector3 p)
        {
            RaycastHit h;
            if (Physics.Raycast(p + Vector3.up * 60f, Vector3.down, out h, 200f, ~0, QueryTriggerInteraction.Ignore)) p.y = h.point.y;
            return p;
        }

        void SpawnSeasonal()
        {
            var hm = FindFirstObjectByType<HuntManager>();
            var pc = PlayerCombat.I;
            if (hm == null || pc == null) return;
            string[] ids = { "pollen_boar", "heat_jackal", "harvest_brute", "frost_wolf" };
            string id = ids[(int)GameCalendar.Current];
            if (MonsterDB.Get(id) == null) return;
            int n = id == "harvest_brute" ? 2 : 5;
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2f, d = Random.Range(25f, 50f);
                Vector3 p = Ground(pc.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d);
                if (Physics.CheckSphere(p + Vector3.up * 1.2f, 0.8f, ~0, QueryTriggerInteraction.Ignore)) continue;
                var go = new GameObject("Musiman_" + id);
                go.transform.position = p;
                go.AddComponent<Monster>().defId = id;
            }
            GameState.Say("Musim ini: " + MonsterDB.Get(id).name + " berkeliaran di area perburuan!");
        }

        void SpawnGovernor()
        {
            var hall = FindFirstObjectByType<GuildHall>();
            if (hall == null || FindFirstObjectByType<GovernorNpc>() != null) return;
            Vector3 c = hall.transform.position;
            for (int i = 0; i < 16; i++)
            {
                float a = i * 0.8f; float rad = 9f + i * 0.8f;
                Vector3 p = Ground(c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rad);
                if (Physics.CheckBox(p + Vector3.up * 1.2f, new Vector3(0.7f, 1.1f, 0.7f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                var g = new GameObject("NPC_Gubernur");
                g.transform.position = p;
                g.transform.rotation = Quaternion.LookRotation(new Vector3(c.x - p.x, 0f, c.z - p.z));
                Part(g.transform, PrimitiveType.Capsule, new Vector3(0, 0.95f, 0), new Vector3(0.62f, 0.8f, 0.62f), new Color(0.35f, 0.08f, 0.15f));
                Part(g.transform, PrimitiveType.Sphere, new Vector3(0, 1.85f, 0), Vector3.one * 0.4f, new Color(0.92f, 0.74f, 0.6f));
                Part(g.transform, PrimitiveType.Cylinder, new Vector3(0, 2.1f, 0), new Vector3(0.42f, 0.08f, 0.42f), new Color(0.9f, 0.75f, 0.25f));
                Part(g.transform, PrimitiveType.Cube, new Vector3(0, 1.1f, -0.3f), new Vector3(0.7f, 1.4f, 0.08f), new Color(0.6f, 0.1f, 0.15f));
                var gv = g.AddComponent<GovernorNpc>();
                gv.prompt = "Bicara: Gubernur Roderic Hale"; gv.speakerName = "Gubernur Roderic Hale"; gv.speakerRole = "Gubernur Ashwatch";
                gv.radius = 3.4f;
                return;
            }
        }

        static void Part(Transform par, PrimitiveType t, Vector3 lp, Vector3 sc, Color col)
        {
            var o = GameObject.CreatePrimitive(t);
            var cl = o.GetComponent<Collider>(); if (cl != null) Destroy(cl);
            o.transform.SetParent(par, false); o.transform.localPosition = lp; o.transform.localScale = sc;
            var r = o.GetComponent<Renderer>();
            var sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh); m.color = col; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            r.sharedMaterial = m;
        }
    }

    public class GovernorNpc : Interactable
    {
        public override void Interact()
        {
            if (TownUI.I == null) return;
            GovernorChain.Open(() => TownUI.I.Close());
        }
    }
}
