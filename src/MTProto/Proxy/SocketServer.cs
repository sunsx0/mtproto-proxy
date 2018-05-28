using System;
using System.Net;
using System.Net.Sockets;

namespace MTProto.Proxy
{
    public class SocketServer : IDisposable
    {
        private object SwitchStateLock = new object();

        public AddressFamily AddressFamily { get; }
        public EndPoint LocalEndPoint { get; }
        public int Backlog { get; }
        public ProxyContext Context { get; }

        public Socket ServerSocket { get; private set; }
        public bool Works { get; private set; }

        public ConnectionTable Connections { get; }

        public SocketServer(
            EndPoint localEndPoint,
            ConnectionTable connections,
            AddressFamily addressFamily = AddressFamily.InterNetwork,
            int backlog = 128,
            ProxyContext context = null)
        {
            LocalEndPoint = localEndPoint;
            AddressFamily = addressFamily;
            Backlog = backlog;
            Context = context;
            Connections = connections;
        }

        public void Start()
        {
            lock (SwitchStateLock)
            {
                if (Works)
                {
                    return;
                }
                ServerSocket = new Socket(AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(LocalEndPoint);
                ServerSocket.Listen(Backlog);
                BeginAccept(ServerSocket);
            }
        }
        public void Stop()
        {
            lock (SwitchStateLock)
            {
                if (!Works)
                {
                    return;
                }
                ServerSocket.Shutdown(SocketShutdown.Both);
                ServerSocket.Close();
                using (ServerSocket) ;
                ServerSocket = null;
                Works = false;
            }
        }

        protected virtual void ProcessAccepted(Socket socket)
        {
            socket.Blocking = false;
            var connection = new ProxyConnection(socket, Context, Connections);
            Connections.Register(connection);
        }

        private void BeginAccept(Socket socket)
        {
            if (socket != ServerSocket)
            {
                return;
            }
            try
            {
                socket.BeginAccept(OnSocketAccept, socket);
            }
            catch
            {
                lock (SwitchStateLock)
                {
                    if (socket == ServerSocket)
                    {
                        throw;
                    }
                }
            }
        }
        private void OnSocketAccept(IAsyncResult result)
        {
            var socket = (Socket)result.AsyncState;
            if (socket != ServerSocket)
            {
                return;
            }
            try
            {
                var accepted = socket.EndAccept(result);
                BeginAccept(socket);
                ProcessAccepted(accepted);
            }
            catch
            {
                lock (SwitchStateLock)
                {
                    if (socket == ServerSocket)
                    {
                        throw;
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServerSocket?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SocketServer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
