using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Tekan E pada NPC kota untuk mengobrol: kalimat acak sesuai ras, peran, dan waktu.</summary>
    public class NpcChat : Interactable
    {
        NpcActor actor; static NpcActor Current;
        void Awake() { actor = GetComponent<NpcActor>(); prompt = "Ajak bicara"; radius = 2.6f; }
        public override void Interact()
        {
            if (actor == null) actor = GetComponent<NpcActor>();
            if (actor == null) { TownUI.I.Open("Warga", () => "...", () => new List<UIEntry> { new UIEntry("Tutup", () => TownUI.I.Close()) }); return; }
            string who = actor.displayName + "  (" + RaceTitle(actor.race) + ")";
            string t1 = NpcTalk.Greet(actor);
            var an = GetComponent<NpcAnimator>(); if (an != null) an.Wave(1.5f);
            Audio.Voice(NpcVoice.KeyForCitizen(actor.outfit, actor.displayName, actor.female), t1);
            Current = actor;
            Show(who, t1);
        }

        // obrolan berantai: NPC menjawab sesuai topik yang dipilih, tanpa pengulangan, lalu menu topik muncul lagi
        static void Show(string who, string text)
        {
            var cur = Current;
            TownUI.I.Open(who, () => text, () =>
            {
                var l = new List<UIEntry>();
                if (cur != null)
                {
                    l.Add(new UIEntry("Kabar hari ini?", () => Show(who, NpcTalk.Topic(cur, 0))));
                    l.Add(new UIEntry("Ceritakan tentang dirimu", () => Show(who, NpcTalk.Topic(cur, 1))));
                    l.Add(new UIEntry("Soal pekerjaanmu", () => Show(who, NpcTalk.Topic(cur, 2))));
                    l.Add(new UIEntry("Ada rumor?", () => Show(who, NpcTalk.Topic(cur, 3))));
                    if (!NameBook.Has(cur.displayName)) l.Add(new UIEntry("Catat namanya di buku", () => { NameBook.Record(cur.displayName, NpcTalk.Fact(cur)); Show(who, NpcTalk.Topic(cur, 1)); }));
                    l.Add(new UIEntry("Ada surat?", () => Mail.NpcMenu(cur, () => TownUI.I.Close())));
                    foreach (var we in Investigation.WitnessEntries(cur, () => TownUI.I.Close())) l.Add(we);
                }
                l.Add(new UIEntry("Pamit", () => { if (cur != null) Show2Bye(who, NpcTalk.Farewell(cur)); else TownUI.I.Close(); }));
                return l;
            });
        }
        static void Show2Bye(string who, string text)
        {
            TownUI.I.Open(who, () => text, () => new List<UIEntry> { new UIEntry("Tutup", () => TownUI.I.Close()) });
        }
        public static string RaceTitle(int r) { return r == 1 ? "Dwarf" : r == 2 ? "Elf" : r == 3 ? "Beastfolk" : "Manusia"; }
    }
}
