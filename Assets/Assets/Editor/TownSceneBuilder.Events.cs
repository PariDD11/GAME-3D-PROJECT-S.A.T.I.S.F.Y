using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HunterGuild;

/// <summary>10 NPC tersembunyi + kejadian acak kota. Semua objek dibangun di editor (tidak aktif) lalu dinyalakan oleh TownEventManager saat Play.</summary>
public static partial class TownSceneBuilder
{
    static Vector2 FreeSpot(Vector2 want, float r = 1.2f)
    {
        if (IsFree(want.x, want.y, r)) return want;
        for (int ring = 1; ring < 14; ring++)
            for (int k = 0; k < 12; k++)
            {
                float a = k * Mathf.PI * 2f / 12f;
                Vector2 p = want + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * ring * 1.5f;
                if (IsFree(p.x, p.y, r)) return p;
            }
        return want;
    }

    static Transform tHidden, tEvents;

    // =====================================================================
    // 10 NPC tersembunyi
    // =====================================================================
    class HDef
    {
        public string id, title, name, lines, item; public int outfit, race, beast = -1, female = -1, gold, count = 1; public Vector2 pos;
        public HDef(string id, string title, string name, int outfit, int race, int female, Vector2 pos, int gold, string item, int count, string lines)
        { this.id = id; this.title = title; this.name = name; this.outfit = outfit; this.race = race; this.female = female; this.pos = pos; this.gold = gold; this.item = item; this.count = count; this.lines = lines; }
    }

    static void BuildHiddenNpcs()
    {
        tHidden = Group("HiddenNPCs", tTown);
        System.Func<float, Vector2> W = WallKit.Out;
        var defs = new List<HDef>
        {
            new HDef("h1","Kakek Elf Penjaga Hutan","Sylvaranis",NpcKit.OPengembara,NpcKit.Elf,0,PlazaPos(3,-24f,22f),120,"herb",5,
                "Ah, kau menemukanku. Sudah dua ratus tahun aku bersembunyi dari keramaian ini...|Hutan di selatan menyimpan banyak rahasia. Bawa herb ini, ia tumbuh di tempat yang diberkati."),
            new HDef("h2","Bibi Pembuat Pai","Bibi Marta",NpcKit.OKoki,NpcKit.Human,1,PlazaPos(1,22f,-22f),60,"bread",4,
                "Psst! Resep pai rahasiaku tak boleh diketahui koki Guild. Ini, coba satu!|Kau Hunter yang kurus sekali. Makan, makan!"),
            new HDef("h3","Dwarf Pandai Pemabuk","Gorrim Si Palu Tumpul",NpcKit.OPandai,NpcKit.Dwarf,0,W(135f)*122f,150,"iron_ingot",3,
                "Hik! ...Kau lihat? Palu ini ditempa dari bintang jatuh! ...atau mungkin dari panci. Hik!|Ambil ingot ini, aku sedang baik hati. Jangan bilang istriku."),
            new HDef("h4","Gadis Kucing Kecil","Nyaa-chan",NpcKit.OWarga,NpcKit.Beast,1,W(300f)*140f,40,"potion",1,
                "Nya! Kamu menemukanku?! Biasanya tak ada yang melihat kalau aku sembunyi di balik tong!|Aku menemukan ini di jalan. Mau? ...Bukan hasil curian, kok! Nya."),
            new HDef("h5","Penyihir Misterius","Vaelis Sang Berjubah Bintang",NpcKit.OPenyihir,NpcKit.Elf,1,W(45f)*100f,200,"flare",2,
                "Mana di sekitarmu bergetar... kau ditakdirkan menemukanku.|Hmm, aku menciptakan sihir yang bisa mengubah batu menjadi roti. Hasilnya... batu berlapis tepung."),
            new HDef("h6","Penyair Buta","Orlan",NpcKit.OBard,NpcKit.Human,0,W(200f)*66f,80,"potion",1,
                "Aku tak bisa melihatmu, tapi langkahmu terdengar seperti pahlawan.|Dengarkan, kisah tentang Hunter yang menolak naik pangkat demi sebuah roti hangat..."),
            new HDef("h7","Pendeta yang Lupa","Bapa Anselm",NpcKit.OPendeta,NpcKit.Human,0,W(330f)*80f,100,"herb",3,
                "Eh? Aku sedang mencari... apa tadi? Ah, doa! Aku lupa doanya.|Berkatilah dirimu sendiri, Nak. Aku sedang sibuk mengingat."),
            new HDef("h8","Bangsawan Menyamar","Lady Isolde",NpcKit.OWarga,NpcKit.Human,1,W(250f)*56f,300,"",0,
                "Sst! Aku menyamar jadi rakyat jelata. Jangan bilang ayahku, sang Duke!|Kota ini lebih menarik dari balik tembok kastil. Ini, untuk tutup mulutmu."),
            new HDef("h9","Petani Rubah","Kitsu",NpcKit.OPetani,NpcKit.Beast,0,W(160f)*262f,70,"wolf_fang",1,
                "Ekorku sering dikira rumput kering oleh sapi-sapi. Menyebalkan!|Hari ini aku menemukan taring di ladang. Kau lebih butuh daripada aku."),
            new HDef("h10","Dwarf Peta Tua","Brundi Si Kartografer",NpcKit.OPelajar,NpcKit.Dwarf,1,W(20f)*190f,160,"leather",2,
                "Peta dunia belum selesai... masih ada wilayah yang tak pernah kujelajahi!|Kau berhasil menemukan sepuluh dari kami? Hebat. Hunter sejati punya mata tajam."),
        };
        int i = 0;
        foreach (var d in defs)
        {
            var rng = new System.Random(900 + i++);
            var spec = NpcKit.RandomSpec(d.outfit, rng, d.race, d.female);
            spec.name = d.name;
            if (d.race == NpcKit.Beast) spec.beast = d.id == "h4" ? 0 : 2;
            if (d.id == "h4") { spec.hair = 3; spec.hat = 0; }
            Vector2 p = FreeSpot(d.pos);
            var go = NpcKit.Spawn(tHidden, "Hidden_" + d.id, V3(p), YawToward(p, Vector2.zero) + Random.Range(-40f, 40f), spec, true);
            go.transform.position = V3(p);
            var actor = go.GetComponent<NpcActor>(); actor.silent = true;
            var h = go.AddComponent<HiddenNpc>();
            h.id = d.id; h.title = d.title; h.lines = d.lines; h.rewardGold = d.gold; h.rewardItem = d.item; h.rewardCount = d.count;
            var m = BuildKit.Text(go.transform, "?", new Vector3(0f, 2.6f, 0f), 0f, 0.14f, new Color(1f, 0.9f, 0.3f));
            h.marker = m.transform;
            m.SetActive(false);
        }
    }

    // =====================================================================
    // Kejadian acak
    // =====================================================================
    static Transform EvGroup(string name) { var g = Group(name, tEvents); g.gameObject.SetActive(false); return g; }

    static PathMover EvMover(GameObject go, Transform[] path, float speed, float delay = 0f)
    {
        var pm = go.AddComponent<PathMover>();
        pm.path = path; pm.speed = speed; pm.startDelay = delay;
        go.SetActive(false);
        return pm;
    }

    static GameObject EvNpc(Transform parent, string name, int outfit, int race, Vector3 pos, float yaw, float scale = 1f, int female = -1, string line = null)
    {
        var spec = NpcKit.RandomSpec(outfit, npcRng, race, female);
        var go = NpcKit.Spawn(parent, name, pos, yaw, spec, false);
        go.transform.position = pos; go.transform.localScale = Vector3.one * scale;
        if (line != null) go.GetComponent<NpcActor>().customLines = line.Split('|');
        return go;
    }

    static void EvWagon(Transform parent)
    {
        var b = new GeoBag();
        var P = new System.Func<float, float, float, Vector3>((x, y, z) => new Vector3(x, y, z));
        b.Box(Sl.Wood, P(0, 1.0f, -1.0f), new Vector3(1.9f, 0.18f, 3.4f));
        b.Box(Sl.WoodLight, P(-0.92f, 1.4f, -1.0f), new Vector3(0.1f, 0.7f, 3.4f));
        b.Box(Sl.WoodLight, P(0.92f, 1.4f, -1.0f), new Vector3(0.1f, 0.7f, 3.4f));
        b.Box(Sl.WoodLight, P(0f, 1.4f, -2.65f), new Vector3(1.9f, 0.7f, 0.1f));
        for (int i = 0; i < 4; i++) b.Beam(Sl.Wood, P(-0.9f, 1.75f, -2.4f + i * 0.9f), P(0.9f, 1.75f, -2.4f + i * 0.9f), 0.08f, 0.08f);
        b.Cyl(Sl.Cloth, P(0f, 1.8f, -2.5f), 1.2f, 2.9f, 3, 1.2f, true, true);     // kanopi segitiga kasar
        b.Box(Sl.ClothB, P(0f, 2.55f, -1.05f), new Vector3(2.15f, 0.12f, 3.3f));
        b.Box(Sl.Wood, P(0f, 0.95f, 1.2f), new Vector3(0.12f, 0.12f, 2.2f));       // tiang tarik
        for (int sg = -1; sg <= 1; sg += 2)
        {
            Vector3 c = P(sg * 1.05f, 0.62f, -0.4f);
            for (int k = 0; k < 8; k++)
            {
                float a0 = k * Mathf.PI / 4f, a1 = (k + 1) * Mathf.PI / 4f;
                b.Beam(Sl.Wood, c + P(0, Mathf.Sin(a0) * 0.6f, Mathf.Cos(a0) * 0.6f), c + P(0, Mathf.Sin(a1) * 0.6f, Mathf.Cos(a1) * 0.6f), 0.1f, 0.1f);
                if (k < 4) b.Beam(Sl.Wood, c + P(0, Mathf.Sin(a0) * 0.6f, Mathf.Cos(a0) * 0.6f), c - P(0, Mathf.Sin(a0) * 0.6f, Mathf.Cos(a0) * 0.6f), 0.05f, 0.05f);
            }
            b.Beam(Sl.Wood, c + P(sg * -0.08f, 0, 0), c + P(sg * -0.08f, 0, 0), 0.05f, 0.05f);
        }
        LandmarkKit.Horse(b, P(-0.55f, 0f, 2.6f), 0f);
        LandmarkKit.Horse(b, P(0.55f, 0f, 2.6f), 0f);
        for (int i = 0; i < 4; i++) b.Box(i % 2 == 0 ? Sl.Wood : Sl.WoodLight, P(-0.4f + (i % 2) * 0.8f, 1.35f + (i / 2) * 0.4f, -0.2f - i * 0.1f), new Vector3(0.6f, 0.4f, 0.6f), i * 10f);
        b.Emit(parent, "Wagon", baseSlots);
    }

    static void BuildEvents()
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

        // 1. Karavan pedagang
        {
            var g = EvGroup("Ev_Karavan");
            var route = new List<Vector2> { V(14, -345), V(14, -121), V(60, -106), V(90, -82), V(110, -52), V(120, -22), V(118, -6) };
            var pts = RoutePts("EvKaravan", route, 0f);
            foreach (var t in pts) t.SetParent(tPaths, true);
            var wagon = new GameObject("Karavan").transform; wagon.SetParent(g, false);
            EvWagon(wagon);
            var mv = new List<PathMover> { EvMover(wagon.gameObject, pts, 2.4f) };
            for (int k = 0; k < 2; k++)
            {
                var n = EvNpc(g, "PengawalKaravan" + k, k == 0 ? NpcKit.OPedagang : NpcKit.OKsatria, -1, Vector3.zero, 0f, 1f, -1, "Karavan dari kota seberang! Barang langka, harga murah!|Minggir, minggir! Roda wagon ini berat!");
                mv.Add(EvMover(n, pts, 2.4f, 2.6f + k * 1.4f));
            }
            var ev = mk(g, "Karavan Pedagang", "Sebuah karavan pedagang memasuki kota lewat Gerbang Selatan...", 150f);
            ev.movers = mv.ToArray(); ev.hours = new Vector2(7f, 18f);
        }
        // 2. Pertunjukan bard di alun-alun Pasar (Timur)
        {
            var g = EvGroup("Ev_Bard");
            Vector2 c = PlazaPos(1, 0f, 0f) + new Vector2(0f, 12f);
            c = FreeSpot(c, 4f);
            var bard = EvNpc(g, "BardPanggung", NpcKit.OBard, -1, V3(c), 180f, 1f, -1, "♪ Oh, pahlawan dari Guild nan jauh~ ♪|Tepuk tangan untuk sang Hunter!");
            bard.GetComponent<NpcAnimator>().Wave(60f);
            var plat = new GeoBag(); plat.Cyl(Sl.Wood, Vector3.zero, 2.2f, 0.4f, 14, 2.2f, true, true);
            plat.Emit(g, "Panggung", baseSlots).transform.position = V3(c);
            bard.transform.position = V3(c) + new Vector3(0f, 0.4f, 0f);
            for (int k = 0; k < 7; k++)
            {
                float a = (-70f + k * 23f) * Mathf.Deg2Rad;
                Vector2 q = c + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * (4.2f + (k % 2) * 1.1f);
                var o = EvNpc(g, "Penonton" + k, CivilianMix[npcRng.Next(CivilianMix.Length)], -1, V3(q), YawToward(q, c));
                o.GetComponent<NpcActor>().customLines = new[] { "Lagunya indah sekali!", "Bravo!", "Lagi, lagi!" };
            }
            var ev = mk(g, "Pertunjukan Bard", "Alunan kecapi terdengar dari alun-alun Pasar. Sebuah pertunjukan bard dimulai!", 100f);
            ev.movers = new PathMover[0]; ev.hours = new Vector2(10f, 21f);
        }
        // 3. Pencuri dikejar penjaga
        {
            var g = EvGroup("Ev_Pencuri");
            var pts = RoutePts("EvPencuri", ArcPts(Vector2.zero, 122f, 20f, 150f, 14), 0f);
            foreach (var t in pts) t.SetParent(tPaths, true);
            var mv = new List<PathMover>();
            var th = EvNpc(g, "Pencuri", NpcKit.OPengembara, NpcKit.Beast, Vector3.zero, 0f, 1f, -1, "Ha ha! Tak akan tertangkap!");
            mv.Add(EvMover(th, pts, 6.2f));
            for (int k = 0; k < 2; k++)
            {
                var gd = EvNpc(g, "PenjagaKejar" + k, NpcKit.OPenjaga, -1, Vector3.zero, 0f, 1f, -1, "Berhenti, pencuri!|Tangkap dia!");
                mv.Add(EvMover(gd, pts, 5.6f, 2.2f + k * 0.8f));
            }
            var ev = mk(g, "Pencuri Dikejar", "\"Tangkap dia!\" — seorang pencuri berlari melewati jalan lingkar dikejar penjaga!", 45f);
            ev.movers = mv.ToArray(); ev.hours = new Vector2(6f, 23f);
        }
        // 4. Festival lentera (malam)
        {
            var g = EvGroup("Ev_Lentera");
            Material lamp = TownMats.Emissive(TownMats.Mk("Ev_Lantern", new Color(1f, 0.55f, 0.25f), null, 1f, 0.3f), new Color(2.6f, 1.1f, 0.4f));
            Material lamp2 = TownMats.Emissive(TownMats.Mk("Ev_Lantern2", new Color(0.75f, 0.85f, 1f), null, 1f, 0.3f), new Color(1.3f, 1.8f, 2.6f));
            Vector2 pc = PlazaCenter(0);
            for (int k = 0; k < 28; k++)
            {
                float a = k * Mathf.PI * 2f / 28f; float r = 6f + (k % 4) * 3.5f;
                Vector2 q = pc + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r;
                var l = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                l.name = "Lentera" + k; l.transform.SetParent(g, false);
                l.transform.position = new Vector3(q.x, 6f + (k % 5) * 1.6f, q.y);
                l.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                l.GetComponent<Renderer>().sharedMaterial = k % 3 == 0 ? lamp2 : lamp;
                Object.DestroyImmediate(l.GetComponent<Collider>());
                var sp = l.AddComponent<SpinObject>(); sp.axis = Vector3.up; sp.degreesPerSecond = 20f + k;
            }
            var ev = mk(g, "Festival Lentera", "Lentera-lentera beterbangan di atas alun-alun Guild. Festival lentera dimulai!", 130f);
            ev.movers = new PathMover[0]; ev.hours = new Vector2(18.5f, 23.5f);
        }
        // 5. Hujan kelopak bunga
        {
            var g = EvGroup("Ev_Kelopak");
            var go = new GameObject("Kelopak"); go.transform.SetParent(g, false);
            go.transform.position = new Vector3(0f, 26f, -4f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.startLifetime = 9f; main.startSpeed = 1.2f; main.startSize = 0.22f; main.maxParticles = 700; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = 0.1f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.74f, 0.82f), new Color(1f, 0.92f, 0.95f));
            var em = ps.emission; em.rateOverTime = 70f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(130f, 130f, 1f);
            var rend = go.GetComponent<ParticleSystemRenderer>();
            Shader shd = Shader.Find("Sprites/Default");
            if (shd != null && rend != null) rend.sharedMaterial = new Material(shd);
            var ev = mk(g, "Hujan Kelopak", "Angin membawa kelopak bunga sakura dari kebun bangsawan...", 60f);
            ev.movers = new PathMover[0]; ev.hours = new Vector2(7f, 18f);
        }
        // 6. Parade ksatria
        {
            var g = EvGroup("Ev_Parade");
            // parade memutari air mancur / monumen Alun-alun Guild (tidak menembusnya)
            var prp = new List<Vector2> { V(0, -325), V(0, -200) };
            Vector2 gc = PlazaCenter(0);
            for (float ang = 180f; ang >= -0.1f; ang -= 22.5f) { float ar = ang * Mathf.Deg2Rad; prp.Add(gc + new Vector2(Mathf.Sin(ar), Mathf.Cos(ar)) * 19f); }
            prp.Add(V(0, -110)); prp.Add(V(0, -70));
            var pts = RoutePts("EvParade", prp, 0f);
            foreach (var t in pts) t.SetParent(tPaths, true);
            var mv = new List<PathMover>();
            for (int k = 0; k < 6; k++)
            {
                var n = EvNpc(g, "Ksatria" + k, k == 0 ? NpcKit.OBangsawan : NpcKit.OKsatria, -1, Vector3.zero, 0f, 1f, -1, "Hormat kepada raja!|Demi kota ini!");
                var sp = n.GetComponent<NpcActor>(); sp.silent = false;
                mv.Add(EvMover(n, pts, 2.0f, (k / 2) * 1.8f + (k % 2) * 0.01f));
                mv[mv.Count - 1].sideOffset = (k % 2 == 0 ? -1.4f : 1.4f);
            }
            var ev = mk(g, "Parade Ksatria", "Terompet berbunyi! Parade ksatria kerajaan berbaris di jalan utama.", 90f);
            ev.movers = mv.ToArray(); ev.hours = new Vector2(8f, 17f);
        }
        // 7. Anak-anak berlarian
        {
            var g = EvGroup("Ev_Anak");
            var pts = RoutePts("EvAnak", ArcPts(PlazaCenter(1), 20f, 0f, 340f, 14), 0f);
            foreach (var t in pts) t.SetParent(tPaths, true);
            var mv = new List<PathMover>();
            for (int k = 0; k < 4; k++)
            {
                var n = EvNpc(g, "Anak" + k, NpcKit.OWarga, -1, Vector3.zero, 0f, 0.62f, -1, "Kejar aku!|Hehe, kamu lambat!|Ibu, lihat ada Hunter!");
                mv.Add(EvMover(n, pts, 4.4f, k * 0.7f));
            }
            var ev = mk(g, "Anak-anak Bermain", "Anak-anak berlarian riang di alun-alun Pasar.", 60f);
            ev.movers = mv.ToArray(); ev.hours = new Vector2(8f, 18f);
        }
        // 8. Pertunjukan sihir
        {
            var g = EvGroup("Ev_Sihir");
            Vector2 c = FreeSpot(PlazaPos(3, 0f, 6f), 4f);
            var mage = EvNpc(g, "PenyihirJalanan", NpcKit.OPenyihir, NpcKit.Elf, V3(c), 180f, 1f, -1, "Lihatlah keajaiban elf!|Abra... kadabra!");
            mage.GetComponent<NpcAnimator>().Wave(60f);
            Material orb = TownMats.Emissive(TownMats.Mk("Ev_Orb", new Color(0.55f, 0.7f, 1f), null, 1f, 0.6f), new Color(1.2f, 1.8f, 3f));
            for (int k = 0; k < 5; k++)
            {
                var o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                o.name = "Orb" + k; o.transform.SetParent(g, false);
                o.transform.position = V3(c) + new Vector3(0f, 3.2f, 0f);
                o.transform.localScale = Vector3.one * 0.45f;
                o.GetComponent<Renderer>().sharedMaterial = orb;
                Object.DestroyImmediate(o.GetComponent<Collider>());
                var pv = new GameObject("Pivot" + k).transform; pv.SetParent(g, false); pv.position = V3(c) + new Vector3(0f, 3.2f, 0f);
                pv.rotation = Quaternion.Euler(k * 22f, k * 72f, 0f);
                o.transform.SetParent(pv, true); o.transform.localPosition = new Vector3(1.6f, 0f, 0f);
                var sp = pv.gameObject.AddComponent<SpinObject>(); sp.axis = Vector3.up; sp.degreesPerSecond = 80f + k * 25f;
            }
            var ev = mk(g, "Pertunjukan Sihir", "Cahaya biru berputar di alun-alun Katedral: seorang penyihir jalanan sedang beraksi!", 80f);
            ev.movers = new PathMover[0]; ev.hours = new Vector2(9f, 20f);
        }
        mgr.events = list.ToArray();
    }
}
