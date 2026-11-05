using System;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Hasil kustomisasi karakter pemain (disimpan di PlayerPrefs sebagai JSON).</summary>
    [Serializable]
    public class CharacterData
    {
        public string name = "Hunter";
        public int race, build = 1, skin, hair, hairCol = 1, beard, beast;
        public int outfit = NpcKit.OHunter, hat = -1, cA = 2, cB = 1, cC = 1;
        public bool female, created;

        static string PrefKey { get { int s = SaveSystem.Slot; return s <= 1 ? "HG_Character_v1" : "HG_Character_v1_s" + s; } }
        /// <summary>Muat ulang data karakter (setelah ganti slot save).</summary>
        public static void Reload() { cur = null; }
        static CharacterData cur;

        // pilihan yang ditampilkan di UI
        public static readonly int[] OutfitChoices = { NpcKit.OHunter, NpcKit.OWarga, NpcKit.OPengembara, NpcKit.OPedagang, NpcKit.OPetani, NpcKit.OBangsawan,
                                                      NpcKit.OPendeta, NpcKit.OPenyihir, NpcKit.OPandai, NpcKit.OBard, NpcKit.OPelajar, NpcKit.OKoki, NpcKit.OPetugas };
        public static readonly int[] HatChoices = { -1, 0, 1, 2, 3, 5, 11, 13, 14, 15 };
        public static readonly string[] HatNames = { "Bawaan pakaian", "Tanpa topi", "Topi jerami", "Baret pedagang", "Topi penyihir", "Tudung", "Topi bard", "Ikat kepala", "Turban", "Kerudung" };
        public static readonly string[] HairNames = { "Pendek", "Panjang lurus", "Kuncir kuda", "Dua kuncir", "Bob", "Ikal bulat", "Sanggul", "Kepang panjang", "Botak", "Jabrik", "Acak sedang", "Poni miring" };
        public static readonly string[] BeardNames = { "Tanpa jenggot", "Tipis + kumis", "Penuh", "Panjang berkepang" };
        public static readonly string[] BuildNames = { "Ramping", "Normal", "Kekar" };
        public static readonly string[] OutfitLabel = { "Warga", "Pedagang", "Petani", "Bangsawan", "Penjaga", "Ksatria", "Pendeta", "Penyihir", "Hunter", "Pandai Besi", "Pelayan", "Bard", "Koki", "Pelajar", "Pengembara", "Petugas Guild" };

        public static CharacterData Current
        {
            get { if (cur == null) cur = Load(); return cur; }
            set { cur = value; }
        }

        public static bool Exists { get { return Current.created; } }

        /// <summary>Game Baru: hapus karakter tersimpan.</summary>
        public static void ResetAll()
        {
            try { PlayerPrefs.DeleteKey(PrefKey); PlayerPrefs.Save(); } catch (Exception) { }
            cur = new CharacterData();
        }

        static CharacterData Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(PrefKey))
                {
                    var d = JsonUtility.FromJson<CharacterData>(PlayerPrefs.GetString(PrefKey));
                    if (d != null) return d;
                }
            }
            catch (Exception) { }
            return new CharacterData();
        }

        public void Save()
        {
            created = true;
            cur = this;
            try { PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(this)); PlayerPrefs.Save(); } catch (Exception) { }
        }

        public CharacterData Clone() { return (CharacterData)MemberwiseClone(); }

        public NpcKit.Spec ToSpec()
        {
            return NpcKit.PlayerSpec(name, race, female, build, skin, hair, hairCol, beard, beast, outfit, hat, cA, cB, cC);
        }

        public void Randomize()
        {
            race = UnityEngine.Random.Range(0, 4);
            female = UnityEngine.Random.value < 0.5f;
            build = UnityEngine.Random.Range(0, 3);
            skin = UnityEngine.Random.Range(0, NpcKit.SkinCount(race));
            hair = UnityEngine.Random.Range(0, 8);
            hairCol = UnityEngine.Random.Range(0, NpcKit.HairColorCount);
            beard = female ? 0 : UnityEngine.Random.Range(0, 4);
            beast = UnityEngine.Random.Range(0, 4);
            outfit = OutfitChoices[UnityEngine.Random.Range(0, OutfitChoices.Length)];
            hat = -1;
            cA = UnityEngine.Random.Range(0, NpcKit.FabricCount); cB = UnityEngine.Random.Range(0, NpcKit.FabricCount); cC = UnityEngine.Random.Range(0, NpcKit.FabricCount);
        }
    }
}
