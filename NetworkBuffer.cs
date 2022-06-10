using System;

namespace NetworkTransport
{
    /// <summary>
    /// Should be used outside of network transport to read/write useful data.
    /// </summary>
    public class NetworkBuffer : Pools.IPoolableBuffer
    {
        public byte[] buffer;
        public int offset;
        public int payload;

        public void InitBuffer(int length)
        {
            buffer = new byte[length];
            offset = PacketHeader.HeaderLength;
            payload = 0;
        }

        public void Recycle()
        {
            Array.Clear(buffer, 0, buffer.Length);
            offset = PacketHeader.HeaderLength;
            payload = 0;
        }

        public int GetBufferLength()
        {
            return buffer.Length;
        }
    }
}
