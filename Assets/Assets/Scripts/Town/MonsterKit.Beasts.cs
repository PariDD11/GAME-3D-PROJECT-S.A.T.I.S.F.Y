using UnityEngine;

namespace HunterGuild
{
    /// <summary>Monster berkaki empat: serigala, babi hutan, banteng, Cerberus, ternak, kadal. Ruang bentuk: +Z depan, kaki di y = 0.</summary>
    public static partial class MonsterKit
    {
        // pipih: telinga/sirip (sisi lebar sejajar X bila hint = depan)
        static void Ear(GeoBag b, int slot, Vector3 baseP, Vector3 tip, float w, float th, Vector3 hint, int segs = 5)
        {
            Tube(b, slot, new[] { baseP, Vector3.Lerp(baseP, tip, 0.5f), tip }, new[] { w, w * 0.8f, w * 0.12f }, new[] { th, th * 0.8f, th * 0.3f }, segs, hint, false, true);
        }

        // satu kaki dua ruas: pinggul/bahu (pivot A) -> siku/lutut (pivot B) -> telapak
        static void QLegs(MkCtx c, MkPart torso, string nm, float side, Vector3 hip, Vector3 knee, Vector3 ankle, Vector3 paw,
                          float rHip, float rKnee, float rAnkle, int slot, int slotPaw, float phase, float swing, float bend, bool haunch, float pawL, bool hoof = false)
        {
            Vector3 H = new Vector3(hip.x * side, hip.y, hip.z), K = new Vector3(knee.x * side, knee.y, knee.z), A = new Vector3(ankle.x * side, ankle.y, ankle.z), P = new Vector3(paw.x * side, paw.y, paw.z);
            var upper = c.New(nm + "A", torso, H, delegate (MkPart p)
            {
                Tube(p.S, slot, new[] { H, Vector3.Lerp(H, K, 0.5f), K }, new[] { rHip, rHip * 0.9f, rKnee }, new[] { rHip * 1.15f, rHip * 0.95f, rKnee }, 8, Vector3.forward, true, true);
                if (haunch) Ell(p.S, slot, H + new Vector3(0.01f * side, -0.04f, 0f), new Vector3(rHip * 0.95f, rHip * 1.55f, rHip * 1.4f), 5, 9);
                else Ell(p.S, slot, H + new Vector3(0f, 0.02f, 0f), new Vector3(rHip * 1.05f, rHip * 1.35f, rHip * 1.3f), 5, 9);
            });
            var lower = c.New(nm + "B", upper, K, delegate (MkPart p)
            {
                Tube(p.S, slot, new[] { K, A, P + new Vector3(0f, 0.07f, 0f) }, new[] { rKnee, rAnkle * 0.95f, rAnkle }, new[] { rKnee, rAnkle * 0.95f, rAnkle }, 7, Vector3.forward, true, true);
                if (hoof) Ell(p.S, slotPaw, P + new Vector3(0f, 0.03f, pawL * 0.1f), new Vector3(rAnkle * 1.25f, rAnkle * 1.05f, pawL * 0.9f), 4, 9);       // teracak
                else
                {
                    Ell(p.S, slotPaw, P + new Vector3(0f, 0.01f, pawL * 0.25f), new Vector3(rAnkle * 1.45f, rAnkle * 1.05f, pawL), 4, 8);
                    for (int i = -1; i <= 1; i++)
                        Spike(p.S, MS.Claw, P + new Vector3(i * rAnkle * 0.55f, 0.0f, pawL * 1.05f), P + new Vector3(i * rAnkle * 0.6f, -0.02f, pawL * 1.05f + 0.07f), rAnkle * 0.22f, 4);
                }
            });
            c.rig.Leg(upper.t, lower.t, phase, swing, bend);
        }

        // rumbai bulu melingkar di sekitar sumbu (leher/dada): pipih, mengarah ke belakang
        static void Ruff(GeoBag b, int slot, Vector3 ctr, Vector3 fwd, float rad, int n, float len, float w, float th, float spread, float phase)
        {
            Quaternion q = Face(fwd, Vector3.up);
            for (int i = 0; i < n; i++)
            {
                float a = phase + i * Mathf.PI * 2f / n;
                Vector3 radial = q * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 bp = ctr + radial * rad;
                Vector3 tp = bp - fwd.normalized * len + radial * spread;
                Ear(b, slot, bp, tp, w, th, radial);
            }
        }

        static void Wolf(MkCtx c)
        {
            bool frost = c.V("frost"), hell = c.V("hell"), jackal = c.V("jackal"), alpha = c.V("alpha"), dire = c.V("dire");
            var col = c.col;
            if (hell) { col[MS.Eye] = new Color(1f, 0.82f, 0.25f); col[MS.Ember] = new Color(1f, 0.38f, 0.1f); col[MS.Pale] = Dk(col[MS.Body], 0.75f); col[MS.Dark] = Dk(col[MS.Body], 0.42f); }
            if (frost) { col[MS.Eye] = new Color(0.55f, 0.9f, 1f); col[MS.Accent] = new Color(0.7f, 0.93f, 1f); col[MS.Dark] = Mix(col[MS.Body], new Color(0.35f, 0.5f, 0.75f), 0.6f); col[MS.Pale] = Mix(col[MS.Body], new Color(0.95f, 0.98f, 1f), 0.75f); }
            if (jackal) col[MS.Pale] = new Color(0.97f, 0.9f, 0.76f);
            col[MS.Dark] = Mix(col[MS.Dark], col[MS.Body], 0.25f);      // pita punggung: gelap tapi masih sewarna bulu
            float earK = jackal ? 1.4f : 1f, ruff = (alpha || dire) ? 1.3f : 1f, lean = jackal ? 0.88f : 1f;

            var torso = c.New("torso", null, new Vector3(0f, 1.1f, 0.1f), delegate (MkPart p)
            {
                // badan: dada dalam -> pinggang ramping -> pinggul. Pita warna: punggung gelap, sisi, perut pucat.
                Vector3[] bp = { new Vector3(0f, 1.16f, 0.88f), new Vector3(0f, 1.13f, 0.32f), new Vector3(0f, 1.07f, -0.3f), new Vector3(0f, 1.10f, -0.86f) };
                float[] brx = { 0.37f * lean, 0.36f * lean, 0.31f * lean, 0.31f * lean }, bry = { 0.52f, 0.45f, 0.37f, 0.41f };
                TubeBands(p.S, new[] { MS.Dark, MS.Body, MS.Pale, MS.Body }, new[] { 50f, 130f, 230f, 310f, 410f }, bp, brx, bry, 5, Vector3.up, true, true);
                // bahu & paha berotot (menonjol sedikit dari badan)
                for (int s = -1; s <= 1; s += 2)
                {
                    Ell(p.S, MS.Body, new Vector3(0.27f * s, 1.13f, 0.62f), new Vector3(0.14f, 0.3f, 0.26f), Quaternion.Euler(14f, 0f, 0f), 4, 9);
                    Ell(p.S, MS.Body, new Vector3(0.26f * s, 1.12f, -0.58f), new Vector3(0.15f, 0.3f, 0.32f), Quaternion.Euler(-10f, 0f, 0f), 4, 9);
                }
                // surai tengkuk -> punggung (gelap), dada berbulu pucat
                for (int i = 0; i < 7; i++)
                {
                    float z = 0.74f - i * 0.2f, y = 1.57f - i * 0.045f - (i > 4 ? (i - 4) * 0.02f : 0f);
                    float h = (0.2f - i * 0.016f) * (alpha || dire ? 1.3f : 1f);
                    Ear(p.S, MS.Dark, new Vector3(0f, y, z), new Vector3(0f, y + h, z - 0.2f), 0.065f, 0.03f, Vector3.right);
                    if (frost) Spike(p.S, MS.Accent, new Vector3(0f, y, z + 0.04f), new Vector3(0f, y + h * 1.9f, z - 0.06f), 0.035f, 4);
                    if (hell) Spike(p.G, MS.Ember, new Vector3(0f, y + 0.02f, z), new Vector3(0f, y + h * 1.8f, z - 0.14f), 0.045f, 4);
                }
                Ruff(p.S, MS.Pale, new Vector3(0f, 1.2f, 0.9f), Vector3.forward, 0.3f * ruff, 9, 0.24f, 0.07f, 0.03f, 0.06f, 0.35f);
            });
            c.rig.Breath(torso.t, 0.012f, 2.4f);

            var head = c.New("head", torso, new Vector3(0f, 1.34f, 0.78f), delegate (MkPart p)
            {
                // leher kokoh miring ke depan
                Tube(p.S, MS.Body, new[] { new Vector3(0f, 1.30f, 0.66f), new Vector3(0f, 1.45f, 1.0f), new Vector3(0f, 1.55f, 1.3f) },
                     new[] { 0.3f, 0.27f, 0.25f }, new[] { 0.31f, 0.28f, 0.26f }, 9, Vector3.up, true, true);
                // tengkorak besar (kesan anime) + dahi
                Ell(p.S, MS.Body, new Vector3(0f, 1.6f, 1.42f), new Vector3(0.3f, 0.28f, 0.34f), 6, 12);
                // moncong: atas Body, bawah Pale
                TubeBands(p.S, new[] { MS.Body, MS.Pale }, new[] { -25f, 205f, 335f }, new[] { new Vector3(0f, 1.57f, 1.58f), new Vector3(0f, 1.5f, 1.84f), new Vector3(0f, 1.46f, 2.1f) },
                          new[] { 0.185f, 0.15f, 0.1f }, new[] { 0.16f, 0.13f, 0.09f }, 5, Vector3.up, true, true);
                // pipi berbulu
                for (int s = -1; s <= 1; s += 2)
                {
                    Ruff(p.S, MS.Pale, new Vector3(0.24f * s, 1.5f, 1.3f), new Vector3(0.0f, -0.1f, 1f), 0.05f, 5, 0.15f, 0.06f, 0.025f, 0.05f, 0.2f);
                    Ell(p.S, MS.Pale, new Vector3(0.21f * s, 1.47f, 1.4f), new Vector3(0.1f, 0.11f, 0.15f), 4, 8);
                }
                Ell(p.S, MS.Claw, new Vector3(0f, 1.47f, 2.14f), new Vector3(0.075f, 0.058f, 0.06f), 4, 8);        // hidung
                // telinga tegak besar (luar gelap, dalam pucat)
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 b0 = new Vector3(0.17f * s, 1.8f, 1.3f), t0 = new Vector3(0.25f * s, 1.8f + 0.42f * earK, 1.14f - 0.03f * earK);
                    Ear(p.S, MS.Dark, b0, t0, 0.11f * earK, 0.04f, Vector3.forward);
                    Ear(p.S, MS.Pale, b0 + new Vector3(0f, 0.02f, 0.045f), Vector3.Lerp(b0, t0, 0.82f) + new Vector3(0f, 0f, 0.05f), 0.066f * earK, 0.02f, Vector3.forward);
                }
                // taring atas
                for (int s = -1; s <= 1; s += 2) Spike(p.S, MS.Bone, new Vector3(0.07f * s, 1.44f, 2.0f), new Vector3(0.07f * s, 1.32f, 2.02f), 0.028f, 5);
                if (hell) for (int s = -1; s <= 1; s += 2) Horn(p.S, MS.Dark, new Vector3(0.15f * s, 1.8f, 1.44f), new Vector3(0.28f * s, 2.08f, 1.2f), new Vector3(0f, 0.06f, 0.12f), 0.055f);
                EyePair(p, new Vector3(0.15f, 1.64f, 1.6f), 0.06f, new Vector3(0.35f, -0.08f, 1f));
                // alis gelap = tatapan galak
                for (int s = -1; s <= 1; s += 2)
                    Tube(p.S, MS.Dark, new[] { new Vector3(0.07f * s, 1.72f, 1.64f), new Vector3(0.16f * s, 1.735f, 1.59f), new Vector3(0.23f * s, 1.7f, 1.52f) }, new[] { 0.016f, 0.019f, 0.014f }, new[] { 0.016f, 0.019f, 0.014f }, 5, Vector3.up, true, true);
            });

            var jaw = c.New("jaw", head, new Vector3(0f, 1.46f, 1.64f), delegate (MkPart p)
            {
                Tube(p.S, MS.Pale, new[] { new Vector3(0f, 1.43f, 1.64f), new Vector3(0f, 1.385f, 1.86f), new Vector3(0f, 1.375f, 2.04f) },
                     new[] { 0.12f, 0.09f, 0.058f }, new[] { 0.065f, 0.055f, 0.04f }, 8, Vector3.up, true, true);
                Tube(p.S, MS.Mouth, new[] { new Vector3(0f, 1.47f, 1.74f), new Vector3(0f, 1.44f, 1.98f) }, new[] { 0.075f, 0.045f }, new[] { 0.013f, 0.011f }, 6, Vector3.up, true, true);
                for (int s = -1; s <= 1; s += 2) Spike(p.S, MS.Bone, new Vector3(0.055f * s, 1.4f, 1.99f), new Vector3(0.055f * s, 1.5f, 1.985f), 0.024f, 5);
            });

            Vector3 shoulder = new Vector3(0.25f, 1.06f, 0.62f), elbow = new Vector3(0.24f, 0.66f, 0.54f), wrist = new Vector3(0.24f, 0.24f, 0.62f), fpaw = new Vector3(0.24f, 0.07f, 0.62f);
            Vector3 hipB = new Vector3(0.25f, 1.02f, -0.58f), knee = new Vector3(0.24f, 0.60f, -0.34f), hock = new Vector3(0.23f, 0.31f, -0.7f), bpaw = new Vector3(0.23f, 0.07f, -0.64f);
            float sw = 36f, bd = 46f;
            QLegs(c, torso, "legFL", 1f, shoulder, elbow, wrist, fpaw, 0.18f, 0.115f, 0.082f, MS.Body, MS.Dark, 0f, sw, bd * 0.6f, false, 0.15f);
            QLegs(c, torso, "legFR", -1f, shoulder, elbow, wrist, fpaw, 0.18f, 0.115f, 0.082f, MS.Body, MS.Dark, Mathf.PI, sw, bd * 0.6f, false, 0.15f);
            QLegs(c, torso, "legBL", 1f, hipB, knee, hock, bpaw, 0.19f, 0.105f, 0.078f, MS.Body, MS.Dark, Mathf.PI, sw * 0.9f, bd, true, 0.15f);
            QLegs(c, torso, "legBR", -1f, hipB, knee, hock, bpaw, 0.19f, 0.105f, 0.078f, MS.Body, MS.Dark, 0f, sw * 0.9f, bd, true, 0.15f);

            var tail = c.New("tail", torso, new Vector3(0f, 1.2f, -0.82f), delegate (MkPart p)
            {
                Tube(p.S, MS.Body, new[] { new Vector3(0f, 1.2f, -0.82f), new Vector3(0f, 1.14f, -1.3f), new Vector3(0f, 1.0f, -1.82f) },
                     new[] { 0.1f, 0.16f * ruff, 0.1f }, new[] { 0.1f, 0.16f * ruff, 0.1f }, 9, Vector3.up, true, true);
                Ell(p.S, MS.Pale, new Vector3(0f, 0.95f, -1.92f), new Vector3(0.085f, 0.11f, 0.19f), Quaternion.Euler(-30f, 0f, 0f), 4, 9);
            });
            c.rig.Sway(tail.t, Vector3.up, 8f, 2.2f, 0f, 14f);

            // gerak badan/kepala/rahang: ancang-ancang (wind) lalu terkam (lunge)
            Vector3 tPos0 = torso.t.localPosition; Quaternion tq = torso.t.localRotation, hq = head.t.localRotation, jq = jaw.t.localRotation;
            c.rig.Add(delegate (MonsterRig r)
            {
                float bob = Mathf.Abs(Mathf.Sin(r.phase * 0.5f)) * 0.05f * r.spd01;
                torso.t.localPosition = tPos0 + new Vector3(0f, bob - 0.1f * r.wind, 0.38f * r.lunge - 0.12f * r.wind);
                torso.t.localRotation = tq * Quaternion.AngleAxis(9f * r.wind - 8f * r.lunge, Vector3.right);
                head.t.localRotation = hq * Quaternion.AngleAxis(-12f * r.wind + 22f * r.lunge + Mathf.Sin(r.phase) * 3f * r.spd01, Vector3.right);
                jaw.t.localRotation = jq * Quaternion.AngleAxis(8f * r.wind + 38f * r.lunge + (1f + Mathf.Sin(r.time * 3f)) * 1.5f, Vector3.right);
            });
        }
    }
}
