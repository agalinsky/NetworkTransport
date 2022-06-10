namespace NetworkTransport
{
    /// <summary>
    /// Internal packet only for send purpose. Can be extended.
    /// </summary>
    internal struct SendNetworkPacket
    {
        internal NetworkBuffer networkBuffer;
        internal bool isHeaderIncluded;

        internal SendNetworkPacket(NetworkBuffer networkBuffer, bool isHeaderIncluded)
        {
            this.networkBuffer = networkBuffer;
            this.isHeaderIncluded = isHeaderIncluded;
        }
    }
}
