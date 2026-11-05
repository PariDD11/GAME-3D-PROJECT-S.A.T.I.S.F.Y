using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n31: Peta Dunia skematis (menu Jeda > Peta Dunia). Simpul = kota/area yang sudah dikunjungi (selain itu "???"), garis = jalur.
    /// Penanda: segitiga emas = posisimu, cincin emas = tujuan quest Guild, "?" = kasus investigasi aktif, berlian ungu = altar boss rahasia yang sudah kamu ketahui.
    /// Klik simpul untuk info; tombol Perjalanan Cepat membuka menu yang sudah ada. Posisi simpul hanya skema, bukan skala asli.
    /// </summary>
    public class WorldMap : MonoBehaviour
    {
        class Node { public string scene, name, note; public float x, y; public bool town; }

        static readonly Node[] Nodes =
        {
            new Node { scene = "StartVillage", name = "Desa Pemula", x = 150, y = 560, town = true, note = "Tempat memulai. Veteran Brann, tutorial, dan hutan pemula." },
            new Node { scene = "Town", name = "Kota Ashwatch", x = 400, y = 470, town = true, note = "Kota Guild. Resepsionis, Pandai Besi, toko, dan rumah." },
            new Node { scene = "HuntArea_G", name = "Hutan Lerai", x = 400, y = 640, note = "Area hunt pemula (Gerbang Luar Selatan)." },
            new Node { scene = "HuntArea_Desert", name = "Gurun Zahar", x = 640, y = 540, note = "Area hunt gurun (Gerbang Luar Timur)." },
            new Node { scene = "HuntArea_Swamp", name = "Rawa Terkutuk", x = 180, y = 330, note = "Area hunt rawa (Gerbang Luar Barat)." },
            new Node { scene = "HuntArea_Frontier", name = "Hutan Perbatasan", x = 520, y = 340, note = "Jalan raya antara Ashwatch dan Eldrath." },
            new Node { scene = "DungeonCity", name = "Kota Eldrath", x = 650, y = 230, town = true, note = "Kota Dungeon. Gerbang ke Menara dan area tingkat tinggi." },
            new Node { scene = "HuntArea_Volcano", name = "Gunung Vulkanik", x = 650, y = 70, note = "Area hunt api (Gerbang Utara Eldrath)." },
            new Node { scene = "HuntArea_Bone", name = "Padang Tulang", x = 900, y = 230, note = "Area hunt tulang (Gerbang Timur Eldrath)." },
            new Node { scene = "HuntArea_Demon", name = "Lembah Iblis", x = 400, y = 200, note = "Area hunt terberat (Gerbang Barat Eldrath)." },
            new Node { scene = "DungeonTower", name = "Menara Eldrath", x = 820, y = 90, note = "Dungeon 100 lantai, boss tiap 10 lantai." },
        };
        static readonly string[,] Edges =
        {
            { "StartVillage", "Town" }, { "Town", "HuntArea_G" }, { "Town", "HuntArea_Desert" }, { "Town", "HuntArea_Swamp" }, { "Town", "HuntArea_Frontier" },
            { "HuntArea_Frontier", "DungeonCity" }, { "DungeonCity", "HuntArea_Volcano" }, { "DungeonCity", "HuntArea_Bone" }, { "DungeonCity", "HuntArea_Demon" },
            { "DungeonCity", "DungeonTower" },
        };

        static readonly Color Gold = new Color(0.88f, 0.70f, 0.30f), Panel = new Color(0.035f, 0.05f, 0.085f, 0.98f);

        static WorldMap inst;
        RectTransform root, mapArea; Text info; string sel = ""; Action back;

        public static void Open(Action back)
        {
            if (inst != null) Destroy(inst.gameObject);
            var ui = TownUI.I; if (ui == null) return;
            ui.Open("PETA DUNIA", () => "", () => new List<UIEntry> { new UIEntry(" ", () => { }, false) });
            var go = new GameObject("WorldMap");
            inst = go.AddComponent<WorldMap>(); inst.back = back;
            inst.sel = SceneManager.GetActiveScene().name;
            inst.Build();
        }

        void Update() { if (TownUI.I != null && !TownUI.I.PanelOpen) { Destroy(gameObject); } }
        void OnDestroy() { if (inst == this) inst = null; if (root != null) Destroy(root.gameObject); }

        static bool Known(Node n)
        {
            var gs = GameState.I; if (gs == null) return false;
            return n.scene == SceneManager.GetActiveScene().name || FastTravel.Visited(n.scene) || n.scene == "StartVillage" || gs.flags.Contains("visited_" + n.scene);
        }

        Node Find(string scene) { foreach (var n in Nodes) if (n.scene == scene) return n; return null; }

        Vector2 P(Node n) { return new Vector2(n.x, n.y); }

        void Build()
        {
            var frame = UIKit.Img("MapFrame", TownUI.I.Root, Gold); frame.raycastTarget = false;
            var c = new Vector2(0.5f, 0.5f);
            UIKit.Place(frame.rectTransform, c, c, c, Vector2.zero, new Vector2(1420, 880));
            root = frame.rectTransform;
            var bg = UIKit.Img("Bg", root, Panel); bg.raycastTarget = true;
            UIKit.At(bg.rectTransform, 4, 4, 1412, 872);
            var title = UIKit.Txt("T", bg.transform, 34, TextAnchor.MiddleLeft, Gold); UIKit.At(title.rectTransform, 24, 10, 800, 50); title.text = "PETA DUNIA";
            var hint = UIKit.Txt("H", bg.transform, 18, TextAnchor.MiddleRight, new Color(0.7f, 0.7f, 0.75f)); UIKit.At(hint.rectTransform, 700, 14, 680, 40); hint.text = "Klik area untuk info   |   Esc tutup";
            mapArea = UIKit.Rect("Map", bg.transform); UIKit.At(mapArea, 20, 70, 1020, 780);
            var mbg = UIKit.Img("MapBg", mapArea, new Color(0.07f, 0.09f, 0.12f, 1f)); mbg.raycastTarget = false; UIKit.At(mbg.rectTransform, 0, 0, 1020, 780);
            // garis jalur
            for (int i = 0; i < Edges.GetLength(0); i++)
            {
                var a = Find(Edges[i, 0]); var b = Find(Edges[i, 1]); if (a == null || b == null) continue;
                bool both = Known(a) && Known(b);
                Line(P(a), P(b), both ? new Color(0.6f, 0.5f, 0.28f) : new Color(0.25f, 0.25f, 0.28f));
            }
            string cur = SceneManager.GetActiveScene().name;
            var gs = GameState.I;
            string questScene = null;
            if (gs != null && gs.activeQuest != null && !gs.activeQuest.completed) { var gq = GuildQuestDB.Find(gs.activeQuest.id); if (gq != null) questScene = gq.huntScene; }
            foreach (var n in Nodes)
            {
                var node = n; bool known = Known(node);
                var b = UIKit.Btn(mapArea, known ? node.name : "???", () => { sel = node.scene; RefreshInfo(); }, true,
                    known ? (node.town ? new Color(0.35f, 0.26f, 0.08f) : new Color(0.16f, 0.2f, 0.28f)) : new Color(0.14f, 0.14f, 0.16f), 17);
                UIKit.At((RectTransform)b.transform, node.x - 75, node.y - 20, 150, 40);
                string marks = "";
                if (node.scene == cur) marks += "▲ ";
                if (node.scene == questScene || (questScene == "DungeonTower" && node.scene == "DungeonTower")) marks += "◎ ";
                foreach (var c2 in Investigation.Cases) if (Investigation.State(c2) == 1 && c2.scene == node.scene) { marks += "? "; break; }
                foreach (var h in HiddenBosses.Defs) if (h.scene == node.scene && HiddenBosses.HintSeen(h) && !HiddenBosses.Defeated(h)) { marks += "◆ "; break; }
                if (marks != "")
                {
                    var m = UIKit.Txt("M", mapArea, 20, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.25f)); UIKit.At(m.rectTransform, node.x - 75, node.y - 46, 150, 26); m.text = marks;
                }
            }
            // panel info
            info = UIKit.Txt("Info", bg.transform, 22, TextAnchor.UpperLeft, new Color(0.94f, 0.9f, 0.8f)); UIKit.At(info.rectTransform, 1060, 80, 330, 560);
            var leg = UIKit.Txt("Leg", bg.transform, 17, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.75f)); UIKit.At(leg.rectTransform, 1060, 660, 330, 130);
            leg.text = "▲ posisimu\n◎ tujuan quest Guild\n? kasus investigasi aktif\n◆ altar boss rahasia diketahui";
            var ft = UIKit.Btn(bg.transform, "Perjalanan Cepat", () => { var bk = back; Close(); FastTravel.Open(() => { if (bk != null) bk(); }); }, FastTravel.Blocked() == null, new Color(0.3f, 0.22f, 0.06f), 22);
            UIKit.At((RectTransform)ft.transform, 1060, 800, 330, 50);
            RefreshInfo();
        }

        void Close() { if (TownUI.I != null) TownUI.I.Close(); }

        void RefreshInfo()
        {
            var n = Find(sel); if (n == null || info == null) { return; }
            var gs = GameState.I;
            if (!Known(n)) { info.text = "??? \n\nBelum pernah dikunjungi."; return; }
            string t = n.name + "\n\n" + n.note;
            if (n.scene == SceneManager.GetActiveScene().name) t += "\n\n▲ Kamu di sini.";
            if (n.scene.StartsWith("HuntArea")) t += "\n\nMonster dibunuh di sini: " + gs.Stat("kills_" + GameState.RegionKeyFor(n.scene)) + "\nPopulasi: " + WorldReact.Population(GameState.RegionKeyFor(n.scene));
            foreach (var c2 in Investigation.Cases) if (Investigation.State(c2) == 1 && c2.scene == n.scene) t += "\n\nKasus aktif: " + c2.title;
            foreach (var h in HiddenBosses.Defs) if (h.scene == n.scene) t += "\n\nBoss rahasia: " + (HiddenBosses.Defeated(h) ? h.name + " (dikalahkan)" : HiddenBosses.HintSeen(h) ? "altar sudah diketahui" : "belum ditemukan petunjuknya");
            t += "\n\n" + WorldReact.Conditions();
            info.text = t;
        }

        void Line(Vector2 a, Vector2 b, Color col)
        {
            var im = UIKit.Img("Line", mapArea, col); im.raycastTarget = false;
            var rt = im.rectTransform;
            UIKit.At(rt, 0, 0, 4, 4);
            Vector2 d = b - a; float len = d.magnitude;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(len, 4f);
            rt.anchoredPosition = new Vector2(a.x, -a.y);
            rt.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
    }
}
