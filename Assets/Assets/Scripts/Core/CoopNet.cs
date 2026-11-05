using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HunterGuild
{
    internal class CoopConn
    {
        public TcpClient tcp; public NetworkStream ns; public int id; public string name = "?";
        public volatile bool dead; public RemotePlayer rp; public string lastScene = "";
        readonly object wl = new object();
        public void Send(string line)
        {
            if (dead) return;
            try { var b = Encoding.UTF8.GetBytes(line + "\n"); lock (wl) ns.Write(b, 0, b.Length); }
            catch (Exception) { dead = true; }
        }
        public void Close() { dead = true; try { tcp.Close(); } catch (Exception) { } }
    }


    /// <summary>
    /// v35z102n33: Co-op LAN ala "Buka ke LAN" Minecraft. Satu pemain jadi HOST (pemilik dunia & semua monster), sampai 3 tamu bergabung lewat Wi-Fi/LAN yang sama
    /// (penemuan otomatis UDP, atau ketik alamat IP). Tiap pemain memakai save/karakter sendiri; tidak ada yang tertukar di file save.
    ///   - Pemain saling terlihat (model dari kustomisasi karakter masing-masing) + nama + chat (Enter).
    ///   - Di area hunt (scene HuntArea*), HOST mengatur semua monster. Tamu melihat monster yang sama (boneka), memberi damage lewat host,
    ///     dan mendapat kredit kill/EXP/drop sendiri saat monster mati. Monster menyerang host MAUPUN tamu.
    ///   - Di scene lain (kota, desa, Menara, Arena) tiap pemain bermain sendiri-sendiri tapi tetap bisa saling melihat bila satu scene.
    /// Protokol teks per baris lewat TCP (port 47770); penemuan lewat UDP broadcast (port 47771). Tanpa paket tambahan Unity.
    /// Batasan ada di README.
    /// </summary>
    public class CoopNet : MonoBehaviour
    {
        public const int TcpPort = 47770, UdpPort = 47771, MaxGuests = 3;
        public const string Proto = "HGCOOP1";

        public enum Mode { Off, Host, Client }
        public static Mode Current { get; private set; }
        public static bool IsHost { get { return Current == Mode.Host; } }
        public static bool IsClient { get { return Current == Mode.Client; } }
        public static bool Connected { get { return Current != Mode.Off; } }
        public static string Status = "";
        public static int MyId;
        public static string MyName = "Hunter";
        public static string HostScene = "";

        static CoopNet inst;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (inst != null) return;
            var go = new GameObject("CoopNet"); DontDestroyOnLoad(go);
            inst = go.AddComponent<CoopNet>(); go.AddComponent<CoopUI>();
        }

        // ---------------- koneksi ----------------
        static readonly ConcurrentQueue<KeyValuePair<CoopConn, string>> inbox = new ConcurrentQueue<KeyValuePair<CoopConn, string>>();
        static readonly List<CoopConn> conns = new List<CoopConn>();            // host: tamu; client: hanya host
        static readonly object connLock = new object();
        static TcpListener listener; static Thread acceptTh; static volatile bool running;
        static UdpClient udpAnnounce; static float nextAnnounce, nextPose, nextSnap;
        static int nextId = 1;
        static CoopConn hostConn;                                            // sisi client
        static readonly Dictionary<int, RemotePlayer> players = new Dictionary<int, RemotePlayer>();
        static readonly Dictionary<int, Monster> puppets = new Dictionary<int, Monster>();
        static float lastSnapAt = -99f; static string lastSnapScene = "";
        public static readonly List<string> Chat = new List<string>();

        // ---------------- API ----------------
        public static string LocalIp()
        {
            try { foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList) if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString(); } catch (Exception) { }
            return "?";
        }

        public static bool StartHost(string name)
        {
            if (Connected) return false;
            try
            {
                MyName = Clean(name); MyId = 0; nextId = 1;
                listener = new TcpListener(IPAddress.Any, TcpPort); listener.Start();
                running = true;
                acceptTh = new Thread(AcceptLoop) { IsBackground = true }; acceptTh.Start();
                udpAnnounce = new UdpClient { EnableBroadcast = true };
                Current = Mode.Host; Application.runInBackground = true;
                Status = "Dunia dibuka. Alamat: " + LocalIp() + ":" + TcpPort;
                AddChat("* Dunia LAN dibuka. Teman bisa bergabung dari menu Main Bersama.");
                return true;
            }
            catch (Exception e) { Status = "Gagal membuka dunia: " + e.Message; Stop(); return false; }
        }

        public static void Join(string address, string name)
        {
            if (Connected) return;
            MyName = Clean(name); Status = "Menghubungkan ke " + address + " ...";
            string host = address; int port = TcpPort;
            int c = address.LastIndexOf(':');
            if (c > 0 && int.TryParse(address.Substring(c + 1), out port)) host = address.Substring(0, c); else port = TcpPort;
            Current = Mode.Client; running = true; Application.runInBackground = true;
            var th = new Thread(() =>
            {
                try
                {
                    var t = new TcpClient { NoDelay = true, SendTimeout = 500 };
                    var ar = t.BeginConnect(host.Trim(), port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(4000)) { t.Close(); connectFail = "Waktu habis: host tidak merespons."; return; }
                    t.EndConnect(ar);
                    var cn = new CoopConn { tcp = t, ns = t.GetStream() };
                    lock (connLock) conns.Add(cn);
                    pendingHost = cn;
                    StartReader(cn);
                }
                catch (Exception e) { connectFail = "Gagal: " + e.Message; }
            }) { IsBackground = true };
            th.Start();
        }
        static volatile string connectFail; static volatile CoopConn pendingHost;

        public static void Stop()
        {
            if (IsHost) Broadcast("BYE|0");
            else if (hostConn != null) hostConn.Send("BYE|" + MyId);
            running = false;
            try { if (listener != null) listener.Stop(); } catch (Exception) { }
            try { if (udpAnnounce != null) udpAnnounce.Close(); } catch (Exception) { }
            lock (connLock) { foreach (var c in conns) c.Close(); conns.Clear(); }
            listener = null; udpAnnounce = null; hostConn = null; pendingHost = null; connectFail = null;
            foreach (var p in players.Values) if (p != null) Destroy(p.gameObject);
            players.Clear();
            foreach (var m in puppets.Values) if (m != null) Destroy(m.gameObject);
            puppets.Clear();
            Current = Mode.Off; HostScene = "";
            if (Status == "" || !Status.StartsWith("Gagal")) Status = "Terputus.";
        }

        static string Clean(string s) { s = (s ?? "").Replace("|", "").Replace("\n", "").Replace(";", "").Replace(",", "").Trim(); if (s.Length > 16) s = s.Substring(0, 16); return s == "" ? "Hunter" : s; }
        public static void AddChat(string s) { Chat.Add(s); if (Chat.Count > 40) Chat.RemoveAt(0); chatShownAt = Time.unscaledTime; }
        public static float chatShownAt;

        // ---------------- thread ----------------
        static void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    var t = listener.AcceptTcpClient();
                    t.NoDelay = true; t.SendTimeout = 500;
                    var cn = new CoopConn { tcp = t, ns = t.GetStream() };
                    lock (connLock) conns.Add(cn);
                    StartReader(cn);
                }
                catch (Exception) { if (!running) return; Thread.Sleep(50); }
            }
        }

        static void StartReader(CoopConn cn)
        {
            new Thread(() =>
            {
                var buf = new byte[4096]; var line = new List<byte>();
                try
                {
                    while (!cn.dead)
                    {
                        int n = cn.ns.Read(buf, 0, buf.Length);
                        if (n <= 0) break;
                        for (int i = 0; i < n; i++)
                        {
                            if (buf[i] == (byte)'\n') { if (line.Count > 0) inbox.Enqueue(new KeyValuePair<CoopConn, string>(cn, Encoding.UTF8.GetString(line.ToArray()))); line.Clear(); }
                            else if (line.Count < 65536) line.Add(buf[i]);
                        }
                    }
                }
                catch (Exception) { }
                cn.dead = true;
            }) { IsBackground = true }.Start();
        }

        // ---------------- util ----------------
        static string F(float v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }
        static bool P(string s, out float v) { return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && !float.IsNaN(v) && !float.IsInfinity(v); }
        static string B64(string s) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? "")); }
        static string UnB64(string s) { try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); } catch (Exception) { return ""; } }
        static string CharJson() { try { return JsonUtility.ToJson(CharacterData.Current); } catch (Exception) { return ""; } }

        static void Broadcast(string line, CoopConn except = null)
        {
            lock (connLock) foreach (var c in conns) if (c != except && c.id > 0 && !c.dead) c.Send(line);
        }

        static string ActiveScene() { return SceneManager.GetActiveScene().name; }
        public static bool SyncScene(string scene) { return scene != null && scene.StartsWith("HuntArea"); }

        static Vector3 MyFeet(out float yaw)
        {
            yaw = 0f;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return Vector3.zero;
            yaw = p.transform.eulerAngles.y;
            var cc = p.GetComponent<CharacterController>();
            Vector3 pos = p.transform.position;
            if (cc != null) pos.y = cc.bounds.min.y;
            return pos;
        }

        static string PoseLine()
        {
            float yaw; Vector3 f = MyFeet(out yaw);
            var pc = PlayerCombat.I;
            float hp = pc != null ? Mathf.Clamp01(pc.hp / Mathf.Max(1f, pc.MaxHp)) : 1f;
            return "P|" + MyId + "|" + ActiveScene() + "|" + F(f.x) + "|" + F(f.y) + "|" + F(f.z) + "|" + F(yaw) + "|" + F(hp);
        }

        // ---------------- loop ----------------
        void Update()
        {
            if (!Connected) return;
            if (connectFail != null) { string msg = connectFail; connectFail = null; Stop(); Status = msg; return; }
            if (IsClient && pendingHost != null)
            {
                hostConn = pendingHost; pendingHost = null;
                hostConn.Send("HI|" + Proto + "|" + MyName + "|" + B64(CharJson()));
                Status = "Terhubung, menunggu sambutan host...";
            }
            KeyValuePair<CoopConn, string> kv;
            int guard = 0;
            while (guard++ < 400 && inbox.TryDequeue(out kv)) { try { Handle(kv.Key, kv.Value); } catch (Exception e) { Debug.LogWarning("[Coop] " + e.Message); } }

            // putus?
            if (IsClient && hostConn != null && hostConn.dead) { AddChat("* Koneksi ke host terputus."); Stop(); Status = "Koneksi ke host terputus."; return; }
            if (IsHost)
            {
                lock (connLock)
                    for (int i = conns.Count - 1; i >= 0; i--)
                        if (conns[i].dead) { DropGuest(conns[i]); conns.RemoveAt(i); }
            }

            float now = Time.unscaledTime;
            if ((IsHost || (IsClient && hostConn != null && MyId > 0)) && now >= nextPose)
            {
                nextPose = now + 0.1f;
                string pl = PoseLine();
                if (IsHost) Broadcast(pl); else hostConn.Send(pl);
            }
            if (IsHost && now >= nextAnnounce) { nextAnnounce = now + 1.5f; Announce(); }
            if (IsHost && now >= nextSnap && SyncScene(ActiveScene())) { nextSnap = now + 0.12f; SendSnapshot(); }
            if (IsClient) SuppressLocalMonsters();
        }

        void OnApplicationQuit() { if (Connected) Stop(); }

        void Announce()
        {
            try
            {
                int n = 1; lock (connLock) foreach (var c in conns) if (c.id > 0 && !c.dead) n++;
                string msg = "HGLAN|" + Proto + "|" + MyName + "|" + TcpPort + "|" + n + "|" + ActiveScene();
                var b = Encoding.UTF8.GetBytes(msg);
                udpAnnounce.Send(b, b.Length, new IPEndPoint(IPAddress.Broadcast, UdpPort));
            }
            catch (Exception) { }
        }

        // ---------------- pesan ----------------
        void Handle(CoopConn from, string line)
        {
            var a = line.Split('|');
            if (a.Length == 0) return;
            switch (a[0])
            {
                case "HI":          // host: tamu baru
                    if (!IsHost || a.Length < 4) return;
                    if (a[1] != Proto) { from.Send("NO|Versi game berbeda."); from.Close(); return; }
                    int cnt = 0; lock (connLock) foreach (var c in conns) if (c.id > 0 && !c.dead) cnt++;
                    if (cnt >= MaxGuests) { from.Send("NO|Dunia penuh."); from.Close(); return; }
                    from.id = nextId++; from.name = Clean(a[2]);
                    from.Send("WE|" + from.id + "|" + MyName + "|" + ActiveScene());
                    from.Send("PL|0|" + MyName + "|" + B64(CharJson()));
                    lock (connLock) foreach (var c in conns) if (c != from && c.id > 0) from.Send("PL|" + c.id + "|" + c.name + "|" + B64(c.rp != null ? c.rp.charJson : ""));
                    string cj = UnB64(a[3]);
                    from.rp = RemotePlayer.Create(from.id, from.name, cj, from);
                    players[from.id] = from.rp;
                    Broadcast("PL|" + from.id + "|" + from.name + "|" + a[3], from);
                    AddChat("* " + from.name + " bergabung.");
                    Broadcast("S|" + from.name + " bergabung.", from);
                    break;
                case "NO":
                    Status = "Ditolak: " + (a.Length > 1 ? a[1] : ""); Stop(); Status = "Ditolak: " + (a.Length > 1 ? a[1] : ""); break;
                case "WE":          // client: disambut host
                    if (!IsClient || a.Length < 4) return;
                    int id; if (!int.TryParse(a[1], out id)) return;
                    MyId = id; HostScene = a[3];
                    Status = "Tersambung ke dunia " + Clean(a[2]) + " (kamu pemain #" + id + ").";
                    AddChat("* Tersambung ke dunia " + Clean(a[2]) + ".");
                    break;
                case "PL":          // daftar pemain
                    if (a.Length < 4) return;
                    int pid; if (!int.TryParse(a[1], out pid) || pid == MyId) return;
                    if (!players.ContainsKey(pid) || players[pid] == null) players[pid] = RemotePlayer.Create(pid, Clean(a[2]), UnB64(a[3]), null);
                    break;
                case "P":
                    if (a.Length < 8) return;
                    int sid; float x, y, z, yaw, hp;
                    if (!int.TryParse(a[1], out sid) || sid == MyId || !P(a[3], out x) || !P(a[4], out y) || !P(a[5], out z) || !P(a[6], out yaw) || !P(a[7], out hp)) return;
                    if (IsHost) { if (from.id != sid) return; }
                    RemotePlayer rp;
                    if (players.TryGetValue(sid, out rp) && rp != null) rp.SetPose(a[2], new Vector3(x, y, z), yaw, hp);
                    if (sid == 0) HostScene = a[2];
                    if (IsHost) Broadcast(line, from);
                    break;
                case "C":
                    if (a.Length < 3) return;
                    int cid; if (!int.TryParse(a[1], out cid)) return;
                    string who = cid == 0 ? MyHostName() : (players.ContainsKey(cid) && players[cid] != null ? players[cid].pname : "?");
                    if (IsHost && from.id != cid) return;
                    string txt = a[2].Length > 200 ? a[2].Substring(0, 200) : a[2];
                    AddChat(who + ": " + txt);
                    if (IsHost) Broadcast(line, from);
                    break;
                case "S": if (a.Length > 1) AddChat("* " + a[1]); break;
                case "BYE":
                    if (IsHost) { from.dead = true; }
                    else if (a.Length > 1 && a[1] == "0") { AddChat("* Host menutup dunia."); Stop(); Status = "Host menutup dunia."; }
                    else { int bid; if (a.Length > 1 && int.TryParse(a[1], out bid)) RemovePlayer(bid); }
                    break;
                case "M":           // client: snapshot monster
                    if (!IsClient || a.Length < 3) return;
                    ApplySnapshot(a[1], a[2]);
                    break;
                case "K":           // client: monster mati
                    if (!IsClient || a.Length < 2) return;
                    int kid; Monster pm;
                    if (int.TryParse(a[1], out kid) && puppets.TryGetValue(kid, out pm)) { puppets.Remove(kid); if (pm != null) pm.NetDie(); }
                    break;
                case "D":           // host: tamu memberi damage ke monster
                    if (!IsHost || a.Length < 3) return;
                    int mid; float dmg;
                    if (!int.TryParse(a[1], out mid) || !P(a[2], out dmg)) return;
                    dmg = Mathf.Clamp(dmg, 0f, 5000f);
                    foreach (var m in Monster.All) if (m != null && !m.netPuppet && m.GetInstanceID() == mid) { m.NetDamage(dmg); break; }
                    break;
                case "H":           // client: kamu terkena serangan monster
                    if (!IsClient || a.Length < 4) return;
                    float hd, hx, hz;
                    if (!P(a[1], out hd) || !P(a[2], out hx) || !P(a[3], out hz)) return;
                    var pc = PlayerCombat.I;
                    if (pc != null && !pc.IsDead) pc.TakeDamage(Mathf.Clamp(hd, 0f, 2000f), new Vector3(hx, pc.transform.position.y, hz));
                    break;
            }
        }

        static string MyHostName() { return IsHost ? MyName : (players.ContainsKey(0) && players[0] != null ? players[0].pname : "Host"); }

        void DropGuest(CoopConn c)
        {
            if (c.id <= 0) return;
            RemovePlayer(c.id);
            AddChat("* " + c.name + " keluar.");
            Broadcast("BYE|" + c.id, c);
            Broadcast("S|" + c.name + " keluar.", c);
        }

        static void RemovePlayer(int id)
        {
            RemotePlayer p;
            if (players.TryGetValue(id, out p)) { players.Remove(id); if (p != null) Destroy(p.gameObject); }
        }

        // ---------------- chat ----------------
        public static void SendChat(string text)
        {
            text = (text ?? "").Replace("|", "/").Replace("\n", " ").Trim();
            if (text == "") return;
            if (!Connected) { AddChat("(belum tersambung ke dunia LAN)"); return; }
            AddChat(MyName + ": " + text);
            string l = "C|" + MyId + "|" + text;
            if (IsHost) Broadcast(l); else if (hostConn != null) hostConn.Send(l);
        }

        // ---------------- monster ----------------
        void SendSnapshot()
        {
            var sb = new StringBuilder(); int n = 0;
            foreach (var m in Monster.All)
            {
                if (m == null || m.IsDead || m.netPuppet || m.Def == null || m.Def.passive || m.Def.id == "dummy") continue;
                if (n++ >= 60) break;
                if (sb.Length > 0) sb.Append(';');
                var t = m.transform.position;
                sb.Append(m.GetInstanceID()).Append(',').Append(m.DefId).Append(',').Append(F(t.x)).Append(',').Append(F(t.y)).Append(',').Append(F(t.z)).Append(',')
                  .Append(F(m.transform.eulerAngles.y)).Append(',').Append(F(m.hp)).Append(',').Append(F(m.MaxHp)).Append(',').Append(m.NetState).Append(',').Append(m.isBoss ? 1 : 0);
            }
            Broadcast("M|" + ActiveScene() + "|" + sb);
        }

        void ApplySnapshot(string scene, string body)
        {
            if (scene != ActiveScene()) return;
            lastSnapAt = Time.unscaledTime; lastSnapScene = scene;
            var seen = new HashSet<int>();
            foreach (var e in body.Split(';'))
            {
                if (e.Length == 0) continue;
                var f = e.Split(',');
                if (f.Length < 10) continue;
                int id; float x, y, z, yaw, hp, mx; int st;
                if (!int.TryParse(f[0], out id) || !P(f[2], out x) || !P(f[3], out y) || !P(f[4], out z) || !P(f[5], out yaw) || !P(f[6], out hp) || !P(f[7], out mx) || !int.TryParse(f[8], out st)) continue;
                if (MonsterDB.Get(f[1]) == null) continue;
                seen.Add(id);
                Monster m;
                if (!puppets.TryGetValue(id, out m) || m == null)
                {
                    var go = new GameObject("NetMonster_" + f[1]);
                    go.transform.position = new Vector3(x, y, z);
                    m = go.AddComponent<Monster>();
                    m.defId = f[1]; m.netPuppet = true; m.netId = id; m.isBoss = f[9] == "1";
                    puppets[id] = m;
                }
                m.NetApply(new Vector3(x, y, z), yaw, hp, mx, st);
            }
            var gone = new List<int>();
            foreach (var kv in puppets) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (var id in gone) { var m = puppets[id]; puppets.Remove(id); if (m != null && !m.IsDead) Destroy(m.gameObject); }
        }

        /// <summary>Client: hapus monster lokal di area yang disinkronkan (host yang mengatur monster). Hewan buruan & boneka latihan tetap lokal.</summary>
        void SuppressLocalMonsters()
        {
            if (!SyncScene(ActiveScene()) || ActiveScene() != lastSnapScene || Time.unscaledTime - lastSnapAt > 2.5f) return;
            foreach (var m in new List<Monster>(Monster.All))
                if (m != null && !m.netPuppet && !m.IsDead && m.Def != null && !m.Def.passive && m.Def.id != "dummy") Destroy(m.gameObject);
        }

        public static void SendDamage(int netId, float dmg)
        {
            if (IsClient && hostConn != null) hostConn.Send("D|" + netId + "|" + F(dmg));
        }

        public static void OnMonsterDied(Monster m)
        {
            if (!IsHost || m == null || m.netPuppet) return;
            Broadcast("K|" + m.GetInstanceID());
        }

        public static void HitPlayer(RemotePlayer rp, float dmg, Vector3 from)
        {
            if (!IsHost || rp == null || rp.conn == null || rp.conn.dead) return;
            rp.conn.Send("H|" + F(dmg) + "|" + F(from.x) + "|" + F(from.z));
        }

        /// <summary>Serangan area boss: kena semua tamu di dalam lingkaran.</summary>
        public static void HitRemotesInDisk(Vector3 center, float r, float dmg)
        {
            if (!IsHost) return;
            foreach (var p in players.Values)
            {
                if (p == null || !p.InThisScene) continue;
                Vector3 d = p.transform.position - center; d.y = 0f;
                if (d.magnitude <= r + 0.4f) p.Hit(dmg, center);
            }
        }

        // ---------------- info untuk UI ----------------
        public static List<string> PlayerLines()
        {
            var l = new List<string>();
            l.Add((IsHost ? MyName : MyHostName()) + "  (host)" + (IsHost ? "  - " + ActiveScene() : "  - " + HostScene));
            if (IsClient) l.Add(MyName + "  (kamu)");
            foreach (var kv in players) if (kv.Key != 0 && kv.Value != null) l.Add(kv.Value.pname + "  - " + kv.Value.scene + "  HP " + Mathf.RoundToInt(kv.Value.hpFrac * 100f) + "%");
            return l;
        }

        // ---------------- penemuan ----------------
        public class Found { public string name, ip, scene; public int port, players; public float seen; }
        public static readonly Dictionary<string, Found> FoundWorlds = new Dictionary<string, Found>();
        static readonly ConcurrentQueue<Found> foundQ = new ConcurrentQueue<Found>();
        static UdpClient udpListen; static volatile bool listening;

        public static void StartDiscovery()
        {
            if (listening) return;
            try
            {
                udpListen = new UdpClient();
                udpListen.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpListen.Client.Bind(new IPEndPoint(IPAddress.Any, UdpPort));
                listening = true;
                new Thread(() =>
                {
                    while (listening)
                    {
                        try
                        {
                            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                            var data = udpListen.Receive(ref ep);
                            var a = Encoding.UTF8.GetString(data).Split('|');
                            int port, pl;
                            if (a.Length >= 6 && a[0] == "HGLAN" && a[1] == Proto && int.TryParse(a[3], out port) && int.TryParse(a[4], out pl))
                                foundQ.Enqueue(new Found { name = a[2], ip = ep.Address.ToString(), port = port, players = pl, scene = a[5] });
                        }
                        catch (Exception) { if (!listening) return; }
                    }
                }) { IsBackground = true }.Start();
            }
            catch (Exception e) { Status = "Penemuan LAN gagal: " + e.Message; listening = false; }
        }

        public static void StopDiscovery() { listening = false; try { if (udpListen != null) udpListen.Close(); } catch (Exception) { } udpListen = null; }

        public static void PumpDiscovery()
        {
            Found f;
            while (foundQ.TryDequeue(out f)) { f.seen = Time.unscaledTime; FoundWorlds[f.ip + ":" + f.port] = f; }
            var old = new List<string>();
            foreach (var kv in FoundWorlds) if (Time.unscaledTime - kv.Value.seen > 6f) old.Add(kv.Key);
            foreach (var k in old) FoundWorlds.Remove(k);
        }
    }

    /// <summary>Wujud pemain lain: model karakter (dari kustomisasi mereka), nama, HP; bergerak halus mengikuti paket posisi.</summary>
    public class RemotePlayer : MonoBehaviour
    {
        public static readonly List<RemotePlayer> All = new List<RemotePlayer>();
        public int id; public string pname = "?", scene = "", charJson = ""; public float hpFrac = 1f;
        internal CoopConn conn;
        Vector3 target; float yaw; Transform model; TextMesh label; bool hasPose; float walkPhase;
        public bool InThisScene { get { return scene == SceneManager.GetActiveScene().name; } }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        internal static RemotePlayer Create(int id, string name, string charJson, CoopConn conn)
        {
            var go = new GameObject("Remote_" + name);
            DontDestroyOnLoad(go);
            var rp = go.AddComponent<RemotePlayer>();
            rp.id = id; rp.pname = name; rp.charJson = charJson; rp.conn = conn;
            rp.Build();
            return rp;
        }

        void Build()
        {
            NpcKit.Spec spec = null;
            try { var cd = JsonUtility.FromJson<CharacterData>(charJson); if (cd != null) spec = cd.ToSpec(); } catch (Exception) { }
            if (spec == null) spec = NpcKit.RandomSpec(NpcKit.OHunter, new System.Random(id * 7919 + 13));
            spec.name = pname;
            var m = NpcKit.Spawn(transform, "Model", Vector3.zero, 0f, spec, false, false);
            model = m.transform;
            var lab = new GameObject("Label"); lab.transform.SetParent(transform, false); lab.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            label = lab.AddComponent<TextMesh>();
            label.fontSize = 48; label.characterSize = 0.05f; label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center; label.color = new Color(0.6f, 0.9f, 1f);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            TextMat.Apply(lab.GetComponent<MeshRenderer>(), label.font.material);
            model.gameObject.SetActive(false); lab.SetActive(false);
        }

        public void SetPose(string sc, Vector3 pos, float y, float hp)
        {
            scene = sc; target = pos; yaw = y; hpFrac = hp;
            if (!hasPose) { transform.position = pos; hasPose = true; }
        }

        public void Hit(float dmg, Vector3 from) { CoopNet.HitPlayer(this, dmg, from); }

        public static RemotePlayer Nearest(Vector3 from, float maxDist, out float dist)
        {
            RemotePlayer best = null; dist = float.MaxValue;
            foreach (var p in All)
            {
                if (p == null || !p.InThisScene || !p.hasPose || p.hpFrac <= 0f) continue;
                Vector3 d = p.transform.position - from; d.y = 0f;
                float m = d.magnitude;
                if (m < dist && m <= maxDist) { dist = m; best = p; }
            }
            return best;
        }

        void Update()
        {
            bool vis = hasPose && InThisScene;
            if (model != null && model.gameObject.activeSelf != vis) { model.gameObject.SetActive(vis); if (label != null) label.gameObject.SetActive(vis); }
            if (!vis) return;
            Vector3 before = transform.position;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, yaw, 0f), 1f - Mathf.Exp(-12f * Time.deltaTime));
            float spd = (transform.position - before).magnitude / Mathf.Max(0.0001f, Time.deltaTime);
            walkPhase += spd * Time.deltaTime * 5f;
            if (model != null) model.localPosition = new Vector3(0f, spd > 0.3f ? Mathf.Abs(Mathf.Sin(walkPhase)) * 0.06f : 0f, 0f);
            if (label != null)
            {
                label.text = pname + "\n" + Mathf.RoundToInt(hpFrac * 100f) + "% HP";
                var cam = Camera.main; if (cam != null) label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
            }
        }
    }

    /// <summary>Alias supaya RemotePlayer tidak perlu tahu kelas CoopConn privat.</summary>
    public class CoopNetConn { }
}
