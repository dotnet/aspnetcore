namespace Kestrel
{
    public class ServerAddress
    {
        public string Host { get; internal set; }
        public string Path { get; internal set; }
        public int Port { get; internal set; }
        public string Scheme { get; internal set; }
    }
}