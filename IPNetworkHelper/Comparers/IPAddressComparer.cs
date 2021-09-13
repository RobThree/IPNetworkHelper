using System;
using System.Collections.Generic;
using System.Net;

namespace IPNetworkHelper
{
    /// <summary>
    /// Compares IP addresses to determine numerically which is greater than the other.
    /// </summary>
    public class IPAddressComparer : Comparer<IPAddress>, IIPAddressComparer
    {

        private static readonly Lazy<IPAddressComparer> _default = new();

        /// <summary>
        /// The default singleton instance of the comparer.
        /// </summary>
        public static new IPAddressComparer Default => _default.Value;

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
        public override int Compare(IPAddress? x, IPAddress? y)
        {

            if (ReferenceEquals(x, y))
                return 0;   // Same instance

            if (x is null)
                return -1;  // Nulls are always less than non-null

            if (y is null)
                return 1;   // Non-null is always greater than null

            if (x.AddressFamily != y.AddressFamily)
                throw new AddressFamilyMismatchException(x.AddressFamily, y.AddressFamily);

            var xBytes = x.GetAddressBytes();
            var yBytes = y.GetAddressBytes();

            if (xBytes.Length != yBytes.Length)
                throw new ArgumentException("IP addresses must be of the same length.");

            // Compare byte by byte
            for (var i = 0; i < xBytes.Length; i++)
            {
                if (xBytes[i] != yBytes[i])
                    return xBytes[i] < yBytes[i] ? -1 : 1;
            }
            return 0;   // Equal
        }
    }
}
