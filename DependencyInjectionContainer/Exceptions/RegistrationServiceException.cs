namespace DependencyInjectionContainer.Exceptions;
using System;

public sealed class RegistrationServiceException: Exception
{
    public RegistrationServiceException(string message) : base(message) { }
}
