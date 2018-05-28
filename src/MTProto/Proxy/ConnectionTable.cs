using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace MTProto.Proxy
{
    public class ConnectionTable
    {
        public event Action<ConnectionTable, ProxyConnection> OnConnectionRegistered;
        public event Action<ConnectionTable, ProxyConnection> OnConnectionRemoved;

        private long NextConnectionIndex = 0;
        public ConcurrentDictionary<ProxyConnection, long> ConnectionIndecies { get; } = new ConcurrentDictionary<ProxyConnection, long>();
        public ConcurrentDictionary<long, ProxyConnection> IndexConnections { get; } = new ConcurrentDictionary<long, ProxyConnection>();

        public long Register(ProxyConnection connection)
        {
            var id = Interlocked.Increment(ref NextConnectionIndex);
            if (!ConnectionIndecies.TryAdd(connection, id) || !IndexConnections.TryAdd(id, connection))
            {
                throw new Exception("Connection register failed");
            }
            OnConnectionRegistered?.Invoke(this, connection);
            return id;
        }
        private void DisposeConnection(ProxyConnection connection)
        {
            try
            {
                connection.Socket.Close();
                connection.Socket.Dispose();
            }
            catch
            {

            }
            if (connection is IDisposable disposable)
            {
                using (disposable) { }
            }
        }
        public void Close(ProxyConnection connection)
        {
            if (ConnectionIndecies.TryRemove(connection, out var id))
            {
                IndexConnections.TryRemove(id, out connection);
                OnConnectionRemoved?.Invoke(this, connection);
                DisposeConnection(connection);
            }
        }
        public void Close(long id)
        {
            if (IndexConnections.TryRemove(id, out var connection))
            {
                if (ConnectionIndecies.TryRemove(connection, out id))
                {
                    OnConnectionRemoved?.Invoke(this, connection);
                    DisposeConnection(connection);
                }
            }
        }
        public ProxyConnection GetConnection(long id)
        {
            return IndexConnections.GetValueOrDefault(id, default(ProxyConnection));
        }
        public long GetId(ProxyConnection connection)
        {
            return ConnectionIndecies.GetValueOrDefault(connection, -1);
        }
    }
}
