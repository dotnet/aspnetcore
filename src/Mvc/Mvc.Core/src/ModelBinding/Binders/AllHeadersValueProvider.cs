// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

// Exposes the request headers as an <see cref="IValueProvider"/> keyed by header name so
// that property-level binders can resolve values when binding a complex [FromHeader] model.
internal sealed class AllHeadersValueProvider : IValueProvider
{
    private readonly IHeaderDictionary _headers;

    public AllHeadersValueProvider(IHeaderDictionary headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        _headers = headers;
    }

    public bool ContainsPrefix(string prefix)
    {
        return _headers.ContainsKey(prefix);
    }

    public ValueProviderResult GetValue(string key)
    {
        if (!_headers.TryGetValue(key, out var values) || values.Count == 0)
        {
            return ValueProviderResult.None;
        }

        return new ValueProviderResult(values, CultureInfo.InvariantCulture);
    }
}
