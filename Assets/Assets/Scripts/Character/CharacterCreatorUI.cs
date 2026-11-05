using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Layar kustomisasi karakter: ras, jenis kelamin, postur, kulit, rambut, jenggot, telinga/ekor, pakaian, warna, topi,
    /// plus pratinjau armor/senjata. Pratinjau 3D dirender ke RenderTexture dari "panggung" tersembunyi di bawah dunia.
    /// Muncul otomatis saat pertama main dan bisa dibuka lagi dengan tombol G (di kota).
    /// </summary>
    public class CharacterCreatorUI : MonoBehaviour
    {
        public static CharacterCreatorUI I { get; private set; }
        public bool IsOpen { get { return root != null && root.activeSelf; } }
        public bool BlocksClose { get { return IsOpen && mandatory; } }

        class Row
        {
            public bool keepExt; public string label; public Func<string> text; public Action<int> step; public Func<bool> visible; public Func<Color> swatch;
            public RectTransform rt; public Text valueText; public Image swatchImg;
        }

        static readonly string[] RaceNames = { "Manusia", "Dwarf", "Elf", "Beastfolk" };
        static readonly string[] BeastNames = { "Kucing", "Serigala", "Rubah", "Kelinci" };
        static readonly string[] HeadPrev = { "(tanpa)", "Topi Kulit", "Helm Besi", "Helm Ksatria" };
        static readonly string[] HeadIds = { "", "leather_cap", "iron_cap", "knight_helm" };
        static readonly string[] BodyPrev = { "(tanpa)", "Armor Kulit", "Kulit Serigala", "Zirah Besi", "Zirah Ksatria" };
        static readonly string[] BodyIds = { "", "leather_armor", "wolfhide_armor", "iron_armor", "knight_armor" };
        static readonly string[] LegPrev = { "(tanpa)", "Bot Kulit", "Greaves Besi" };
        static readonly string[] LegIds = { "", "leather_boots", "iron_boots" };
        static readonly string[] WeapPrev = { "(tanpa)", "Hunter Blade", "Iron Sword", "Knight Sword", "Bone Bow", "Hunter's Bow", "Long Bow" };
        static readonly string[] WeapIds = { "", "hunter_blade", "iron_sword", "knight_sword", "bone_bow", "hunters_bow", "long_bow" };

        GameObject root;
        RectTransform listRoot;
        RawImage previewImg;
        InputField nameField;
        readonly List<Row> rows = new List<Row>();
        CharacterData edit;
        bool mandatory;
        int pHead, pBody, pLegs, pWeap;
        int openedFrame;

        // pratinjau 3D
        GameObject stage;
        Camera cam;
        RenderTexture rt;
        GameObject rigGO;
        CharacterRig rig;
        PlayerAnimator pa;
        Transform spin;
        float yaw = 160f, autoSpin = 18f;
        float spinHold;
        bool dirty;

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; DestroyStage(); }

        public static void Open(bool mandatory)
        {
            if (I == null) new GameObject("CharacterCreatorUI").AddComponent<CharacterCreatorUI>();
            I.Show(mandatory);
        }

        public void Show(bool mand)
        {
            mandatory = mand;
            edit = CharacterData.Current.Clone();
            pHead = pBody = pLegs = pWeap = 0;
            if (root == null) Build();
            root.SetActive(true);
            openedFrame = Time.frameCount;
            TownUI.I.SetExternalOpen(true);
            nameField.text = edit.name;
            BuildStage();
            Refresh();
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            DestroyStage();
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
        }

        void Update()
        {
            if (!IsOpen) return;
            if (Time.frameCount != openedFrame && !mandatory && (TownInput.CancelDown() || TownInput.CustomizeDown()) && !nameField.isFocused) { Hide(); return; }
            if (mandatory && TownUI.I != null) TownUI.I.SetExternalOpen(true);
            if (spinHold > 0f) spinHold -= Time.unscaledDeltaTime; else yaw += autoSpin * Time.unscaledDeltaTime;
            if (spin != null) spin.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (pa != null && rig != null)
            {
                bool bow = NpcKit.WeaponKind(WeapIds[pWeap]) == "bow";
                pa.ApplyPose(PlayerAnimator.Locomotion(0f, 0f, WeapIds[pWeap] != "", bow, Time.unscaledTime, false));
            }
            if (dirty) { dirty = false; RebuildPreviewRig(); }
        }

        // ------------------------------------------------------------ UI
        void Build()
        {
            var bg = UIKit.Img("CreatorWindow", TownUI.I.Root, new Color(0.07f, 0.06f, 0.1f, 0.98f));
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(bg.rectTransform, c, c, c, Vector2.zero, new Vector2(1240, 780));
            root = bg.gameObject;

            var title = UIKit.Txt("Title", root.transform, 34, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.4f));
            UIKit.At(title.rectTransform, 24, 10, 800, 50);
            title.text = "Kustomisasi Karakter";

            // kolom pilihan
            listRoot = UIKit.Rect("Rows", root.transform);
            UIKit.At(listRoot, 24, 66, 640, 700);
            DefineRows();

            // pratinjau
            var frame = UIKit.Img("PreviewFrame", root.transform, new Color(0.14f, 0.1f, 0.22f, 1f));
            UIKit.At(frame.rectTransform, 690, 66, 520, 560);
            previewImg = UIKit.Rect("Preview", frame.transform).gameObject.AddComponent<RawImage>();
            var prt = previewImg.rectTransform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = new Vector2(4, 4); prt.offsetMax = new Vector2(-4, -4);

            var rl = UIKit.Btn(root.transform, "< Putar", () => { yaw -= 35f; spinHold = 4f; }, true, new Color(0.22f, 0.25f, 0.32f), 20);
            UIKit.At((RectTransform)rl.transform, 690, 634, 100, 38);
            var rr = UIKit.Btn(root.transform, "Putar >", () => { yaw += 35f; spinHold = 4f; }, true, new Color(0.22f, 0.25f, 0.32f), 20);
            UIKit.At((RectTransform)rr.transform, 800, 634, 100, 38);

            var nl = UIKit.Txt("NameLabel", root.transform, 22, TextAnchor.MiddleLeft, Color.white);
            UIKit.At(nl.rectTransform, 920, 634, 80, 38);
            nl.text = "Nama:";
            var nbg = UIKit.Img("NameBox", root.transform, new Color(0.15f, 0.15f, 0.2f, 1f));
            UIKit.At(nbg.rectTransform, 1000, 634, 210, 38);
            nameField = nbg.gameObject.AddComponent<InputField>();
            var nt = UIKit.Txt("Text", nbg.transform, 22, TextAnchor.MiddleLeft, Color.white);
            nt.rectTransform.anchorMin = Vector2.zero; nt.rectTransform.anchorMax = Vector2.one; nt.rectTransform.offsetMin = new Vector2(8, 2); nt.rectTransform.offsetMax = new Vector2(-8, -2);
            nt.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameField.textComponent = nt; nameField.characterLimit = 16; nameField.targetGraphic = nbg;
            nameField.onValueChanged.AddListener(v => { if (edit != null) edit.name = string.IsNullOrEmpty(v) ? "Hunter" : v; });

            var rnd = UIKit.Btn(root.transform, "Acak", () => { edit.Randomize(); Refresh(); }, true, new Color(0.3f, 0.3f, 0.5f), 22);
            UIKit.At((RectTransform)rnd.transform, 690, 690, 130, 52);
            var ok = UIKit.Btn(root.transform, "Simpan & Mulai", Confirm, true, new Color(0.2f, 0.5f, 0.3f), 24);
            UIKit.At((RectTransform)ok.transform, 830, 690, 250, 52);
            var cancel = UIKit.Btn(root.transform, "Batal (G)", () => { if (!mandatory) Hide(); }, true, new Color(0.45f, 0.22f, 0.22f), 20);
            UIKit.At((RectTransform)cancel.transform, 1090, 690, 120, 52);

            var hint = UIKit.Txt("Hint", root.transform, 17, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.75f));
            UIKit.At(hint.rectTransform, 690, 748, 520, 26);
            hint.text = "Armor & senjata asli dipasang lewat Tab (Tas). Baris 'Pratinjau' hanya untuk melihat.";
            root.SetActive(false);
        }

        void AddRow(string label, Func<string> text, Action<int> step, Func<bool> visible = null, Func<Color> swatch = null, bool keepExt = false)
        {
            var r = new Row { keepExt = keepExt, label = label, text = text, step = step, visible = visible, swatch = swatch };
            var rt2 = UIKit.Rect("Row_" + label, listRoot);
            r.rt = rt2;
            var l = UIKit.Txt("Label", rt2, 22, TextAnchor.MiddleLeft, new Color(0.9f, 0.9f, 0.95f));
            UIKit.At(l.rectTransform, 0, 0, 230, 38); l.text = label;
            var bl = UIKit.Btn(rt2, "<", () => { r.step(-1); Refresh(); }, true, new Color(0.25f, 0.27f, 0.36f), 22);
            UIKit.At((RectTransform)bl.transform, 240, 0, 44, 38);
            var vt = UIKit.Txt("Value", rt2, 21, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.7f));
            UIKit.At(vt.rectTransform, 290, 0, 250, 38);
            r.valueText = vt;
            if (swatch != null)
            {
                var sw = UIKit.Img("Swatch", rt2, Color.white);
                UIKit.At(sw.rectTransform, 546, 3, 32, 32);
                r.swatchImg = sw;
            }
            var br = UIKit.Btn(rt2, ">", () => { r.step(1); Refresh(); }, true, new Color(0.25f, 0.27f, 0.36f), 22);
            UIKit.At((RectTransform)br.transform, swatch != null ? 586 : 546, 0, 44, 38);
            rows.Add(r);
        }

        static int Wrap(int v, int n) { return ((v % n) + n) % n; }

        void DefineRows()
        {
            AddRow("Ras", () => RaceNames[edit.race], d => { edit.race = Wrap(edit.race + d, 4); edit.skin = Wrap(edit.skin, NpcKit.SkinCount(edit.race)); });
            AddRow("Jenis Kelamin", () => edit.female ? "Wanita" : "Pria", d => { edit.female = !edit.female; if (edit.female) edit.beard = 0; }, null, null, true);
            AddRow("Postur Badan", () => CharacterData.BuildNames[edit.build], d => edit.build = Wrap(edit.build + d, 3));
            AddRow("Warna Kulit", () => "Nada " + (edit.skin + 1), d => edit.skin = Wrap(edit.skin + d, NpcKit.SkinCount(edit.race)), () => edit.race != 3, () => NpcKit.SkinColor(edit.race, edit.skin));
            AddRow("Warna Bulu", () => "Corak " + (edit.skin + 1), d => edit.skin = Wrap(edit.skin + d, 3), () => edit.race == 3, () => NpcKit.FurColor(edit.beast, edit.skin));
            AddRow("Telinga & Ekor", () => BeastNames[edit.beast], d => edit.beast = Wrap(edit.beast + d, 4), () => edit.race == 3);
            AddRow("Gaya Rambut", () => CharacterData.HairNames[edit.hair], d => edit.hair = Wrap(edit.hair + d, CharacterData.HairNames.Length));
            AddRow("Warna Rambut", () => "Warna " + (edit.hairCol + 1), d => edit.hairCol = Wrap(edit.hairCol + d, NpcKit.HairColorCount), () => edit.race != 3, () => NpcKit.HairColor(edit.hairCol));
            AddRow("Jenggot", () => CharacterData.BeardNames[edit.beard], d => edit.beard = Wrap(edit.beard + d, 4), () => !edit.female && edit.race != 3);
            AddRow("Pakaian", () => CharacterData.OutfitLabel[CharacterData.OutfitChoices[OutfitIndex()]], d => edit.outfit = CharacterData.OutfitChoices[Wrap(OutfitIndex() + d, CharacterData.OutfitChoices.Length)]);
            AddRow("Warna Utama", () => "Warna " + (edit.cA + 1), d => edit.cA = Wrap(edit.cA + d, NpcKit.FabricCount), null, () => NpcKit.FabricColor(edit.cA));
            AddRow("Warna Kedua", () => "Warna " + (edit.cB + 1), d => edit.cB = Wrap(edit.cB + d, NpcKit.FabricCount), null, () => NpcKit.FabricColor(edit.cB));
            AddRow("Warna Aksen", () => "Warna " + (edit.cC + 1), d => edit.cC = Wrap(edit.cC + d, NpcKit.FabricCount), null, () => NpcKit.FabricColor(edit.cC));
            AddRow("Topi / Penutup Kepala", () => CharacterData.HatNames[HatIndex()], d => edit.hat = CharacterData.HatChoices[Wrap(HatIndex() + d, CharacterData.HatChoices.Length)]);
            AddRow("Pratinjau: Kepala", () => HeadPrev[pHead], d => { pHead = Wrap(pHead + d, HeadPrev.Length); });
            AddRow("Pratinjau: Badan", () => BodyPrev[pBody], d => { pBody = Wrap(pBody + d, BodyPrev.Length); });
            AddRow("Pratinjau: Kaki", () => LegPrev[pLegs], d => { pLegs = Wrap(pLegs + d, LegPrev.Length); });
            AddRow("Pratinjau: Senjata", () => WeapPrev[pWeap], d => { pWeap = Wrap(pWeap + d, WeapPrev.Length); });
        }

        int OutfitIndex() { int i = Array.IndexOf(CharacterData.OutfitChoices, edit.outfit); return i < 0 ? 0 : i; }
        int HatIndex() { int i = Array.IndexOf(CharacterData.HatChoices, edit.hat); return i < 0 ? 0 : i; }

        void Refresh()
        {
            float y = 0f;
            bool extMode = HGModelConfig.UsesExternalPlayer;
            foreach (var r in rows)
            {
                bool vis = (!extMode || r.keepExt) && (r.visible == null || r.visible());
                r.rt.gameObject.SetActive(vis);
                if (!vis) continue;
                UIKit.At(r.rt, 0, y, 640, 38);
                y += 39f;
                r.valueText.text = r.text();
                if (r.swatchImg != null) r.swatchImg.color = r.swatch();
            }
            dirty = true;
        }

        void Confirm()
        {
            if (edit == null) return;
            if (string.IsNullOrEmpty(edit.name)) edit.name = "Hunter";
            edit.Save();
            if (PlayerAvatar.I != null) PlayerAvatar.I.Rebuild();
            Hide();
            if (TownUI.I != null) TownUI.I.Toast("Penampilan disimpan. Tekan G kapan saja untuk mengubahnya.");
        }

        // ------------------------------------------------------------ pratinjau 3D
        void BuildStage()
        {
            DestroyStage();
            stage = new GameObject("CreatorStage");
            stage.transform.position = new Vector3(0f, -800f, 0f);
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Platform"; disc.transform.SetParent(stage.transform, false);
            disc.transform.localScale = new Vector3(1.5f, 0.04f, 1.5f); disc.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            var col = disc.GetComponent<Collider>(); if (col != null) Destroy(col);
            var dr = disc.GetComponent<Renderer>();
            if (NpcKit.RuntimeShader != null) { var m = new Material(NpcKit.RuntimeShader); m.color = new Color(0.3f, 0.26f, 0.4f); dr.sharedMaterial = m; }

            spin = new GameObject("Spin").transform; spin.SetParent(stage.transform, false);

            var camGo = new GameObject("PreviewCam"); camGo.transform.SetParent(stage.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.05f, 4.3f);
            camGo.transform.localRotation = Quaternion.Euler(4f, 180f, 0f);
            cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 30f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.19f, 0.13f, 0.32f, 1f);
            rt = new RenderTexture(520, 560, 16, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            previewImg.texture = rt;
            AddLight(new Vector3(2.5f, 3.5f, 3.5f), 1.6f); AddLight(new Vector3(-3f, 2f, 3f), 0.9f); AddLight(new Vector3(0f, 2f, -3f), 0.8f);
            dirty = true;
        }

        void AddLight(Vector3 p, float intensity)
        {
            var g = new GameObject("L"); g.transform.SetParent(stage.transform, false); g.transform.localPosition = p;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.range = 14f; l.intensity = intensity * 2.4f; l.color = new Color(1f, 0.96f, 0.9f);
        }

        void DestroyStage()
        {
            if (stage != null) Destroy(stage);
            if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
            stage = null; rigGO = null; rig = null; pa = null; spin = null;
        }

        void RebuildPreviewRig()
        {
            if (stage == null || edit == null) return;
            if (rigGO != null) Destroy(rigGO);
            if (HGModelConfig.UsesExternalPlayer)
            {
                var pf = ExternalModelDriver.LoadPrefab(edit.female, 0);
                if (pf != null)
                {
                    var inst = Instantiate(pf, spin, false); inst.name = "PreviewExt";
                    foreach (var c in inst.GetComponentsInChildren<Collider>()) Destroy(c);
                    var an = inst.GetComponentInChildren<Animator>(); var cfg = HGModelConfig.Load();
                    if (an != null && cfg.playerController != null) an.runtimeAnimatorController = cfg.playerController;
                    spin.localScale = Vector3.one;
                    ExternalModelDriver.FitTo(inst.transform, 1.9f);
                    rigGO = inst; rig = null; pa = null;
                    return;
                }
            }
            var spec = edit.ToSpec();
            NpcKit.ApplyEquipment(spec, HeadIds[pHead], BodyIds[pBody], LegIds[pLegs]);
            var go = NpcKit.Spawn(spin, "PreviewRig", Vector3.zero, 0f, spec, false, false);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            float sc = 1.9f / go.GetComponent<CharacterRig>().height;
            spin.localScale = Vector3.one * sc;
            rigGO = go; rig = go.GetComponent<CharacterRig>();
            var pv = new GameObject("Pv").transform; pv.SetParent(go.transform, false);
            pa = go.AddComponent<PlayerAnimator>();
            pa.Init(null, rig, pv, 0f);
            string w = WeapIds[pWeap];
            string kind = NpcKit.WeaponKind(w);
            if (kind == "sword") { Prop(rig.handR, NpcKit.Weapon(w), Vector3.zero, PlayerAvatar.SwordGrip); Prop(rig.foreL, NpcKit.Weapon("@shield"), new Vector3(-0.055f, -0.13f, 0.02f), Quaternion.Euler(0f, 0f, 90f)); }
            else if (kind == "bow") Prop(rig.handL, NpcKit.Weapon(w), Vector3.zero, PlayerAvatar.BowGrip);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.gameObject.layer = 0;
        }

        static void Prop(Transform parent, NpcKit.WeaponModel m, Vector3 pos, Quaternion rot)
        {
            var g = new GameObject(m.id);
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = m.mesh;
            g.AddComponent<MeshRenderer>().sharedMaterials = m.mats;
            g.transform.localPosition = pos; g.transform.localRotation = rot;
        }
    }
}
