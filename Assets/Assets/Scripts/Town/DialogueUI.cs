using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>Kartu quest di sisi kanan dialog (panel 6). Diisi pemanggil sebelum menu dibuka; tombol Terima memicu UIEntry berawalan acceptPrefix.</summary>
    public class DialogueCard
    {
        public string title = "Permintaan Misi", desc = "", objective = "", reward = "", acceptPrefix = "Terima";
        public string itemName = "";              // bila diisi: objektif bergaya item (kotak ikon + nama + progres)
        public int have, need, gold, exp;         // progres item dan hadiah (koin + EXP)
    }

    /// <summary>
    /// UI dialog NPC bergaya emas-gelap. Memakai API TownUI.Open yang sudah ada: bila menu dibuka saat berbicara dengan NPC,
    /// tampilan otomatis berganti ke dialog (potret + nama + peran + teks + pilihan). Daftar panjang tetap memakai panel biasa.
    /// Panel 1: penanda di atas kepala NPC. 2/8: dialog biasa/narasi. 3: pilihan di kanan. 4/7: pilihan di bawah teks (termasuk Toko).
    /// 5: potret di kanan. 6: kartu quest. 9: tombol Lanjut.
    /// </summary>
    public class DialogueUI
    {
        // ------------------------------------------------------------ konteks
        public static Interactable Context;
        static int ctxFrame = -10;
        public static DialogueCard Card;
        public static bool SuppressPrompt;
        public static int ClosedFrame = -10;
        public static void SetContext(Interactable it) { Context = it; ctxFrame = Time.frameCount; }

        // ------------------------------------------------------------ status
        readonly Transform root;
        readonly Canvas canvas;
        GameObject box;
        Interactable ctx;
        bool declined;
        int openFrame;
        Text body;
        string fullText = "";
        float reveal;
        bool revealed = true;
        RectTransform chevron;
        CanvasGroup chevronGroup;
        Action<UIEntry> pick;
        Action close;
        UIEntry lanjut;
        readonly List<UIEntry> choiceEntries = new List<UIEntry>();
        readonly List<Image> choiceImgs = new List<Image>();
        int sel;
        ScrollRect listScroll, bodyScrollRect;
        bool hasCard; UIEntry acceptEntry;

        // penanda panel 1
        GameObject marker; RectTransform markerRt; Text mName, mRole, mGlyph; Image mRing; Interactable markerIt;
        PlayerController player;

        static Sprite disc;
        static readonly Dictionary<string, Sprite> portraitCache = new Dictionary<string, Sprite>();

        /// <summary>Potret ilustrasi: Resources/Portraits/NamaNPC.png (tipe Sprite atau Texture biasa).</summary>
        public static Sprite LoadPortrait(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            Sprite sp;
            if (portraitCache.TryGetValue(name, out sp)) return sp;
            sp = Resources.Load<Sprite>("Portraits/" + name);
            if (sp == null)
            {
                var tex = Resources.Load<Texture2D>("Portraits/" + name);
                if (tex != null) sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f);
            }
            portraitCache[name] = sp;
            return sp;
        }

        public DialogueUI(Transform root)
        {
            this.root = root;
            canvas = root.GetComponentInParent<Canvas>();
        }

        public bool IsOpen { get { return box != null && box.activeSelf; } }

        // ------------------------------------------------------------ siklus hidup
        /// <summary>Dipanggil TownUI.Open: bila baru saja berbicara dengan NPC, ambil konteksnya.</summary>
        public void Begin()
        {
            if (Context != null && Time.frameCount == ctxFrame && ctx != Context) { ctx = Context; declined = false; }
            else if (Context != null && Time.frameCount == ctxFrame) { declined = false; }
        }

        public void End()
        {
            if (IsOpen) ClosedFrame = Time.frameCount;
            Hide();
            if (curActor != null) { curActor.SetTalking(false); if (IsOpen || ClosedFrame == Time.frameCount) curActor.NextPortrait(); }
            ctx = null; Context = null; Card = null; declined = false;
        }

        public void Hide() { if (box != null) box.SetActive(false); }

        // ------------------------------------------------------------ pembantu pembicara
        public static bool Resolve(Interactable it, out string name, out string role, out NpcActor actor)
        {
            name = ""; role = ""; actor = null;
            if (it == null) return false;
            if (!string.IsNullOrEmpty(it.speakerName)) { name = it.speakerName; role = it.speakerRole ?? ""; return true; }
            actor = it.GetComponentInParent<NpcActor>();
            if (actor == null) return false;
            name = actor.displayName;
            string label = "";
            foreach (var tm in actor.GetComponentsInChildren<TextMesh>())
            {
                if (tm.transform.parent != null && tm.transform.parent.name == "Bubble") continue;
                label = tm.text; break;
            }
            if (!string.IsNullOrEmpty(label))
            {
                int a = label.IndexOf('<'), b = label.IndexOf('>');
                if (a >= 0 && b > a) role = label.Substring(a + 1, b - a - 1);
                else { int nl = label.IndexOf('\n'); if (nl >= 0) role = label.Substring(nl + 1).Trim(); }
                if (string.IsNullOrEmpty(name) || name == "NPC") { int nl2 = label.IndexOf('\n'); name = nl2 > 0 ? label.Substring(0, nl2).Trim() : label.Trim(); }
            }
            if (string.IsNullOrEmpty(role) && it is NpcChat) role = NpcChat.RaceTitle(actor.race);
            return true;
        }

        public bool Wants(string title, string header, List<UIEntry> list)
        {
            if (ctx == null || list == null) return false;
            return ctx.GetComponentInParent<NpcActor>() != null;      // hanya NPC yang bicara; benda/papan memakai panel biasa (UI-nya diganti nanti)
        }

        // ------------------------------------------------------------ pengukuran
        static float Measure(string txt, int size, float width, bool bold = false)
        {
            if (string.IsNullOrEmpty(txt)) return 0f;
            var st = new TextGenerationSettings
            {
                font = UIKit.F, fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, lineSpacing = 1.1f, scaleFactor = 1f, richText = true,
                textAnchor = TextAnchor.UpperLeft, horizontalOverflow = HorizontalWrapMode.Wrap, verticalOverflow = VerticalWrapMode.Overflow,
                generateOutOfBounds = true, resizeTextForBestFit = false, updateBounds = false,
                generationExtents = new Vector2(width, 0f), pivot = new Vector2(0f, 1f), color = Color.white
            };
            return new TextGenerator().GetPreferredHeight(txt, st);
        }

        public static Sprite DiscSprite { get { return Disc; } }
        static Sprite Disc
        {
            get
            {
                if (disc != null) return disc;
                const int n = 64;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - d) * n * 0.5f);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                t.SetPixels32(px); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; t.hideFlags = HideFlags.HideAndDontSave; t.Apply();
                disc = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
                return disc;
            }
        }

        // ------------------------------------------------------------ render dialog
        public void Render(string title, string header, List<UIEntry> list, Action<UIEntry> onPick, Action onClose)
        {
            pick = onPick; close = onClose;
            scrollFix = 4;
            string spName, spRole; NpcActor actor;
            if (!Resolve(ctx, out spName, out spRole, out actor)) { spName = title; spRole = ""; actor = null; }
            if (curActor != null && curActor != actor) curActor.SetTalking(false);
            curActor = actor;
            if (actor != null) actor.SetTalking(true);
            if (string.IsNullOrEmpty(header)) header = title;

            // kartu quest
            hasCard = false; acceptEntry = null;
            var card = declined ? null : Card;
            var rest = new List<UIEntry>();
            foreach (var e in list)
            {
                if (card != null && acceptEntry == null && e.label.StartsWith(card.acceptPrefix, StringComparison.OrdinalIgnoreCase)) { acceptEntry = e; continue; }
                rest.Add(e);
            }
            hasCard = card != null && acceptEntry != null;

            // mode
            lanjut = null;
            if (rest.Count == 1 && !hasCard && rest[0].label.StartsWith("Lanjut", StringComparison.OrdinalIgnoreCase)) lanjut = rest[0];
            bool shop = false;
            foreach (var e in rest) if (e.label.StartsWith("Beli", StringComparison.OrdinalIgnoreCase)) shop = true;
            bool rightList = !hasCard && lanjut == null && !shop && rest.Count >= 2 && rest.Count <= 4 && header.Length <= 90 && header.IndexOf('\n') < 0;
            bool underList = lanjut == null && rest.Count > 0 && !rightList;
            bool portraitRight = spName.IndexOf("Master", StringComparison.OrdinalIgnoreCase) >= 0 || spRole.IndexOf("Master", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 spRole.IndexOf("Tetua", StringComparison.OrdinalIgnoreCase) >= 0 || spRole.IndexOf("Gubernur", StringComparison.OrdinalIgnoreCase) >= 0;

            // ukuran
            const float boxW = 1340f, cardW = 410f;
            Sprite cut = (actor != null && actor.CurrentPortrait != null) ? actor.CurrentPortrait : LoadPortrait(spName);
            if (actor != null && actor.pSide != 0) portraitRight = actor.pSide == 2;
            float cutW = 300f * (actor != null ? Mathf.Clamp(actor.pScale, 0.4f, 1.8f) : 1f);
            float cutH = cut != null ? cutW * cut.rect.height / cut.rect.width : 0f;
            float portW = cut != null ? cutW * 0.82f : 250f;
            float padL = portraitRight ? 56f : 40f + portW + 26f;
            float padR = portraitRight ? 40f + portW + 26f : 56f;
            float textW = boxW - padL - padR - (hasCard ? cardW + 20f : 0f);
            float textFull = Mathf.Max(64f, Measure(header, 28, textW));
            float textH = Mathf.Min(textFull, 300f);
            bool bodyScroll = textFull > 300f;
            int visRows = Mathf.Min(rest.Count, 5);
            bool listScrolls = rest.Count > 5;
            float underH = underList ? visRows * 62f + 6f : 0f;
            float boxH = 22f + 40f + 30f + 20f + textH + (underList ? 14f + underH : 0f) + (lanjut != null ? 76f : 46f);
            float cardH = 0f;
            if (hasCard)
            {
                cardH = 22f + 40f + Measure(card.desc, 20, cardW - 44f) + 16f + 56f + 14f + 38f + 12f + 62f + 20f + (string.IsNullOrEmpty(card.itemName) ? 0f : 40f);
                boxH = Mathf.Max(boxH, cardH + 44f);
            }
            boxH = Mathf.Max(boxH, 270f);

            // bangun
            if (box == null)
            {
                box = UiStyle.Classic.Frame("DialogueBox", root).gameObject;
            }
            for (int i = box.transform.childCount - 1; i >= 0; i--) { var c = box.transform.GetChild(i); c.SetParent(null); UnityEngine.Object.Destroy(c.gameObject); }
            bool boxWas = box.activeSelf;
            box.SetActive(true);
            if (!boxWas) UiPop.Play(box);
            var rt = box.GetComponent<RectTransform>();
            UIKit.Place(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(boxW, boxH));
            var back = UIKit.Img("Backing", box.transform, new Color(0.04f, 0.045f, 0.075f, 0.95f));
            back.rectTransform.anchorMin = Vector2.zero; back.rectTransform.anchorMax = Vector2.one;
            back.rectTransform.offsetMin = new Vector2(8f, 8f); back.rectTransform.offsetMax = new Vector2(-8f, -8f);
            back.transform.SetAsFirstSibling();
            back.raycastTarget = true;
            var bb = back.gameObject.AddComponent<Button>();
            bb.transition = Selectable.Transition.None;
            bb.onClick.AddListener(Advance);
            var cls = UIKit.Btn(box.transform, "Tutup (Esc)", () => { if (close != null) close(); }, true, new Color(0.3f, 0.2f, 0.2f), 20);
            UIKit.At((RectTransform)cls.transform, 20f, -48f, 150f, 40f);

            // potret
            float ph = Mathf.Min(boxH - 28f, 340f);
            float px = portraitRight ? boxW - 40f - portW : 40f;
            if (cut != null)
            {
                // potret tanpa bingkai: menonjol di atas kotak dialog (gaya referensi)
                var ci = UIKit.Img("PortraitCut", box.transform, Color.white);
                ci.sprite = cut; ci.preserveAspect = true; ci.raycastTarget = false;
                float cx0 = portraitRight ? boxW - 24f - cutW : 24f;
                float adjX = actor != null ? actor.pX : 0f, adjY = actor != null ? actor.pY : 0f;
                UIKit.At(ci.rectTransform, cx0 + adjX, boxH - 10f - cutH - adjY, cutW, cutH);
                if (actor != null && actor.pFlip) ci.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                var pf = UiStyle.Classic.Frame("PortraitFrame", box.transform);
                UIKit.At(pf.rectTransform, px, boxH - 14f - ph, portW, ph);
                BuildPortrait(pf.transform, portW - 16f, ph - 16f, spName, actor);
            }

            // nama + peran + garis
            float tx = padL;
            float x0 = tx;
            var nm = UIKit.Txt("Name", box.transform, 36, TextAnchor.UpperLeft, UiStyle.Classic.Gold);
            nm.text = spName; UIKit.At(nm.rectTransform, x0, 20f, textW + (hasCard ? 0f : 0f), 44f);
            if (!string.IsNullOrEmpty(spRole))
            {
                var rl = UIKit.Txt("Role", box.transform, 22, TextAnchor.UpperLeft, UiStyle.Classic.Muted);
                rl.text = "<" + spRole + ">"; UIKit.At(rl.rectTransform, x0, 62f, textW, 28f);
            }
            var dv = UIKit.Img("Divider", box.transform, Color.white);
            dv.sprite = UiStyle.Classic.DividerSprite; dv.raycastTarget = false;
            UIKit.At(dv.rectTransform, x0, 92f, textW, 12f);

            Transform bodyParent = box.transform;
            float bx = x0, by0 = 112f;
            if (bodyScroll) { bodyParent = NewScroll(box.transform, x0, 112f, textW, textH, textFull + 8f, out bodyScrollRect); bx = 0f; by0 = 0f; }
            body = UIKit.Txt("Body", bodyParent, 28, TextAnchor.UpperLeft, UiStyle.Classic.Cream);
            body.supportRichText = true; body.lineSpacing = 1.1f;
            UIKit.At(body.rectTransform, bx, by0, textW - (bodyScroll ? 8f : 0f), textFull + 6f);
            fullText = header;
            revealed = header.IndexOf('<') >= 0 || header.Length < 3;
            reveal = revealed ? header.Length : 0f;
            body.text = revealed ? header : "";

            // pilihan
            choiceEntries.Clear(); choiceImgs.Clear(); sel = 0; listScroll = null;
            var all = new List<UIEntry>(rest);
            if (rightList)
            {
                float lw = 430f, lh = 58f;
                float lx = portraitRight ? 40f : boxW - 40f - lw + 0f;
                // kolom di atas kotak, rata kanan
                var col = UIKit.Rect("Choices", box.transform);
                UIKit.Place(col, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(-30f, 10f), new Vector2(lw, rest.Count * (lh + 8f)));
                for (int i = 0; i < rest.Count; i++) MakeChoice(col, rest[i], i, 0f, (rest.Count - 1 - i) * (lh + 8f), lw, lh, true);
            }
            else if (underList)
            {
                float y = 112f + textH + 14f;
                float lw = Mathf.Min(textW, 640f);
                Transform lp = box.transform; float lx = x0, ly = y;
                if (listScrolls) { lp = NewScroll(box.transform, x0, y, lw + 6f, visRows * 62f, rest.Count * 62f + 4f, out listScroll); lx = 0f; ly = 0f; }
                for (int i = 0; i < rest.Count; i++) { MakeChoice(lp, rest[i], i, lx, ly + i * 62f, lw, 54f, false); }
            }
            else if (lanjut != null)
            {
                var lb = UIKit.Img("Lanjut", box.transform, Color.white);
                lb.sprite = UiStyle.Classic.PlateSprite; lb.type = Image.Type.Sliced;
                UiStyle.Classic.StyleButton(lb, true);   // sama seperti tombol pilihan: pelat gelap berbingkai emas
                UIKit.At(lb.rectTransform, boxW - 56f - 220f - (portraitRight ? portW + 26f : 0f), boxH - 78f, 220f, 54f);
                var b = lb.gameObject.AddComponent<Button>(); b.targetGraphic = lb;
                var cb = b.colors; cb.highlightedColor = new Color(1f, 0.92f, 0.65f); b.colors = cb;
                var lt = UIKit.Txt("T", lb.transform, 28, TextAnchor.MiddleCenter, UiStyle.Classic.Cream);
                lt.text = "Lanjut  >"; lt.fontStyle = FontStyle.Bold;
                var lo = lt.gameObject.AddComponent<Outline>(); lo.effectColor = new Color(0f, 0f, 0f, 0.9f); lo.effectDistance = new Vector2(1.5f, -1.5f);
                UIKit.Place(lt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var le = lanjut;
                b.onClick.AddListener(() => { Audio.Sfx("click"); Fire(le); });
            }
            ApplySel();

            // kartu quest
            if (hasCard) BuildCard(card, boxW - 40f - cardW - (portraitRight ? portW + 26f : 0f) + (portraitRight ? 0f : 0f), 22f, cardW, boxH - 44f);

            // chevron (lanjut dialog tanpa pilihan)
            chevron = null;
            if (lanjut == null && rest.Count == 0 && !hasCard)
            {
                var cg = UIKit.Rect("Chevron", box.transform);
                UIKit.At(cg, boxW - 56f - (portraitRight ? portW + 26f : 0f) - 40f, boxH - 62f, 40f, 34f);
                for (int k = 0; k < 2; k++)
                    foreach (float sgn in new[] { -1f, 1f })
                    {
                        var bar = UIKit.Img("Bar", cg, UiStyle.Classic.Gold); bar.raycastTarget = false;
                        UIKit.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(sgn * 8.5f, -8f - k * 12f), new Vector2(22f, 5f));
                        bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -sgn * 38f);
                    }
                chevron = cg;
                chevronGroup = cg.gameObject.AddComponent<CanvasGroup>();
            }

            openFrame = Time.frameCount;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        static Transform NewScroll(Transform parent, float x, float y, float w, float h, float contentH, out ScrollRect sr)
        {
            var vp = UIKit.Img("Scroll", parent, new Color(1f, 1f, 1f, 0.01f));
            UIKit.At(vp.rectTransform, x, y, w, h);
            vp.gameObject.AddComponent<RectMask2D>();
            sr = vp.gameObject.AddComponent<ScrollRect>();
            var content = UIKit.Rect("Content", vp.transform);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = new Vector2(0f, contentH);
            sr.viewport = vp.rectTransform; sr.content = content;
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40f;
            return content;
        }

        // ------------------------------------------------------------ elemen
        void MakeChoice(Transform parent, UIEntry e, int idx, float x, float y, float w, float h, bool bottomAnchored)
        {
            var im = UIKit.Img("Choice" + idx, parent, Color.white);
            im.sprite = UiStyle.Classic.PlateSprite; im.type = Image.Type.Sliced;
            if (bottomAnchored)
            {
                im.rectTransform.anchorMin = new Vector2(0f, 0f); im.rectTransform.anchorMax = new Vector2(0f, 0f); im.rectTransform.pivot = new Vector2(0f, 0f);
                im.rectTransform.anchoredPosition = new Vector2(x, y); im.rectTransform.sizeDelta = new Vector2(w, h);
            }
            else UIKit.At(im.rectTransform, x, y, w, h);
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im; b.interactable = e.enabled;
            UiStyle.Classic.StyleButton(im, e.enabled);
            // ikon
            var ring = UIKit.Img("Icon", im.transform, e.enabled ? UiStyle.Classic.Gold : new Color(0.5f, 0.5f, 0.5f));
            ring.sprite = UiStyle.Classic.RingSprite; ring.raycastTarget = false;
            UIKit.Place(ring.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(h - 18f, h - 18f));
            var ic = UIKit.Txt("Glyph", ring.transform, 20, TextAnchor.MiddleCenter, e.enabled ? UiStyle.Classic.Gold : new Color(0.5f, 0.5f, 0.5f));
            ic.text = Glyph(e.label);
            UIKit.Place(ic.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var t = UIKit.Txt("Label", im.transform, 26, TextAnchor.MiddleLeft, e.enabled ? UiStyle.Classic.Cream : new Color(0.55f, 0.55f, 0.55f));
            string lbl = e.label; int nl = lbl.IndexOf('\n');
            string pre = idx < 9 ? (idx + 1) + ".  " : "";
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            if (nl > 0)
            {
                string sub = lbl.Substring(nl + 1).Replace("\n", "  ");
                int maxc = Mathf.Max(30, (int)((w - h) / 10.5f));
                if (sub.Length > maxc) sub = sub.Substring(0, maxc - 1).TrimEnd() + "\u2026";
                t.text = pre + lbl.Substring(0, nl) + "\n<size=18><color=#B8B2A0>" + sub + "</color></size>";
                t.resizeTextForBestFit = false; t.fontSize = 24;
            }
            else
            {
                t.text = pre + lbl;
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 15; t.resizeTextMaxSize = 26;
            }
            UIKit.Place(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            t.rectTransform.offsetMin = new Vector2(h + 4f, 0f); t.rectTransform.offsetMax = new Vector2(-12f, 0f);
            int my = choiceEntries.Count;
            choiceEntries.Add(e); choiceImgs.Add(im);
            b.onClick.AddListener(() => { Audio.Sfx("click"); sel = my; Fire(choiceEntries[my]); });
        }

        static string Glyph(string label)
        {
            string l = label.ToLowerInvariant();
            if (l.StartsWith("beli") || l.Contains("barang")) return "B";
            if (l.StartsWith("jual")) return "$";
            if (l.StartsWith("<") || l.StartsWith("kembali") || l.StartsWith("tutup") || l.StartsWith("sampai") || l.StartsWith("nanti")) return "<";
            if (l.Contains("quest") || l.Contains("misi")) return "!";
            if (l.EndsWith(">")) return ">";
            return "...";
        }

        void ApplySel()
        {
            for (int i = 0; i < choiceImgs.Count; i++)
            {
                if (choiceImgs[i] == null) continue;
                bool on = i == sel && choiceEntries[i].enabled;
                choiceImgs[i].color = on ? new Color(1f, 0.9f, 0.5f) : Color.white;
            }
        }

        static void Chip(Transform par, float x, float y, float w, string icon, Color col, string val)
        {
            var bg = UiStyle.Classic.Plate("Chip", par); bg.color = new Color(0.12f, 0.12f, 0.16f, 1f);
            UIKit.At(bg.rectTransform, x, y, w, 46f);
            var ic = UIKit.Img("Ic", bg.transform, col); ic.sprite = Disc; ic.raycastTarget = false;
            UIKit.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(34f, 34f));
            var it = UIKit.Txt("T", ic.transform, icon.Length > 1 ? 13 : 20, TextAnchor.MiddleCenter, new Color(0.25f, 0.15f, 0.02f)); it.text = icon; it.fontStyle = FontStyle.Bold;
            UIKit.Place(it.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var vt = UIKit.Txt("V", bg.transform, 24, TextAnchor.MiddleCenter, UiStyle.Classic.Cream); vt.text = val;
            UIKit.Place(vt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            vt.rectTransform.offsetMin = new Vector2(46f, 0f); vt.rectTransform.offsetMax = new Vector2(-6f, 0f);
        }

        void BuildCard(DialogueCard c, float x, float y, float w, float h)
        {
            var fr = UiStyle.Classic.Plate("Card", box.transform);
            fr.color = new Color(0.07f, 0.07f, 0.1f, 0.95f);
            UIKit.At(fr.rectTransform, x, y, w, h);
            float cy = 16f, cw = w - 40f;
            var t = UIKit.Txt("CT", fr.transform, 26, TextAnchor.UpperLeft, UiStyle.Classic.Gold); t.text = c.title; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            float th = Mathf.Max(34f, Measure(c.title, 26, cw)); UIKit.At(t.rectTransform, 20f, cy, cw, th + 2f); cy += th + 10f;
            var d = UIKit.Txt("CD", fr.transform, 20, TextAnchor.UpperLeft, UiStyle.Classic.Cream); d.text = c.desc; d.supportRichText = true;
            float dh = Measure(c.desc, 20, cw); UIKit.At(d.rectTransform, 20f, cy, cw, dh + 4f); cy += dh + 14f;
            // objektif
            bool itemObj = !string.IsNullOrEmpty(c.itemName);
            float oh = itemObj ? 78f : 52f;
            var ob = UiStyle.Classic.Plate("Obj", fr.transform); ob.color = new Color(0.12f, 0.12f, 0.16f, 1f);
            UIKit.At(ob.rectTransform, 20f, cy, cw, oh);
            if (itemObj)
            {
                var ib = UiStyle.Classic.Frame("ItemBox", ob.transform);
                UIKit.Place(ib.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(62f, 62f));
                var ii = UIKit.Txt("I", ib.transform, 34, TextAnchor.MiddleCenter, new Color(0.45f, 0.75f, 1f)); ii.text = c.itemName.Substring(0, 1).ToUpper(); ii.fontStyle = FontStyle.Bold;
                UIKit.Place(ii.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var inm = UIKit.Txt("N", ob.transform, 22, TextAnchor.MiddleLeft, UiStyle.Classic.Cream); inm.text = c.itemName + "\n" + c.have + " / " + c.need;
                UIKit.Place(inm.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                inm.rectTransform.offsetMin = new Vector2(84f, 0f); inm.rectTransform.offsetMax = new Vector2(-8f, 0f);
            }
            else
            {
                var oi = UIKit.Img("OI", ob.transform, new Color(0.35f, 0.6f, 0.95f)); oi.sprite = UiStyle.Classic.PlateSprite; oi.type = Image.Type.Sliced; oi.raycastTarget = false;
                UIKit.Place(oi.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(34f, 34f));
                var ot = UIKit.Txt("OT", ob.transform, 20, TextAnchor.MiddleLeft, UiStyle.Classic.Cream); ot.text = c.objective; ot.supportRichText = true;
                UIKit.Place(ot.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                ot.rectTransform.offsetMin = new Vector2(52f, 0f); ot.rectTransform.offsetMax = new Vector2(-8f, 0f);
            }
            cy += oh + 12f;
            var rh = UIKit.Txt("RH", fr.transform, 20, TextAnchor.UpperLeft, UiStyle.Classic.Gold); rh.text = itemObj ? "Hadiah:" : "Hadiah"; UIKit.At(rh.rectTransform, 20f, cy, cw, 26f); cy += 28f;
            if (c.gold > 0 || c.exp > 0)
            {
                float chipW = (cw - 10f) / 2f; float cx = 20f;
                if (c.gold > 0) { Chip(fr.transform, cx, cy, chipW, "G", new Color(1f, 0.82f, 0.25f), c.gold.ToString("N0")); cx += chipW + 10f; }
                if (c.exp > 0) Chip(fr.transform, cx, cy, chipW, "EXP", new Color(0.95f, 0.75f, 0.3f), c.exp.ToString("N0"));
            }
            else
            {
                var rv = UIKit.Txt("RV", fr.transform, 20, TextAnchor.UpperLeft, UiStyle.Classic.Cream); rv.text = c.reward; rv.supportRichText = true;
                UIKit.At(rv.rectTransform, 20f, cy, cw, 30f);
            }
            // tombol
            float by = h - 66f;
            var yes = UIKit.Img("Terima", fr.transform, new Color(0.95f, 0.75f, 0.3f)); yes.sprite = UiStyle.Classic.PlateSprite; yes.type = Image.Type.Sliced;
            UIKit.At(yes.rectTransform, 20f, by, cw * 0.55f - 6f, 52f);
            var yb = yes.gameObject.AddComponent<Button>(); yb.targetGraphic = yes;
            var yt = UIKit.Txt("T", yes.transform, 26, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.65f)); yt.text = "Terima"; yt.fontStyle = FontStyle.Bold;
            var yo = yt.gameObject.AddComponent<Outline>(); yo.effectColor = new Color(0f, 0f, 0f, 0.9f); yo.effectDistance = new Vector2(1.5f, -1.5f);
            UIKit.Place(yt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var ae = acceptEntry;
            yb.onClick.AddListener(() => { Audio.Sfx("click"); Fire(ae); });
            var no = UiStyle.Classic.Plate("Tolak", fr.transform); no.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            UIKit.At(no.rectTransform, 20f + cw * 0.55f + 6f, by, cw * 0.45f - 6f, 52f);
            no.raycastTarget = true;
            var nb = no.gameObject.AddComponent<Button>(); nb.targetGraphic = no;
            var nt = UIKit.Txt("T", no.transform, 26, TextAnchor.MiddleCenter, UiStyle.Classic.Cream); nt.text = "Tolak";
            UIKit.Place(nt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            nb.onClick.AddListener(() => { Audio.Sfx("click"); declined = true; if (pick != null) pick(null); });
        }

        // ------------------------------------------------------------ potret
        static readonly Color[] hairCols = { new Color(0.32f, 0.2f, 0.12f), new Color(0.1f, 0.08f, 0.08f), new Color(0.72f, 0.55f, 0.25f), new Color(0.6f, 0.6f, 0.62f), new Color(0.55f, 0.18f, 0.12f), new Color(0.22f, 0.25f, 0.45f) };
        static readonly Color[] clothCols = { new Color(0.35f, 0.2f, 0.12f), new Color(0.2f, 0.28f, 0.4f), new Color(0.4f, 0.12f, 0.15f), new Color(0.18f, 0.32f, 0.22f), new Color(0.3f, 0.22f, 0.4f), new Color(0.15f, 0.15f, 0.18f) };

        static Image Part(Transform par, Sprite s, Color c, float cx, float cy, float w, float h)
        {
            var im = UIKit.Img("P", par, c);
            im.sprite = s; im.raycastTarget = false;
            UIKit.Place(im.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(cx, cy), new Vector2(w, h));
            return im;
        }

        /// <summary>Potret placeholder buatan kode. Taruh sprite di Resources/Portraits/NamaNPC.png untuk menggantinya dengan ilustrasi.</summary>
        public static void BuildPortrait(Transform par, float w, float h, string name, NpcActor a)
        {
            var holder = UIKit.Rect("Art", par);
            UIKit.Place(holder, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            holder.offsetMin = new Vector2(8f, 8f); holder.offsetMax = new Vector2(-8f, -8f);
            holder.gameObject.AddComponent<RectMask2D>();
            w -= 16f; h -= 16f;
            var custom = (a != null && a.CurrentPortrait != null) ? a.CurrentPortrait : LoadPortrait(name);
            if (custom != null)
            {
                var im = UIKit.Img("Custom", holder, Color.white); im.sprite = custom; im.preserveAspect = true; im.raycastTarget = false;
                UIKit.Place(im.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                return;
            }
            int hs = Mathf.Abs((name ?? "x").GetHashCode());
            Color skin = new Color(0.93f, 0.76f, 0.62f), hair = hairCols[hs % hairCols.Length], cloth = clothCols[(hs / 7) % clothCols.Length];
            bool female = a != null ? a.female : (hs % 2 == 0);
            int race = a != null ? a.race : 0;
            if (race == 1) skin = new Color(0.86f, 0.62f, 0.48f);
            else if (race == 2) skin = new Color(0.97f, 0.84f, 0.74f);
            else if (race == 3) skin = new Color(0.78f, 0.58f, 0.38f);
            if (name != null && (name.IndexOf("Thorin", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Elder", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Tetua", StringComparison.OrdinalIgnoreCase) >= 0)) hair = new Color(0.82f, 0.82f, 0.84f);
            var bg = UIKit.Img("BG", holder, new Color(0.14f, 0.12f, 0.18f)); bg.raycastTarget = false;
            UIKit.Place(bg.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Part(holder, Disc, new Color(0.3f, 0.24f, 0.14f, 0.7f), w * 0.5f, h * 0.55f, w * 1.1f, w * 1.1f);     // lingkaran cahaya
            float cx = w * 0.5f;
            if (female) { Part(holder, Disc, hair, cx, h * 0.5f, w * 0.62f, h * 0.55f); Part(holder, null, hair, cx - w * 0.26f, h * 0.3f, w * 0.14f, h * 0.36f); Part(holder, null, hair, cx + w * 0.26f, h * 0.3f, w * 0.14f, h * 0.36f); }
            Part(holder, Disc, cloth, cx, 0f, w * 1.0f, h * 0.55f);                                              // bahu
            Part(holder, null, skin * 0.9f, cx, h * 0.3f, w * 0.17f, h * 0.14f);                                 // leher
            Part(holder, Disc, Color.Lerp(cloth, Color.white, 0.25f), cx, h * 0.22f, w * 0.3f, h * 0.1f);        // kerah
            if (race == 3) { Part(holder, Disc, skin, cx - w * 0.17f, h * 0.76f, w * 0.14f, h * 0.14f); Part(holder, Disc, skin, cx + w * 0.17f, h * 0.76f, w * 0.14f, h * 0.14f); }
            if (race == 2) { Part(holder, Disc, skin, cx - w * 0.28f, h * 0.55f, w * 0.12f, h * 0.06f); Part(holder, Disc, skin, cx + w * 0.28f, h * 0.55f, w * 0.12f, h * 0.06f); }
            Part(holder, Disc, skin, cx, h * 0.52f, w * 0.44f, h * 0.38f);                                        // kepala
            Part(holder, Disc, hair, cx, h * 0.68f, w * 0.5f, h * 0.2f);                                         // poni
            if (!female && hs % 3 == 0) Part(holder, Disc, hair, cx, h * 0.4f, w * 0.34f, h * 0.14f);            // jenggot
            Part(holder, Disc, new Color(0.12f, 0.1f, 0.1f), cx - w * 0.09f, h * 0.53f, w * 0.05f, h * 0.045f);   // mata
            Part(holder, Disc, new Color(0.12f, 0.1f, 0.1f), cx + w * 0.09f, h * 0.53f, w * 0.05f, h * 0.045f);
            Part(holder, null, new Color(0.55f, 0.25f, 0.22f), cx, h * 0.42f, w * 0.08f, h * 0.012f);            // mulut
        }

        // ------------------------------------------------------------ input & animasi
        void Fire(UIEntry e)
        {
            if (pick != null) pick(e);
        }

        void Advance()
        {
            if (!revealed) { revealed = true; reveal = fullText.Length; if (body != null) body.text = fullText; return; }
            if (lanjut != null) { Fire(lanjut); return; }
            if (choiceEntries.Count == 0 && !hasCard) { if (close != null) close(); }
        }

        int scrollFix;
        public void Tick()
        {
            UpdateMarker();
            if (!IsOpen) return;
            if (scrollFix > 0)
            {
                scrollFix--;
                Canvas.ForceUpdateCanvases();
                if (bodyScrollRect != null) bodyScrollRect.verticalNormalizedPosition = 1f;
                if (listScroll != null) listScroll.verticalNormalizedPosition = 1f;
            }
            if (!revealed && body != null)
            {
                reveal += Time.unscaledDeltaTime * 70f;
                int n = Mathf.Min(fullText.Length, (int)reveal);
                body.text = fullText.Substring(0, n);
                if (n >= fullText.Length) revealed = true;
            }
            if (chevron != null)
            {
                chevronGroup.alpha = revealed ? 1f : 0f;
                chevron.anchoredPosition = new Vector2(chevron.anchoredPosition.x, chevron.anchoredPosition.y + Mathf.Sin(Time.unscaledTime * 6f) * 0.25f);
            }
            if (Time.frameCount <= openFrame) return;
            int nav = TownInput.NavDown();
            if (nav != 0 && choiceEntries.Count > 0)
            {
                for (int k = 0; k < choiceEntries.Count; k++)
                {
                    sel = (sel + nav + choiceEntries.Count) % choiceEntries.Count;
                    if (choiceEntries[sel].enabled) break;
                }
                ApplySel();
                if (listScroll != null && choiceEntries.Count > 1) listScroll.verticalNormalizedPosition = 1f - (float)sel / (choiceEntries.Count - 1);
            }
            int dg = TownInput.DigitPressed();
            if (dg >= 0 && dg < choiceEntries.Count && choiceEntries[dg].enabled) { Audio.Sfx("click"); sel = dg; Fire(choiceEntries[dg]); return; }
            if (TownInput.ConfirmDown())
            {
                if (!revealed) { Advance(); return; }
                if (hasCard) { Audio.Sfx("click"); Fire(acceptEntry); return; }
                if (lanjut != null) { Audio.Sfx("click"); Fire(lanjut); return; }
                if (choiceEntries.Count > 0) { if (choiceEntries[sel].enabled) { Audio.Sfx("click"); Fire(choiceEntries[sel]); } return; }
                if (close != null) close();
            }
        }

        // ------------------------------------------------------------ panel 1: penanda NPC
        NpcActor markerActor, curActor, hiddenFor; List<Renderer> hiddenLbl = new List<Renderer>();
        void HideLabel(NpcActor a)
        {
            if (a == hiddenFor) return;
            foreach (var r in hiddenLbl) if (r != null) r.enabled = true;
            hiddenLbl.Clear(); hiddenFor = a;
            if (a == null) return;
            foreach (var tm in a.GetComponentsInChildren<TextMesh>(true))
            {
                if (tm.transform.parent != null && tm.transform.parent.name == "Bubble") continue;
                var r = tm.GetComponent<Renderer>(); if (r != null && r.enabled) { r.enabled = false; hiddenLbl.Add(r); }
            }
        }

        void UpdateMarker()
        {
            SuppressPrompt = false;
            bool show = false;
            var tui = TownUI.I;
            if (tui != null && !tui.PanelOpen && Camera.main != null)
            {
                if (player == null) player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    Interactable best = null; float bd = float.MaxValue; string bn = "", br = ""; NpcActor ba = null;
                    foreach (var it in Interactable.All)
                    {
                        if (it == null) continue;
                        Vector3 d = it.transform.position - player.transform.position; d.y = 0f;
                        float dist = d.magnitude;
                        if (dist > it.radius || dist >= bd) continue;
                        string n, r; NpcActor a;
                        if (it is NpcChat) continue;      // warga biasa tanpa penanda; hanya NPC khusus
                        if (!Resolve(it, out n, out r, out a)) continue;
                        best = it; bd = dist; bn = n; br = r; ba = a;
                    }
                    if (best != null)
                    {
                        markerActor = ba; markerIt = best;
                        float hgt = ba != null ? ba.height + 1.25f : 3.0f;
                        Vector3 wp = (ba != null ? ba.transform.position : best.transform.position) + Vector3.up * hgt;
                        Vector3 sp = Camera.main.WorldToScreenPoint(wp);
                        if (sp.z > 0.1f)
                        {
                            EnsureMarker();
                            mName.text = bn; mRole.text = string.IsNullOrEmpty(br) ? "" : "<" + br + ">";
                            // v35z102n11: "!" hanya untuk NPC pemberi quest; "?" = quest siap dilapor; lainnya "..." (hanya bicara)
                            int qm = markerIt is IQuestGiver ? ((IQuestGiver)markerIt).QuestMarkState() : 0;
                            if (qm == 0 && markerActor != null && Mail.HasDeliveryFor(markerActor)) qm = 2;      // v35z102n23: "?" = kamu membawa surat untuk NPC ini
                            if (mGlyph != null)
                            {
                                mGlyph.text = qm == 1 ? "!" : qm == 2 ? "?" : "...";
                                mGlyph.fontSize = qm == 0 ? 36 : 50;
                                mGlyph.color = qm == 2 ? new Color(0.6f, 1f, 0.6f) : qm == 1 ? new Color(1f, 0.85f, 0.35f) : new Color(0.85f, 0.85f, 0.85f);
                                if (mRing != null) mRing.color = qm == 0 ? UiStyle.Classic.GoldDim : UiStyle.Classic.Gold;
                            }
                            float k = canvas != null ? canvas.scaleFactor : 1f;
                            markerRt.anchoredPosition = new Vector2(sp.x / k, sp.y / k);
                            show = true;
                        }
                    }
                }
            }
            NpcActor want = null;
            if (show) want = markerActor;
            else if (IsOpen) want = curActor;
            HideLabel(want);
            if (marker != null && marker.activeSelf != show) marker.SetActive(show);
            if (show) { SuppressPrompt = true; markerRt.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 4f)); }
        }

        void EnsureMarker()
        {
            if (marker != null) return;
            var rt = UIKit.Rect("NpcMarker", root);
            marker = rt.gameObject; markerRt = rt;
            UIKit.Place(rt, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300f, 200f));
            Vector2 top = new Vector2(0.5f, 1f);
            // penunjuk "!" berbentuk pin: cakram gelap + cincin emas + segitiga di bawah
            var back = UIKit.Img("Glow", rt, new Color(0.05f, 0.04f, 0.02f, 0.75f)); back.sprite = Disc; back.raycastTarget = false;
            UIKit.Place(back.rectTransform, top, top, top, new Vector2(0f, -2f), new Vector2(76f, 76f));
            var ptr = UIKit.Img("Pointer", rt, new Color(0.05f, 0.04f, 0.02f, 0.75f)); ptr.raycastTarget = false;
            UIKit.Place(ptr.rectTransform, top, top, top, new Vector2(0f, -66f), new Vector2(20f, 20f));
            ptr.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var ring = UIKit.Img("Ring", rt, UiStyle.Classic.Gold); ring.sprite = UiStyle.Classic.RingSprite; ring.raycastTarget = false;
            UIKit.Place(ring.rectTransform, top, top, top, new Vector2(0f, 0f), new Vector2(80f, 80f));
            var ex = UIKit.Txt("Ex", ring.transform, 50, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f)); ex.text = "!"; ex.fontStyle = FontStyle.Bold;
            mGlyph = ex; mRing = ring;
            UIKit.Place(ex.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            mName = UIKit.Txt("Name", rt, 32, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.6f));
            UIKit.Place(mName.rectTransform, top, top, top, new Vector2(0f, -84f), new Vector2(300f, 40f));
            var sh = mName.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.95f); sh.effectDistance = new Vector2(2f, -2f);
            mRole = UIKit.Txt("Role", rt, 22, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(mRole.rectTransform, top, top, top, new Vector2(0f, -122f), new Vector2(300f, 30f));
            var sh2 = mRole.gameObject.AddComponent<Shadow>(); sh2.effectColor = new Color(0, 0, 0, 0.95f); sh2.effectDistance = new Vector2(2f, -2f);
            // tombol interaksi di sisi kiri-bawah (setinggi dada NPC), dengan garis emas tipis
            var line = UIKit.Img("Line", rt, new Color(0.9f, 0.73f, 0.35f, 0.7f)); line.raycastTarget = false;
            UIKit.Place(line.rectTransform, top, top, top, new Vector2(0f, -156f), new Vector2(0f, 0f)); line.gameObject.SetActive(false);
            var chip = UiStyle.Classic.Frame("Key", rt);
            UIKit.Place(chip.rectTransform, top, top, top, new Vector2(0f, -154f), new Vector2(170f, 44f));
            var chipBg = UIKit.Img("Bg", chip.transform, new Color(0.04f, 0.04f, 0.06f, 0.96f)); chipBg.raycastTarget = false;
            GoldUI.Stretch(chipBg.rectTransform, 5f); chipBg.transform.SetAsFirstSibling();
            var cap = UIKit.Img("Cap", chip.transform, new Color(0.93f, 0.93f, 0.96f)); cap.raycastTarget = false;
            UIKit.At(cap.rectTransform, 8f, 6f, 32f, 32f);
            var capT = UIKit.Txt("C", cap.transform, 22, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.12f)); capT.text = "E"; capT.fontStyle = FontStyle.Bold;
            UIKit.Place(capT.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var kt = UIKit.Txt("KT", chip.transform, 26, TextAnchor.MiddleLeft, Color.white); kt.text = "Bicara";
            UIKit.At(kt.rectTransform, 50f, 0f, 116f, 44f);
            marker.SetActive(false);
        }
    }
}
