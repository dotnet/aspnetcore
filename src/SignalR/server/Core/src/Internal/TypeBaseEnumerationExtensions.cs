// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR.Internal;

internal static class TypeBaseEnumerationExtensions
{
    public static IEnumerable<Type> AllBaseTypes(this Type type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}
