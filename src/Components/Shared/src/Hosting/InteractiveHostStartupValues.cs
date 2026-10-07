// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Hosting;

internal sealed class InteractiveHostStartupValues : IHostStartupValues
{
    private IReadOnlyDictionary<string, string>? _values;

    public string? GetValue(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _values is not null && _values.TryGetValue(key, out var value) ? value : null;
    }

    public string GetRequired(string key)
        => GetValue(key) ?? throw new InvalidOperationException($"Startup value '{key}' was not provided.");

    internal void Initialize(IReadOnlyDictionary<string, string> values)
        => _values = new Dictionary<string, string>(values, StringComparer.Ordinal);
}
