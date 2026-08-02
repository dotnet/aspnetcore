// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HostFiltering;

internal sealed record MiddlewareConfiguration(bool AllowAnyNonEmptyHost, bool AllowEmptyHosts, bool IncludeFailureMessage, ReadOnlyCollection<StringSegment>? AllowedHosts = default);

internal sealed class MiddlewareConfigurationManager
{
    private MiddlewareConfiguration _middlewareConfiguration;
    private readonly ILogger<HostFilteringMiddleware> _logger;

    internal MiddlewareConfigurationManager(IOptionsMonitor<HostFilteringOptions> _optionsMonitor, ILogger<HostFilteringMiddleware> logger)
    {
        _logger = logger;
        _middlewareConfiguration = ConfigureMiddleware(_optionsMonitor.CurrentValue);
        _optionsMonitor.OnChange(options => _middlewareConfiguration = ConfigureMiddleware(options));
    }
    internal MiddlewareConfiguration GetLatestMiddlewareConfiguration()
    {
        var config = _middlewareConfiguration;

        if (!config.AllowAnyNonEmptyHost && (config.AllowedHosts is null || config.AllowedHosts.Count == 0))
        {
            throw new InvalidOperationException("No allowed hosts were configured.");
        }

        return config;
    }

    private MiddlewareConfiguration ConfigureMiddleware(HostFilteringOptions options)
    {
        var allowedHosts = new List<StringSegment>();

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.MiddlewareConfigurationStarted(GetLogMessageForHostFilteringOptions(options));
        }

        if (options.AllowedHosts?.Count > 0 && !TryProcessHosts(options.AllowedHosts, allowedHosts))
        {
            _logger.WildcardDetected();
            return new(true, options.AllowEmptyHosts, options.IncludeFailureMessage);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.AllowedHosts(string.Join("; ", allowedHosts));
        }

        return new(false, options.AllowEmptyHosts, options.IncludeFailureMessage, allowedHosts.AsReadOnly());
    }

    // returns false if any wildcards were found
    private static bool TryProcessHosts(IEnumerable<string> incoming, List<StringSegment> results)
    {
        foreach (var entry in incoming)
        {
            // Punycode. Http.Sys requires you to register Unicode hosts, but the headers contain punycode.
            var host = new HostString(entry).ToUriComponent();

            if (IsTopLevelWildcard(host))
            {
                // Disable filtering
                return false;
            }

            if (!results.Contains(host, StringSegmentComparer.OrdinalIgnoreCase))
            {
                results.Add(host);
            }
        }

        return true;
    }

    private static bool IsTopLevelWildcard(string host)
    {
        return string.Equals("*", host, StringComparison.Ordinal) // HttpSys wildcard
            || string.Equals("[::]", host, StringComparison.Ordinal) // Kestrel wildcard, IPv6 Any
            || string.Equals("0.0.0.0", host, StringComparison.Ordinal);// IPv4 Any
    }

    private static string GetLogMessageForHostFilteringOptions(HostFilteringOptions hostFilteringOptions)
    {
        return $"{{AllowedHosts = {string.Join("; ", hostFilteringOptions.AllowedHosts)}, AllowEmptyHosts = {hostFilteringOptions.AllowEmptyHosts}, IncludeFailureMessage = {hostFilteringOptions.IncludeFailureMessage}}}";
    }
}
