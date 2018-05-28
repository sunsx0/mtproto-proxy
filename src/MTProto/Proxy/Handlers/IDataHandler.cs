namespace MTProto.Proxy.Handlers
{
    public interface IDataHandler
    {
        int ReceiveBufferLimit { get; }
        bool ProcessData(byte[] buffer, int offset, int length);
    }
}
