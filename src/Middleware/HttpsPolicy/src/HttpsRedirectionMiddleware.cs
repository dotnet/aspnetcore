// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.HttpsPolicy.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpsPolicy
{
    public class HttpsRedirectionMiddleware
    {
        private readonly RequestDelegate _next;
        private bool _portEvaluated = false;
        private int? _httpsPort;
        private readonly int _statusCode;

        private readonly IServerAddressesFeature _serverAddressesFeature;
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes the HttpsRedirectionMiddleware
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="config"></param>
        /// <param name="loggerFactory"></param>
        public HttpsRedirectionMiddleware(RequestDelegate next, IOptions<HttpsRedirectionOptions> options, IConfiguration config, ILoggerFactory loggerFactory)

        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            var httpsRedirectionOptions = options.Value;
            _httpsPort = httpsRedirectionOptions.HttpsPort;
            _portEvaluated = _httpsPort.HasValue;
            _statusCode = httpsRedirectionOptions.RedirectStatusCode;
            _logger = loggerFactory.CreateLogger<HttpsRedirectionMiddleware>();
        }

        /// <summary>
        /// Initializes the HttpsRedirectionMiddleware
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="config"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="serverAddressesFeature">The</param>
        public HttpsRedirectionMiddleware(RequestDelegate next, IOptions<HttpsRedirectionOptions> options, IConfiguration config, ILoggerFactory loggerFactory,
            IServerAddressesFeature serverAddressesFeature)
            : this(next, options, config, loggerFactory)
        {
            _serverAddressesFeature = serverAddressesFeature ?? throw new ArgumentNullException(nameof(serverAddressesFeature));
        }

        /// <summary>
        /// Invokes the HttpsRedirectionMiddleware
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            if (context.Request.IsHttps || !TryGetHttpsPort(out var port))
            {
                return _next(context);
            }

            var host = context.Request.Host;
            if (port != 443)
            {
                host = new HostString(host.Host, port);
            }
            else
            {
                host = new HostString(host.Host);
            }

            var request = context.Request;
            var redirectUrl = UriHelper.BuildAbsolute(
                "https", 
                host,
                request.PathBase,
                request.Path,
                request.QueryString);

            context.Response.StatusCode = _statusCode;
            context.Response.Headers[HeaderNames.Location] = redirectUrl;

            _logger.RedirectingToHttps(redirectUrl);

            return Task.CompletedTask;
        }

        private bool TryGetHttpsPort(out int port)
        {
            // The IServerAddressesFeature will not be ready until the middleware is Invoked,
            // Order for finding the HTTPS port:
            // 1. Set in the HttpsRedirectionOptions
            // 2. HTTPS_PORT environment variable
            // 3. IServerAddressesFeature
            // 4. Fail if not set

            port = -1;

            if (_portEvaluated)
            {
                port = _httpsPort ?? port;
                return _httpsPort.HasValue;
            }
            _portEvaluated = true;

            _httpsPort = _config.GetValue<int?>("HTTPS_PORT");
            if (_httpsPort.HasValue)
            {
                port = _httpsPort.Value;
                _logger.PortLoadedFromConfig(port);
                return true;
            }

            if (_serverAddressesFeature == null)
            {
                _logger.FailedToDeterminePort();
                return false;
            }

            int? httpsPort = null;
            foreach (var address in _serverAddressesFeature.Addresses)
            {
                var bindingAddress = BindingAddress.Parse(address);
                if (bindingAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    // If we find multiple different https ports specified, throw
                    if (httpsPort.HasValue && httpsPort != bindingAddress.Port)
                    {
                        _logger.FailedMultiplePorts();
                        return false;
                    }
                    else
                    {
                        httpsPort = bindingAddress.Port;
                    }
                }
            }

            if (httpsPort.HasValue)
            {
                _httpsPort = httpsPort;
                port = _httpsPort.Value;
                _logger.PortFromServer(port);
                return true;
            }

            _logger.FailedToDeterminePort();
            return false;
        }
    }
}
