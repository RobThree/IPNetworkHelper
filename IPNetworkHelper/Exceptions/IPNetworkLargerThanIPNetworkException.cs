using System.Net;

namespace IPNetworkHelper.Exceptions;

public class IPNetworkLargerThanIPNetworkException(IPNetwork network, IPNetwork other)
    : IPNetworkException($"Network is larger than network")
{
    public IPNetwork IPNetwork { get; private set; } = network;
    public IPNetwork Other { get; private set; } = other;
}
