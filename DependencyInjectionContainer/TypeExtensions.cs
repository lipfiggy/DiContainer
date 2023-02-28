namespace DependencyInjectionContainer;
using System;
using System.Collections.Generic;

internal static class TypeExtensions
{
    public static bool IsEnumerable(this Type type) => type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
}
