using System.Net.Sockets;

namespace IPNetworkHelper.Exceptions;

public class AddressFamilyMismatchException(AddressFamily addressFamily, AddressFamily other)
    : IPNetworkException($"AddressFamily mismatch")
{
    public AddressFamily AddressFamily { get; private set; } = addressFamily;
    public AddressFamily Other { get; private set; } = other;
}
