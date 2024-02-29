using System;
using System.Net;

namespace IPNetworkHelper.Exceptions;

public class UnableToSplitIPNetworkException(IPNetwork network, Exception? innerExeption = null)
    : IPNetworkException("Unable to split network into smaller network", innerExeption)
{
    public IPNetwork IPNetwork { get; private set; } = network;
}
