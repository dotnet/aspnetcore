// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpsPolicy
{
    public class HttpsRedirectionMiddleware
    {
        private readonly RequestDelegate _next;
        private int? _httpsPort;
        private readonly int _statusCode;

        private readonly IServerAddressesFeature _serverAddressesFeature;
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes the HttpsRedirectionMiddleware
        /// </summary>
        /// <param name="next"></param>
        /// <param name="serverAddressesFeature">The</param>
        /// <param name="options"></param>
        /// <param name="config"></param>
        public HttpsRedirectionMiddleware(RequestDelegate next, IServerAddressesFeature serverAddressesFeature, IOptions<HttpsRedirectionOptions> options, IConfiguration config)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _config = config ?? throw new ArgumentException(nameof(config));
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _serverAddressesFeature = serverAddressesFeature ?? throw new ArgumentNullException(nameof(serverAddressesFeature));

            var httpsRedirectionOptions = options.Value;
            _httpsPort = httpsRedirectionOptions.HttpsPort;
            _statusCode = httpsRedirectionOptions.RedirectStatusCode;
        }

        /// <summary>
        /// Invokes the HttpsRedirectionMiddleware
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            if (context.Request.IsHttps)
            {
                return _next(context);
            }

            if (!_httpsPort.HasValue)
            {
                CheckForHttpsPorts();
            }

            var host = context.Request.Host;
            if (_httpsPort != 443)
            {
                host = new HostString(host.Host, _httpsPort.Value);
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

            return Task.CompletedTask;
        }

        private void CheckForHttpsPorts()
        {
            // The IServerAddressesFeature will not be ready until the middleware is Invoked,
            // Order for finding the HTTPS port:
            // 1. Set in the HttpsRedirectionOptions
            // 2. HTTPS_PORT environment variable
            // 3. IServerAddressesFeature
            // 4. 443 (or not set)

            _httpsPort = _config.GetValue<int?>("HTTPS_PORT");
            if (_httpsPort.HasValue)
            {
                return;
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
                        throw new ArgumentException("Cannot determine the https port from IServerAddressesFeature, multiple values were found. " +
                            "Please set the desired port explicitly on HttpsRedirectionOptions.HttpsPort.");
                    }
                    else
                    {
                        httpsPort = bindingAddress.Port;
                    }
                }
            }
            _httpsPort = httpsPort ?? 443;
        }
    }
}
