using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Perpustakaan: buku memberi SP (4 buku x 1 SP, GDD 4.4), buku resep, dan lore Arc 1.</summary>
    public class Library : Interactable
    {
        class Book { public string id, title, job; public int price, sp; }

        static Book B(string id, string title, int price, int sp, string job = "") { return new Book { id = id, title = title, price = price, sp = sp, job = job }; }

        static readonly Book[] Books =
        {
            B("book_basic",   "Dasar Membaca Pola Monster", 50, 1),
            B("book_stamina", "Mengatur Napas dan Stamina", 50, 1),
            B("book_guard",   "Seni Bertahan di Garis Depan", 60, 1),
            B("book_track",   "Jejak dan Pelacakan", 60, 1),
            B("book_recipe",  "Resep Hidangan Lanjutan", 80, 0),
            B("book_elem",    "Dasar Sihir Elemental", 70, 1),
            B("book_fire",    "Kitab Api Purba (khusus Penyihir Api)", 90, 1, "FireMage"),
            B("book_earth",   "Kitab Batu Tua (khusus Penyihir Tanah)", 90, 1, "EarthMage"),
        };

        const string Lore =
            "Ashwatch awalnya hanya pos kecil di ujung hutan. Para pemburu berhimpun dalam Guild untuk menjaga kota dan mencari nafkah dari hasil hutan. " +
            "Yang paling berani menyebut diri mereka Hunter. Konon seorang rakyat biasa yang dijuluki Sang Perintis-lah yang pertama menyusun cara Guild menguji dan memberi peringkat para hunter. " +
            "Catatan lengkapnya tidak dipajang di rak umum. Ia tersimpan di tempat lain, atau terukir di reruntuhan tua.";

        public override void Interact() { ShopHub.Open(this, OpenMain, "Lihat koleksi buku."); }

        void OpenMain()
        {
            var gs = GameState.I;
            TownUI.I.Open("Perpustakaan",
                () => "Gold: " + gs.gold + "   Skill Point: " + gs.skillPoints + "\nBuku keterampilan memberi +1 SP (sekali baca). Kitab sihir khusus penyihir sesuai elemennya.",
                () =>
                {
                    var l = new List<UIEntry>();
                    foreach (var b in Books)
                    {
                        var book = b;
                        bool read = gs.flags.Contains(book.id);
                        string effect = book.sp > 0 ? "+" + book.sp + " SP" : "membuka resep lanjutan";
                        string label = read ? book.title + "  (sudah dibaca)" : book.title + "  (" + effect + ")  - " + book.price + "g";
                        bool jobOk = book.job == "" || gs.job == book.job;
                        l.Add(new UIEntry(label, () => Read(book), !read && jobOk && gs.gold >= book.price));
                    }
                    l.Add(new UIEntry("Baca: Sejarah Guild (gratis) >", OpenLore));
                    l.Add(new UIEntry("Arsip Lore Ashwatch (" + LoreDB.UnlockedCount + "/" + LoreDB.All.Length + ") >", () => LoreCodex.OpenArchive(OpenMain)));
                    return l;
                });
        }

        void Read(Book b)
        {
            var gs = GameState.I;
            if (!gs.TrySpend(b.price)) return;
            gs.flags.Add(b.id);
            gs.skillPoints += b.sp;
            TownUI.I.Toast(b.sp > 0 ? "Kamu belajar sesuatu. +" + b.sp + " SP." : "Resep lanjutan terbuka (dipakai saat sistem cooking dibuat).");
        }

        void OpenLore()
        {
            TownUI.I.Open("Sejarah Guild", () => Lore, () =>
            {
                var l = new List<UIEntry>();
                l.Add(new UIEntry("< Kembali", OpenMain));
                return l;
            });
        }
    }
}
