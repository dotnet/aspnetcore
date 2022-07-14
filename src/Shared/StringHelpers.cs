// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class StringHelpers
{
    public static T ParseValueOrDefault<T>(T defaultValue, string? stringValue, Func<string, T> parser)
    {
        if (string.IsNullOrEmpty(stringValue))
        {
            return defaultValue;
        }

        return parser(stringValue);
    }
}
