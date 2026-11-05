using System.Collections.Generic;
using UnityEngine;

namespace HunterGuild
{
    /// <summary>Ikon item buatan kode (placeholder): pedang, busur, helm, baju, celana, ramuan, makanan, minuman, daun, kristal. Ganti dengan sprite asli bila sudah ada.</summary>
    public static class ItemIcons
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        const int N = 64;
        static readonly Dictionary<string, Sprite> custom_ = new Dictionary<string, Sprite>();

        public static Sprite For(ItemDef d)
        {
            if (d == null) return null;
            // icon buatan sendiri: taruh PNG (Texture Type = Sprite) di Assets/Resources/ItemIcons/<id>.png
            Sprite custom;
            if (!custom_.TryGetValue(d.id, out custom))
            {
                custom = Resources.Load<Sprite>("ItemIcons/" + d.id);
                if (custom == null)
                {
                    // PNG tanpa import setting Sprite -> bangun Sprite dari Texture2D
                    Texture2D tx = Resources.Load<Texture2D>("ItemIcons/" + d.id);
                    if (tx != null)
                        custom = Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, 0.5f), 100f);
                }
                custom_[d.id] = custom;
            }
            if (custom != null) return custom;
            string kind; Color tint;
            Pick(d, out kind, out tint);
            string key = kind + tint.ToString();
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            s = Make(kind, tint);
            cache[key] = s;
            return s;
        }

        static void Pick(ItemDef d, out string kind, out Color tint)
        {
            tint = new Color(0.8f, 0.85f, 0.92f);
            switch (d.type)
            {
                case ItemType.Gear:
                    if (d.slot == "Weapon" && d.job == "FireMage") { kind = "staff"; tint = new Color(1f, 0.45f, 0.18f); }
                    else if (d.slot == "Weapon" && d.job == "WaterMage") { kind = "staff"; tint = new Color(0.35f, 0.7f, 1f); }
                    else if (d.slot == "Weapon" && d.job == "WindMage") { kind = "staff"; tint = new Color(0.6f, 1f, 0.85f); }
                    else if (d.slot == "Weapon" && d.job == "Priest") { kind = "staff"; tint = new Color(1f, 0.95f, 0.6f); }
                    else if (d.slot == "Weapon" && d.job == "EarthMage") { kind = "staff"; tint = new Color(0.5f, 0.78f, 0.35f); }
                    else if (d.slot == "Weapon") { kind = d.job == "Archer" ? "bow" : "sword"; tint = new Color(0.82f, 0.86f, 0.93f); }
                    else if (d.slot == "Head") { kind = "helm"; tint = new Color(0.65f, 0.68f, 0.75f); }
                    else if (d.slot == "Hands") { kind = "gloves"; tint = new Color(0.55f, 0.5f, 0.45f); }
                    else if (d.slot == "Ring") { kind = "ring"; tint = new Color(0.9f, 0.75f, 0.3f); }
                    else if (d.slot == "Neck") { kind = "amulet"; tint = new Color(0.9f, 0.75f, 0.3f); }
                    else if (d.slot == "Legs") { kind = "legs"; tint = new Color(0.5f, 0.38f, 0.28f); }
                    else { kind = "chest"; tint = new Color(0.55f, 0.42f, 0.3f); }
                    return;
                case ItemType.Attachment:
                    kind = "gem";
                    tint = d.attachKind == "Sihir" ? new Color(0.55f, 0.4f, 0.95f) : d.attachKind == "Serangan" ? new Color(0.85f, 0.3f, 0.3f) : d.attachKind == "Pertahanan" ? new Color(0.3f, 0.5f, 0.9f) : new Color(0.35f, 0.75f, 0.45f);
                    return;
                case ItemType.QuestItem: kind = d.id == "guild_card" ? "card" : "scroll"; tint = d.id == "quest_proof" ? new Color(0.9f, 0.8f, 0.4f) : new Color(0.88f, 0.82f, 0.66f); return;
                case ItemType.Food: kind = "meat"; tint = new Color(0.78f, 0.42f, 0.22f); return;
                case ItemType.Drink: kind = "mug"; tint = new Color(0.3f, 0.55f, 0.85f); return;
                case ItemType.Consumable: kind = "flask"; tint = new Color(0.85f, 0.2f, 0.2f); return;
                default:
                    if (d.id != null && d.id.Contains("herb")) { kind = "leaf"; tint = new Color(0.35f, 0.7f, 0.3f); return; }
                    kind = "gem";
                    int h = d.id == null ? 0 : Mathf.Abs(d.id.GetHashCode());
                    Color[] pal = { new Color(0.6f, 0.35f, 0.85f), new Color(0.5f, 0.55f, 0.62f), new Color(0.85f, 0.65f, 0.3f), new Color(0.4f, 0.7f, 0.75f), new Color(0.75f, 0.3f, 0.35f) };
                    tint = pal[h % pal.Length];
                    return;
            }
        }

        // ---------- sdf ----------
        static float Circ(Vector2 p, float cx, float cy, float r) { return Vector2.Distance(p, new Vector2(cx, cy)) - r; }
        static float Box(Vector2 p, float x0, float y0, float x1, float y1)
        {
            float hx = (x1 - x0) * 0.5f, hy = (y1 - y0) * 0.5f;
            return Mathf.Max(Mathf.Abs(p.x - (x0 + x1) * 0.5f) - hx, Mathf.Abs(p.y - (y0 + y1) * 0.5f) - hy);
        }
        static float Seg(Vector2 p, float ax, float ay, float bx, float by, float r)
        {
            Vector2 a = new Vector2(ax, ay), b = new Vector2(bx, by), ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t) - r;
        }

        delegate void Add(float d, Color c);

        static void Shape(string kind, Vector2 p, Color t, Add add)
        {
            Color dark = t * 0.6f; dark.a = 1f;
            Color gold = new Color(0.9f, 0.72f, 0.3f), wood = new Color(0.5f, 0.32f, 0.18f);
            switch (kind)
            {
                case "sword":
                    add(Seg(p, 0.16f, 0.16f, 0.27f, 0.27f, 0.045f), wood);
                    add(Seg(p, 0.24f, 0.24f, 0.78f, 0.78f, 0.06f), t);
                    add(Seg(p, 0.30f, 0.48f, 0.48f, 0.30f, 0.045f), gold);
                    add(Circ(p, 0.14f, 0.14f, 0.055f), gold);
                    break;
                case "bow":
                    {
                        float ring = Mathf.Abs(Vector2.Distance(p, new Vector2(0.25f, 0.5f)) - 0.4f) - 0.04f;
                        add(Mathf.Max(ring, 0.25f - p.x), wood);
                        add(Seg(p, 0.25f, 0.12f, 0.25f, 0.88f, 0.012f), new Color(0.95f, 0.92f, 0.8f));
                        add(Seg(p, 0.25f, 0.5f, 0.78f, 0.5f, 0.015f), t);
                        break;
                    }
                case "helm":
                    add(Mathf.Max(Circ(p, 0.5f, 0.45f, 0.32f), 0.4f - p.y > 0f ? 1f : -1f), t);
                    add(Box(p, 0.2f, 0.22f, 0.8f, 0.42f), t * 0.85f);
                    add(Box(p, 0.4f, 0.22f, 0.6f, 0.62f), dark);
                    add(Box(p, 0.46f, 0.7f, 0.54f, 0.84f), new Color(0.8f, 0.2f, 0.2f));
                    break;
                case "chest":
                    add(Box(p, 0.26f, 0.2f, 0.74f, 0.72f), t);
                    add(Circ(p, 0.24f, 0.68f, 0.13f), t * 0.85f);
                    add(Circ(p, 0.76f, 0.68f, 0.13f), t * 0.85f);
                    add(Box(p, 0.26f, 0.38f, 0.74f, 0.46f), dark);
                    add(Box(p, 0.46f, 0.2f, 0.54f, 0.72f), gold);
                    break;
                case "legs":
                    add(Box(p, 0.28f, 0.56f, 0.72f, 0.72f), dark);
                    add(Box(p, 0.28f, 0.1f, 0.47f, 0.6f), t);
                    add(Box(p, 0.53f, 0.1f, 0.72f, 0.6f), t);
                    break;
                case "gloves":
                    add(Box(p, 0.3f, 0.12f, 0.7f, 0.5f), t);
                    add(Box(p, 0.3f, 0.5f, 0.38f, 0.82f), t * 0.9f);
                    add(Box(p, 0.4f, 0.5f, 0.48f, 0.88f), t * 0.9f);
                    add(Box(p, 0.5f, 0.5f, 0.58f, 0.86f), t * 0.9f);
                    add(Box(p, 0.6f, 0.5f, 0.68f, 0.78f), t * 0.9f);
                    add(Box(p, 0.28f, 0.12f, 0.72f, 0.22f), dark);
                    break;
                case "ring":
                    add(Mathf.Abs(Vector2.Distance(p, new Vector2(0.5f, 0.4f)) - 0.24f) - 0.06f, t);
                    add(Circ(p, 0.5f, 0.7f, 0.1f), new Color(0.3f, 0.7f, 0.9f));
                    break;
                case "amulet":
                    add(Seg(p, 0.2f, 0.85f, 0.5f, 0.45f, 0.02f), dark);
                    add(Seg(p, 0.8f, 0.85f, 0.5f, 0.45f, 0.02f), dark);
                    add(Circ(p, 0.5f, 0.35f, 0.17f), t);
                    add(Circ(p, 0.5f, 0.35f, 0.08f), new Color(0.8f, 0.25f, 0.3f));
                    break;
                case "staff":
                    add(Seg(p, 0.2f, 0.18f, 0.72f, 0.7f, 0.035f), wood);
                    add(Seg(p, 0.66f, 0.64f, 0.8f, 0.82f, 0.055f), gold);
                    add(Circ(p, 0.8f, 0.82f, 0.12f), t);
                    add(Circ(p, 0.77f, 0.85f, 0.04f), Color.white);
                    break;
                case "scroll":
                    add(Box(p, 0.24f, 0.2f, 0.76f, 0.8f), t);
                    add(Box(p, 0.2f, 0.74f, 0.8f, 0.84f), dark);
                    add(Box(p, 0.2f, 0.16f, 0.8f, 0.26f), dark);
                    add(Circ(p, 0.5f, 0.46f, 0.1f), new Color(0.75f, 0.2f, 0.2f));
                    break;
                case "card":
                    add(Box(p, 0.14f, 0.26f, 0.86f, 0.74f), t * 0.9f);
                    add(Box(p, 0.14f, 0.58f, 0.86f, 0.74f), gold);
                    add(Circ(p, 0.32f, 0.42f, 0.09f), dark);
                    add(Box(p, 0.5f, 0.38f, 0.78f, 0.44f), dark);
                    break;
                case "flask":
                    add(Box(p, 0.43f, 0.55f, 0.57f, 0.8f), new Color(0.8f, 0.85f, 0.9f));
                    add(Circ(p, 0.5f, 0.36f, 0.26f), t);
                    add(Box(p, 0.44f, 0.8f, 0.56f, 0.9f), wood);
                    add(Circ(p, 0.4f, 0.42f, 0.06f), new Color(1f, 1f, 1f));
                    break;
                case "meat":
                    add(Seg(p, 0.56f, 0.56f, 0.8f, 0.8f, 0.04f), new Color(0.95f, 0.9f, 0.78f));
                    add(Circ(p, 0.83f, 0.83f, 0.06f), new Color(0.95f, 0.9f, 0.78f));
                    add(Circ(p, 0.4f, 0.4f, 0.26f), t);
                    add(Circ(p, 0.32f, 0.48f, 0.07f), t * 1.25f);
                    break;
                case "mug":
                    add(Mathf.Abs(Vector2.Distance(p, new Vector2(0.72f, 0.46f)) - 0.14f) - 0.04f, new Color(0.85f, 0.85f, 0.88f));
                    add(Box(p, 0.24f, 0.18f, 0.66f, 0.76f), new Color(0.85f, 0.85f, 0.88f));
                    add(Box(p, 0.28f, 0.22f, 0.62f, 0.62f), t);
                    break;
                case "leaf":
                    {
                        Vector2 q = new Vector2((p.x + p.y) * 0.7071f, (p.y - p.x) * 0.7071f);
                        float a = Vector2.Distance(q, new Vector2(0.7f, 0.0f + 0.2f)) - 0.42f;
                        float b = Vector2.Distance(q, new Vector2(0.7f, 0.0f - 0.2f)) - 0.42f;
                        add(Mathf.Max(a, b), t);
                        add(Seg(p, 0.22f, 0.22f, 0.76f, 0.76f, 0.012f), t * 0.6f);
                        break;
                    }
                default:     // gem
                    {
                        float dd = (Mathf.Abs(p.x - 0.5f) * 1.0f + Mathf.Abs(p.y - 0.5f) * 0.8f) - 0.34f;
                        add(dd, t);
                        add(Mathf.Max(dd, (p.x - 0.5f) * 1f + (p.y - 0.5f) * 0.1f < 0f ? 1f : -1f), t * 1.3f);
                        break;
                    }
            }
        }

        static Sprite Make(string kind, Color tint)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / N, (y + 0.5f) / N);
                    Color outc = new Color(0, 0, 0, 0);
                    Add add = (d, c) =>
                    {
                        if (d >= 0.012f) return;
                        if (d > -0.02f) outc = new Color(0.08f, 0.06f, 0.05f, 1f);
                        else { float sh = 0.85f + 0.3f * p.y; outc = new Color(Mathf.Clamp01(c.r * sh), Mathf.Clamp01(c.g * sh), Mathf.Clamp01(c.b * sh), 1f); }
                    };
                    Shape(kind, p, tint, add);
                    px[y * N + x] = outc;
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
