namespace MTProto.Proxy
{
    public class ProxyContext
    {
        public byte[] Secret { get; set; }
        public bool CheckProto { get; set; }
        public string[] DataCentres { get; set; } = Defaults.DataCentres;
    }
}
