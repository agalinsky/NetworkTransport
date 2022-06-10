namespace NetworkTransport
{
    /// <summary>
    /// Indicate state of connection in list of socket connections.
    /// </summary>
    internal enum ConnectionState : byte
    {
        None = 0,
        Connected = 1,
        Disconnected = 2,
    }

    internal struct NetworkConnection
    {
        internal PacketHeader header;
        internal ConnectionState state;

        internal NetworkConnection(PacketHeader packetHeader, ConnectionState connectionState)
        {
            header = packetHeader;
            state = connectionState;
        }
    }
}
