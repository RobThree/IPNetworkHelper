using System;

namespace IPNetworkHelper.Exceptions;

public abstract class IPNetworkException(string? message, Exception? innerExeption = null)
    : Exception(message, innerExeption)
{ }
