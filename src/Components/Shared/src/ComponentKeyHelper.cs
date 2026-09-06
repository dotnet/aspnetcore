// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Components;

internal static class ComponentKeyHelper
{
    internal static bool IsSerializableKey(object key)
    {
        if (key == null)
        {
            return false;
        }
        var keyType = key.GetType();
        var result = Type.GetTypeCode(keyType) != TypeCode.Object
            || keyType == typeof(Guid)
            || keyType == typeof(DateTimeOffset)
            || keyType == typeof(DateOnly)
            || keyType == typeof(TimeOnly);

        return result;
    }

    internal static string? FormatSerializableKey(object? key)
    {
        if (key is null || !IsSerializableKey(key))
        {
            return null;
        }

        return key switch
        {
            IFormattable formattable => formattable.ToString("", CultureInfo.InvariantCulture),
            IConvertible convertible => convertible.ToString(CultureInfo.InvariantCulture),
            _ => default,
        };
    }
}
