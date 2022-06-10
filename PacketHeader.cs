using System;
using System.Net;
using NetworkTransport.Utils;

namespace NetworkTransport
{
    /// <summary>
    /// Send request to remote socket what should do with local connection.
    /// </summary>
    internal enum ConnectionRequest : byte
    {
        StayConnectedRequest = 0,
        ConnectRequest = 1,
        DisconnectRequest = 2,
    }

    /// <summary>
    /// Contains Read/Write schema.
    /// </summary>
    internal struct PacketHeader
    {
        public IPAddress sourceAddress;
        public int sourcePort;
        public IPAddress destAddress;
        public int destPort;
        public ConnectionRequest requestType;
        public const int HeaderLength = 17;

        public IPEndPoint SourceEndPoint => new IPEndPoint(sourceAddress, sourcePort);
        public IPEndPoint DestEndPoint => new IPEndPoint(destAddress, destPort);

        public PacketHeader(IPAddress sourceAddress, int sourcePort,
                            IPAddress destAddress, int destPort,
                            ConnectionRequest requestType)
        {
            this.sourceAddress = sourceAddress;
            this.sourcePort = sourcePort;
            this.destAddress = destAddress;
            this.destPort = destPort;
            this.requestType = requestType;
        }

        public void SetRequestState(ConnectionRequest requestType)
        {
            this.requestType = requestType;
        }

        public void SwipeSourceDest()
        {
            var tempAddress = sourceAddress;
            var tempPort = sourcePort;
            sourceAddress = destAddress;
            sourcePort = destPort;
            destAddress = tempAddress;
            destPort = tempPort;
        }

        public void ReadFromBuffer(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new NullReferenceException("Buffer is null.");
            }

            if (buffer.Length < HeaderLength)
            {
                throw new Exception("Buffer length not enough to read header.");
            }

            try
            {
                ByteConverter.ReadIpAddress(buffer, 0, out sourceAddress);
                ByteConverter.ReadInt32(buffer, 4, out sourcePort);
                ByteConverter.ReadIpAddress(buffer, 8, out destAddress);
                ByteConverter.ReadInt32(buffer, 12, out destPort);
                requestType = (ConnectionRequest)buffer[16];
            }
            catch (Exception e)
            {
                throw new Exception($"PacketHeader ReadFromBuffer Exception: {e.Message}");
            }
        }

        public void WriteToBuffer(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new NullReferenceException("Buffer is null.");
            }

            if (buffer.Length < HeaderLength)
            {
                throw new Exception("Buffer length not enough to write header.");
            }

            try
            {
                ByteConverter.WriteIpAddress(buffer, 0, sourceAddress.Address);
                ByteConverter.WriteInt32(buffer, 4, sourcePort);
                ByteConverter.WriteIpAddress(buffer, 8, destAddress.Address);
                ByteConverter.WriteInt32(buffer, 12, destPort);
                buffer[16] = (byte)requestType;
            }
            catch (Exception e)
            {
                throw new Exception($"PacketHeader WriteToBuffer Exception: {e.Message}");
            }
        }
    }
}
