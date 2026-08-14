// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

internal sealed class DbscSourceSchemes
{
    private readonly object _claimsLock = new();
    private readonly Dictionary<string, string> _dbscSchemesBySourceScheme = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _sourceSchemesByDbscScheme = new(StringComparer.Ordinal);

    /// <summary>The registered DBSC handler schemes.</summary>
    public ISet<string> DbscSchemes { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Maps refresh cookie scheme → DBSC handler scheme.</summary>
    public IDictionary<string, string> RefreshSchemes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Maps session cookie scheme → DBSC handler scheme.</summary>
    public IDictionary<string, string> SessionSchemes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public string? FindDbscScheme(string sourceScheme, IOptionsMonitor<DbscOptions> optionsMonitor)
    {
        foreach (var dbscScheme in DbscSchemes)
        {
            _ = optionsMonitor.Get(dbscScheme);
        }

        lock (_claimsLock)
        {
            return _dbscSchemesBySourceScheme.GetValueOrDefault(sourceScheme);
        }
    }

    public void ClaimSourceScheme(string dbscScheme, string sourceScheme)
    {
        lock (_claimsLock)
        {
            if (_sourceSchemesByDbscScheme.TryGetValue(dbscScheme, out var previousSourceScheme) &&
                string.Equals(previousSourceScheme, sourceScheme, StringComparison.Ordinal))
            {
                return;
            }

            if (_dbscSchemesBySourceScheme.TryGetValue(sourceScheme, out var existingDbscScheme) &&
                !string.Equals(existingDbscScheme, dbscScheme, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The DBSC schemes '{existingDbscScheme}' and '{dbscScheme}' cannot share the source scheme '{sourceScheme}'.");
            }

            if (previousSourceScheme is not null)
            {
                _dbscSchemesBySourceScheme.Remove(previousSourceScheme);
            }

            _sourceSchemesByDbscScheme[dbscScheme] = sourceScheme;
            _dbscSchemesBySourceScheme[sourceScheme] = dbscScheme;
        }
    }
}
