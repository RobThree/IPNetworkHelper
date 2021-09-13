using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;

namespace IPNetworkHelper.Tests
{
    [TestClass]
    public class IPNetworkHelperTests
    {
        [TestMethod]
        public void TryParseCIDRIPv4()
        {
            Assert.IsTrue(IPNetworkHelper.TryParse("192.168.0.0/24", out var network));
            Assert.AreEqual(24, network.PrefixLength);
            Assert.IsTrue(IPAddress.Parse("192.168.0.0").Equals(network.Prefix));
        }

        [TestMethod]
        public void TryParseCIDRIPv6()
        {
            Assert.IsTrue(IPNetworkHelper.TryParse("DEAD:BEEF:0:1234::/64", out var network));
            Assert.AreEqual(64, network.PrefixLength);
            Assert.IsTrue(IPAddress.Parse("DEAD:BEEF:0:1234::").Equals(network.Prefix));
        }

        [TestMethod]
        public void TryParsePrefixMustBeNetworkIPv4()
        {
            Assert.IsFalse(IPNetworkHelper.TryParse("192.168.0.150/24", out var _));
            Assert.IsTrue(IPNetworkHelper.TryParse("192.168.0.0/24", out var _));
        }

        [TestMethod]
        public void TryParsePrefixMustBeNetworkIPv6()
        {
            Assert.IsFalse(IPNetworkHelper.TryParse("DEAD:BEEF:0:1234::FF00/64", out var _));
            Assert.IsTrue(IPNetworkHelper.TryParse("DEAD:BEEF:0:1234::/64", out var _));
        }

        [TestMethod]
        public void TryParsePrefixLengthMustBeValid()
        {
            Assert.IsFalse(IPNetworkHelper.TryParse("192.168.0.0/33", out var _));  // Should fail, max is 32
            Assert.IsFalse(IPNetworkHelper.TryParse("0.0.0.0/0", out var _));  // Should fail, min is 1

            Assert.IsFalse(IPNetworkHelper.TryParse("DEAD:BEEF:0:1234::/129", out var _));  // Should fail, max is 128
            Assert.IsFalse(IPNetworkHelper.TryParse("0::0/0", out var _));    // Should fail, min is 1

            // These should all pass since they're at the limits (min/max)
            Assert.IsTrue(IPNetworkHelper.TryParse("192.168.0.0/32", out var _));
            Assert.IsTrue(IPNetworkHelper.TryParse("0.0.0.0/1", out var _));

            Assert.IsTrue(IPNetworkHelper.TryParse("DEAD:BEEF:0:0:1234:5678:90AB:CDEF/128", out var _));
            Assert.IsTrue(IPNetworkHelper.TryParse("0::0/1", out var _));
        }

        [TestMethod]
        public void TryParseHandlesNullEmptyAndInvalidValues()
        {
            Assert.IsFalse(IPNetworkHelper.TryParse(null, out var _));
            Assert.IsFalse(IPNetworkHelper.TryParse(string.Empty, out var _));
            Assert.IsFalse(IPNetworkHelper.TryParse("invalid", out var _));
        }

        [TestMethod]
        public void SplitIPv4()
        {
            var network = IPNetworkHelper.Parse("192.168.0.0/24");
            var (left, right) = network.Split();

            Assert.IsTrue(IPAddress.Parse("192.168.0.0").Equals(left.Prefix));
            Assert.AreEqual(25, left.PrefixLength);
            Assert.IsTrue(IPAddress.Parse("192.168.0.128").Equals(right.Prefix));
            Assert.AreEqual(25, right.PrefixLength);
        }

        [TestMethod]
        public void SplitIPv6()
        {
            var network = IPNetworkHelper.Parse("DEAD:BEEF:0:1234::/64");
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
            var network = IPNetworkHelper.Parse("192.168.0.0/32");
            network.Split();
        }

        [TestMethod]
        [ExpectedException(typeof(UnableToSplitIPNetworkException))]
        public void SplitThrowsOnUnsplittableNetworkIPv6()
        {
            var network = IPNetworkHelper.Parse("DEAD:BEEF:0:0:1234:5678:90AB:CDEF/128");
            network.Split();
        }


        [TestMethod]
        [ExpectedException(typeof(AddressFamilyMismatchException))]
        public void ExtractThrowsOnAddressFamilyMismatch()
        {
            var ipv4 = IPNetworkHelper.Parse("192.168.0.0/24");
            var ipv6 = IPNetworkHelper.Parse("DEAD:BEEF:0:1234::/64");

            ipv4.Extract(ipv6).ToArray();
        }

        [TestMethod]
        [ExpectedException(typeof(IPNetworkLargerThanIPNetworkException))]
        public void ExtractThrowsOnLargerNetwork()
        {
            var network = IPNetworkHelper.Parse("192.168.0.0/24");
            var biggernetwork = IPNetworkHelper.Parse("192.168.0.0/23");

            network.Extract(biggernetwork).ToArray();
        }

        [TestMethod]
        [ExpectedException(typeof(IPNetworkNotInIPNetworkException))]
        public void ExtractThrowsOnDifferentNetwork()
        {
            var network = IPNetworkHelper.Parse("192.168.0.0/24");
            var different = IPNetworkHelper.Parse("172.16.0.0/30");

            network.Extract(different).ToArray();
        }

        [TestMethod]
        public void ExtractIPv4()
        {
            var network = IPNetworkHelper.Parse("192.168.0.0/16");
            var desired = IPNetworkHelper.Parse("192.168.16.16/28");

            var result = network.Extract(desired).ToArray();
            var expected = new[] { "192.168.0.0/20", "192.168.16.0/28", "192.168.16.16/28", "192.168.16.32/27", "192.168.16.64/26", "192.168.16.128/25", "192.168.17.0/24", "192.168.18.0/23", "192.168.20.0/22", "192.168.24.0/21", "192.168.32.0/19", "192.168.64.0/18", "192.168.128.0/17" }.Select(v => IPNetworkHelper.Parse(v)).ToArray();

            Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
        }

        [TestMethod]
        public void ExtractIPv6()
        {
            var network = IPNetworkHelper.Parse("1111:2222::/32");
            var desired = IPNetworkHelper.Parse("1111:2222:3333:8840::/64");

            var result = network.Extract(desired).ToArray();
            var expected = new[] { "1111:2222::/35", "1111:2222:2000::/36", "1111:2222:3000::/39", "1111:2222:3200::/40", "1111:2222:3300::/43", "1111:2222:3320::/44", "1111:2222:3330::/47", "1111:2222:3332::/48", "1111:2222:3333::/49", "1111:2222:3333:8000::/53", "1111:2222:3333:8800::/58", "1111:2222:3333:8840::/64", "1111:2222:3333:8841::/64", "1111:2222:3333:8842::/63", "1111:2222:3333:8844::/62", "1111:2222:3333:8848::/61", "1111:2222:3333:8850::/60", "1111:2222:3333:8860::/59", "1111:2222:3333:8880::/57", "1111:2222:3333:8900::/56", "1111:2222:3333:8a00::/55", "1111:2222:3333:8c00::/54", "1111:2222:3333:9000::/52", "1111:2222:3333:a000::/51", "1111:2222:3333:c000::/50", "1111:2222:3334::/46", "1111:2222:3338::/45", "1111:2222:3340::/42", "1111:2222:3380::/41", "1111:2222:3400::/38", "1111:2222:3800::/37", "1111:2222:4000::/34", "1111:2222:8000::/33" }.Select(v => IPNetworkHelper.Parse(v)).ToArray();

            Assert.IsTrue(result.Select((n, i) => expected[i].Equals(n)).All(v => true));
        }

        [TestMethod]
        public void HasValidPrefix()
        {
            var ip = IPAddress.Parse("192.168.0.16");
            var n1 = new IPNetwork(ip, 28);
            var n2 = new IPNetwork(ip, 27);

            Assert.IsTrue(IPNetworkHelper.HasValidPrefix(n1));
            Assert.IsFalse(IPNetworkHelper.HasValidPrefix(n2));

            // Parse a network, throws on invalid networks
            var network = IPNetworkHelper.Parse("192.168.0.0/16");
            
            // Tries to parse, returns true when succeeded, false otherwise and the parsed network
            if (IPNetworkHelper.TryParse("192.168.0.0/16", out var othernetwork))
            {
                // ...
            }

            // Get first/last IP from network
            var first = network.GetFirstIP();   // Network      (192.168.0.0)
            var last = network.GetLastIP();     // Broadcast    (192.168.255.255)

            // Splits a network into two halves
            var (left, right) = network.Split();    // Returns 192.168.0.0/17 and 192.168.128.0/17

            // Remove a subnet from a network
            var desired = IPNetworkHelper.Parse("192.168.10.16/28");
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
        }
    }
}
