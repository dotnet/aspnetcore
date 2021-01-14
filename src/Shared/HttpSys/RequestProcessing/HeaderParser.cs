// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

// Remove once HttpSys has enabled nullable
#nullable enable

namespace Microsoft.AspNetCore.HttpSys.Internal
{
    internal static class HeaderParser
    {
        internal static IEnumerable<string> Empty = new string[0];

        // Split on commas, except in quotes
        internal static IEnumerable<string> SplitValues(StringValues values)
        {
            foreach (var value in values)
            {
                int start = 0;
                bool inQuotes = false;
                int current = 0;
                for ( ; current < value.Length; current++)
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
                        start = current + 1;
                    }
                }
            }
        }
    }
}
