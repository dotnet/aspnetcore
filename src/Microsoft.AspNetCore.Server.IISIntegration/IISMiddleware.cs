// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    public class IISMiddleware
    {
        private const string MSAspNetCoreWinAuthToken = "MS-ASPNETCORE-WINAUTHTOKEN";
        private const string MSAspNetCoreClientCert = "MS-ASPNETCORE-CLIENTCERT";
        private const string MSAspNetCoreToken = "MS-ASPNETCORE-TOKEN";

        private readonly RequestDelegate _next;
        private readonly IISOptions _options;
        private readonly ILogger _logger;
        private readonly string _pairingToken;

        public IISMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IISOptions> options, string pairingToken)
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
            if (string.IsNullOrEmpty(pairingToken))
            {
                throw new ArgumentException("Missing or empty pairing token.");
            }

            _next = next;
            _options = options.Value;
            _pairingToken = pairingToken;
            _logger = loggerFactory.CreateLogger<IISMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!string.Equals(_pairingToken, httpContext.Request.Headers[MSAspNetCoreToken], StringComparison.Ordinal))
            {
                _logger.LogTrace($"'{MSAspNetCoreToken}' does not match the expected pairing token '{_pairingToken}', skipping {nameof(IISMiddleware)}.");
                await _next(httpContext);
                return;
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
                var winPrincipal = UpdateUser(httpContext);
                var handler = new AuthenticationHandler(httpContext, _options, winPrincipal);
                AttachAuthenticationHandler(handler);
                try
                {
                    await _next(httpContext);
                }
                finally
                {
                   DetachAuthenticationhandler(handler);
                }
            }
            else
            {
                await _next(httpContext);
            }
        }

        private WindowsPrincipal UpdateUser(HttpContext httpContext)
        {
            var tokenHeader = httpContext.Request.Headers[MSAspNetCoreWinAuthToken];

            int hexHandle;
            WindowsPrincipal winPrincipal = null;
            if (!StringValues.IsNullOrEmpty(tokenHeader)
                && int.TryParse(tokenHeader, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hexHandle))
            {
                // Always create the identity if the handle exists, we need to dispose it so it does not leak.
                var handle = new IntPtr(hexHandle);
                var winIdentity = new WindowsIdentity(handle);

                // WindowsIdentity just duplicated the handle so we need to close the original.
                NativeMethods.CloseHandle(handle);

                httpContext.Response.RegisterForDispose(winIdentity);
                winPrincipal = new WindowsPrincipal(winIdentity);

                if (_options.AutomaticAuthentication)
                {
                    // Don't get it from httpContext.User, that always returns a non-null anonymous user by default.
                    var existingPrincipal = httpContext.Features.Get<IHttpAuthenticationFeature>()?.User;
                    if (existingPrincipal != null)
                    {
                        httpContext.User = SecurityHelper.MergeUserPrincipal(existingPrincipal, winPrincipal);
                    }
                    else
                    {
                        httpContext.User = winPrincipal;
                    }
                }
            }

            return winPrincipal;
        }

        private void AttachAuthenticationHandler(AuthenticationHandler handler)
        {
            var auth = handler.HttpContext.Features.Get<IHttpAuthenticationFeature>();
            if (auth == null)
            {
                auth = new HttpAuthenticationFeature();
                handler.HttpContext.Features.Set(auth);
            }
            handler.PriorHandler = auth.Handler;
            auth.Handler = handler;
        }

        private void DetachAuthenticationhandler(AuthenticationHandler handler)
        {
            var auth = handler.HttpContext.Features.Get<IHttpAuthenticationFeature>();
            if (auth != null)
            {
                auth.Handler = handler.PriorHandler;
            }
        }
    }
}
