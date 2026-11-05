using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// Kursor mouse kustom (pengganti panah bawaan). Gambar: Assets/Resources/Cursors/Cursor_Arrow.png
    /// (ujung panah = pojok kiri-atas gambar). Opsional: Cursor_Click.png dipakai saat tombol mouse ditahan.
    /// Ganti file-nya dengan gambar sendiri (nama sama, atau beri nama baru agar tidak menimpa lalu ubah di sini).
    /// </summary>
    public class GameCursor : MonoBehaviour
    {
        const int Size = 48;                       // ukuran kursor di layar (px)
        static readonly Vector2 Hot = new Vector2(2f, 2f);   // titik klik, dari kiri-atas

        Texture2D arrow, click; bool showingClick, applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<GameCursor>() != null) return;
            var go = new GameObject("GameCursor"); DontDestroyOnLoad(go); go.AddComponent<GameCursor>();
        }

        void Start()
        {
            arrow = Prepare(Resources.Load<Texture2D>("Cursors/Cursor_Arrow"));
            click = Prepare(Resources.Load<Texture2D>("Cursors/Cursor_Click"));
            Apply(arrow);
        }

        void Update()
        {
            if (arrow == null) return;
            bool down = click != null && Input.GetMouseButton(0);
            if (down != showingClick) { showingClick = down; Apply(down ? click : arrow); }
            else if (!applied) Apply(arrow);
        }

        void OnApplicationFocus(bool f) { if (f) applied = false; }

        void Apply(Texture2D t) { if (t == null) return; Cursor.SetCursor(t, Hot, CursorMode.Auto); applied = true; }

        // Skala ke Size x Size lewat RenderTexture -> texture yang pasti bisa dibaca (tidak perlu atur import Read/Write).
        static Texture2D Prepare(Texture2D src)
        {
            if (src == null) return null;
            var rt = RenderTexture.GetTemporary(Size, Size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.filterMode = FilterMode.Bilinear;
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, Size, Size), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            return t;
        }
    }
}
