using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n6: dandanan monster. Setiap monster mendapat bayangan lembut + denyut cahaya tepi (emission) + serpihan cahaya;
    /// boss mendapat lingkaran rune berputar, mahkota partikel yang mengorbit, tiang cahaya tipis, dan lampu aura berdenyut.
    /// Efek jauh dari kamera dimatikan otomatis (hemat performa).
    /// </summary>
    public class MonsterRoyal : MonoBehaviour
    {
        Monster m; Color col; bool boss; float size = 1f, height = 1.5f, radius = 0.7f;
        ParticleSystem wisps, crown; Transform rune; Light glow; Renderer[] rs; float poll, phase;
        bool bossBuilt;

        public void Init(Monster mo, Color c, bool isBoss, float sz, float h, float r)
        {
            m = mo; col = c; boss = isBoss; size = Mathf.Max(0.5f, sz); height = h; radius = r;
            phase = Random.value * 6.28f;
            if (!RoyalFx.On) { enabled = false; return; }
            BuildCommon();
            if (boss) BuildBoss();
        }

        static Color Vivid(Color c)
        {
            float h, s, v; Color.RGBToHSV(c, out h, out s, out v);
            return Color.HSVToRGB(h, Mathf.Clamp01(s * 0.85f + 0.15f), Mathf.Clamp01(v * 0.6f + 0.4f));
        }

        void BuildCommon()
        {
            Color vc = Vivid(col);
            // bayangan/pendar tanah: cakram lembut gelap di bawah kaki, supaya monster "menempel" ke tanah
            var sh = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(sh.GetComponent<Collider>());
            sh.name = "RoyalShadow";
            sh.transform.SetParent(transform, false);
            sh.transform.localPosition = new Vector3(0f, 0.045f, 0f);
            sh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            sh.transform.localScale = Vector3.one * radius * 3.2f * size;
            sh.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Solid(CombatFx.Soft(), new Color(0f, 0f, 0.02f, 0.55f));
            sh.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // cahaya tepi: emission halus pada semua material tubuh (diabaikan otomatis bila shader tak punya _EmissionColor).
            // Dilewati untuk model MonsterKit / model pengganti: bahannya dibagi (palet / milik prefab) dan r.material akan menggandakannya per bagian.
            if (m == null || !m.HasRig)
            {
                rs = GetComponentsInChildren<Renderer>();
                for (int i = 0; i < rs.Length; i++)
                {
                    var r = rs[i]; if (r == null || r.name == "RoyalShadow") continue;
                    try
                    {
                        var mt = r.material;
                        if (mt != null && mt.HasProperty("_EmissionColor"))
                        {
                            mt.EnableKeyword("_EMISSION");
                            mt.SetColor("_EmissionColor", vc * (boss ? 0.35f : 0.12f));
                        }
                    }
                    catch { }
                }
            }

            // serpihan cahaya yang naik pelan dari tubuh
            wisps = MakeLoop("RoyalWisps", new Vector3(0f, height * 0.45f, 0f), vc, boss ? 24f : 7f, 0.07f * size, 1.6f, -0.12f);
            var sp = wisps.shape; sp.shapeType = ParticleSystemShapeType.Sphere; sp.radius = Mathf.Max(0.35f, radius * 0.8f * size);
        }

        void BuildBoss()
        {
            if (bossBuilt) return; bossBuilt = true;
            Color gold = RoyalFx.Gold;
            // lingkaran rune berputar di tanah
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "RoyalRune";
            q.transform.SetParent(transform, false);
            q.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = Vector3.one * (radius * 4.5f * size + 2f);
            q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Ring(), Color.Lerp(col, gold, 0.5f) * 0.9f);
            q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rune = q.transform;

            // mahkota partikel mengorbit di atas kepala
            crown = MakeLoop("RoyalCrown", new Vector3(0f, height * 1.05f, 0f), gold, 18f, 0.09f * size, 1.1f, 0f);
            var sp = crown.shape; sp.shapeType = ParticleSystemShapeType.Circle; sp.radius = Mathf.Max(0.8f, radius * 0.9f * size); sp.radiusThickness = 0.1f;
            sp.rotation = new Vector3(90f, 0f, 0f);
            var vo = crown.velocityOverLifetime; vo.enabled = true; vo.space = ParticleSystemSimulationSpace.Local; vo.orbitalY = new ParticleSystem.MinMaxCurve(2.2f);

            // lampu aura berdenyut (menimpa BossAura lama yang statis tidak dihapus; ini menambah warna emas)
            var lg = new GameObject("RoyalAura"); lg.transform.SetParent(transform, false); lg.transform.localPosition = new Vector3(0f, height * 0.6f, 0f);
            glow = lg.AddComponent<Light>(); glow.type = LightType.Point; glow.color = Color.Lerp(col, gold, 0.5f); glow.range = 9f * size; glow.intensity = 1.6f; glow.shadows = LightShadows.None;

            RoyalFx.Summon(transform.position, Color.Lerp(col, gold, 0.4f), 2.5f * size);
        }

        ParticleSystem MakeLoop(string name, Vector3 localPos, Color c, float rate, float sz, float life, float grav)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.simulationSpace = name == "RoyalCrown" ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(sz * 0.5f, sz);
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.6f));
            main.gravityModifier = grav;
            main.maxParticles = 120;
            var em = ps.emission; em.rateOverTime = rate * FxQuality.Count * 0.6f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(c, 0.5f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.2f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var sz2 = ps.sizeOverLifetime; sz2.enabled = true; sz2.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), Color.white);
            ps.Play();
            return ps;
        }

        void Update()
        {
            if (m == null || m.IsDead) { if (m != null && m.IsDead) Quiet(); return; }
            if (boss && rune != null) rune.Rotate(0f, 0f, 25f * Time.deltaTime, Space.Self);
            if (glow != null) glow.intensity = 1.5f + Mathf.Sin(Time.time * 2.4f + phase) * 0.45f;
            poll -= Time.deltaTime;
            if (poll > 0f) return;
            poll = 0.5f;
            if (m.isBoss && !bossBuilt) { boss = true; BuildBoss(); }
            var cam = Camera.main;
            bool near = cam == null || (cam.transform.position - transform.position).sqrMagnitude < (boss ? 90f * 90f : 45f * 45f);
            Toggle(wisps, near); Toggle(crown, near);
            if (glow != null) glow.enabled = near;
        }

        static void Toggle(ParticleSystem p, bool on) { if (p == null) return; var e = p.emission; e.enabled = on; }

        void Quiet()
        {
            Toggle(wisps, false); Toggle(crown, false);
            if (glow != null) glow.enabled = false;
            if (rune != null) rune.gameObject.SetActive(false);
            enabled = false;
        }
    }
}
