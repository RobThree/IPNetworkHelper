using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;

namespace IPNetworkHelper.Tests;

[TestClass]
public class NetworkHelperTests
{
    [TestMethod]
    public void TryParseInvalidCIDRIPv4()
        => Assert.IsFalse(NetworkHelper.TryParse("127.0.0.1/1/2", out var network));

    [TestMethod]
    public void TryParseInvalidCIDRIPv6()
    => Assert.IsFalse(NetworkHelper.TryParse("::1/1/2", out var network));


    [TestMethod]
    public void TryParseCIDRWithoutPrefixLengthIPv4()
    {
        Assert.IsTrue(NetworkHelper.TryParse("127.0.0.1", out var network));
        Assert.AreEqual(32, network.PrefixLength);
        Assert.IsTrue(IPAddress.Parse("127.0.0.1").Equals(network.Prefix));
    }

    [TestMethod]
    public void TryParseCIDRWithoutPrefixLengthIPv6()
    {
        Assert.IsTrue(NetworkHelper.TryParse("::1", out var network));
        Assert.AreEqual(128, network.PrefixLength);
        Assert.IsTrue(IPAddress.Parse("::1").Equals(network.Prefix));
    }

    [TestMethod]
    public void TryParseCIDRIPv4()
    {
        Assert.IsTrue(NetworkHelper.TryParse("192.168.0.0/24", out var network));
        Assert.AreEqual(24, network.PrefixLength);
        Assert.IsTrue(IPAddress.Parse("192.168.0.0").Equals(network.Prefix));
    }

    [TestMethod]
    public void TryParseCIDRIPv6()
    {
        Assert.IsTrue(NetworkHelper.TryParse("DEAD:BEEF:0:1234::/64", out var network));
        Assert.AreEqual(64, network.PrefixLength);
        Assert.IsTrue(IPAddress.Parse("DEAD:BEEF:0:1234::").Equals(network.Prefix));
    }

    [TestMethod]
    public void TryParsePrefixMustBeNetworkIPv4()
    {
        Assert.IsFalse(NetworkHelper.TryParse("192.168.0.150/24", out var _));
        Assert.IsTrue(NetworkHelper.TryParse("192.168.0.0/24", out var _));
    }

    [TestMethod]
    public void TryParsePrefixMustBeNetworkIPv6()
    {
        Assert.IsFalse(NetworkHelper.TryParse("DEAD:BEEF:0:1234::FF00/64", out var _));
        Assert.IsTrue(NetworkHelper.TryParse("DEAD:BEEF:0:1234::/64", out var _));
    }

    [TestMethod]
    public void TryParsePrefixLengthMustBeValid()
    {
        Assert.IsFalse(NetworkHelper.TryParse("192.168.0.0/33", out var _));  // Should fail, max is 32
        Assert.IsFalse(NetworkHelper.TryParse("0.0.0.0/-1", out var _));  // Should fail, min is 0

        Assert.IsFalse(NetworkHelper.TryParse("DEAD:BEEF:0:1234::/129", out var _));  // Should fail, max is 128
        Assert.IsFalse(NetworkHelper.TryParse("0::0/-1", out var _));    // Should fail, min is 0

        // These should all pass since they're at the limits (min/max)
        Assert.IsTrue(NetworkHelper.TryParse("192.168.0.0/32", out var _));
        Assert.IsTrue(NetworkHelper.TryParse("0.0.0.0/0", out var _));

        Assert.IsTrue(NetworkHelper.TryParse("DEAD:BEEF:0:0:1234:5678:90AB:CDEF/128", out var _));
        Assert.IsTrue(NetworkHelper.TryParse("0::0/0", out var _));
    }

    [TestMethod]
    public void TryParseHandlesNullEmptyAndInvalidValues()
    {
        Assert.IsFalse(NetworkHelper.TryParse(null, out var _));
        Assert.IsFalse(NetworkHelper.TryParse(string.Empty, out var _));
        Assert.IsFalse(NetworkHelper.TryParse("invalid", out var _));
    }

    [TestMethod]
    public void SplitIPv4()
    {
        var network = NetworkHelper.Parse("192.168.0.0/24");
        var (left, right) = network.Split();

        Assert.IsTrue(IPAddress.Parse("192.168.0.0").Equals(left.Prefix));
        Assert.AreEqual(25, left.PrefixLength);
        Assert.IsTrue(IPAddress.Parse("192.168.0.128").Equals(right.Prefix));
        Assert.AreEqual(25, right.PrefixLength);
    }

    [TestMethod]
    public void SplitIPv6()
    {
        var network = NetworkHelper.Parse("DEAD:BEEF:0:1234::/64");
        var (left, right) = network.Split();

        Assert.IsTrue(IPAddress.Parse("DEAD:BEEF:0:1234::0").Equals(left.Prefix));
        Assert.AreEqual(65, left.PrefixLength);
        Assert.IsTrue(IPAddress.Parse("DEAD:BEEF:0:1234:8000::0").Equals(right.Prefix));
        Assert.AreEqual(65, right.PrefixLength);
    }

    [TestMethod]
    [ExpectedException(typeof(UnableToSplitIPNetworkException))]
    public void SplitThrowsOnUnsplittableNetworkIPv4()
    {
        var network = NetworkHelper.Parse("192.168.0.0/32");
        network.Split();
    }

    [TestMethod]
    [ExpectedException(typeof(UnableToSplitIPNetworkException))]
    public void SplitThrowsOnUnsplittableNetworkIPv6()
    {
        var network = NetworkHelper.Parse("DEAD:BEEF:0:0:1234:5678:90AB:CDEF/128");
        network.Split();
    }


    [TestMethod]
    [ExpectedException(typeof(AddressFamilyMismatchException))]
    public void ExtractThrowsOnAddressFamilyMismatch()
    {
        var ipv4 = NetworkHelper.Parse("192.168.0.0/24");
        var ipv6 = NetworkHelper.Parse("DEAD:BEEF:0:1234::/64");

        ipv4.Extract(ipv6).ToArray();
    }

    [TestMethod]
    [ExpectedException(typeof(IPNetworkLargerThanIPNetworkException))]
    public void ExtractThrowsOnLargerNetwork()
    {
        var network = NetworkHelper.Parse("192.168.0.0/24");
        var biggernetwork = NetworkHelper.Parse("192.168.0.0/23");

        network.Extract(biggernetwork).ToArray();
    }

    [TestMethod]
    [ExpectedException(typeof(IPNetworkNotInIPNetworkException))]
    public void ExtractThrowsOnDifferentNetwork()
    {
        var network = NetworkHelper.Parse("192.168.0.0/24");
        var different = NetworkHelper.Parse("172.16.0.0/30");

        network.Extract(different).ToArray();
    }

    [TestMethod]
    public void ExtractIPv4()
    {
        var network = NetworkHelper.Parse("192.168.0.0/16");
        var desired = NetworkHelper.Parse("192.168.16.16/28");

        var result = network.Extract(desired).ToArray();
        var expected = new[] { "192.168.0.0/20", "192.168.16.0/28", "192.168.16.16/28", "192.168.16.32/27", "192.168.16.64/26", "192.168.16.128/25", "192.168.17.0/24", "192.168.18.0/23", "192.168.20.0/22", "192.168.24.0/21", "192.168.32.0/19", "192.168.64.0/18", "192.168.128.0/17" }.Select(NetworkHelper.Parse).ToArray();

        Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
    }

    [TestMethod]
    public void ExtractSelfReturnsSelfIPv4()
    {
        var network = NetworkHelper.Parse("192.168.0.0/16");
        var desired = NetworkHelper.Parse("192.168.0.0/16");

        var result = network.Extract(desired).ToArray();
        var expected = new[] { NetworkHelper.Parse("192.168.0.0/16") };

        Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
    }

    [TestMethod]
    public void ExtractIPv6()
    {
        var network = NetworkHelper.Parse("1111:2222::/32");
        var desired = NetworkHelper.Parse("1111:2222:3333:8840::/64");

        var result = network.Extract(desired).ToArray();
        var expected = new[] { "1111:2222::/35", "1111:2222:2000::/36", "1111:2222:3000::/39", "1111:2222:3200::/40", "1111:2222:3300::/43", "1111:2222:3320::/44", "1111:2222:3330::/47", "1111:2222:3332::/48", "1111:2222:3333::/49", "1111:2222:3333:8000::/53", "1111:2222:3333:8800::/58", "1111:2222:3333:8840::/64", "1111:2222:3333:8841::/64", "1111:2222:3333:8842::/63", "1111:2222:3333:8844::/62", "1111:2222:3333:8848::/61", "1111:2222:3333:8850::/60", "1111:2222:3333:8860::/59", "1111:2222:3333:8880::/57", "1111:2222:3333:8900::/56", "1111:2222:3333:8a00::/55", "1111:2222:3333:8c00::/54", "1111:2222:3333:9000::/52", "1111:2222:3333:a000::/51", "1111:2222:3333:c000::/50", "1111:2222:3334::/46", "1111:2222:3338::/45", "1111:2222:3340::/42", "1111:2222:3380::/41", "1111:2222:3400::/38", "1111:2222:3800::/37", "1111:2222:4000::/34", "1111:2222:8000::/33" }.Select(NetworkHelper.Parse).ToArray();

        Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
    }

    [TestMethod]
    public void ExtractSelfReturnsSelfIPv6()
    {
        var network = NetworkHelper.Parse("1111:2222::/32");
        var desired = NetworkHelper.Parse("1111:2222::/32");

        var result = network.Extract(desired).ToArray();
        var expected = new[] { NetworkHelper.Parse("1111:2222::/32") };

        Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
    }

    [TestMethod]
    public void ExtractMultipleIPv4()
    {
        var network = NetworkHelper.Parse("192.168.0.0/16");
        var desired = new[] { "192.168.239.252/30", "192.168.228.0/24", "192.168.174.0/24" }.Select(NetworkHelper.Parse).ToArray();

        var result = network.Extract(desired).ToArray();
        var expected = new[] { "192.168.0.0/17", "192.168.128.0/19", "192.168.160.0/21", "192.168.168.0/22", "192.168.172.0/23", "192.168.174.0/24", "192.168.175.0/24", "192.168.176.0/20", "192.168.192.0/19", "192.168.224.0/22", "192.168.228.0/24", "192.168.229.0/24", "192.168.230.0/23", "192.168.232.0/22", "192.168.236.0/23", "192.168.238.0/24", "192.168.239.0/25", "192.168.239.128/26", "192.168.239.192/27", "192.168.239.224/28", "192.168.239.240/29", "192.168.239.248/30", "192.168.239.252/30", "192.168.240.0/20" }.Select(NetworkHelper.Parse).ToArray();

        Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
    }

    [TestMethod]
    public void ExtractMultipleSkipsCreated()
    {
        // See https://github.com/RobThree/IPNetworkHelper/issues/1#issuecomment-1034793979
        var network = NetworkHelper.Parse("37.0.0.0/8");
        var desired = new[] { "37.10.128.0/17", "37.12.128.0/18", "37.13.64.0/18", "37.13.128.0/17" }.Select(NetworkHelper.Parse).ToArray();

        var result = network.Extract(desired).ToArray();
        var expected = new[] { "37.0.0.0/13", "37.8.0.0/15", "37.10.0.0/17", "37.10.128.0/17", "37.11.0.0/16", "37.12.0.0/17", "37.12.128.0/18", "37.12.192.0/18", "37.13.0.0/18", "37.13.64.0/18", "37.13.128.0/17", "37.14.0.0/15", "37.16.0.0/12", "37.32.0.0/11", "37.64.0.0/10", "37.128.0.0/9" }.Select(NetworkHelper.Parse).ToArray();

        Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
    }

    [TestMethod]
    public void ExtractMultipleIPv6()
    {
        var network = NetworkHelper.Parse("1111:2222::/32");
        var desired = new[] { "1111:2222:3333:8840::/64", "1111:2222:6571:c8a3:9400::/70", "1111:2222:39a2:6427:954e::/80" }.Select(NetworkHelper.Parse).ToArray();

        var result = network.Extract(desired).ToArray();
        var expected = new[] { "1111:2222::/35", "1111:2222:2000::/36", "1111:2222:3000::/39", "1111:2222:3200::/40", "1111:2222:3300::/43", "1111:2222:3320::/44", "1111:2222:3330::/47", "1111:2222:3332::/48", "1111:2222:3333::/49", "1111:2222:3333:8000::/53", "1111:2222:3333:8800::/58", "1111:2222:3333:8840::/64", "1111:2222:3333:8841::/64", "1111:2222:3333:8842::/63", "1111:2222:3333:8844::/62", "1111:2222:3333:8848::/61", "1111:2222:3333:8850::/60", "1111:2222:3333:8860::/59", "1111:2222:3333:8880::/57", "1111:2222:3333:8900::/56", "1111:2222:3333:8a00::/55", "1111:2222:3333:8c00::/54", "1111:2222:3333:9000::/52", "1111:2222:3333:a000::/51", "1111:2222:3333:c000::/50", "1111:2222:3334::/46", "1111:2222:3338::/45", "1111:2222:3340::/42", "1111:2222:3380::/41", "1111:2222:3400::/38", "1111:2222:3800::/40", "1111:2222:3900::/41", "1111:2222:3980::/43", "1111:2222:39a0::/47", "1111:2222:39a2::/50", "1111:2222:39a2:4000::/51", "1111:2222:39a2:6000::/54", "1111:2222:39a2:6400::/59", "1111:2222:39a2:6420::/62", "1111:2222:39a2:6424::/63", "1111:2222:39a2:6426::/64", "1111:2222:39a2:6427::/65", "1111:2222:39a2:6427:8000::/68", "1111:2222:39a2:6427:9000::/70", "1111:2222:39a2:6427:9400::/72", "1111:2222:39a2:6427:9500::/74", "1111:2222:39a2:6427:9540::/77", "1111:2222:39a2:6427:9548::/78", "1111:2222:39a2:6427:954c::/79", "1111:2222:39a2:6427:954e::/80", "1111:2222:39a2:6427:954f::/80", "1111:2222:39a2:6427:9550::/76", "1111:2222:39a2:6427:9560::/75", "1111:2222:39a2:6427:9580::/73", "1111:2222:39a2:6427:9600::/71", "1111:2222:39a2:6427:9800::/69", "1111:2222:39a2:6427:a000::/67", "1111:2222:39a2:6427:c000::/66", "1111:2222:39a2:6428::/61", "1111:2222:39a2:6430::/60", "1111:2222:39a2:6440::/58", "1111:2222:39a2:6480::/57", "1111:2222:39a2:6500::/56", "1111:2222:39a2:6600::/55", "1111:2222:39a2:6800::/53", "1111:2222:39a2:7000::/52", "1111:2222:39a2:8000::/49", "1111:2222:39a3::/48", "1111:2222:39a4::/46", "1111:2222:39a8::/45", "1111:2222:39b0::/44", "1111:2222:39c0::/42", "1111:2222:3a00::/39", "1111:2222:3c00::/38", "1111:2222:4000::/35", "1111:2222:6000::/38", "1111:2222:6400::/40", "1111:2222:6500::/42", "1111:2222:6540::/43", "1111:2222:6560::/44", "1111:2222:6570::/48", "1111:2222:6571::/49", "1111:2222:6571:8000::/50", "1111:2222:6571:c000::/53", "1111:2222:6571:c800::/57", "1111:2222:6571:c880::/59", "1111:2222:6571:c8a0::/63", "1111:2222:6571:c8a2::/64", "1111:2222:6571:c8a3::/65", "1111:2222:6571:c8a3:8000::/68", "1111:2222:6571:c8a3:9000::/70", "1111:2222:6571:c8a3:9400::/70", "1111:2222:6571:c8a3:9800::/69", "1111:2222:6571:c8a3:a000::/67", "1111:2222:6571:c8a3:c000::/66", "1111:2222:6571:c8a4::/62", "1111:2222:6571:c8a8::/61", "1111:2222:6571:c8b0::/60", "1111:2222:6571:c8c0::/58", "1111:2222:6571:c900::/56", "1111:2222:6571:ca00::/55", "1111:2222:6571:cc00::/54", "1111:2222:6571:d000::/52", "1111:2222:6571:e000::/51", "1111:2222:6572::/47", "1111:2222:6574::/46", "1111:2222:6578::/45", "1111:2222:6580::/41", "1111:2222:6600::/39", "1111:2222:6800::/37", "1111:2222:7000::/36", "1111:2222:8000::/33" }.Select(NetworkHelper.Parse).ToArray();

        Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
    }

    [TestMethod]
    public void ExtractMultipleThrowsOnNetworkOutsideOfStartingNetwork()
    {
        var network = NetworkHelper.Parse("192.168.0.0/16");
        var desired = new[] { "192.168.239.252/30", "192.168.228.0/24", "10.10.5.0/24" }.Select(NetworkHelper.Parse).ToArray();

        try
        {
            network.Extract(desired);
            Assert.Fail();
        }
        catch (IPNetworkNotInIPNetworkException ex)
        {
            Assert.AreEqual(network, ex.IPNetwork);
            Assert.AreEqual(desired[2], ex.Other);
        }
        catch (Exception)
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    public void HasValidPrefix()
    {
        var ip = IPAddress.Parse("192.168.0.16");
        var n1 = new IPNetwork(ip, 28);
        var n2 = new IPNetwork(ip, 27);

        Assert.IsTrue(NetworkHelper.HasValidPrefix(n1));
        Assert.IsFalse(NetworkHelper.HasValidPrefix(n2));
    }

    [TestMethod]
    public void Network_Contains_OtherNetwork()
    {

        /*  0    16   32   48   64   80            128                                     255 
         *  |----|----|----|----|----|----|----|----|----|----|----|----|----|----|----|----|
         *  |\_A_/    \__B_/\_C_/\_D_/
         *  |                   |
         *  \________E__________/
         */

        var network_a = NetworkHelper.Parse("192.168.0.0/28");   // 192.168.0.0 - ..15
        var network_b = NetworkHelper.Parse("192.168.0.32/28");  // 192.168.0.32 - ..47
        var network_c = NetworkHelper.Parse("192.168.0.48/28");  // 192.168.0.48 - ..63
        var network_d = NetworkHelper.Parse("192.168.0.64/28");  // 192.168.0.64 - ..79
        var network_e = NetworkHelper.Parse("192.168.0.0/26");   // 192.168.0.0  - ..63

        Assert.IsTrue(network_a.Contains(network_e));
        Assert.IsTrue(network_e.Contains(network_a));

        Assert.IsTrue(network_b.Contains(network_e));
        Assert.IsTrue(network_e.Contains(network_b));

        Assert.IsTrue(network_c.Contains(network_e));
        Assert.IsTrue(network_e.Contains(network_c));

        Assert.IsFalse(network_d.Contains(network_e));
        Assert.IsFalse(network_e.Contains(network_d));
    }
}
