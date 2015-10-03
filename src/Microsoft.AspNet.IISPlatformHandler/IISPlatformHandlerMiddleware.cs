// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.IISPlatformHandler
{
    public class IISPlatformHandlerMiddleware
    {
        private const string XForwardedForHeaderName = "X-Forwarded-For";
        private const string XForwardedProtoHeaderName = "X-Forwarded-Proto";
        private const string XIISWindowsAuthToken = "X-IIS-WindowsAuthToken";
        private const string XOriginalProtoName = "X-Original-Proto";
        private const string XOriginalIPName = "X-Original-IP";

        private readonly RequestDelegate _next;

        public IISPlatformHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
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
            
            var xForwardedForHeaderValue = httpContext.Request.Headers.GetCommaSeparatedValues(XForwardedForHeaderName);
            if (xForwardedForHeaderValue != null && xForwardedForHeaderValue.Length > 0)
            {
                IPAddress ipFromHeader;
                if (IPAddress.TryParse(xForwardedForHeaderValue[0], out ipFromHeader))
                {
                    var remoteIPString = httpContext.Connection.RemoteIpAddress?.ToString();
                    if (!string.IsNullOrEmpty(remoteIPString))
                    {
                        httpContext.Request.Headers[XOriginalIPName] = remoteIPString;
                    }
                    httpContext.Connection.RemoteIpAddress = ipFromHeader;
                }
            }

            var xIISWindowsAuthToken = httpContext.Request.Headers[XIISWindowsAuthToken];
            int hexHandle;
            if (!StringValues.IsNullOrEmpty(xIISWindowsAuthToken)
                && int.TryParse(xIISWindowsAuthToken, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hexHandle))
            {
                var handle = new IntPtr(hexHandle);
                var winIdentity = new WindowsIdentity(handle);
                // WindowsIdentity just duplicated the handle so we need to close the original.
                NativeMethods.CloseHandle(handle);

                httpContext.Response.RegisterForDispose(winIdentity);
                var winPrincipal = new WindowsPrincipal(winIdentity);

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

            return _next(httpContext);
        }
    }
}
