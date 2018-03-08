// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    public class IISMiddleware
    {
        private const string MSAspNetCoreClientCert = "MS-ASPNETCORE-CLIENTCERT";
        private const string MSAspNetCoreToken = "MS-ASPNETCORE-TOKEN";
        private const string MSAspNetCoreEvent = "MS-ASPNETCORE-EVENT";
        private const string ANCMShutdownEventHeaderValue = "shutdown";
        private static readonly PathString ANCMRequestPath = new PathString("/iisintegration");

        private readonly RequestDelegate _next;
        private readonly IISOptions _options;
        private readonly ILogger _logger;
        private readonly string _pairingToken;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly bool _isWebsocketsSupported;

        // Can't break public API, so creating a second constructor to propagate the isWebsocketsSupported flag.
        public IISMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory,
            IOptions<IISOptions> options,
            string pairingToken,
            IAuthenticationSchemeProvider authentication,
            IApplicationLifetime applicationLifetime)
            : this(next, loggerFactory, options, pairingToken, isWebsocketsSupported: true, authentication, applicationLifetime)
        {
        }

        public IISMiddleware(RequestDelegate next,
            ILoggerFactory loggerFactory,
            IOptions<IISOptions> options,
            string pairingToken,
            bool isWebsocketsSupported,
            IAuthenticationSchemeProvider authentication,
            IApplicationLifetime applicationLifetime)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (applicationLifetime == null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }
            if (string.IsNullOrEmpty(pairingToken))
            {
                throw new ArgumentException("Missing or empty pairing token.");
            }

            _next = next;
            _options = options.Value;

            if (_options.ForwardWindowsAuthentication)
            {
                authentication.AddScheme(new AuthenticationScheme(IISDefaults.AuthenticationScheme, _options.AuthenticationDisplayName, typeof(AuthenticationHandler)));
            }

            _pairingToken = pairingToken;
            _applicationLifetime = applicationLifetime;
            _logger = loggerFactory.CreateLogger<IISMiddleware>();
            _isWebsocketsSupported = isWebsocketsSupported;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!string.Equals(_pairingToken, httpContext.Request.Headers[MSAspNetCoreToken], StringComparison.Ordinal))
            {
                _logger.LogError($"'{MSAspNetCoreToken}' does not match the expected pairing token '{_pairingToken}', request rejected.");
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Handle shutdown from ANCM
            if (HttpMethods.IsPost(httpContext.Request.Method) &&
                httpContext.Request.Path.Equals(ANCMRequestPath) &&
                string.Equals(ANCMShutdownEventHeaderValue, httpContext.Request.Headers[MSAspNetCoreEvent], StringComparison.OrdinalIgnoreCase))
            {
                // Execute shutdown task on background thread without waiting for completion
                var shutdownTask = Task.Run(() => _applicationLifetime.StopApplication());
                httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
                return;
            }

            if (Debugger.IsAttached && string.Equals("DEBUG", httpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                // The Visual Studio debugger tooling sends a DEBUG request to make IIS & AspNetCoreModule launch the process
                // so the debugger can attach. Filter out this request from the app.
                return;
            }

            var bodySizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodySizeFeature != null && !bodySizeFeature.IsReadOnly)
            {
                // IIS already limits this, no need to do it twice.
                bodySizeFeature.MaxRequestBodySize = null;
            }

            if (_options.ForwardClientCertificate)
            {
                var header = httpContext.Request.Headers[MSAspNetCoreClientCert];
                if (!StringValues.IsNullOrEmpty(header))
                {
                    httpContext.Features.Set<ITlsConnectionFeature>(new ForwardedTlsConnectionFeature(_logger, header));
                }
            }

            if (_options.ForwardWindowsAuthentication)
            {
                // We must always process and clean up the windows identity, even if we don't assign the User.
                var result = await httpContext.AuthenticateAsync(IISDefaults.AuthenticationScheme);
                if (result.Succeeded && _options.AutomaticAuthentication)
                {
                    httpContext.User = result.Principal;
                }
            }

            // Remove the upgrade feature if websockets are not supported by ANCM.
            // The feature must be removed on a per request basis as the Upgrade feature exists per request.
            if (!_isWebsocketsSupported)
            {
                httpContext.Features.Set<IHttpUpgradeFeature>(null);
            }

            await _next(httpContext);
        }
    }
}
