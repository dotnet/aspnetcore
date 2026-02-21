// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components.QuickGrid.Infrastructure;

internal static class QueryStringHelper
{
    /// <summary>
    /// Reads the decoded value of the first query string parameter matching <paramref name="parameterName"/>
    /// from the given <paramref name="uri"/>, or returns <see langword="null"/> if not found.
    /// </summary>
    internal static string? ReadQueryStringValue(string uri, string parameterName)
    {
        var queryStart = uri.IndexOf('?');
        if (queryStart < 0)
        {
            return null;
        }

        var queryEnd = uri.IndexOf('#', queryStart);
        var query = uri.AsMemory(queryStart..((queryEnd < 0) ? uri.Length : queryEnd));
        var enumerable = new QueryStringEnumerable(query);

        foreach (var pair in enumerable)
        {
            if (pair.DecodeName().Span.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return pair.DecodeValue().ToString();
            }
        }

        return null;
    }
}
