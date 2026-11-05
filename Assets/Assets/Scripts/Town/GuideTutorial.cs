using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Tutorial pemandu sampai Level 5. Avatar cowok dipandu cewek (Lynx), avatar cewek dipandu cowok (Kael).
    /// Alur tiap langkah: dialog pemandu -> kartu tutorial (halaman) -> misi di Quest Tracking -> pujian -> langkah berikut.
    /// Progres tersimpan di GameState.flags / stats ("gt_*"). Tekan Y kapan saja untuk mendengar penjelasan langkah aktif lagi.
    /// Potret: Resources/Portraits/Lynx.png dan Kael.png (bila belum ada, dipakai ilustrasi sementara buatan kode).
    /// </summary>
    public class GuideTutorial : MonoBehaviour
    {
        class Page { public string text; public string[] keys; }
        class Step
        {
            public string title, intro, praise, objective, stat, stq; public int count; public Page[] pages;
        }

        static readonly Step[] Steps =
        {
            new Step { title = "Pergerakan Dasar", stat = "gt_walk", count = 25, objective = "Gerakkan karakter sejauh 25 meter",
                intro = "Halo! Aku {G}, sistem pemandu yang akan membantumu selama petualangan di Ashwatch.\nPertama-tama, mari kita pelajari <color=#FFD54A>cara bergerak</color>.",
                praise = "Bagus! Kamu sudah bisa bergerak dengan lancar.",
                pages = new[] {
                    new Page { text = "Gunakan tombol <color=#FFD54A>W A S D</color> untuk menggerakkan karakter ke depan, belakang, kiri, dan kanan.", keys = new[] { "W", "A", "S", "D" } },
                    new Page { text = "Tahan <color=#FFD54A>Shift</color> untuk berlari, tekan <color=#FFD54A>Ctrl</color> untuk berguling menghindar dan <color=#FFD54A>Spasi</color> untuk melompat. Mouse mengatur arah kamera.", keys = new[] { "Shift", "Ctrl", "Spasi" } } } },
            new Step { title = "Cari Veteran Brann", stat = "met", count = 1, objective = "Temui Veteran Brann di desa (tekan E)",
                intro = "Di desa ini ada seorang veteran bernama <color=#FFD54A>Brann</color>. Dia akan melatihmu sampai siap berburu.\nIkuti <color=#FFD54A>penanda biru</color> di kompas atas dan minimap untuk menemukannya.",
                praise = "Itu dia Veteran Brann. Dengarkan nasihatnya; dia akan memberimu latihan, satu per satu.",
                pages = new[] {
                    new Page { text = "Dekati orang lalu tekan <color=#FFD54A>E</color> saat tulisan [E] muncul. Penanda tujuan tampil di <color=#FFD54A>kompas</color> atas dan <color=#FFD54A>minimap</color>.", keys = new[] { "E", "M" } },
                    new Page { text = "Tekan <color=#FFD54A>M</color> untuk memperbesar peta. Huruf N/E/S/W di cincin peta menunjukkan arah mata angin.", keys = new[] { "M" } } } },
            new Step { title = "Latihan Dasar", stq = "s1", objective = "",
                intro = "Brann akan mengajarimu <color=#FFD54A>bertarung</color>. Bicaralah lagi dengannya untuk menerima latihan, lalu pukul Boneka Latihan di komplek pelatihan.",
                praise = "Pukulan yang mantap! Kamu siap menghadapi monster sungguhan.",
                pages = new[] {
                    new Page { text = "<color=#FFD54A>Klik kiri</color> atau <color=#FFD54A>J</color> untuk menyerang. Tiga serangan beruntun membentuk kombo.", keys = new[] { "Klik Kiri", "J" } },
                    new Page { text = "<color=#FFD54A>Klik kanan</color> menahan serangan, <color=#FFD54A>Ctrl</color> berguling. Awasi bar HP dan Stamina/MP di kiri-atas.", keys = new[] { "Klik Kanan", "Ctrl" } } } },
            new Step { title = "Tas & Skill", stat = "open_bag", count = 1, objective = "Buka tas dengan tombol Tab",
                intro = "Sekarang kenali <color=#FFD54A>tas dan skill</color>-mu.",
                praise = "Bagus. Ramuan dan makanan di tas bisa menyelamatkanmu saat HP tipis.",
                pages = new[] {
                    new Page { text = "<color=#FFD54A>Tab</color> membuka tas. Pasang item ke slot cepat <color=#FFD54A>1-6</color>, minum dengan <color=#FFD54A>F</color>.", keys = new[] { "Tab", "1-6", "F" } },
                    new Page { text = "<color=#FFD54A>K</color> membuka skill; skill dipakai dengan <color=#FFD54A>Z X C V R T Q</color>. Tombol <color=#FFD54A>H</color> membuka Profil.", keys = new[] { "K", "Z X C V R T", "H" } } } },
            new Step { title = "Hama Slime", stq = "s2", objective = "",
                intro = "Latihan berikutnya: <color=#FFD54A>berburu Slime</color>. Ambil misinya dari Brann, lalu susuri jalan utara menuju Hutan Pemula. Penandanya akan menuntunmu.",
                praise = "Slime sudah beres. Kamu mulai terbiasa!",
                pages = new[] {
                    new Page { text = "Keluar desa lewat <color=#FFD54A>jalan utara</color>. Di Hutan Pemula, kalahkan Slime yang berkeliaran.", keys = new string[0] },
                    new Page { text = "Awasi HP-mu. Minum ramuan atau makan roti dari tas bila perlu, dan gunakan <color=#FFD54A>Ctrl</color> untuk menghindar.", keys = new[] { "Ctrl" } } } },
            new Step { title = "Mengumpulkan Herb", stq = "s3", objective = "",
                intro = "Sekarang <color=#FFD54A>mengumpulkan bahan</color>. Herb tumbuh sebagai semak hijau bercahaya di hutan.",
                praise = "Herb terkumpul. Bahan seperti ini dijual dan dipakai untuk crafting.",
                pages = new[] {
                    new Page { text = "Dekati semak hijau bercahaya lalu tekan <color=#FFD54A>E</color> untuk memetik Herb.", keys = new[] { "E" } } } },
            new Step { title = "Goblin Pengganggu", stq = "s4", objective = "",
                intro = "Goblin mencuri hasil ladang desa. Mereka <color=#FFD54A>menyerang lebih keras</color> dari Slime, jadi berhati-hatilah.",
                praise = "Goblin-goblin itu sudah kapok. Hebat!",
                pages = new[] {
                    new Page { text = "Goblin punya serangan yang berkedip merah sebelum menghantam. Saat itu terjadi, <color=#FFD54A>berguling</color> dengan Ctrl.", keys = new[] { "Ctrl" } } } },
            new Step { title = "Menuju Level 5", stq = "s5", objective = "",
                intro = "Ujian terakhir: capai <color=#FFD54A>Level 5</color> dengan berburu di Hutan Pemula, lalu lapor ke Brann.",
                praise = "Kamu lulus! Temui Veteran Brann sekali lagi untuk memilih job permanenmu. Sampai jumpa di petualangan berikutnya!",
                pages = new[] {
                    new Page { text = "Berburu di <color=#FFD54A>Hutan Pemula</color> memberi EXP. Di Level 5 kamu memilih job dan boleh menempuh jalan ke Ashwatch.", keys = new string[0] },
                    new Page { text = "Tekan <color=#FFD54A>Y</color> kapan saja bila ingin kuulangi penjelasan langkah ini.", keys = new[] { "Y" } } } },
        };

        static StarterQuests.Stage Stage(Step st) { foreach (var q in StarterQuests.All) if (q.id == st.stq) return q; return null; }

        static string Obj(int s)
        {
            var st = Steps[s]; var q = Stage(st);
            if (q == null) return st.objective;
            if (!StarterQuests.Accepted(q)) return "Temui Veteran Brann untuk menerima latihan";
            if (StarterQuests.Ready(q)) return "Lapor ke Veteran Brann";
            return q.title + " " + StarterQuests.Progress(q) + "/" + q.count;
        }

        static bool StepDone(int s)
        {
            var gs = GameState.I; var st = Steps[s]; var q = Stage(st);
            if (q != null) return gs.flags.Contains("stq_done_" + q.id);
            if (st.stat == "met") return gs.flags.Contains("stq_met");
            return false;
        }

        // ------------------------------------------------------------------ penanda tujuan
        VillageElder elder; float wpAt;
        void UpdateWaypoint()
        {
            if (Time.unscaledTime < wpAt) return; wpAt = Time.unscaledTime + 0.4f;
            Minimap.WpOn = false;
            if (!Running || phase != Phase.Objective || player == null) return;
            var st = Steps[step]; var q = Stage(st); string kind = null;
            if (st.stat == "met") kind = "brann";
            else if (q != null)
            {
                if (!StarterQuests.Accepted(q) || StarterQuests.Ready(q)) kind = "brann";
                else kind = q.stat == "dummy_hits" ? "dummy" : q.stat == "kill_slime" ? "slime" : q.stat == "gather_herb" ? "herb" : q.stat == "kill_goblin" ? "goblin" : "monster";
            }
            if (kind == null) return;
            Vector3 pos; string label;
            if (Find(kind, out pos, out label)) { Minimap.WpOn = true; Minimap.WpPos = pos; Minimap.WpLabel = label; }
        }

        bool Find(string kind, out Vector3 pos, out string label)
        {
            pos = Vector3.zero; label = "";
            if (kind == "brann")
            {
                if (elder == null) elder = Object.FindFirstObjectByType<VillageElder>();
                if (elder != null) { pos = elder.transform.position; label = "Veteran Brann"; return true; }
                return ExitTo(StarterConfig.VillageScene, out pos, out label);
            }
            string want = kind == "dummy" ? "dummy" : kind == "slime" ? "slime" : kind == "goblin" ? "goblin" : null;
            float best = 1e9f; bool found = false;
            if (kind == "herb")
            {
                foreach (var n in GatherNode.Nodes) { if (n == null || n.itemId != "herb") continue; float d = (n.transform.position - player.position).sqrMagnitude; if (d < best) { best = d; pos = n.transform.position; found = true; } }
                label = "Herb"; if (found) return true;
            }
            else
            {
                foreach (var m in Monster.All)
                {
                    if (m == null || m.IsDead || m.isBoss) continue;
                    if (want != null ? m.defId != want : m.defId == "dummy") continue;
                    float d = (m.transform.position - player.position).sqrMagnitude; if (d < best) { best = d; pos = m.transform.position; found = true; }
                }
                label = kind == "dummy" ? "Boneka Latihan" : kind == "slime" ? "Slime" : kind == "goblin" ? "Goblin" : "Monster"; if (found) return true;
            }
            // tidak ada di scene ini: arahkan ke jalan menuju Hutan Pemula
            return ExitTo(StarterConfig.TutorialScene, out pos, out label);
        }

        static bool ExitTo(string dest, out Vector3 pos, out string label)
        {
            pos = Vector3.zero; label = "";
            foreach (var e in Object.FindObjectsByType<VillageExit>(FindObjectsSortMode.None))
                if (e != null && e.destScene == dest) { pos = e.transform.position; label = "Jalan ke Hutan Pemula"; return true; }
            return false;
        }

        // ------------------------------------------------------------------ status publik
        static GuideTutorial inst;
        /// <summary>Baris untuk Quest Tracking di HUD (null bila tutorial tidak aktif).</summary>
        public static bool TrackerLine(out string title, out string obj, out bool done)
        {
            title = obj = ""; done = false;
            if (inst == null || !inst.Running) return false;
            int s = inst.step; if (s < 0 || s >= Steps.Length) return false;
            var st = Steps[s];
            title = "Quest Tutorial: " + st.title;
            var q = Stage(st);
            done = StepDone(s) || (q != null ? StarterQuests.Ready(q) : inst.Progress(s) >= st.count);
            obj = q != null || st.stat == "met" ? Obj(s) : st.objective + "  " + inst.Progress(s) + "/" + st.count;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("GuideTutorial"); DontDestroyOnLoad(go); inst = go.AddComponent<GuideTutorial>();
        }

        // ------------------------------------------------------------------ state
        int step = -1; float sceneT, nextCheck; Transform player; Vector3 lastPos; float walk;
        enum Phase { Idle, Dialogue, Card, Objective }
        Phase phase = Phase.Idle; bool praiseMode;
        int page; bool builtUi;
        GameObject dlgRoot, cardRoot; Text nameTx, bodyTx, cardTitle, cardCount, cardText, cardNextLabel; Image portraitImg;
        RectTransform keysRoot, choiceRoot; Button prevBtn;
        string fullText = ""; float typed; bool sceneOk;

        bool Running { get { return GameState.I != null && !GameState.I.flags.Contains("gt_done") && step >= 0 && step < Steps.Length && CharacterData.Exists; } }
        bool Female { get { return CharacterData.Current.female; } }
        string GuideId { get { return Female ? "Kael" : "Lynx"; } }
        string GuideName { get { var c = GuideConfig.Load(); var g = c != null ? c.Find(GuideId) : null; return g != null && !string.IsNullOrEmpty(g.displayName) ? g.displayName : GuideId; } }   // avatar cewek -> pemandu cowok, avatar cowok -> pemandu cewek

        int Progress(int s)
        {
            var gs = GameState.I; var st = Steps[s];
            if (st.stat == "met") return gs.flags.Contains("stq_met") ? 1 : 0;
            if (st.stq != null) return StepDone(s) ? 1 : 0;
            if (st.stat == "level") return Mathf.Min(st.count, gs.level);
            if (st.stat == "gt_walk") return Mathf.Min(st.count, gs.Stat("gt_walk") - gs.Stat("gtb_" + s));
            return Mathf.Clamp(gs.Stat(st.stat) - gs.Stat("gtb_" + s), 0, st.count);
        }

        void Start() { SceneManager.sceneLoaded += (a, b) => { sceneT = Time.unscaledTime; phase = phase == Phase.Objective ? phase : Phase.Idle; if (builtUi) { dlgRoot = null; cardRoot = null; builtUi = false; if (phase != Phase.Objective) phase = Phase.Idle; } }; sceneT = Time.unscaledTime; }

        void Update()
        {
            var gs = GameState.I; var ui = TownUI.I;
            if (gs == null || ui == null || ui.Root == null || !CharacterData.Exists) return;
            if (gs.flags.Contains("gt_done")) { Minimap.WpOn = false; return; }
            if (!MainMenu.TitleDone) return;     // v35z102n18: tunggu menu utama / Lanjutkan selesai memuat save sebelum menentukan langkah

            // Level 5 ke atas (atau job sudah dipilih): tutorial dasar sudah tidak relevan, tandai selesai dan sembunyikan
            if (gs.level >= 5 || gs.jobChosen)
            {
                gs.flags.Add("gt_done"); Minimap.WpOn = false;
                if (phase != Phase.Idle || builtUi) { phase = Phase.Idle; HideAll(); }
                step = -1;
                return;
            }

            // tentukan langkah aktif dari save
            if (step < 0)
            {
                int s = 0;
                while (s < Steps.Length && (gs.flags.Contains("gt_s" + s) || StepDone(s) || (s == 0 && gs.flags.Contains("stq_met")))) { gs.flags.Add("gt_s" + s); s++; }
                if (s >= Steps.Length) { gs.flags.Add("gt_done"); return; }
                step = s;
            }

            string scn = SceneManager.GetActiveScene().name;
            sceneOk = scn == StarterConfig.VillageScene || scn == StarterConfig.TutorialScene;

            // lacak jarak jalan
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) { player = p.transform; lastPos = player.position; } }
            else
            {
                Vector3 d = player.position - lastPos; d.y = 0f; lastPos = player.position;
                if (d.magnitude < 3f && !ui.PanelOpen) { walk += d.magnitude; if (walk >= 1f) { int m = Mathf.FloorToInt(walk); gs.AddStat("gt_walk", m); walk -= m; } }
            }

            if (phase == Phase.Idle)
            {
                if (!sceneOk || ui.PanelOpen || !MainMenu.TitleDone || Time.unscaledTime - sceneT < 2.5f) return;
                if (CharacterCreatorUI.I != null && CharacterCreatorUI.I.IsOpen) return;
                if (PlayerController.InFight()) return;
                BeginStep(false);
                return;
            }

            UpdateWaypoint();
            if (phase == Phase.Dialogue) { TickDialogue(); return; }
            if (phase == Phase.Card) { TickCard(); return; }

            // Objective
            if (sceneOk && !ui.PanelOpen && TownInput.GuideDown() && !PlayerController.InFight()) { page = 0; ShowCard(); return; }
            if (Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + 0.25f;
                if ((Steps[step].stq != null ? StepDone(step) : Progress(step) >= Steps[step].count) && sceneOk && !ui.PanelOpen && !PlayerController.InFight()) FinishStep();
            }
        }

        // ------------------------------------------------------------------ alur
        void BeginStep(bool replay)
        {
            var gs = GameState.I;
            if (!replay && Steps[step].stq == null && Steps[step].stat != "met" && !gs.flags.Contains("gtb_set_" + step)) { gs.flags.Add("gtb_set_" + step); gs.stats["gtb_" + step] = Steps[step].stat == "level" ? 0 : gs.Stat(Steps[step].stat); }
            praiseMode = false;
            ShowDialogue(Steps[step].intro.Replace("{G}", GuideName), new[] { "Baik, aku mengerti!", "Bisa jelaskan lagi?" });
        }

        void FinishStep()
        {
            var gs = GameState.I; var st = Steps[step];
            gs.flags.Add("gt_s" + step);
            Audio.Sfx("quest");
            praiseMode = true;
            bool last = step >= Steps.Length - 1;
            ShowDialogue(st.praise, new[] { last ? "Terima kasih!" : "Lanjut" });
        }

        void AfterPraise()
        {
            var gs = GameState.I;
            HideAll();
            int s = step + 1;
            if (s >= Steps.Length) { gs.flags.Add("gt_done"); phase = Phase.Idle; return; }
            step = s; phase = Phase.Idle; sceneT = Time.unscaledTime - 1.5f;
        }

        void HideAll()
        {
            if (dlgRoot != null) dlgRoot.SetActive(false);
            if (cardRoot != null) cardRoot.SetActive(false);
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        // ------------------------------------------------------------------ dialog
        void ShowDialogue(string text, string[] choices)
        {
            Build();
            phase = Phase.Dialogue; confirmGuard = Time.frameCount + 20;
            TownUI.I.SetExternalOpen(true);
            cardRoot.SetActive(false);
            dlgRoot.SetActive(true);
            nameTx.text = GuideName; fullText = text; typed = 0f; bodyTx.text = "";
            portraitImg.sprite = Portrait();
            var gcfg = GuideConfig.Load(); var gg = gcfg != null ? gcfg.Find(GuideId) : null;
            float gsc = gg != null && gg.scale > 0.01f ? gg.scale : 1f;
            var prt = portraitImg.rectTransform; prt.sizeDelta = new Vector2(760f * gsc, 980f * gsc); prt.anchoredPosition = new Vector2(10f, 130f) + (gg != null ? gg.offset : Vector2.zero);
            UIKit.Clear(choiceRoot);
            for (int i = 0; i < choices.Length; i++)
            {
                int id = i; bool primary = i == 0;
                var b = Btn(choiceRoot, choices[i], 0, i * 84f, 520, 70, primary, () => OnChoice(id));
            }
            Audio.Sfx("ui_open");
        }

        void OnChoice(int id)
        {
            if (praiseMode) { AfterPraise(); return; }
            if (id == 0) { page = 0; ShowCard(); }
            else { fullText = fullText + "\n\nBaik, kujelaskan sekali lagi dengan lebih rinci."; typed = 0f; bodyTx.text = ""; }
        }

        void TickDialogue()
        {
            if (bodyTx == null) { phase = Phase.Idle; return; }
            if (typed < fullText.Length) { typed = Mathf.Min(fullText.Length, typed + 70f * Time.unscaledDeltaTime); bodyTx.text = Visible(fullText, (int)typed); if (TownInput.AttackDown()) typed = fullText.Length; }
            else if (TownInput.ConfirmDown() && Time.frameCount > confirmGuard) OnChoice(0);
        }
        int confirmGuard;

        /// <summary>Potong teks sampai n karakter terlihat tanpa memotong tag warna.</summary>
        static string Visible(string s, int n)
        {
            var sb = new System.Text.StringBuilder(); int shown = 0; bool tag = false; bool opened = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '<') { tag = true; if (i + 1 < s.Length && s[i + 1] != '/') opened = true; else opened = false; }
                if (tag) { sb.Append(c); if (c == '>') tag = false; continue; }
                if (shown >= n) { if (opened) sb.Append("</color>"); break; }
                sb.Append(c); shown++;
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ kartu tutorial
        void ShowCard()
        {
            Build();
            phase = Phase.Card; confirmGuard = Time.frameCount + 2;
            TownUI.I.SetExternalOpen(true);
            dlgRoot.SetActive(true); cardRoot.SetActive(true);
            // saat kartu terbuka, dialog menyusut ke nama + ucapan singkat
            nameTx.text = GuideName; fullText = "Perhatikan baik-baik ya."; typed = fullText.Length; bodyTx.text = fullText;
            UIKit.Clear(choiceRoot);
            RefreshCard();
        }

        void RefreshCard()
        {
            var st = Steps[step]; var pg = st.pages[page];
            cardTitle.text = "Tutorial: " + st.title; cardCount.text = (page + 1) + " / " + st.pages.Length;
            cardText.text = pg.text;
            cardNextLabel.text = page >= st.pages.Length - 1 ? "Mengerti" : "Selanjutnya";
            prevBtn.interactable = page > 0;
            UIKit.Clear(keysRoot);
            float x = 0f, y = 0f;
            for (int i = 0; i < pg.keys.Length; i++)
            {
                string k = pg.keys[i]; float w = Mathf.Max(64f, 22f * k.Length + 30f);
                if (st.stat == "gt_walk" && page == 0)
                {   // susunan W / ASD
                    float[] px = { 76f, 0f, 76f, 152f }, py = { 0f, 76f, 76f, 76f };
                    Key(keysRoot, k, px[i] + 20f, py[i] + 10f, 68f); continue;
                }
                if (x + w > 300f) { x = 0f; y += 84f; }
                Key(keysRoot, k, x, y, w); x += w + 14f;
            }
        }

        void TickCard()
        {
            if (TownInput.ConfirmDown() && Time.frameCount > confirmGuard) NextPage();
        }

        void NextPage()
        {
            var st = Steps[step];
            if (page < st.pages.Length - 1) { page++; RefreshCard(); return; }
            // selesai membaca: mulai misi
            HideAll(); phase = Phase.Objective; nextCheck = Time.unscaledTime + 0.5f;
            if (TownUI.I != null) TownUI.I.Toast("Quest Tutorial: " + Obj(step));
        }

        // ------------------------------------------------------------------ pembuatan UI
        static readonly Color Navy = new Color(0.035f, 0.06f, 0.12f, 0.94f), Gold = new Color(0.86f, 0.70f, 0.36f), Cream = new Color(0.93f, 0.93f, 0.90f), Sky = new Color(0.45f, 0.78f, 1f);

        RectTransform Pnl(Transform parent, string name, float x, float y, float w, float h, Color fill, Color trim)
        {
            var rt = UIKit.Rect(name, parent); UIKit.At(rt, x, y, w, h);
            var f = rt.gameObject.AddComponent<Image>(); f.sprite = HudArt.PanelFill(96, 12, 32); f.type = Image.Type.Sliced; f.color = fill;
            var t = UIKit.Img("Trim", rt, trim); t.sprite = HudArt.PanelTrim(96, 12, 32, true); t.type = Image.Type.Sliced; t.raycastTarget = false;
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            return rt;
        }

        Text Lbl(Transform p, string txt, int size, TextAnchor an, Color c, float x, float y, float w, float h, bool bold = false)
        {
            var t = UIKit.Txt("T", p, size, an, c); UIKit.At(t.rectTransform, x, y, w, h); t.text = txt; if (bold) t.fontStyle = FontStyle.Bold; t.supportRichText = true; return t;
        }

        Button Btn(Transform parent, string label, float x, float y, float w, float h, bool primary, System.Action act, Text[] outLabel = null)
        {
            var rt = Pnl(parent, "Btn", x, y, w, h, primary ? new Color(0.06f, 0.13f, 0.26f, 0.96f) : new Color(0.07f, 0.09f, 0.14f, 0.92f), primary ? Sky : new Color(0.5f, 0.55f, 0.65f));
            var img = rt.GetComponent<Image>();
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            var cb = b.colors; cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f); cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f); b.colors = cb;
            var t = Lbl(rt, label, 26, TextAnchor.MiddleCenter, primary ? Color.white : new Color(0.85f, 0.88f, 0.95f), 0, 0, w, h);
            if (outLabel != null) outLabel[0] = t;
            b.onClick.AddListener(() => { Audio.Sfx("click"); if (act != null) act(); });
            return b;
        }

        void Key(Transform parent, string k, float x, float y, float w)
        {
            var rt = Pnl(parent, "Key", x, y, w, 64f, new Color(0.10f, 0.12f, 0.18f, 1f), new Color(0.95f, 0.95f, 0.95f));
            Lbl(rt, k, k.Length > 3 ? 22 : 30, TextAnchor.MiddleCenter, Color.white, 0, 0, w, 64f, true);
        }

        void Build()
        {
            if (builtUi && dlgRoot != null) return;
            var root = TownUI.I.Root;
            // ---- dialog pemandu (kiri-bawah)
            var dlg = UIKit.Rect("GuideDialogue", root); dlg.anchorMin = Vector2.zero; dlg.anchorMax = Vector2.one; dlg.offsetMin = dlg.offsetMax = Vector2.zero; dlgRoot = dlg.gameObject;
            var pimg = UIKit.Img("Portrait", dlg, Color.white); pimg.raycastTarget = false; pimg.preserveAspect = true; portraitImg = pimg;
            var pr = pimg.rectTransform; pr.anchorMin = pr.anchorMax = new Vector2(0f, 0f); pr.pivot = new Vector2(0f, 0f); pr.anchoredPosition = new Vector2(10f, 130f); pr.sizeDelta = new Vector2(760f, 980f);
            var box = Pnl(dlg, "Box", 0, 0, 1120, 300, Navy, Gold);
            box.anchorMin = box.anchorMax = new Vector2(0f, 0f); box.pivot = new Vector2(0f, 0f); box.anchoredPosition = new Vector2(310f, 150f); box.sizeDelta = new Vector2(1120f, 300f);
            var plate = Pnl(box, "Plate", 0, 0, 300, 58, new Color(0.08f, 0.17f, 0.34f, 0.98f), Gold);
            plate.anchorMin = plate.anchorMax = new Vector2(0f, 1f); plate.pivot = new Vector2(0f, 0.5f); plate.anchoredPosition = new Vector2(34f, 6f);
            nameTx = Lbl(plate, "", 34, TextAnchor.MiddleCenter, Color.white, 0, 0, 300, 58, true);
            bodyTx = Lbl(box, "", 36, TextAnchor.UpperLeft, Cream, 50, 62, 1020, 220);
            bodyTx.horizontalOverflow = HorizontalWrapMode.Wrap; bodyTx.lineSpacing = 1.15f;
            choiceRoot = UIKit.Rect("Choices", dlg); choiceRoot.anchorMin = choiceRoot.anchorMax = new Vector2(0f, 0f); choiceRoot.pivot = new Vector2(0f, 0f);
            choiceRoot.anchoredPosition = new Vector2(1440f, 200f); choiceRoot.sizeDelta = new Vector2(520f, 170f);

            // ---- kartu tutorial (tengah-atas)
            var card = Pnl(root, "GuideCard", 0, 0, 1000, 560, Navy, Gold);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f); card.pivot = new Vector2(0.5f, 1f); card.anchoredPosition = new Vector2(260f, -80f); card.sizeDelta = new Vector2(1000f, 560f); cardRoot = card.gameObject;
            cardTitle = Lbl(card, "", 34, TextAnchor.MiddleLeft, Color.white, 70, 12, 700, 56, true);
            cardCount = Lbl(card, "", 28, TextAnchor.MiddleRight, new Color(0.8f, 0.85f, 0.95f), 780, 12, 190, 56);
            var line = UIKit.Img("Line", card, new Color(Gold.r, Gold.g, Gold.b, 0.5f)); UIKit.At(line.rectTransform, 24, 72, 952, 2); line.raycastTarget = false;
            var pic = Pnl(card, "Pic", 30, 90, 940, 330, new Color(0.02f, 0.03f, 0.06f, 0.9f), new Color(0.35f, 0.4f, 0.5f));
            keysRoot = UIKit.Rect("Keys", pic); UIKit.At(keysRoot, 44, 46, 340, 250);
            var tbox = Pnl(pic, "Text", 0, 0, 520, 290, new Color(0.04f, 0.06f, 0.1f, 0.96f), new Color(0.3f, 0.35f, 0.45f));
            tbox.anchorMin = tbox.anchorMax = new Vector2(1f, 0.5f); tbox.pivot = new Vector2(1f, 0.5f); tbox.anchoredPosition = new Vector2(-24f, 0f); tbox.sizeDelta = new Vector2(520f, 290f);
            cardText = Lbl(tbox, "", 34, TextAnchor.MiddleLeft, Cream, 24, 10, 472, 270);
            cardText.horizontalOverflow = HorizontalWrapMode.Wrap; cardText.lineSpacing = 1.1f;
            prevBtn = Btn(card, "< Sebelumnya", 400, 450, 280, 84, false, () => { if (page > 0) { page--; RefreshCard(); } });
            var lab = new Text[1];
            Btn(card, "Selanjutnya >", 700, 450, 270, 84, true, NextPage, lab); cardNextLabel = lab[0];
            Lbl(card, "Y = ulangi penjelasan   |   Enter / Spasi = lanjut", 22, TextAnchor.MiddleLeft, new Color(0.7f, 0.75f, 0.85f), 36, 462, 360, 60);

            dlgRoot.SetActive(false); cardRoot.SetActive(false);
            builtUi = true;
        }

        // ------------------------------------------------------------------ potret
        static Sprite lynxFb, kaelFb;
        Sprite Portrait()
        {
            var cfg = GuideConfig.Load(); var gd = cfg != null ? cfg.Find(GuideId) : null;
            if (gd != null && gd.portrait != null) return gd.portrait;
            var sp = DialogueUI.LoadPortrait(GuideId);
            if (sp != null) return sp;
            if (GuideId == "Lynx") return lynxFb != null ? lynxFb : lynxFb = MakeFallback(true);
            return kaelFb != null ? kaelFb : kaelFb = MakeFallback(false);
        }

        /// <summary>Ilustrasi sementara (siluet berwarna) sampai file potret asli ditaruh di Resources/Portraits.</summary>
        static Sprite MakeFallback(bool lynx)
        {
            int W = 320, H = 480; var tex = new Texture2D(W, H, TextureFormat.RGBA32, false); tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[W * H];
            Color hairA = lynx ? new Color(0.86f, 0.89f, 0.99f) : new Color(0.13f, 0.11f, 0.16f), hairB = lynx ? new Color(0.45f, 0.70f, 0.98f) : new Color(0.42f, 0.14f, 0.16f);
            Color cloth = lynx ? new Color(0.10f, 0.16f, 0.36f) : new Color(0.14f, 0.10f, 0.12f), trim = lynx ? new Color(0.9f, 0.75f, 0.4f) : new Color(0.85f, 0.35f, 0.3f);
            Color skin = new Color(0.98f, 0.86f, 0.78f), eye = lynx ? new Color(0.25f, 0.55f, 0.95f) : new Color(0.95f, 0.65f, 0.2f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W - 0.5f, v = y / (float)H; Color c = Color.clear;
                    float hx = u, hy = v - 0.66f;
                    if (Ell(hx, hy, 0.46f * (lynx ? 1f : 0.82f), 0.34f, 0f, 0.02f)) c = Color.Lerp(hairB, hairA, Mathf.Clamp01((v - 0.35f) * 2.6f));            // rambut belakang
                    if (Ell(u, v - 0.12f, 0.50f, 0.26f, 0f, 0f) && v < 0.30f) c = cloth;                                                                        // bahu
                    if (v < 0.30f && Mathf.Abs(u) < 0.05f * 2f && v > 0.18f) c = Color.Lerp(cloth, trim, 0.4f);
                    if (Ell(u, v - 0.43f, 0.075f, 0.11f, 0f, 0f)) c = skin;                                                                                       // leher
                    if (Ell(hx, hy, 0.25f, 0.215f, 0f, 0f)) c = skin;                                                                                             // wajah
                    if (Ell(hx, hy - 0.10f, 0.28f, 0.14f, 0f, 0f) && hy > 0.04f - Mathf.Abs(u) * 0.25f) c = Color.Lerp(hairA, hairB, 0.12f);                       // poni
                    if (Ell(u - 0.095f, hy + 0.02f, 0.045f, 0.03f, 0f, 0f) || Ell(u + 0.095f, hy + 0.02f, 0.045f, 0.03f, 0f, 0f)) c = eye;                        // mata
                    if (Ell(u, hy + 0.12f, 0.03f, 0.008f, 0f, 0f)) c = new Color(0.8f, 0.4f, 0.4f);                                                              // mulut
                    if (lynx && Ell(u, v - 0.31f, 0.035f, 0.05f, 0f, 0f)) c = new Color(0.4f, 0.8f, 1f);                                                           // permata dada
                    px[y * W + x] = c;
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 100f);
        }
        static bool Ell(float x, float y, float rx, float ry, float cx, float cy) { float a = (x - cx) / rx, b = (y - cy) / ry; return a * a + b * b <= 1f; }
    }
}
