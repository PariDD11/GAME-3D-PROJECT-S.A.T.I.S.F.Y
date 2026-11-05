using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>Riwayat chat/log HUD. Pesan sistem (toast, loot, level, quest) masuk otomatis lewat TownUI.Toast.</summary>
    public static class HudChat
    {
        public struct Line { public string tag, msg; public int kind; }   // kind: 0 sistem, 1 dunia, 2 party, 3 guild
        public static readonly List<Line> Lines = new List<Line>();
        public static int Version;
        public static void Add(string tag, string msg, int kind = 0)
        {
            if (string.IsNullOrEmpty(msg)) return;
            if (Lines.Count > 0 && Lines[Lines.Count - 1].msg == msg && Lines[Lines.Count - 1].tag == tag) return;
            Lines.Add(new Line { tag = tag, msg = msg.Replace("\n", " "), kind = kind });
            if (Lines.Count > 60) Lines.RemoveAt(0);
            Version++;
        }
    }

    /// <summary>
    /// HUD in-game bergaya MMORPG fantasi: potret heksagon + bar HP/MP + status berlian (kiri-atas), boss bar (tengah-atas, hanya saat boss fight),
    /// lokasi + jam + minimap + quest tracking (kanan-atas), chat berlabel (kiri-bawah), bar skill 6 slot dengan slot ultimate besar di tengah + 4 item (bawah),
    /// tombol menu (kanan-bawah) dan bar EXP. Seluruh warna logam/kristal/panel mengikuti warna job.
    /// </summary>
    public class GameHud
    {
        const float K = 1.148f;   // piksel gambar acuan -> piksel kanvas 1920x1080

        // ------------------------------------------------------------------ tema per job
        struct Theme { public Color accent, trim, fill, dark, text, hi; }

        static Theme ThemeFor(string job, bool none)
        {
            Color a; float mix;
            switch (none ? "" : job)
            {
                case "Archer": a = new Color(0.30f, 0.88f, 0.38f); mix = 0.65f; break;
                case "FireMage": a = new Color(1.00f, 0.26f, 0.16f); mix = 0.7f; break;
                case "EarthMage": a = new Color(0.88f, 0.58f, 0.22f); mix = 0.6f; break;
                case "WaterMage": a = new Color(0.20f, 0.55f, 1.00f); mix = 0.65f; break;
                case "WindMage": a = new Color(0.45f, 0.95f, 0.80f); mix = 0.65f; break;
                case "Priest": a = new Color(1.00f, 0.94f, 0.62f); mix = 0.35f; break;
                default: a = new Color(0.36f, 0.66f, 1.00f); mix = 0.12f; break;   // Swordsman / belum punya job: emas + kristal biru (seperti gambar acuan)
            }
            var gold = new Color(0.92f, 0.76f, 0.44f);
            Color trim = Color.Lerp(gold, a, mix);
            float mx = Mathf.Max(trim.r, Mathf.Max(trim.g, trim.b)); if (mx < 0.9f && mx > 0.01f) trim *= 0.92f / mx;
            return new Theme
            {
                accent = a, trim = trim,
                fill = new Color(a.r * 0.13f + 0.02f, a.g * 0.13f + 0.02f, a.b * 0.13f + 0.035f, 0.90f),
                dark = new Color(a.r * 0.05f + 0.01f, a.g * 0.05f + 0.01f, a.b * 0.06f + 0.015f, 0.96f),
                text = Color.Lerp(new Color(0.95f, 0.94f, 0.9f), a, 0.10f),
                hi = Color.Lerp(trim, Color.white, 0.25f)
            };
        }

        enum R { Trim, Fill, Dark, Accent, AccentSoft, Text, Hi }
        sealed class Th { public Graphic g; public R r; public float a; }
        readonly List<Th> themed = new List<Th>();
        Theme cur, tgt; string themeKey = "?"; bool themeInit;

        void Reg(Graphic g, R r, float a = 1f) { themed.Add(new Th { g = g, r = r, a = a }); }

        void ApplyTheme()
        {
            for (int i = 0; i < themed.Count; i++)
            {
                var t = themed[i]; if (t.g == null) continue;
                Color c;
                switch (t.r)
                {
                    case R.Trim: c = cur.trim; break;
                    case R.Fill: c = cur.fill; break;
                    case R.Dark: c = cur.dark; break;
                    case R.Accent: c = cur.accent; break;
                    case R.AccentSoft: c = cur.accent; break;
                    case R.Hi: c = cur.hi; break;
                    default: c = cur.text; break;
                }
                c.a = (t.r == R.Fill || t.r == R.Dark) ? c.a * t.a : t.a;
                t.g.color = c;
            }
        }

        static Theme LerpT(Theme a, Theme b, float k)
        {
            return new Theme { accent = Color.Lerp(a.accent, b.accent, k), trim = Color.Lerp(a.trim, b.trim, k), fill = Color.Lerp(a.fill, b.fill, k), dark = Color.Lerp(a.dark, b.dark, k), text = Color.Lerp(a.text, b.text, k), hi = Color.Lerp(a.hi, b.hi, k) };
        }

        // ------------------------------------------------------------------ pembantu tata letak (piksel gambar acuan 1672x941)
        enum Cn { TL, TR, BL, BR, BC, TC }

        static void Anchor(RectTransform rt, Cn c, float x, float y, float w, float h)
        {
            Vector2 a, p; float sx = 1f, sy = 1f;
            switch (c)
            {
                case Cn.TL: a = new Vector2(0, 1); p = a; sx = 1; sy = -1; break;
                case Cn.TR: a = new Vector2(1, 1); p = a; sx = -1; sy = -1; break;
                case Cn.BL: a = new Vector2(0, 0); p = a; sx = 1; sy = 1; break;
                case Cn.BR: a = new Vector2(1, 0); p = a; sx = -1; sy = 1; break;
                case Cn.BC: a = new Vector2(0.5f, 0); p = a; sx = 1; sy = 1; break;
                default: a = new Vector2(0.5f, 1); p = a; sx = 1; sy = -1; break;
            }
            rt.anchorMin = a; rt.anchorMax = a; rt.pivot = p;
            rt.anchoredPosition = new Vector2(x * K * sx, y * K * sy);
            rt.sizeDelta = new Vector2(w * K, h * K);
        }

        /// <summary>Posisi anak dalam klaster: dari pojok kiri-atas klaster, satuan piksel gambar.</summary>
        static void L(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x * K, -y * K); rt.sizeDelta = new Vector2(w * K, h * K);
        }

        static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        static RectTransform Box(string n, Transform p) { return UIKit.Rect(n, p); }

        Image Pic(Transform p, string n, Sprite s, R r, float a = 1f, bool sliced = false)
        {
            var im = UIKit.Img(n, p, Color.white); im.sprite = s; im.raycastTarget = false;
            if (sliced) im.type = Image.Type.Sliced;
            Reg(im, r, a);
            return im;
        }

        /// <summary>Panel: isi gelap + bingkai logam (9-slice). Mengembalikan wadah (anak diletakkan di dalamnya).</summary>
        RectTransform Panel(Transform p, string name, bool ornate = true, int cut = 12)
        {
            var rt = Box(name, p);
            var f = Pic(rt, "Fill", HudArt.PanelFill(96, cut, 32), R.Fill, 1f, true); Stretch(f.rectTransform);
            var t = Pic(rt, "Trim", HudArt.PanelTrim(96, cut, 32, ornate), R.Trim, 1f, true); Stretch(t.rectTransform);
            return rt;
        }

        RectTransform Plate(Transform p, string name)
        {
            var rt = Box(name, p);
            var f = Pic(rt, "Fill", HudArt.BarFill(), R.Fill, 1f, true); Stretch(f.rectTransform);
            var t = Pic(rt, "Trim", HudArt.BarTrim(), R.Trim, 1f, true); Stretch(t.rectTransform);
            return rt;
        }

        Text Tx(Transform p, string n, int size, TextAnchor an, R role, bool bold = false)
        {
            var t = UIKit.Txt(n, p, size, an, Color.white);
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true; if (bold) t.fontStyle = FontStyle.Bold;
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.9f); sh.effectDistance = new Vector2(1.2f, -1.2f);
            Reg(t, role);
            return t;
        }

        struct BarRef { public RectTransform fill; public Text label; }

        BarRef Bar(Transform p, float x, float y, float w, float h, Color fillCol, int font)
        {
            var rt = Box("Bar", p); L(rt, x, y, w, h);
            var bg = Pic(rt, "Bg", HudArt.BarFill(), R.Dark, 1f, true); Stretch(bg.rectTransform);
            var inner = Box("Inner", rt); Stretch(inner, 3f * K);
            var fill = UIKit.Img("Fill", inner, fillCol); fill.sprite = HudArt.BarFill(); fill.type = Image.Type.Sliced; fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one; fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
            var sh = UIKit.Img("Sheen", fill.transform, new Color(1, 1, 1, 0.22f)); sh.sprite = UiStyle.SheenSprite; sh.raycastTarget = false; Stretch(sh.rectTransform);
            var tr = Pic(rt, "Trim", HudArt.BarTrim(), R.Trim, 1f, true); Stretch(tr.rectTransform);
            var lab = Tx(rt, "Label", font, TextAnchor.MiddleCenter, R.Text, true); Stretch(lab.rectTransform);
            return new BarRef { fill = fill.rectTransform, label = lab };
        }

        // ------------------------------------------------------------------ state HUD
        readonly Transform root;
        RectTransform tl, tr, bl, bc, br, boss, expRow;
        CanvasGroup cgTL, cgTR, cgBL, cgBC, cgBR, cgBoss;

        // kiri-atas
        Text lvName, portraitInit, badgeLv;
        Image badgeGlyph, portraitGlyph, portraitHi, faceArt; GameObject faceRoot; float faceAt;
        Text[] mapLetters; Image gradTop, gradBot;
        // kompas tengah-atas
        RectTransform compass, cmpMask; Text cmpHeading, cmpInfo;
        readonly Image[] cmpTick = new Image[24]; readonly Text[] cmpLabel = new Text[8]; readonly Image[] cmpMark = new Image[8];
        static readonly string[] Dir8 = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        BarRef hp, mp; RectTransform staStrip, staFill;
        readonly GameObject[] sideBtn = new GameObject[3];
        readonly Image[] dmInner = new Image[5]; readonly Image[] dmGlow = new Image[5]; readonly Text[] dmTxt = new Text[5];
        // boss
        BarRef bossHp; Text bossName, bossPct; float bossShown, bossFill = 1f, lastBossSeen = -99f; string bossLabel = "";
        // kanan-atas
        Text zoneName, zoneSub, clockTxt, goldTxt; Image weatherDot;
        float questH = 120f;
        RectTransform questPanel, questList, questBody; Text questHdr, questChevron; bool questCollapsed; readonly List<GameObject> questRows = new List<GameObject>();
        string questKey = "";
        // kiri-bawah
        Text chatText; readonly Text[] chatTab = new Text[4]; readonly Image[] chatTabLine = new Image[4]; int chatTabSel; int chatVersionDrawn = -1, chatTabDrawn = -1;
        // bawah-tengah
        sealed class Slot { public Image icon, cd, glow, frame; public Text cdTxt, key, count; public RectTransform rt; }
        readonly Slot[] skillSlot = new Slot[7]; readonly Slot[] itemSlot = new Slot[6];
        // exp
        BarRef exp;

        static readonly string[] SkillKeys = { "Z", "X", "C", "V", "R", "T", "Q" };
        // urutan tampilan: kiri = slot 0,1,2 ; tengah besar = slot 3 ; kanan = slot 4,5
        static readonly int[] SkillOrder = { 0, 1, 2, 3, 4, 5 };

        static GameHud instance;
        HudArt.Glyph lastBadge = (HudArt.Glyph)(-1), lastWeather = (HudArt.Glyph)(-1);

        /// <summary>Dipanggil Minimap saat membangun bingkainya: pasang gaya panel logam + penanda N + tombol zoom.</summary>
        public static void StyleMapFrame(Image im)
        {
            var h = instance; if (h == null) return;
            im.sprite = HudArt.DiscFill(); im.type = Image.Type.Simple; h.Reg(im, R.Dark);
            var tr = h.Pic(im.transform, "Trim", HudArt.BigRing(), R.Trim, 1f); Stretch(tr.rectTransform);
            h.mapLetters = new Text[4]; string[] ls = { "N", "E", "S", "W" };
            for (int i = 0; i < 4; i++)
            {
                var n = h.Tx(im.transform, "L" + ls[i], i == 0 ? 20 : 16, TextAnchor.MiddleCenter, i == 0 ? R.Accent : R.Hi, true);
                n.rectTransform.anchorMin = n.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); n.rectTransform.pivot = new Vector2(0.5f, 0.5f); n.rectTransform.sizeDelta = new Vector2(28f, 24f); n.text = ls[i];
                h.mapLetters[i] = n;
            }
            for (int i = 0; i < 2; i++)
            {
                bool plus = i == 0;
                var b = UIKit.Rect(plus ? "Zin" : "Zout", im.transform);
                b.anchorMin = b.anchorMax = plus ? new Vector2(0.883f, 0.179f) : new Vector2(0.711f, 0.047f); b.pivot = new Vector2(0.5f, 0.5f); b.anchoredPosition = Vector2.zero; b.sizeDelta = new Vector2(36f, 36f);
                var f = h.Pic(b, "F", HudArt.HexFill(), R.Dark, 1f); Stretch(f.rectTransform);
                var r = h.Pic(b, "R", HudArt.HexRing(), R.Trim); Stretch(r.rectTransform);
                var t = h.Tx(b, "T", 24, TextAnchor.MiddleCenter, R.Hi, true); Stretch(t.rectTransform); t.text = plus ? "+" : "-";
                var btn = b.gameObject.AddComponent<Button>(); var hit = UIKit.Img("Hit", b, new Color(0, 0, 0, 0)); Stretch(hit.rectTransform); btn.targetGraphic = hit; btn.transition = Selectable.Transition.None;
                bool big = plus; btn.onClick.AddListener(() => { Audio.Sfx("click"); Minimap.SetBig(big); });
            }
            h.ApplyTheme();
        }

        public GameHud(Transform root)
        {
            instance = this;
            this.root = root;
            // gradien tipis atas/bawah agar HUD menyatu dan terbaca di atas langit/tanah terang
            BuildCompass(); BuildTopLeft(); BuildBoss(); BuildTopRight(); BuildChat(); BuildBottom(); BuildMenu(); BuildExp();
            cur = tgt = ThemeFor("", true);
            ApplyTheme();
        }

        CanvasGroup Group(RectTransform rt) { var g = rt.gameObject.AddComponent<CanvasGroup>(); g.interactable = false; g.blocksRaycasts = false; return g; }

        /// <summary>Permata kecil di tepi panel (xFrac 0..1, yFrac 0 = tepi atas, 1 = tepi bawah).</summary>
        void Gem(Transform parent, float xFrac, float yFrac, float size)
        {
            var g = Box("Gem", parent); g.anchorMin = g.anchorMax = new Vector2(xFrac, 1f - yFrac); g.pivot = new Vector2(0.5f, 0.5f); g.anchoredPosition = Vector2.zero; g.sizeDelta = new Vector2(size, size);
            var f = Pic(g, "F", HudArt.DiamondFill(), R.Dark, 1f); Stretch(f.rectTransform);
            var c = Pic(g, "C", HudArt.DiamondFill(), R.Accent, 0.9f); Stretch(c.rectTransform, size * 0.28f);
            var r2 = Pic(g, "R", HudArt.DiamondRing(), R.Trim); Stretch(r2.rectTransform);
        }

        static HudArt.Glyph WeatherGlyph(string wx, bool night)
        {
            string w = (wx ?? "").ToLowerInvariant();
            if (w.Contains("hujan") || w.Contains("badai") || w.Contains("petir")) return HudArt.Glyph.Drop;
            if (w.Contains("salju") || w.Contains("es")) return HudArt.Glyph.Star;
            if (w.Contains("berawan") || w.Contains("kabut") || w.Contains("mendung")) return HudArt.Glyph.Cloud;
            return night ? HudArt.Glyph.Moon : HudArt.Glyph.Sun;
        }

        // ------------------------------------------------------------------ kompas horizontal (tengah-atas)
        const float CmpHalf = 190f;       // setengah lebar jendela kompas (px gambar)
        const float CmpSpan = 80f;        // derajat yang terlihat ke kiri/kanan

        void BuildCompass()
        {
            compass = Box("HudCompass", root); Anchor(compass, Cn.TC, 0, 8, 420, 58);
            var g = Group(compass); cgCompass = g;
            var pn = Panel(compass, "Strip", false, 8); L(pn, 0, 0, 420, 32);
            cmpMask = Box("Mask", pn); L(cmpMask, 12, 0, 396, 32); cmpMask.gameObject.AddComponent<RectMask2D>();
            for (int i = 0; i < 24; i++)
            {
                var t = UIKit.Img("t" + i, cmpMask, Color.white); t.raycastTarget = false; t.sprite = HudArt.White; cmpTick[i] = t;
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            for (int i = 0; i < 8; i++)
            {
                var tx = Tx(cmpMask, "d" + i, 17, TextAnchor.MiddleCenter, R.Hi, true); tx.text = Dir8[i]; cmpLabel[i] = tx;
                tx.rectTransform.anchorMin = tx.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); tx.rectTransform.pivot = new Vector2(0.5f, 0.5f); tx.rectTransform.sizeDelta = new Vector2(44f, 30f);
                if (i % 2 == 1) tx.fontSize = 12;
            }
            for (int i = 0; i < 8; i++)
            {
                var m = UIKit.Img("m" + i, cmpMask, Color.white); m.sprite = HudArt.DiamondFill(); m.raycastTarget = false; cmpMark[i] = m;
                m.rectTransform.anchorMin = m.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); m.rectTransform.pivot = new Vector2(0.5f, 0.5f); m.rectTransform.sizeDelta = new Vector2(11f, 11f);
            }
            // penunjuk tengah
            var ptr = Box("Ptr", pn); L(ptr, 210f - 8f, 26, 16, 12);
            var pf = Pic(ptr, "F", HudArt.DiamondFill(), R.Accent); Stretch(pf.rectTransform);
            var pr = Pic(ptr, "R", HudArt.DiamondRing(), R.Trim); Stretch(pr.rectTransform);
            cmpHeading = Tx(compass, "Hd", 13, TextAnchor.MiddleCenter, R.Hi, true); L(cmpHeading.rectTransform, 110, 36, 200, 20);
            cmpInfo = Tx(compass, "Info", 13, TextAnchor.MiddleCenter, R.Text); L(cmpInfo.rectTransform, 40, 36, 340, 20); cmpInfo.text = "";
        }
        CanvasGroup cgCompass;

        void TickCompass()
        {
            var cam = Camera.main; var pc = PlayerCombat.I;
            float yaw = cam != null ? cam.transform.eulerAngles.y : (pc != null ? pc.transform.eulerAngles.y : 0f);
            float half = CmpHalf * K;
            // garis skala tiap 15 derajat
            for (int i = 0; i < 24; i++)
            {
                float d = i * 15f, delta = Mathf.DeltaAngle(yaw, d), a = 1f - Mathf.Pow(Mathf.Abs(delta) / CmpSpan, 2.2f);
                var t = cmpTick[i]; bool on = a > 0.02f; t.enabled = on; if (!on) continue;
                bool major = i % 3 == 0;
                t.rectTransform.anchoredPosition = new Vector2(delta / CmpSpan * half, major ? -9f : -10f);
                t.rectTransform.sizeDelta = new Vector2(major ? 2f : 1.4f, major ? 9f : 5f);
                var c = cur.trim; c.a = a * (major ? 0.95f : 0.6f); t.color = c;
            }
            for (int i = 0; i < 8; i++)
            {
                float delta = Mathf.DeltaAngle(yaw, i * 45f), a = 1f - Mathf.Pow(Mathf.Abs(delta) / CmpSpan, 2.2f);
                var t = cmpLabel[i]; bool on = a > 0.05f; t.enabled = on; if (!on) continue;
                t.rectTransform.anchoredPosition = new Vector2(delta / CmpSpan * half, 3f);
                var c = i == 0 ? cur.accent : cur.hi; c.a = a; t.color = c;
            }
            // penanda target / camp / gerbang / boss
            int mi = 0; string near = ""; float nd = 1e9f;
            if (Minimap.Showing)
                foreach (var m in Minimap.Markers)
                {
                    if (mi >= cmpMark.Length) break;
                    Vector3 to = m.pos - Minimap.PlayerPos; to.y = 0f;
                    float ang = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, delta = Mathf.DeltaAngle(yaw, ang);
                    float x = Mathf.Clamp(delta / CmpSpan, -1f, 1f) * (half - 10f);
                    var im = cmpMark[mi++]; im.enabled = true; im.rectTransform.anchoredPosition = new Vector2(x, -9f);
                    var c = m.col; c.a = Mathf.Abs(delta) > CmpSpan ? 0.55f : 1f; im.color = c;
                    float dist = to.magnitude; if (Mathf.Abs(delta) < 30f && dist < nd) { nd = dist; near = m.label + "  " + Mathf.RoundToInt(dist) + " m"; }
                }
            for (; mi < cmpMark.Length; mi++) cmpMark[mi].enabled = false;
            int deg = Mathf.RoundToInt(Mathf.Repeat(yaw, 360f)) % 360;
            cmpHeading.text = Dir8[Mathf.RoundToInt(deg / 45f) % 8] + "  " + deg + "\u00b0";
            cmpInfo.text = near;
            if (near != "") { cmpHeading.rectTransform.anchoredPosition = new Vector2(-240f * K, cmpHeading.rectTransform.anchoredPosition.y); } else { cmpHeading.rectTransform.anchoredPosition = new Vector2(110f * K, cmpHeading.rectTransform.anchoredPosition.y); }
        }

        // ------------------------------------------------------------------ kiri-atas
        void BuildTopLeft()
        {
            tl = Box("HudTL", root); Anchor(tl, Cn.TL, 8, 8, 440, 150); cgTL = Group(tl);
            // pelat nama (ujung kanan runcing lewat sprite bar)
            var plate = Plate(tl, "NamePlate"); L(plate, 96, 10, 330, 38);
            lvName = Tx(plate, "LvName", 23, TextAnchor.MiddleLeft, R.Text, true); Stretch(lvName.rectTransform); lvName.rectTransform.offsetMin = new Vector2(30f * K, 0f);
            hp = Bar(tl, 96, 52, 310, 22, new Color(0.86f, 0.12f, 0.14f), 14); hp.fill.GetComponent<Image>().color = new Color(0.86f, 0.12f, 0.14f);
            mp = Bar(tl, 96, 78, 290, 17, new Color(0.14f, 0.5f, 0.98f), 11);
            // strip stamina tipis di bawah MP (kuning-hijau)
            var sb = Box("Sta", tl); L(sb, 96, 99, 270, 4);
            var sbg = UIKit.Img("Bg", sb, new Color(0.05f, 0.06f, 0.05f, 0.9f)); sbg.raycastTarget = false; Stretch(sbg.rectTransform);
            var sf = UIKit.Img("Fill", sb, new Color(0.42f, 0.88f, 0.34f, 1f)); sf.raycastTarget = false;
            sf.rectTransform.anchorMin = Vector2.zero; sf.rectTransform.anchorMax = Vector2.one; sf.rectTransform.offsetMin = Vector2.zero; sf.rectTransform.offsetMax = Vector2.zero;
            staStrip = sb; staFill = sf.rectTransform;
            // status berlian
            string[] ds = { "L", "H", "B", "Z", "U" };
            for (int i = 0; i < 5; i++)
            {
                var d = Box("Dm" + i, tl); L(d, 94 + i * 36, 108, 34, 34);
                var bg = Pic(d, "Bg", HudArt.DiamondFill(), R.Dark, 1f); Stretch(bg.rectTransform);
                var gl = Pic(d, "Glow", HudArt.Glow(), R.Accent, 0f); Stretch(gl.rectTransform, -8f); dmGlow[i] = gl;
                var inn = UIKit.Img("Inner", d, Color.white); inn.sprite = HudArt.DiamondFill(); inn.type = Image.Type.Filled; inn.fillMethod = Image.FillMethod.Vertical; inn.fillOrigin = 0; inn.raycastTarget = false; Stretch(inn.rectTransform, 4f); dmInner[i] = inn;
                var rg = Pic(d, "Ring", HudArt.DiamondRing(), R.Trim); Stretch(rg.rectTransform);
                var tx = Tx(d, "T", 11, TextAnchor.MiddleCenter, R.Hi, true); Stretch(tx.rectTransform); tx.text = ds[i]; dmTxt[i] = tx;
            }
            // potret heksagon
            var hx = Box("Portrait", tl); L(hx, 0, 0, 110, 110);
            var hf = Pic(hx, "Fill", HudArt.HexFill(), R.Dark, 1f); Stretch(hf.rectTransform);
            var hgl = Pic(hx, "Glow", HudArt.Glow(), R.Accent, 0.25f); Stretch(hgl.rectTransform, 6f);
            var pg = UIKit.Img("Glyph", hx, Color.white); pg.sprite = HudArt.GlyphSprite(HudArt.Glyph.Shield); pg.raycastTarget = false; Stretch(pg.rectTransform, 26f); portraitGlyph = pg; Reg(pg, R.Accent);
            var pgs = UIKit.Img("GlyphHi", hx, new Color(1, 1, 1, 0.16f)); pgs.sprite = HudArt.Glow(); pgs.raycastTarget = false; Stretch(pgs.rectTransform, 30f); portraitHi = pgs;
            // potret dari plugin (Hunter Guild > Potret HUD): ditampilkan di dalam hex bila ada sprite untuk job/evolusi ini
            var face = UIKit.Img("Face", hx, Color.white); face.sprite = HudArt.HexFill(); face.raycastTarget = false; Stretch(face.rectTransform, 5f);
            var fmask = face.gameObject.AddComponent<Mask>(); fmask.showMaskGraphic = false;
            var art = UIKit.Img("Art", face.transform, Color.white); art.raycastTarget = false; art.preserveAspect = true;
            art.rectTransform.anchorMin = art.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); art.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            faceRoot = face.gameObject; faceArt = art; faceRoot.SetActive(false);
            var hr = Pic(hx, "Ring", HudArt.HexRing(), R.Trim); Stretch(hr.rectTransform);
            // lencana job (kiri-bawah potret)
            var bd = Box("Badge", tl); L(bd, 2, 74, 42, 42);
            var bf = Pic(bd, "Fill", HudArt.HexFill(), R.Dark, 1f); Stretch(bf.rectTransform);
            badgeLv = Tx(bd, "Lv", 16, TextAnchor.MiddleCenter, R.Hi, true); Stretch(badgeLv.rectTransform);
            var br2 = Pic(bd, "Ring", HudArt.HexRing(), R.Trim); Stretch(br2.rectTransform);
        }

        // ------------------------------------------------------------------ boss bar (tengah-atas, hanya saat boss fight)
        void BuildBoss()
        {
            boss = Box("HudBoss", root); Anchor(boss, Cn.TC, 91, 80, 640, 150); cgBoss = Group(boss); cgBoss.alpha = 0f;
            var plate = Plate(boss, "NamePlate"); L(plate, 112, 24, 330, 46);
            bossName = Tx(plate, "Name", 27, TextAnchor.MiddleLeft, R.Text, true); Stretch(bossName.rectTransform); bossName.rectTransform.offsetMin = new Vector2(26f * K, 0f);
            bossHp = Bar(boss, 112, 66, 470, 40, new Color(0.88f, 0.1f, 0.12f), 18);
            var tag = Panel(boss, "Pct", false, 10); L(tag, 548, 62, 92, 46);
            bossPct = Tx(tag, "T", 26, TextAnchor.MiddleCenter, R.Text, true); Stretch(bossPct.rectTransform);
            var hx = Box("Portrait", boss); L(hx, 0, 2, 132, 132);
            var hf = Pic(hx, "Fill", HudArt.HexFill(), R.Dark, 1f); Stretch(hf.rectTransform);
            var gl = Pic(hx, "Glow", HudArt.Glow(), R.Accent, 0.35f); Stretch(gl.rectTransform, 6f);
            var sk = UIKit.Img("Skull", hx, Color.white); sk.sprite = HudArt.GlyphSprite(HudArt.Glyph.Skull); sk.raycastTarget = false; Stretch(sk.rectTransform, 30f); Reg(sk, R.Accent);
            var hr = Pic(hx, "Ring", HudArt.HexRing(), R.Trim); Stretch(hr.rectTransform);
        }

        // ------------------------------------------------------------------ kanan-atas
        void BuildTopRight()
        {
            tr = Box("HudTR", root); Anchor(tr, Cn.TR, 8, 8, 300, 600); cgTR = Group(tr);
            var zone = Panel(tr, "Zone", false, 8); L(zone, 0, 0, 300, 36);
            var ic = Pic(zone, "Wx", HudArt.GlyphSprite(HudArt.Glyph.Star), R.Accent); L(ic.rectTransform, 188, 6, 24, 24); weatherDot = ic;
            zoneName = Tx(zone, "Zone", 18, TextAnchor.MiddleLeft, R.Text, true); L(zoneName.rectTransform, 14, 0, 172, 36);
            clockTxt = Tx(zone, "Clock", 19, TextAnchor.MiddleRight, R.Text, true); L(clockTxt.rectTransform, 216, 0, 76, 36);
            zoneSub = Tx(tr, "Sub", 12, TextAnchor.MiddleRight, R.Hi); L(zoneSub.rectTransform, 0, 38, 300, 16);

            // kolom tombol kiri minimap: Peta / Quest / Target
            string[] nm = { "Peta", "Quest", "Target" }; HudArt.Glyph[] gl = { HudArt.Glyph.Book, HudArt.Glyph.Scroll, HudArt.Glyph.Skull };
            for (int i = 0; i < 3; i++)
            {
                int id = i;
                var b = Box("Side" + i, tr); L(b, 0, 60 + i * 46, 42, 42); sideBtn[i] = b.gameObject;
                var bf = Pic(b, "Fill", HudArt.HexFill(), R.Dark, 1f); Stretch(bf.rectTransform);
                var g = UIKit.Img("G", b, Color.white); g.sprite = HudArt.GlyphSprite(gl[i]); g.raycastTarget = false; Stretch(g.rectTransform, 11f * K); Reg(g, R.Accent);
                var rg = Pic(b, "Ring", HudArt.HexRing(), R.Trim); Stretch(rg.rectTransform);
                var btn = b.gameObject.AddComponent<Button>(); var hit = UIKit.Img("Hit", b, new Color(0, 0, 0, 0)); Stretch(hit.rectTransform); btn.targetGraphic = hit; btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { Audio.Sfx("click"); if (id == 0) Minimap.ToggleSize(); else if (id == 1) PlayerController.OpenWindow(3); else HudInfoUI.OpenTarget(); });
            }

            // panel quest tracking
            questPanel = Panel(tr, "Quest", false, 9); L(questPanel, 0, 268, 300, 120);
            var hdr = Box("Hdr", questPanel); L(hdr, 0, 0, 300, 38);
            var qi = UIKit.Img("Ico", hdr, Color.white); qi.sprite = HudArt.GlyphSprite(HudArt.Glyph.Scroll); qi.raycastTarget = false; L(qi.rectTransform, 10, 7, 24, 24); Reg(qi, R.Accent);
            questHdr = Tx(hdr, "T", 17, TextAnchor.MiddleLeft, R.Text, true); L(questHdr.rectTransform, 42, 0, 200, 38); questHdr.text = "Quest Tracking";
            var cb = Box("Chev", hdr); L(cb, 262, 7, 28, 24);
            var cbf = Pic(cb, "F", HudArt.DiamondFill(), R.Dark, 1f); Stretch(cbf.rectTransform);
            questChevron = Tx(cb, "T", 16, TextAnchor.MiddleCenter, R.Hi, true); Stretch(questChevron.rectTransform); questChevron.text = "^";
            var cbtn = cb.gameObject.AddComponent<Button>(); var chit = UIKit.Img("Hit", cb, new Color(0, 0, 0, 0)); Stretch(chit.rectTransform); cbtn.targetGraphic = chit; cbtn.transition = Selectable.Transition.None;
            cbtn.onClick.AddListener(() => { questCollapsed = !questCollapsed; questKey = ""; });
            var dv = UIKit.Img("Line", questPanel, Color.white); dv.raycastTarget = false; L(dv.rectTransform, 12, 38, 276, 1.5f); Reg(dv, R.Trim, 0.5f);
            questBody = Box("Body", questPanel); L(questBody, 0, 42, 300, 180);
            // tombol di kolom kiri harus bisa diklik: aktifkan raycast pada grup tombol saja
            cgTR.blocksRaycasts = true;
        }

        // ------------------------------------------------------------------ kiri-bawah: chat
        void BuildChat()
        {
            bl = Box("HudBL", root); Anchor(bl, Cn.BL, 8, 22, 340, 124); cgBL = Group(bl);
            var pn = Panel(bl, "Chat", false, 9); L(pn, 0, 0, 340, 124);
            string[] tabs = { "Umum", "Sistem" };
            for (int i = 0; i < 2; i++)
            {
                int id = i;
                var tb = Box("Tab" + i, pn); L(tb, 8 + i * 92, 4, 90, 26);
                var line = UIKit.Img("Sel", tb, Color.white); line.raycastTarget = false; L(line.rectTransform, 8, 23, 74, 2); chatTabLine[i] = line; Reg(line, R.Accent);
                var t = Tx(tb, "T", 14, TextAnchor.MiddleCenter, R.Text); Stretch(t.rectTransform); t.text = tabs[i]; chatTab[i] = t;
                var btn = tb.gameObject.AddComponent<Button>(); var hit = UIKit.Img("Hit", tb, new Color(0, 0, 0, 0)); Stretch(hit.rectTransform); btn.targetGraphic = hit; btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { chatTabSel = id; Audio.Sfx("click"); });
            }
            var dv = UIKit.Img("Line", pn, Color.white); dv.raycastTarget = false; L(dv.rectTransform, 8, 31, 324, 1.2f); Reg(dv, R.Trim, 0.5f);
            chatText = Tx(pn, "Log", 13, TextAnchor.LowerLeft, R.Text); L(chatText.rectTransform, 12, 34, 318, 84);
            chatText.horizontalOverflow = HorizontalWrapMode.Wrap; chatText.verticalOverflow = VerticalWrapMode.Truncate; chatText.lineSpacing = 1.12f;
        }

        // ------------------------------------------------------------------ bawah-tengah: bar skill
        void BuildBottom()
        {
            bc = Box("HudBC", root); Anchor(bc, Cn.BC, 20, 24, 860, 215); cgBC = Group(bc); bc.localScale = new Vector3(0.82f, 0.82f, 1f);
            var body = Panel(bc, "Body", false, 12); L(body, 62, 70, 736, 128);
            // 3 skill kiri, 1 besar tengah (V = ultimate), 3 skill kanan
            float[] cx = { 157, 238, 317, 430, 543, 622, 703 };
            for (int i = 0; i < 7; i++)
            {
                if (i == 3) { skillSlot[i] = BigSlot(bc, cx[i], 114, i); continue; }
                skillSlot[i] = SmallSlot(bc, cx[i], 122, 66, SkillKeys[i], true);
            }
            // 6 slot item kecil tepat di atas bar skill
            for (int i = 0; i < 6; i++) itemSlot[i] = SmallSlot(bc, 430f + (i - 2.5f) * 42f, 27f, 36f, (i + 1).ToString(), false, true);
        }

        Slot SmallSlot(Transform p, float cx, float cy, float sz, string key, bool skill, bool mini = false)
        {
            var s = new Slot();
            var rt = Box("Slot" + key, p); L(rt, cx - sz * 0.5f, cy - sz * 0.5f, sz, sz); s.rt = rt;
            var f = Pic(rt, "Fill", HudArt.PanelFill(64, 8, 20), R.Dark, 1f, true); Stretch(f.rectTransform); s.frame = f;
            var ic = UIKit.Img("Icon", rt, Color.white); ic.raycastTarget = false; Stretch(ic.rectTransform, 5f * K); ic.enabled = false; s.icon = ic;
            var cd = UIKit.Img("Cd", rt, new Color(0, 0, 0, 0.62f)); cd.sprite = HudArt.White; cd.type = Image.Type.Filled; cd.fillMethod = Image.FillMethod.Radial360; cd.fillOrigin = 2; cd.fillClockwise = false; cd.fillAmount = 0f; cd.raycastTarget = false; Stretch(cd.rectTransform, 3f); s.cd = cd;
            var tr2 = Pic(rt, "Trim", HudArt.PanelTrim(64, 8, 20, false), R.Trim, 1f, true); Stretch(tr2.rectTransform);
            s.cdTxt = Tx(rt, "Cd", 18, TextAnchor.MiddleCenter, R.Text, true); Stretch(s.cdTxt.rectTransform);
            s.count = Tx(rt, "N", 15, TextAnchor.LowerRight, R.Text, true); Stretch(s.count.rectTransform); s.count.rectTransform.offsetMax = new Vector2(-3f, 0f); s.count.rectTransform.offsetMin = new Vector2(0f, 2f);
            if (mini)
            {
                s.key = Tx(rt, "K", 10, TextAnchor.UpperLeft, R.Hi, true); Stretch(s.key.rectTransform); s.key.rectTransform.offsetMin = new Vector2(3f, 0f); s.key.rectTransform.offsetMax = new Vector2(0f, -2f); s.key.text = key;
                s.count.fontSize = 11;
                return s;
            }
            var kb = Panel(p, "Key" + key, false, 5); L(kb, cx - 12, cy + sz * 0.5f - 2f, 24, 22);
            s.key = Tx(kb, "K", 14, TextAnchor.MiddleCenter, R.Hi, true); Stretch(s.key.rectTransform); s.key.text = key;
            return s;
        }

        Slot BigSlot(Transform p, float cx, float cy, int idx)
        {
            var s = new Slot(); float sz = 136f;
            var rt = Box("SlotUlt", p); L(rt, cx - sz * 0.5f, cy - sz * 0.5f, sz, sz); s.rt = rt;
            var gl = Pic(rt, "Glow", HudArt.Glow(), R.Accent, 0.0f); Stretch(gl.rectTransform, -22f); s.glow = gl;
            var f = Pic(rt, "Fill", HudArt.DiscFill(), R.Dark, 1f); Stretch(f.rectTransform); s.frame = f;
            var ic = UIKit.Img("Icon", rt, Color.white); ic.raycastTarget = false; Stretch(ic.rectTransform, 22f * K); ic.enabled = false; s.icon = ic;
            var cd = UIKit.Img("Cd", rt, new Color(0, 0, 0, 0.62f)); cd.sprite = HudArt.White; cd.type = Image.Type.Filled; cd.fillMethod = Image.FillMethod.Radial360; cd.fillOrigin = 2; cd.fillClockwise = false; cd.fillAmount = 0f; cd.raycastTarget = false; Stretch(cd.rectTransform, 14f * K); s.cd = cd;
            var rg = Pic(rt, "Ring", HudArt.BigRing(), R.Trim); Stretch(rg.rectTransform);
            s.cdTxt = Tx(rt, "Cd", 28, TextAnchor.MiddleCenter, R.Text, true); Stretch(s.cdTxt.rectTransform);
            var kb = Panel(p, "KeyUlt", false, 5); L(kb, cx - 14, cy + sz * 0.5f - 12f, 28, 24);
            s.key = Tx(kb, "K", 16, TextAnchor.MiddleCenter, R.Hi, true); Stretch(s.key.rectTransform); s.key.text = SkillKeys[idx];
            return s;
        }

        // ------------------------------------------------------------------ kanan-bawah: tombol menu + emas
        void BuildMenu()
        {
            br = Box("HudBR", root); Anchor(br, Cn.BR, 8, 22, 330, 104); cgBR = Group(br); cgBR.blocksRaycasts = true;
            string[] nm = { "Profil", "Skill", "Tas", "Quest", "Bestiari", "Sistem" }, key = { "H", "K", "Tab", "L", "B", "Esc" };
            HudArt.Glyph[] gl = { HudArt.Glyph.Person, HudArt.Glyph.Star, HudArt.Glyph.Bag, HudArt.Glyph.Scroll, HudArt.Glyph.Book, HudArt.Glyph.Gear };
            for (int i = 0; i < 6; i++)
            {
                int id = i;
                var b = Box("M" + i, br); L(b, 2 + i * 54, 34, 52, 66);
                var bf = Pic(b, "Fill", HudArt.DiamondFill(), R.Dark, 1f); L(bf.rectTransform, 4, 0, 44, 44);
                var g = UIKit.Img("G", b, Color.white); g.sprite = HudArt.GlyphSprite(gl[i]); g.raycastTarget = false; L(g.rectTransform, 13, 9, 26, 26); Reg(g, R.Accent);
                var rg = Pic(b, "Ring", HudArt.DiamondRing(), R.Trim); L(rg.rectTransform, 4, 0, 44, 44);
                var kk = Tx(b, "K", 12, TextAnchor.MiddleCenter, R.Hi, true); L(kk.rectTransform, 0, 46, 52, 16); kk.text = key[i];
                var btn = b.gameObject.AddComponent<Button>(); var hit = UIKit.Img("Hit", b, new Color(0, 0, 0, 0)); Stretch(hit.rectTransform); btn.targetGraphic = hit; btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { Audio.Sfx("click"); if (id == 5) MainMenu.OpenPause(); else PlayerController.OpenWindow(id == 0 ? 0 : id == 1 ? 1 : id == 2 ? 2 : id == 3 ? 3 : 4); });
            }
            var gp = Panel(br, "Gold", false, 7); L(gp, 2, 0, 322, 28);
            goldTxt = Tx(gp, "T", 14, TextAnchor.MiddleCenter, R.Text, true); Stretch(goldTxt.rectTransform);
            var gbtn = gp.gameObject.AddComponent<Button>(); var ghit = UIKit.Img("Hit", gp, new Color(0, 0, 0, 0)); Stretch(ghit.rectTransform); gbtn.targetGraphic = ghit; gbtn.transition = Selectable.Transition.None;
            gbtn.onClick.AddListener(() => { Audio.Sfx("click"); HudInfoUI.OpenGold(); });
        }

        // ------------------------------------------------------------------ bar EXP
        void BuildExp()
        {
            expRow = Box("HudExp", root);
            expRow.anchorMin = new Vector2(0, 0); expRow.anchorMax = new Vector2(1, 0); expRow.pivot = new Vector2(0, 0);
            expRow.offsetMin = new Vector2(10f * K, 4f * K); expRow.offsetMax = new Vector2(-348f * K, 18f * K);
            var b = Box("B", expRow); b.anchorMin = new Vector2(0, 0); b.anchorMax = new Vector2(1, 1); b.offsetMin = new Vector2(0, 0); b.offsetMax = new Vector2(0, 0);
            var bg = Pic(b, "Bg", HudArt.BarFill(), R.Dark, 1f, true); Stretch(bg.rectTransform);
            var inner = Box("In", b); Stretch(inner, 3f);
            var fill = UIKit.Img("Fill", inner, new Color(0.98f, 0.76f, 0.2f)); fill.sprite = HudArt.BarFill(); fill.type = Image.Type.Sliced; fill.raycastTarget = false;
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one; fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
            var sh = UIKit.Img("Sheen", fill.transform, new Color(1, 1, 1, 0.25f)); sh.sprite = UiStyle.SheenSprite; sh.raycastTarget = false; Stretch(sh.rectTransform);
            var tr2 = Pic(b, "Trim", HudArt.BarTrim(), R.Trim, 1f, true); Stretch(tr2.rectTransform);
            var lab = Tx(expRow, "Label", 11, TextAnchor.MiddleLeft, R.Text, true); L(lab.rectTransform, 12, 0, 400, 14);
            exp = new BarRef { fill = fill.rectTransform, label = lab };
        }

        // ------------------------------------------------------------------ nama klaster untuk fade HUD
        public CanvasGroup[] FadeGroups() { return new[] { cgTL, cgTR, cgBL, cgBC, cgBR, cgCompass }; }

        // ------------------------------------------------------------------ per-frame
        public void Tick(GameState gs)
        {
            float dt = Time.unscaledDeltaTime;
            UpdateTheme(gs, dt);
            var pc = PlayerCombat.I; var cd = CharacterData.Current;
            bool noJob = gs.NoJob || (gs.requireJobChoice && !gs.jobChosen);
            string nm = string.IsNullOrEmpty(cd.name) ? "Hunter" : cd.name;
            lvName.text = "Lv. " + gs.level + "   " + (noJob ? nm : JobInfo.Display(gs)) + (gs.IsFatigued ? "  <color=#FF8866>[LELAH]</color>" : "");
            badgeLv.text = gs.level.ToString();
            var bgl = noJob ? HudArt.Glyph.Shield : HudArt.JobGlyph(gs.job);
            if (bgl != lastBadge) { lastBadge = bgl; portraitGlyph.sprite = HudArt.GlyphSprite(bgl); }
            if (Time.unscaledTime >= faceAt)
            {
                faceAt = Time.unscaledTime + 0.5f;
                Sprite fs; Vector2 fo; float fsc;
                bool hasFace = HudPortraitConfig.Resolve(gs, CharacterData.Exists && CharacterData.Current.female, out fs, out fo, out fsc);
                faceRoot.SetActive(hasFace); portraitGlyph.enabled = !hasFace; portraitHi.enabled = !hasFace;
                if (hasFace)
                {
                    faceArt.sprite = fs;
                    float side = 100f * K * fsc; faceArt.rectTransform.sizeDelta = new Vector2(side, side); faceArt.rectTransform.anchoredPosition = fo * K;
                }
            }

            if (pc != null)
            {
                hp.fill.anchorMax = new Vector2(Mathf.Clamp01(pc.hp / pc.MaxHp), 1f);
                hp.label.text = N(Mathf.CeilToInt(pc.hp)) + " / " + N(Mathf.RoundToInt(pc.MaxHp));
                bool mage = gs.IsMage;
                if (mage)
                {
                    mp.fill.anchorMax = new Vector2(Mathf.Clamp01(pc.mana / Mathf.Max(1f, pc.MaxMana)), 1f);
                    mp.label.text = N(Mathf.RoundToInt(pc.mana)) + " / " + N(Mathf.RoundToInt(pc.MaxMana));
                    mp.fill.GetComponent<Image>().color = new Color(0.14f, 0.5f, 0.98f);
                }
                else
                {
                    mp.fill.anchorMax = new Vector2(Mathf.Clamp01(pc.stamina / pc.MaxStamina), 1f);
                    mp.label.text = "STA  " + N(Mathf.RoundToInt(pc.stamina)) + " / " + N(Mathf.RoundToInt(pc.MaxStamina));
                    mp.fill.GetComponent<Image>().color = new Color(0.30f, 0.80f, 0.34f);
                }
                staStrip.gameObject.SetActive(mage);
                if (mage) staFill.anchorMax = new Vector2(Mathf.Clamp01(pc.stamina / pc.MaxStamina), 1f);
            }
            else { hp.fill.anchorMax = Vector2.one; hp.label.text = "HP"; mp.fill.anchorMax = Vector2.one; mp.label.text = ""; staStrip.gameObject.SetActive(false); }

            // berlian status: Lapar, Haus, Buff makan, Lelah, Ultimate siap
            Diamond(0, gs.hunger / 100f, new Color(0.96f, 0.6f, 0.2f), gs.hunger < 25f);
            Diamond(1, gs.thirst / 100f, new Color(0.3f, 0.65f, 1f), gs.thirst < 25f);
            bool buff = !string.IsNullOrEmpty(gs.mealBuff);
            Diamond(2, buff ? 1f : 0f, new Color(0.55f, 0.95f, 0.45f), false);
            Diamond(3, gs.IsFatigued ? 1f : Mathf.Clamp01(gs.hoursAwake / Mathf.Max(1f, GameState.FatigueAfterHours)), gs.IsFatigued ? new Color(1f, 0.35f, 0.3f) : new Color(0.75f, 0.7f, 0.5f), gs.IsFatigued);
            var ult = SkillDB.Get(gs.skillSlots[3]);
            bool ultReady = ult != null && SkillExec.CooldownLeft(ult.id) <= 0f;
            Diamond(4, ult == null ? 0f : (ultReady ? 1f : 1f - Mathf.Clamp01(SkillExec.CooldownLeft(ult.id) / Mathf.Max(1f, ult.cooldown))), cur.accent, ultReady);

            TickBoss(gs, pc, dt);
            TickCompass();
            TickMapLetters();
            TickTopRight(gs);
            TickChat();
            TickSlots(gs);
            goldTxt.text = "<color=#FFD24A>Gold</color> " + N(gs.gold) + "   ·   Botol Minum " + gs.canteen + "/" + GameState.CanteenMax;
            int need = GameState.ExpToNext(Mathf.Max(1, gs.level));
            bool cap = gs.level >= GameState.LevelCap;
            exp.fill.anchorMax = new Vector2(cap ? 1f : Mathf.Clamp01(gs.exp / (float)Mathf.Max(1, need)), 1f);
            exp.label.text = cap ? "EXP  MAX" : "EXP  " + N(gs.exp) + " / " + N(need);
        }

        static string N(int v) { return v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture); }

        void Diamond(int i, float v, Color c, bool pulse)
        {
            dmInner[i].fillAmount = Mathf.Clamp01(v); dmInner[i].color = new Color(c.r, c.g, c.b, 0.95f);
            var g = dmGlow[i].color; g.a = pulse ? 0.35f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f) : 0f; g.r = c.r; g.g = c.g; g.b = c.b; dmGlow[i].color = g;
            dmGlow[i].canvasRenderer.SetColor(Color.white);
        }

        void UpdateTheme(GameState gs, float dt)
        {
            bool none = gs.NoJob || (gs.requireJobChoice && !gs.jobChosen);
            string key = none ? "-" : gs.job;
            if (key != themeKey) { themeKey = key; tgt = ThemeFor(gs.job, none); if (!themeInit) { cur = tgt; themeInit = true; ApplyTheme(); } }
            if (!Same(cur, tgt)) { cur = LerpT(cur, tgt, Mathf.Clamp01(dt * 5f)); ApplyTheme(); }
        }

        static bool Same(Theme a, Theme b) { return (a.accent - b.accent).maxColorComponent < 0.004f && (b.accent - a.accent).maxColorComponent < 0.004f && (a.trim - b.trim).maxColorComponent < 0.004f && (b.trim - a.trim).maxColorComponent < 0.004f; }

        void TickMapLetters()
        {
            if (mapLetters == null) return;
            for (int i = 0; i < 4; i++)
            {
                var t = mapLetters[i]; if (t == null) continue;
                bool on = Minimap.Showing; if (t.enabled != on) t.enabled = on; if (!on) continue;
                float a = (i * 90f - Minimap.Yaw) * Mathf.Deg2Rad; Vector2 v = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                float edge = Minimap.HalfPx - 12f;
                t.rectTransform.anchoredPosition = v * edge;
            }
        }

        // ------------------------------------------------------------------ boss
        void TickBoss(GameState gs, PlayerCombat pc, float dt)
        {
            Monster best = null; float bd = 1e9f;
            if (pc != null)
                foreach (var m in Monster.All)
                {
                    if (m == null || !m.isBoss || m.IsDead) continue;
                    float d = (m.transform.position - pc.transform.position).sqrMagnitude;
                    if (d < bd) { bd = d; best = m; }
                }
            bool fight = best != null && (best.InCombat || best.hp < best.MaxHp - 0.5f || bd < 28f * 28f);
            if (fight)
            {
                lastBossSeen = Time.unscaledTime;
                bossName.text = "Lv. " + BossLevel(best) + "   " + best.Name;
                float f = Mathf.Clamp01(best.hp / Mathf.Max(1f, best.MaxHp));
                bossFill = Mathf.MoveTowards(bossFill, f, dt * 1.2f);
                bossHp.fill.anchorMax = new Vector2(bossFill, 1f);
                bossHp.label.text = N(Mathf.CeilToInt(best.hp)) + " / " + N(Mathf.RoundToInt(best.MaxHp));
                bossPct.text = Mathf.CeilToInt(f * 100f) + "%";
            }
            bool show = fight || Time.unscaledTime - lastBossSeen < 1.2f;
            bossShown = Mathf.MoveTowards(bossShown, show ? 1f : 0f, dt * 3f);
            cgBoss.alpha = bossShown;
            boss.gameObject.SetActive(bossShown > 0.01f);
        }

        static int BossLevel(Monster m)
        {
            if (TowerManager.I != null) return TowerDB.RecommendedLevel(Mathf.Max(1, TowerManager.I.Floor));
            return Mathf.Clamp(Mathf.RoundToInt(m.MaxHp / 70f), 1, 99);
        }

        // ------------------------------------------------------------------ kanan-atas
        void TickTopRight(GameState gs)
        {
            string sub; zoneName.text = TownUI.PlaceNameFor(out sub);
            string wx = SeasonRuntime.Outdoors ? SeasonRuntime.WeatherName : "";
            clockTxt.text = gs.Clock;
            zoneSub.text = "Hari " + gs.day + "  ·  " + GameCalendar.DateShort + (wx != "" ? "  ·  " + wx : "");
            float h = gs.timeOfDay;
            bool night = h < 5.5f || h >= 19f;
            var wg = WeatherGlyph(wx, night);
            if (wg != lastWeather) { lastWeather = wg; weatherDot.sprite = HudArt.GlyphSprite(wg); }
            for (int i = 0; i < 3; i++) if (sideBtn[i] != null && sideBtn[i].activeSelf != Minimap.Showing) sideBtn[i].SetActive(Minimap.Showing);
            // posisi panel quest mengikuti minimap
            float y = Minimap.Showing ? 268f + (Minimap.Big ? 50f : 0f) : 60f;
            L(questPanel, 0, y, 300, questCollapsed ? 38f : questH);
            BuildQuestRows(gs);
        }

        struct Q { public string title, obj; public bool done; }

        void BuildQuestRows(GameState gs)
        {
            var list = new List<Q>();
            if (gs.activeQuest != null && string.IsNullOrEmpty(gs.activeQuest.title)) gs.activeQuest = null;
            { string gt, go; bool gd; if (GuideTutorial.TrackerLine(out gt, out go, out gd)) list.Add(new Q { title = gt, obj = go, done = gd }); }
            if (gs.activeQuest != null)
                list.Add(new Q { title = gs.activeQuest.title, done = gs.activeQuest.completed, obj = gs.activeQuest.completed ? "Selesai - lapor ke Guild" : (gs.activeQuest.targetCount > 0 ? "Progres " + gs.questProgress + "/" + gs.activeQuest.targetCount : "Sedang berjalan") });
            if (StarterQuests.Current < StarterQuests.All.Length)
            {
                var sq = StarterQuests.All[StarterQuests.Current];
                if (StarterQuests.Accepted(sq)) list.Add(new Q { title = sq.title, done = StarterQuests.Ready(sq), obj = StarterQuests.Ready(sq) ? "Selesai - lapor ke Brann" : "Progres " + StarterQuests.Progress(sq) + "/" + sq.count });
            }
            if (gs.requireJobChoice && !gs.jobChosen && gs.level >= StarterConfig.JobLevel) list.Add(new Q { title = "Pilih Jalanmu (Job)", obj = "Temui Veteran Brann di desa", done = false });
            foreach (var wr in WorldQuests.TrackerRows()) list.Add(new Q { title = wr[0], obj = wr[1], done = false });
            foreach (var l in ShopQuests.TrackerLines()) list.Add(new Q { title = l, obj = "", done = false });
            foreach (var l in Investigation.TrackerLines()) list.Add(new Q { title = l, obj = "", done = false });
            foreach (var l in Mail.TrackerLines()) list.Add(new Q { title = l, obj = "", done = false });
            foreach (var rr in RecipeTracker.Rows()) list.Add(new Q { title = rr[0], obj = rr[1], done = false });
            var sb = new System.Text.StringBuilder();
            sb.Append(questCollapsed ? "c" : "o");
            foreach (var q in list) sb.Append(q.title).Append('|').Append(q.obj).Append(q.done ? 1 : 0).Append(';');
            string key = sb.ToString();
            if (key == questKey) return;
            questKey = key;
            themed.RemoveAll(x => x.g == null);
            foreach (var g in questRows) if (g != null) UnityEngine.Object.Destroy(g);
            questRows.Clear();
            questChevron.text = questCollapsed ? "v" : "^";
            questBody.gameObject.SetActive(!questCollapsed);
            var panelRt = questPanel; 
            if (questCollapsed) return;
            int shown = Mathf.Min(3, list.Count);
            questH = 38f + 8f + Mathf.Max(1, shown) * 52f + (list.Count > 3 ? 18f : 0f);
            if (shown == 0)
            {
                questH = 100f; var t0 = Tx(questBody, "None", 14, TextAnchor.MiddleCenter, R.Text); L(t0.rectTransform, 10, 8, 280, 44); t0.text = "Belum ada quest aktif.\nAmbil di Papan Quest Guild."; questRows.Add(t0.gameObject);
                var c0 = t0.color; c0.a = 0.6f; t0.color = c0; themed.RemoveAt(themed.Count - 1);
                return;
            }
            for (int i = 0; i < shown; i++)
            {
                float y = 2 + i * 52;
                var ex = Tx(questBody, "Ex" + i, 24, TextAnchor.MiddleCenter, R.Accent, true); L(ex.rectTransform, 8, y, 22, 34); ex.text = "!"; questRows.Add(ex.gameObject);
                var tt = Tx(questBody, "Title" + i, 16, TextAnchor.MiddleLeft, R.Hi, true); L(tt.rectTransform, 34, y, 258, 22); tt.text = list[i].title; tt.horizontalOverflow = HorizontalWrapMode.Wrap; tt.verticalOverflow = VerticalWrapMode.Truncate; questRows.Add(tt.gameObject);
                if (list[i].obj != "")
                {
                    var box = Box("Cb" + i, questBody); L(box, 40, y + 28, 13, 13); questRows.Add(box.gameObject);
                    var bf = Pic(box, "F", HudArt.White, R.Dark, 1f); Stretch(bf.rectTransform);
                    var br2 = Pic(box, "R", HudArt.PanelTrim(32, 3, 8, false), R.Trim, 1f, true); Stretch(br2.rectTransform);
                    if (list[i].done) { var ck = Pic(box, "Ck", HudArt.White, R.Accent); Stretch(ck.rectTransform, 3.5f); }
                    var ob = Tx(questBody, "Obj" + i, 14, TextAnchor.MiddleLeft, R.Text); L(ob.rectTransform, 60, y + 22, 234, 24); ob.text = list[i].obj; questRows.Add(ob.gameObject);
                }
            }
            if (list.Count > 3)
            {
                var more = Tx(questBody, "More", 14, TextAnchor.MiddleRight, R.Hi); L(more.rectTransform, 10, 2 + shown * 52, 280, 16); more.text = "+" + (list.Count - 3) + " quest lagi"; questRows.Add(more.gameObject);
            }
        }

        // ------------------------------------------------------------------ chat
        void TickChat()
        {
            if (chatVersionDrawn == HudChat.Version && chatTabDrawn == chatTabSel) return;
            chatVersionDrawn = HudChat.Version; chatTabDrawn = chatTabSel;
            for (int i = 0; i < 2; i++) { chatTabLine[i].enabled = i == chatTabSel; }
            var sb = new System.Text.StringBuilder(); int n = 0;
            for (int i = HudChat.Lines.Count - 1; i >= 0 && n < 6; i--)
            {
                var ln = HudChat.Lines[i];
                if (chatTabSel == 1 && ln.kind != 0) continue;
                string col = ln.kind == 0 ? "#F2C94C" : ln.kind == 1 ? "#4FC3F7" : ln.kind == 2 ? "#6EE07A" : "#C792EA";
                string row = "<color=" + col + ">[" + ln.tag + "]</color> " + ln.msg;
                if (sb.Length > 0) sb.Insert(0, row + "\n"); else sb.Append(row);
                n++;
            }
            chatText.text = sb.ToString();
        }

        // ------------------------------------------------------------------ slot
        void TickSlots(GameState gs)
        {
            for (int i = 0; i < 7; i++)
            {
                var s = skillSlot[i]; var sd = SkillDB.Get(gs.skillSlots[i]);
                s.icon.enabled = sd != null;
                s.key.text = SkillKeys[i];
                float left = 0f;
                if (sd != null)
                {
                    s.icon.sprite = SkillIcons.For(sd);
                    left = SkillExec.CooldownLeft(sd.id);
                    s.icon.color = left > 0f ? new Color(0.6f, 0.6f, 0.65f, 1f) : Color.white;
                }
                s.cd.fillAmount = sd != null && left > 0f ? Mathf.Clamp01(left / Mathf.Max(0.5f, sd.cooldown)) : 0f;
                s.cdTxt.text = left > 0.05f ? (left >= 10f ? Mathf.CeilToInt(left).ToString() : left.ToString("0.0")) : "";
                if (i == 3 && s.glow != null)
                {
                    bool ready = sd != null && left <= 0f;
                    var gc = s.glow.color; gc.a = ready ? 0.45f + 0.3f * Mathf.Sin(Time.unscaledTime * 3.2f) : 0f; s.glow.color = gc;
                }
            }
            for (int i = 0; i < 6; i++)
            {
                var s = itemSlot[i]; string id = gs.quickbar[i]; var def = ItemDB.Get(id);
                s.icon.enabled = def != null;
                if (def != null) { s.icon.sprite = ItemIcons.For(def); s.count.text = gs.Count(id).ToString(); s.icon.color = gs.Count(id) > 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f); }
                else s.count.text = "";
            }
        }
    }
}
