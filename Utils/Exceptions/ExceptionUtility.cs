using System;
using System.Net;
using System.Net.Sockets;

namespace NetworkTransport.Utils
{
    public static class ExceptionUtility
    {
        public static void CheckAddressFamily(this IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                throw new IPv6NotSupportedException();
            }
        }

        public static void CheckPortValidity(int endPointPort)
        {
            if (endPointPort < IPEndPoint.MinPort || endPointPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException($"Not allowable port value: {endPointPort}");
            }
        }
    }
}
