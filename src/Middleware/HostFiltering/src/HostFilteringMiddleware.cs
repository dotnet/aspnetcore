// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HostFiltering
{
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
            + "</BODY></HTML>");

        private readonly RequestDelegate _next;
        private readonly ILogger<HostFilteringMiddleware> _logger;
        private readonly IOptionsMonitor<HostFilteringOptions> _optionsMonitor;
        private HostFilteringOptions _options;
        private IList<StringSegment> _allowedHosts;
        private bool? _allowAnyNonEmptyHost;

        /// <summary>
        /// A middleware used to filter requests by their Host header.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        /// <param name="optionsMonitor"></param>
        public HostFilteringMiddleware(RequestDelegate next, ILogger<HostFilteringMiddleware> logger, 
            IOptionsMonitor<HostFilteringOptions> optionsMonitor)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _options = _optionsMonitor.CurrentValue;
            _optionsMonitor.OnChange(options =>
            {
                // Clear the cached settings so the next EnsureConfigured will re-evaluate.
                _options = options;
                _allowedHosts = new List<StringSegment>();
                _allowAnyNonEmptyHost = null;
            });
        }

        /// <summary>
        /// Processes requests
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            var allowedHosts = EnsureConfigured();

            if (!CheckHost(context, allowedHosts))
            {
                return HostValidationFailed(context);
            }

            return _next(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IList<StringSegment> EnsureConfigured()
        {
            if (_allowAnyNonEmptyHost == true || _allowedHosts?.Count > 0)
            {
                return _allowedHosts;
            }

            return Configure();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Task HostValidationFailed(HttpContext context)
        {
            context.Response.StatusCode = 400;
            if (_options.IncludeFailureMessage)
            {
                context.Response.ContentLength = DefaultResponse.Length;
                context.Response.ContentType = "text/html";
                return context.Response.Body.WriteAsync(DefaultResponse, 0, DefaultResponse.Length);
            }
            return Task.CompletedTask;
        }

        private IList<StringSegment> Configure()
        {
            var allowedHosts = new List<StringSegment>();
            if (_options.AllowedHosts?.Count > 0 && !TryProcessHosts(_options.AllowedHosts, allowedHosts))
            {
                _logger.WildcardDetected();
                _allowedHosts = allowedHosts;
                _allowAnyNonEmptyHost = true;
                return _allowedHosts;
            }

            if (allowedHosts.Count == 0)
            {
                throw new InvalidOperationException("No allowed hosts were configured.");
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.AllowedHosts(string.Join("; ", allowedHosts));
            }

            _allowedHosts = allowedHosts;
            return _allowedHosts;
        }

        // returns false if any wildcards were found
        private bool TryProcessHosts(IEnumerable<string> incoming, IList<StringSegment> results)
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

        private bool IsTopLevelWildcard(string host)
        {
            return (string.Equals("*", host, StringComparison.Ordinal) // HttpSys wildcard
                           || string.Equals("[::]", host, StringComparison.Ordinal) // Kestrel wildcard, IPv6 Any
                           || string.Equals("0.0.0.0", host, StringComparison.Ordinal)); // IPv4 Any
        }

        // This does not duplicate format validations that are expected to be performed by the host.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckHost(HttpContext context, IList<StringSegment> allowedHosts)
        {
            var host = context.Request.Headers[HeaderNames.Host].ToString();

            if (host.Length == 0)
            {
                return IsEmptyHostAllowed(context);
            }

            if (_allowAnyNonEmptyHost == true)
            {
                _logger.AllHostsAllowed();
                return true;
            }

            return CheckHostInAllowList(allowedHosts, host);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool CheckHostInAllowList(IList<StringSegment> allowedHosts, string host)
        {
            if (HostString.MatchesAny(new StringSegment(host), allowedHosts))
            {
                _logger.AllowedHostMatched(host);
                return true;
            }

            _logger.NoAllowedHostMatched(host);
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool IsEmptyHostAllowed(HttpContext context)
        {
            // Http/1.0 does not require the host header.
            // Http/1.1 requires the header but the value may be empty.
            if (!_options.AllowEmptyHosts)
            {
                _logger.RequestRejectedMissingHost(context.Request.Protocol);
                return false;
            }
            _logger.RequestAllowedMissingHost(context.Request.Protocol);
            return true;
        }
    }
}
