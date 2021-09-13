using System.Net.Sockets;

namespace IPNetworkHelper
{
    public class AddressFamilyMismatchException : IPNetworkException
    {
        public AddressFamily AddressFamily { get; private set; }
        public AddressFamily Other { get; private set; }

        public AddressFamilyMismatchException(AddressFamily addressFamily, AddressFamily other)
            : base($"AddressFamily mismatch")
        {
            AddressFamily = addressFamily;
            Other = other;
        }
    }
}
