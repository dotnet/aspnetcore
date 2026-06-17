// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static class HeaderParser
{
    internal static IEnumerable<string> Empty = Array.Empty<string>();

    // Split on commas, except in quotes
    internal static IEnumerable<string> SplitValues(StringValues values)
    {
        foreach (var value in values)
        {
            int start = 0;
            bool inQuotes = false;
            int current = 0;
            for (; current < value!.Length; current++)
            {
                char ch = value[current];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        inQuotes = false;
                    }
                }
                else if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == ',')
                {
                    var subValue = value.Substring(start, current - start);
                    if (!string.IsNullOrWhiteSpace(subValue))
                    {
                        yield return subValue.Trim();
                        start = current + 1;
                    }
                }
            }

            if (start < current)
            {
                var subValue = value.Substring(start, current - start);
                if (!string.IsNullOrWhiteSpace(subValue))
                {
                    yield return subValue.Trim();
                }
            }
        }
    }
}
