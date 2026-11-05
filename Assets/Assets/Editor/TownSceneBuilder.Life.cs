using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

public static partial class TownSceneBuilder
{
    // =====================================================================
    // Properti: lampu jalan, pohon di distrik, tong dan peti
    // =====================================================================
    static void BuildProps()
    {
        var lamps = Group("Lampu", tProps);
        var lamp = PropKit.LampProto();
        int n = 0;
        foreach (var s in streets)
        {
            if (s.pts.Count < 2) continue;
            bool wide = s.w >= 12f;
            bool lampStreet = wide || s.name.StartsWith("Lingkar") || s.name.StartsWith("Promenade");
            if (!lampStreet) continue;
            float spacing = wide ? 26f : 30f;
            float acc = spacing * 0.5f;
            for (int i = 0; i + 1 < s.pts.Count; i++)
            {
                Vector2 a = s.pts[i], b = s.pts[i + 1];
                float len = (b - a).magnitude;
                acc += len;
                if (acc < spacing) continue;
                acc = 0f;
                Vector2 tan = (b - a).normalized;
                Vector2 nrm = new Vector2(tan.y, -tan.x);
                int sides = wide ? 2 : 1;
                for (int sd = 0; sd < sides; sd++)
                {
                    float sgn = sd == 0 ? 1f : -1f;
                    Vector2 p = (a + b) * 0.5f + nrm * sgn * (s.w / 2f + 0.5f);
                    // lengan lampu menjorok ke jalan: yaw agar +X lokal menghadap jalan
                    float yaw = Mathf.Atan2(nrm.y * sgn, -nrm.x * sgn) * Mathf.Rad2Deg;
                    var go = PropKit.Place(lamp, lamps, "Lampu", V3(p, s.y), yaw, 1f, baseSlots);
                    if ((n++ % 4) == 0) AddLight(go.transform, new Vector3(0.7f, 3.3f, 0f), 15f, 1.6f);
                }
            }
        }

        // pohon peneduh di ruang kosong distrik ekonomi
        var trees = Group("PohonKota", tProps);
        int placed = 0;
        for (int i = 0; i < 3000 && placed < 90; i++)
        {
            float a = Random.Range(0f, 360f), r = Random.Range(112f, 200f);
            Vector2 p = Pol(a, r);
            if (!IsFree(p.x, p.y, 3.2f)) continue;
            Reserve(p.x, p.y, 3.2f);
            var go = PropKit.Place(PropKit.TreeProto(Random.value < 0.7f ? 0 : 5), trees, "Pohon", V3(p), Random.Range(0f, 360f), Random.Range(0.8f, 1.1f), baseSlots);
            var cc = go.AddComponent<CapsuleCollider>();
            cc.center = new Vector3(0f, 1.5f, 0f); cc.radius = 0.45f; cc.height = 3f;
            placed++;
        }
    }

    // =====================================================================
    // NPC
    // =====================================================================
    static Material[] npcMats;

    static Transform[] RoutePts(string key, List<Vector2> pts, float y = 0f)
    {
        pts = ClearRoute(pts);
        var g = Group("WP_" + key, tWp);
        var arr = new Transform[pts.Count];
        for (int i = 0; i < pts.Count; i++)
        {
            var t = new GameObject("P" + i).transform;
            t.SetParent(g, false);
            t.position = V3(pts[i], y);
            arr[i] = t;
        }
        return arr;
    }

    static List<Vector2> ArcPts(Vector2 center, float r, float a0, float a1, int count)
    {
        var l = new List<Vector2>();
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.Lerp(a0, a1, i / (float)Mathf.Max(1, count - 1)) * Mathf.Deg2Rad;
            l.Add(center + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r);
        }
        return l;
    }

    static void Npcs(string label, int count, Transform[] route)
    {
        if (route == null || route.Length < 2) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = route[Random.Range(0, route.Length)].position;
            MakeNpc("NPC_" + (++npcCount).ToString("00"), label, p, route, false);
        }
    }

    static void BuildNpcs()
    {
        // alun-alun: berkeliling pinggir
        string[] who = { "Warga", "Pedagang", "Pelajar", "Peziarah" };
        for (int i = 0; i < 4; i++)
        {
            var r = RoutePts("Plaza" + i, ArcPts(PlazaCenter(i), 28.5f, 0f, 360f, 13));
            Npcs(who[i], 3, r);
            Npcs("Hunter", 1, r);
        }
        // jalan lingkar ekonomi
        Npcs("Warga", 3, RoutePts("Lingkar122", ArcPts(Vector2.zero, 122f, 10f, 350f, 18)));
        Npcs("Pedagang", 2, RoutePts("Lingkar159", ArcPts(Vector2.zero, 159f, 35f, 145f, 8)));
        Npcs("Warga", 2, RoutePts("Lingkar159b", ArcPts(Vector2.zero, 159f, 215f, 325f, 8)));
        // distrik bangsawan
        Npcs("Bangsawan", 2, RoutePts("Promenade", ArcPts(Vector2.zero, 66f, 8f, 352f, 18)));
        Npcs("Pelayan", 2, RoutePts("Promenade2", ArcPts(Vector2.zero, 66f, 100f, 260f, 10)));
        // petani di jalan ladang
        for (int q = 0; q < 4; q++)
        {
            float ac = 45f + q * 90f;
            Npcs("Petani", 2, RoutePts("Ladang" + q, ArcPts(Vector2.zero, 272f, ac - 30f, ac + 30f, 7)));
        }

        // prajurit di dalam kota
        Vector2 pb = WallKit.Out(225f) * 144f;
        MakeNpc("NPC_Prajurit", "Prajurit", V3(pb + new Vector2(-13f, -13f)), null, true);
        MakeNpc("NPC_Prajurit", "Prajurit", V3(pb + new Vector2(13f, 13f)), null, true);
        // penjaga kastil
        MakeNpc("NPC_Penjaga_Kastil", "Penjaga", new Vector3(-6f, 0f, -42f), null, true);
        MakeNpc("NPC_Penjaga_Kastil", "Penjaga", new Vector3(6f, 0f, -42f), null, true);
        PlacePropertyNpcs(new Vector3(-13f, 0f, -42f), new Vector3(13f, 0f, -42f), GuildConfig.TownScene);
        PlaceManaCrystals(new Vector3(-24f, 0f, -42f), new Vector3(24f, 0f, -42f));
    }

    static readonly int[] CivilianMix = { NpcKit.OWarga, NpcKit.OWarga, NpcKit.OWarga, NpcKit.OPendeta, NpcKit.OPenyihir, NpcKit.OBard, NpcKit.OKoki,
                                          NpcKit.OPandai, NpcKit.OPengembara, NpcKit.OPetugas, NpcKit.OPelajar };
    static System.Random npcRng = new System.Random(77);

    static int OutfitForLabel(string label)
    {
        switch (label)
        {
            case "Pedagang": return NpcKit.OPedagang;
            case "Pelajar": return NpcKit.OPelajar;
            case "Peziarah": return npcRng.Next(2) == 0 ? NpcKit.OPendeta : NpcKit.OPengembara;
            case "Hunter": return NpcKit.OHunter;
            case "Bangsawan": return NpcKit.OBangsawan;
            case "Pelayan": return NpcKit.OPelayan;
            case "Petani": return NpcKit.OPetani;
            case "Penjaga": case "Prajurit": return NpcKit.OPenjaga;
            case "Ksatria": return NpcKit.OKsatria;
            default: return CivilianMix[npcRng.Next(CivilianMix.Length)];
        }
    }

    /// <summary>Agen Properti + Penjual Furnitur (menu rumah, lihat PropertyMenus).</summary>
    static void PlacePropertyNpcs(Vector3 agentPos, Vector3 furnPos, string townScene)
    {
        // Kantor Properti dibangun lebih dulu (dekat alun-alun), lalu rumah-rumah dijual di sekitarnya.
        // Agen Properti & Penjual Furnitur kini berada DI DALAM kantor (scene Interior_PropOffice).
        Vector2 anchor0 = new Vector2((agentPos.x + furnPos.x) * 0.5f, (agentPos.z + furnPos.z) * 0.5f);
        if (complexScene == townScene) return;                       // sudah dibangun sebagai Komplek Properti
        if (PlacePropertyComplex(townScene, anchor0)) return;
        PlacePropertyOffice(townScene, (agentPos + furnPos) * 0.5f);
        PlaceHomes(townScene, new Vector2((agentPos.x + furnPos.x) * 0.5f, (agentPos.z + furnPos.z) * 0.5f));
    }

    /// <param name="faceYaw">arah hadap penjaga/NPC diam (derajat); NaN = menghadap pusat peta seperti semula</param>
    static GameObject MakeNpc(string name, string label, Vector3 pos, Transform[] route, bool elite, float faceYaw = float.NaN)
    {
        int outfit = OutfitForLabel(label);
        var spec = NpcKit.RandomSpec(outfit, npcRng);
        if (elite) spec.elite = true;
        var go = NpcKit.Spawn(tNpcs, name, pos, 0f, spec, true);
        go.transform.position = pos;
        string shown = NpcKit.OutfitName[outfit];
        var actor = go.GetComponent<NpcActor>();
        actor.displayName = spec.name;
        var lab = BuildKit.Text(go.transform, spec.name + "\n<" + shown + ">", new Vector3(0f, actor.height + 0.3f, 0f), 0f, 0.045f, Color.white);
        var npc = go.AddComponent<WanderNPC>();
        npc.waypoints = route;
        npc.label = lab.transform;
        npc.speed = 1.1f + (float)npcRng.NextDouble() * 0.5f;
        bool guard = route == null || route.Length < 2;
        if (guard) go.transform.rotation = Quaternion.Euler(0f, float.IsNaN(faceYaw) ? YawToward(new Vector2(pos.x, pos.z), Vector2.zero) : faceYaw, 0f);
        if (outfit != NpcKit.OPenjaga && outfit != NpcKit.OKsatria) go.AddComponent<NpcChat>();
        else actor.greetRadius = 4f;
        return go;
    }

    // =====================================================================
    // Pemain, kamera, manajer
    // =====================================================================
    static void BuildPlayerAndManagers()
    {
        Material playerMat = UMat("Player", new Color(0.2f, 0.4f, 0.85f));
        var pl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pl.name = "Player";
        pl.tag = "Player";
        pl.transform.position = new Vector3(0f, 1.1f, GateSouthZ + 32f);
        pl.GetComponent<Renderer>().sharedMaterial = playerMat;
        Object.DestroyImmediate(pl.GetComponent<CapsuleCollider>());
        var cc = pl.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero; cc.stepOffset = 0.4f;
        pl.AddComponent<PlayerController>();
        pl.AddComponent<HunterGuild.PlayerAvatar>();
        Prim(PrimitiveType.Cube, "FacingMarker", pl.transform, new Vector3(0f, 0.5f, 0.45f), new Vector3(0.3f, 0.3f, 0.5f), mMarker, false);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 3f, GateSouthZ + 20f);
            cam.farClipPlane = 1500f;
            cam.gameObject.AddComponent<ThirdPersonCamera>().target = pl.transform;
        }

        new GameObject("GameManager").AddComponent<GameState>();
        new GameObject("TownUI").AddComponent<TownUI>();
        new GameObject("BackpackUI").AddComponent<BackpackUI>();
        new GameObject("SkillUI").AddComponent<SkillUI>();
        var sunGo = GameObject.Find("Directional Light");
        if (sunGo != null)
        {
            var dn = new GameObject("DayNight").AddComponent<TownDayNight>();
            dn.sun = sunGo.GetComponent<Light>();
        }
    }
}
