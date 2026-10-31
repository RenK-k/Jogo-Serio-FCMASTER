using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CoachMaster.Career
{
    public static class LanRoom
    {
        static readonly object Gate = new object();
        static readonly Queue<string> Inbox = new Queue<string>();
        static readonly Dictionary<int, StreamWriter> Writers = new Dictionary<int, StreamWriter>();
        static readonly Dictionary<int, string> Clubs = new Dictionary<int, string>();
        static readonly List<TcpClient> Clients = new List<TcpClient>();
        static TcpListener _listener;
        static int _nextId;

        public static bool IsHost { get; private set; }
        public static bool IsGuest { get; private set; }
        public static bool PendingShare;
        public static bool HoldShare;
        public static string Status { get; private set; } = "";

        public static bool Connected
        {
            get
            {
                lock (Gate)
                    return Writers.Count > 0;
            }
        }

        public static string LocalIPv4Address()
        {
            try
            {
                var addresses = Dns.GetHostAddresses(Dns.GetHostName());
                string fallback = null;
                for (var i = 0; i < addresses.Length; i++)
                {
                    var address = addresses[i];
                    if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                        continue;
                    var bytes = address.GetAddressBytes();
                    if (bytes[0] == 169 && bytes[1] == 254)
                        continue;
                    if (bytes[0] == 10 || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                        || (bytes[0] == 192 && bytes[1] == 168))
                        return address.ToString();
                    if (fallback == null)
                        fallback = address.ToString();
                }
                return fallback ?? "";
            }
            catch
            {
                return "";
            }
        }

        public static void StartHost(int port)
        {
            if (IsHost && _listener != null)
                return;
            Stop();
            if (port <= 0 || port > 65535)
                port = CareerOnline.DefaultPort;
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
            }
            catch (Exception error)
            {
                Status = error.Message;
                _listener = null;
                return;
            }
            IsHost = true;
            Status = "Servidor aberto na porta " + port + ".";
            var thread = new Thread(AcceptLoop) { IsBackground = true, Name = "CoachMasterLan" };
            thread.Start();
        }

        public static void Join(string ip, int port, string name)
        {
            Stop();
            IsGuest = true;
            Status = "A ligar…";
            if (port <= 0 || port > 65535)
                port = CareerOnline.DefaultPort;
            var host = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
            var hello = "JOIN|" + CareerOnline.Clean(name);
            var thread = new Thread(() => Connect(host, port, hello)) { IsBackground = true, Name = "CoachMasterJoin" };
            thread.Start();
        }

        public static void Remember(int id, string clubId)
        {
            if (id <= 0 || string.IsNullOrEmpty(clubId))
                return;
            lock (Gate)
                Clubs[id] = clubId;
        }

        public static string ClubFor(int id)
        {
            lock (Gate)
            {
                string clubId;
                return Clubs.TryGetValue(id, out clubId) ? clubId : "";
            }
        }

        public static void MarkConnected(CareerState career)
        {
            if (career?.Online == null)
                return;
            lock (Gate)
            {
                foreach (var pair in Clubs)
                {
                    for (var i = 0; i < career.Online.Seats.Count; i++)
                    {
                        var seat = career.Online.Seats[i];
                        if (seat != null && seat.ClubId == pair.Value)
                            seat.Connected = true;
                    }
                }
            }
        }

        public static void Reply(int id, string line)
        {
            StreamWriter writer = null;
            lock (Gate)
            {
                if (!Writers.TryGetValue(id, out writer))
                    writer = null;
            }
            Write(writer, line);
        }

        public static void Share(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            Send("FILE|" + Convert.ToBase64String(Encoding.UTF8.GetBytes(text)));
        }

        public static void Send(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            List<StreamWriter> copy;
            lock (Gate)
                copy = new List<StreamWriter>(Writers.Values);
            for (var i = 0; i < copy.Count; i++)
                Write(copy[i], line);
        }

        public static bool ClubTaken(string clubId)
        {
            if (string.IsNullOrEmpty(clubId))
                return false;
            lock (Gate)
            {
                foreach (var pair in Clubs)
                {
                    if (pair.Value == clubId)
                        return true;
                }
            }
            return false;
        }

        public static bool TryTake(out string line)
        {
            lock (Gate)
            {
                if (Inbox.Count == 0)
                {
                    line = null;
                    return false;
                }
                line = Inbox.Dequeue();
                return true;
            }
        }

        public static void Stop()
        {
            IsHost = false;
            IsGuest = false;
            PendingShare = false;
            var listener = _listener;
            _listener = null;
            if (listener != null)
            {
                try { listener.Stop(); }
                catch { }
            }
            List<TcpClient> clients;
            lock (Gate)
            {
                clients = new List<TcpClient>(Clients);
                Clients.Clear();
                Writers.Clear();
                Clubs.Clear();
                Inbox.Clear();
            }
            for (var i = 0; i < clients.Count; i++)
            {
                try { clients[i].Close(); }
                catch { }
            }
            Status = "";
        }

        static void AcceptLoop()
        {
            while (IsHost && _listener != null)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch
                {
                    break;
                }
                var id = Interlocked.Increment(ref _nextId);
                Attach(client, id, null);
            }
        }

        static void Connect(string ip, int port, string hello)
        {
            try
            {
                var client = new TcpClient();
                client.Connect(ip, port);
                Attach(client, 0, hello);
                Status = "Ligado a " + ip + ".";
            }
            catch (Exception error)
            {
                Status = error.Message;
                Enqueue("ERRO|" + error.Message);
            }
        }

        static void Attach(TcpClient client, int id, string hello)
        {
            StreamWriter writer;
            StreamReader reader;
            try
            {
                var stream = client.GetStream();
                writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                reader = new StreamReader(stream, Encoding.UTF8);
            }
            catch (Exception error)
            {
                Status = error.Message;
                try { client.Close(); }
                catch { }
                return;
            }
            lock (Gate)
            {
                Clients.Add(client);
                Writers[id] = writer;
            }
            if (!string.IsNullOrEmpty(hello))
                Write(writer, hello);
            var thread = new Thread(() => ReadLoop(reader, id)) { IsBackground = true, Name = "CoachMasterLanRead" };
            thread.Start();
        }

        static void ReadLoop(StreamReader reader, int id)
        {
            try
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (IsHost && line.StartsWith("JOIN|"))
                        Enqueue("JOIN|" + id + "|" + line.Substring(5));
                    else if (IsHost && line.StartsWith("READY"))
                        Enqueue("READY|" + id);
                    else if (IsHost && line.StartsWith("PICK|"))
                        Enqueue("PICK|" + id + "|" + line.Substring(5));
                    else
                        Enqueue(line);
                }
            }
            catch (Exception error)
            {
                Status = error.Message;
            }
        }

        static void Write(StreamWriter writer, string line)
        {
            if (writer == null || line == null)
                return;
            try
            {
                lock (writer)
                {
                    writer.WriteLine(line);
                    writer.Flush();
                }
            }
            catch (Exception error)
            {
                Status = error.Message;
            }
        }

        static void Enqueue(string line)
        {
            lock (Gate)
                Inbox.Enqueue(line);
        }
    }
}
