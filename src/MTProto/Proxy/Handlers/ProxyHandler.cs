namespace MTProto.Proxy.Handlers
{
    public class ProxyHandler : IDataHandler
    {
        public ProxyConnection Connection { get; }
        public int ReceiveBufferLimit { get; } = -1;

        public ProxyHandler(ProxyConnection connection)
        {
            Connection = connection;
        }

        public bool ProcessData(byte[] buffer, int offset, int length)
        {
            return Connection.ReverseConnection.Send(buffer, offset, length);
        }
    }
}
