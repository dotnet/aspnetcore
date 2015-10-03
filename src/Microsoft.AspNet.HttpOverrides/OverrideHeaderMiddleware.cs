// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNet.HttpOverrides
{
    public class OverrideHeaderMiddleware
    {
        private const string XForwardedForHeaderName = "X-Forwarded-For";
        private const string XForwardedHostHeaderName = "X-Forwarded-Host";
        private const string XForwardedProtoHeaderName = "X-Forwarded-Proto";
        private const string XOriginalIPName = "X-Original-IP";
        private const string XOriginalHostName = "X-Original-Host";
        private const string XOriginalProtoName = "X-Original-Proto";
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
                if (xForwardedForHeaderValue != null && xForwardedForHeaderValue.Length > 0)
                {
                    IPAddress ipFromHeader;
                    if (IPAddress.TryParse(xForwardedForHeaderValue[0], out ipFromHeader))
                    {
                        var remoteIPString = context.Connection.RemoteIpAddress?.ToString();
                        if (!string.IsNullOrEmpty(remoteIPString))
                        {
                            context.Request.Headers[XOriginalIPName] = remoteIPString;
                        }
                        context.Connection.RemoteIpAddress = ipFromHeader;
                    }
                }
            }

            if ((_options.ForwardedOptions & ForwardedHeaders.XForwardedHost) != 0)
            {
                var xForwardHostHeaderValue = context.Request.Headers[XForwardedHostHeaderName];
                if (!string.IsNullOrEmpty(xForwardHostHeaderValue))
                {
                    var hostString = context.Request.Host.ToString();
                    if (!string.IsNullOrEmpty(hostString))
                    {
                        context.Request.Headers[XOriginalHostName] = hostString;
                    }
                    context.Request.Host = HostString.FromUriComponent(xForwardHostHeaderValue);
                }
            }

            if ((_options.ForwardedOptions & ForwardedHeaders.XForwardedProto) != 0)
            {
                var xForwardProtoHeaderValue = context.Request.Headers[XForwardedProtoHeaderName];
                if (!string.IsNullOrEmpty(xForwardProtoHeaderValue))
                {
                    if (!string.IsNullOrEmpty(context.Request.Scheme))
                    {
                        context.Request.Headers[XOriginalProtoName] = context.Request.Scheme;
                    }
                    context.Request.Scheme = xForwardProtoHeaderValue;
                }
            }

            return _next(context);
        }
    }
}
