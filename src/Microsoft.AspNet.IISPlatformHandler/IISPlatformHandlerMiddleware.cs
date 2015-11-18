// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.Http.Features.Authentication.Internal;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.IISPlatformHandler
{
    public class IISPlatformHandlerMiddleware
    {
        private const string XForwardedForHeaderName = "X-Forwarded-For";
        private const string XForwardedProtoHeaderName = "X-Forwarded-Proto";
        private const string XIISWindowsAuthToken = "X-IIS-WindowsAuthToken";
        private const string XOriginalPortName = "X-Original-Port";
        private const string XOriginalProtoName = "X-Original-Proto";
        private const string XOriginalIPName = "X-Original-IP";

        private readonly RequestDelegate _next;
        private readonly IISPlatformHandlerOptions _options;

        public IISPlatformHandlerMiddleware(RequestDelegate next, IISPlatformHandlerOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _next = next;
            _options = options;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            UpdateScheme(httpContext);

            UpdateRemoteIp(httpContext);

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

        private static void UpdateScheme(HttpContext httpContext)
        {
            var xForwardProtoHeaderValue = httpContext.Request.Headers[XForwardedProtoHeaderName];
            if (!string.IsNullOrEmpty(xForwardProtoHeaderValue))
            {
                if (!string.IsNullOrEmpty(httpContext.Request.Scheme))
                {
                    httpContext.Request.Headers[XOriginalProtoName] = httpContext.Request.Scheme;
                }
                httpContext.Request.Scheme = xForwardProtoHeaderValue;
            }
        }

        private static void UpdateRemoteIp(HttpContext httpContext)
        {
            var xForwardedForHeaderValue = httpContext.Request.Headers.GetCommaSeparatedValues(XForwardedForHeaderName);
            if (xForwardedForHeaderValue != null && xForwardedForHeaderValue.Length > 0)
            {
                IPAddress ipFromHeader;
                int? port;
                if (IPAddressWithPortParser.TryParse(xForwardedForHeaderValue[0], out ipFromHeader, out port))
                {
                    var connection = httpContext.Connection;
                    var remoteIPString = connection.RemoteIpAddress?.ToString();
                    if (!string.IsNullOrEmpty(remoteIPString))
                    {
                        httpContext.Request.Headers[XOriginalIPName] = remoteIPString;
                    }
                    if (port.HasValue)
                    {
                        if (connection.RemotePort != 0)
                        {
                            httpContext.Request.Headers[XOriginalPortName] = connection.RemotePort.ToString(CultureInfo.InvariantCulture);
                        }
                        connection.RemotePort = port.Value;
                    }
                    connection.RemoteIpAddress = ipFromHeader;
                }
            }
        }

        private WindowsPrincipal UpdateUser(HttpContext httpContext)
        {
            var xIISWindowsAuthToken = httpContext.Request.Headers[XIISWindowsAuthToken];
            int hexHandle;
            WindowsPrincipal winPrincipal = null;
            if (!StringValues.IsNullOrEmpty(xIISWindowsAuthToken)
                && int.TryParse(xIISWindowsAuthToken, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hexHandle))
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
                    var existingPrincipal = httpContext.User;
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
