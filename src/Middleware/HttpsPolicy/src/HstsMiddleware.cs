// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpsPolicy
{
    /// <summary>
    /// Enables HTTP Strict Transport Security (HSTS)
    /// See https://tools.ietf.org/html/rfc6797.
    /// </summary>
    public class HstsMiddleware
    {
        private const string IncludeSubDomains = "; includeSubDomains";
        private const string Preload = "; preload";

        private readonly RequestDelegate _next;
        private readonly StringValues _strictTransportSecurityValue;
        private readonly IList<string> _excludedHosts;
        private readonly ILogger _logger;

        /// <summary>
        /// Initialize the HSTS middleware.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public HstsMiddleware(RequestDelegate next, IOptions<HstsOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next ?? throw new ArgumentNullException(nameof(next));

            var hstsOptions = options.Value;
            var maxAge = Convert.ToInt64(Math.Floor(hstsOptions.MaxAge.TotalSeconds))
                            .ToString(CultureInfo.InvariantCulture);
            var includeSubdomains = hstsOptions.IncludeSubDomains ? IncludeSubDomains : StringSegment.Empty;
            var preload = hstsOptions.Preload ? Preload : StringSegment.Empty;
            _strictTransportSecurityValue = new StringValues($"max-age={maxAge}{includeSubdomains}{preload}");
            _excludedHosts = hstsOptions.ExcludedHosts;
            _logger = loggerFactory.CreateLogger<HstsMiddleware>();
        }

        /// <summary>
        /// Initialize the HSTS middleware.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public HstsMiddleware(RequestDelegate next, IOptions<HstsOptions> options)
            : this(next, options, NullLoggerFactory.Instance) { }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            if (!context.Request.IsHttps)
            {
                _logger.SkippingInsecure();
                return _next(context);
            }

            if (IsHostExcluded(context.Request.Host.Host))
            {
                _logger.SkippingExcludedHost(context.Request.Host.Host);
                return _next(context);
            }

            context.Response.Headers[HeaderNames.StrictTransportSecurity] = _strictTransportSecurityValue;
            _logger.AddingHstsHeader();

            return _next(context);
        }

        private bool IsHostExcluded(string host)
        {
            if (_excludedHosts == null)
            {
                return false;
            }

            for (var i = 0; i < _excludedHosts.Count; i++)
            {
                if (string.Equals(host, _excludedHosts[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
