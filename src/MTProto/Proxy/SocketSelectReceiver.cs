using System.Collections.Generic;

namespace MTProto.Proxy
{
    public class SocketSelectReceiver : ISocketReceiver
    {
        public int ConnectionsPerThread { get; }
        public int SelectTimeout { get; }
        public int ReceiveBufferSize { get; }
        private object ConnectionLock = new object();
        private Dictionary<ProxyConnection, SocketSelectReceiverThread> ConnectionReceiver { get; } = new Dictionary<ProxyConnection, SocketSelectReceiverThread>();
        private Stack<SocketSelectReceiverThread> IncompleteReceivers { get; } = new Stack<SocketSelectReceiverThread>();

        public SocketSelectReceiver(int connectionsPerThread, int selectTimeout = 1, int receiveBufferSize = 4096)
        {
            ConnectionsPerThread = connectionsPerThread;
            ReceiveBufferSize = receiveBufferSize;
            SelectTimeout = selectTimeout;
        }

        public void Subscribe(ConnectionTable connectionTable)
        {
            connectionTable.OnConnectionRegistered += ConnectionTable_OnConnectionRegistered;
            connectionTable.OnConnectionRemoved += ConnectionTable_OnConnectionRemoved;
        }

        public void Unsubscribe(ConnectionTable connectionTable)
        {
            connectionTable.OnConnectionRegistered -= ConnectionTable_OnConnectionRegistered;
            connectionTable.OnConnectionRemoved -= ConnectionTable_OnConnectionRemoved;
        }

        private void ConnectionTable_OnConnectionRemoved(ConnectionTable connectionTable, ProxyConnection connection)
        {
            lock (ConnectionLock)
            {
                if (ConnectionReceiver.TryGetValue(connection, out var receiver))
                {
                    if (receiver.Count == ConnectionsPerThread)
                    {
                        IncompleteReceivers.Push(receiver);
                    }
                    receiver.RemoveConnection(connection);
                    ConnectionReceiver.Remove(connection);
                }
            }
        }

        private void ConnectionTable_OnConnectionRegistered(ConnectionTable connectionTable, ProxyConnection connection)
        {
            lock (ConnectionLock)
            {
                if (IncompleteReceivers.Count == 0)
                {
                    IncompleteReceivers.Push(new SocketSelectReceiverThread(SelectTimeout, ReceiveBufferSize));
                }
                SocketSelectReceiverThread receiver = IncompleteReceivers.Peek();
                if (receiver.Count + 1 >= ConnectionsPerThread)
                {
                    IncompleteReceivers.Pop();
                }
                ConnectionReceiver[connection] = receiver;
                receiver.AddConnection(connection);
            }
        }
    }
}
