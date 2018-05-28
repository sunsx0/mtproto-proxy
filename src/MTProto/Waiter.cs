using System.Net.Sockets;

namespace MTProto
{
    public class Waiter
    {
        private static readonly Socket skt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static void SleepMicro(int microseconds)
        {
            skt.Poll(microseconds, SelectMode.SelectRead);
        }
    }
}
