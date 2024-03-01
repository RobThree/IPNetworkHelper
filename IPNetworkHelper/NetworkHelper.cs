using IPNetworkHelper.Comparers;
using IPNetworkHelper.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace IPNetworkHelper;

public static class NetworkHelper
{
    /// <summary>
    /// Checks if this network contains the given network entirely.
    /// </summary>
    /// <param name="thisNetwork">The network to check if it contains the other network entirely.</param>
    /// <param name="otherNetwork">The network to be checked if it is contained in this network.</param>
    /// <returns>True when this network contains the other network entirely.</returns>
    public static bool Contains(this IPNetwork thisNetwork, IPNetwork otherNetwork)
        => thisNetwork.Contains(otherNetwork.GetFirstIP())
        && thisNetwork.Contains(otherNetwork.GetLastIP());

    /// <summary>
    /// Checks if this network overlaps with the given network.
    /// </summary>
    /// <param name="thisNetwork">The network to check if it overlaps with the other network.</param>
    /// <param name="otherNetwork">The network to be checked if it overlaps with this network.</param>
    /// <returns>True when this network overlaps with the other network.</returns>
    public static bool Overlaps(this IPNetwork thisNetwork, IPNetwork otherNetwork)
        => thisNetwork.Contains(otherNetwork.GetFirstIP())
        || thisNetwork.Contains(otherNetwork.GetLastIP())
        || otherNetwork.Contains(thisNetwork.GetFirstIP())
        || otherNetwork.Contains(thisNetwork.GetLastIP());

    /// <summary>
    /// Gets the first IP address of the given network (which is the <see cref="IPNetwork.BaseAddress"/>.
    /// </summary>
    /// <param name="network">The network to get the first IP address from.</param>
    /// <returns>Returns the first IP address of the given network.</returns>

    public static IPAddress GetFirstIP(this IPNetwork network)
        => network.BaseAddress;

    /// <summary>
    /// Gets the last IP address of the given network.
    /// </summary>
    /// <param name="network">The network to get the last IP address from.</param>
    /// <returns>Returns the last IP address of the given network.</returns>
    public static IPAddress GetLastIP(this IPNetwork network)
    {
        var addressbytes = network.BaseAddress.GetAddressBytes();
        var result = new byte[addressbytes.Length];
        var mask = CreateMask(addressbytes, network.PrefixLength);
        for (var i = 0; i < addressbytes.Length; i++)
        {
            result[i] = (byte)(addressbytes[i] | ~mask[i]);
        }

        return new(result);
    }

    private static byte[] CreateMask(byte[] addressBytes, int prefixLength)
    {
        var mask = new byte[addressBytes.Length];
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

    /// <summary>
    /// Splits the given network into two halves.
    /// </summary>
    /// <param name="network">The network to split.</param>
    /// <returns>Returns the left and right half of the given network.</returns>
    /// <exception cref="UnableToSplitIPNetworkException">Thrown when the network is already at its maximum prefixlength.</exception>
    public static (IPNetwork left, IPNetwork right) Split(this IPNetwork network)
    {
        var addressbytes = network.BaseAddress.GetAddressBytes();
        var maxprefix = addressbytes.Length * 8;
        if (network.PrefixLength >= maxprefix)
        {
            throw new UnableToSplitIPNetworkException(network);
        }

        // Left part of split is simply first half of network
        var left = new IPNetwork(new(addressbytes), network.PrefixLength + 1);

        // Right part of split is second half of network
        // We need to set the "network MSB" for the second half
        var byteindex = network.PrefixLength / 8;
        var bitinbyteindex = 7 - (network.PrefixLength % 8);
        addressbytes[byteindex] |= (byte)(1 << bitinbyteindex);

        return (left, new IPNetwork(new(addressbytes), network.PrefixLength + 1));
    }

    /// <summary>
    /// Takes a random subnet with the given prefix from the network and returns all subnets after taking the desired subnet, including the desired subnet.
    /// </summary>
    /// <param name="network">The network to extract the subnet from.</param>
    /// <param name="prefixLength">The prefixlength of the subnect to extract from the network.</param>
    /// <returns>Returns all subnets after taking the desired subnet, including the desired subnet.</returns>
    /// <exception cref="NotSupportedException">Thrown for all non-IPv4/6 networks.</exception>
    public static IEnumerable<IPNetwork> Extract(this IPNetwork network, int prefixLength)
        => Extract(network, network.BaseAddress.AddressFamily switch
        {
            AddressFamily.InterNetwork => new IPNetwork(IPAddress.Any, prefixLength),
            AddressFamily.InterNetworkV6 => new IPNetwork(IPAddress.IPv6Any, prefixLength),
            _ => throw new NotSupportedException($"Network addressfamily '{network.BaseAddress.AddressFamily}' not supported")
        });

    /// <summary>
    /// Extracts the given subnet from the network and returns all subnets after taking the desired subnet, including the desired subnet.
    /// </summary>
    /// <param name="network">The network to extract the desired subnet from.</param>
    /// <param name="desiredNetwork">The subnet to extract from the network.</param>
    /// <returns>Returns all subnets after taking the desired subnet, including the desired subnet.</returns>
    public static IEnumerable<IPNetwork> Extract(this IPNetwork network, IPNetwork desiredNetwork)
        => Extract(network, [desiredNetwork]);

    /// <summary>
    /// Extracts the given subnets from the network and returns all subnets after taking the desired subnet, including the desired subnets.
    /// </summary>
    /// <param name="network">The network to extract the desired subnets from.</param>
    /// <param name="desiredNetworks">The subnets to extract from the network.</param>
    /// <returns>Returns all subnets after taking the desired subnet, including the desired subnets.</returns>
    /// <exception cref="IPNetworkNotInIPNetworkException">Thrown when any of the desired subnets is not in the network.</exception>
    public static IEnumerable<IPNetwork> Extract(this IPNetwork network, IEnumerable<IPNetwork> desiredNetworks)
    {
        // We start with a single network
        var networks = new List<IPNetwork>([network]);
        // For each network we want to extract
        foreach (var d in desiredNetworks)
        {
            // Find the target network in our networks that contains the network to be extracted
            var target = networks.Where(n => n.Contains(d.BaseAddress)).Cast<IPNetwork?>().FirstOrDefault() ?? throw new IPNetworkNotInIPNetworkException(network, d);

            // Remove the target network from the list
            networks.Remove(target);

            // Extract the network from the target and add the results to our networks list
            networks.AddRange(ExtractImpl(target, d));
        }
        return networks.OrderBy(i => i, IPNetworkComparer.Default);
    }

    private static IEnumerable<IPNetwork> ExtractImpl(IPNetwork network, IPNetwork desiredNetwork)
    {
        if (desiredNetwork.BaseAddress.AddressFamily != network.BaseAddress.AddressFamily)
        {
            throw new AddressFamilyMismatchException(network.BaseAddress.AddressFamily, desiredNetwork.BaseAddress.AddressFamily);
        }

        if (desiredNetwork.PrefixLength < network.PrefixLength)
        {
            throw new IPNetworkLargerThanIPNetworkException(network, desiredNetwork);
        }

        var pickatrandom = desiredNetwork.BaseAddress.Equals(IPAddress.Any) || desiredNetwork.BaseAddress.Equals(IPAddress.IPv6Any);
        if (!pickatrandom && !network.Contains(desiredNetwork.BaseAddress))
        {
            throw new IPNetworkNotInIPNetworkException(network, desiredNetwork);
        }

        while (network.PrefixLength < desiredNetwork.PrefixLength) // Repeat until we reach desired prefixlength
        {
            var (left, right) = network.Split();            // Split the given network into two halves
            var goleft = pickatrandom                       // If "pick at random"
                ? Random.Shared.Next(0, 2) == 0             // ... use a 50/50 chance to pick a half
                : left.Contains(desiredNetwork.BaseAddress);// ... else: is the desired address in the left half?
            yield return goleft ? right : left;             // Return half that DOESN'T contain desired network
            network = goleft ? left : right;                // This is the part containing our desired network
        }
        yield return network;                               // Lastly, return the extracted network
    }
}