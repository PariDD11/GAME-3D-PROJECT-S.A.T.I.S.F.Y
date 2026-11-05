using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Siluet hewan raksasa dari garis cahaya untuk ultimate yang bernama hewan:
    /// Dragon King Slash (naga), Wild Hunt (kawanan serigala), Phoenix Rebirth / Phoenix Ascension / Eternal Rebirth (feniks),
    /// Leviathan's Wrath / Leviathan Call (leviathan laut). Tiap hewan punya bentuk & gerak sendiri (bukan sekadar pita).
    /// </summary>
    public static partial class SkillSilhouette
    {
        public struct Col { public Color m, h, k; }

        static readonly HashSet<string> Ids = new HashSet<string> { "e30_2_6", "rg_6", "fm_wrd_6", "ph_6", "e30_7_6", "td_6", "e30_13_6" };
        public static bool Handles(string id) { return Ids.Contains(id); }

        // ------------------------------------------------------------------ bentuk (koordinat lokal: x = arah hadap, y = atas)
        static Vector2 V(float x, float y) { return new Vector2(x, y); }

        // Bentuk lama (garis) diganti seni baru (MythShapes): dipakai fase naga Swordmaster, Wolf Pack, Phoenix, Leviathan.
        static MySpec Spec(string bas, string prof, params string[] f) { return new MySpec { bas = bas, prof = prof, f = f, scale = 1f }; }

        static List<Vector2[]> Rich(List<Vector2[]> l, float k)
        {
            if (Mathf.Abs(k - 1f) < 0.001f) return l;
            for (int i = 0; i < l.Count; i++) for (int j = 0; j < l[i].Length; j++) l[i][j] *= k;
            for (int i = 0; i < myFills.Count; i++) { var f = myFills[i]; for (int j = 0; j < f.p.Length; j++) f.p[j] *= k; }
            myHead *= k; myTop *= k;
            return l;
        }

        static readonly MySpec SpDragon = Spec("d", "fafnir"), SpWolf = Spec("q", "wolf", "bushy"), SpPhoenix = Spec("b", "simurgh", "crest", "plumes", "fire"), SpLev = Spec("s", "midgard");
        static List<Vector2[]> Dragon(float phase) { return Rich(MythShape(Spec("d", "fafnir"), phase, 0.6f), 1f); }
        static List<Vector2[]> Phoenix(float flap) { return Rich(MythShape(Spec("b", "simurgh", "crest", "plumes", "fire"), Mathf.Asin(Mathf.Clamp(flap, -1f, 1f)), 0.3f), 1f); }
        static List<Vector2[]> PhoenixFront(float flap) { return Phoenix(flap); }
        static List<Vector2[]> Wolf(float phase) { return Rich(MythShape(Spec("q", "wolf", "bushy"), phase, 0.4f), 1f); }
        static List<Vector2[]> Leviathan(float phase, float mouth) { return Rich(MythShape(Spec("s", "midgard"), phase, mouth), 0.6f); }

        // ------------------------------------------------------------------ gambar
        // ------------------------------------------------------------------ renderer ringan: satu LineRenderer per garis, dipakai ulang tiap frame
        sealed class View
        {
            readonly GameObject root = new GameObject("FxSilhouette");
            readonly List<LineRenderer> main = new List<LineRenderer>(), hot = new List<LineRenderer>();
            readonly Material mm, mh;
            readonly Color cm, ch;
            Mesh fillMesh; Material mf, ma; GameObject aura;
            readonly List<Vector3> fv = new List<Vector3>(); readonly List<Color> fc = new List<Color>(); readonly List<int> ft = new List<int>();
            int bbSign = 1;
            public static Vector3 LastAx = Vector3.forward;   // sumbu hadap terakhir yang dipakai menggambar (untuk posisi kepala)
            public View(Col c)
            {
                cm = c.m; ch = c.h;
                mm = CombatFx.Mat(null, new Color(c.m.r, c.m.g, c.m.b, 0.9f));
                mh = CombatFx.Mat(null, new Color(c.h.r, c.h.g, c.h.b, 1f));
            }
            LineRenderer New(Material m)
            {
                var go = new GameObject("l"); go.transform.SetParent(root.transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = false; lr.sharedMaterial = m; lr.numCapVertices = 2; lr.numCornerVertices = 2;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return lr;
            }
            void EnsureFill()
            {
                if (fillMesh != null) return;
                fillMesh = new Mesh(); fillMesh.MarkDynamic();
                var go = new GameObject("fill"); go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = fillMesh;
                var r = go.AddComponent<MeshRenderer>(); mf = CombatFx.Solid(null, Color.white); r.sharedMaterial = mf;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                aura = GameObject.CreatePrimitive(PrimitiveType.Quad); aura.name = "aura";
                var col = aura.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                aura.transform.SetParent(root.transform, false);
                ma = CombatFx.Mat(CombatFx.Soft(), new Color(ch.r, ch.g, ch.b, 0.26f));
                var ar = aura.GetComponent<MeshRenderer>(); ar.sharedMaterial = ma; ar.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; ar.receiveShadows = false;
            }
            /// <summary>Hadapkan sumbu ke kamera (tampak samping selalu terbaca), dengan histeresis supaya tidak berkedip kiri-kanan.</summary>
            Vector3 Billboard(Vector3 ax)
            {
                var cam = Camera.main; if (cam == null) return ax;
                Vector3 r = cam.transform.right; r.y = 0f;
                if (r.sqrMagnitude < 1e-4f) return ax;
                r.Normalize();
                float d = Vector3.Dot(ax, r);
                if (d > 0.3f) bbSign = 1; else if (d < -0.3f) bbSign = -1;
                Vector3 a = r * bbSign + ax * 0.3f; a.y = 0f;
                return a.sqrMagnitude < 1e-4f ? r : a.normalized;
            }
            public void Set(List<Vector2[]> shape, Vector3 O, Vector3 ax, float sc, float w) { Set(shape, O, ax, sc, w, false); }
            public void Set(List<Vector2[]> shape, Vector3 O, Vector3 ax, float sc, float w, bool rich)
            {
                if (root == null) return;
                root.SetActive(true);
                Vector3 Ow = O;
                if (rich) { ax = Billboard(ax); LastAx = ax; root.transform.position = Ow; O = Vector3.zero; }
                else root.transform.position = Vector3.zero;
                while (main.Count < shape.Count) { main.Add(New(mm)); hot.Add(New(mh)); }
                for (int i = 0; i < main.Count; i++)
                {
                    bool on = i < shape.Count;
                    main[i].enabled = on && !rich; hot[i].enabled = on;
                    if (!on) continue;
                    var pl = shape[i];
                    main[i].positionCount = pl.Length; hot[i].positionCount = pl.Length;
                    for (int k = 0; k < pl.Length; k++)
                    {
                        Vector3 q = O + ax * pl[k].x * sc + Vector3.up * pl[k].y * sc;
                        if (!rich) main[i].SetPosition(k, q);
                        hot[i].SetPosition(k, q);
                    }
                    main[i].widthMultiplier = w * sc; hot[i].widthMultiplier = rich ? w * sc * 0.22f : w * sc * 0.3f;
                }
                if (!rich) return;
                EnsureFill();
                fv.Clear(); fc.Clear(); ft.Clear();
                for (int i = 0; i < myFills.Count; i++)
                {
                    var f = myFills[i]; var p = f.p; int n = p.Length;
                    if (n < 3) continue;
                    if (f.strip) AddStrip(f, O, ax, sc); else AddPoly(f, O, ax, sc);
                }
                fillMesh.Clear();
                if (fv.Count > 0 && fv.Count < 60000)
                {
                    MeshSafe.Clean(fv); fillMesh.SetVertices(fv); fillMesh.SetColors(fc); fillMesh.SetTriangles(ft, 0, false);
                    fillMesh.bounds = new Bounds(Vector3.up * 6f * sc, new Vector3(60f, 40f, 60f) * sc);
                }
                // aura lembut di belakang makhluk
                var cam = Camera.main;
                if (cam != null && aura != null)
                {
                    aura.SetActive(true);
                    aura.transform.position = Ow + Vector3.up * Mathf.Max(3f, myTop.y * 0.45f) * sc - cam.transform.forward * 1.5f;
                    aura.transform.rotation = cam.transform.rotation;
                    float sz = Mathf.Max(14f, myTop.y * 1.8f) * sc;
                    aura.transform.localScale = new Vector3(sz * 1.25f, sz, 1f);
                }
            }
            // ---- warna bergradasi: gelap (bawah/pangkal) -> warna dasar -> terang keputihan (atas/ujung), dengan tepi tinta gelap ala anime
            Color Dk { get { return new Color(cm.r * 0.2f, cm.g * 0.2f, cm.b * 0.2f + 0.02f, 1f); } }
            Color Mid { get { return Color.Lerp(cm, ch, 0.2f); } }
            Color Lt { get { return Color.Lerp(ch, Color.white, 0.62f); } }
            Color Ink { get { return new Color(cm.r * 0.07f, cm.g * 0.07f, cm.b * 0.09f, 0.96f); } }
            Color Grad(float t, float sh)
            {
                t = Mathf.Clamp01(t);
                Color c = t < 0.5f ? Color.Lerp(Dk, Mid, t * 2f) : Color.Lerp(Mid, Lt, (t - 0.5f) * 2f);
                c.r *= sh; c.g *= sh; c.b *= sh; c.a = 0.97f; return c;
            }
            Vector3 W3(Vector2 q, Vector3 O, Vector3 ax, float sc) { return O + ax * q.x * sc + Vector3.up * q.y * sc; }
            void Tri(int a, int b, int c) { ft.Add(a); ft.Add(b); ft.Add(c); }
            const float InkW = 0.12f;

            void AddPoly(MyFill f, Vector3 O, Vector3 ax, float sc)
            {
                var p = f.p; int n = p.Length;
                Vector2 cen = Vector2.zero; float y0 = 1e9f, y1 = -1e9f;
                for (int k = 0; k < n; k++) { cen += p[k]; if (p[k].y < y0) y0 = p[k].y; if (p[k].y > y1) y1 = p[k].y; }
                cen /= n; float span = Mathf.Max(0.2f, y1 - y0);
                if (f.ink)
                {
                    int b0 = fv.Count; Color ic = Ink; ic.r *= f.sh; ic.g *= f.sh; ic.b *= f.sh;
                    fv.Add(W3(cen, O, ax, sc)); fc.Add(ic);
                    for (int k = 0; k < n; k++) { Vector2 d = (p[k] - cen).normalized; fv.Add(W3(p[k] + d * InkW, O, ax, sc)); fc.Add(ic); }
                    for (int k = 0; k < n; k++) Tri(b0, b0 + 1 + k, b0 + 1 + (k + 1) % n);
                }
                {
                    int b0 = fv.Count;
                    Color cc = f.kind == 3 ? Color.Lerp(ch, Color.white, 0.9f) : Grad(0.5f, f.sh);
                    fv.Add(W3(cen, O, ax, sc)); fc.Add(f.kind == 3 ? cc : Grad(0.42f, f.sh));
                    for (int k = 0; k < n; k++) { fv.Add(W3(p[k], O, ax, sc)); fc.Add(f.kind == 3 ? cc : Grad(0.1f + 0.85f * (p[k].y - y0) / span, f.sh)); }
                    for (int k = 0; k < n; k++) Tri(b0, b0 + 1 + k, b0 + 1 + (k + 1) % n);
                }
            }

            void AddStrip(MyFill f, Vector3 O, Vector3 ax, float sc)
            {
                var p = f.p; int h = p.Length / 2;
                if (f.ink)
                {
                    int b0 = fv.Count; Color ic = Ink; ic.r *= f.sh; ic.g *= f.sh; ic.b *= f.sh;
                    for (int k = 0; k < h; k++)
                    {
                        Vector2 L = p[k], R = p[h + k], d = (L - R); d = d.sqrMagnitude < 1e-6f ? Vector2.zero : d.normalized;
                        float e = (k == 0 || k == h - 1) ? InkW * 0.8f : InkW * 0.45f;
                        fv.Add(W3(L + d * e, O, ax, sc)); fc.Add(ic); fv.Add(W3(R - d * e, O, ax, sc)); fc.Add(ic);
                    }
                    for (int k = 0; k + 1 < h; k++) { int a = b0 + 2 * k; Tri(a, a + 1, a + 2); Tri(a + 1, a + 3, a + 2); }
                }
                {
                    int b0 = fv.Count;
                    for (int k = 0; k < h; k++)
                    {
                        Vector2 L = p[k], R = p[h + k]; float u = k / (float)Mathf.Max(1, h - 1);
                        bool lUp = (L.y + 0.3f * L.x) > (R.y + 0.3f * R.x);
                        float tc = f.kind == 2 ? Mathf.Lerp(0.18f, 0.9f, u) : 0.5f + 0.1f * u;
                        float tl = Mathf.Clamp01(tc + 0.2f), tr = Mathf.Clamp01(tc - 0.3f);
                        fv.Add(W3(L, O, ax, sc)); fc.Add(Grad(lUp ? tl : tr, f.sh));
                        fv.Add(W3(R, O, ax, sc)); fc.Add(Grad(lUp ? tr : tl, f.sh));
                    }
                    for (int k = 0; k + 1 < h; k++) { int a = b0 + 2 * k; Tri(a, a + 1, a + 2); Tri(a + 1, a + 3, a + 2); }
                }
            }
            // ---- animasi per-frame: coroutine hanya memberi target; View menghaluskan gerak (60 fps) dan membangun ulang bentuk ~30 Hz
            sealed class Drv : MonoBehaviour { public System.Action tick; void LateUpdate() { if (tick != null) tick(); } }
            MySpec dSp; bool driven;
            Vector3 tPos, cPos, vel, tAx, cAx, prevPos; float tSc, cSc, tRoar, cRoar, dPh, lastPh, lastT, phRate, acc, clock, speedSm;
            public void Drive(MySpec sp, Vector3 O, Vector3 ax, float sc, float phTarget, float roar)
            {
                if (root == null) return;
                float now = Time.time;
                dSp = sp;
                if (!driven)
                {
                    driven = true; cPos = tPos = prevPos = O; cAx = tAx = ax; cSc = tSc = sc; cRoar = tRoar = roar; dPh = lastPh = phTarget; lastT = now;
                    root.AddComponent<Drv>().tick = Tick;
                    spr = BuildArt(sp);
                    if (spr) ArtUpdate(); else Rebuild();
                    return;
                }
                float dt = now - lastT;
                if (dt > 0.001f) phRate = Mathf.Lerp(phRate, (phTarget - lastPh) / dt, 0.5f);
                lastPh = phTarget; lastT = now; tPos = O; tAx = ax; tSc = sc; tRoar = roar;
                dPh = Mathf.Lerp(dPh, phTarget, 0.5f);
            }
            void Tick()
            {
                float dt = Mathf.Min(0.05f, Time.deltaTime);
                clock += dt; dPh += phRate * dt;
                Vector3 before = cPos;
                cPos = Vector3.SmoothDamp(cPos, tPos, ref vel, 0.08f);
                float k = 1f - Mathf.Exp(-dt * 12f);
                cSc = Mathf.Lerp(cSc, tSc, k); cRoar = Mathf.Lerp(cRoar, tRoar, 1f - Mathf.Exp(-dt * 14f));
                cAx = Vector3.Slerp(cAx, tAx, 1f - Mathf.Exp(-dt * 10f));
                speedSm = Mathf.Lerp(speedSm, dt > 0f ? (cPos - before).magnitude / dt : 0f, 1f - Mathf.Exp(-dt * 6f));
                root.transform.position = cPos;
                acc += dt;
                if (spr) { ArtUpdate(); return; }
                if (acc >= 0.033f) { acc = 0f; Rebuild(); }
            }
            // ---- makhluk bergambar vektor (SkillSilhouette.Trace.cs): mesh poligon berwarna-gradasi, dibalik/digerakkan tiap frame
            bool spr; GameObject art, shadowGo; Mesh artMesh; TraceArt tr; Vector3[] artV; bool artCenter, artLeft; int artMode; float artW, artH;
            static readonly string[] WingedProf = { "garuda", "roc", "anzu", "griffin", "lamassu", "sphinx", "tengu", "thunder", "raven", "typhon", "nidhogg", "hippogriff", "chimera", "wyvern", "bahamut", "ziz", "simurgh", "crow3", "heron", "fafnir" };
            bool BuildArt(MySpec sp)
            {
                tr = TraceArt.Get(sp.id); if (tr == null) return false;
                float K = 0.0195f * 180f;                       // tinggi dasar (m) pada skala 1
                artH = K * 0.95f; artW = artH * tr.aspect;
                artCenter = sp.bas == "b" || sp.bas == "d" || sp.mot == "fly" || sp.bas == "o" || sp.bas == "k";
                artLeft = sp.id == "em_flw_6" || sp.id == "pr_mer_6" || sp.id == "sd_6" || sp.id == "e30_15_6";
                bool coil = sp.bas == "s" || sp.bas == "y" || sp.bas == "o" || sp.bas == "k" || sp.bas == "t" || sp.bas == "m" || sp.prof == "maw" || sp.prof == "kraken" || sp.prof == "ouro";
                bool wing = sp.bas == "b" || sp.bas == "d" || sp.Has("wing") || sp.Has("batwing") || System.Array.IndexOf(WingedProf, sp.prof) >= 0;
                artMode = coil ? 2 : wing ? 1 : 0;
                artV = new Vector3[tr.nx.Length];
                artMesh = new Mesh(); artMesh.MarkDynamic();
                tr.Pose(artV, artW, artH, artCenter, artLeft, artMode, 0f, 0f, 0f, 0f);
                artMesh.vertices = artV; artMesh.colors = tr.col; artMesh.triangles = tr.tris;
                art = new GameObject("beastArt"); art.transform.SetParent(root.transform, false);
                art.AddComponent<MeshFilter>().sharedMesh = artMesh;
                var r = art.AddComponent<MeshRenderer>(); r.sharedMaterial = CombatFx.Solid(null, Color.white);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                if (!artCenter)
                {
                    shadowGo = GameObject.CreatePrimitive(PrimitiveType.Quad); shadowGo.name = "beastShadow";
                    var c = shadowGo.GetComponent<Collider>(); if (c != null) Object.Destroy(c);
                    shadowGo.transform.SetParent(root.transform, false);
                    var sr = shadowGo.GetComponent<MeshRenderer>(); sr.sharedMaterial = CombatFx.Mat(CombatFx.Soft(), new Color(0f, 0f, 0f, 0.45f));
                    sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sr.receiveShadows = false;
                }
                root.SetActive(true);
                return true;
            }
            void ArtUpdate()
            {
                if (art == null || root == null || tr == null) return;
                var cam = Camera.main;
                Vector3 camF = cam != null ? cam.transform.forward : Vector3.forward; camF.y = 0f; camF = camF.sqrMagnitude < 1e-4f ? Vector3.forward : camF.normalized;
                Vector3 ax = cAx.sqrMagnitude < 1e-4f ? Vector3.forward : cAx.normalized;
                Vector3 camR = Vector3.Cross(Vector3.up, camF);
                float d = Vector3.Dot(ax, camR);
                if (d > 0.25f) bbSign = 1; else if (d < -0.25f) bbSign = -1;
                float flip = (bbSign > 0) ^ artLeft ? 1f : -1f;
                LastAx = camR * bbSign + ax * 0.3f; LastAx.y = 0f; LastAx = LastAx.sqrMagnitude < 1e-4f ? ax : LastAx.normalized;
                float run = Mathf.Clamp01(speedSm / 5f), k = Mathf.Max(0.02f, cSc);
                tr.Pose(artV, artW, artH, artCenter, artLeft, artMode, clock, dPh, run, cRoar);
                artMesh.vertices = artV; artMesh.RecalculateBounds();
                float step = Mathf.Abs(Mathf.Sin(dPh * 1.9f));
                float bob = artCenter ? Mathf.Sin(clock * 3.2f) * 0.03f * artH : step * 0.03f * artH * run;
                float tilt = -Mathf.Clamp(speedSm * 0.3f, 0f, 6f) * flip + cRoar * 4f * flip;
                art.transform.rotation = Quaternion.LookRotation(camF, Vector3.up) * Quaternion.Euler(0f, 0f, tilt);
                art.transform.localPosition = new Vector3(0f, bob * k, 0f);
                art.transform.localScale = new Vector3(flip * k, k * (1f + Mathf.Sin(clock * 2.4f) * 0.01f + cRoar * 0.05f), k);
                myHead = new Vector2(artW * 0.42f, artCenter ? artH * 0.08f : artH * 0.6f);   // posisi mulut untuk sinar serangan
                if (shadowGo != null)
                {
                    shadowGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    shadowGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    float sw = artW * k * 0.75f;
                    shadowGo.transform.localScale = new Vector3(sw, sw * 0.3f, 1f);
                }
            }
            void Rebuild()
            {
                if (dSp == null || root == null) return;
                var shape = MythShape(dSp, dPh, cRoar);
                float lean = Mathf.Clamp(speedSm * 0.012f, 0f, 0.2f) - cRoar * 0.08f;
                MyWarp(shape, clock, dPh, lean, dSp.bas);
                Set(shape, cPos, cAx.sqrMagnitude < 1e-4f ? Vector3.forward : cAx.normalized, cSc, 0.2f, true);
            }
            public void Kill() { if (root != null) Object.Destroy(root); if (fillMesh != null) Object.Destroy(fillMesh); if (artMesh != null) Object.Destroy(artMesh); }
            public void KillAfter(float t) { if (root != null) Object.Destroy(root, t); if (fillMesh != null) Object.Destroy(fillMesh, t); if (artMesh != null) Object.Destroy(artMesh, t); }
        }

        static IEnumerator W(float t) { yield return new WaitForSeconds(t); }
        static IEnumerator Late(float t, System.Action a) { yield return new WaitForSeconds(t); if (a != null) a(); }

        static Vector3 Flat(Vector3 f) { f.y = 0f; return f.sqrMagnitude < 1e-4f ? Vector3.forward : f.normalized; }

        // ------------------------------------------------------------------ NAGA
        /// <summary>Naga raksasa melayang di atas pemain, kepala menghadap arah tebasan (fase "naga di belakang pendekar").</summary>
        public static void DragonHover(Vector3 pos, Vector3 fwd, float secs, Col c, float sc)
        {
            CombatFx.Host.I.StartCoroutine(DragonHoverR(pos, Flat(fwd), secs, c, sc));
        }

        static IEnumerator DragonHoverR(Vector3 pos, Vector3 f, float secs, Col c, float sc)
        {
            float t = 0f; var v = new View(c);
            while (t < secs)
            {
                float grow = Mathf.Clamp01(t / (secs * 0.6f));
                v.Drive(SpDragon, pos + Vector3.up * (1.2f + Mathf.Sin(t * 5f) * 0.3f), f, sc * Mathf.Lerp(0.3f, 1f, grow), t * 7f, 0.6f);
                if (Random.value < 0.5f) CombatFx.Burst(pos + Vector3.up * 3f - f * Random.Range(0f, 8f), c.m, 2, 0.5f, 1f, 0.6f, 0.12f);
                yield return W(0.07f);
                t += 0.07f;
            }
            v.Kill();
        }

        /// <summary>Naga menerjang lurus ke depan: siluet utuh bergerak, tanah di bawahnya hancur, ledakan kiri-kanan, rahang menggigit di ujung.</summary>
        public static void DragonCharge(Vector3 origin, Vector3 fwd, float range, Col c, float sc, string job)
        {
            CombatFx.Host.I.StartCoroutine(DragonChargeR(origin, Flat(fwd), range, c, sc, job));
        }

        static IEnumerator DragonChargeR(Vector3 o, Vector3 f, float range, Col c, float sc, string job)
        {
            Vector3 right = Vector3.Cross(Vector3.up, f);
            float speed = 34f, t = 0f, nextHit = 0f; int side = 1; var v = new View(c);
            CombatFx.Flash(o + Vector3.up * 2f, c.h, 20f, 5f, 0.3f);
            while (speed * t < range)
            {
                float d = speed * t; Vector3 head = o + f * (d + 3f);
                v.Drive(SpDragon, head + Vector3.up * (0.8f + Mathf.Sin(t * 6f) * 0.4f), f, sc, t * 11f, 0.6f);
                CombatFx.Flash(head + Vector3.up * 4.2f, c.h, 10f, 3f, 0.1f);
                Vector3 g = o + f * d;
                CombatFx.Dust(g, 6); CombatFx.Burst(g + Vector3.up * 0.3f, c.m, 6, 2.5f * sc, 3f, 0.7f, 0.16f);
                if (t >= nextHit)
                {
                    nextHit = t + 0.09f; side = -side;
                    Vector3 q = g + right * side * Random.Range(1.5f, 4.5f) * sc;
                    MagicFx.Column(q, 1.1f * sc, 8f, c.m, 0.6f); CombatFx.ShockRing(q, 3.4f * sc, c.m, 0.3f);
                    MagicFx.Crack(g, 3f * sc, 3f); MagicFx.Debri(g, 3f * sc, 9, 1.2f);
                    ThirdPersonCamera.Shake(0.25f, 0.12f);
                }
                yield return W(0.05f);
                t += 0.05f;
            }
            // gigitan penutup: rahang menutup, ledakan kolom besar
            Vector3 fin = o + f * range;
            for (int i = 0; i < 3; i++) { v.Drive(SpDragon, fin + f * 3f + Vector3.up * 0.8f, f, sc * (1f + i * 0.08f), 8f, 0.6f); yield return W(0.06f); }
            v.Kill();
            MagicFx.Column(fin, 4f * sc, 30f, c.m, 1.2f); CombatFx.Beam(fin, fin + Vector3.up * 34f, c.h, 1.5f, 0.9f);
            CombatFx.ShockRing(fin, 14f * sc, c.h, 0.8f); CombatFx.ShockRing(fin, 9f * sc, c.m, 0.6f);
            CombatFx.Burst(fin + Vector3.up * 2f, c.m, 50, 6f * sc, 9f, 1.5f, 0.22f); MagicFx.RockBurst(fin, 5f, 18);
            ThirdPersonCamera.Shake(0.7f, 0.55f);
        }

        // ------------------------------------------------------------------ KAWANAN SERIGALA
        static IEnumerator WolfPack(PlayerCombat pc, Vector3 C, Vector3 F, Col col)
        {
            Vector3 P = pc.transform.position - Vector3.up * 0.9f;
            Vector3 right = Vector3.Cross(Vector3.up, F);
            Vector3[] offs = { Vector3.zero, right * 4f - F * 3f, -right * 4f - F * 3f, right * 8f - F * 6f, -right * 8f - F * 6f };
            float[] scl = { 1.4f, 1.0f, 1.0f, 0.85f, 0.85f };
            // munculnya: cincin sihir, kilatan, siluet terbentuk
            MagicFx.MagicCircle(P, 6f, col.m, 1f);
            for (int w = 0; w < 5; w++) { CombatFx.Flash(P + offs[w] + Vector3.up * 2f, col.h, 8f, 2f, 0.3f); CombatFx.ShockRing(P + offs[w], 2f, col.m, 0.3f); }
            yield return W(0.3f);
            Vector3 dir = (C - P); dir.y = 0f; float dist = dir.magnitude; Vector3 dn = dir.sqrMagnitude > 0.01f ? dir.normalized : F;
            float speed = 24f, t = 0f, travel = Mathf.Max(6f, dist - 2f);
            var views = new View[5]; for (int w = 0; w < 5; w++) views[w] = new View(col);
            while (speed * t < travel)
            {
                for (int w = 0; w < 5; w++)
                {
                    Vector3 pos = P + offs[w] + dn * (speed * t) + Vector3.up * (Mathf.Abs(Mathf.Sin(t * 14f + w)) * 0.6f);
                    views[w].Drive(SpWolf, pos, dn, scl[w] * 0.8f, t * 18f + w, 0.4f);
                    if (Random.value < 0.4f) CombatFx.Dust(pos, 2);
                }
                yield return W(0.05f);
                t += 0.05f;
            }
            // lompat & gigit di titik tujuan, dengan goresan taring
            for (int w = 0; w < 5; w++)
            {
                Vector3 tgt = C + new Vector3(Mathf.Cos(w * 1.26f), 0f, Mathf.Sin(w * 1.26f)) * 1.6f;
                int ww = w;
                CombatFx.Host.I.StartCoroutine(Late(0.08f * w, () =>
                {
                    views[ww].Drive(SpWolf, tgt + Vector3.up * 1.4f, -(new Vector3(Mathf.Cos(ww * 1.26f), 0f, Mathf.Sin(ww * 1.26f))), scl[ww] * 0.9f, 1.2f, 0.4f); views[ww].KillAfter(0.5f);
                    for (int k = -1; k <= 1; k++) CombatFx.Beam(tgt + Vector3.up * 3f + right * k * 0.7f + F * 1f, tgt + Vector3.up * 0.3f + right * k * 0.7f - F * 1f, col.h, 0.18f, 0.3f);
                    CombatFx.HitSpark(tgt + Vector3.up, col.h, 1.2f, 14); CombatFx.ShockRing(tgt, 3f, col.m, 0.3f);
                    ThirdPersonCamera.Shake(0.2f, 0.12f);
                }));
            }
            yield return W(0.5f);
            // lolongan: lingkaran perburuan, kawanan memudar jadi bara
            for (int i = 0; i < 24; i++) { float a = i * Mathf.PI / 12f; Vector3 q = C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 7f; CombatFx.Beam(q, q + Vector3.up * 4f, col.m, 0.2f, 0.5f); CombatFx.Burst(q, col.h, 3, 0.4f, 2f, 0.9f, 0.1f); }
            MagicFx.MagicCircle(C, 7f, col.m, 1.2f); CombatFx.ShockRing(C, 11f, col.h, 0.7f); ThirdPersonCamera.Shake(0.5f, 0.45f);
        }

        // ------------------------------------------------------------------ FENIKS
        static IEnumerator PhoenixRebirth(PlayerCombat pc, Col col)
        {
            Vector3 P = pc.transform.position - Vector3.up * 0.9f; Vector3 right = Vector3.Cross(Vector3.up, Flat(pc.transform.forward));
            MagicFx.MagicCircle(P, 5f, col.m, 1.4f); var v = new View(col);
            for (float t = 0f; t < 1.6f; t += 0.07f)
            {
                float k = Mathf.Clamp01(t / 0.9f);
                v.Drive(SpPhoenix, P + Vector3.up * (k * 2f), right, Mathf.Lerp(0.25f, 1.25f, k), t * 9f, 0.3f);
                CombatFx.Burst(P + Vector3.up * 1f, col.m, 3, 1.2f, 4f, 0.8f, 0.14f);
                yield return W(0.07f);
            }
            v.Kill();
            // sayap menghentak keluar: ledakan lingkar + bulu api
            MagicFx.Column(P, 2f, 22f, col.m, 1.2f); CombatFx.Flash(P + Vector3.up * 5f, col.h, 30f, 7f, 0.5f);
            for (int i = 0; i < 3; i++) CombatFx.ShockRing(P + Vector3.up * 0.3f, 8f + 5f * i, i % 2 == 0 ? col.m : col.h, 0.6f);
            CombatFx.Burst(P + Vector3.up * 5f, col.h, 50, 8f, 5f, 1.6f, 0.2f); ThirdPersonCamera.Shake(0.6f, 0.5f);
        }

        static IEnumerator PhoenixAscend(PlayerCombat pc, Vector3 C, Vector3 F, Col col)
        {
            Vector3 P = pc.transform.position - Vector3.up * 0.9f; Vector3 f = Flat(F);
            MagicFx.MagicCircle(P, 4f, col.m, 1f); var v = new View(col);
            // naik diagonal ke langit
            for (float t = 0f; t < 1.1f; t += 0.06f)
            {
                Vector3 pos = P + f * (t * 6f) + Vector3.up * (1f + t * t * 16f);
                v.Drive(SpPhoenix, pos, f, 0.7f + t * 0.6f, t * 12f, 0.3f);
                CombatFx.Burst(pos - f * 3f, col.k, 3, 0.8f, 0.6f, 0.9f, 0.16f);
                yield return W(0.06f);
            }
            Vector3 apex = P + f * 7f + Vector3.up * 20f;
            for (int i = 0; i < 5; i++) { v.Drive(SpPhoenix, apex, f, 1.5f, i * 1.5f, 0.3f); CombatFx.Flash(apex, col.h, 20f, 5f, 0.15f); yield return W(0.06f); }
            // terjun ke target
            Vector3 dive = (C - apex).normalized;
            Vector3 dflat = Flat(C - apex);
            for (float t = 0f; t < 0.5f; t += 0.05f)
            {
                Vector3 pos = apex + dive * (Vector3.Distance(apex, C) * t / 0.5f);
                v.Drive(SpPhoenix, pos, dflat, 1.5f, Mathf.Asin(0.7f), 0.3f);
                CombatFx.Beam(pos - dive * 8f, pos, col.m, 1.2f, 0.2f);
                yield return W(0.05f);
            }
            v.Kill();
            MagicFx.Column(C, 4f, 34f, col.m, 1.4f); CombatFx.ShockRing(C, 14f, col.h, 0.8f); CombatFx.ShockRing(C, 9f, col.m, 0.6f);
            CombatFx.Burst(C + Vector3.up * 2f, col.h, 60, 7f, 10f, 1.6f, 0.22f); MagicFx.Crack(C, 8f, 4f); ThirdPersonCamera.Shake(0.7f, 0.6f);
        }

        static IEnumerator EternalRebirth(PlayerCombat pc, Vector3 C, Vector3 F, Col col)
        {
            Vector3 P = pc.transform.position - Vector3.up * 0.9f; Vector3 right = Vector3.Cross(Vector3.up, Flat(F));
            Vector3 core = C;
            MagicFx.MagicCircle(core, 9f, col.m, 2f);
            var vf = new View(col); var vs = new View[3]; for (int j = 0; j < 3; j++) vs[j] = new View(col);
            // tiga feniks samping mengitari, satu feniks depan raksasa terbit
            for (float t = 0f; t < 2.0f; t += 0.07f)
            {
                float k = Mathf.Clamp01(t / 1.0f);
                vf.Drive(SpPhoenix, core + Vector3.up * k * 3f - Flat(F) * 3f, right, Mathf.Lerp(0.3f, 2.0f, k), t * 8f, 0.3f);
                for (int j = 0; j < 3; j++)
                {
                    float a = t * 2.2f + j * Mathf.PI * 2f / 3f; Vector3 pos = core + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 9f + Vector3.up * (4f + j);
                    Vector3 tang = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                    vs[j].Drive(SpPhoenix, pos, tang, 0.7f, t * 10f + j, 0.3f);
                }
                if (Mathf.Repeat(t, 0.5f) < 0.07f) CombatFx.ShockRing(core, 4f + t * 4f, col.m, 0.4f);
                yield return W(0.07f);
            }
            vf.Kill(); for (int j = 0; j < 3; j++) vs[j].Kill();
            for (int j = 0; j < 3; j++) { float a = j * Mathf.PI * 2f / 3f; CombatFx.Beam(core + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 9f + Vector3.up * 5f, core + Vector3.up * 1f, col.h, 1.3f, 0.4f); }
            MagicFx.Column(core, 5f, 40f, col.m, 1.6f); CombatFx.Flash(core + Vector3.up * 3f, col.h, 40f, 9f, 0.7f);
            for (int i = 0; i < 4; i++) { CombatFx.ShockRing(core, 8f + 5f * i, i % 2 == 0 ? col.m : col.h, 0.7f); yield return W(0.08f); }
            CombatFx.Burst(core + Vector3.up * 4f, col.h, 70, 10f, 12f, 1.8f, 0.24f); ThirdPersonCamera.Shake(0.75f, 0.65f);
        }

        // ------------------------------------------------------------------ LEVIATHAN
        static IEnumerator LeviathanWrath(PlayerCombat pc, Vector3 C, Vector3 F, Col col)
        {
            Vector3 f = Flat(F), P = pc.transform.position - Vector3.up * 0.9f;
            MagicFx.MagicCircle(C, 9f, col.m, 1.6f); MagicFx.Column(C, 5f, 14f, col.m, 1.2f); var v = new View(col);
            for (int i = 0; i < 3; i++) { CombatFx.ShockRing(C, 5f + 4f * i, col.h, 0.6f); yield return W(0.08f); }
            // bangkit dari air, mulut terbuka lebar
            for (float t = 0f; t < 1.1f; t += 0.07f)
            {
                float k = Mathf.Clamp01(t / 0.8f);
                v.Drive(SpLev, C - f * 2f - Vector3.up * (1f - k) * 9f, f, 0.69f, t * 6f, Mathf.Min(1f, t));
                CombatFx.Burst(C + Vector3.up * 2f, col.h, 4, 3f, 6f, 0.9f, 0.14f);
                yield return W(0.07f);
            }
            // menghantam ke depan: mulut menutup, ombak raksasa menyapu
            for (float t = 0f; t < 0.5f; t += 0.05f)
            {
                float k = t / 0.5f;
                v.Drive(SpLev, C - f * 2f + f * (k * 7f) - Vector3.up * (k * 4f), f, 0.69f, 5f, 1f - k);
                yield return W(0.05f);
            }
            v.Kill();
            Vector3 hit = C + f * 6f;
            MagicFx.Column(hit, 5f, 26f, col.m, 1.4f); CombatFx.Flash(hit + Vector3.up * 3f, col.h, 30f, 6f, 0.5f);
            for (int i = 0; i < 5; i++) { CombatFx.ShockRing(hit, 6f + 4f * i, i % 2 == 0 ? col.m : col.h, 0.7f); yield return W(0.07f); }
            CombatFx.Burst(hit + Vector3.up * 3f, col.m, 70, 9f, 11f, 1.6f, 0.22f); ThirdPersonCamera.Shake(0.75f, 0.6f);
        }

        static IEnumerator LeviathanCall(PlayerCombat pc, Vector3 C, Vector3 F, Col col)
        {
            Vector3 f = Flat(F), right = Vector3.Cross(Vector3.up, f);
            MagicFx.MagicCircle(C, 11f, col.m, 2f);
            for (int i = 0; i < 24; i++) { float a = i * Mathf.PI / 12f; CombatFx.Beam(C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 11f, C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 11f + Vector3.up * 3f, col.m, 0.3f, 0.5f); }
            var va = new View(col); var vb = new View(col);
            // dua leviathan muncul dari kiri & kanan, melengkung saling berhadapan ke titik tengah (pusaran)
            for (float t = 0f; t < 1.6f; t += 0.07f)
            {
                float k = Mathf.Clamp01(t / 1.0f);
                va.Drive(SpLev, C + right * 8f - Vector3.up * (1f - k) * 10f, -right, 0.6f, t * 5f, Mathf.Clamp01(t / 1.2f));
                vb.Drive(SpLev, C - right * 8f - Vector3.up * (1f - k) * 10f, right, 0.6f, t * 5f + 2f, Mathf.Clamp01(t / 1.2f));
                for (int s = 0; s < 3; s++) { float a = t * 6f + s * 2.1f; Vector3 q = C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (7f - t * 3f); CombatFx.Beam(q, q + new Vector3(-Mathf.Sin(a), 0.1f, Mathf.Cos(a)) * 2.5f, col.h, 0.3f, 0.25f); }
                yield return W(0.07f);
            }
            va.Kill(); vb.Kill();
            // gigitan serentak: pusaran runtuh
            MagicFx.Column(C, 6f, 36f, col.m, 1.6f); CombatFx.Flash(C + Vector3.up * 3f, col.h, 40f, 9f, 0.7f);
            for (int i = 0; i < 6; i++) { CombatFx.ShockRing(C, 5f + 3.5f * i, i % 2 == 0 ? col.m : col.h, 0.7f); yield return W(0.07f); }
            CombatFx.Burst(C + Vector3.up * 3f, col.h, 80, 11f, 12f, 1.8f, 0.24f); ThirdPersonCamera.Shake(0.8f, 0.65f);
        }

        // ------------------------------------------------------------------ titik masuk dari SkillSpectacle
        public static void Start(SkillDef s, PlayerCombat pc, Vector3 C, Vector3 F, Color m, Color h, Color k)
        {
            var col = new Col { m = m, h = h, k = k };
            var host = CombatFx.Host.I;
            switch (s.id)
            {
                case "rg_6": host.StartCoroutine(WolfPack(pc, C, F, col)); break;
                case "fm_wrd_6": host.StartCoroutine(PhoenixRebirth(pc, col)); break;
                case "ph_6": host.StartCoroutine(PhoenixAscend(pc, C, F, col)); break;
                case "e30_7_6": host.StartCoroutine(EternalRebirth(pc, C, F, col)); break;
                case "td_6": host.StartCoroutine(LeviathanWrath(pc, C, F, col)); break;
                case "e30_13_6": host.StartCoroutine(LeviathanCall(pc, C, F, col)); break;
            }
        }
    }
}
