using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n26: rumah sebagai markas. Peti Simpanan (perabot "stash"), Rak Trofi (perabot "trophy": boss yang sudah dikalahkan)
    /// dan Pajangan (Akuarium untuk ikan, Meja Kayu untuk masakan yang pernah dibuat). Isi peti disimpan di GameState.stats
    /// (kunci stash_&lt;item&gt;), jadi ikut save. Peti bersama antar rumah milikmu.
    /// </summary>
    public static class PropertyExtra
    {
        static GameState GS { get { return GameState.I; } }
        static string Nm(string id) { var d = ItemDB.Get(id); return d != null ? d.name : id; }

        // ---------------- Peti Simpanan
        static List<string> StashIds()
        {
            var l = new List<string>();
            foreach (var kv in GS.stats) if (kv.Key.StartsWith("stash_") && kv.Value > 0) l.Add(kv.Key.Substring(6));
            l.Sort(StringComparer.Ordinal);
            return l;
        }
        public static int StashCount { get { int n = 0; foreach (var kv in GS.stats) if (kv.Key.StartsWith("stash_")) n += Mathf.Max(0, kv.Value); return n; } }

        public static void OpenStash(Action back)
        {
            var ui = TownUI.I;
            ui.Open("PETI SIMPANAN",
                () => "Isi peti: " + StashCount + " barang. Barang yang disimpan aman dan tidak memenuhi tas.\nPilih barang untuk dipindahkan (semua jumlahnya).",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Simpan dari tas >", () => OpenDeposit(back)));
                    l.Add(new UIEntry("Ambil dari peti >", () => OpenWithdraw(back)));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        static void OpenDeposit(Action back)
        {
            var ui = TownUI.I;
            ui.Open("SIMPAN KE PETI", () => "Pilih barang dari tas untuk disimpan.",
                () =>
                {
                    var l = new List<UIEntry>();
                    var ids = new List<string>(GS.inventory.Keys); ids.Sort(StringComparer.Ordinal);
                    foreach (var id0 in ids)
                    {
                        string id = id0; int n = GS.Count(id); if (n <= 0) continue;
                        l.Add(new UIEntry(Nm(id) + " x" + n, () => { int c = GS.Count(id); if (c > 0 && GS.RemoveItem(id, c)) { GS.stats["stash_" + id] = GS.Stat("stash_" + id) + c; Audio.Sfx("ui_open"); } OpenDeposit(back); }));
                    }
                    if (l.Count == 0) l.Add(new UIEntry("(tas kosong)", () => { }, false));
                    l.Add(new UIEntry("< Kembali", () => OpenStash(back)));
                    return l;
                });
        }

        static void OpenWithdraw(Action back)
        {
            var ui = TownUI.I;
            ui.Open("AMBIL DARI PETI", () => "Pilih barang untuk diambil ke tas.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var id0 in StashIds())
                    {
                        string id = id0; int n = GS.Stat("stash_" + id);
                        l.Add(new UIEntry(Nm(id) + " x" + n, () =>
                        {
                            int c = GS.Stat("stash_" + id); int take = 0;
                            for (int i = 0; i < c; i++) { if (!GS.HasSpace(id, 1)) break; GS.AddItem(id, 1); take++; }
                            GS.stats["stash_" + id] = c - take;
                            if (take < c && ui != null) ui.Toast("Tas penuh. Sisa " + (c - take) + " tetap di peti.");
                            OpenWithdraw(back);
                        }));
                    }
                    if (l.Count == 0) l.Add(new UIEntry("(peti kosong)", () => { }, false));
                    l.Add(new UIEntry("< Kembali", () => OpenStash(back)));
                    return l;
                });
        }

        // ---------------- Rak Trofi
        static bool IsBossId(string id)
        {
            return id.StartsWith("hb_") || id.StartsWith("tbs_") || id.StartsWith("srank_") || id == "alpha_wolf" || id == "giant_boar" || id == "bandit_leader" || id == "sand_golem" ||
                   id == "swamp_troll" || id == "necromancer" || id == "arena_warden" || id == "gladiator_elite";
        }

        public static void OpenTrophies(Action back)
        {
            var ui = TownUI.I;
            ui.Open("RAK TROFI",
                () =>
                {
                    int got = 0, tot = 0;
                    foreach (var d in MonsterDB.All()) if (IsBossId(d.id)) { tot++; if (GS.Stat("bk_" + d.id) > 0) got++; }
                    return "Boss yang pernah kamu kalahkan: " + got + "/" + tot + ".\n(Trofi baru muncul setelah boss dikalahkan.)";
                },
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var d in MonsterDB.All())
                    {
                        if (!IsBossId(d.id)) continue;
                        int n = GS.Stat("bk_" + d.id);
                        l.Add(new UIEntry((n > 0 ? "[x] " : "[ ] ") + (n > 0 ? d.name + "  (dikalahkan " + n + " kali)" : "???"), () => { }, false));
                    }
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }

        // ---------------- Pajangan ikan / masakan
        public static void OpenDisplay(Action back, bool fish)
        {
            var ui = TownUI.I;
            string prefix = fish ? "fc_" : "cc_";
            ui.Open(fish ? "AKUARIUM" : "MEJA MASAKAN",
                () => fish ? "Ikan yang pernah kamu tangkap tampil di sini." : "Masakan yang pernah kamu buat tampil di sini.",
                () =>
                {
                    var l = new List<UIEntry>();
                    var ids = new List<string>();
                    foreach (var kv in GS.stats) if (kv.Key.StartsWith(prefix) && kv.Value > 0) ids.Add(kv.Key.Substring(prefix.Length));
                    ids.Sort(StringComparer.Ordinal);
                    foreach (var id in ids) l.Add(new UIEntry(Nm(id) + "  x" + GS.Stat(prefix + id), () => { }, false));
                    if (ids.Count == 0) l.Add(new UIEntry(fish ? "(belum ada ikan; pergi memancing)" : "(belum ada masakan; coba memasak)", () => { }, false));
                    l.Add(new UIEntry("< Kembali", () => { if (back != null) back(); }));
                    return l;
                });
        }
    }
}
