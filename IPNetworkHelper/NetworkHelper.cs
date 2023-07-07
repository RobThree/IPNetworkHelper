using Microsoft.AspNetCore.HttpOverrides;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace IPNetworkHelper;

public static class NetworkHelper
{
    public static IPNetwork Parse(string value)
        => TryParse(value, out var result)
        ? result : throw new FormatException($"{value} is not a valid IP network");

    public static bool TryParse(string value, [NotNullWhen(true)] out IPNetwork? result)
    {
        result = null;

        var parts = (value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 1 && IPAddress.TryParse(parts[0], out var prefix))
        {
            int prefixlength = parts.Length switch
            {
                1 => GetDefaultPrefixLength(prefix),
                2 => int.TryParse(parts[1], out prefixlength) ? prefixlength : -1,
                _ => -1
            };

            var ipbytes = prefix.GetAddressBytes();
            if (!IsValidPrefixLength(ipbytes, prefixlength))
            {
                return false;
            }

            var network = new IPAddress(CalculateFirstBytes(ipbytes, prefixlength));
            if (!network.Equals(prefix))
            {
                return false;
            }

            result = new IPNetwork(network, prefixlength);
            return true;
        }

        return false;
    }

    private static int GetDefaultPrefixLength(IPAddress ip)
        => ip.AddressFamily switch
        {
            AddressFamily.InterNetwork => 32,
            AddressFamily.InterNetworkV6 => 128,
            _ => throw new NotSupportedException($"Network addressfamily '{ip.AddressFamily}' not supported")
        };

    public static bool Contains(this IPNetwork thisNetwork, IPNetwork otherNetwork)
        => thisNetwork.Contains(otherNetwork.Prefix)
        || otherNetwork.Contains(thisNetwork.Prefix);

    public static IPAddress GetFirstIP(this IPNetwork network) => network == null
            ? throw new ArgumentNullException(nameof(network))
            : new(CalculateFirstBytes(network.Prefix.GetAddressBytes(), network.PrefixLength));

    private static byte[] CalculateFirstBytes(byte[] prefixBytes, int prefixLength)
    {
        var result = new byte[prefixBytes.Length];
        var mask = CreateMask(prefixBytes, prefixLength);
        for (var i = 0; i < prefixBytes.Length; i++)
        {
            result[i] = (byte)(prefixBytes[i] & mask[i]);
        }

        return result;
    }

    public static IPAddress GetLastIP(this IPNetwork network)
        => network == null
        ? throw new ArgumentNullException(nameof(network))
        : new(CalculateLastBytes(network.Prefix.GetAddressBytes(), network.PrefixLength));

    internal static byte[] CalculateLastBytes(byte[] prefixBytes, int prefixLength)
    {
        var result = new byte[prefixBytes.Length];
        var mask = CreateMask(prefixBytes, prefixLength);
        for (var i = 0; i < prefixBytes.Length; i++)
        {
            result[i] = (byte)(prefixBytes[i] | ~mask[i]);
        }

        return result;
    }

    private static bool IsValidPrefixLength(byte[] prefixBytes, int prefixLength)
        => prefixLength >= 0 && prefixLength <= prefixBytes.Length * 8;

    private static byte[] CreateMask(byte[] prefixBytes, int prefixLength)
    {
        if (!IsValidPrefixLength(prefixBytes, prefixLength))
        {
            throw new ArgumentOutOfRangeException(nameof(prefixLength), "Invalid prefix length");
        }

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
        {
            throw new ArgumentNullException(nameof(network));
        }

        var prefixbytes = CalculateFirstBytes(network.Prefix.GetAddressBytes(), network.PrefixLength);
        var maxprefix = prefixbytes.Length * 8;
        if (network.PrefixLength >= maxprefix)
        {
            throw new UnableToSplitIPNetworkException(network);
        }

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

    public static IEnumerable<IPNetwork> Extract(this IPNetwork network, IPNetwork desiredNetwork)
        => ExtractImpl(network, desiredNetwork).OrderBy(i => i, IPNetworkComparer.Default);

    public static IEnumerable<IPNetwork> Extract(this IPNetwork network, IEnumerable<IPNetwork> desiredNetworks)
    {
        // We start with a single network
        var networks = new List<IPNetwork>(new[] { network });
        // For each network we want to extract
        foreach (var d in desiredNetworks)
        {
            // Find the target network in our networks that contains the network to be extracted
            var target = networks.Where(n => n.Contains(d.Prefix)).FirstOrDefault() ?? throw new IPNetworkNotInIPNetworkException(network, d);

            // Remove the target network from the list
            networks.Remove(target);

            // Extract the network from the target and add the results to our networks list
            networks.AddRange(target.Extract(d));
        }
        return networks.OrderBy(i => i, IPNetworkComparer.Default);
    }

    private static readonly Random _rng = new();
    private static IEnumerable<IPNetwork> ExtractImpl(IPNetwork network, IPNetwork desiredNetwork)
    {
        if (network == null)
        {
            throw new ArgumentNullException(nameof(network));
        }

        if (desiredNetwork == null)
        {
            throw new ArgumentNullException(nameof(desiredNetwork));
        }

        if (desiredNetwork.Prefix.AddressFamily != network.Prefix.AddressFamily)
        {
            throw new AddressFamilyMismatchException(network.Prefix.AddressFamily, desiredNetwork.Prefix.AddressFamily);
        }

        if (desiredNetwork.PrefixLength < network.PrefixLength)
        {
            throw new IPNetworkLargerThanIPNetworkException(network, desiredNetwork);
        }

        var pickatrandom = desiredNetwork.Prefix.Equals(IPAddress.Any) || desiredNetwork.Prefix.Equals(IPAddress.IPv6Any);
        if (!pickatrandom && !network.Contains(desiredNetwork.Prefix))
        {
            throw new IPNetworkNotInIPNetworkException(network, desiredNetwork);
        }

        while (network.PrefixLength < desiredNetwork.PrefixLength) // Repeat until we reach desired prefixlength
        {
            var (left, right) = network.Split();            // Split the given network into two halves
            var goleft = pickatrandom                       // If "pick at random"
                ? _rng.Next(0, 2) == 0                      // ... use a 50/50 chance to pick a half
                : left.Contains(desiredNetwork.Prefix);            // ... else: is the desired prefix in the left half?
            yield return goleft ? right : left;             // Return half that DOESN'T contain desired network
            network = goleft ? left : right;                // This is the part containing our desired network
        }
        yield return network;                               // Lastly, return the extracted network
    }
}