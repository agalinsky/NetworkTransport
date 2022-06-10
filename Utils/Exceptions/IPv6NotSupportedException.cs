using System;

namespace NetworkTransport
{
    public class IPv6NotSupportedException : Exception
    {
        public override string Message => "AddressFamily: IPv6 protocol is not supported!";
    }
}
