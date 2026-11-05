using System;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>v35z102n33: UI IMGUI untuk Co-op LAN: menu Main Bersama (host / gabung), daftar pemain, chat (Enter).</summary>
    public class CoopUI : MonoBehaviour
    {
        static bool open; static Action back;
        string nameField = "", ipField = "", chatField = "";
        bool typing;
        GUIStyle small, big;

        public static bool IsOpen { get { return open; } }

        public static void Open(Action backAction)
        {
            back = backAction; open = true;
            if (TownUI.I != null) { TownUI.I.Close(); TownUI.I.SetExternalOpen(true); }
            CoopNet.StartDiscovery();
        }

        static void CloseMenu()
        {
            open = false; CoopNet.StopDiscovery();
            if (TownUI.I != null) TownUI.I.SetExternalOpen(false);
            var b = back; back = null;
            if (b != null) b();
        }

        void Update() { if (open) CoopNet.PumpDiscovery(); }

        void OnGUI()
        {
            if (GameState.I == null) return;
            if (small == null)
            {
                small = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                big = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            float sc = Screen.height / 900f;
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(sc, sc, 1f));
            float W = Screen.width / sc;
            var e = Event.current;

            // ---- buka kotak chat dengan Enter
            if (CoopNet.Connected && !open && !typing && e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && TownUI.I != null && !TownUI.I.PanelOpen && !MiniGame.Active && !Cutscene.Active)
            { typing = true; chatField = ""; TownUI.I.SetExternalOpen(true); e.Use(); }

            if (CoopNet.Connected) DrawHud(W);
            if (typing) DrawTyping(W, e);
            if (open) DrawMenu(W);
            GUI.matrix = old;
        }

        void DrawHud(float W)
        {
            // daftar pemain (kiri atas, kecil)
            var lines = CoopNet.PlayerLines();
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(10, 130, 330, 24 + lines.Count * 22), Texture2D.whiteTexture);
            GUI.color = new Color(0.7f, 0.95f, 1f);
            GUI.Label(new Rect(16, 132, 320, 22), "CO-OP LAN" + (CoopNet.IsHost ? " (HOST)" : ""), small);
            GUI.color = Color.white;
            for (int i = 0; i < lines.Count; i++) GUI.Label(new Rect(16, 154 + i * 22, 320, 22), lines[i], small);
            // chat (kiri bawah), memudar bila tidak aktif
            bool show = typing || Time.unscaledTime - CoopNet.chatShownAt < 12f;
            if (!show) return;
            int n = Mathf.Min(7, CoopNet.Chat.Count);
            float y = 760 - n * 22;
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(10, y - 4, 520, n * 22 + 8), Texture2D.whiteTexture);
            GUI.color = Color.white;
            for (int i = 0; i < n; i++) GUI.Label(new Rect(16, y + i * 22, 510, 22), CoopNet.Chat[CoopNet.Chat.Count - n + i], small);
        }

        void DrawTyping(float W, Event e)
        {
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            { CoopNet.SendChat(chatField); EndTyping(); e.Use(); return; }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { EndTyping(); e.Use(); return; }
            GUI.SetNextControlName("coopchat");
            chatField = GUI.TextField(new Rect(10, 790, 520, 28), chatField, 120);
            GUI.FocusControl("coopchat");
        }

        void EndTyping() { typing = false; chatField = ""; if (TownUI.I != null) TownUI.I.SetExternalOpen(false); }

        void DrawMenu(float W)
        {
            if (nameField == "") nameField = CoopNet.MyName != "Hunter" ? CoopNet.MyName : (CharacterData.Current != null ? CharacterData.Current.name : "Hunter");
            Rect win = new Rect(W * 0.5f - 380, 50, 760, 800);
            GUI.color = new Color(0.04f, 0.05f, 0.09f, 0.96f);
            GUI.DrawTexture(win, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(win.x + 20, win.y + 14, win.width - 40, win.height - 28));
            GUILayout.Label("MAIN BERSAMA (CO-OP LAN)", big);
            GUILayout.Space(6);
            if (!CoopNet.Connected)
            {
                GUILayout.Label("Satu pemain membuka dunia (HOST), yang lain bergabung lewat jaringan Wi-Fi / LAN yang sama. Tiap pemain memakai karakter dan save sendiri. Di area hunt, HOST yang mengatur monster; tamu ikut melawan monster yang sama dan mendapat EXP/drop sendiri.", small);
                GUILayout.Space(8);
                GUILayout.BeginHorizontal(); GUILayout.Label("Nama tampil:", GUILayout.Width(120)); nameField = GUILayout.TextField(nameField, 16, GUILayout.Width(240)); GUILayout.EndHorizontal();
                GUILayout.Space(8);
                if (GUILayout.Button("BUKA DUNIA KE LAN (jadi Host)", GUILayout.Height(44))) CoopNet.StartHost(nameField);
                GUILayout.Space(14);
                GUILayout.Label("Dunia yang ditemukan di jaringan:", small);
                if (CoopNet.FoundWorlds.Count == 0) GUILayout.Label("  (belum ada. Pastikan host sudah membuka dunia dan kalian satu Wi-Fi.)", small);
                foreach (var f in new System.Collections.Generic.List<CoopNet.Found>(CoopNet.FoundWorlds.Values))
                    if (GUILayout.Button("Gabung: " + f.name + "   (" + f.players + " pemain, di " + f.scene + ")   " + f.ip, GUILayout.Height(34))) CoopNet.Join(f.ip + ":" + f.port, nameField);
                GUILayout.Space(14);
                GUILayout.Label("Atau ketik alamat IP host (mis. 192.168.1.5):", small);
                GUILayout.BeginHorizontal();
                ipField = GUILayout.TextField(ipField, 40, GUILayout.Width(300));
                if (GUILayout.Button("Gabung via alamat", GUILayout.Height(28))) CoopNet.Join(ipField, nameField);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(CoopNet.IsHost ? "Kamu HOST. Alamat untuk teman: " + CoopNet.LocalIp() + ":" + CoopNet.TcpPort : "Kamu tamu di dunia host.", small);
                GUILayout.Space(8);
                GUILayout.Label("Pemain:", small);
                foreach (var l in CoopNet.PlayerLines()) GUILayout.Label("  " + l, small);
                GUILayout.Space(10);
                if (CoopNet.IsClient) GUILayout.Label("Host berada di: " + CoopNet.HostScene + ". Monster hanya tersinkron bila kalian berada di area hunt (HuntArea) yang sama. Pergi ke sana lewat perjalanan cepat / berjalan seperti biasa.", small);
                else GUILayout.Label("Tamu melihat monster milikmu hanya saat berada di area hunt (HuntArea) yang sama denganmu. Menara & Arena dimainkan sendiri-sendiri.", small);
                GUILayout.Space(10);
                GUILayout.Label("Tekan Enter di game untuk chat.", small);
                GUILayout.Space(10);
                if (GUILayout.Button(CoopNet.IsHost ? "TUTUP DUNIA LAN" : "KELUAR DARI DUNIA HOST", GUILayout.Height(40))) CoopNet.Stop();
            }
            GUILayout.Space(10);
            GUILayout.Label("Status: " + CoopNet.Status, small);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("< Kembali", GUILayout.Height(40))) CloseMenu();
            GUILayout.EndArea();
        }
    }
}
