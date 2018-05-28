using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Numerics;
using MTProto.Proxy;
using System.IO;

namespace tgsocks
{
    class Program
    {
        const string DefaultConfigPath = "config.json";
        static void Log(string msg)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("u")}] {msg}");
        }
        static void Main(string[] args)
        {
            if (args.Intersect(new[] { "--help", "-h" }).Count() > 0 || args.Length > 1)
            {
                Console.WriteLine("./tgsocks [CONFIG_PATH]");
            }
            var configPath = DefaultConfigPath;
            if (args.Length == 1)
            {
                configPath = args[0];
            }
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"{configPath} not found");
                return;
            }
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
            var proxyContext = new ProxyContext();
            proxyContext.CheckProto = true;
            proxyContext.DataCentres = config.DataCentres.ToArray();
            proxyContext.Secret = Enumerable.
                Range(0, config.Secret.Length / 2).
                Select(
                    x => Convert.ToByte(
                        config.Secret.Substring(x * 2, 2), 
                        16
                    )
                ).
                ToArray();
            Log("Secret: " + string.Join("", proxyContext.Secret.Select(x => x.ToString("x2"))));
            var proxyManager = new ProxyManager(
                proxyContext, 
                config.ConnectionsPerThread,
                config.SelectTimeout, 
                config.ReceiveBufferSize
            );
            foreach (var srv in config.Servers)
            {
                var ip = IPAddress.Parse(srv.Host);
                var port = srv.Port;
                proxyManager.AddServer(new IPEndPoint(ip, port), ip.AddressFamily, srv.Backlog);
            }

            proxyManager.Connections.OnConnectionRegistered += Connections_OnConnectionRegistered;
            proxyManager.Connections.OnConnectionRemoved += Connections_OnConnectionRemoved;

            Log("OK, Load config complete, starting");
            proxyManager.Start();
            Log("Server started");
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
        }

        private static void ProcessConnectionEvent(ConnectionTable connectionTable, ProxyConnection connection, string eventMessage)
        {
            var connections = connectionTable.ConnectionIndecies.Count;
            var endPoint = (IPEndPoint)connection.Socket.RemoteEndPoint;
            var target = connection.IsClient ? "client" : "telegram";
            Log($"{endPoint.Address}:{endPoint.Port} ({target}) {eventMessage} ({connections} connections)");
        }

        private static void Connections_OnConnectionRemoved(ConnectionTable connectionTable, ProxyConnection connection)
        {
            ProcessConnectionEvent(connectionTable, connection, "disconnected");
        }

        private static void Connections_OnConnectionRegistered(ConnectionTable connectionTable, ProxyConnection connection)
        {
            ProcessConnectionEvent(connectionTable, connection, "connected");
        }
    }
}
