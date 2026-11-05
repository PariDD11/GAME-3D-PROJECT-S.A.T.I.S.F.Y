using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Menu TAS (tombol Tab): tab Inventory / Equipment / Material / Quest Item, daftar item bergulir di kiri,
    /// slot perlengkapan + quickbar di tengah, panel detail di kanan, dan dialog konfirmasi pemasangan.
    /// Ikon item buatan kode (ItemIcons); ganti dengan sprite asli bila sudah ada.
    /// </summary>
    public class BackpackUI : MonoBehaviour
    {
        public static BackpackUI I { get; private set; }
        public bool IsOpen { get { return root != null && root.activeSelf; } }

        static readonly string[] TabIds = { "inv", "equip", "mat", "quest" };
        static readonly string[] TabNames = { "Inventory", "Equipment", "Material", "Quest Item" };
        // kolom kiri: Senjata, Kepala, Sarung Tangan, Kaki; kolom kanan: Badan, Cincin, Kalung
        static readonly string[] EquipSlots = { "Weapon", "Head", "Hands", "Legs", "Body", "Ring", "Neck" };
        static readonly string[] EquipCaption = { "Senjata", "Kepala", "Sarung Tangan", "Kaki", "Badan", "Cincin", "Kalung" };
        static readonly float[] EquipX = { 6f, 6f, 6f, 6f, 188f, 188f, 188f };
        static readonly float[] EquipY = { 4f, 92f, 180f, 268f, 4f, 92f, 180f };
        static readonly string[] PickKinds = { "Semua", "Serangan", "Pertahanan", "Utility", "Sihir" };

        // warna isi mengikuti tema job (UiStyle.Cur)
        static Color SlotCol { get { return UiStyle.Cur.Slot; } }
        static Color SlotSel { get { return UiStyle.Cur.SlotSel; } }
        static Color SlotEmpty { get { return UiStyle.Cur.SlotEmpty; } }
        static Color RowCol { get { return UiStyle.Cur.Row; } }
        static Color OnInk { get { return UiStyle.Cur.PanelLo; } }      // teks gelap di atas isi terang (terpilih)

        static Color TypeColor(ItemDef d)
        {
            if (d == null) return Color.gray;
            switch (d.type)
            {
                case ItemType.Food: return new Color(0.96f, 0.66f, 0.30f);
                case ItemType.Drink: return new Color(0.42f, 0.72f, 1.00f);
                case ItemType.Material: return new Color(0.72f, 0.62f, 0.46f);
                case ItemType.Consumable: return new Color(0.52f, 0.92f, 0.55f);
                case ItemType.Attachment: return new Color(0.78f, 0.52f, 0.96f);
                case ItemType.QuestItem: return new Color(0.98f, 0.86f, 0.32f);
                default: return UiStyle.Cur.Gold;
            }
        }

        static int maxAtk = -1, maxDef = -1;
        static void EnsureMax()
        {
            if (maxAtk >= 0) return;
            maxAtk = 10; maxDef = 10;
            foreach (var g in ItemDB.OfType(ItemType.Gear)) { if (g.atk > maxAtk) maxAtk = g.atk; if (g.def > maxDef) maxDef = g.def; }
        }

        /// <summary>Kartu berbingkai tema dengan isi bergradasi (warna isi = fill, warna garis = border).</summary>
        static Image Card(Transform parent, string name, float x, float y, float w, float h, Color fill, Color border, bool raycast)
        {
            var im = UiStyle.Plate(name, parent); im.color = border; im.raycastTarget = raycast;
            UIKit.At(im.rectTransform, x, y, w, h);
            var f = UIKit.Img("Fill", im.transform, fill); f.sprite = UiStyle.SheenSprite; f.raycastTarget = false;
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = Vector2.one;
            f.rectTransform.offsetMin = new Vector2(3, 3); f.rectTransform.offsetMax = new Vector2(-3, -3);
            return im;
        }

        Image TabBtn(Transform parent, string label, float x, float y, float w, float h, bool on, Action act, int fs, out Text lbl)
        {
            var th = UiStyle.Cur;
            var c = Card(parent, "Tab", x, y, w, h, on ? th.TabOn : th.TabOff, on ? Color.white : new Color(0.78f, 0.78f, 0.78f), true);
            var bt = c.gameObject.AddComponent<Button>(); bt.targetGraphic = c;
            bt.onClick.AddListener(() => Audio.Sfx("click"));
            bt.onClick.AddListener(() => { if (act != null) act(); });
            lbl = UiStyle.Label(c.transform, "L", fs, TextAnchor.MiddleCenter, on ? OnInk : UiStyle.Cream, 0, 0, w, h);
            lbl.text = label; lbl.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
            return c;
        }

        static Image Watermark(Transform parent, float x, float y, float size, float alpha)
        {
            var w = UIKit.Img("Emblem", parent, new Color(1f, 1f, 1f, alpha));
            w.sprite = UiStyle.EmblemSprite; w.preserveAspect = true; w.raycastTarget = false;
            UIKit.At(w.rectTransform, x, y, size, size);
            return w;
        }

        GameObject root, confirmRoot, pickRoot;
        RectTransform listContent, centerRoot, detailRoot, confirmBody, pickContent, pickBody;
        Image[] pickTabImgs = new Image[PickKinds.Length];
        Text[] pickTabLbl = new Text[PickKinds.Length];
        string detailTab = "stats";
        string pickSlot, pickKind = "Semua", pickSel;
        int pickIdx;
        Text slotInfoText, emptyHint;
        Image[] tabImgs = new Image[4];
        Image[] tabFill = new Image[4];
        Text[] tabLbl = new Text[4];
        RectTransform capFill; Image capFillImg; Text goldText, subText;
        int themeVer = -1;
        string tab = "inv";
        string selItem, selEquip;
        int selQuick = -1;
        int openedFrame = -1;

        // pratinjau karakter 3D (panggung tersembunyi di bawah dunia, dirender ke RenderTexture)
        GameObject pvStage, pvRig;
        Transform pvSpin;
        RenderTexture pvRT;
        string pvSig = "";
        float pvYaw = 160f;

        void Awake() { I = this; }
        void OnDestroy() { DestroyPreview(); if (I == this) I = null; }

        void Update()
        {
            if (pvSpin != null) { pvYaw += 24f * Time.unscaledDeltaTime; pvSpin.localRotation = Quaternion.Euler(0f, pvYaw, 0f); }
            if (IsOpen && Time.frameCount != openedFrame && TownInput.InventoryDown()) Hide();
        }

        public void Show()
        {
            if (UiStyle.Stale(ref themeVer) && root != null) { Destroy(root); root = null; DestroyPreview(); }
            if (root == null) Build();
            if (GameState.I != null) GameState.I.AddStat("open_bag");
            root.SetActive(true);
            confirmRoot.SetActive(false);
            pickRoot.SetActive(false);
            openedFrame = Time.frameCount;
            TownUI.I.SetExternalOpen(true);
            Refresh();
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            DestroyPreview();
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        // ---------------- pratinjau karakter ----------------
        static string EqId(GameState gs, string slot) { string v; return gs.equipment.TryGetValue(slot, out v) ? v : ""; }

        void EnsurePreview(GameState gs)
        {
            if (pvStage == null)
            {
                pvStage = new GameObject("BackpackPreviewStage");
                pvStage.transform.position = new Vector3(0f, -900f, 0f);
                pvSpin = new GameObject("Spin").transform; pvSpin.SetParent(pvStage.transform, false);
                var camGo = new GameObject("PreviewCam"); camGo.transform.SetParent(pvStage.transform, false);
                camGo.transform.localPosition = new Vector3(0f, 1.0f, 4.6f);
                camGo.transform.localRotation = Quaternion.Euler(3f, 180f, 0f);
                var cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = 30f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 30f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.Lerp(UiStyle.Cur.PanelLo, UiStyle.Cur.PanelHi, 0.55f);
                pvRT = new RenderTexture(260, 846, 16, RenderTextureFormat.ARGB32);
                cam.targetTexture = pvRT;
                Vector3[] lp = { new Vector3(2.5f, 3.5f, 3.5f), new Vector3(-3f, 2f, 3f), new Vector3(0f, 2f, -3f) };
                float[] li = { 1.6f, 0.9f, 0.8f };
                for (int i = 0; i < 3; i++)
                {
                    var g = new GameObject("L"); g.transform.SetParent(pvStage.transform, false); g.transform.localPosition = lp[i];
                    var l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 14f; l.intensity = li[i] * 2.4f; l.color = new Color(1f, 0.96f, 0.9f);
                }
                pvSig = "";
            }
            string sig = JsonUtility.ToJson(CharacterData.Current) + "|" + EqId(gs, "Head") + EqId(gs, "Body") + EqId(gs, "Legs") + EqId(gs, "Weapon") + "|" + gs.job + (StarterConfig.Unarmed ? "U" : "");
            if (sig == pvSig && pvRig != null) return;
            pvSig = sig;
            if (pvRig != null) Destroy(pvRig);
            pvSpin.localScale = Vector3.one;
            var data = CharacterData.Current;
            if (HGModelConfig.UsesExternalPlayer)
            {
                var pf = ExternalModelDriver.LoadPrefab(data.female, 0);
                if (pf != null)
                {
                    var inst = Instantiate(pf, pvSpin, false); inst.name = "PreviewExt";
                    foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
                    var an = inst.GetComponentInChildren<Animator>(); var cfg = HGModelConfig.Load();
                    if (an != null && cfg.playerController != null) an.runtimeAnimatorController = cfg.playerController;
                    ExternalModelDriver.FitTo(inst.transform, 1.9f);
                    pvRig = inst;
                    return;
                }
            }
            var spec = data.ToSpec();
            NpcKit.ApplyEquipment(spec, EqId(gs, "Head"), EqId(gs, "Body"), EqId(gs, "Legs"));
            var go = NpcKit.Spawn(pvSpin, "PreviewRig", Vector3.zero, 0f, spec, false, false);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            var rig = go.GetComponent<CharacterRig>();
            pvSpin.localScale = Vector3.one * (1.9f / rig.height);
            pvRig = go;
            var pv = new GameObject("Pv").transform; pv.SetParent(go.transform, false);
            var pa = go.AddComponent<PlayerAnimator>();
            pa.Init(null, rig, pv, 0f);
            string w = EqId(gs, "Weapon");
            string kind = StarterConfig.Unarmed ? "" : NpcKit.WeaponKind(w);
            if (kind == "staff") PvProp(rig.handR, NpcKit.Weapon(w), Vector3.zero, PlayerAvatar.SwordGrip);
            else if (kind == "sword") { PvProp(rig.handR, NpcKit.Weapon(w), Vector3.zero, PlayerAvatar.SwordGrip); PvProp(rig.foreL, NpcKit.Weapon("@shield"), new Vector3(-0.055f, -0.13f, 0.02f), Quaternion.Euler(0f, 0f, 90f)); }
            else if (kind == "bow") PvProp(rig.handL, NpcKit.Weapon(w), Vector3.zero, PlayerAvatar.BowGrip);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.gameObject.layer = 0;
        }

        static void PvProp(Transform parent, NpcKit.WeaponModel m, Vector3 pos, Quaternion rot)
        {
            var g = new GameObject(m.id);
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = m.mesh;
            g.AddComponent<MeshRenderer>().sharedMaterials = m.mats;
            g.transform.localPosition = pos; g.transform.localRotation = rot;
        }

        void DestroyPreview()
        {
            if (pvStage != null) Destroy(pvStage);
            if (pvRT != null) { pvRT.Release(); Destroy(pvRT); }
            pvStage = null; pvRig = null; pvSpin = null; pvRT = null; pvSig = "";
        }

        // ---------------- Build (sekali) ----------------
        void Build()
        {
            var bg = UiStyle.Frame("BackpackWindow", TownUI.I.Root); bg.raycastTarget = true;
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(bg.rectTransform, c, c, c, Vector2.zero, new Vector2(1260, 790));
            root = bg.gameObject;
            var rt = root.transform;

            var th = UiStyle.Cur;
            // ----- header -----
            var badge = UiStyle.Frame("Badge", rt);
            UIKit.At(badge.rectTransform, 30, 14, 80, 80);
            var em = UIKit.Img("Emblem", badge.transform, Color.white);
            em.sprite = UiStyle.EmblemSprite; em.preserveAspect = true; em.raycastTarget = false;
            UIKit.At(em.rectTransform, 12, 12, 56, 56);
            UiStyle.Label(rt, "Title", 40, TextAnchor.MiddleLeft, UiStyle.Cream, 124, 16, 560, 50).text = th.Title;
            subText = UiStyle.Label(rt, "Sub", 18, TextAnchor.MiddleLeft, UiStyle.Muted, 126, 62, 600, 26);
            subText.fontStyle = FontStyle.Italic;
            // kapasitas tas (bar) + gold
            var capLbl = (Text)null;
            capFill = UiStyle.Bar(rt, 700, 26, 270, 36, UiStyle.Gold, out capLbl, 20);
            capFillImg = capFill.GetComponent<Image>();
            slotInfoText = capLbl;
            UiStyle.Label(rt, "CapCap", 14, TextAnchor.MiddleLeft, UiStyle.Muted, 704, 64, 260, 20).text = "KAPASITAS TAS";
            var gp = Card(rt, "GoldPlate", 990, 26, 150, 36, th.Slot, Color.white, false);
            goldText = UiStyle.Label(gp.transform, "Gold", 20, TextAnchor.MiddleCenter, UiStyle.Cream, 0, 0, 150, 36);
            UiStyle.Label(rt, "GoldCap", 14, TextAnchor.MiddleLeft, UiStyle.Muted, 994, 64, 140, 20).text = "GOLD";
            var close = UIKit.Btn(rt, "X", Hide, true, new Color(0.45f, 0.25f, 0.25f), 30);
            UIKit.At((RectTransform)close.transform, 1160, 24, 62, 58);
            UiStyle.Divider(rt, 34, 92, 1192);

            // ----- tab -----
            for (int i = 0; i < TabIds.Length; i++)
            {
                string id = TabIds[i];
                var tb = Card(rt, "TabCard", 36 + i * 168, 102, 160, 48, th.TabOff, Color.white, true);
                var tbn = tb.gameObject.AddComponent<Button>(); tbn.targetGraphic = tb;
                tbn.onClick.AddListener(() => Audio.Sfx("click"));
                tbn.onClick.AddListener(() => { tab = id; Refresh(); });
                tabImgs[i] = tb; tabFill[i] = tb.transform.Find("Fill").GetComponent<Image>();
                tabLbl[i] = UiStyle.Label(tb.transform, "L", 21, TextAnchor.MiddleCenter, UiStyle.Cream, 0, 0, 160, 48);
            }
            var tip = UiStyle.Label(rt, "Tip", 15, TextAnchor.MiddleRight, UiStyle.Muted, 720, 112, 500, 28);
            tip.fontStyle = FontStyle.Italic; tip.text = "Shift + Buang = jatuhkan seluruh tumpukan   |   Tab/Esc = tutup";

            // ----- daftar item bergulir (kiri) -----
            var lp = UiStyle.Frame("ListPanel", rt);
            UIKit.At(lp.rectTransform, 34, 160, 410, 600);
            Watermark(rt, 80, 270, 320, 0.06f);
            var vp = UIKit.Img("Viewport", rt, new Color(1f, 1f, 1f, 0.004f));
            UIKit.At(vp.rectTransform, 46, 172, 386, 576);
            vp.gameObject.AddComponent<RectMask2D>();
            listContent = UIKit.Rect("Content", vp.transform);
            listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1); listContent.pivot = new Vector2(0, 1);
            listContent.anchoredPosition = Vector2.zero; listContent.sizeDelta = new Vector2(0, 576);
            var sr = vp.gameObject.AddComponent<ScrollRect>();
            sr.content = listContent; sr.viewport = vp.rectTransform;
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40f; sr.inertia = true;
            emptyHint = UiStyle.Label(rt, "Empty", 20, TextAnchor.MiddleCenter, UiStyle.Muted, 60, 360, 358, 100);
            emptyHint.fontStyle = FontStyle.Italic;

            // ----- tengah: perlengkapan + quickbar -----
            var cp = UiStyle.Frame("CenterPanel", rt);
            UIKit.At(cp.rectTransform, 456, 160, 280, 600);
            centerRoot = UIKit.Rect("Center", rt);
            UIKit.At(centerRoot, 468, 172, 256, 576);

            // ----- detail (kanan) -----
            var dp = UiStyle.Frame("DetailPanel", rt);
            UIKit.At(dp.rectTransform, 748, 104, 478, 656);
            Watermark(rt, 830, 230, 330, 0.05f);
            detailRoot = UIKit.Rect("Detail", rt);
            UIKit.At(detailRoot, 766, 120, 442, 628);

            BuildPicker(rt);
            BuildConfirm(rt);
            root.SetActive(false);
        }

        // jendela "Pilih Attachment"
        void BuildPicker(Transform rt)
        {
            var dim = UIKit.Img("PickDim", rt, new Color(0f, 0f, 0f, 0.55f));
            UIKit.Place(dim.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            pickRoot = dim.gameObject;
            var box = UiStyle.Frame("PickBox", pickRoot.transform); box.raycastTarget = true;
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(box.rectTransform, c, c, c, Vector2.zero, new Vector2(760, 650));
            UiStyle.Label(box.transform, "PTitle", 32, TextAnchor.MiddleLeft, UiStyle.Gold, 32, 14, 500, 52).text = "Pilih Attachment";
            var x = UIKit.Btn(box.transform, "X", () => pickRoot.SetActive(false), true, new Color(0.45f, 0.25f, 0.25f), 28);
            UIKit.At((RectTransform)x.transform, 684, 12, 58, 52);
            UiStyle.Divider(box.transform, 30, 70, 700);
            for (int i = 0; i < PickKinds.Length; i++)
            {
                string k = PickKinds[i];
                pickTabImgs[i] = TabBtn(box.transform, k, 30 + i * 138, 80, 132, 42, false, () => { pickKind = k; pickSel = null; RefreshPicker(); }, 21, out pickTabLbl[i]);
            }
            UiStyle.Label(box.transform, "H1", 17, TextAnchor.MiddleLeft, UiStyle.Muted, 112, 126, 240, 24).text = "Nama Attachment";
            UiStyle.Label(box.transform, "H2", 17, TextAnchor.MiddleLeft, UiStyle.Muted, 372, 126, 120, 24).text = "Efek";
            UiStyle.Label(box.transform, "H3", 17, TextAnchor.MiddleLeft, UiStyle.Muted, 490, 126, 120, 24).text = "Stat";
            var vp = UIKit.Img("PickViewport", box.transform, new Color(1f, 1f, 1f, 0.004f));
            UIKit.At(vp.rectTransform, 30, 152, 700, 300);
            vp.gameObject.AddComponent<RectMask2D>();
            pickContent = UIKit.Rect("PickContent", vp.transform);
            pickContent.anchorMin = new Vector2(0, 1); pickContent.anchorMax = new Vector2(1, 1); pickContent.pivot = new Vector2(0, 1);
            pickContent.anchoredPosition = Vector2.zero; pickContent.sizeDelta = new Vector2(0, 300);
            var sr = vp.gameObject.AddComponent<ScrollRect>();
            sr.content = pickContent; sr.viewport = vp.rectTransform;
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 40f;
            UiStyle.Divider(box.transform, 30, 462, 700);
            pickBody = UIKit.Rect("PickBody", box.transform);
            UIKit.At(pickBody, 30, 472, 700, 166);
            pickRoot.SetActive(false);
        }

        void OpenPicker(string slot, int idx)
        {
            pickSlot = slot; pickIdx = idx; pickKind = "Semua"; pickSel = null;
            pickRoot.SetActive(true);
            RefreshPicker();
        }

        void RefreshPicker()
        {
            var gs = GameState.I;
            for (int i = 0; i < pickTabImgs.Length; i++)
            {
                bool on = PickKinds[i] == pickKind;
                pickTabImgs[i].transform.Find("Fill").GetComponent<Image>().color = on ? UiStyle.Cur.TabOn : UiStyle.Cur.TabOff;
                pickTabImgs[i].color = on ? Color.white : new Color(0.78f, 0.78f, 0.78f);
                pickTabLbl[i].color = on ? OnInk : UiStyle.Cream;
                pickTabLbl[i].fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
            }
            UIKit.Clear(pickContent);
            UIKit.Clear(pickBody);
            var list = new List<ItemDef>();
            foreach (var d in ItemDB.OfType(ItemType.Attachment))
                if (gs.Count(d.id) > 0 && (pickKind == "Semua" || d.attachKind == pickKind)) list.Add(d);
            if (pickSel != null && !list.Exists(d => d.id == pickSel)) pickSel = null;
            for (int i = 0; i < list.Count; i++)
            {
                var def = list[i]; bool sel = def.id == pickSel;
                var im = Card(pickContent, "Row", 0, i * 78f, 690, 72, sel ? SlotSel : RowCol, sel ? Color.white : new Color(0.82f, 0.82f, 0.82f), true);
                var b = im.gameObject.AddComponent<Button>(); b.targetGraphic = im;
                b.onClick.AddListener(() => Audio.Sfx("click"));
                b.onClick.AddListener(() => { pickSel = def.id; RefreshPicker(); });
                MakeSlot(im.transform, 8, 8, 56, ItemIcons.For(def), null, false, false, null, null, def);
                Color tc = sel ? OnInk : UiStyle.Cream;
                UiStyle.Label(im.transform, "N", 22, TextAnchor.MiddleLeft, tc, 80, 0, 270, 72).text = def.name;
                UiStyle.Label(im.transform, "E", 19, TextAnchor.MiddleLeft, tc, 342, 0, 120, 72).text = def.attachKind;
                UiStyle.Label(im.transform, "S", 19, TextAnchor.MiddleLeft, tc, 460, 0, 140, 72).text = StatLine(def);
                UiStyle.Label(im.transform, "C", 22, TextAnchor.MiddleRight, tc, 600, 0, 80, 72).text = "x" + gs.Count(def.id);
            }
            pickContent.sizeDelta = new Vector2(0, Mathf.Max(300f, list.Count * 78f));
            if (list.Count == 0)
            {
                var t = UiStyle.Label(pickBody, "None", 20, TextAnchor.MiddleCenter, UiStyle.Muted, 0, 0, 700, 166);
                t.fontStyle = FontStyle.Italic;
                t.text = "Tidak ada attachment di kategori ini.\nBeli di Toko Umum (Attachment gear).";
                return;
            }
            var sd = pickSel != null ? ItemDB.Get(pickSel) : null;
            if (sd == null)
            {
                var t = UiStyle.Label(pickBody, "Hint", 20, TextAnchor.MiddleCenter, UiStyle.Muted, 0, 0, 700, 166);
                t.fontStyle = FontStyle.Italic; t.text = "Pilih attachment dari daftar.";
                return;
            }
            MakeSlot(pickBody, 0, 10, 84, ItemIcons.For(sd), null, false, false, null, null, sd);
            UiStyle.Label(pickBody, "SN", 28, TextAnchor.MiddleLeft, UiStyle.Gold, 100, 6, 400, 44).text = sd.name;
            UiStyle.Label(pickBody, "SE", 19, TextAnchor.UpperLeft, UiStyle.Muted, 100, 52, 400, 100).text = sd.attachKind + "   " + StatLine(sd) + "\nDipasang ke slot " + (pickIdx + 1) + " pada gear terpilih.";
            var ok = UIKit.Btn(pickBody, "Pasang", () =>
            {
                string id = sd.id, slot = pickSlot; int idx = pickIdx;
                ShowConfirm(null, sd, () => { gs.TryAttach(slot, idx, id); pickRoot.SetActive(false); });
            }, true, new Color(1f, 0.8f, 0.4f), 26);
            UIKit.At((RectTransform)ok.transform, 510, 70, 190, 62);
        }

        void BuildConfirm(Transform rt)
        {
            var dim = UIKit.Img("ConfirmDim", rt, new Color(0f, 0f, 0f, 0.6f));
            UIKit.Place(dim.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            confirmRoot = dim.gameObject;
            var box = UiStyle.Frame("ConfirmBox", confirmRoot.transform); box.raycastTarget = true;
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(box.rectTransform, c, c, c, Vector2.zero, new Vector2(620, 320));
            UiStyle.Label(box.transform, "CTitle", 28, TextAnchor.MiddleCenter, UiStyle.Gold, 0, 20, 620, 44).text = "Konfirmasi Pemasangan";
            UiStyle.Divider(box.transform, 40, 66, 540);
            confirmBody = UIKit.Rect("Body", box.transform);
            UIKit.At(confirmBody, 0, 80, 620, 230);
        }

        // ---------------- elemen ----------------
        Button MakeSlot(Transform parent, float x, float y, float size, Sprite icon, string count, bool sel, bool empty, string caption, Action onClick, ItemDef gd = null)
        {
            Color gb = gd != null && !empty ? Color.Lerp(Color.white, ItemGrades.Tint(gd), 0.85f) : Color.white;
            var im = Card(parent, "Slot", x, y, size, size, empty ? SlotEmpty : (sel ? SlotSel : SlotCol), sel ? Color.white : (empty ? new Color(0.7f, 0.7f, 0.7f) : gb), onClick != null);
            Button b = null;
            if (onClick != null)
            {
                b = im.gameObject.AddComponent<Button>();
                b.targetGraphic = im;
                var cb = b.colors;
                cb.normalColor = Color.white; cb.highlightedColor = new Color(1.25f, 1.2f, 1.0f); cb.pressedColor = new Color(0.8f, 0.75f, 0.6f);
                b.colors = cb;
                b.onClick.AddListener(() => Audio.Sfx("click"));
                b.onClick.AddListener(() => onClick());
            }
            if (icon != null)
            {
                var ic = UIKit.Img("Icon", im.transform, Color.white);
                ic.sprite = icon; ic.preserveAspect = true; ic.raycastTarget = false;
                float m = size * 0.14f;
                UIKit.At(ic.rectTransform, m, m, size - 2 * m, size - 2 * m);
            }
            if (gd != null && !empty && ItemGrades.Of(gd) >= ItemGrade.Rare)
            {
                var gem = UIKit.Img("Grade", im.transform, ItemGrades.Tint(gd)); gem.raycastTarget = false;
                UIKit.At(gem.rectTransform, size * 0.08f, size * 0.08f, size * 0.16f, size * 0.16f);
            }
            if (!string.IsNullOrEmpty(count))
                UiStyle.Label(im.transform, "Cnt", Mathf.RoundToInt(size * 0.24f), TextAnchor.LowerRight, Color.white, 0, 0, size - 5, size - 2).text = count;
            if (!string.IsNullOrEmpty(caption))
                UiStyle.Label(parent, "Cap", 15, TextAnchor.UpperCenter, UiStyle.Muted, x - 8, y + size + 1, size + 16, 20).text = caption;
            return b;
        }

        void ListRow(float y, ItemDef def, int count, bool sel, Action onClick)
        {
            var th = UiStyle.Cur;
            Color tc = TypeColor(def);
            Color gc = ItemGrades.Tint(def);
            var im = Card(listContent, "Row", 0, y, 374, 72, sel ? th.SlotSel : th.Row, sel ? Color.white : Color.Lerp(new Color(0.82f, 0.82f, 0.82f), gc, ItemGrades.Of(def) == ItemGrade.Common ? 0f : 0.7f), true);
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            var cb = b.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(1.25f, 1.2f, 1.0f); cb.pressedColor = new Color(0.8f, 0.75f, 0.6f); b.colors = cb;
            b.onClick.AddListener(() => Audio.Sfx("click"));
            b.onClick.AddListener(() => onClick());
            var stripe = UIKit.Img("Stripe", im.transform, tc); stripe.raycastTarget = false;     // garis warna menurut jenis item
            UIKit.At(stripe.rectTransform, 6, 8, 5, 56);
            MakeSlot(im.transform, 16, 8, 56, ItemIcons.For(def), null, false, false, null, null, def);
            var n = UiStyle.Label(im.transform, "N", 22, TextAnchor.MiddleLeft, sel ? OnInk : Color.Lerp(UiStyle.Cream, gc, 0.55f), 82, 6, 224, 34);
            n.text = def.name;
            var s = UiStyle.Label(im.transform, "S", 16, TextAnchor.MiddleLeft, sel ? OnInk : Color.Lerp(tc, UiStyle.Muted, 0.35f), 82, 38, 224, 26);
            s.text = (ItemGrades.Of(def) == ItemGrade.Common ? "" : ItemGrades.Name(def) + "  -  ") + SubLine(def);
            if (count > 1)
            {
                var pill = Card(im.transform, "Cnt", 300, 20, 64, 32, sel ? OnInk : th.PanelLo, Color.white, false);
                var c = UiStyle.Label(pill.transform, "C", 20, TextAnchor.MiddleCenter, sel ? th.SlotSel : UiStyle.Cream, 0, 0, 64, 32);
                c.text = "x" + count;
            }
        }

        static string SubLine(ItemDef d)
        {
            if (d.type == ItemType.Gear || d.type == ItemType.Attachment) return TypeLabel(d) + (d.atk > 0 ? "   ATK +" + d.atk : "") + (d.def > 0 ? "   DEF +" + d.def : "");
            return TypeLabel(d);
        }

        // ---------------- Refresh ----------------
        void Refresh()
        {
            var gs = GameState.I;
            gs.SyncQuestItems();
            if (selItem != null && gs.Count(selItem) <= 0) selItem = null;
            if (selEquip != null && !gs.equipment.ContainsKey(selEquip)) selEquip = null;

            UIKit.Clear(listContent);
            UIKit.Clear(centerRoot);
            UIKit.Clear(detailRoot);

            var th = UiStyle.Cur;
            var allStacks = BuildStacks(gs);
            for (int i = 0; i < tabImgs.Length; i++)
            {
                bool on = TabIds[i] == tab;
                string keep = tab; tab = TabIds[i];
                int cnt = 0; foreach (var st0 in allStacks) if (TabMatch(ItemDB.Get(st0.Key))) cnt++;
                tab = keep;
                tabFill[i].color = on ? th.TabOn : th.TabOff;
                tabImgs[i].color = on ? Color.white : new Color(0.78f, 0.78f, 0.78f);
                tabLbl[i].color = on ? OnInk : UiStyle.Cream;
                tabLbl[i].fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
                tabLbl[i].text = TabNames[i] + (cnt > 0 ? "  (" + cnt + ")" : "");
            }
            string jobTxt = (gs.NoJob || (gs.requireJobChoice && !gs.jobChosen)) ? "Tanpa Job" : JobInfo.Display(gs);
            subText.text = jobTxt + "  Lv " + gs.level + "   -   " + th.Subtitle;
            goldText.text = GoldUI.Num(gs.gold) + " g";

            // ----- daftar -----
            var view = new List<KeyValuePair<string, int>>();
            foreach (var st in allStacks)
                if (TabMatch(ItemDB.Get(st.Key))) view.Add(st);
            for (int i = 0; i < view.Count; i++)
            {
                string id = view[i].Key;
                ListRow(i * 78f, ItemDB.Get(id), view[i].Value, id == selItem, () => { selItem = id; selEquip = null; selQuick = -1; Refresh(); });
            }
            listContent.sizeDelta = new Vector2(0, Mathf.Max(576f, view.Count * 78f));
            emptyHint.text = view.Count > 0 ? "" : (tab == "quest" ? "Belum ada item quest." : "Tidak ada item di kategori ini.");
            int used = gs.SlotsUsed();
            slotInfoText.text = used + " / " + GameState.PouchCapacity;
            float frac = Mathf.Clamp01(used / (float)Mathf.Max(1, GameState.PouchCapacity));
            capFill.anchorMax = new Vector2(frac, 1f);
            capFillImg.color = frac > 0.9f ? new Color(0.95f, 0.35f, 0.3f) : frac > 0.7f ? new Color(0.98f, 0.75f, 0.3f) : th.Gold;

            RefreshCenter(gs);
            RefreshDetail(gs);
        }

        void RefreshCenter(GameState gs)
        {
            // 7 slot perlengkapan: kiri Senjata/Kepala/Sarung Tangan/Kaki, kanan Badan/Cincin/Kalung
            for (int i = 0; i < EquipSlots.Length; i++)
            {
                string slot = EquipSlots[i];
                string eid; bool has = gs.equipment.TryGetValue(slot, out eid);
                var ed = has ? ItemDB.Get(eid) : null;
                MakeSlot(centerRoot, EquipX[i], EquipY[i], 62f, ed != null ? ItemIcons.For(ed) : null, null, slot == selEquip, !has, EquipCaption[i],
                    () => { selEquip = slot; selItem = null; selQuick = -1; Refresh(); }, ed);
            }
            // panel tengah karakter
            var ch = UiStyle.Frame("CharBox", centerRoot);
            UIKit.At(ch.rectTransform, 76, 4, 104, 338);
            EnsurePreview(gs);
            var raw = new GameObject("CharView", typeof(RectTransform)).AddComponent<RawImage>();
            raw.transform.SetParent(ch.transform, false);
            raw.texture = pvRT; raw.raycastTarget = false;
            UIKit.At(raw.rectTransform, 6, 6, 92, 326);
            UiStyle.Label(ch.transform, "CL", 17, TextAnchor.LowerCenter, UiStyle.Gold, 0, 0, 104, 334).text = "Lv " + gs.level;

            int hp = 100 + 3 * (gs.level - 1);
            int stamina = 100 + Mathf.RoundToInt(0.5f * gs.level);
            string jobLabel = (gs.NoJob || (gs.requireJobChoice && !gs.jobChosen)) ? "Tanpa Job" : JobInfo.Display(gs);
            var cth = UiStyle.Cur;
            var st = UiStyle.Label(centerRoot, "Stats", 18, TextAnchor.MiddleCenter, UiStyle.Gold, 0, 346, 256, 26);
            st.text = jobLabel + "  Lv " + gs.level;
            // bar lapar & haus (data nyata) + chip ATK / DEF
            Text l1, l2;
            var f1 = UiStyle.Bar(centerRoot, 0, 374, 256, 22, new Color(0.96f, 0.66f, 0.30f), out l1, 15);
            f1.anchorMax = new Vector2(Mathf.Clamp01(gs.hunger / 100f), 1f); l1.text = "Lapar " + Mathf.RoundToInt(gs.hunger);
            var f2 = UiStyle.Bar(centerRoot, 0, 400, 256, 22, new Color(0.42f, 0.72f, 1.00f), out l2, 15);
            f2.anchorMax = new Vector2(Mathf.Clamp01(gs.thirst / 100f), 1f); l2.text = "Haus " + Mathf.RoundToInt(gs.thirst);
            var ca = Card(centerRoot, "AtkChip", 0, 428, 124, 30, cth.Chip, Color.white, false);
            UiStyle.Label(ca.transform, "T", 17, TextAnchor.MiddleCenter, UiStyle.Cream, 0, 0, 124, 30).text = "ATK  " + gs.TotalAtk;
            var cd = Card(centerRoot, "DefChip", 132, 428, 124, 30, cth.Chip, Color.white, false);
            UiStyle.Label(cd.transform, "T", 17, TextAnchor.MiddleCenter, UiStyle.Cream, 0, 0, 124, 30).text = "DEF  " + gs.TotalDef;

            // quickbar
            UiStyle.Divider(centerRoot, 0, 462, 256);
            UiStyle.Label(centerRoot, "QH", 18, TextAnchor.MiddleLeft, UiStyle.Gold, 10, 474, 240, 24).text = "Quickbar (tombol 1-6)";
            for (int i = 0; i < 6; i++)
            {
                int qi = i;
                string qid = gs.quickbar[i];
                var qd = ItemDB.Get(qid);
                MakeSlot(centerRoot, 6 + i * 42f, 504, 39f, qd != null ? ItemIcons.For(qd) : null, qd != null ? gs.Count(qid).ToString() : null,
                    selQuick == qi, qd == null, "[" + (i + 1) + "]", () => { selQuick = qi; selItem = null; selEquip = null; Refresh(); }, qd);
            }
        }

        /// <summary>"Buang": item dijatuhkan di depan pemain dan tetap ada di tanah (tidak hilang). Tahan Shift = jatuhkan semua tumpukan.</summary>
        void DropFromBag(GameState gs, string sid)
        {
            int n = TownInput.Sprint() ? gs.Count(sid) : 1;
            WorldDrops.PlayerDrop(sid, n);
        }

        void RefreshDetail(GameState gs)
        {
            if (selItem != null)
            {
                string sid = selItem;
                var def = ItemDB.Get(sid);
                float y = DetailHead(def, def.name, TypeLabel(def));
                ItemDef cmpDef = null;
                if (def.type == ItemType.Gear) { string ce; if (gs.equipment.TryGetValue(def.slot, out ce)) cmpDef = ItemDB.Get(ce); }
                y = DetailStats(y, def, null, cmpDef);
                y = DetailDesc(y, def.Desc() + "\n\nDimiliki: " + gs.Count(sid) + (def.type == ItemType.QuestItem && gs.activeQuest != null ? "\nQuest: " + gs.activeQuest.title : "") + (def.type == ItemType.Gear ? "\nPasang dulu agar slot attachment bisa dipakai." : "") + (def.sellPrice > 0 ? "   |   Harga jual " + def.sellPrice + "g" : ""), (def.type == ItemType.Food || def.type == ItemType.Drink || def.type == ItemType.Consumable) ? 462f : 546f);
                bool consumable = def.type == ItemType.Food || def.type == ItemType.Drink;
                if (consumable || def.type == ItemType.Consumable) QuickButtons(sid);
                if (consumable) BottomButtons("Pakai", () => gs.Consume(sid), true, "Buang", () => DropFromBag(gs, sid), true);
                else if (def.type == ItemType.Consumable)
                {
                    bool cx = Combat2.IsItem(sid); bool canPotion = (cx || sid == "potion" || sid == "mana_potion") && PlayerCombat.I != null;
                    BottomButtons(canPotion ? "Pakai" : "Saat hunt", () => { if (!canPotion) return; if (cx) { Combat2.UseItem(sid); return; } if (sid == "potion") PlayerCombat.I.UsePotion(); else PlayerCombat.I.UseManaPotion(); }, canPotion, "Buang", () => DropFromBag(gs, sid), true);
                }
                else if (def.type == ItemType.Gear)
                {
                    string eid; gs.equipment.TryGetValue(def.slot, out eid);
                    var old = eid != null ? ItemDB.Get(eid) : null;
                    BottomButtons("Pasang", null, true, "Buang", () => DropFromBag(gs, sid), true, () => ShowConfirm(old, def, () => gs.EquipFromInventory(sid)));
                }
                else if (def.type == ItemType.QuestItem) BottomButtons(null, null, false, "Tidak bisa dibuang", null, false);
                else BottomButtons(null, null, false, "Buang", () => DropFromBag(gs, sid), true);
            }
            else if (selEquip != null)
            {
                string slot = selEquip;
                var def = ItemDB.Get(gs.equipment[slot]);
                float y = DetailHead(def, def.name, "Terpasang: " + EquipCaption[Array.IndexOf(EquipSlots, slot)]);
                string[] dt = { "stats", "att" }; string[] dn = { "Stats", "Attachment" };
                for (int i = 0; i < 2; i++)
                {
                    string id = dt[i];
                    Text tl;
                    TabBtn(detailRoot, dn[i], i * 224, y, 218, 44, id == detailTab, () => { detailTab = id; Refresh(); }, 22, out tl);
                }
                y += 54f;
                if (detailTab == "att") AttachmentTab(gs, slot, y);
                else
                {
                    y = DetailStats(y, def, "Durability " + DurText(gs, slot));
                    y = DetailDesc(y, def.Desc());
                }
                BottomButtons("Lepas", () => gs.Unequip(slot), true, null, null, false);
            }
            else if (selQuick >= 0)
            {
                int qi = selQuick;
                var qd = ItemDB.Get(gs.quickbar[qi]);
                float y = DetailHead(qd, qd == null ? "Quickbar " + (qi + 1) : qd.name, "Quickbar " + (qi + 1));
                UiStyle.Divider(detailRoot, 0, y, 442);
                y = DetailDesc(y + 14f, qd == null ? "Slot kosong. Pilih item konsumsi di daftar lalu tekan tombol nomor untuk memasangnya." : "Dimiliki: " + gs.Count(qd.id));
                BottomButtons(qd != null ? "Kosongkan" : null, () => { gs.quickbar[qi] = null; }, qd != null, null, null, false);
            }
            else
            {
                if (gs.IsMage) AttributePanel(gs);
                else
                {
                var t = UiStyle.Label(detailRoot, "Hint", 22, TextAnchor.UpperLeft, UiStyle.Muted, 6, 10, 430, 300);
                t.fontStyle = FontStyle.Italic;
                t.text = "Pilih item di daftar, slot perlengkapan, atau slot quickbar untuk melihat detail dan aksinya.\n\nKapasitas tas " + GameState.PouchCapacity + " slot.";
                }
            }
        }

        /// <summary>Panel atribut sihir (tampil saat tidak ada item terpilih): INT & WIS dengan tombol +.</summary>
        void AttributePanel(GameState gs)
        {
            UiStyle.Label(detailRoot, "AH", 26, TextAnchor.MiddleLeft, UiStyle.Gold, 6, 6, 430, 36).text = "Atribut Sihir";
            UiStyle.Label(detailRoot, "AP", 20, TextAnchor.MiddleLeft, UiStyle.Cream, 6, 44, 430, 30).text = "Poin tersedia: " + gs.statPoints + "   (+2 tiap naik level)";
            string[] keys = { "int", "wis" };
            string[] names = { "INT  (Intelijen)", "WIS  (Kebijaksanaan)" };
            string[] desc =
            {
                "Mana maks +6 dan damage sihir +1% per poin.\nSaat ini: " + gs.intel + " poin  (+" + gs.intel * 6 + " mana, +" + gs.intel + "% damage)",
                "Pemulihan mana +4% dan biaya sihir -1% per poin.\nSaat ini: " + gs.wis + " poin  (+" + gs.wis * 4 + "% regen, -" + Mathf.Min(45, gs.wis) + "% biaya)"
            };
            for (int i = 0; i < 2; i++)
            {
                string k = keys[i];
                float ry = 84f + i * 100f;
                var row = Card(detailRoot, "AttrRow", 0, ry, 442, 92, UiStyle.Cur.Row, Color.white, false);
                UiStyle.Label(row.transform, "N", 22, TextAnchor.MiddleLeft, UiStyle.Cream, 12, 6, 300, 30).text = names[i];
                UiStyle.Label(row.transform, "D", 16, TextAnchor.UpperLeft, UiStyle.Muted, 12, 38, 320, 50).text = desc[i];
                var pb = UIKit.Btn(row.transform, "+", () => { gs.SpendStatPoint(k); Refresh(); }, gs.statPoints > 0, new Color(1f, 0.8f, 0.4f), 34);
                UIKit.At((RectTransform)pb.transform, 346, 20, 84, 52);
            }
            var t = UiStyle.Label(detailRoot, "Hint2", 18, TextAnchor.UpperLeft, UiStyle.Muted, 6, 292, 430, 200);
            t.fontStyle = FontStyle.Italic;
            t.text = "Mana " + Mathf.RoundToInt(gs.MaxMana) + "  |  Pemulihan x" + gs.ManaRegenMult.ToString("0.00") + "  |  Biaya x" + gs.ManaCostMult.ToString("0.00") + "\nBola sihir 6 mana, skill Tier 2 = 25, Tier 3 = 50.\nMana habis: serangan dasar jadi pukulan tongkat.\nMana pulih lebih cepat saat tenang (tak menyihir 2,5 dtk).";
        }

        void AttachmentTab(GameState gs, string slot, float y)
        {
            for (int i = 0; i < GameState.AttachUnlockLevel.Length; i++)
            {
                int idx = i;
                float ry = y + i * 86f;
                var row = Card(detailRoot, "AttRow", 0, ry, 442, 80, UiStyle.Cur.Row, Color.white, false);
                bool unlocked = gs.AttachUnlocked(idx);
                string aid = gs.AttachmentAt(slot, idx);
                var ad = aid != null ? ItemDB.Get(aid) : null;
                MakeSlot(row.transform, 8, 8, 64, ad != null ? ItemIcons.For(ad) : null, null, false, ad == null, null, null, ad);
                if (!unlocked)
                {
                    UiStyle.Label(row.transform, "L", 20, TextAnchor.MiddleLeft, UiStyle.Muted, 84, 0, 350, 80).text = "Terbuka pada Lv. " + GameState.AttachUnlockLevel[idx];
                }
                else if (ad != null)
                {
                    UiStyle.Label(row.transform, "N", 22, TextAnchor.MiddleLeft, UiStyle.Cream, 84, 8, 230, 34).text = ad.name;
                    UiStyle.Label(row.transform, "S", 17, TextAnchor.MiddleLeft, UiStyle.Muted, 84, 42, 230, 30).text = ad.attachKind + "  " + StatLine(ad);
                    var rb = UIKit.Btn(row.transform, "Lepas", () => { gs.Detach(slot, idx); Refresh(); }, true, new Color(0.3f, 0.36f, 0.5f), 20);
                    UIKit.At((RectTransform)rb.transform, 326, 18, 104, 44);
                }
                else
                {
                    UiStyle.Label(row.transform, "E", 20, TextAnchor.MiddleLeft, UiStyle.Muted, 84, 0, 150, 80).text = "Empty";
                    var pb = UIKit.Btn(row.transform, "+", () => OpenPicker(slot, idx), true, new Color(1f, 0.8f, 0.4f), 30);
                    UIKit.At((RectTransform)pb.transform, 326, 18, 104, 44);
                }
            }
        }

        // ---------------- Helper detail ----------------
        static string TypeLabel(ItemDef d)
        {
            switch (d.type)
            {
                case ItemType.Food: return "Makanan";
                case ItemType.Drink: return "Minuman";
                case ItemType.Material: return "Material";
                case ItemType.Consumable: return "Konsumsi";
                case ItemType.Attachment: return "Attachment (" + d.attachKind + ")";
                case ItemType.QuestItem: return "Item Quest";
                default:
                    if (d.slot == "Weapon") return "Senjata";
                    if (d.slot == "Hands") return "Sarung Tangan";
                    if (d.slot == "Ring") return "Cincin";
                    if (d.slot == "Neck") return "Kalung";
                    return "Armor (" + d.slot + ")";
            }
        }

        float DetailHead(ItemDef def, string name, string type)
        {
            Color tc = def != null ? TypeColor(def) : Color.white;
            Color gc = def != null ? ItemGrades.Tint(def) : Color.white;
            var box = Card(detailRoot, "IconBox", 0, 0, 104, 104, UiStyle.Cur.Slot, def != null ? Color.Lerp(Color.white, gc, 0.9f) : Color.white, false);
            if (def != null)
            {
                var ic = UIKit.Img("Icon", box.transform, Color.white);
                ic.sprite = ItemIcons.For(def); ic.preserveAspect = true; ic.raycastTarget = false;
                UIKit.At(ic.rectTransform, 12, 12, 80, 80);
            }
            var nm = UiStyle.Label(detailRoot, "Name", 30, TextAnchor.MiddleLeft, def != null ? Color.Lerp(UiStyle.Gold, gc, 0.6f) : UiStyle.Gold, 120, 2, 320, 54);
            nm.text = name;
            float tw = Mathf.Min(250f, 22f + type.Length * 11.5f);
            var chip = Card(detailRoot, "TypeChip", 120, 62, tw, 32, def != null ? Color.Lerp(UiStyle.Cur.PanelLo, tc, 0.4f) : UiStyle.Cur.Chip, Color.white, false);
            UiStyle.Label(chip.transform, "T", 19, TextAnchor.MiddleCenter, UiStyle.Cream, 0, 0, tw, 32).text = type;
            if (def != null)
            {
                float gw = 22f + ItemGrades.Name(def).Length * 11f;
                var gchip = Card(detailRoot, "GradeChip", 120 + tw + 8f, 62, gw, 32, Color.Lerp(UiStyle.Cur.PanelLo, gc, 0.55f), Color.Lerp(Color.white, gc, 0.9f), false);
                UiStyle.Label(gchip.transform, "G", 19, TextAnchor.MiddleCenter, Color.white, 0, 0, gw, 32).text = ItemGrades.Name(def);
            }
            return 118f;
        }

        float DetailStats(float y, ItemDef d, string extra, ItemDef cmp = null)
        {
            EnsureMax();
            UiStyle.Divider(detailRoot, 0, y, 442);
            y += 8f;
            if (d.type == ItemType.Gear || d.type == ItemType.Attachment)
            {
                if (d.atk != 0) y = StatRow(y, "ATK", "+" + d.atk + Diff(d.atk, cmp != null ? cmp.atk : 0, cmp != null), d.atk / (float)maxAtk, new Color(1f, 0.55f, 0.35f));
                if (d.def != 0) y = StatRow(y, "DEF", "+" + d.def + Diff(d.def, cmp != null ? cmp.def : 0, cmp != null), d.def / (float)maxDef, new Color(0.45f, 0.75f, 1f));
                if (d.effect != "") y = StatRow(y, "Efek", d.effVal + "% " + d.effect, -1f, Color.white);
            }
            else
            {
                if (d.hunger != 0) y = StatRow(y, "Lapar", "+" + d.hunger, Mathf.Clamp01(d.hunger / 60f), new Color(0.96f, 0.66f, 0.30f));
                if (d.thirst != 0) y = StatRow(y, "Haus", "+" + d.thirst, Mathf.Clamp01(d.thirst / 60f), new Color(0.42f, 0.72f, 1f));
                if (!string.IsNullOrEmpty(d.buff)) y = StatRow(y, "Efek", d.buff, -1f, Color.white);
            }
            if (extra != null) { int sp = extra.IndexOf(' '); y = StatRow(y, extra.Substring(0, sp), extra.Substring(sp).Trim(), -1f, Color.white); }
            UiStyle.Divider(detailRoot, 0, y, 442);
            return y + 10f;
        }

        static string Diff(int v, int old, bool has)
        {
            if (!has) return "";
            int df = v - old;
            if (df == 0) return "  <color=#C8C8C8>(=)</color>";
            return df > 0 ? "  <color=#7CFF8A>(+" + df + ")</color>" : "  <color=#FF8A7C>(" + df + ")</color>";
        }

        float StatRow(float y, string label, string value, float frac, Color barCol)
        {
            UiStyle.Label(detailRoot, "SL", 24, TextAnchor.MiddleLeft, UiStyle.Cream, 8, y, 120, 36).text = label;
            var v = UiStyle.Label(detailRoot, "SV", 24, TextAnchor.MiddleRight, UiStyle.Gold, 130, y, 306, 36);
            v.supportRichText = true; v.text = value;
            if (frac < 0f) return y + 40f;
            Text bl;
            var f = UiStyle.Bar(detailRoot, 8, y + 34, 426, 14, barCol, out bl, 10);
            f.anchorMax = new Vector2(Mathf.Clamp(frac, 0.03f, 1f), 1f);
            return y + 56f;
        }

        float DetailDesc(float y, string body, float limit = 546f)
        {
            UiStyle.Label(detailRoot, "DH", 24, TextAnchor.MiddleLeft, UiStyle.Gold, 8, y, 300, 34).text = "Deskripsi";
            float bh = Mathf.Clamp(limit - (y + 38f), 70f, 170f);
            var box = Card(detailRoot, "DescBox", 0, y + 38, 442, bh, UiStyle.Cur.Desc, Color.white, false);
            var t = UiStyle.Label(box.transform, "Desc", 19, TextAnchor.UpperLeft, UiStyle.Muted, 12, 8, 418, bh - 14f);
            t.fontStyle = FontStyle.Italic;
            t.text = body;
            return y + 216f;
        }

        void BottomButtons(string primary, Action pAct, bool pEnabled, string secondary, Action sAct, bool sEnabled, Action pCustom = null)
        {
            float y = 560f;
            float w = secondary != null && primary != null ? 214f : 442f;
            if (primary != null)
            {
                var b = UIKit.Btn(detailRoot, primary, () => { if (pCustom != null) { pCustom(); return; } if (pAct != null) pAct(); Refresh(); }, pEnabled, new Color(1f, 0.8f, 0.4f), 26);
                UIKit.At((RectTransform)b.transform, 0, y, w, 62);
            }
            if (secondary != null)
            {
                var b = UIKit.Btn(detailRoot, secondary, () => { if (sAct != null) sAct(); Refresh(); }, sEnabled, new Color(0.3f, 0.36f, 0.5f), 26);
                UIKit.At((RectTransform)b.transform, primary != null ? 228 : 0, y, w, 62);
            }
        }

        void QuickButtons(string id)
        {
            UiStyle.Label(detailRoot, "QCap", 18, TextAnchor.MiddleLeft, UiStyle.Muted, 8, 470, 300, 30).text = "Pasang ke quickbar (1-6):";
            for (int i = 0; i < 6; i++)
            {
                int qi = i;
                var b = UIKit.Btn(detailRoot, (i + 1).ToString(), () => { GameState.I.quickbar[qi] = id; Refresh(); }, true, new Color(0.3f, 0.36f, 0.5f), 22);
                UIKit.At((RectTransform)b.transform, 8 + i * 54, 504, 50, 40);
            }
        }

        // ---------------- dialog konfirmasi ----------------
        void ShowConfirm(ItemDef oldDef, ItemDef newDef, Action ok)
        {
            UIKit.Clear(confirmBody);
            MakeSlot(confirmBody, 40, 14, 76, oldDef != null ? ItemIcons.For(oldDef) : null, null, false, oldDef == null, null, null, oldDef);
            UiStyle.Label(confirmBody, "ON", 20, TextAnchor.MiddleLeft, UiStyle.Cream, 126, 14, 130, 40).text = oldDef != null ? oldDef.name : "(kosong)";
            UiStyle.Label(confirmBody, "OS", 16, TextAnchor.MiddleLeft, UiStyle.Muted, 126, 54, 130, 40).text = oldDef != null ? StatLine(oldDef) : "Terpasang";
            UiStyle.Label(confirmBody, "Arrow", 44, TextAnchor.MiddleCenter, UiStyle.Gold, 262, 20, 90, 64).text = ">>";
            MakeSlot(confirmBody, 364, 14, 76, ItemIcons.For(newDef), null, false, false, null, null, newDef);
            UiStyle.Label(confirmBody, "NN", 20, TextAnchor.MiddleLeft, UiStyle.Cream, 450, 14, 160, 40).text = newDef.name;
            UiStyle.Label(confirmBody, "NS", 16, TextAnchor.MiddleLeft, UiStyle.Muted, 450, 54, 160, 40).text = StatLine(newDef);
            var cancel = UIKit.Btn(confirmBody, "Batal", () => confirmRoot.SetActive(false), true, new Color(0.3f, 0.36f, 0.5f), 24);
            UIKit.At((RectTransform)cancel.transform, 120, 130, 180, 60);
            var yes = UIKit.Btn(confirmBody, "Pasang", () => { confirmRoot.SetActive(false); if (ok != null) ok(); Refresh(); }, true, new Color(1f, 0.8f, 0.4f), 24);
            UIKit.At((RectTransform)yes.transform, 320, 130, 180, 60);
            confirmRoot.SetActive(true);
        }

        static string StatLine(ItemDef d) { return (d.atk > 0 ? "ATK +" + d.atk + " " : "") + (d.def > 0 ? "DEF +" + d.def : ""); }

        // ---------------- Util ----------------
        static List<KeyValuePair<string, int>> BuildStacks(GameState gs)
        {
            var ids = new List<string>(gs.inventory.Keys);
            ids.Sort((a, b) =>
            {
                var da = ItemDB.Get(a); var db = ItemDB.Get(b);
                int ta = da == null ? 99 : (int)da.type;
                int tb = db == null ? 99 : (int)db.type;
                if (ta != tb) return ta.CompareTo(tb);
                return string.Compare(a, b, StringComparison.Ordinal);
            });
            var list = new List<KeyValuePair<string, int>>();
            foreach (var id in ids)
            {
                var def = ItemDB.Get(id);
                if (def == null) continue;
                int left = gs.Count(id);
                while (left > 0)
                {
                    int n = Mathf.Min(left, def.stack);
                    list.Add(new KeyValuePair<string, int>(id, n));
                    left -= n;
                }
            }
            return list;
        }

        bool TabMatch(ItemDef d)
        {
            if (d == null) return false;
            switch (tab)
            {
                case "equip": return d.type == ItemType.Gear || d.type == ItemType.Attachment;
                case "mat": return d.type == ItemType.Material;
                case "quest": return d.type == ItemType.QuestItem;
                default: return d.type != ItemType.Material && d.type != ItemType.QuestItem;
            }
        }

        static string DurText(GameState gs, string slot)
        {
            float d, m;
            gs.durability.TryGetValue(slot, out d);
            if (!gs.maxDurability.TryGetValue(slot, out m)) m = 100f;
            return Mathf.RoundToInt(d) + "/" + Mathf.RoundToInt(m);
        }
    }
}
