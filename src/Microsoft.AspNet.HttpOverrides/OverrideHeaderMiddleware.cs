// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.HttpOverrides
{
    public class OverrideHeaderMiddleware
    {
        private const string XForwardedForHeaderName = "X-Forwarded-For";
        private const string XForwardedHostHeaderName = "X-Forwarded-Host";
        private const string XForwardedProtoHeaderName = "X-Forwarded-Proto";
        private readonly OverrideHeaderMiddlewareOptions _options;
        private readonly RequestDelegate _next;

        public OverrideHeaderMiddleware([NotNull] RequestDelegate next, [NotNull] OverrideHeaderMiddlewareOptions options)
        {
            _options = options;
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if ((_options.ForwardedOptions & ForwardedHeaders.XForwardedFor) != 0)
            {
                var xForwardedForHeaderValue = context.Request.Headers.GetCommaSeparatedValues(XForwardedForHeaderName);
                if (xForwardedForHeaderValue != null && xForwardedForHeaderValue.Count > 0)
                {
                    IPAddress originalIPAddress;
                    if (IPAddress.TryParse(xForwardedForHeaderValue[0], out originalIPAddress))
                    {
                        if (context.Connection.RemoteIpAddress != null)
                        {
                            var ipList = context.Request.Headers.Get(XForwardedForHeaderName);
                            context.Request.Headers.Set(XForwardedForHeaderName, (ipList + "," + context.Connection.RemoteIpAddress.ToString()));
                        }
                        context.Connection.RemoteIpAddress = originalIPAddress;
                    }
                }
            }

            if ((_options.ForwardedOptions & ForwardedHeaders.XForwardedHost) != 0)
            {
                var xForwardHostHeaderValue = context.Request.Headers.Get(XForwardedHostHeaderName);
                if (!string.IsNullOrEmpty(xForwardHostHeaderValue))
                {
                    context.Request.Host = HostString.FromUriComponent(xForwardHostHeaderValue);
                }
            }

            if ((_options.ForwardedOptions & ForwardedHeaders.XForwardedProto) != 0)
            {
                var xForwardProtoHeaderValue = context.Request.Headers.Get(XForwardedProtoHeaderName);
                if (!string.IsNullOrEmpty(xForwardProtoHeaderValue))
                {
                    context.Request.Scheme = xForwardProtoHeaderValue;
                }
            }

            return _next(context);
        }
    }
}
