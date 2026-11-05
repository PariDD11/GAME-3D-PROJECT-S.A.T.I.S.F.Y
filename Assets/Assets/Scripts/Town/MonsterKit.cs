using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HunterGuild
{
    /// <summary>
    /// Slot bahan monster. Indeks Sl.* dipakai ulang sebagai saluran GeoBag; maknanya khusus monster
    /// (Eye dan Ember dikeluarkan ke mesh terpisah bershader Glow, sisanya bershader URP Lit lalu di-toon oleh AnimeToon).
    /// </summary>
    public static class MS
    {
        public const int Body = Sl.CoatA, Dark = Sl.CoatB, Pale = Sl.CoatC, Bone = Sl.Marble, Claw = Sl.DarkStone, Pupil = Sl.Dark,
                         Mouth = Sl.Lip, Accent = Sl.Gold, ClothA = Sl.Cloth, ClothB = Sl.ClothB, Metal = Sl.Metal, Leather = Sl.Leather,
                         Rock = Sl.Stone, Wood = Sl.Wood, Hair = Sl.Hair, Skin = Sl.SkinS, Eye = Sl.Eye, Ember = Sl.Glow;
    }

    /// <summary>Satu bagian model (pivot + mesh padat + mesh bercahaya). Geometri ditulis dalam ruang bentuk; pivot menjadi titik putar animasi.</summary>
    public sealed class MkPart
    {
        public Transform t;
        public Vector3 o;               // letak pivot dalam ruang bentuk
        public GeoBag S, G;             // S = padat (toon), G = bercahaya (mata, bara)
    }

    /// <summary>Animasi prosedural satu monster: daftar penutup (closure) yang membaca fase jalan, waktu, kecepatan, serangan.</summary>
    public sealed class MonsterRig
    {
        public Transform root;
        public float refSpeed = 3.5f, strideK = 3.6f;
        public float phase, time, spd, spd01, wind, lunge;
        public Texture2D pal;                       // palet warna bagian padat (1 texel per slot); Tint menulis ulang texel
        public Color[] palBase;
        public Material solidMat;
        public Material[] glowMats = new Material[0];
        Color[] palBuf; Color lastTo; float lastAmt;
        readonly List<Action<MonsterRig>> anims = new List<Action<MonsterRig>>();

        public void Add(Action<MonsterRig> a) { anims.Add(a); }

        /// <summary>speed = laju gerak (m/dtk), winding = sedang membidik/telegraph (tubuh mengambil ancang-ancang).</summary>
        public void Tick(float speed, float dt, bool winding)
        {
            if (dt <= 0f) return;
            time += dt; spd = speed; spd01 = Mathf.Clamp01(speed / Mathf.Max(0.5f, refSpeed));
            phase += speed * dt * strideK;
            wind = Mathf.MoveTowards(wind, winding ? 1f : 0f, dt * 4f);
            lunge = Mathf.MoveTowards(lunge, 0f, dt * 3.2f);
            for (int i = 0; i < anims.Count; i++) anims[i](this);
        }

        public void Lunge() { lunge = 1f; }

        /// <summary>Warna semua bagian padat bergeser ke "to" sebesar amount (0 = warna asli). Mata/bara tidak ikut.</summary>
        public void Tint(Color to, float amount)
        {
            if (pal == null || palBase == null) return;
            if (amount < 0.001f) amount = 0f;
            if (Mathf.Abs(amount - lastAmt) < 0.004f && (amount == 0f || to == lastTo)) return;
            lastAmt = amount; lastTo = to;
            if (palBuf == null) palBuf = new Color[palBase.Length];
            for (int i = 0; i < palBase.Length; i++) palBuf[i] = amount <= 0f ? palBase[i] : Color.Lerp(palBase[i], to, amount);
            pal.SetPixels(palBuf); pal.Apply(false);
        }

        /// <summary>Bebaskan tekstur dan bahan milik monster ini (mesh berbagi lewat cache, jadi tidak dihapus).</summary>
        public void Dispose()
        {
            if (pal != null) UnityEngine.Object.Destroy(pal);
            if (solidMat != null) UnityEngine.Object.Destroy(solidMat);
            for (int i = 0; i < glowMats.Length; i++) if (glowMats[i] != null) UnityEngine.Object.Destroy(glowMats[i]);
            pal = null; solidMat = null; glowMats = new Material[0];
        }

        // ---- gerak umum ----
        /// <summary>Kaki: pinggul berayun (derajat), lutut menekuk saat kaki terangkat. off = geseran fase (radian).</summary>
        public void Leg(Transform hip, Transform knee, float off, float swing, float bend)
        {
            Quaternion h0 = hip.localRotation, k0 = knee != null ? knee.localRotation : Quaternion.identity;
            Add(delegate (MonsterRig r)
            {
                float s = Mathf.Sin(r.phase + off) * swing * r.spd01;
                hip.localRotation = h0 * Quaternion.AngleAxis(s, Vector3.right);
                if (knee != null) knee.localRotation = k0 * Quaternion.AngleAxis(Mathf.Max(0f, Mathf.Cos(r.phase + off)) * bend * r.spd01, Vector3.right);
            });
        }

        /// <summary>Goyang pelan (ekor, telinga, jubah): amp derajat pada sumbu axis; ikut lebih cepat saat bergerak.</summary>
        public void Sway(Transform tr, Vector3 axis, float amp, float freq, float off, float follow)
        {
            Quaternion q0 = tr.localRotation;
            Add(delegate (MonsterRig r)
            {
                float a = Mathf.Sin(r.time * freq + off) * (amp + follow * r.spd01);
                tr.localRotation = q0 * Quaternion.AngleAxis(a, axis);
            });
        }

        /// <summary>Napas: skala Y naik turun sedikit.</summary>
        public void Breath(Transform tr, float amp, float freq)
        {
            Vector3 s0 = tr.localScale;
            Add(delegate (MonsterRig r) { float k = Mathf.Sin(r.time * freq) * amp; tr.localScale = new Vector3(s0.x * (1f - k * 0.5f), s0.y * (1f + k), s0.z * (1f - k * 0.5f)); });
        }
    }

    /// <summary>Konteks pembangunan satu monster: bahan, cache mesh, hierarki bagian.</summary>
    public sealed class MkCtx
    {
        public MonsterDef def;
        public MonsterRig rig;
        public Transform root;
        public string shape, variant, key;
        public Color[] col;
        public readonly Material[] mats = new Material[Sl.Count];      // hanya bahan bercahaya (Eye/Ember)
        public Material solidMat;                                       // satu bahan palet untuk semua bagian padat
        public int parts;

        public bool V(string name) { return variant == name; }

        /// <summary>Bahan bercahaya per slot (mata/bara). Slot padat tidak memakai ini: lihat Solid.</summary>
        public Material M(int slot)
        {
            if (mats[slot] == null)
                mats[slot] = MonsterKit.MakeGlow("mk_" + slot, col[slot], slot == MS.Eye ? 1.25f : 1.0f);
            return mats[slot];
        }

        public Material Solid()
        {
            if (solidMat == null) { solidMat = MonsterKit.MakeLit("mk_pal", Color.white, 0.12f); rig.solidMat = solidMat; }
            return solidMat;
        }

        /// <summary>Panggil setelah semua bagian dibangun: tulis palet warna akhir (varian boleh mengubah col kapan saja) ke tekstur.</summary>
        public void Finish()
        {
            var t = new Texture2D(Sl.Count, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var px = new Color[Sl.Count];
            for (int i = 0; i < px.Length; i++) { Color k = col[i]; k.a = 1f; px[i] = k; }
            t.SetPixels(px); t.Apply(false);
            rig.pal = t; rig.palBase = px;
            if (solidMat != null)
            {
                if (solidMat.HasProperty("_BaseMap")) solidMat.SetTexture("_BaseMap", t);
                solidMat.mainTexture = t;
            }
            var gl = new List<Material>();
            for (int i = 0; i < mats.Length; i++) if (mats[i] != null) gl.Add(mats[i]);
            rig.glowMats = gl.ToArray();
        }

        /// <summary>Buat satu bagian. build hanya dijalankan saat mesh belum ada di cache (kunci = bentuk+varian+nama bagian).</summary>
        public MkPart New(string name, MkPart parent, Vector3 pivot, Action<MkPart> build)
        {
            var go = new GameObject(name);
            var tr = go.transform;
            tr.SetParent(parent != null ? parent.t : root, false);
            tr.localPosition = pivot - (parent != null ? parent.o : Vector3.zero);
            var p = new MkPart { t = tr, o = pivot };
            string ck = key + "/" + name;
            MonsterKit.CachedPart cm;
            if (!MonsterKit.Cache.TryGetValue(ck, out cm) || !cm.Alive())
            {
                p.S = new GeoBag(); p.G = new GeoBag();
                p.S.Push(-pivot, 0f); p.G.Push(-pivot, 0f);
                if (build != null) build(p);
                cm = new MonsterKit.CachedPart();
                int[] us = new int[0], ug = new int[0];
                if (p.S.VertexCount > 0) { cm.solid = p.S.ToMeshPalette(ck + "/s", out us); cm.hasSolid = true; }
                if (p.G.VertexCount > 0) { cm.glow = p.G.ToMesh(ck + "/g", null, out ug); cm.gSlots = ug; cm.hasGlow = true; }
                MonsterKit.Cache[ck] = cm;
            }
            parts++;
            if (cm.solid != null)
            {
                var g = new GameObject("s"); g.transform.SetParent(tr, false);
                g.AddComponent<MeshFilter>().sharedMesh = cm.solid;
                var mr = g.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Solid();
            }
            if (cm.glow != null)
            {
                var g = new GameObject("g"); g.transform.SetParent(tr, false);
                g.AddComponent<MeshFilter>().sharedMesh = cm.glow;
                var mr = g.AddComponent<MeshRenderer>();
                var ms = new Material[cm.gSlots.Length];
                for (int i = 0; i < ms.Length; i++) ms[i] = M(cm.gSlots[i]);
                mr.sharedMaterials = ms; mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            return p;
        }
    }

    /// <summary>
    /// Pembangun model monster dari kode (menggantikan tumpukan primitif Unity). Satu fungsi per bentuk (MonsterDef.shape) dengan varian per id.
    /// Mesh di-cache per bentuk+varian+bagian dan dipakai bersama semua individu; bahan dibuat per individu.
    /// </summary>
    public static partial class MonsterKit
    {
        public sealed class CachedPart
        {
            public Mesh solid, glow; public int[] gSlots; public bool hasSolid, hasGlow;
            public bool Alive() { return (!hasSolid || solid != null) && (!hasGlow || glow != null); }
        }

        public static readonly Dictionary<string, CachedPart> Cache = new Dictionary<string, CachedPart>();

        /// <summary>Bangun visual monster di bawah root. Mengembalikan null bila bentuk belum punya model baru (pemanggil memakai model primitif lama).</summary>
        public static MonsterRig Build(MonsterDef def, Transform root)
        {
            if (def == null || root == null) return null;
            var c = new MkCtx();
            c.def = def; c.root = root; c.shape = def.shape ?? ""; c.variant = VariantOf(def); c.key = c.shape + ":" + c.variant;
            c.rig = new MonsterRig(); c.rig.root = root; c.rig.refSpeed = Mathf.Max(1f, def.speed);
            c.col = DefaultPal(def.color);
            if (!Dispatch(c)) { c.rig.Dispose(); return null; }
            c.Finish();
            return c.rig;
        }

        static readonly Dictionary<string, float> refH = new Dictionary<string, float>();

        /// <summary>Tinggi (ruang bentuk, sebelum dikali def.size) model bawaan monster ini. Acuan ukuran bagi model pengganti (MonsterSwap).</summary>
        public static float RefHeight(MonsterDef def)
        {
            if (def == null) return 1.5f;
            string k = (def.shape ?? "") + ":" + VariantOf(def);
            float h;
            if (refH.TryGetValue(k, out h)) return h;
            h = Mathf.Max(0.5f, def.height);
            var tmp = new GameObject("mk_ref");
            try
            {
                var rg = Build(def, tmp.transform);
                if (rg != null)
                {
                    var rs = tmp.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0)
                    {
                        Bounds b = rs[0].bounds;
                        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                        h = Mathf.Max(0.3f, b.max.y);
                    }
                    rg.Dispose();
                }
            }
            catch (Exception ex) { Debug.LogWarning("[MonsterKit] tinggi acuan " + k + ": " + ex.Message); }
            finally { UnityEngine.Object.DestroyImmediate(tmp); }
            refH[k] = h;
            return h;
        }

        static bool Dispatch(MkCtx c)
        {
            switch (c.shape)
            {
                case "wolf": Wolf(c); return true;
                case "bandit": case "leader": Bandit(c); return true;
                case "archer": if (c.V("bone")) Skeleton(c, true); else Bandit(c); return true;
                case "goblin": Goblin(c); return true;
                case "troll": Troll(c); return true;
                case "zombie": Zombie(c); return true;
                case "skeleton": Skeleton(c, false); return true;
                case "mummy": Mummy(c); return true;
                case "dummy": Dummy(c); return true;
                case "witch": Witch(c); return true;
                case "necro": Necro(c); return true;
                case "wraith": Wraith(c); return true;
                case "slime": Slime(c); return true;
                case "chicken": Chicken(c); return true;
                case "scorpion": Scorpion(c); return true;
                case "golem": Golem(c); return true;
                case "boar": Boar(c); return true;
                case "bull": Bull(c); return true;
                case "cerberus": Cerberus(c); return true;
                case "sheep": Sheep(c); return true;
                case "goat": Goat(c); return true;
                case "cow": Cow(c); return true;
                case "lizard": Lizard(c); return true;
                default: return false;
            }
        }

        /// <summary>Varian bentuk menurut id monster (aksesori/geometri tambahan). Kosong = bentuk dasar.</summary>
        public static string VariantOf(MonsterDef d)
        {
            switch (d.id)
            {
                case "frost_wolf": return "frost";
                case "hellhound": case "tbs_20": return "hell";
                case "heat_jackal": return "jackal";
                case "alpha_wolf": return "alpha";
                case "dire_wolf": return "dire";
                case "hb_lerai": return "frost";
                case "imp": return "imp";
                case "ember_imp": return "ember";
                case "tbs_10": return "king";
                case "swamp_troll": case "hb_frontier": return "moss";
                case "ogre": return "ogre";
                case "ogre_chief": return "chief";
                case "harvest_brute": return "pumpkin";
                case "demon_brute": return "demon";
                case "tbs_50": return "stone";
                case "tbs_90": return "king";
                case "hb_bone": return "knight";
                case "tbs_40": return "pharaoh";
                case "grave_knight": return "knight";
                case "arena_warden": return "warden";
                case "gladiator_elite": return "gladiator";
                case "srank_blade": return "blade";
                case "tbs_30": return "dark";
                case "bone_archer": return "bone";
                case "srank_shadow": return "shadow";
                case "fire_cultist": return "fire";
                case "dark_cultist": return "dark";
                case "srank_ash": case "ash_wraith": return "ash";
                case "tbs_80": return "abyss";
                case "hb_swamp": return "hag";
                case "bone_king": return "king";
                case "demon_lord": return "demon";
                case "tbs_100": return "tower";
                case "hb_demon": return "echo";
                case "banshee": return "banshee";
                case "tbs_60": return "queen";
                case "hb_desert": return "gold";
                case "magma_golem": case "hb_volcano": return "magma";
                case "tbs_70": return "obsidian";
                case "pollen_boar": return "pollen";
                case "lava_lizard": return "lava";
                case "ignarok": return "boss";
                default: return "";
            }
        }

        // ------------------------------------------------------------------ bahan
        public static Color[] DefaultPal(Color c)
        {
            var p = new Color[Sl.Count];
            c.a = 1f;
            p[MS.Body] = c; p[MS.Dark] = Dk(c, 0.55f); p[MS.Pale] = Mix(c, new Color(0.96f, 0.93f, 0.86f), 0.6f);
            p[MS.Bone] = new Color(0.93f, 0.9f, 0.8f); p[MS.Claw] = new Color(0.2f, 0.17f, 0.15f); p[MS.Pupil] = new Color(0.04f, 0.03f, 0.04f);
            p[MS.Mouth] = new Color(0.58f, 0.16f, 0.18f); p[MS.Accent] = new Color(0.92f, 0.72f, 0.24f);
            p[MS.ClothA] = new Color(0.36f, 0.27f, 0.2f); p[MS.ClothB] = new Color(0.55f, 0.12f, 0.1f);
            p[MS.Metal] = new Color(0.62f, 0.64f, 0.68f); p[MS.Leather] = new Color(0.3f, 0.19f, 0.12f);
            p[MS.Rock] = new Color(0.52f, 0.49f, 0.46f); p[MS.Wood] = new Color(0.44f, 0.3f, 0.17f); p[MS.Hair] = Dk(c, 0.7f);
            p[MS.Skin] = new Color(0.8f, 0.64f, 0.52f);
            p[MS.Eye] = new Color(1f, 0.25f, 0.12f); p[MS.Ember] = new Color(1f, 0.55f, 0.15f);
            return p;
        }

        public static Material MakeLit(string name, Color c, float smooth)
        {
            var m = new Material(RT.Sh);
            m.name = name; m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        public static Material MakeGlow(string name, Color c, float intensity)
        {
            Shader sh = Shader.Find("HunterGuild/Glow");
            if (sh == null) return MakeLit(name, c, 0.3f);
            var m = new Material(sh);
            m.name = name; m.color = c;
            if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", intensity);
            return m;
        }

        // ------------------------------------------------------------------ komponen kecil yang dipakai banyak bentuk
        /// <summary>Sepasang mata: bola bercahaya (G) + pupil gelap di depannya (S). pos = mata kanan (x positif); fwd = arah pandang.</summary>
        public static void EyePair(MkPart p, Vector3 pos, float r, Vector3 fwd, float pupil = 0.5f, float flat = 0.7f)
        {
            Quaternion q = Face(fwd, Vector3.up);
            for (int s = 0; s < 2; s++)
            {
                Vector3 e = s == 0 ? pos : Mx(pos);
                Ell(p.G, MS.Eye, e, new Vector3(r, r * 0.92f, r * flat), q, 5, 8);
                Ell(p.S, MS.Pupil, e + fwd.normalized * (r * flat * 0.55f), new Vector3(r * pupil * 0.55f, r * 0.82f, r * 0.28f), q, 4, 7);
            }
        }
    }
}
