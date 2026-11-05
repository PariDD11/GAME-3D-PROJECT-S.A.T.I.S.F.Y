using UnityEngine;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>HUD area hunt: HP, stamina, progres quest, kompas ke target, nama zona, kondisi monster terdekat.</summary>
    public class HuntHUD : MonoBehaviour
    {
        RectTransform needle;
        Text infoText, zoneText, distText, monsterText;
        Image vignette;
        GameObject dial;
        Transform cam;

        void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
            Build();
        }

        static Image Quiet(Image im) { im.raycastTarget = false; return im; }

        static RectTransform Bar(Transform parent, Vector2 pos, Vector2 size, Color fill, out Text label)
        {
            var bg = Quiet(UIKit.Img("BarBg", parent, new Color(0, 0, 0, 0.6f)));
            UIKit.Place(bg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, size);
            var f = Quiet(UIKit.Img("Fill", bg.transform, fill));
            f.rectTransform.anchorMin = Vector2.zero;
            f.rectTransform.anchorMax = Vector2.one;
            f.rectTransform.offsetMin = Vector2.zero;
            f.rectTransform.offsetMax = Vector2.zero;
            label = UIKit.Txt("Label", bg.transform, 17, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return f.rectTransform;
        }

        void Build()
        {
            var root = TownUI.I.Root;

            vignette = Quiet(UIKit.Img("HitVignette", root, new Color(0.7f, 0f, 0f, 0f)));
            UIKit.Place(vignette.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);


            infoText = UIKit.Txt("QuestInfo", root, 22, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(infoText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(1100, 34));
            monsterText = UIKit.Txt("MonsterInfo", root, 20, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.5f));
            UIKit.Place(monsterText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -124), new Vector2(800, 30));

            // kompas kanan-atas
            var dialImg = Quiet(UIKit.Img("CompassDial", root, Color.white));
            dialImg.sprite = UiStyle.RingSprite;
            dial = dialImg.gameObject;
            UIKit.Place(dial.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -96), new Vector2(130, 130));
            var n = Quiet(UIKit.Img("Needle", dial.transform, new Color(1f, 0.85f, 0.3f)));
            UIKit.Place(n.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(10, 52));
            needle = n.rectTransform;
            var dot = Quiet(UIKit.Img("Dot", dial.transform, Color.white));
            UIKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
            distText = UIKit.Txt("Dist", root, 20, TextAnchor.MiddleRight, Color.white);
            UIKit.Place(distText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -232), new Vector2(300, 28));
            zoneText = UIKit.Txt("Zone", root, 20, TextAnchor.MiddleCenter, new Color(0.8f, 0.95f, 1f));
            zoneText.horizontalOverflow = HorizontalWrapMode.Wrap; zoneText.verticalOverflow = VerticalWrapMode.Overflow;
            UIKit.Place(zoneText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -154), new Vector2(700, 30));
        }

        static string Info(GameState gs)
        {
            var q = gs.activeQuest;
            if (q == null) return "Tidak ada quest aktif. Ambil quest di Guild.";
            if (q.completed) return "QUEST SELESAI! Kembali ke Base Camp lalu pulang ke kota.";

            string name = q.targetId;
            if (q.type == "gather") { var it = ItemDB.Get(q.targetId); if (it != null) name = it.name; }
            else { var md = MonsterDB.Get(q.targetId); if (md != null) name = md.name; }
            string verb = q.type == "hunt" ? "Buru" : (q.type == "exterm" ? "Basmi" : "Kumpulkan");
            string s = verb + " " + name + "  " + gs.questProgress + "/" + q.targetCount;
            if (q.type == "hunt" && HuntManager.I != null && true)
            {
                int t = HuntManager.I.Tracks(q.targetId);
                if (t < HuntManager.I.tracksNeeded) s += "   |   Jejak " + t + "/" + HuntManager.I.tracksNeeded;
            }
            return s + "   |   Pingsan " + q.faints + "/3";
        }

        void Update()
        {
            var gs = GameState.I;
            var pc = PlayerCombat.I;
            if (gs == null || pc == null || needle == null) return;

            float a = Mathf.Clamp01(1f - (Time.time - pc.lastHitTime) / 0.5f) * 0.35f;
            if (pc.IsDead) a = 0.55f;
            vignette.color = new Color(0.7f, 0f, 0f, a);

            bool inHunt = HuntManager.I != null;
            infoText.text = inHunt ? Info(gs) : "";

            // kompas
            Vector3 tp;
            string label;
            bool active = HuntManager.I != null && HuntManager.I.CompassActive(out tp, out label);
            if (active)
            {
                HuntManager.I.CompassActive(out tp, out label);
                Vector3 to = tp - pc.transform.position;
                to.y = 0f;
                Vector3 fwd = cam != null ? cam.forward : pc.transform.forward;
                fwd.y = 0f;
                float ang = Vector3.SignedAngle(fwd, to, Vector3.up);
                needle.localRotation = Quaternion.Euler(0, 0, -ang);
                distText.text = "";   // jarak target ditampilkan kompas HUD (GameHud)
            }
            else distText.text = "";
            dial.SetActive(false);   // jarum lama diganti kompas horizontal di tengah-atas

            string zone = HuntManager.I != null ? HuntManager.I.CurrentZone : "";
            string hint = HuntManager.I != null ? HuntManager.I.AreaHint() : "";
            zoneText.text = hint;

            // kondisi monster terdekat (GDD 3.5: tanpa bar HP, hanya ikon/teks kondisi)
            Monster near = null;
            float bd = 30f * 30f;
            foreach (var m in Monster.All)
            {
                if (m.IsDead) continue;
                float d = (m.transform.position - pc.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; near = m; }
            }
            monsterText.text = near != null && inHunt ? near.Name + ": " + near.Condition : "";
        }
    }
}
