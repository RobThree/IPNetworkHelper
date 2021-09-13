using Microsoft.AspNetCore.HttpOverrides;
using System;

namespace IPNetworkHelper
{
    public class IPNetworkLargerThanIPNetworkException : IPNetworkException
    {
        public IPNetwork IPNetwork { get; private set; }
        public IPNetwork Other { get; private set; }

        public IPNetworkLargerThanIPNetworkException(IPNetwork network, IPNetwork other)
            : base($"Network is larger than network")
        {
            IPNetwork = network ?? throw new ArgumentNullException(nameof(network));
            Other = other ?? throw new ArgumentNullException(nameof(other));
        }
    }
}
