using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>
/// Kota Ashwatch "Konsentris": kehidupan kota. NPC warga (rute dari T2.NpcRoutes, tidak melewati jembatan/sungai/bangunan), penjaga,
/// 10 NPC tersembunyi (h1..h10, id & hadiah sama dengan denah lama), 8 kejadian acak (karavan, bard, pencuri, lentera, kelopak, parade, anak-anak, sihir),
/// serta pemain, kamera, dan manajer. Jalur kejadian mengikuti tinggi medan (PathMover.followPathY) dan naik-turun lengkung jembatan.
/// </summary>
public static partial class TownSceneBuilder
{
    // =====================================================================
    // NPC warga + penjaga
    // =====================================================================
    /// <summary>Titik rute NPC: tinggi = medan (WanderNPC juga menempel ke terrain saat Play).</summary>
    static Transform[] T2RoutePts(string key, List<Vector2> pts)
    {
        pts = ClearRoute(pts);
        var g = Group("WP_" + key, tWp);
        var arr = new Transform[pts.Count];
        for (int i = 0; i < pts.Count; i++)
        {
            var t = new GameObject("P" + i).transform;
            t.SetParent(g, false);
            t.position = T2P(pts[i].x, pts[i].y);
            arr[i] = t;
        }
        return arr;
    }

    static GameObject T2Guard(string name, string label, Vector2 want, float faceYaw, bool elite, bool snap)
    {
        Vector2 p = snap ? T2FreeSpot(want, 0.8f, 8, 0f, true) : want;
        return MakeNpc(name, label, T2P(p.x, p.y), null, elite, faceYaw);
    }

    static void T2Npcs()
    {
        T2.BuildRoutes();
        foreach (var note in T2.RouteNotes) Debug.Log("[Kota T2] rute: " + note);
        int routes = 0;
        foreach (var r in T2.NpcRoutes)
        {
            var wp = T2RoutePts(r.key, r.pts);
            if (wp.Length < 2) continue;
            routes++;
            // tiap NPC mulai di titik rute berbeda (dibagi rata + acak kecil), jadi tidak bertumpuk di satu titik
            int m = r.who.Length;
            for (int k = 0; k < m; k++)
            {
                int idx = Mathf.Min(wp.Length - 1, (int)((k + Random.value * 0.6f) * wp.Length / (float)m));
                MakeNpc("NPC_" + (++npcCount).ToString("00"), r.who[k], wp[idx].position, wp, false);
            }
        }

        // halaman barak (2 prajurit) + halaman istal (1 penjaga)
        T2Guard("NPC_Prajurit", "Prajurit", T2PtOr("guard_barak_a", new Vector2(-26f, -301f)), 90f, true, true);
        T2Guard("NPC_Prajurit", "Prajurit", T2PtOr("guard_barak_b", new Vector2(-26f, -291f)), 90f, true, true);
        T2Guard("NPC_Penjaga_Istal", "Penjaga", T2PtOr("guard_istal", new Vector2(33f, -297f)), 270f, true, true);

        // penjaga kastil: di depan pintu kastil, menghadap keluar
        var cl = T2.LotOf("castle");
        if (cl != null)
        {
            foreach (float sg in new[] { -1f, 1f })
            {
                Vector2 q = T2.ToWorld(cl, sg * 5.5f, cl.d * 0.5f + 3.4f);
                T2Guard("NPC_Penjaga_Kastil", "Penjaga", q, cl.yaw, true, false);
            }
        }

        // Kristal Mana: dua di alun-alun gerbang (kota bawah, terbuka untuk semua level)
        Vector2 ma = T2FreeSpot(T2PtOr("mana_a", new Vector2(-12f, -246f)), 1.6f, 10, 0.5f, true), mb = T2FreeSpot(T2PtOr("mana_b", new Vector2(47f, -258f)), 1.6f, 10, 0.5f, true);
        PlaceManaCrystals(T2P(ma.x, ma.y), T2P(mb.x, mb.y));
        Debug.Log("[Kota T2] NPC warga: " + npcCount + " di " + routes + " rute");
    }

    // =====================================================================
    // 10 NPC tersembunyi
    // =====================================================================
    static float T2FaceRoad(Vector2 p)
    {
        float best = 1e12f; Vector2 bp = Vector2.zero;
        foreach (var r in T2.Roads)
        {
            if (r.dead) continue;
            Vector2 tg; Vector2 q = T2.NearestOnPoly(p.x, p.y, r.pts, out tg);
            float d = (q - p).sqrMagnitude;
            if (d < best) { best = d; bp = q; }
        }
        return T2YawTo(p, bp);
    }

    /// <summary>Gerombolan kecil prop (tong/peti/karung/jerami) di belakang NPC tersembunyi supaya ia terlihat "bersembunyi di sudut".</summary>
    static void T2Nook(Transform g, Vector2 p, float behindYaw, int kind)
    {
        System.Action<GeoBag> build;
        string key;
        switch (kind)
        {
            case 0: key = "T2_NookTong"; build = b => { V2Kit.Barrel(b, new Vector3(-0.8f, 0f, 0f)); V2Kit.Barrel(b, new Vector3(0.7f, 0f, 0.3f)); V2Kit.Crate(b, new Vector3(0f, 0f, -0.9f), 1f, 14f); }; break;
            case 1: key = "T2_NookPeti"; build = b => { V2Kit.Crate(b, new Vector3(-0.7f, 0f, 0f), 1f, 8f); V2Kit.Crate(b, new Vector3(0.7f, 0f, 0.2f), 0.85f, -12f); V2Kit.Sack(b, new Vector3(0f, 0f, -0.9f), 1f); }; break;
            default: key = "T2_NookJerami"; build = b => { V2Kit.Bale(b, new Vector3(-0.8f, 0f, 0f), 10f); V2Kit.Bale(b, new Vector3(0.8f, 0f, 0.1f), -20f); V2Kit.Sack(b, new Vector3(0f, 0f, -0.8f), 0.9f); }; break;
        }
        float a = behindYaw * Mathf.Deg2Rad;
        Vector2 q = p + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 1.7f;
        var go = PropKit.Place(PropKit.Get(key, build), g, "Sudut", T2P(q.x, q.y), behindYaw + 180f, 1f, baseSlots);
        BoxCol(go.transform, "SudutCol", new Vector3(0f, 0.6f, 0f), new Vector3(2.4f, 1.2f, 1.6f), Vector3.zero);
        Reserve(q.x, q.y, 1.6f);
    }

    static void T2HiddenNpcs()
    {
        tHidden = Group("HiddenNPCs", tTown);
        var W = T2.HiddenWant;
        var defs = new List<HDef>
        {
            new HDef("h1","Kakek Elf Penjaga Hutan","Sylvaranis",NpcKit.OPengembara,NpcKit.Elf,0,W[0],120,"herb",5,
                "Ah, kau menemukanku. Sudah dua ratus tahun aku bersembunyi dari keramaian ini...|Hutan di selatan menyimpan banyak rahasia. Bawa herb ini, ia tumbuh di tempat yang diberkati."),
            new HDef("h2","Bibi Pembuat Pai","Bibi Marta",NpcKit.OKoki,NpcKit.Human,1,W[1],60,"bread",4,
                "Psst! Resep pai rahasiaku tak boleh diketahui koki Guild. Ini, coba satu!|Kau Hunter yang kurus sekali. Makan, makan!"),
            new HDef("h3","Dwarf Pandai Pemabuk","Gorrim Si Palu Tumpul",NpcKit.OPandai,NpcKit.Dwarf,0,W[2],150,"iron_ingot",3,
                "Hik! ...Kau lihat? Palu ini ditempa dari bintang jatuh! ...atau mungkin dari panci. Hik!|Ambil ingot ini, aku sedang baik hati. Jangan bilang istriku."),
            new HDef("h4","Gadis Kucing Kecil","Nyaa-chan",NpcKit.OWarga,NpcKit.Beast,1,W[3],40,"potion",1,
                "Nya! Kamu menemukanku?! Biasanya tak ada yang melihat kalau aku sembunyi di balik tong!|Aku menemukan ini di jalan. Mau? ...Bukan hasil curian, kok! Nya."),
            new HDef("h5","Penyihir Misterius","Vaelis Sang Berjubah Bintang",NpcKit.OPenyihir,NpcKit.Elf,1,W[4],200,"flare",2,
                "Mana di sekitarmu bergetar... kau ditakdirkan menemukanku.|Hmm, aku menciptakan sihir yang bisa mengubah batu menjadi roti. Hasilnya... batu berlapis tepung."),
            new HDef("h6","Penyair Buta","Orlan",NpcKit.OBard,NpcKit.Human,0,W[5],80,"potion",1,
                "Aku tak bisa melihatmu, tapi langkahmu terdengar seperti pahlawan.|Dengarkan, kisah tentang Hunter yang menolak naik pangkat demi sebuah roti hangat..."),
            new HDef("h7","Pendeta yang Lupa","Bapa Anselm",NpcKit.OPendeta,NpcKit.Human,0,W[6],100,"herb",3,
                "Eh? Aku sedang mencari... apa tadi? Ah, doa! Aku lupa doanya.|Berkatilah dirimu sendiri, Nak. Aku sedang sibuk mengingat."),
            new HDef("h8","Bangsawan Menyamar","Lady Isolde",NpcKit.OWarga,NpcKit.Human,1,W[7],300,"",0,
                "Sst! Aku menyamar jadi rakyat jelata. Jangan bilang ayahku, sang Duke!|Kota ini lebih menarik dari balik tembok kastil. Ini, untuk tutup mulutmu."),
            new HDef("h9","Petani Rubah","Kitsu",NpcKit.OPetani,NpcKit.Beast,0,W[8],70,"wolf_fang",1,
                "Ekorku sering dikira rumput kering oleh sapi-sapi. Menyebalkan!|Hari ini aku menemukan taring di ladang. Kau lebih butuh daripada aku."),
            new HDef("h10","Dwarf Peta Tua","Brundi Si Kartografer",NpcKit.OPelajar,NpcKit.Dwarf,1,W[9],160,"leather",2,
                "Peta dunia belum selesai... masih ada wilayah yang tak pernah kujelajahi!|Kau berhasil menemukan sepuluh dari kami? Hebat. Hunter sejati punya mata tajam."),
        };
        int[] nookKind = { -1, 1, 0, 0, -1, -1, -1, -1, 2, -1 };           // h2 peti, h3/h4 tong, h9 jerami
        var nooks = Group("SudutSembunyi", tHidden);
        int i = 0;
        foreach (var d in defs)
        {
            var rng = new System.Random(900 + i);
            var spec = NpcKit.RandomSpec(d.outfit, rng, d.race, d.female);
            spec.name = d.name;
            if (d.race == NpcKit.Beast) spec.beast = d.id == "h4" ? 0 : 2;
            if (d.id == "h4") { spec.hair = 3; spec.hat = 0; }
            Vector2 p = T2FreeSpot(d.pos, 1.2f, 14, 3f, false);
            float face = T2FaceRoad(p);
            var go = NpcKit.Spawn(tHidden, "Hidden_" + d.id, T2P(p.x, p.y), face + Random.Range(-40f, 40f), spec, true);
            go.transform.position = T2P(p.x, p.y);
            Reserve(p.x, p.y, 1.6f);
            var actor = go.GetComponent<NpcActor>(); actor.silent = true;
            var h = go.AddComponent<HiddenNpc>();
            h.id = d.id; h.title = d.title; h.lines = d.lines; h.rewardGold = d.gold; h.rewardItem = d.item; h.rewardCount = d.count;
            var m = BuildKit.Text(go.transform, "?", new Vector3(0f, 2.6f, 0f), 0f, 0.14f, new Color(1f, 0.9f, 0.3f));
            h.marker = m.transform;
            m.SetActive(false);
            if (nookKind[i] >= 0) T2Nook(nooks, p, face + 180f, nookKind[i]);
            i++;
        }
    }

    // =====================================================================
    // Kejadian acak
    // =====================================================================
    static Transform[] T2EvPath(Transform parent, string key, IList<Vector3> pts)
    {
        var arr = new Transform[pts.Count];
        for (int i = 0; i < pts.Count; i++)
        {
            var t = new GameObject(key + "_P" + i).transform;
            t.SetParent(parent, false);
            t.position = pts[i];
            arr[i] = t;
        }
        return arr;
    }

    static PathMover T2EvMover(GameObject go, Transform[] path, float speed, float delay = 0f)
    {
        var pm = EvMover(go, path, speed, delay);
        pm.followPathY = true;
        return pm;
    }

    /// <summary>Titik menyeberangi jembatan (lengkung parabola seperti mesh jembatan), dari ujung selatan/barat ke seberang. lat = geser lateral.</summary>
    static List<Vector3> T2BridgeCross(T2.Bridge br, float lat, bool forward, int n)
    {
        var l = new List<Vector3>();
        float a = br.yaw * Mathf.Deg2Rad;
        Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), nr = new Vector2(t.y, -t.x);
        float hump = br.wide ? 1.9f : 1.6f;
        for (int i = 0; i <= n; i++)
        {
            float k = -1f + 2f * i / n;
            if (!forward) k = -k;
            Vector2 p = br.c + t * (k * br.L * 0.5f) + nr * lat;
            l.Add(new Vector3(p.x, br.y + hump * (1f - k * k) + 0.02f, p.y));
        }
        return l;
    }

    /// <summary>Titik menyeberangi jembatan parit di depan gerbang: dek rata setinggi lantai gerbang, menurun halus ke sisi luar (sama dengan mesh T2MoatBridge). inbound = dari sisi luar ke gerbang.</summary>
    static List<Vector3> T2MoatCross(T2.Bridge br, float lat, bool inbound, int n)
    {
        var l = new List<Vector3>();
        float a = br.yaw * Mathf.Deg2Rad;
        Vector2 t = new Vector2(Mathf.Sin(a), Mathf.Cos(a)), nr = new Vector2(t.y, -t.x);
        float y0 = T2G(br.gate.x, br.gate.y) + 0.02f;
        Vector2 far = br.c + t * (br.L * 0.5f + 0.6f);
        float drop = Mathf.Clamp(T2G(far.x, far.y) - y0 + 0.02f, -4.5f, 0.6f);
        for (int i = 0; i <= n; i++)
        {
            float f = i / (float)n;                                                       // 0 = sisi gerbang, 1 = sisi luar
            if (inbound) f = 1f - f;
            Vector2 p = br.c + t * ((f - 0.5f) * br.L) + nr * lat;
            l.Add(new Vector3(p.x, y0 + drop * f + 0.04f, p.y));
        }
        return l;
    }

    static T2.Bridge T2BridgeOf(string id)
    {
        foreach (var b in T2.Bridges) if (b.id == id) return b;
        return null;
    }

    /// <summary>Jalur arak-arakan: jalan raya dari Gerbang Selatan melewati alun-alun gerbang, belok ke timur di depan Guild (jalur antara barisan lapak dan muka Guild).</summary>
    static List<Vector3> T2ParadeBase()
    {
        float aEnd = T2.ArcAt("raya", 4.6f, -236f);
        var path = T2P3(T2.RoadSeg("raya", 0f, aEnd, 0f, 12f));
        var gl = T2.LotOf("guild");
        if (gl != null)
        {
            Vector2 e = T2.ToWorld(gl, 6f, gl.d * 0.5f + 4.2f);
            path.AddRange(T2P3(new List<Vector2> { new Vector2(10f, e.y), e }));
        }
        return path;
    }

    static void T2Events()
    {
        tEvents = Group("RandomEvents", tTown);
        var mgr = tEvents.gameObject.AddComponent<TownEventManager>();
        var list = new List<TownEvent>();
        System.Func<Transform, string, string, float, TownEvent> mk = (g, title, ann, dur) =>
        {
            var ev = g.gameObject.AddComponent<TownEvent>();
            ev.title = title; ev.announce = ann; ev.duration = dur;
            ev.show = new[] { g.gameObject };
            list.Add(ev);
            return ev;
        };
        var tPaths = Group("EventPaths", tEvents);
        Vector2 fgate = T2.FountainGate, pmid = T2.PlazaMid;

        // 1. Karavan pedagang: Gerbang Luar Selatan -> alun-alun gerbang -> jembatan sungai -> Gerbang Tengah Selatan -> simpang kota tengah
        {
            var g = EvGroup("Ev_Karavan");
                        var br = T2BridgeOf("jembatan_raya");
            var brOut = T2BridgeOf("jembatan_Gerbang_Luar_Selatan");
            var brMid = T2BridgeOf("jembatan_Gerbang_Tengah_Selatan");
            var path = new List<Vector3>();
            if (br != null)
            {
                float ba = br.yaw * Mathf.Deg2Rad;
                Vector2 bt = new Vector2(Mathf.Sin(ba), Mathf.Cos(ba));
                Vector2 b0 = br.c - bt * (br.L * 0.5f + 2.5f), b1 = br.c + bt * (br.L * 0.5f + 2.5f);
                if (brOut != null) path.AddRange(T2MoatCross(brOut, 0f, true, 8));                // jembatan parit di depan Gerbang Luar Selatan
                path.AddRange(T2P3(T2.RoadSeg("raya", 0f, T2.ArcAt("raya", b0.x, b0.y), 0f, 12f)));
                path.AddRange(T2BridgeCross(br, 0f, true, 10));                                     // jembatan sungai
                if (brMid != null)
                {
                    path.AddRange(T2MoatCross(brMid, 0f, true, 8));                                 // parit kering di depan Gerbang Tengah Selatan
                    float ag = T2.ArcAt("raya", brMid.gate.x, brMid.gate.y);
                    path.AddRange(T2P3(T2.RoadSeg("raya", ag + 6f, T2.ArcAt("raya", 0f, -100f), 0f, 12f)));
                }
                else path.AddRange(T2P3(T2.RoadSeg("raya", T2.ArcAt("raya", b1.x, b1.y), T2.ArcAt("raya", 0f, -100f), 0f, 12f)));
            }
            else path.AddRange(T2ParadeBase());
            var pts = T2EvPath(tPaths, "Karavan", path);
            var wagon = new GameObject("Karavan").transform; wagon.SetParent(g, false);
            EvWagon(wagon);
            var mv = new List<PathMover> { T2EvMover(wagon.gameObject, pts, 2.4f) };
            for (int k = 0; k < 2; k++)
            {
                var n = EvNpc(g, "PengawalKaravan" + k, k == 0 ? NpcKit.OPedagang : NpcKit.OKsatria, -1, Vector3.zero, 0f, 1f, -1, "Karavan dari kota seberang! Barang langka, harga murah!|Minggir, minggir! Roda wagon ini berat!");
                mv.Add(T2EvMover(n, pts, 2.4f, 2.6f + k * 1.4f));
            }
            var ev = mk(g, "Karavan Pedagang", "Sebuah karavan pedagang memasuki kota lewat Gerbang Selatan...", 150f);
            ev.movers = mv.ToArray(); ev.hours = new Vector2(7f, 18f);
        }
        // 2. Pertunjukan bard di alun-alun gerbang (di antara dua barisan lapak)
        {
            var g = EvGroup("Ev_Bard");
            Vector2 c = T2FreeSpot(T2PtOr("bard", new Vector2(-10f, -258f)), 4f, 14, 0.3f, true);
            var bard = EvNpc(g, "BardPanggung", NpcKit.OBard, -1, T2P(c.x, c.y, 0.4f), 180f, 1f, -1, "♪ Oh, pahlawan dari Guild nan jauh~ ♪|Tepuk tangan untuk sang Hunter!");
            bard.GetComponent<NpcAnimator>().Wave(60f);
            var plat = new GeoBag(); plat.Cyl(Sl.Wood, Vector3.zero, 2.2f, 0.4f, 14, 2.2f, true, true);
            plat.Emit(g, "Panggung", baseSlots).transform.position = T2P(c.x, c.y);
            for (int k = 0; k < 7; k++)
            {
                float a = (-70f + k * 23f) * Mathf.Deg2Rad;
                Vector2 q = c + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * (4.2f + (k % 2) * 1.1f);
                var o = EvNpc(g, "Penonton" + k, CivilianMix[npcRng.Next(CivilianMix.Length)], -1, T2P(q.x, q.y), YawToward(q, c));
                o.GetComponent<NpcActor>().customLines = new[] { "Lagunya indah sekali!", "Bravo!", "Lagi, lagi!" };
            }
            var ev = mk(g, "Pertunjukan Bard", "Alunan kecapi terdengar dari alun-alun Gerbang. Sebuah pertunjukan bard dimulai!", 100f);
            ev.movers = new PathMover[0]; ev.hours = new Vector2(10f, 21f);
        }
        // 3. Pencuri dikejar penjaga di Jalan Lingkar (timur)
        {
            var g = EvGroup("Ev_Pencuri");
            var pts = T2EvPath(tPaths, "Pencuri", T2P3(T2.RoadSeg("lintas", 150f, 395f, 0f, 12f)));
            var mv = new List<PathMover>();
            var th = EvNpc(g, "Pencuri", NpcKit.OPengembara, NpcKit.Beast, Vector3.zero, 0f, 1f, -1, "Ha ha! Tak akan tertangkap!");
            mv.Add(T2EvMover(th, pts, 6.2f));
            for (int k = 0; k < 2; k++)
            {
                var gd = EvNpc(g, "PenjagaKejar" + k, NpcKit.OPenjaga, -1, Vector3.zero, 0f, 1f, -1, "Berhenti, pencuri!|Tangkap dia!");
                mv.Add(T2EvMover(gd, pts, 5.6f, 2.2f + k * 0.8f));
            }
            var ev = mk(g, "Pencuri Dikejar", "\"Tangkap dia!\" — seorang pencuri berlari melewati jalan lintas dikejar penjaga!", 45f);
            ev.movers = mv.ToArray(); ev.hours = new Vector2(6f, 23f);
        }
        // 4. Festival lentera (malam) di atas alun-alun gerbang
        {
            var g = EvGroup("Ev_Lentera");
            Material lamp = TownMats.Emissive(TownMats.Mk("Ev_Lantern", new Color(1f, 0.55f, 0.25f), null, 1f, 0.3f), new Color(2.6f, 1.1f, 0.4f));
            Material lamp2 = TownMats.Emissive(TownMats.Mk("Ev_Lantern2", new Color(0.75f, 0.85f, 1f), null, 1f, 0.3f), new Color(1.3f, 1.8f, 2.6f));
            for (int k = 0; k < 28; k++)
            {
                float a = k * Mathf.PI * 2f / 28f; float r = 6f + (k % 4) * 3.5f;
                Vector2 q = fgate + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r;
                var l = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                l.name = "Lentera" + k; l.transform.SetParent(g, false);
                l.transform.position = T2P(q.x, q.y, 6.5f + (k % 5) * 1.6f);
                l.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                l.GetComponent<Renderer>().sharedMaterial = k % 3 == 0 ? lamp2 : lamp;
                Object.DestroyImmediate(l.GetComponent<Collider>());
                var sp = l.AddComponent<SpinObject>(); sp.axis = Vector3.up; sp.degreesPerSecond = 20f + k;
            }
            var ev = mk(g, "Festival Lentera", "Lentera-lentera beterbangan di atas alun-alun Gerbang. Festival lentera dimulai!", 130f);
            ev.movers = new PathMover[0]; ev.hours = new Vector2(18.5f, 23.5f);
        }
        // 5. Hujan kelopak bunga dari taman sakura (kota tengah, sisi barat laut)
        {
            var g = EvGroup("Ev_Kelopak");
            var go = new GameObject("Kelopak"); go.transform.SetParent(g, false);
            Vector2 kp0 = T2PtOr("kelopak", new Vector2(-150f, 118f));
            go.transform.position = T2P(kp0.x, kp0.y, 26f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.startLifetime = 9f; main.startSpeed = 1.2f; main.startSize = 0.22f; main.maxParticles = 700; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = 0.1f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.74f, 0.82f), new Color(1f, 0.92f, 0.95f));
            var em = ps.emission; em.rateOverTime = 70f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(110f, 90f, 1f);
            var rend = go.GetComponent<ParticleSystemRenderer>();
            Shader shd = Shader.Find("Sprites/Default");
            if (shd != null && rend != null) rend.sharedMaterial = new Material(shd);
            var ev = mk(g, "Hujan Kelopak", "Angin membawa kelopak bunga sakura dari taman di sisi barat kota tengah...", 60f);
            ev.movers = new PathMover[0]; ev.hours = new Vector2(7f, 18f);
        }
        // 6. Parade ksatria: dari Gerbang Selatan, memutari air mancur bundaran, sampai depan Guild
        {
            var g = EvGroup("Ev_Parade");
            var pts = T2EvPath(tPaths, "Parade", T2ParadeBase());
            var mv = new List<PathMover>();
            for (int k = 0; k < 6; k++)
            {
                var n = EvNpc(g, "Ksatria" + k, k == 0 ? NpcKit.OBangsawan : NpcKit.OKsatria, -1, Vector3.zero, 0f, 1f, -1, "Hormat kepada raja!|Demi kota ini!");
                var sp = n.GetComponent<NpcActor>(); sp.silent = false;
                mv.Add(T2EvMover(n, pts, 2.0f, (k / 2) * 1.8f + (k % 2) * 0.01f));
                mv[mv.Count - 1].sideOffset = (k % 2 == 0 ? -1.4f : 1.4f);
            }
            var ev = mk(g, "Parade Ksatria", "Terompet berbunyi! Parade ksatria kerajaan berbaris di jalan utama.", 100f);
            ev.movers = mv.ToArray(); ev.hours = new Vector2(8f, 17f);
        }
        // 7. Anak-anak berlarian di alun-alun batu poles (bukit mahkota)
        {
            var g = EvGroup("Ev_Anak");
            var pts = T2EvPath(tPaths, "Anak", T2P3(T2.ArcPts(pmid, 11.5f, 0f, 340f, 14)));
            var mv = new List<PathMover>();
            for (int k = 0; k < 4; k++)
            {
                var n = EvNpc(g, "Anak" + k, NpcKit.OWarga, -1, Vector3.zero, 0f, 0.62f, -1, "Kejar aku!|Hehe, kamu lambat!|Ibu, lihat ada Hunter!");
                mv.Add(T2EvMover(n, pts, 4.4f, k * 0.7f));
            }
            var ev = mk(g, "Anak-anak Bermain", "Anak-anak berlarian riang di alun-alun batu poles di bawah Katedral.", 60f);
            ev.movers = mv.ToArray(); ev.hours = new Vector2(8f, 18f);
        }
        // 8. Pertunjukan sihir di parvis Katedral
        {
            var g = EvGroup("Ev_Sihir");
            Vector2 c = T2FreeSpot(T2PtOr("sihir", new Vector2(-34f, 36f)), 3.5f, 14, 0.3f, true);
            var mage = EvNpc(g, "PenyihirJalanan", NpcKit.OPenyihir, NpcKit.Elf, T2P(c.x, c.y), 180f, 1f, -1, "Lihatlah keajaiban elf!|Abra... kadabra!");
            mage.GetComponent<NpcAnimator>().Wave(60f);
            Material orb = TownMats.Emissive(TownMats.Mk("Ev_Orb", new Color(0.55f, 0.7f, 1f), null, 1f, 0.6f), new Color(1.2f, 1.8f, 3f));
            Vector3 hub = T2P(c.x, c.y, 3.2f);
            for (int k = 0; k < 5; k++)
            {
                var o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                o.name = "Orb" + k; o.transform.SetParent(g, false);
                o.transform.position = hub;
                o.transform.localScale = Vector3.one * 0.45f;
                o.GetComponent<Renderer>().sharedMaterial = orb;
                Object.DestroyImmediate(o.GetComponent<Collider>());
                var pv = new GameObject("Pivot" + k).transform; pv.SetParent(g, false); pv.position = hub;
                pv.rotation = Quaternion.Euler(k * 22f, k * 72f, 0f);
                o.transform.SetParent(pv, true); o.transform.localPosition = new Vector3(1.6f, 0f, 0f);
                var sp = pv.gameObject.AddComponent<SpinObject>(); sp.axis = Vector3.up; sp.degreesPerSecond = 80f + k * 25f;
            }
            var ev = mk(g, "Pertunjukan Sihir", "Cahaya biru berputar di parvis Katedral: seorang penyihir jalanan sedang beraksi!", 80f);
            ev.movers = new PathMover[0]; ev.hours = new Vector2(9f, 20f);
        }
        mgr.events = list.ToArray();
    }

    // =====================================================================
    // Pemain, kamera, manajer
    // =====================================================================
    static void T2PlayerAndManagers()
    {
        Material playerMat = UMat("Player", new Color(0.2f, 0.4f, 0.85f));
        var pl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        pl.name = "Player";
        pl.tag = "Player";
        var rya = T2.RoadOf("raya");
        Vector2 rtg; Vector2 sp0 = T2.At(rya.pts, 38f, out rtg);
        pl.transform.position = T2P(sp0.x, sp0.y, 1.1f);                                // di dalam Gerbang Selatan, di jalan raya, menghadap utara
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
            Vector2 cp0 = T2.At(rya.pts, 22f, out rtg);
            cam.transform.position = T2P(cp0.x, cp0.y, 3f);
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
