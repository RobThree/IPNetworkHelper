using Microsoft.AspNetCore.HttpOverrides;

namespace IPNetworkHelper
{
    /// <summary>
    /// Provides the base interface for implementation of the <see cref="IPNetworkComparer"/> class.
    /// </summary>
    public interface IIPNetworkComparer
    {
        /// <summary>
        /// Performs a comparison of two <see cref="IPNetwork"/>s and returns a value indicating whether one network
        /// is less than,  equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first <see cref="IPNetwork"/> to compare.</param>
        /// <param name="y">The second <see cref="IPNetwork"/> to compare.</param>
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
        int Compare(IPNetwork? x, IPNetwork? y);
    }
}
