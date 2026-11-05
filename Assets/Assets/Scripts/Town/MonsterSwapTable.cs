using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n78: aturan penggantian model monster (Assets/Resources/MonsterSwapTable.asset), dikelola lewat Hunter Guild > Ganti Monster.
    /// Aturan yang paling spesifik menang: id monster > bentuk > boss/biasa > scene. Monster tanpa aturan memakai model bawaan (MonsterKit).
    /// Hanya aturan yang punya minimal satu prefab yang dipakai.
    /// </summary>
    public class MonsterSwapTable : ScriptableObject
    {
        [Serializable]
        public class Rule
        {
            public string label = "Aturan baru";
            public bool enabled = true;
            // ---- kecocokan (kosong / 0 = bebas)
            public string monsterId = "";       // id persis monster (mis. "alpha_wolf", "tbs_30"); aturan khusus 1 jenis
            public string shape = "";           // bentuk dasar (wolf, goblin, golem, ...); berlaku untuk semua monster berbentuk itu
            public string scene = "";           // nama scene (kosong = semua)
            public int tier;                    // 0 bebas, 1 hanya boss, 2 hanya bukan boss
            // ---- hasil
            public List<GameObject> prefabs = new List<GameObject>();   // lebih dari satu = divariasikan per individu
            public float heightScale = 1f;      // 1 = setinggi model bawaan monster itu
            public float yOffset;               // naik/turun model (meter, dunia)
            public float yaw;                   // putar model bila prefab tidak menghadap +Z (derajat)
            // ---- animasi
            public RuntimeAnimatorController controller;
            public float animSpeed = 1f;        // Animator.speed
            public float speedParamScale = 1f;  // pengali nilai parameter Speed (m/dtk) yang dikirim ke Animator
            public bool lifeMotion = true;      // model tanpa Animator digerakkan secara prosedural (naik-turun, miring, menerjang)
            public AnimationClip clipIdle, clipWalk, clipAttack, clipHurt, clipDie;   // bahan tombol "Buat Controller dari klip"
        }

        public List<Rule> rules = new List<Rule>();
    }
}
