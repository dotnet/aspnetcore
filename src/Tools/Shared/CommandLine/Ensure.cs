// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Tools.Internal;

internal static class Ensure
{
    public static T NotNull<T>(T obj, string paramName)
        where T : class
    {
        if (obj == null)
        {
            throw new ArgumentNullException(paramName);
        }
        return obj;
    }

    public static string NotNullOrEmpty(string obj, string paramName)
    {
        if (string.IsNullOrEmpty(obj))
        {
            throw new ArgumentException("Value cannot be null or an empty string.", paramName);
        }
        return obj;
    }
}
