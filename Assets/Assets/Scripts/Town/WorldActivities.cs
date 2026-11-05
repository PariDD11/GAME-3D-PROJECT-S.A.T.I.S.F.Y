using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>Menambang (bijih + permata di area hunt) dan bertani (lahan di Desa Pemula). Dipasang otomatis saat scene dimuat, tanpa rebuild.</summary>
    public static class Activities
    {
        public class Crop { public string id, seed, name; public float hours; public bool[] seasons; public int yMin, yMax, seedPrice, sell; public Color col; public string where; }
        static bool[] S(params int[] s) { var a = new bool[4]; foreach (var i in s) a[i] = true; return a; }
        public static readonly Crop[] Crops =
        {
            new Crop { id = "wheat", seed = "seed_wheat", name = "Gandum", hours = 36f, seasons = S(0, 1, 2), yMin = 3, yMax = 5, seedPrice = 4, sell = 5, col = new Color(0.9f, 0.78f, 0.3f) },
            new Crop { id = "carrot", seed = "seed_carrot", name = "Wortel", hours = 30f, seasons = S(0, 2), yMin = 2, yMax = 4, seedPrice = 5, sell = 7, col = new Color(0.95f, 0.5f, 0.15f) },
            new Crop { id = "tomato", seed = "seed_tomato", name = "Tomat", hours = 40f, seasons = S(1), yMin = 2, yMax = 4, seedPrice = 7, sell = 9, col = new Color(0.85f, 0.15f, 0.12f) },
            new Crop { id = "pumpkin", seed = "seed_pumpkin", name = "Labu", hours = 60f, seasons = S(2), yMin = 1, yMax = 2, seedPrice = 10, sell = 16, col = new Color(0.95f, 0.55f, 0.1f) },
            new Crop { id = "frost_leek", seed = "seed_leek", name = "Daun Bawang Salju", hours = 48f, seasons = S(3), yMin = 2, yMax = 3, seedPrice = 8, sell = 11, col = new Color(0.75f, 0.9f, 0.85f) },
            new Crop { id = "strawberry", seed = "seed_strawberry", name = "Stroberi", hours = 32f, seasons = S(0, 1), yMin = 3, yMax = 5, seedPrice = 6, sell = 8, col = new Color(0.9f, 0.15f, 0.25f) },
            new Crop { id = "corn", seed = "seed_corn", name = "Jagung", hours = 44f, seasons = S(1, 2), yMin = 2, yMax = 4, seedPrice = 7, sell = 10, col = new Color(0.95f, 0.85f, 0.25f) },
            new Crop { id = "melon", seed = "seed_melon", name = "Semangka", hours = 56f, seasons = S(1), yMin = 1, yMax = 2, seedPrice = 12, sell = 24, col = new Color(0.3f, 0.7f, 0.35f) },
        };
        public static Crop CropOfSeed(string seed) { foreach (var c in Crops) if (c.seed == seed) return c; return null; }

        public static void Register()
        {
            foreach (var c in Crops)
            {
                ItemDB.AddRaw(new ItemDef { id = c.id, name = c.name, type = ItemType.Material, sellPrice = c.sell, stack = 30 });
                ItemDB.AddRaw(new ItemDef { id = c.seed, name = "Benih " + c.name, type = ItemType.Material, buyPrice = c.seedPrice, sellPrice = 1, stack = 30 });
            }
            ItemDB.AddRaw(new ItemDef { id = "pick_iron", name = "Beliung Besi", type = ItemType.Material, buyPrice = 130, sellPrice = 45, stack = 1 });
            ItemDB.AddRaw(new ItemDef { id = "pick_steel", name = "Beliung Baja", type = ItemType.Material, buyPrice = 520, sellPrice = 190, stack = 1 });
            ItemDB.AddRaw(new ItemDef { id = "iron_ore", name = "Bijih Besi", type = ItemType.Material, sellPrice = 7, stack = 30 });
            ItemDB.AddRaw(new ItemDef { id = "gem_ruby", name = "Rubi", type = ItemType.Material, sellPrice = 70, stack = 20 });
            ItemDB.AddRaw(new ItemDef { id = "gem_sapphire", name = "Safir", type = ItemType.Material, sellPrice = 70, stack = 20 });
            ItemDB.AddRaw(new ItemDef { id = "gem_emerald", name = "Zamrud", type = ItemType.Material, sellPrice = 70, stack = 20 });
            ItemDB.AddRaw(new ItemDef { id = "gem_topaz", name = "Topas", type = ItemType.Material, sellPrice = 70, stack = 20 });
        }

        /// <summary>Beliung: yang terbaik di tas dipakai otomatis. Tanpa beliung = tangan kosong + palu seadanya.</summary>
        public class Pick { public string id, name; public int bonus, stam, price; public float gem; }
        public static readonly Pick[] Picks =
        {
            new Pick { id = "pick_steel", name = "Beliung Baja", bonus = 2, stam = 8, gem = 0.10f, price = 520 },
            new Pick { id = "pick_iron", name = "Beliung Besi", bonus = 1, stam = 11, gem = 0.05f, price = 130 },
        };
        static readonly Pick HandPick = new Pick { id = "", name = "Palu seadanya", bonus = 0, stam = 15, gem = 0f };
        public static Pick BestPick() { var g = GameState.I; if (g != null) foreach (var p in Picks) if (g.Count(p.id) > 0) return p; return HandPick; }

        public static string[] Gems = { "gem_ruby", "gem_sapphire", "gem_emerald", "gem_topaz" };
        public static float Hours { get { var g = GameState.I; return g == null ? 0f : g.day * 24f + g.timeOfDay; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Install(SceneManager.GetActiveScene()); }
        static void OnLoaded(Scene s, LoadSceneMode m) { Install(s); }
        static void Install(Scene s)
        {
            if ((s.name.StartsWith("HuntArea_") || s.name == "Town") && Object.FindFirstObjectByType<MineSpawner>() == null) new GameObject("MineSpawner").AddComponent<MineSpawner>();
            if (Object.FindFirstObjectByType<HomeFarmSpawner>() == null) new GameObject("HomeFarmSpawner").AddComponent<HomeFarmSpawner>();
            if (s.name == StarterConfig.VillageScene && Object.FindFirstObjectByType<FarmSpawner>() == null) new GameObject("FarmSpawner").AddComponent<FarmSpawner>();
        }
    }

    // =================================================== TAMBANG
    public class MineSpawner : MonoBehaviour
    {
        float born; bool done;
        void Start() { born = Time.time; }
        void Update()
        {
            if (done || Time.time - born < 3f) return;
            var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return;
            done = true;
            var rng = new System.Random(SceneManager.GetActiveScene().name.GetHashCode());
            int made = 0; bool town = SceneManager.GetActiveScene().name == "Town"; int want = town ? 6 : 10;
            for (int a = 0; a < 300 && made < want; a++)
            {
                double ang = rng.NextDouble() * 6.2832, rad = town ? 45 + rng.NextDouble() * 70 : 22 + rng.NextDouble() * 75;
                Vector3 q = p.transform.position + new Vector3((float)(System.Math.Cos(ang) * rad), 0f, (float)(System.Math.Sin(ang) * rad)), g;
                if (!FarmAnimals.Ground(q, out g)) continue;
                float sy; if (WaterZone.Query(new Vector3(g.x, g.y, g.z), out sy) && sy > g.y - 0.2f) continue;
                OreNode.Make(g, made % 4 == 3 || rng.NextDouble() < 0.15); made++;
            }
        }
    }

    public class OreNode : Interactable
    {
        public bool gemRich; GameObject vis;
        public static void Make(Vector3 pos, bool gemRich)
        {
            var go = new GameObject("OreNode"); go.transform.position = pos;
            var n = go.AddComponent<OreNode>(); n.gemRich = gemRich; n.radius = 2.6f; n.prompt = gemRich ? "Menambang (batu berkilau)" : "Menambang (bijih besi)";
            n.vis = new GameObject("vis"); n.vis.transform.SetParent(go.transform, false);
            Color rock = new Color(0.42f, 0.4f, 0.4f);
            RT.Prim(PrimitiveType.Cube, n.vis.transform, new Vector3(0, 0.45f, 0), new Vector3(1.4f, 0.9f, 1.2f), rock).transform.localRotation = Quaternion.Euler(0, 20, 6);
            RT.Prim(PrimitiveType.Cube, n.vis.transform, new Vector3(0.5f, 0.3f, 0.4f), new Vector3(0.8f, 0.6f, 0.7f), rock * 0.85f).transform.localRotation = Quaternion.Euler(10, -25, 0);
            RT.Prim(PrimitiveType.Cube, n.vis.transform, new Vector3(-0.4f, 0.25f, -0.5f), new Vector3(0.6f, 0.5f, 0.6f), rock * 1.1f).transform.localRotation = Quaternion.Euler(0, 50, 8);
            Color fleck = gemRich ? Activities.Gems.Length > 0 ? new Color(0.4f, 0.85f, 1f) : Color.white : new Color(0.75f, 0.45f, 0.25f);
            for (int i = 0; i < 4; i++)
            {
                var c = RT.Prim(PrimitiveType.Cube, n.vis.transform, new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.55f, 0.95f), Random.Range(-0.4f, 0.4f)), Vector3.one * Random.Range(0.12f, 0.22f), gemRich ? Color.HSVToRGB(Random.value, 0.6f, 1f) : fleck);
                c.transform.localRotation = Random.rotation;
            }
            var col = go.AddComponent<SphereCollider>(); col.radius = 0.75f; col.center = new Vector3(0, 0.6f, 0);
        }

        public override void Interact()
        {
            var gs = GameState.I; var pc = PlayerCombat.I;
            var pick = Activities.BestPick();
            if (pc == null || pc.stamina < pick.stam) { TownUI.I.Toast("Stamina kurang untuk menambang (butuh " + pick.stam + ")."); return; }
            if (!gs.HasSpace("iron_ore", 1)) { TownUI.I.Toast("Tas penuh."); return; }
            pc.SpendStamina(pick.stam);
            TownUI.I.Toast(pick.name + ". Mulai menambang: 3 pukulan, tekan tepat di zona.");
            Swing(0, 0, 0);
        }

        void Swing(int n, int hits, int perfects)
        {
            MiniGame.Timing("Menambang: pukulan " + (n + 1) + "/3", 1.0f + 0.25f * n, 0.24f, res =>
            {
                Audio.Sfx(res == 0 ? "hit_light" : "hit_heavy", transform.position);
                CombatFx.Burst(transform.position + Vector3.up * 0.8f, new Color(0.8f, 0.75f, 0.7f), 8, 0.5f, 2.5f, 0.5f, 0.08f);
                ThirdPersonCamera.Shake(res == 0 ? 0.02f : 0.07f, 0.1f);
                int h = hits + (res > 0 ? 1 : 0), p = perfects + (res == 2 ? 1 : 0);
                if (n < 2) Swing(n + 1, h, p); else Finish(h, p);
            });
        }

        void Finish(int hits, int perfects)
        {
            var gs = GameState.I;
            var pick = Activities.BestPick();
            int ore = Mathf.Max(1, hits + perfects) + pick.bonus;
            if (!gs.HasSpace("iron_ore", ore)) ore = 1;
            gs.AddItem("iron_ore", ore); gs.AddStat("mined", ore);
            for (int i = 0; i < ore; i++) HuntManager.Report("gather", "iron_ore");
            string msg = "Bijih Besi x" + ore;
            float chance = 0.05f + 0.07f * perfects + (gemRich ? 0.25f : 0f) + pick.gem;
            if (Random.value < chance)
            {
                string g = Activities.Gems[Random.Range(0, Activities.Gems.Length)];
                if (gs.HasSpace(g, 1)) { gs.AddItem(g, 1); msg += "  + " + ItemDB.Get(g).name + "!"; CombatFx.ShockRing(transform.position + Vector3.up * 0.5f, 2.5f, new Color(1f, 0.95f, 0.5f, 0.9f), 0.5f); Audio.Sfx("quest_reward", transform.position); }
            }
            TownUI.I.Toast(msg);
            if (vis != null) vis.SetActive(false);
            StartCoroutine(Regrow()); enabled = false;
        }

        IEnumerator Regrow() { yield return new WaitForSeconds(360f); if (vis != null) vis.SetActive(true); enabled = true; }
    }

    // =================================================== TANI
    public class FarmSpawner : MonoBehaviour
    {
        float born; bool done;
        void Start() { born = Time.time; }
        void Update()
        {
            if (done || Time.time - born < 3f) return;
            var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return;
            done = true;
            var anchorGo = GameObject.Find(StarterConfig.VillageSpawn);
            Vector3 anchor = anchorGo != null ? anchorGo.transform.position : p.transform.position;
            var rng = new System.Random(77);
            for (int a = 0; a < 260; a++)
            {
                double ang = rng.NextDouble() * 6.2832, rad = 10 + rng.NextDouble() * 38;
                Vector3 c = anchor + new Vector3((float)(System.Math.Cos(ang) * rad), 0f, (float)(System.Math.Sin(ang) * rad)), g;
                if (!FarmAnimals.Ground(c, out g)) continue;
                bool ok = true; var pts = new Vector3[6];
                for (int i = 0; i < 6 && ok; i++)
                {
                    Vector3 q = g + new Vector3((i % 3 - 1) * 2.6f, 0f, (i / 3 - 0.5f) * 2.6f), gg;
                    if (!FarmAnimals.Ground(q, out gg) || Mathf.Abs(gg.y - g.y) > 0.4f) ok = false; else pts[i] = gg;
                }
                if (!ok) continue;
                for (int i = 0; i < 6; i++) FarmPlot.Make(i, pts[i]);
                return;
            }
        }
    }

    /// <summary>Ladang kecil (3 petak) di depan rumah milik pemain, di scene mana pun rumah itu berada.</summary>
    public class HomeFarmSpawner : MonoBehaviour
    {
        readonly HashSet<string> done = new HashSet<string>(); float next = 3f;
        void Update()
        {
            if (Time.time < next || GameState.I == null) return; next = Time.time + 2.5f;
            foreach (var d in Object.FindObjectsByType<HomeDoor>(FindObjectsSortMode.None))
            {
                var h = PropertyMenus.Find(d.houseId); if (h == null || !PropertyMenus.Owns(h) || done.Contains(h.id)) continue;
                int hi = System.Array.IndexOf(PropertyMenus.Houses, h);
                for (int a = 0; a < 8; a++)
                {
                    float ang = a * 45f * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                    Vector3 c = d.transform.position + dir * 6f, g; if (!FarmAnimals.Ground(c, out g)) continue;
                    var side = new Vector3(-dir.z, 0, dir.x); bool ok = true; var pts = new Vector3[3];
                    for (int i = 0; i < 3 && ok; i++) { Vector3 q = g + side * ((i - 1) * 2.6f), gg; if (!FarmAnimals.Ground(q, out gg) || Mathf.Abs(gg.y - g.y) > 0.4f) ok = false; else pts[i] = gg; }
                    if (!ok) continue;
                    for (int i = 0; i < 3; i++) FarmPlot.Make(100 + hi * 3 + i, pts[i]);
                    done.Add(h.id); break;
                }
            }
        }
    }

    public class FarmPlot : Interactable
    {
        int idx; Transform[] crops = new Transform[6]; int shownStage = -9; int shownCrop = -9;
        static string K(int i, string w) { return "farm_" + w + "_" + i; }
        static int Get(string k) { int v; return GameState.I.stats.TryGetValue(k, out v) ? v : 0; }

        public static void Make(int i, Vector3 pos)
        {
            var go = new GameObject("FarmPlot" + i); go.transform.position = pos;
            var f = go.AddComponent<FarmPlot>(); f.idx = i; f.radius = 2.0f;
            RT.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, 0.07f, 0), new Vector3(2.2f, 0.16f, 2.2f), new Color(0.32f, 0.2f, 0.12f));
            for (int k = 0; k < 6; k++)
            {
                var c = RT.Prim(PrimitiveType.Cube, go.transform, new Vector3((k % 3 - 1) * 0.65f, 0.2f, (k / 3 - 0.5f) * 0.9f), Vector3.one * 0.1f, Color.green);
                c.SetActive(false); f.crops[k] = c.transform;
            }
        }

        Activities.Crop Planted() { int c = Get(K(idx, "c")); return c <= 0 ? null : Activities.Crops[c - 1]; }
        float Progress(Activities.Crop c) { return (Activities.Hours - Get(K(idx, "t")) + Get(K(idx, "w"))) / c.hours; }

        void Update()
        {
            if (GameState.I == null) return;
            var c = Planted();
            if (c == null) prompt = "Lahan kosong: tanam benih";
            else { float p = Progress(c); prompt = p >= 1f ? "Panen " + c.name : c.name + " tumbuh " + Mathf.Min(99, Mathf.FloorToInt(p * 100f)) + "%  (siram / lihat)"; }
            int stage = c == null ? -1 : Progress(c) >= 1f ? 2 : Progress(c) >= 0.34f ? 1 : 0, ci = c == null ? -1 : System.Array.IndexOf(Activities.Crops, c);
            if (stage != shownStage || ci != shownCrop)
            {
                shownStage = stage; shownCrop = ci;
                foreach (var t in crops)
                {
                    t.gameObject.SetActive(stage >= 0);
                    if (stage < 0) continue;
                    float h = stage == 0 ? 0.12f : stage == 1 ? 0.4f : 0.5f;
                    t.localScale = new Vector3(stage == 2 ? 0.3f : 0.14f, h, stage == 2 ? 0.3f : 0.14f);
                    t.localPosition = new Vector3(t.localPosition.x, 0.15f + h * 0.5f, t.localPosition.z);
                    t.GetComponent<Renderer>().sharedMaterial.color = stage == 2 ? c.col : new Color(0.3f, 0.7f, 0.25f);
                }
            }
        }

        public override void Interact()
        {
            var gs = GameState.I; var c = Planted();
            if (c == null) { OpenPlant(); return; }
            if (Progress(c) >= 1f) { Harvest(c); return; }
            int w = Get(K(idx, "n"));
            TownUI.I.Open("Lahan " + (idx + 1),
                () => c.name + " tumbuh " + Mathf.FloorToInt(Mathf.Clamp01(Progress(c)) * 100f) + "%\nDisiram " + Get(K(idx, "n")) + "/2 kali. Siram memakai 1 isi botol minum dan mempercepat tumbuh.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Siram (pakai 1 isi botol)  botol " + gs.canteen + "/" + GameState.CanteenMax, () => Water(), gs.canteen > 0 && Get(K(idx, "n")) < 2));
                    l.Add(new UIEntry("Cabut tanaman (hilang)", () => { Clear(); TownUI.I.Close(); }));
                    l.Add(new UIEntry("< Tutup", () => TownUI.I.Close()));
                    return l;
                });
        }

        void Water()
        {
            var gs = GameState.I; var c = Planted(); if (c == null || gs.canteen <= 0) return;
            gs.canteen--;
            gs.stats[K(idx, "n")] = Get(K(idx, "n")) + 1; gs.stats[K(idx, "w")] = Get(K(idx, "w")) + Mathf.RoundToInt(c.hours * 0.2f);
            CombatFx.Burst(transform.position + Vector3.up * 0.4f, new Color(0.6f, 0.8f, 1f), 10, 0.6f, 1.5f, 0.4f, 0.08f);
            Audio.Sfx("gather", transform.position); TownUI.I.Toast("Disiram. Tumbuh lebih cepat.");
        }

        void Clear() { var s = GameState.I.stats; s.Remove(K(idx, "c")); s.Remove(K(idx, "t")); s.Remove(K(idx, "w")); s.Remove(K(idx, "n")); }

        void OpenPlant()
        {
            var gs = GameState.I; var se = GameCalendar.Current;
            TownUI.I.Open("Tanam",
                () => GameCalendar.Name(se) + ". Pilih benih. Tiap tanaman hanya bisa ditanam di musimnya (beli benih di Toko Umum).",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var cc in Activities.Crops)
                    {
                        var crop = cc; bool ok = crop.seasons[(int)se] && gs.Count(crop.seed) > 0;
                        string ms = ""; for (int s = 0; s < 4; s++) if (crop.seasons[s]) ms += GameCalendar.SeasonNames[s].Substring(6) + " ";
                        l.Add(new UIEntry(crop.name + "   benih " + gs.Count(crop.seed) + "   [" + ms.Trim() + "]   ~" + Mathf.RoundToInt(crop.hours) + " jam", () =>
                        {
                            if (!gs.RemoveItem(crop.seed, 1)) return;
                            int ci = System.Array.IndexOf(Activities.Crops, crop) + 1;
                            gs.stats[K(idx, "c")] = ci; gs.stats[K(idx, "t")] = Mathf.RoundToInt(Activities.Hours); gs.stats[K(idx, "w")] = 0; gs.stats[K(idx, "n")] = 0;
                            Audio.Sfx("gather", transform.position); TownUI.I.Close(); TownUI.I.Toast("Menanam " + crop.name + ".");
                        }, ok));
                    }
                    l.Add(new UIEntry("< Tutup", () => TownUI.I.Close()));
                    return l;
                });
        }

        void Harvest(Activities.Crop c)
        {
            var gs = GameState.I;
            MiniGame.Timing("Panen " + c.name, 1.0f, 0.3f, res =>
            {
                int n = Random.Range(c.yMin, c.yMax + 1) + (res == 2 ? 1 : res == 0 ? -1 : 0); n = Mathf.Max(1, n);
                if (!gs.HasSpace(c.id, n)) { TownUI.I.Toast("Tas penuh."); return; }
                gs.AddItem(c.id, n); gs.AddStat("harvested", n);
                for (int i = 0; i < n; i++) HuntManager.Report("gather", c.id);
                Clear();
                Audio.Sfx("gather", transform.position);
                CombatFx.Burst(transform.position + Vector3.up * 0.5f, c.col, 12, 0.7f, 2f, 0.5f, 0.1f);
                TownUI.I.Toast(c.name + " x" + n + (res == 2 ? "  (panen sempurna!)" : ""));
            });
        }
    }
}
