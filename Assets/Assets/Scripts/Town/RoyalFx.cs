using UnityEngine;

namespace HunterGuild
{
    /// <summary>
    /// v35z102n6 "Megah &amp; Mewah": lapisan efek tambahan di atas CombatFx. Semua buatan kode (tanpa aset),
    /// memakai ulang material aditif HDR CombatFx.Mat. Bisa dimatikan lewat RoyalFx.On (PlayerPrefs "hg_lux").
    /// </summary>
    public static class RoyalFx
    {
        public static readonly Color Gold = new Color(1f, 0.84f, 0.38f);
        public static readonly Color Ivory = new Color(1f, 0.96f, 0.85f);
        public static readonly Color Violet = new Color(0.62f, 0.4f, 1f);

        public static bool On { get { try { return PlayerPrefs.GetInt("hg_lux", 1) == 1 && FxQuality.Level > 0; } catch { return true; } } }
        public static void Toggle() { try { PlayerPrefs.SetInt("hg_lux", PlayerPrefs.GetInt("hg_lux", 1) == 1 ? 0 : 1); PlayerPrefs.Save(); } catch { } }

        // ---------------------------------------------------------------- pembangun partikel
        /// <summary>Semburan partikel sekali jalan (hancur sendiri). up = dorongan ke atas, grav = gravitasi (negatif = naik).</summary>
        public static ParticleSystem Puff(Vector3 pos, Color c, int count, float speed, float size, float life, float grav, float radius = 0.2f)
        {
            var go = new GameObject("RoyalPuff");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.3f; main.loop = false; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(c, Color.Lerp(c, Color.white, 0.55f));
            main.gravityModifier = grav;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)FxQuality.N(count)) });
            var sp = ps.shape; sp.enabled = true; sp.shapeType = ParticleSystemShapeType.Sphere; sp.radius = radius;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(c, 0.4f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), Color.white);
            ps.Play();
            return ps;
        }

        /// <summary>Kilau bintang besar dengan empat sinar (billboard).</summary>
        public static void Glint(Vector3 pos, Color c, float size, float life = 0.22f)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "RoyalGlint";
            q.transform.position = pos;
            q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Star(), c);
            var fx = q.AddComponent<CombatFx.FxAnim>();
            fx.life = life; fx.s0 = Vector3.one * size * 0.3f; fx.s1 = Vector3.one * size; fx.billboard = true; fx.color = c; fx.spin = 0f;
        }

        /// <summary>Cincin tegak (menghadap kamera) yang melebar: kesan gelombang kejut berlapis.</summary>
        public static void HaloRing(Vector3 pos, float radius, Color c, float life = 0.4f)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "RoyalHalo";
            q.transform.position = pos;
            q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Ring(), c);
            var fx = q.AddComponent<CombatFx.FxAnim>();
            fx.life = life; fx.s0 = Vector3.one * radius * 0.25f; fx.s1 = Vector3.one * radius * 2f; fx.billboard = true; fx.color = c;
        }

        /// <summary>Tiang cahaya vertikal (dipakai kematian boss dan level naik).</summary>
        public static void LightPillar(Vector3 pos, Color c, float height, float width, float life)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "RoyalPillar";
            q.transform.position = pos + Vector3.up * height * 0.5f;
            q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Soft(), c * 1.2f);
            var fx = q.AddComponent<CombatFx.FxAnim>();
            fx.life = life; fx.s0 = new Vector3(width, height * 0.4f, 1f); fx.s1 = new Vector3(width * 0.15f, height, 1f); fx.billboard = true; fx.color = c;
        }

        // ---------------------------------------------------------------- tembakan siap pakai
        /// <summary>Tambahan setiap pukulan kena (dipanggil dari CombatFx.HitSpark): kilau, cincin halo, serpihan emas.</summary>
        public static void HitExtra(Vector3 pos, Color c, float size)
        {
            if (!On) return;
            Glint(pos, Color.Lerp(c, Color.white, 0.5f), 1.9f * Mathf.Max(0.6f, size), 0.2f);
            HaloRing(pos, 0.55f * Mathf.Max(0.6f, size), Color.Lerp(c, Color.white, 0.35f), 0.28f);
            if (FxQuality.Level >= 2) Puff(pos, Color.Lerp(c, Gold, 0.5f), 6, 5f * size, 0.06f, 0.5f, 0.6f, 0.05f);
        }

        /// <summary>Gelombang kedua mengikuti ShockRing: cincin emas tipis + debu cahaya naik.</summary>
        public static void RingExtra(Vector3 pos, float maxRadius, Color c)
        {
            if (!On) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "RoyalRing2";
            q.transform.position = pos + Vector3.up * 0.1f;
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Ring(), Color.Lerp(c, Gold, 0.55f));
            var fx = q.AddComponent<CombatFx.FxAnim>();
            fx.life = 0.55f; fx.s0 = Vector3.one * 0.2f; fx.s1 = Vector3.one * maxRadius * 2.6f; fx.color = Gold;
            if (FxQuality.Level >= 2 && maxRadius >= 1.5f) Puff(pos + Vector3.up * 0.2f, c, 14, 2.2f, 0.12f, 1.1f, -0.15f, Mathf.Min(maxRadius * 0.6f, 3f));
        }

        /// <summary>Kematian monster: jiwa naik, serpihan, cincin; boss mendapat tiang cahaya dan ledakan besar.</summary>
        public static void MonsterDeath(Vector3 pos, Color c, bool boss, float size)
        {
            if (!On) return;
            float s = Mathf.Clamp(size, 0.6f, 3f);
            Puff(pos, c, 22, 4.5f * s, 0.16f, 0.9f, 0.9f, 0.25f * s);               // serpihan jatuh
            Puff(pos, Color.Lerp(c, Ivory, 0.5f), 16, 1.4f, 0.2f, 1.8f, -0.25f, 0.35f * s);   // jiwa/ember naik
            HaloRing(pos, 1.3f * s, Color.Lerp(c, Color.white, 0.4f), 0.5f);
            Glint(pos, Ivory, 3f * s, 0.3f);
            CombatFx.Flash(pos, Color.Lerp(c, Color.white, 0.4f), 6f * s, 3f, 0.35f);
            if (boss)
            {
                LightPillar(pos, Gold, 14f, 2.2f, 1.6f);
                HaloRing(pos, 4f, Gold, 0.9f);
                RingExtra(pos, 5f, Gold);
                Puff(pos, Gold, 60, 9f, 0.22f, 1.6f, 0.4f, 0.5f);
                Puff(pos + Vector3.up, Ivory, 40, 2f, 0.28f, 3f, -0.2f, 1.2f);
                CombatFx.Flash(pos + Vector3.up, Gold, 16f, 5f, 0.9f);
            }
        }

        /// <summary>Munculnya boss/elite: lingkaran rune menyala di tanah.</summary>
        public static void Summon(Vector3 pos, Color c, float radius)
        {
            if (!On) return;
            HaloRing(pos + Vector3.up * 0.2f, radius, c, 0.7f);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "RoyalSummon";
            q.transform.position = pos + Vector3.up * 0.06f;
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.GetComponent<MeshRenderer>().sharedMaterial = CombatFx.Mat(CombatFx.Ring(), c);
            var fx = q.AddComponent<CombatFx.FxAnim>();
            fx.life = 1.2f; fx.s0 = Vector3.one * radius * 0.6f; fx.s1 = Vector3.one * radius * 2.4f; fx.color = c; fx.spin = 60f;
            Puff(pos, c, 30, 3f, 0.14f, 1.2f, -0.2f, radius * 0.4f);
        }
    }
}
