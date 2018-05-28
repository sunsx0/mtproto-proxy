using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;

namespace MTProto.Proxy
{
    public class SocketSelectReceiverThread
    {
        private object SwitchStateLock = new object();
        private object SelectLock = new object();

        public Thread Thread { get; private set; }
        public bool Works { get; private set; }
        
        private HashSet<ProxyConnection> Connections { get; } = new HashSet<ProxyConnection>();
        public int Count => Connections.Count;

        private byte[] Buffer { get; }
        private int Timeout { get; }
        public SocketSelectReceiverThread(int timeout = 1, int bufferSize = 4096)
        {
            Buffer = new byte[bufferSize];
            Timeout = timeout;
        }

        public void AddConnection(ProxyConnection connection)
        {
            lock (Connections)
            {
                Connections.Add(connection);
                if (Connections.Count == 1)
                {
                    Start();
                }
            }
            connection.Socket.ReceiveTimeout = 30000;
        }
        public void RemoveConnection(ProxyConnection connection)
        {
            lock (Connections)
            {
                Connections.Remove(connection);
                if (Connections.Count == 0)
                {
                    Stop();
                }
            }
        }

        private void Start()
        {
            lock (SwitchStateLock)
            {
                if (Works)
                {
                    return;
                }
                Thread = new Thread(ThreadLoop);
                Thread.Start(Thread);
                Works = true;
            }
        }
        private void Stop()
        {
            lock (SwitchStateLock)
            {
                if (!Works)
                {
                    return;
                }
                Thread = null;
                Works = false;
            }
        }
        List<Socket> ToRead = new List<Socket>();
        List<Socket> ToError = new List<Socket>();
        List<Socket> Closed = new List<Socket>();
        Dictionary<Socket, ProxyConnection> SocketConnections = new Dictionary<Socket, ProxyConnection>();
        private void ThreadLoop(object threadObj)
        {
            Thread thread = (Thread)threadObj;
            while (Thread == thread)
            {
                lock (SelectLock)
                {
                    ToRead.Clear();
                    ToError.Clear();
                    Closed.Clear();
                    SocketConnections.Clear();

                    lock (Connections)
                    {
                        foreach (var con in Connections)
                        {
                            if (con.ReceiveBufferLimit == 0)
                            {
                                continue;
                            }
                            if (con.Socket.Connected)
                            {
                                ToRead.Add(con.Socket);
                                ToError.Add(con.Socket);
                            }
                            else
                            {
                                Closed.Add(con.Socket);
                            }
                            SocketConnections[con.Socket] = con;
                        }
                    }
                    foreach (var skt in Closed)
                    {
                        SocketConnections[skt].Close();
                    }
                    if (ToRead.Count + ToError.Count == 0)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    Socket.Select(ToRead, null, ToError, Timeout);
                    foreach (var skt in ToRead)
                    {
                        var con = SocketConnections[skt];
                        var recvLen = con.ReceiveBufferLimit;
                        if (recvLen <= 0 || Buffer.Length < recvLen)
                        {
                            recvLen = Buffer.Length;
                        }
                        var len = skt.Receive(Buffer, 0, recvLen, SocketFlags.None, out var errorCode);
                        if (errorCode != SocketError.Success || len <= 0)
                        {
                            ToError.Add(skt);
                            continue;
                        }
                        try
                        {
                            if (!con.OnDataReceive(Buffer, 0, len))
                            {
                                ToError.Add(skt);
                            }
                        }
                        catch
                        {
                            ToError.Add(skt);
                        }
                    }
                    foreach (var skt in ToError)
                    {
                        SocketConnections[skt].Close();
                    }
                    if (ToRead.Count + ToError.Count != 0)
                    {
                        Waiter.SleepMicro(Timeout);
                    }
                }
            }
        }
    }
}
