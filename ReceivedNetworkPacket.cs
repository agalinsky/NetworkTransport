namespace NetworkTransport
{
    /// <summary>
    /// Internal packet only for receive purpose. Can be extended.
    /// </summary>
    internal struct ReceivedNetworkPacket
    {
        internal NetworkBuffer networkBuffer;

        internal ReceivedNetworkPacket(NetworkBuffer networkBuffer)
        {
            this.networkBuffer = networkBuffer;
        }
    }
}
