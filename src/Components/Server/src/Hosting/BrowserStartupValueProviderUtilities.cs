// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

internal static class BrowserStartupValueProviderUtilities
{
    internal static string[] GetKeys(IEnumerable<IBrowserStartupValueProvider> providers)
    {
        var keys = new List<string>();
        var uniqueKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var provider in providers)
        {
            foreach (var key in provider.Keys)
            {
                if (key is null)
                {
                    throw new InvalidOperationException("A browser startup value provider returned a null key.");
                }

                if (!uniqueKeys.Add(key))
                {
                    throw new InvalidOperationException($"The browser startup value key '{key}' was provided more than once.");
                }

                keys.Add(key);
            }
        }

        return [.. keys];
    }
}
