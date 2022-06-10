using System;
using System.Net;
using System.Text;

namespace NetworkTransport.Utils
{
    public static class ByteConverter
    {
        public static int WriteInt32(byte[] buffer, int index, int integer32)
        {
            CheckBufferValidity(buffer, index, index + 3);

            buffer[index++] = (byte)integer32;
            buffer[index++] = (byte)(integer32 >> 8);
            buffer[index++] = (byte)(integer32 >> 16);
            buffer[index++] = (byte)(integer32 >> 24);
            return index;
        }

        public static int ReadInt32(byte[] buffer, int index, out int integer32)
        {
            CheckBufferValidity(buffer, index, index + 3);

            integer32 = buffer[index++];
            integer32 = integer32 | (buffer[index++] << 8);
            integer32 = integer32 | (buffer[index++] << 16);
            integer32 = integer32 | (buffer[index++] << 24);
            return index;
        }

        public static int ReadIpAddress(byte[] buffer, int index, out IPAddress address)
        {
            CheckBufferValidity(buffer, index, index + 3);

            int val0 = buffer[index++];
            int val1 = buffer[index++];
            int val2 = buffer[index++];
            int val3 = buffer[index++];
            string ipAddressBuild
                = new StringBuilder()
                    .Append(val0).Append(".")
                    .Append(val1).Append(".")
                    .Append(val2).Append(".")
                    .Append(val3).ToString();
            address = IPAddress.Parse(ipAddressBuild);
            return index;
        }

        public static int WriteIpAddress(byte[] buffer, int index, long address)
        {
            CheckBufferValidity(buffer, index, index + 3);

            buffer[index++] = (byte)(address % 256);
            buffer[index++] = (byte)(address / 256 % 256);
            buffer[index++] = (byte)(address / 256 / 256 % 256);
            buffer[index++] = (byte)(address / 256 / 256 / 256);
            return index;
        }

        private static void CheckBufferValidity(byte[] buffer, int startIndex, int endIndex)
        {
            if (buffer == null || startIndex < 0 || endIndex > buffer.Length - 1)
            {
                throw new Exception($"Buffer {buffer} with start index {startIndex} and end index {endIndex} are not valid.");
            }
        }
    }
}
