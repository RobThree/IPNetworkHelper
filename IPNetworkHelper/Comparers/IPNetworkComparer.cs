using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Collections.Generic;

namespace IPNetworkHelper
{
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

        /// <summary>
        /// Performs a comparison of two <see cref="IPNetwork"/>s and returns a value indicating whether one network
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
        public override int Compare(IPNetwork? x, IPNetwork? y)
        {
            if (ReferenceEquals(x, y))
                return 0;   // same instance

            if (x is null)
                return -1;  // nulls are always less than non-null

            if (y is null)
                return 1;   // non-null is always greater than null

            // First compare by prefix
            var result = IPAddressComparer.Default.Compare(x.Prefix, y.Prefix);
            if (result == 0)    // Equal? Then compare size, return biggest first
                return x.PrefixLength.CompareTo(y.PrefixLength);
            return result;
        }
    }
}
