using System.Net.Sockets;
using MTProto.Proxy.Handlers;
using MTProto.Proxy.Cryptography;

namespace MTProto.Proxy
{
    public class ProxyConnection
    {
        public Socket Socket { get; }
        public ProxyContext Context { get; }
        public ConnectionTable AssignedTabled { get; }

        public IDataHandler DataHandler { get; internal set; }
        public int ReceiveBufferLimit => DataHandler.ReceiveBufferLimit;

        public bool IsClient { get; }
        public ProxyConnection ReverseConnection { get; internal set; }

        public MTProtoCryptor Encryptor { get; internal set; }
        public MTProtoCryptor Decryptor { get; internal set; }

        public ProxyConnection(Socket socket, ProxyContext context, ConnectionTable assignedTable)
        {
            Socket = socket;
            Context = context;
            AssignedTabled = assignedTable;
            
            IsClient = true;
            DataHandler = new CryptoSetupHandler(this);
        }
        public ProxyConnection(Socket socket, ProxyConnection reverseConnection)
        {
            Socket = socket;
            Context = reverseConnection.Context;
            ReverseConnection = reverseConnection;
            IsClient = false;
            DataHandler = new ProxyHandler(this);
            AssignedTabled = reverseConnection.AssignedTabled;
        }

        public bool Send(byte[] buffer, int offset, int length)
        {
            if (length <= 0)
            {
                return true;
            }
            if (Encryptor != null)
            {
                var dst = new byte[length];
                Encryptor.Transform(buffer, offset, length, dst, 0);
                buffer = dst;
                offset = 0;
            }
            while (length > 0)
            {
                var len = Socket.Send(buffer, offset, length, SocketFlags.None, out var errorCode);
                if (len <= 0 || errorCode != SocketError.Success)
                {
                    return false;
                }
                offset += len;
                length -= len;
            }
            return true;
        }

        public bool OnDataReceive(byte[] buffer, int offset, int length)
        {
            if (Decryptor != null)
            {
                Decryptor.Transform(buffer, offset, length, buffer, offset);
            }
            return DataHandler.ProcessData(buffer, offset, length);
        }
        public void Close()
        {
            AssignedTabled.Close(this);
        }
    }
}
