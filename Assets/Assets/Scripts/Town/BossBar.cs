using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HunterGuild
{
    /// <summary>
    /// Presentasi boss sebagai event: HP bar besar di tengah-atas layar (nama, fase, persen HP), banner pembuka saat bertemu,
    /// dan banner kemenangan saat boss tumbang. Dibuat otomatis di scene yang punya TownUI. Musik boss ada di Audio.Direct.
    /// </summary>
    public class BossBar : MonoBehaviour
    {
        RectTransform panel, fill, ghost;
        Text nameTxt, infoTxt, banner, bannerSub;
        Image[] pips = new Image[4];
        Monster cur;
        float ghostV = 1f, bannerUntil;
        bool built;
        Text verTxt; float diagT; bool showDiag;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Spawn();
        }
        static void OnLoaded(Scene s, LoadSceneMode m) { Spawn(); }
        static void Spawn()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<BossBar>() != null) return;
#else
            if (Object.FindObjectOfType<BossBar>() != null) return;
#endif
            new GameObject("BossBar").AddComponent<BossBar>();
        }

        static string Epithet(string id)
        {
            switch (id)
            {
                case "behemoth": return "Banteng Raksasa Hutan Perbatasan";
                case "cerberus": return "Anjing Tiga Kepala Gerbang Makam";
                case "ogre_chief": return "Raja Ogre Hutan Perbatasan";
                case "ignarok": return "Naga Magma Gunung Vulkanik";
                case "bone_king": return "Raja Tulang Padang Tulang";
                case "demon_lord": return "Penguasa Lembah Iblis";
            }
            return "BOSS";
        }

        void Build()
        {
            var root = TownUI.I.Root;
            var pf = UiStyle.Frame("BossPanel", root);
            panel = pf.rectTransform;
            UIKit.Place(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -272), new Vector2(760, 74));
            nameTxt = UIKit.Txt("BossName", panel, 24, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.7f));
            UIKit.Place(nameTxt.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -4), new Vector2(0, 30));
            var back = UIKit.Img("BossBack", panel, new Color(0.06f, 0.02f, 0.02f, 0.95f));
            UIKit.Place(back.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(-40, 20));
            var g = UIKit.Img("BossGhost", back.transform, new Color(0.95f, 0.8f, 0.3f));
            ghost = g.rectTransform; Stretch(ghost);
            var f = UIKit.Img("BossFill", back.transform, new Color(0.78f, 0.12f, 0.12f));
            fill = f.rectTransform; Stretch(fill);
            for (int i = 0; i < 4; i++)
            {
                var p = UIKit.Img("Pip" + i, back.transform, new Color(1f, 1f, 1f, 0.55f));
                p.raycastTarget = false;
                pips[i] = p;
            }
            infoTxt = UIKit.Txt("BossInfo", back.transform, 14, TextAnchor.MiddleCenter, Color.white);
            UIKit.Place(infoTxt.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            banner = UIKit.Txt("BossBanner", root, 54, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.45f));
            UIKit.Place(banner.rectTransform, new Vector2(0, 0.62f), new Vector2(1, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 90));
            bannerSub = UIKit.Txt("BossBannerSub", root, 24, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.8f));
            UIKit.Place(bannerSub.rectTransform, new Vector2(0, 0.62f), new Vector2(1, 0.62f), new Vector2(0.5f, 0.5f), new Vector2(0, -54), new Vector2(0, 40));
            banner.raycastTarget = bannerSub.raycastTarget = false;
            // penanda versi build (kecil, kanan-atas) untuk memastikan file terbaru benar-benar terpasang
            var ver = UIKit.Txt("BuildStamp", root, 14, TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.45f));
            ver.raycastTarget = false;
            ver.text = "build v35a";
            UIKit.Place(ver.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -196), new Vector2(420, 90));
            verTxt = ver;
            panel.gameObject.SetActive(false);
            banner.gameObject.SetActive(false); bannerSub.gameObject.SetActive(false);
            built = true;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        void Show(string a, string b, float dur)
        {
            banner.text = a; bannerSub.text = b;
            banner.gameObject.SetActive(true); bannerSub.gameObject.SetActive(true);
            bannerUntil = Time.unscaledTime + dur;
        }

        void Update()
        {
            if (TownUI.I == null || TownUI.I.Root == null) return;
            if (!built) Build();
            if (TownInput.DiagToggleDown()) showDiag = !showDiag;
            if (built && verTxt != null && Time.unscaledTime >= diagT)
            {
                diagT = Time.unscaledTime + 0.5f;
                verTxt.text = showDiag ? "build v35k" + HuntDiag.Text() : "";
            }
            if (built && banner.gameObject.activeSelf)
            {
                float left = bannerUntil - Time.unscaledTime;
                var c = banner.color; c.a = Mathf.Clamp01(left * 1.5f); banner.color = c;
                var c2 = bannerSub.color; c2.a = c.a; bannerSub.color = c2;
                if (left <= 0f) { banner.gameObject.SetActive(false); bannerSub.gameObject.SetActive(false); }
            }

            var pl = PlayerCombat.I;
            // boss terdekat yang sedang bertarung
            if (cur == null || (cur.IsDead && Time.unscaledTime > bannerUntil))
            {
                cur = null;
                if (pl != null)
                {
                    float best = 60f;
                    foreach (var m in Monster.All)
                    {
                        if (m == null || m.IsDead || !m.isBoss) continue;
                        float d = Vector3.Distance(m.transform.position, pl.transform.position);
                        if (d < best && (m.InCombat || m.hp < m.MaxHp - 0.5f)) { best = d; cur = m; }
                    }
                    if (cur != null)
                    {
                        ghostV = 1f;
                        Show(cur.Name.ToUpper(), Epithet(cur.DefId), 3.2f);
                        Audio.Sfx("boss_roar");
                        Cutscene.BossIntro(cur);
                    }
                }
            }

            if (cur == null) { panel.gameObject.SetActive(false); return; }
            if (cur.IsDead)
            {
                if (panel.gameObject.activeSelf)
                {
                    panel.gameObject.SetActive(false);
                    Show(cur.Name.ToUpper() + " TUMBANG", "Boss dikalahkan  -  periksa tasmu untuk drop eksklusif", 5f);
                }
                return;
            }
            // boss kabur jauh / pemain mati: sembunyikan bar tapi ingat targetnya
            bool far = pl == null || Vector3.Distance(cur.transform.position, pl.transform.position) > 90f || pl.IsDead;
            if (far) { panel.gameObject.SetActive(false); cur = null; return; }

            panel.gameObject.SetActive(true);
            float r = Mathf.Clamp01(cur.hp / cur.MaxHp);
            fill.anchorMax = new Vector2(r, 1f);
            ghostV = Mathf.MoveTowards(ghostV, r, Time.unscaledDeltaTime * 0.25f);
            ghost.anchorMax = new Vector2(Mathf.Max(ghostV, r), 1f);
            var bb = cur.GetComponent<BossBrain>();
            int pc = bb != null ? bb.PhaseCount : 0;
            string ph = bb != null && bb.Phase >= 0 ? "   Fase " + (bb.Phase + 1) + "/" + pc : "";
            nameTxt.text = cur.Name + "   -   " + Epithet(cur.DefId);
            infoTxt.text = Mathf.CeilToInt(cur.hp) + " / " + Mathf.CeilToInt(cur.MaxHp) + "   (" + Mathf.RoundToInt(r * 100f) + "%)" + ph;
            // garis batas fase
            float[] cuts = pc == 4 ? new[] { 0.7f, 0.4f, 0.15f } : cur.DefId == "ogre_chief" ? new[] { 0.66f, 0.33f } : new[] { 0.6f, 0.3f };
            for (int i = 0; i < pips.Length; i++)
            {
                bool on = bb != null && i < cuts.Length;
                pips[i].gameObject.SetActive(on);
                if (!on) continue;
                var rt = pips[i].rectTransform;
                rt.anchorMin = new Vector2(cuts[i], 0f); rt.anchorMax = new Vector2(cuts[i], 1f);
                rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(3f, 0f); rt.anchoredPosition = Vector2.zero;
            }
        }
    }

    /// <summary>Info diagnostik kecil di bawah penanda versi (posisi pemain vs tinggi terrain, status langit gunung).</summary>
    public static class HuntDiag
    {
        public static string Text()
        {
            var sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!sc.StartsWith("HuntArea")) return "";
            var p = GameObject.FindWithTag("Player");
            string t = "\n" + sc;
            if (p != null)
            {
                t += "\npemain y=" + p.transform.position.y.ToString("0.0");
                var terr = Terrain.activeTerrain;
                if (terr != null) t += "  terrain=" + (terr.SampleHeight(p.transform.position) + terr.transform.position.y).ToString("0.0");
                else t += "  TERRAIN TIDAK ADA";
            }
            if (TerrainHeightRestore.LastInfo.Length > 0) t += "\n" + TerrainHeightRestore.LastInfo;
            if (VolcanoFx.Active) t += "\n" + VolcanoFx.Diag();
            var cam = Camera.main;
            if (cam != null) t += "\nfar=" + cam.farClipPlane.ToString("0");
            return t;
        }
    }
}
