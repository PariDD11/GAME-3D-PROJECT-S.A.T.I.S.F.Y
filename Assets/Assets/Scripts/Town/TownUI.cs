using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HunterGuild
{
    public class UIEntry
    {
        public string label;
        public Action action;
        public bool enabled;
        public UIEntry(string label, Action action, bool enabled = true)
        {
            this.label = label; this.action = action; this.enabled = enabled;
        }
    }

    /// <summary>UI runtime (HUD + panel menu generik) yang dibangun lewat kode, tanpa setup manual di Inspector.</summary>
    public class TownUI : MonoBehaviour
    {
        public static TownUI I { get; private set; }
        public bool PanelOpen { get { return (panel != null && panel.activeSelf) || externalOpen || (dlg != null && dlg.IsOpen); } }
        public Transform Root { get; private set; }

        const float BarWidth = 240f;
        GameHud hud;

        Font font;
        Text promptText, toastText, panelTitle, panelHeader, hungerLabel, thirstLabel;
        Text nameText, subText, hpLabel, staLabel, portraitText, bannerTitle, bannerSub, clockText, logText;
        RectTransform hungerFill, thirstFill, hpFill, staFill, manaFill, listRoot;
        Text manaLabel;
        RectTransform staBg, manaBg;
        GameObject panel;
        DialogueUI dlg;
        bool dlgBroken;
        string currentTitle, prompt = "";
        Func<string> headerFn;
        Func<List<UIEntry>> entriesFn;
        float toastTimer;
        bool externalOpen;
        readonly Text[] itemSlotTxt = new Text[4];
        readonly Text[] skillSlotTxt = new Text[4];
        readonly Image[] skillSlotIcon = new Image[4];
        static readonly string[] SkillKeyLabels = { "Z", "X", "C", "V" };

        void Awake() { I = this; Build(); }
        void OnDestroy() { if (I == this) I = null; }

        // ---------------- Public API ----------------
        public void SetPrompt(string text) { if (DialogueUI.SuppressPrompt) return; prompt = text; }

        public void Toast(string msg)
        {
            HudChat.Add("Sistem", System.Text.RegularExpressions.Regex.Replace(msg ?? "", "<[^>]*>", ""));
            if (toastText == null) return;
            msg = Loc.T(msg);
            toastText.text = msg;
            toastTimer = 3.5f;
        }

        public void Open(string title, Func<string> header, Func<List<UIEntry>> entries)
        {
            currentTitle = title; headerFn = header; entriesFn = entries;
            if (!PanelOpen) Audio.Sfx("ui_open");
            if (dlg != null) dlg.Begin();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();
        }

        public void Close()
        {
            if (CharacterCreatorUI.I != null && CharacterCreatorUI.I.BlocksClose) return;
            if (CharacterCreatorUI.I != null && CharacterCreatorUI.I.IsOpen) CharacterCreatorUI.I.Hide();
            if (PanelOpen) Audio.Sfx("ui_close");
            if (dlg != null) dlg.End();
            if (panel != null) panel.SetActive(false);
            entriesFn = null;
            if (BackpackUI.I != null) BackpackUI.I.Hide();
            if (SkillUI.I != null) SkillUI.I.Hide();
            if (LeaderboardUI.I != null) LeaderboardUI.I.Hide();
            SetExternalOpen(false);
        }

        /// <summary>Dipanggil jendela eksternal (Backpack/Skill) agar gerak, waktu, dan kursor ikut terkunci.</summary>
        public void SetExternalOpen(bool open)
        {
            externalOpen = open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        // ---------------- Update ----------------
        // ---------------- pengaturan kenyamanan (UiSettings) ----------------
        CanvasScaler uiScaler;
        List<CanvasGroup> hudGroups; float hudAlpha = 1f, calmT, lastHp = -1f, txScan;
        readonly Dictionary<int, int> txBase = new Dictionary<int, int>(), txApplied = new Dictionary<int, int>();

        public void ApplyUiSettings()
        {
            if (uiScaler != null) uiScaler.referenceResolution = new Vector2(1920f, 1080f) / UiSettings.Scale;
            txScan = 0f;
            if (!UiSettings.HudFade && hudGroups != null) foreach (var g in hudGroups) if (g != null) g.alpha = 1f;
            hudAlpha = 1f;
        }

        void CollectHud()
        {
            hudGroups = new List<CanvasGroup>();
            if (Root == null) return;
            if (hud != null) foreach (var g in hud.FadeGroups()) if (g != null) hudGroups.Add(g);
        }

        // HUD memudar halus saat tenang (tidak bertarung, HP/lapar/haus aman, tidak ada panel) dan kembali jelas saat ada kejadian
        void UpdateHudFade()
        {
            if (hudGroups == null) CollectHud();
            bool calm = UiSettings.HudFade && !PanelOpen;
            var pc = PlayerCombat.I; var gs = GameState.I;
            if (calm && pc != null)
            {
                if (lastHp >= 0f && pc.hp < lastHp - 0.01f) calmT = 0f;          // baru terkena damage
                lastHp = pc.hp;
                if (pc.hp < pc.MaxHp * 0.7f || pc.IsDead) calm = false;
            }
            if (calm && gs != null && (gs.hunger < 25f || gs.thirst < 25f)) calm = false;
            if (calm) foreach (var m in Monster.All) if (m != null && m.InCombat) { calm = false; break; }
            if (calm && (TownInput.AnyActivity())) calmT = 0f;
            calmT = calm ? calmT + Time.unscaledDeltaTime : 0f;
            float target = calm && calmT > 8f ? 0.38f : 1f;
            hudAlpha = Mathf.MoveTowards(hudAlpha, target, Time.unscaledDeltaTime * (target < hudAlpha ? 0.35f : 2.5f));
            foreach (var g in hudGroups) if (g != null) g.alpha = hudAlpha;
        }

        // skala teks: ukuran dasar tiap Text dicatat lalu dikalikan UiSettings.TextScale (dialog tidak ikut karena tata letaknya dihitung)
        void UpdateTextScale()
        {
            txScan -= Time.unscaledDeltaTime;
            if (txScan > 0f || Root == null) return;
            txScan = 1.2f;
            float k = UiSettings.TextScale;
            foreach (var t in Root.GetComponentsInChildren<Text>(true))
            {
                if (t == null || t.resizeTextForBestFit) continue;
                int id = t.GetInstanceID(), b, ap;
                if (!txBase.TryGetValue(id, out b)) { if (Mathf.Approximately(k, 1f)) continue; b = t.fontSize; txBase[id] = b; txApplied[id] = b; }
                else if (txApplied.TryGetValue(id, out ap) && t.fontSize != ap) { b = t.fontSize; txBase[id] = b; }
                if (InDialogue(t.transform)) continue;
                int ns = Mathf.Max(8, Mathf.RoundToInt(b * k));
                if (t.fontSize != ns) t.fontSize = ns;
                txApplied[id] = ns;
            }
        }

        static bool InDialogue(Transform t)
        {
            for (int i = 0; i < 8 && t != null; i++, t = t.parent) if (t.name == "DialogueBox") return true;
            return false;
        }

        int themeVer = -1;

        void Update()
        {
            var gs = GameState.I;
            UpdateHudFade();
            UpdateTextScale();
            if (gs != null && UiStyle.Stale(ref themeVer)) UiStyle.Reskin(Root);      // job berganti -> HUD & panel ikut tema baru
            if (gs != null) UpdateHud(gs);
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastText.text = "";
            }
            if (dlg != null) dlg.Tick();
            if (PanelOpen && TownInput.CancelDown()) Close();
        }

        void LateUpdate()
        {
            promptText.text = PanelOpen ? "" : Loc.T(prompt);
            prompt = "";
        }

        void UpdateHud(GameState gs) { if (hud != null) hud.Tick(gs); }

        const float PanelH = 900f;

        internal static string PlaceNameFor(out string sub) { return PlaceName(out sub); }

        static string PlaceName(out string sub)
        {
            string sn = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (TowerManager.I != null) { sub = "Lantai " + TowerManager.I.Floor + "  -  " + TowerDB.TierName(Mathf.Max(1, TowerManager.I.Floor)); return "Menara Eldrath"; }
            if (HuntManager.I != null)
            {
                string z = HuntManager.I.CurrentZone;
                sub = z == HuntManager.I.defaultZone ? "Area Perburuan" : HuntManager.I.defaultZone;
                return z;
            }
            if (sn == GuildConfig.InteriorScene) { sub = "Balai Hunter"; return "Guild Hunter"; }
            if (InteriorInfo.IsInterior(sn)) return InteriorInfo.Title(sn, out sub);
            if (sn == CityGate.DungeonCityScene) { sub = "Gerbang Menara"; return "Kota Eldrath"; }
            if (sn == GuildConfig.TownScene) { sub = "Kota Perbatasan - Pusat Hunter Guild"; return "Ashwatch"; }
            if (sn == "StartVillage") { sub = "Kampung Awal Hunter"; return "Desa Pemula"; }
            sub = ""; return sn;
        }

        static void SetBar(RectTransform fill, Text label, string name, float value)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
            label.text = name + " " + Mathf.RoundToInt(value);
        }

        void Refresh()
        {
            var fn = entriesFn;
            var list = fn();
            string head = headerFn != null ? headerFn() : "";
            if (dlg != null && !dlgBroken && dlg.Wants(currentTitle, head, list))
            {
                try
                {
                    panel.SetActive(false);
                    dlg.Render(currentTitle, head, list, e =>
                    {
                        if (e == null) { Refresh(); return; }
                        if (e.action != null) e.action();
                        if (PanelOpen && entriesFn == fn) Refresh();
                    }, Close);
                    return;
                }
                catch (Exception ex)
                {
                    // dialog gagal digambar: jangan blokir gameplay, pakai panel biasa dan catat errornya
                    dlgBroken = true;
                    Debug.LogError("[DialogueUI] gagal menggambar dialog, kembali ke panel biasa: " + ex);
                    dlg.Hide();
                }
            }
            if (dlg != null) dlg.Hide();
            bool panelWas = panel.activeSelf;
            panel.SetActive(true);
            if (!panelWas) UiPop.Play(panel);
            panelTitle.text = Loc.T(currentTitle);
            panelHeader.text = Loc.T(head);
            LayoutPanel();
            for (int i = listRoot.childCount - 1; i >= 0; i--)
            {
                var c = listRoot.GetChild(i);
                c.SetParent(null);
                Destroy(c.gameObject);
            }
            foreach (var e in list)
            {
                var entry = e;
                MakeButton(listRoot, Loc.T(entry.label), () =>
                {
                    if (entry.action != null) entry.action();
                    if (PanelOpen && entriesFn == fn) Refresh();
                }, entry.enabled, EntryHeight(entry.label));
            }
        }

        /// <summary>Header bisa panjang (deskripsi misi): ukur tingginya, kecilkan font bila perlu, lalu letakkan daftar tombol tepat di bawahnya.</summary>
        void LayoutPanel()
        {
            const float width = 752f, maxH = 430f, top = 76f;
            string txt = panelHeader.text;
            int size = 24;
            float h = 0f;
            var gen = new TextGenerator();
            for (; size >= 17; size -= 2)
            {
                var st = panelHeader.GetGenerationSettings(new Vector2(width, 0f));
                st.fontSize = size; st.resizeTextForBestFit = false; st.generateOutOfBounds = true;
                h = gen.GetPreferredHeight(txt, st) / Mathf.Max(0.01f, panelHeader.pixelsPerUnit);
                if (h <= maxH) break;
            }
            panelHeader.fontSize = Mathf.Max(size, 17);
            if (string.IsNullOrEmpty(txt)) h = 0f;
            h = Mathf.Min(h, maxH + 20f);
            panelHeader.rectTransform.sizeDelta = new Vector2(-48f, h + 6f);
            float listTop = top + h + (h > 0f ? 16f : 4f);
            listRoot.anchoredPosition = new Vector2(0f, -listTop);
            listRoot.sizeDelta = new Vector2(-48f, PanelH - listTop - 96f);
        }

        // ---------------- Builder ----------------
        void Build()
        {
#if UNITY_2022_2_OR_NEWER
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            EnsureEventSystem();

            var cgo = new GameObject("TownCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.AddComponent<CanvasScaler>(); uiScaler = scaler;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080) / UiSettings.Scale;
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            Transform root = canvas.transform;
            Root = root;

            // ---- HUD baru (potret, bar, boss bar, minimap, quest, chat, skill bar, menu, EXP) -> GameHud.cs
            hud = new GameHud(root);

            // Prompt interaksi tengah-bawah
            promptText = Txt("Prompt", root, 34, TextAnchor.MiddleCenter, UiStyle.Gold);
            var psh = promptText.gameObject.AddComponent<Shadow>(); psh.effectColor = new Color(0, 0, 0, 0.95f); psh.effectDistance = new Vector2(2f, -2f);
            Place(promptText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 330), new Vector2(1000, 60));

            // Toast tengah-atas
            toastText = Txt("Toast", root, 30, TextAnchor.MiddleCenter, Color.white);
            toastText.fontSize = 26;
            toastText.verticalOverflow = VerticalWrapMode.Truncate;
            Place(toastText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -372), new Vector2(1300, 56));
            var tsh = toastText.gameObject.AddComponent<Shadow>(); tsh.effectColor = new Color(0, 0, 0, 0.95f); tsh.effectDistance = new Vector2(2f, -2f);

            // Panel menu
            var pbg = UiStyle.Frame("Panel", root);
            pbg.raycastTarget = true;
            var c = new Vector2(0.5f, 0.5f);
            Place(pbg.rectTransform, c, c, c, Vector2.zero, new Vector2(800, PanelH));
            panel = pbg.gameObject;
            var back = Img("Backing", pbg.transform, new Color(0.04f, 0.05f, 0.08f, 0.93f));
            back.raycastTarget = false;
            back.rectTransform.anchorMin = Vector2.zero; back.rectTransform.anchorMax = Vector2.one;
            back.rectTransform.offsetMin = new Vector2(8f, 8f); back.rectTransform.offsetMax = new Vector2(-8f, -8f);
            back.transform.SetAsFirstSibling();

            panelTitle = Txt("Title", pbg.transform, 38, TextAnchor.MiddleCenter, UiStyle.Gold);
            Place(panelTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(0, 56));

            panelHeader = Txt("Header", pbg.transform, 24, TextAnchor.UpperLeft, UiStyle.Cream);
            Place(panelHeader.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -76), new Vector2(-48, 120));

            var pdiv = UiStyle.Divider(pbg.transform, 60, 70, 680);
            listRoot = Rect("List", pbg.transform);
            Place(listRoot, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -206), new Vector2(-48, 440));
            var vlg = listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var close = MakeButton(pbg.transform, "Tutup (Esc)", Close, true, 54f);
            close.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;
            Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(300, 54));

            toastText.transform.SetAsLastSibling();
            panel.SetActive(false);
            dlg = new DialogueUI(root);
            toastText.transform.SetAsLastSibling();
        }

        void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            if (FindFirstObjectByType<EventSystem>() != null) return;
#else
            if (FindObjectOfType<EventSystem>() != null) return;
#endif
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        void MakeSlotRow(Transform parent, float y, string[] keys, Text[] store, Image[] icons = null)
        {
            for (int i = 0; i < 4; i++)
            {
                var bg = UiStyle.Plate("Slot" + keys[i], parent);
                Place(bg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-189f + i * 126f, y), new Vector2(120, 60));
                var t = Txt("T", bg.transform, 15, TextAnchor.MiddleCenter, UiStyle.Cream);
                Place(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                t.rectTransform.offsetMin = new Vector2(6f, 2f); t.rectTransform.offsetMax = new Vector2(-6f, -2f);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 10; t.resizeTextMaxSize = 16;
                t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
                t.text = "[" + keys[i] + "]";
                store[i] = t;
                if (icons != null)
                {
                    var ic = Img("Icon", bg.transform, Color.white);
                    Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(48f, 48f));
                    ic.raycastTarget = false; ic.enabled = false;
                    icons[i] = ic;
                    t.alignment = TextAnchor.MiddleRight;
                    t.rectTransform.offsetMin = new Vector2(54f, 2f);
                }
            }
        }

        RectTransform MakeBar(Transform parent, Vector2 pos, Color fillColor, out Text label)
        {
            var bg = Img("BarBg", parent, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            Place(bg.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, pos, new Vector2(BarWidth, 22));
            var fill = Img("Fill", bg.transform, fillColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            label = Txt("Label", bg.transform, 16, TextAnchor.MiddleCenter, Color.white);
            Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return fill.rectTransform;
        }

        /// <summary>Label bertingkat: baris pertama = judul (emas), sisanya = keterangan (kecil).</summary>
        static string Rich(string label)
        {
            int i = label.IndexOf('\n');
            if (i < 0) return label;
            return "<color=#F0D58A>" + label.Substring(0, i) + "</color>\n<size=18>" + label.Substring(i + 1) + "</size>";
        }

        float EntryHeight(string label)
        {
            var st = new TextGenerationSettings
            {
                font = font, fontSize = 24, fontStyle = FontStyle.Normal, lineSpacing = 1f, scaleFactor = 1f, richText = true,
                textAnchor = TextAnchor.MiddleLeft, horizontalOverflow = HorizontalWrapMode.Wrap, verticalOverflow = VerticalWrapMode.Overflow,
                generateOutOfBounds = true, resizeTextForBestFit = false, updateBounds = false,
                generationExtents = new Vector2(716f, 0f), pivot = new Vector2(0f, 0.5f), color = Color.white
            };
            var gen = new TextGenerator();
            float h = gen.GetPreferredHeight(Rich(label), st);
            return Mathf.Max(46f, h + 20f);
        }

        Button MakeButton(Transform parent, string label, Action onClick, bool enabled, float height)
        {
            var im = Img("Btn", parent, Color.white);
            var le = im.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            b.interactable = enabled;
            UiStyle.StyleButton(im, enabled);
            var t = Txt("Label", im.transform, 24, TextAnchor.MiddleLeft, enabled ? UiStyle.Cream : new Color(0.55f, 0.55f, 0.55f));
            Place(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            t.rectTransform.offsetMin = new Vector2(16, 0);
            t.rectTransform.offsetMax = new Vector2(-16, 0);
            t.supportRichText = true;
            t.text = enabled ? Rich(label) : Regex.Replace(Rich(label), "<color=[^>]*>|</color>", "");
            b.onClick.AddListener(() => Audio.Sfx("click"));
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        Image Img(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = color;
            return im;
        }

        Text Txt(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}
