using System.Net;

namespace IPNetworkHelper;

/// <summary>
/// Provides the base interface for implementation of the <see cref="IPAddressComparer"/> class.
/// </summary>
public interface IIPAddressComparer
{
    /// <summary>
    /// Performs a comparison of two <see cref="IPAddress"/>es and returns a value indicating whether one address
    /// is less than,  equal to, or greater than the other.
    /// </summary>
    /// <param name="x">The first <see cref="IPAddress"/> to compare.</param>
    /// <param name="y">The second <see cref="IPAddress"/> to compare.</param>
    /// <returns>
    /// Value
    /// Condition
    /// Less than zero
    /// <paramref name="x"/> is less than <paramref name="y"/>.
    /// Zero
    /// <paramref name="x"/> equals <paramref name="y"/>.
    /// Greater than zero
    /// <paramref name="x"/> is greater than <paramref name="y"/>.
    /// </returns>
    int Compare(IPAddress? x, IPAddress? y);
}
