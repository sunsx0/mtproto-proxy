using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace MTProto.Proxy
{
    public class ProxyManager
    {
        public ProxyContext Context { get; }

        private ISocketReceiver Receiver { get; }
        public ConnectionTable Connections { get; }
        public HashSet<SocketServer> Servers { get; } = new HashSet<SocketServer>();

        public ProxyManager(ProxyContext context, int connectionsPerThread = 64, int selectTimeout = 1, int receiveBufferSize = 4096)
        {
            Context = context;
            Receiver = new SocketSelectReceiver(connectionsPerThread, selectTimeout, receiveBufferSize);
            Connections = new ConnectionTable();
            Receiver.Subscribe(Connections);
        }

        public void Start()
        {
            foreach (var srv in Servers)
            {
                srv.Start();
            }
        }

        public void Stop()
        {
            foreach (var srv in Servers)
            {
                srv.Stop();
            }
        }

        public SocketServer AddServer(EndPoint localEndPoint, AddressFamily addressFamily, int backlog)
        {
            var server = new SocketServer(
                localEndPoint,
                Connections,
                addressFamily: addressFamily,
                backlog: backlog,
                context: Context
            );
            lock (Servers)
            {
                Servers.Add(server);
            }
            return server;
        }

        public void RemoveServer(SocketServer server)
        {
            lock (Servers)
            {
                Servers.Remove(server);
            }
        }
    }
}
