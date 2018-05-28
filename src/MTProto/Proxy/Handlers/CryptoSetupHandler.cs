using System;
using System.Net.Sockets;

namespace MTProto.Proxy.Handlers
{
    public class CryptoSetupHandler : IDataHandler
    {
        private Socket TelegramSocket;

        public ProxyConnection Connection { get; }
        public byte[] Buffer { get; } = new byte[64];
        public int Offset { get; private set; }

        public int ReceiveBufferLimit => Buffer.Length - Offset;
        
        public CryptoSetupHandler(ProxyConnection connection)
        {
            Connection = connection;
        }

        public static MTProtoProtocolType GetProto(byte[] buffer)
        {
            if (buffer[56] == 0xef && buffer[57] == 0xef && buffer[58] == 0xef && buffer[59] == 0xef)
            {
                return MTProtoProtocolType.AbridgedObfuscated2;
            }
            else if (buffer[56] == 0xee && buffer[57] == 0xee && buffer[58] == 0xee && buffer[59] == 0xee)
            {
                return MTProtoProtocolType.IntermediateObfuscated2;
            }
            else
            {
                return MTProtoProtocolType.None;
            }
        }

        public bool ProcessData(byte[] buffer, int offset, int length)
        {
            System.Buffer.BlockCopy(buffer, offset, Buffer, Offset, length);
            Offset += length;
            if (Offset == Buffer.Length)
            {
                Connection.Encryptor = new Cryptography.MTProtoCryptor(Buffer, true, Connection.Context.Secret);
                Connection.Decryptor = new Cryptography.MTProtoCryptor(Buffer, false, Connection.Context.Secret);

                var tmp = new byte[Buffer.Length];
                Connection.Decryptor.Transform(Buffer, 0, Buffer.Length, tmp, 0);
                var proto = GetProto(tmp);
                if (proto == MTProtoProtocolType.None && Connection.Context.CheckProto)
                {
                    return false;
                }
                Cryptography.MTProtoHelper.GenerateNewBuffer(proto, Buffer);
                var dcId = Math.Abs(BitConverter.ToInt16(tmp, 60)) - 1;
                var dcHost = Connection.Context.DataCentres[dcId % Defaults.DataCentres.Length];

                TelegramSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                TelegramSocket.BeginConnect(dcHost, 443, OnConnected, null);
            }
            return true;
        }
        private void OnConnected(IAsyncResult result)
        {
            try
            {
                TelegramSocket.EndConnect(result);
                if (!TelegramSocket.Connected)
                {
                    Connection.Close();
                    return;
                }

                var telegramConnection = new ProxyConnection(TelegramSocket, Connection);

                var encryptor = new Cryptography.MTProtoCryptor(Buffer, false);
                var decryptor = new Cryptography.MTProtoCryptor(Buffer, true);
                
                var tmp = new byte[Buffer.Length];
                encryptor.Transform(Buffer, 0, Buffer.Length, tmp, 0);
                for (var i = 0; i < 56; i++)
                {
                    tmp[i] = Buffer[i];
                }
                telegramConnection.Send(tmp, 0, tmp.Length);

                telegramConnection.Encryptor = encryptor;
                telegramConnection.Decryptor = decryptor;

                Connection.ReverseConnection = telegramConnection;
                Connection.DataHandler = new ProxyHandler(Connection);
                Connection.AssignedTabled.Register(telegramConnection);
            }
            catch
            {
                Connection.Close();
            }
        }
    }
}
