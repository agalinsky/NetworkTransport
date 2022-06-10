using System.Net;

namespace NetworkTransport
{
    /// <summary>
    /// In case of multiple servers for different lobby rooms
    /// NetworkConfig should be serializable for each machine.
    /// And provided as instance but not static through some provider.
    /// </summary>
    public static class NetworkConfig
    {
        public const int MTU = 1024;
        public const int MaxConnections = 10;
        public static readonly IPEndPoint ServerAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2345);
    }
}
