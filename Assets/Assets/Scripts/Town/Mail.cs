using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    /// <summary>Surat antar-NPC: warga/pedagang menitipkan surat untuk NPC lain di scene yang sama. Antar suratnya, dapat upah; kadang ada surat balasan. Status tersimpan di stats.</summary>
    public static class Mail
    {
        public const int MaxSlots = 3;
        static readonly string[] Templates =
        {
            "Kepada {T},\n\nSemoga kabarmu baik. Barang yang kau pesan sudah siap. Mampirlah kalau sempat, dan jangan lupa membawa kantong yang cukup.\n\nSalam, {S}",
            "{T} yang kuhormati,\n\nCuaca belakangan ini tidak menentu dan daganganku seret. Bisakah kita bicara soal harga bulan depan? Aku titip surat ini lewat pemburu yang lewat.\n\nHormatku, {S}",
            "Untuk {T},\n\nIngat janji kita tempo hari? Aku masih menunggu jawabanmu. Jangan terlalu lama, ya.\n\n{S}",
            "{T},\n\nAda kabar yang tidak boleh kudengar sendirian: orang-orang bilang jalan di luar kota makin berbahaya. Hati-hati, dan simpan surat ini baik-baik.\n\n- {S}",
            "Kepada {T},\n\nTerima kasih untuk bantuanmu kemarin. Aku berutang budi. Kapan-kapan biar kutraktir makan di kedai.\n\nSalam hangat, {S}",
            "{T} tersayang,\n\nRumah terasa sepi tanpa kabar darimu. Kalau tidak keberatan, balaslah surat ini lewat siapa saja yang kau percaya.\n\nPeluk, {S}",
        };
        // ---- direktori NPC (nama per scene) disimpan di PlayerPrefs supaya surat bisa lintas scene ----
        static Dictionary<string, List<string>> dir;
        static void LoadDir()
        {
            if (dir != null) return; dir = new Dictionary<string, List<string>>();
            string raw = PlayerPrefs.GetString("hg_npcdir", "");
            foreach (var line in raw.Split('\n'))
            {
                int b = line.IndexOf('|'); if (b <= 0) continue;
                dir[line.Substring(0, b)] = new List<string>(line.Substring(b + 1).Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
            }
        }
        public static void Record()
        {
            LoadDir(); string sc = SceneManager.GetActiveScene().name;
            var names = new List<string>(); foreach (var n in Named()) if (!names.Contains(n.displayName) && n.displayName.IndexOf('|') < 0 && n.displayName.IndexOf(';') < 0) names.Add(n.displayName);
            if (names.Count == 0) return;
            List<string> old; if (dir.TryGetValue(sc, out old) && old.Count == names.Count && old.TrueForAll(x => names.Contains(x))) return;
            dir[sc] = names;
            var sb = new System.Text.StringBuilder(); foreach (var kv in dir) sb.Append(kv.Key).Append('|').Append(string.Join(";", kv.Value.ToArray())).Append('\n');
            PlayerPrefs.SetString("hg_npcdir", sb.ToString()); PlayerPrefs.Save();
        }
        static string NameOfHash(int h) { LoadDir(); foreach (var kv in dir) foreach (var n in kv.Value) if (Hash(n) == h) return n; return null; }
        static string SceneOfHash(int h) { LoadDir(); foreach (var kv in dir) if (Hash(kv.Key) == h) return kv.Key; return null; }
        static string SceneOfNpc(string npcName) { LoadDir(); foreach (var kv in dir) if (kv.Value.Contains(npcName)) return kv.Key; return null; }
        public static string Friendly(string sc)
        {
            switch (sc) { case "StartVillage": return "Desa Pemula"; case "Town": return "Kota Ashwatch"; case "DungeonCity": return "Kota Eldrath"; case "HuntArea_G": return "Hutan Lerai"; case "HuntArea_Tutorial": return "Hutan Pemula"; }
            return sc ?? "tempat lain";
        }
        static string Nm(int hash) { var n = Find(hash); if (n != null) return n.displayName; return NameOfHash(hash) ?? "seorang warga"; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; MailHost.Ensure(); }
        static void OnLoaded(Scene s, LoadSceneMode m) { MailHost.Ensure(); MailHost.RecordSoon(); }

        public static int Hash(string s) { unchecked { uint h = 2166136261; foreach (char ch in s ?? "") { h ^= ch; h *= 16777619; } return (int)(h & 0x7fffffff); } }
        static string K(int i, string w) { return "mail_" + i + "_" + w; }
        static int G(string k) { return GameState.I.Stat(k); }
        static int SceneIdx() { return Hash(SceneManager.GetActiveScene().name); }

        internal static List<NpcActor> Named()
        {
            var l = new List<NpcActor>();
            foreach (var n in Object.FindObjectsByType<NpcActor>(FindObjectsSortMode.None))
                if (n != null && !string.IsNullOrEmpty(n.displayName) && n.displayName != "NPC") l.Add(n);
            return l;
        }
        static NpcActor Find(int hash) { foreach (var n in Named()) if (Hash(n.displayName) == hash) return n; return null; }

        static int Active() { int c = 0; for (int i = 0; i < MaxSlots; i++) if (G(K(i, "t")) != 0) c++; return c; }
        static int FreeSlot() { for (int i = 0; i < MaxSlots; i++) if (G(K(i, "t")) == 0) return i; return -1; }

        public static List<string> TrackerLines()
        {
            var res = new List<string>(); if (GameState.I == null) return res;
            for (int i = 0; i < MaxSlots; i++)
            {
                int t = G(K(i, "t")); if (t == 0) continue;
                int sc = G(K(i, "c")); string tsc = SceneOfHash(sc);
                res.Add("Antar surat ke " + Nm(t) + (sc != 0 && sc != SceneIdx() ? " (di " + Friendly(tsc) + ")" : ""));
            }
            return res;
        }

        // ---------- kotak surat pemain (di rumah): warga mengirim surat, kadang dengan hadiah ----------
        public const int InboxMax = 5;
        static readonly string[] InboxText =
        {
            "Kepada pemburu yang baik,\n\nAku dengar kabar kerjamu di luar sana. Terima kasih sudah menjaga jalan tetap aman. Ini sedikit tanda terima kasihku.\n\nSalam, {S}",
            "Hai,\n\nBelakangan kota ramai membicarakanmu. Jaga dirimu, ya, dan jangan lupa makan teratur. Kuselipkan sesuatu di surat ini.\n\n- {S}",
            "Teman,\n\nMusim berganti, dagangan pun berganti. Kalau lewat, mampirlah. Aku selalu punya waktu untuk ngobrol.\n\nHormatku, {S}",
            "Pemburu,\n\nAda yang bilang di jalan sana masih banyak monster berkeliaran. Hati-hati. Aku titip sedikit bekal.\n\n{S}",
        };
        static string IK(int i, string w) { return "inbox_" + i + "_" + w; }
        public static int InboxCount() { if (GameState.I == null) return 0; int c = 0; for (int i = 0; i < InboxMax; i++) if (G(IK(i, "s")) != 0) c++; return c; }
        static void FillInbox()
        {
            var gs = GameState.I; LoadDir(); Record();
            var names = new List<string>(); foreach (var kv in dir) names.AddRange(kv.Value);
            int last = gs.Stat("inbox_day"); if (last == 0 || last > gs.day) last = gs.day - 1;
            int days = Mathf.Min(3, gs.day - last);
            for (int d = 0; d < days && names.Count > 0; d++)
            {
                if (Random.value > 0.6f) continue;
                int slot = -1; for (int i = 0; i < InboxMax; i++) if (G(IK(i, "s")) == 0) { slot = i; break; }
                if (slot < 0) break;
                string from = names[Random.Range(0, names.Count)];
                gs.stats[IK(slot, "s")] = Hash(from); gs.stats[IK(slot, "g")] = Random.Range(10, 41); gs.stats[IK(slot, "t")] = Random.Range(0, InboxText.Length);
            }
            gs.stats["inbox_day"] = gs.day;
        }
        /// <summary>Menu Kotak Surat (dipanggil dari menu rumah).</summary>
        public static void OpenInbox(System.Action back)
        {
            var ui = TownUI.I; var gs = GameState.I; FillInbox();
            ui.Open("Kotak Surat",
                () => "Surat masuk: " + InboxCount() + "/" + InboxMax + ". Warga yang mengenalmu kadang mengirim surat dan sedikit hadiah.",
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 0; i < InboxMax; i++)
                    {
                        int slot = i; int sh = G(IK(i, "s")); if (sh == 0) continue;
                        string sname = NameOfHash(sh) ?? "seorang warga";
                        l.Add(new UIEntry("Surat dari " + sname, () =>
                        {
                            int g = G(IK(slot, "g")); int t = G(IK(slot, "t")) % InboxText.Length;
                            foreach (var k in new[] { "s", "g", "t" }) gs.stats.Remove(IK(slot, k));
                            gs.gold += g; gs.AddStat("letters_read"); Audio.Sfx("quest_reward");
                            ui.Open("Surat", () => InboxText[t].Replace("{S}", sname) + "\n\n(+" + g + "g)", () => { var q = new List<UIEntry>(); q.Add(new UIEntry("< Kembali", () => OpenInbox(back))); return q; });
                        }));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static string Text(int s, int t, string sn, string tn)
        {
            return Templates[(s ^ t) % Templates.Length].Replace("{S}", sn).Replace("{T}", tn);
        }

        /// <summary>True bila pemain sedang membawa surat yang ditujukan ke NPC ini.</summary>
        public static bool HasDeliveryFor(NpcActor npc)
        {
            try
            {
                if (npc == null || string.IsNullOrEmpty(npc.displayName) || npc.displayName == "NPC") return false;
                int me = Hash(npc.displayName);
                for (int i = 0; i < MaxSlots; i++) if (G(K(i, "t")) == me) return true;
            }
            catch { }
            return false;
        }

        /// <summary>Menu surat di dialog NPC: ambil surat baru atau serahkan surat yang dibawa.</summary>
        public static void NpcMenu(NpcActor npc, System.Action back)
        {
            var ui = TownUI.I; var gs = GameState.I;
            if (npc == null || string.IsNullOrEmpty(npc.displayName) || npc.displayName == "NPC") { ui.Toast("Dia tidak punya urusan surat."); if (back != null) back(); return; }
            int me = Hash(npc.displayName);
            ui.Open(npc.displayName + " - Surat",
                () => "Surat dibawa: " + Active() + "/" + MaxSlots + ". Antar surat antar warga, dapat upah.",
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 0; i < MaxSlots; i++)
                    {
                        int slot = i; if (G(K(i, "t")) != me) continue;
                        int sh = G(K(i, "s"));
                        l.Add(new UIEntry("Serahkan surat dari " + Nm(sh), () => Deliver(slot, npc, back)));
                    }
                    l.Add(new UIEntry("Ada surat yang perlu dikirim?", () => Offer(npc, back), FreeSlot() >= 0));
                    l.Add(new UIEntry("Baca surat yang kubawa >", () => ReadMenu(() => NpcMenu(npc, back)), Active() > 0));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); else ui.Close(); }));
                    return l;
                });
        }

        static void Offer(NpcActor npc, System.Action back)
        {
            var gs = GameState.I; var ui = TownUI.I; int me = Hash(npc.displayName);
            int day = gs.day;
            if (gs.Stat("mailgave_" + me) == day) { Say(npc, "Hari ini aku sudah menitipkan surat. Datanglah lagi besok.", back); return; }
            LoadDir(); Record();
            var cands = new List<NpcActor>();
            foreach (var n in Named()) if (n != npc && Vector3.Distance(n.transform.position, npc.transform.position) > 18f) cands.Add(n);
            var far = new List<KeyValuePair<string, string>>();                       // (nama, scene) di scene lain
            string cur = SceneManager.GetActiveScene().name;
            foreach (var kv in dir) if (kv.Key != cur) foreach (var nm in kv.Value) far.Add(new KeyValuePair<string, string>(nm, kv.Key));
            bool cross = far.Count > 0 && (cands.Count == 0 || Random.value < 0.45f);
            if (!cross && cands.Count == 0) { Say(npc, "Tidak ada orang yang perlu kukirimi surat saat ini.", back); return; }
            string tname, msg;
            if (cross)
            {
                var f = far[Random.Range(0, far.Count)]; tname = f.Key;
                Create(me, Hash(f.Key), 70 + Random.Range(0, 30), Hash(f.Value));
                msg = "Surat ini untuk " + f.Key + " di " + Friendly(f.Value) + ". Jauh memang, makanya upahnya besar. Tolong, ya!";
            }
            else
            {
                var tgt = cands[Random.Range(0, cands.Count)]; tname = tgt.displayName;
                Create(me, Hash(tgt.displayName), 25 + Mathf.RoundToInt(Vector3.Distance(tgt.transform.position, npc.transform.position) * 0.5f), SceneIdx());
                msg = "Tolong antarkan surat ini ke " + tgt.displayName + ". Dia ada di sekitar sini. Upahnya menyusul setelah sampai!";
            }
            gs.stats["mailgave_" + me] = day;
            Audio.Sfx("quest_accept");
            Say(npc, msg, back);
        }

        static void Create(int s, int t, int reward, int targetScene)
        {
            int slot = FreeSlot(); if (slot < 0) return; var gs = GameState.I;
            gs.stats[K(slot, "s")] = s; gs.stats[K(slot, "t")] = t; gs.stats[K(slot, "c")] = targetScene; gs.stats[K(slot, "o")] = SceneIdx(); gs.stats[K(slot, "d")] = gs.day; gs.stats[K(slot, "r")] = reward;
        }

        static void Deliver(int slot, NpcActor npc, System.Action back)
        {
            var gs = GameState.I; int s = G(K(slot, "s")), t = G(K(slot, "t")), r = G(K(slot, "r")), d = G(K(slot, "d")), origin = G(K(slot, "o"));
            string sname0 = Nm(s);
            bool fast = gs.day - d <= (origin != SceneIdx() ? 3 : 1);
            int pay = r + (fast ? Mathf.RoundToInt(r * 0.25f) : 0);
            foreach (var k in new[] { "s", "t", "c", "d", "r", "o" }) gs.stats.Remove(K(slot, k));
            gs.gold += pay; gs.AddExp(8); gs.AddStat("letters"); Audio.Sfx("quest_reward");
            string msg = "Terima kasih sudah mengantar! " + sname0 + " memang bisa diandalkan.\n(+" + pay + "g" + (fast ? ", bonus cepat" : "") + ")";
            if (Random.value < 0.5f && FreeSlot() >= 0 && origin != 0)
            {
                Create(t, s, Mathf.RoundToInt(r * 1.2f), origin);
                msg += "\n\nSebentar, tolong bawa balasan ini kembali ke " + sname0 + (origin != SceneIdx() ? " di " + Friendly(SceneOfHash(origin)) : "") + ".";
            }
            Say(npc, msg, back);
        }

        static void ReadMenu(System.Action back)
        {
            var ui = TownUI.I;
            ui.Open("Surat yang kubawa",
                () => "Pilih surat untuk dibaca.",
                () =>
                {
                    var l = new List<UIEntry>();
                    for (int i = 0; i < MaxSlots; i++)
                    {
                        int s = G(K(i, "s")), t = G(K(i, "t")); if (t == 0) continue;
                        string sname = Nm(s), tname = Nm(t);
                        l.Add(new UIEntry(sname + " > " + tname, () => ui.Open("Surat", () => Text(s, t, sname, tname), () => { var q = new List<UIEntry>(); q.Add(new UIEntry("< Kembali", () => ReadMenu(back))); return q; })));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static void Say(NpcActor npc, string text, System.Action back)
        {
            TownUI.I.Open(npc.displayName, () => text, () => { var l = new List<UIEntry>(); l.Add(new UIEntry("Lanjut >", () => NpcMenu(npc, back))); return l; });
        }
    }

    /// <summary>Pelaksana coroutine kecil: mencatat nama NPC tiap scene ke direktori surat.</summary>
    public class MailHost : MonoBehaviour
    {
        static MailHost inst;
        public static void Ensure() { if (inst != null) return; var go = new GameObject("MailHost"); DontDestroyOnLoad(go); inst = go.AddComponent<MailHost>(); }
        public static void RecordSoon() { if (inst != null) { inst.StopAllCoroutines(); inst.StartCoroutine(inst.Rec()); } }
        System.Collections.IEnumerator Rec() { yield return new WaitForSeconds(4f); Mail.Record(); }
    }
}
