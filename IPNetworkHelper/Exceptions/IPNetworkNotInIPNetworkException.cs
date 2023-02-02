using Microsoft.AspNetCore.HttpOverrides;
using System;

namespace IPNetworkHelper;

public class IPNetworkNotInIPNetworkException : IPNetworkException
{
    public IPNetwork IPNetwork { get; private set; }
    public IPNetwork Other { get; private set; }

    public IPNetworkNotInIPNetworkException(IPNetwork network, IPNetwork other)
        : base($"Network not in network")
    {
        IPNetwork = network ?? throw new ArgumentNullException(nameof(network));
        Other = other ?? throw new ArgumentNullException(nameof(other));
    }
}
