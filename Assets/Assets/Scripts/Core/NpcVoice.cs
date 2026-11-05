using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Suara NPC. Tanpa klip asli, tiap NPC "mengoceh" dengan rangkaian blip bernada khas (seperti game RPG klasik); panjang ocehan mengikuti panjang kalimat,
    /// dan tanda tanya menaikkan nada akhir. Klip asli: Resources/Audio/voice_{kunci}.wav (varian _1.._3) dan akan dipakai sebagai gantinya.
    /// NPC penting punya kunci tetap (resepsionis, guild master, pandai besi, ...); warga umum memakai citizen_m / citizen_f dengan nada dari nama mereka.
    /// </summary>
    public static class NpcVoice
    {
        public class VoiceDef { public string key; public float pitch, tempo; public int wave; }

        public static readonly int[] Steps = { 0, 2, 4, -2, 5, -4, 7, 3, 0, -1, 9, 2 };

        static readonly Dictionary<string, VoiceDef> defs = new Dictionary<string, VoiceDef>
        {
            { "receptionist", new VoiceDef { key = "receptionist", pitch = 330f, tempo = 0.075f, wave = 0 } },
            { "guildmaster",  new VoiceDef { key = "guildmaster",  pitch = 105f, tempo = 0.11f,  wave = 2 } },
            { "trainer",      new VoiceDef { key = "trainer",      pitch = 165f, tempo = 0.07f,  wave = 1 } },
            { "referee",      new VoiceDef { key = "referee",      pitch = 140f, tempo = 0.08f,  wave = 1 } },
            { "appraiser",    new VoiceDef { key = "appraiser",    pitch = 260f, tempo = 0.07f,  wave = 0 } },
            { "smith",        new VoiceDef { key = "smith",        pitch = 95f,  tempo = 0.1f,   wave = 2 } },
            { "innkeeper",    new VoiceDef { key = "innkeeper",    pitch = 210f, tempo = 0.09f,  wave = 0 } },
            { "merchant",     new VoiceDef { key = "merchant",     pitch = 185f, tempo = 0.065f, wave = 2 } },
            { "librarian",    new VoiceDef { key = "librarian",    pitch = 285f, tempo = 0.1f,   wave = 0 } },
            { "cook",         new VoiceDef { key = "cook",         pitch = 150f, tempo = 0.075f, wave = 2 } },
            { "priest",       new VoiceDef { key = "priest",       pitch = 195f, tempo = 0.12f,  wave = 0 } },
            { "guard",        new VoiceDef { key = "guard",        pitch = 115f, tempo = 0.08f,  wave = 1 } },
            { "bard",         new VoiceDef { key = "bard",         pitch = 300f, tempo = 0.07f,  wave = 0 } },
            { "citizen_m",    new VoiceDef { key = "citizen_m",    pitch = 140f, tempo = 0.08f,  wave = 0 } },
            { "citizen_f",    new VoiceDef { key = "citizen_f",    pitch = 235f, tempo = 0.08f,  wave = 0 } },
        };

        static readonly Dictionary<string, AudioClip> blips = new Dictionary<string, AudioClip>();

        public static VoiceDef Def(string key)
        {
            VoiceDef d;
            if (defs.TryGetValue(key, out d)) return d;
            // kunci dinamis "citizen_m#nama": nada dari hash nama
            int h = Mathf.Abs(key.GetHashCode());
            bool f = key.StartsWith("citizen_f");
            var dd = new VoiceDef { key = key, pitch = (f ? 200f : 120f) + (h % 60), tempo = 0.07f + (h % 5) * 0.006f, wave = h % 3 == 0 ? 2 : 0 };
            defs[key] = dd;
            return dd;
        }

        public static AudioClip Blip(VoiceDef v, float freq, float amp)
        {
            string k = v.key + "|" + Mathf.RoundToInt(freq) + "|" + (amp > 1.1f ? "!" : "");
            AudioClip c;
            if (blips.TryGetValue(k, out c)) return c;
            var b = BlipSamples(v, freq, amp);
            c = AudioClip.Create("blip_" + k, b.Length, 1, AudioManager.SampleRate, false);
            c.SetData(b, 0);
            blips[k] = c;
            return c;
        }

        static float[] BlipSamples(VoiceDef v, float freq, float amp)
        {
            var b = AudioManager.Buf(0.085f);
            AudioManager.Tone(b, 0f, 0.08f, freq * 0.97f, freq * 1.03f, 0.5f * amp, v.wave);
            AudioManager.Tone(b, 0f, 0.07f, freq * 2f, freq * 2f, 0.12f * amp, 0);          // formant tipis
            for (int i = 0; i < b.Length; i++) b[i] = Mathf.Clamp(b[i], -1f, 1f);
            return b;
        }

        /// <summary>Contoh ocehan (untuk ekspor WAV pengganti): 7 blip berurutan.</summary>
        public static float[] Sample(string key)
        {
            var v = Def(key);
            int gap = Mathf.RoundToInt(v.tempo * AudioManager.SampleRate);
            int n = 7;
            var all = new float[gap * n + AudioManager.SampleRate / 10];
            for (int i = 0; i < n; i++)
            {
                int step = Steps[(i * 7) % Steps.Length];
                if (i == n - 1) step = -3;
                var b = BlipSamples(v, v.pitch * Mathf.Pow(2f, step / 12f), 1f);
                for (int j = 0; j < b.Length && i * gap + j < all.Length; j++) all[i * gap + j] += b[j];
            }
            return all;
        }

        // ---------------- Pemetaan NPC -> suara ----------------
        public static string KeyFor(Interactable it)
        {
            if (it == null) return null;
            switch (it.GetType().Name)
            {
                case "ReceptionDesk": case "GuildBranch": return "receptionist";
                case "GuildMasterNPC": case "VillageElder": return "guildmaster";
                case "VillageHealer": return "priest";
                case "TrainerNPC": return "trainer";
                case "ExamReferee": return "referee";
                case "AppraiserDesk": return "appraiser";
                case "Blacksmith": return "smith";
                case "Inn": return "innkeeper";
                case "GeneralShop": return "merchant";
                case "Library": return "librarian";
                case "Restaurant": return "cook";
                case "Chapel": return "priest";
                case "CityGate": case "GateRequest": return "guard";
                default: return null;
            }
        }

        /// <summary>Kunci suara untuk NPC kota berdasarkan pakaian/peran (NpcKit.O*), jenis kelamin, dan nama (nada khas per orang).</summary>
        public static string KeyForCitizen(int outfit, string name, bool female)
        {
            if (outfit == NpcKit.OPenjaga || outfit == NpcKit.OKsatria) return "guard";
            if (outfit == NpcKit.OPendeta) return "priest";
            if (outfit == NpcKit.OBard) return "bard";
            if (outfit == NpcKit.OKoki) return "cook";
            if (outfit == NpcKit.OPandai) return "smith";
            if (outfit == NpcKit.OPedagang) return "merchant";
            return (female ? "citizen_f#" : "citizen_m#") + name;
        }

        /// <summary>Dipanggil saat pemain menekan E pada titik interaksi. Item non-NPC (papan quest) mendapat bunyi kertas.</summary>
        public static void Greet(Interactable it)
        {
            if (it == null) return;
            string n = it.GetType().Name;
            if (n == "QuestBoardPoint" || n == "NoticeBoardPoint" || n == "RankingBoardPoint") { Audio.Sfx("paper"); return; }
            if (n == "BuildingDoor" || n == "GuildDoor" || n == "InteriorExit") { Audio.Sfx("door"); return; }
            string key = KeyFor(it);
            if (key != null) Audio.Voice(key, "...");
        }
    }
}
