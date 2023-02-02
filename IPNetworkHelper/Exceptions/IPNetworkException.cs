using System;

namespace IPNetworkHelper;

public abstract class IPNetworkException : Exception
{
    public IPNetworkException(string? message, Exception? innerExeption = null)
        : base(message, innerExeption) { }
}
