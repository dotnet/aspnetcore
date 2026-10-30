// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Localization;

/// <summary>
/// Determines the culture information for a request via the value of the Accept-Language header.
/// </summary>
public class AcceptLanguageHeaderRequestCultureProvider : RequestCultureProvider
{
    /// <summary>
    /// The maximum number of values in the Accept-Language header to attempt to create a <see cref="System.Globalization.CultureInfo"/>
    /// from for the current request.
    /// Defaults to <c>3</c>.
    /// </summary>
    public int MaximumAcceptLanguageHeaderValuesToTry { get; set; } = 3;

    private const int MaxValuesToSortInPlace = 16;

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var acceptLanguageHeader = httpContext.Request.GetTypedHeaders().AcceptLanguage;

        if (acceptLanguageHeader == null || acceptLanguageHeader.Count == 0)
        {
            return NullProviderCultureResult;
        }

        var count = acceptLanguageHeader.Count;

        if (MaximumAcceptLanguageHeaderValuesToTry > 0)
        {
            // We take only the first configured number of languages from the header and then order those that we
            // attempt to parse as a CultureInfo to mitigate potentially spinning CPU on lots of parse attempts.
            count = Math.Min(count, MaximumAcceptLanguageHeaderValuesToTry);
        }

        List<StringSegment> orderedLanguages;

        if (count <= MaxValuesToSortInPlace)
        {
            orderedLanguages = SortByQuality(acceptLanguageHeader, count);
        }
        else
        {
            orderedLanguages = acceptLanguageHeader.Take(count)
                .OrderByDescending(h => h, StringWithQualityHeaderValueComparer.QualityComparer)
                .Select(x => x.Value).ToList();
        }

        if (orderedLanguages.Count > 0)
        {
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(orderedLanguages));
        }

        return NullProviderCultureResult;
    }

    // Stable insertion sort by descending quality, equivalent to OrderByDescending for small inputs.
    private static List<StringSegment> SortByQuality(IList<StringWithQualityHeaderValue> values, int count)
    {
        Span<int> order = stackalloc int[MaxValuesToSortInPlace];
        order = order.Slice(0, count);
        var comparer = StringWithQualityHeaderValueComparer.QualityComparer;

        for (var i = 0; i < count; i++)
        {
            var value = values[i];
            var j = i - 1;
            while (j >= 0 && comparer.Compare(values[order[j]], value) < 0)
            {
                order[j + 1] = order[j];
                j--;
            }
            order[j + 1] = i;
        }

        var result = new List<StringSegment>(count);
        for (var i = 0; i < count; i++)
        {
            result.Add(values[order[i]].Value);
        }

        return result;
    }
}
