using UnityEngine;

namespace HunterGuild
{
    /// <summary>Data & perilaku sosial NPC: menyapa pemain dengan kalimat acak lewat balon ucapan saat didekati.</summary>
    public class NpcActor : MonoBehaviour
    {
        public string displayName = "NPC";
        public int race, outfit, role, beastKind = -1;
        public bool female;
        public float height = 1.75f;
        public float greetRadius = 5f;
        public float cooldown = 18f;
        public bool silent;                 // penjaga gerbang tertentu / NPC kunci Guild
        public string[] customLines;
        public Sprite[] gallery; int galleryIdx;   // beberapa foto: ganti tiap percakapan baru
        public Sprite CurrentPortrait { get { return gallery != null && gallery.Length > 0 ? gallery[galleryIdx % gallery.Length] : portrait; } }
        public void NextPortrait() { if (gallery != null && gallery.Length > 1) galleryIdx = (galleryIdx + 1) % gallery.Length; }
        public float pScale = 1f, pX, pY; public bool pFlip; public int pSide;   // penyesuaian posisi foto dialog (dari aturan Ganti NPC); pSide 0 otomatis, 1 kiri, 2 kanan
        public Sprite portrait;             // foto 2D dialog (diisi NpcSwap atau skrip lain); kosong = Resources/Portraits/Nama.png atau placeholder

        static Transform player; static Font font;
        TextMesh tm; Transform bubble, bg; float hideAt, nextGreet, scan, size;
        NpcAnimator anim; bool inRange;

        public void RebindAnimator() { anim = GetComponent<NpcAnimator>(); }
        public void SetTalking(bool on) { if (anim == null) anim = GetComponent<NpcAnimator>(); if (anim != null) anim.Talk(on); }

        void Awake() { anim = GetComponent<NpcAnimator>(); nextGreet = Random.Range(0f, 3f); }

        static Font GetFont()
        {
            if (font != null) return font;
#if UNITY_2022_2_OR_NEWER
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            return font;
        }

        public void Say(string text, float seconds = 4.5f)
        {
            if (bubble == null) Build();
            tm.text = Wrap(text, 24);
            bubble.gameObject.SetActive(true);
            size = 2;           // ukur ulang 2 frame lagi (bounds baru valid setelah render)
            hideAt = Time.time + seconds;
        }

        static string Wrap(string s, int w)
        {
            var words = s.Split(' '); var sb = new System.Text.StringBuilder(); int len = 0;
            foreach (var wd in words)
            {
                if (len + wd.Length > w && len > 0) { sb.Append('\n'); len = 0; }
                else if (len > 0) { sb.Append(' '); len++; }
                sb.Append(wd); len += wd.Length;
            }
            return sb.ToString();
        }

        void Build()
        {
            bubble = new GameObject("Bubble").transform;
            bubble.SetParent(transform, false);
            bubble.localPosition = new Vector3(0f, height + 0.55f, 0f);
            var shader = Shader.Find("Sprites/Default");
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            bg = q.transform; bg.SetParent(bubble, false);
            var m = new Material(shader); m.color = new Color(0.08f, 0.07f, 0.1f, 0.82f);
            q.GetComponent<MeshRenderer>().material = m;
            var t = new GameObject("Text").transform; t.SetParent(bubble, false); t.localPosition = new Vector3(0f, 0f, -0.01f);
            tm = t.gameObject.AddComponent<TextMesh>();
            tm.font = GetFont(); tm.fontSize = 48; tm.characterSize = 0.045f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.96f, 0.86f);
            var mr = t.GetComponent<MeshRenderer>(); TextMat.Apply(mr, tm.font.material);
            bubble.gameObject.SetActive(false);
        }

        void Update()
        {
            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.transform;
                if (player == null) return;
            }
            scan -= Time.deltaTime;
            if (scan <= 0f && !silent)
            {
                scan = 0.25f;
                Vector3 d = player.position - transform.position; d.y = 0f;
                bool now = d.sqrMagnitude < greetRadius * greetRadius;
                if (now && !inRange && Time.time >= nextGreet)
                {
                    nextGreet = Time.time + cooldown + Random.Range(0f, 8f);
                    string line = customLines != null && customLines.Length > 0 && Random.value < 0.6f
                        ? customLines[Random.Range(0, customLines.Length)] : NpcDialogue.Greeting(race, role);
                    Say(line);
                    if (anim != null) { anim.Wave(); anim.LookAt(player); }
                    var w = GetComponent<WanderNPC>(); if (w != null && w.waypoints != null && w.waypoints.Length > 1) { /* tetap jalan */ }
                    else if (d.sqrMagnitude > 0.5f) transform.rotation = Quaternion.LookRotation(d);
                }
                if (!now && inRange && anim != null) anim.LookAt(null);
                inRange = now;
            }
            if (bubble != null && bubble.gameObject.activeSelf)
            {
                if (Time.time > hideAt) { bubble.gameObject.SetActive(false); if (anim != null && !inRange) anim.LookAt(null); return; }
                if (Camera.main != null)
                {
                    var ct = Camera.main.transform;
                    bubble.rotation = Quaternion.LookRotation(bubble.position - ct.position);
                    float dist = Vector3.Distance(ct.position, bubble.position);
                    float sc = Mathf.Clamp(dist * 0.09f, 0.45f, 1.3f);
                    bubble.localScale = Vector3.one * sc / Mathf.Max(0.01f, transform.lossyScale.y);
                }
                if (size > 0f)
                {
                    size -= 1f;
                    var mr = tm.GetComponent<MeshRenderer>();
                    if (mr.bounds.size.sqrMagnitude > 0.0001f)
                    {
                        Vector3 s = mr.bounds.size; float k = 1f / Mathf.Max(0.0001f, bubble.lossyScale.x);
                        float w = new Vector2(s.x, s.z).magnitude * k;       // lebar menghadap kamera ≈ ukuran bidang xz
                        float h = s.y * k;
                        bg.localScale = new Vector3(Mathf.Max(0.5f, w + 0.18f), Mathf.Max(0.18f, h + 0.1f), 1f);
                        size = 0f;
                    }
                }
            }
        }
    }
}
