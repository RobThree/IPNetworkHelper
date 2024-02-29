using System;
using System.Collections.Generic;
using System.Net;

namespace IPNetworkHelper.Comparers;

/// <summary>
/// Compares IP addresses to determine numerically which is greater than the other.
/// </summary>
public class IPNetworkComparer : Comparer<IPNetwork>, IIPNetworkComparer
{

    private static readonly Lazy<IPNetworkComparer> _default = new();

    /// <summary>
    /// The default singleton instance of the comparer.
    /// </summary>
    public static new IPNetworkComparer Default => _default.Value;

    /// <inheritdoc/>
    public override int Compare(IPNetwork x, IPNetwork y)
    {
        var result = IPAddressComparer.Default.Compare(x.BaseAddress, y.BaseAddress);
        return result == 0
            ? x.PrefixLength.CompareTo(y.PrefixLength)
            : result;
    }
}
