using System.Net;

namespace IPNetworkHelper.Exceptions;

public class IPNetworkNotInIPNetworkException(IPNetwork network, IPNetwork other)
    : IPNetworkException($"Network not in network")
{
    public IPNetwork IPNetwork { get; private set; } = network;
    public IPNetwork Other { get; private set; } = other;
}
