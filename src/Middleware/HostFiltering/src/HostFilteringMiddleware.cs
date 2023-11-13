// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HostFiltering;

/// <summary>
/// A middleware used to filter requests by their Host header.
/// </summary>
public class HostFilteringMiddleware
{
    // Matches Http.Sys.
    private static readonly byte[] DefaultResponse = Encoding.ASCII.GetBytes(
        "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\"\"http://www.w3.org/TR/html4/strict.dtd\">\r\n"
            + "<HTML><HEAD><TITLE>Bad Request</TITLE>\r\n"
            + "<META HTTP-EQUIV=\"Content-Type\" Content=\"text/html; charset=us-ascii\"></ HEAD >\r\n"
            + "<BODY><h2>Bad Request - Invalid Hostname</h2>\r\n"
            + "<hr><p>HTTP Error 400. The request hostname is invalid.</p>\r\n"
            + "</BODY></HTML>"
    );

    private readonly RequestDelegate _next;
    private readonly ILogger<HostFilteringMiddleware> _logger;
    private readonly MiddlewareConfigurationManager _middlewareConfigurationManager;

    /// <summary>
    /// A middleware used to filter requests by their Host header.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="logger"></param>
    /// <param name="optionsMonitor"></param>
    public HostFilteringMiddleware(
        RequestDelegate next,
        ILogger<HostFilteringMiddleware> logger,
        IOptionsMonitor<HostFilteringOptions> optionsMonitor
    )
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _middlewareConfigurationManager = new MiddlewareConfigurationManager(optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor)), logger);
    }

    /// <summary>
    /// Processes requests
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Invoke(HttpContext context)
    {
        var middlewareConfiguration = _middlewareConfigurationManager.GetLatestMiddlewareConfiguration();

        if (!CheckHost(context, middlewareConfiguration))
        {
            return HostValidationFailed(context, middlewareConfiguration);
        }

        return _next(context);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task HostValidationFailed(HttpContext context, MiddlewareConfiguration middlewareConfiguration)
    {
        context.Response.StatusCode = 400;
        if (middlewareConfiguration.IncludeFailureMessage)
        {
            context.Response.ContentLength = DefaultResponse.Length;
            context.Response.ContentType = "text/html";
            return context.Response.Body.WriteAsync(DefaultResponse, 0, DefaultResponse.Length);
        }
        return Task.CompletedTask;
    }

    // This does not duplicate format validations that are expected to be performed by the host.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckHost(HttpContext context, MiddlewareConfiguration middlewareConfiguration)
    {
        var host = context.Request.Headers.Host.ToString();

        if (host.Length == 0)
        {
            return IsEmptyHostAllowed(context, middlewareConfiguration);
        }

        if (middlewareConfiguration.AllowAnyNonEmptyHost)
        {
            _logger.AllHostsAllowed();
            return true;
        }

        return CheckHostInAllowList(middlewareConfiguration.AllowedHosts, host);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool CheckHostInAllowList(IList<StringSegment>? allowedHosts, string host)
    {
        if (allowedHosts is null)
        {
            return false;
        }

        if (HostString.MatchesAny(new StringSegment(host), allowedHosts))
        {
            _logger.AllowedHostMatched(host);
            return true;
        }

        _logger.NoAllowedHostMatched(host);
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool IsEmptyHostAllowed(HttpContext context, MiddlewareConfiguration middlewareConfiguration)
    {
        // Http/1.0 does not require the host header.
        // Http/1.1 requires the header but the value may be empty.
        if (!middlewareConfiguration.AllowEmptyHosts)
        {
            _logger.RequestRejectedMissingHost(context.Request.Protocol);
            return false;
        }
        _logger.RequestAllowedMissingHost(context.Request.Protocol);
        return true;
    }

    internal sealed record MiddlewareConfiguration(bool AllowAnyNonEmptyHost, bool AllowEmptyHosts, bool IncludeFailureMessage, ReadOnlyCollection<StringSegment>? AllowedHosts = default);

    internal sealed class MiddlewareConfigurationManager
    {
        private readonly object _syncObject = new();
        private event Func<MiddlewareHostFilteringOptions>? _getChangeRequestObject;
        private MiddlewareConfiguration? _middlewareConfiguration;
        private readonly ILogger<HostFilteringMiddleware> _logger;

        internal MiddlewareConfigurationManager(IOptionsMonitor<HostFilteringOptions> _optionsMonitor, ILogger<HostFilteringMiddleware> logger)
        {
            _logger = logger;
            // configuration will evaluate during first request
            _getChangeRequestObject = () => new(_optionsMonitor.CurrentValue);
            _optionsMonitor.OnChange(options =>
            {
                lock (_syncObject)
                {
                    var middlewareHostFilteringOptions = new MiddlewareHostFilteringOptions(options);
                    _getChangeRequestObject = () => middlewareHostFilteringOptions;
                }
            });
        }
        internal MiddlewareConfiguration GetLatestMiddlewareConfiguration()
        {
            if (_getChangeRequestObject is not null)
            {
                MiddlewareHostFilteringOptions options;
                lock (_syncObject)
                {
                    options = _getChangeRequestObject();
                    _getChangeRequestObject = null;
                }
                _middlewareConfiguration = ConfigureMiddleware(options);
            }
            if (_middlewareConfiguration?.AllowAnyNonEmptyHost == true || _middlewareConfiguration?.AllowedHosts?.Count > 0)
            {
                return _middlewareConfiguration;
            }
            if (_middlewareConfiguration?.AllowedHosts is null || _middlewareConfiguration.AllowedHosts?.Count == 0)
            {
                throw new InvalidOperationException("No allowed hosts were configured.");
            }
            return _middlewareConfiguration;
        }

        private MiddlewareConfiguration ConfigureMiddleware(MiddlewareHostFilteringOptions options)
        {
            var allowedHosts = new List<StringSegment>();
            if (
                options.AllowedHosts?.Count > 0
                && !TryProcessHosts(options.AllowedHosts, allowedHosts)
            )
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
            return (
                string.Equals("*", host, StringComparison.Ordinal) // HttpSys wildcard
                || string.Equals("[::]", host, StringComparison.Ordinal) // Kestrel wildcard, IPv6 Any
                || string.Equals("0.0.0.0", host, StringComparison.Ordinal)
            ); // IPv4 Any
        }

        private sealed record MiddlewareHostFilteringOptions(ReadOnlyCollection<string> AllowedHosts, bool AllowEmptyHosts, bool IncludeFailureMessage)
        {
            public MiddlewareHostFilteringOptions(HostFilteringOptions options) : this(options.AllowedHosts.AsReadOnly(), options.AllowEmptyHosts, options.IncludeFailureMessage)
            {
            }
        };
    }
}
