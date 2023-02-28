namespace DependencyInjectionContainer.Exceptions;
using System;

public sealed class ResolveServiceException : Exception
{
    public ResolveServiceException(string message) : base(message) { }
}
