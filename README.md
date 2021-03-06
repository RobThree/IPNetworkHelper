# <img src="https://raw.githubusercontent.com/RobThree/IPNetworkHelper/master/logo.png" width="24" height="24"> IPNetworkHelper

Provides helper (extension)methods for working with (IPv4 and/or IPv6) [IPNetworks](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.httpoverrides.ipnetwork). These include parsing, splitting and extracting networks from larger networks. Available as [NuGet package](https://www.nuget.org/packages/IPNetworkHelper/).

## Quickstart

All of the below examples use IPv4 but IPv6 works just as well.

```c#
// Parse a network, throws on invalid networks
var network = NetworkHelper.Parse("192.168.0.0/16");

// Tries to parse network, returns true when succeeded, false otherwise and the parsed network
if (NetworkHelper.TryParse("192.168.0.0/16", out var othernetwork))
{
    // ...
}

// Check prefix; returns true for 192.168.0.0/16, returns false for 192.168.0.3/16
var validprefix = network.HasValidPrefix();   

// Get first/last IP from network
var first = network.GetFirstIP();   // Network      (192.168.0.0)
var last = network.GetLastIP();     // Broadcast    (192.168.255.255)

// Splits a network into two halves
var (left, right) = network.Split();    // Returns 192.168.0.0/17 and 192.168.128.0/17

// Remove a subnet from a network
var desired = NetworkHelper.Parse("192.168.10.16/28");
var result = network.Extract(desired);

// Result:
// 192.168.0.0/21
// 192.168.8.0/23
// 192.168.10.0/28
// 192.168.10.16/28
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

Note: NetworkHelper's `Parse()` and `TryParse()` methods will only accept networks with a prefix at network boundaries, unlike [IPNetwork's constructor](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.httpoverrides.ipnetwork.-ctor). Use the `HasValidPrefix()` extension method to check wether a prefix is valid for any given `IPNetwork`.

This library also includes an `IPAddressComparer` and `IPNetworkComparer` to be used when sorting IPAddresses or networks:

```c#
var ips = new[] { "192.168.64.0", "192.168.10.32", "192.168.0.0", "192.168.10.16", "192.168.10.0" }
    .Select(IPAddress.Parse)
    .OrderBy(n => n, IPAddressComparer.Default);

var networks = new[] { "192.168.64.0/18", "192.168.10.32/27", "192.168.0.0/16", "192.168.10.16/28", "192.168.10.0/28" }
    .Select(NetworkHelper.Parse)
    .OrderBy(n => n, IPNetworkComparer.Default);
```

<hr>

Icon made by [prettycons](http://www.flaticon.com/authors/prettycons) from [www.flaticon.com](http://www.flaticon.com) is licensed by [CC 3.0](http://creativecommons.org/licenses/by/3.0/).
