# ![logo](https://raw.githubusercontent.com/RobThree/IPNetworkHelper/master/logo_24x24.png) IPNetworkHelper

Provides helper (extension)methods for working with (IPv4 and/or IPv6) [IPNetworks](https://learn.microsoft.com/en-us/dotnet/api/system.net.ipnetwork). These include splitting and extracting networks from larger networks. Available as [NuGet package](https://www.nuget.org/packages/IPNetworkHelper/).

Note that since version 2.0 we use the [`System.Net.IPNetwork`](https://learn.microsoft.com/en-us/dotnet/api/system.net.ipnetwork) struct, which, unfortunately, is only available in .NET 8.0 and later. If you need support for earlier versions of .NET, use version 1.0 of this library.

Version 3.0 has a breaking change where `Contains()` now does the more intuitive thing and checks if the network is entirely contained within another network. If you want to check if two networks overlap, use the `Overlaps()` method.

## Quickstart

All of the below examples use IPv4 but IPv6 works just as well.

```c#
// Parse a network
var network = IPNetwork.Parse("192.168.0.0/16");

// Tries to parse network, returns true when succeeded, false otherwise and the parsed network
if (IPNetwork.TryParse("192.168.0.0/16", out var othernetwork))
{
    // ...
}

// Get last IP from network
var first = network.GetFirstIP();   // Network      (192.168.0.0)
var last = network.GetLastIP();     // Broadcast    (192.168.255.255)

// Splits a network into two halves
var (left, right) = network.Split();    // Returns 192.168.0.0/17 and 192.168.128.0/17

// Extract a subnet from a network
var desired = IPNetwork.Parse("192.168.10.16/28");
var result = network.Extract(desired);

// Result:
// 192.168.0.0/21
// 192.168.8.0/23
// 192.168.10.0/28
// 192.168.10.16/28 <- desired
// 192.168.10.32/27
// 192.168.10.64/26
// 192.168.10.128/25
// 192.168.11.0/24
// 192.168.12.0/22
// 192.168.16.0/20
// 192.168.32.0/19
// 192.168.64.0/18
// 192.168.128.0/17
```

The `Contains(IPNetwork)` method can be used to check if a network is contained (entirely) within another network and the `Overlaps(IPNetwork)` method can be used to check if two networks overlap.

This library also includes an `IPAddressComparer` and `IPNetworkComparer` to be used when sorting IPAddresses or networks:

```c#
var ips = new[] { "192.168.64.0", "192.168.10.32", "192.168.0.0", "192.168.10.16", "192.168.10.0" }
    .Select(IPAddress.Parse)
    .OrderBy(n => n, IPAddressComparer.Default);

var networks = new[] { "192.168.64.0/18", "192.168.10.32/27", "192.168.0.0/16", "192.168.10.16/28", "192.168.10.0/28" }
    .Select(IPNetwork.Parse)
    .OrderBy(n => n, IPNetworkComparer.Default);
```

---

Icon made by [prettycons](http://www.flaticon.com/authors/prettycons) from [www.flaticon.com](http://www.flaticon.com) is licensed by [CC 3.0](http://creativecommons.org/licenses/by/3.0/).
