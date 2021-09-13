using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace IPNetworkHelper
{
    public static class IPNetworkHelper
    {
        public static IPNetwork Parse(string value)
        {
            if (TryParse(value, out var result))
                return result;
            throw new FormatException($"{value} is not a valid IP network");
        }

        public static bool TryParse(string value, [NotNullWhen(true)] out IPNetwork? result)
        {
            result = null;

            var parts = (value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && IPAddress.TryParse(parts[0].Trim(), out var prefix) && int.TryParse(parts[1].Trim(), out var prefixlength))
            {
                var ipbytes = prefix.GetAddressBytes();
                if (!IsValidPrefixLength(ipbytes, prefixlength))
                    return false;

                var network = new IPAddress(CalculateFirstBytes(ipbytes, prefixlength));
                if (!network.Equals(prefix))
                    return false;

                result = new IPNetwork(network, prefixlength);
                return true;
            }
            return false;
        }

        public static IPAddress GetFirstIP(this IPNetwork network)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));

            return new(CalculateFirstBytes(network.Prefix.GetAddressBytes(), network.PrefixLength));
        }
            

        private static byte[] CalculateFirstBytes(byte[] prefixBytes, int prefixLength)
        {
            var result = new byte[prefixBytes.Length];
            var mask = CreateMask(prefixBytes, prefixLength);
            for (var i = 0; i < prefixBytes.Length; i++)
                result[i] = (byte)(prefixBytes[i] & mask[i]);
            return result;
        }

        public static IPAddress GetLastIP(this IPNetwork network)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));

            return new(CalculateLastBytes(network.Prefix.GetAddressBytes(), network.PrefixLength));
        }

        internal static byte[] CalculateLastBytes(byte[] prefixBytes, int prefixLength)
        {
            var result = new byte[prefixBytes.Length];
            var mask = CreateMask(prefixBytes, prefixLength);
            for (var i = 0; i < prefixBytes.Length; i++)
                result[i] = (byte)(prefixBytes[i] | ~mask[i]);
            return result;
        }

        private static bool IsValidPrefixLength(byte[] prefixBytes, int prefixLength)
            => prefixLength <= prefixBytes.Length * 8 && prefixLength >= 0;

        private static byte[] CreateMask(byte[] prefixBytes, int prefixLength)
        {
            if (!IsValidPrefixLength(prefixBytes, prefixLength))
                throw new ArgumentOutOfRangeException(nameof(prefixLength), "Invalid prefix length");

            var mask = new byte[prefixBytes.Length];
            var remainingbits = prefixLength;
            var i = 0;
            while (remainingbits >= 8)
            {
                mask[i] = 0xFF;
                i++;
                remainingbits -= 8;
            }
            if (remainingbits > 0)
            {
                mask[i] = (byte)(0xFF << (8 - remainingbits));
            }

            return mask;
        }

        public static bool HasValidPrefix(this IPNetwork network)
            => GetFirstIP(network).Equals(network.Prefix);

        public static (IPNetwork left, IPNetwork right) Split(this IPNetwork network)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));

            var prefixbytes = CalculateFirstBytes(network.Prefix.GetAddressBytes(), network.PrefixLength);
            var maxprefix = prefixbytes.Length * 8;
            if (network.PrefixLength >= maxprefix)
                throw new UnableToSplitIPNetworkException(network);

            // Left part of split is simply first half of network
            var left = new IPNetwork(new(prefixbytes), network.PrefixLength + 1);

            // Right part of split is second half of network
            // We need to set the "network MSB" for the second half
            var byteindex = network.PrefixLength / 8;
            var bitinbyteindex = 7 - (network.PrefixLength % 8);
            prefixbytes[byteindex] |= (byte)(1 << bitinbyteindex);

            return (left, new IPNetwork(new(prefixbytes), network.PrefixLength + 1));
        }

        public static IEnumerable<IPNetwork> Extract(this IPNetwork network, int prefixLength)
            => Extract(network, network.Prefix.AddressFamily switch
            {
                AddressFamily.InterNetwork => new IPNetwork(IPAddress.Any, prefixLength),
                AddressFamily.InterNetworkV6 => new IPNetwork(IPAddress.IPv6Any, prefixLength),
                _ => throw new NotSupportedException($"Network addressfamily '{network.Prefix.AddressFamily}' not supported")
            });

        public static IEnumerable<IPNetwork> Extract(this IPNetwork network, IPNetwork desired)
            => ExtractImpl(network, desired).OrderBy(i => i, IPNetworkComparer.Default);

        private static readonly Random _rng = new();
        private static IEnumerable<IPNetwork> ExtractImpl(IPNetwork network, IPNetwork desired)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));
            if (desired == null)
                throw new ArgumentNullException(nameof(desired));

            if (desired.Prefix.AddressFamily != network.Prefix.AddressFamily)
                throw new AddressFamilyMismatchException(network.Prefix.AddressFamily, desired.Prefix.AddressFamily);

            if (desired.PrefixLength <= network.PrefixLength)
                throw new IPNetworkLargerThanIPNetworkException(network, desired);

            var pickatrandom = desired.Prefix.Equals(IPAddress.Any) || desired.Prefix.Equals(IPAddress.IPv6Any);

            if (!pickatrandom && !network.Contains(desired.Prefix))
                throw new IPNetworkNotInIPNetworkException(network, desired);

            while (network.PrefixLength < desired.PrefixLength) // Repeat until we reach desired prefixlength
            {
                var (left, right) = network.Split();            // Split the given network into two halves
                var goleft = pickatrandom                       // If "pick at random"
                    ? _rng.Next(0, 2) == 0                      // ... use a 50/50 chance to pick a half
                    : left.Contains(desired.Prefix);            // ... else: is the desired prefix in the left half?
                yield return goleft ? right : left;             // Return half that DOESN'T contain desired network
                network = goleft ? left : right;                // This is the part containing our desired network
            }
            yield return network;                               // Lastly, return the extracted network
        }
    }
}
