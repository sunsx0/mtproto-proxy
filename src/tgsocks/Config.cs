using System;
using System.Collections.Generic;
using System.Text;

namespace tgsocks
{
    public class Server
    {
        public string Host { get; set; }
        public int Port { get; set; } = 443;
        public int Backlog { get; set; } = 128;
    }
    public class Config
    {
        public List<Server> Servers { get; } = new List<Server>();
        public List<string> DataCentres { get; } = new List<string>(MTProto.Defaults.DataCentres);
        public int ReceiveBufferSize { get; set; } = 4096;
        public int SelectTimeout { get; set; } = 1;
        public int ConnectionsPerThread { get; set; } = 64;
        public string Secret { get; set; }
    }
}
