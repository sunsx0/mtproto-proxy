namespace MTProto.Proxy
{
    public interface ISocketReceiver
    {
        void Subscribe(ConnectionTable connectionTable);
        void Unsubscribe(ConnectionTable connectionTable);
    }
}
