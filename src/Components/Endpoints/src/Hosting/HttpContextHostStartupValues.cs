// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class HttpContextHostStartupValues(
    IEnumerable<IHttpContextStartupValueProvider> providers) : IHostStartupValues
{
    private IReadOnlyDictionary<string, string>? _values;

    public string? GetValue(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _values is not null && _values.TryGetValue(key, out var value) ? value : null;
    }

    public string GetRequired(string key)
        => GetValue(key) ?? throw new InvalidOperationException($"Startup value '{key}' was not provided.");

    internal void Initialize(HttpContext httpContext)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var provider in providers)
        {
            foreach (var (key, value) in provider.GetValues(httpContext))
            {
                if (!values.TryAdd(key, value))
                {
                    throw new InvalidOperationException($"The startup value key '{key}' was provided more than once.");
                }
            }
        }

        _values = values;
    }
}
