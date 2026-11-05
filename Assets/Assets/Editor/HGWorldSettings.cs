using System;
using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild.EditorTools
{
    /// <summary>Pengaturan terrain milik wizard (disimpan di Assets/HG_World/WorldSettings.asset).</summary>
    public class HGWorldSettings : ScriptableObject
    {
        [Serializable]
        public class Layer
        {
            public string name;
            public Texture2D diffuse;
            public Texture2D normal;
            public float tileSize = 8f;
        }

        public Layer[] layers =
        {
            new Layer { name = "Rumput" },
            new Layer { name = "Tanah / Jalan" },
            new Layer { name = "Batu" },
            new Layer { name = "Pasir" },
        };

        public Vector3 size = new Vector3(400f, 60f, 400f);
        public int heightRes = 513;
        public int alphaRes = 512;
        public int seed = 7;
        public float noiseScale = 0.012f;
        public float hillAmount = 0.6f;       // 0..1 tinggi bukit relatif
        public float flatRadius = 40f;        // area datar di tengah (kota/plaza)
        public float flatBlend = 35f;
        public float sandHeight = 0.06f;      // di bawah tinggi ini = pasir
        public float rockSlope = 32f;         // derajat; lebih curam = batu
        public float dirtNoise = 0.35f;       // bercak tanah acak di rumput

        public GameObject[] trees = new GameObject[0];
        public int treeCount = 300;
        public float treeMinScale = 0.85f, treeMaxScale = 1.3f;

        public Texture2D grassTexture;
        public float grassDensity = 1.2f;
        public float grassMinSize = 0.8f, grassMaxSize = 1.4f;
    }
}
