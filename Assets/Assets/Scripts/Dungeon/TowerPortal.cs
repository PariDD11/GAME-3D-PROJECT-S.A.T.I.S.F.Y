using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Portal di Menara: naik ke lantai berikutnya (setelah semua musuh kalah) atau keluar ke Kota Eldrath.</summary>
    public class TowerPortal : Interactable
    {
        public enum Kind { Next, Exit }
        public Kind kind = Kind.Next;

        public override void Interact()
        {
            var tm = TowerManager.I;
            if (tm == null) return;
            if (kind == Kind.Next)
            {
                if (!tm.Cleared) { TownUI.I.Toast("Kalahkan semua musuh di lantai ini dulu."); return; }
                Audio.Sfx("tower_portal", transform.position);
                tm.GoNext();
                return;
            }
            TownUI.I.Open("Keluar dari Menara?",
                () => "Lantai " + tm.Floor + " / " + TowerDB.Floors + ".\nCheckpoint tersimpan setiap boss dikalahkan (tertinggi: " + (GameState.I.dungeonBossCleared <= 0 ? "-" : "boss lantai " + GameState.I.dungeonBossCleared) +
                      ").\nKeluar sekarang berarti memulai lagi dari checkpoint terakhir.",
                () =>
                {
                    var l = new List<UIEntry>();
                    l.Add(new UIEntry("Ya, kembali ke Kota Eldrath", () => { TownUI.I.Close(); tm.Leave(); }));
                    l.Add(new UIEntry("Tetap di Menara", () => TownUI.I.Close()));
                    return l;
                });
        }

        /// <summary>Membangun tampilan portal (cincin bercahaya + cahaya titik) di bawah parent.</summary>
        public static TowerPortal Create(Transform parent, Vector3 pos, Kind k, string prompt, Color glow)
        {
            var go = new GameObject(k == Kind.Next ? "PortalNaik" : "PortalKeluar");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            RT.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.08f, 0f), new Vector3(4.2f, 0.08f, 4.2f), new Color(0.12f, 0.12f, 0.15f));
            var ring = RT.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 0.2f, 0f), new Vector3(3.4f, 0.04f, 3.4f), glow);
            var beam = RT.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0f, 2.5f, 0f), new Vector3(0.7f, 2.4f, 0.7f), new Color(glow.r, glow.g, glow.b));
            var mat = beam.GetComponent<Renderer>().material;
            mat.color = new Color(glow.r, glow.g, glow.b, 0.25f);
            var lg = new GameObject("Glow").AddComponent<Light>();
            lg.transform.SetParent(go.transform, false);
            lg.transform.localPosition = Vector3.up * 2.5f;
            lg.type = LightType.Point; lg.range = 14f; lg.intensity = 2.4f; lg.color = glow;
            var spin = go.AddComponent<SpinObject>();
            spin.axis = Vector3.up; spin.degreesPerSecond = 40f;
            var p = go.AddComponent<TowerPortal>();
            p.kind = k; p.prompt = prompt; p.radius = 3.4f;
            return p;
        }
    }

}
