using Microsoft.AspNetCore.HttpOverrides;
using System;

namespace IPNetworkHelper;

public class UnableToSplitIPNetworkException : IPNetworkException
{
    public IPNetwork IPNetwork { get; private set; }

    public UnableToSplitIPNetworkException(IPNetwork network, Exception? innerExeption = null)
        : base("Unable to split network into smaller network", innerExeption)
        => IPNetwork = network ?? throw new ArgumentNullException(nameof(network));
}
