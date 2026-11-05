using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Kejelasan tempur monster: bar HP + nama melayang, tanda "!" saat mulai mengejar, lingkaran bahaya di tanah yang terisi
    /// selama monster bersiap menyerang (telegraph), dan sentakan maju + efek tebasan saat serangan mengenai.
    /// </summary>
    public class MonsterFeedback : MonoBehaviour
    {
        Monster mon; MonsterDef def;
        Transform bar, fill; TextMesh nameTm;
        Material mat, ringMat, discMat;
        Transform ringT, discT;
        float teleStart, teleDur; bool tele;
        float lungeUntil; Vector3 lungeDir;
        static Texture2D ringTex, discTex;

        static Texture2D MakeTex(bool ring)
        {
            var t = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float d = Vector2.Distance(new Vector2((x + .5f) / 128f, (y + .5f) / 128f), new Vector2(.5f, .5f)) * 2f;
                float a = ring ? Mathf.Clamp01((d - 0.9f) * 30f) * Mathf.Clamp01((1f - d) * 30f) : Mathf.Clamp01((1f - d) * 20f) * (0.55f + 0.45f * d);
                px[y * 128 + x] = new Color(1, 1, 1, a);
            }
            t.SetPixels(px); t.Apply();
            return t;
        }

        public void Init(Monster m, MonsterDef d)
        {
            mon = m; def = d;
            var sh = Resources.Load<Shader>("SkyDisc");
            if (sh == null) { enabled = false; return; }
            mat = new Material(sh);
            if (ringTex == null) { ringTex = MakeTex(true); discTex = MakeTex(false); }
            ringMat = new Material(sh) { mainTexture = ringTex };
            discMat = new Material(sh) { mainTexture = discTex };

            float top = d.height * d.size + 0.7f;
            var root = new GameObject("HpBar").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0f, top, 0f);
            bar = root;
            var bg = Quad(root, new Color(0f, 0f, 0f, 0.7f), new Vector3(1.5f, 0.2f, 1f), Vector3.zero);
            fill = Quad(root, new Color(0.85f, 0.15f, 0.12f, 0.95f), new Vector3(1.4f, 0.12f, 1f), new Vector3(0f, 0f, -0.01f));
            fill.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(0.85f, 0.15f, 0.12f, 0.95f);

            var go = new GameObject("Name");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            nameTm = go.AddComponent<TextMesh>();
            nameTm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameTm.fontSize = 48; nameTm.characterSize = 0.045f;
            nameTm.anchor = TextAnchor.MiddleCenter; nameTm.alignment = TextAlignment.Center;
            nameTm.color = d.large ? new Color(1f, 0.85f, 0.4f) : Color.white;
            nameTm.text = m.Name;
            TextMat.Apply(go.GetComponent<MeshRenderer>(), nameTm.font.material);
            bar.gameObject.SetActive(false);
        }

        Transform Quad(Transform parent, Color c, Vector3 scale, Vector3 pos)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            var r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = new Material(mat) { color = c };
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return g.transform;
        }

        void LateUpdate()
        {
            if (mon == null) return;
            var cam = Camera.main;
            if (mon.IsDead) { if (bar != null) bar.gameObject.SetActive(false); EndTelegraph(); return; }

            // bar HP: tampil bila terluka atau sedang bertarung, dalam jarak 35 m
            if (bar != null && cam != null)
            {
                bool hurt = mon.hp < mon.MaxHp * 0.999f;
                bool near = (cam.transform.position - transform.position).sqrMagnitude < 50f * 50f;
                bool show = near;
                bar.gameObject.SetActive(show);
                if (show)
                {
                    bar.rotation = cam.transform.rotation;
                    float r = Mathf.Clamp01(mon.hp / Mathf.Max(1f, mon.MaxHp));
                    fill.localScale = new Vector3(1.4f * r, 0.12f, 1f);
                    fill.localPosition = new Vector3(-0.7f * (1f - r), 0f, -0.01f);
                }
            }

            if (tele && ringT != null)
            {
                float p = Mathf.Clamp01((Time.time - teleStart) / Mathf.Max(0.05f, teleDur));
                Vector3 pos = transform.position; pos.y += 0.12f;
                ringT.position = pos; discT.position = pos + Vector3.up * 0.01f;
                float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * 22f);
                ringMat.color = new Color(1f, 0.15f, 0.1f, 0.9f);
                float s = discT.localScale.x;
                discT.localScale = new Vector3(ringT.localScale.x * Mathf.Lerp(0.15f, 1f, p), ringT.localScale.y * Mathf.Lerp(0.15f, 1f, p), 1f);
                discMat.color = new Color(1f, 0.2f + 0.2f * p, 0.1f, 0.25f + 0.35f * p * pulse);
                if (Time.time - teleStart > teleDur + 0.4f) EndTelegraph();
            }

            if (Time.time < lungeUntil)
                transform.position += lungeDir * (10f * Time.deltaTime);
        }

        // ---------------- API dipanggil Monster ----------------
        public void Alert()
        {
            var go = new GameObject("Alert");
            go.transform.position = transform.position + Vector3.up * (def.height * def.size + 1.5f);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.text = "!"; tm.fontSize = 90; tm.characterSize = 0.07f; tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.3f, 0.2f);
            TextMat.Apply(go.GetComponent<MeshRenderer>(), tm.font.material);
            go.AddComponent<RiseFade>();
            Destroy(go, 1.0f);
            Audio.Sfx("monster_growl", transform.position);
        }

        public void BeginTelegraph(float duration, float radius)
        {
            EndTelegraph();
            tele = true; teleStart = Time.time; teleDur = duration;
            float d = (radius + 0.8f) * 2f;
            ringT = FlatQuad("TeleRing", ringMat, d);
            discT = FlatQuad("TeleFill", discMat, d * 0.15f);
        }

        Transform FlatQuad(string n, Material m, float size)
        {
            var g = new GameObject(n);
            g.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            g.transform.localScale = new Vector3(size, size, 1f);
            return g.transform;
        }

        public void EndTelegraph()
        {
            tele = false;
            if (ringT != null) Destroy(ringT.gameObject);
            if (discT != null) Destroy(discT.gameObject);
            ringT = discT = null;
        }

        /// <summary>Serangan mengenai: sentak maju, tebasan, percikan.</summary>
        public void Strike(Vector3 targetPos, bool hit)
        {
            EndTelegraph();
            Vector3 d = targetPos - transform.position; d.y = 0f;
            lungeDir = d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
            lungeUntil = Time.time + Mathf.Min(0.14f, Mathf.Max(0f, d.magnitude - 1f) / 10f);
            Vector3 c = transform.position + Vector3.up * Mathf.Max(0.7f, def.height * 0.5f) + lungeDir * 0.6f;
            CombatFx.SlashArc(c, Quaternion.LookRotation(lungeDir), 1.6f + def.reach * 0.35f, Random.value < 0.5f ? 25f : -25f, Color.Lerp(def.color, CombatFx.HurtCol, 0.6f), 0.18f, def.large ? 1.4f : 0.9f);
            if (def.large) { CombatFx.ShockRing(transform.position + lungeDir * 1.5f, 2.6f, CombatFx.HurtCol, 0.3f); CombatFx.Dust(transform.position + lungeDir * 1.5f, 8); }
        }

        class RiseFade : MonoBehaviour
        {
            float t; TextMesh tm;
            void Start() { tm = GetComponent<TextMesh>(); }
            void LateUpdate()
            {
                t += Time.deltaTime;
                transform.position += Vector3.up * (1.2f * Time.deltaTime);
                if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
                if (tm != null) { var c = tm.color; c.a = Mathf.Clamp01(1f - t / 0.9f); tm.color = c; }
            }
        }
    }
}
