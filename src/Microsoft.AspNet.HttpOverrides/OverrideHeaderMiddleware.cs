// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNet.HttpOverrides
{
    public class OverrideHeaderMiddleware
    {
        private const string XForwardedForHeaderName = "X-Forwarded-For";
        private const string XForwardedHostHeaderName = "X-Forwarded-Host";
        private const string XForwardedProtoHeaderName = "X-Forwarded-Proto";
        private const string XOriginalForName = "X-Original-For";
        private const string XOriginalHostName = "X-Original-Host";
        private const string XOriginalProtoName = "X-Original-Proto";
        private readonly OverrideHeaderOptions _options;
        private readonly RequestDelegate _next;

        public OverrideHeaderMiddleware(RequestDelegate next, IOptions<OverrideHeaderOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            UpdateRemoteIp(context);

            UpdateHost(context);

            UpdateScheme(context);

            return _next(context);
        }

        private void UpdateRemoteIp(HttpContext context)
        {
            if ((_options.ForwardedOptions & ForwardedHeaders.XForwardedFor) != 0)
            {
                var xForwardedForHeaderValue = context.Request.Headers.GetCommaSeparatedValues(XForwardedForHeaderName);
                if (xForwardedForHeaderValue != null && xForwardedForHeaderValue.Length > 0)
                {
                    IPEndPoint endpoint;
                    if (IPEndPointParser.TryParse(xForwardedForHeaderValue[0], out endpoint))
                    {
                        var connection = context.Connection;
                        var remoteIP = connection.RemoteIpAddress;
                        if (remoteIP != null)
                        {
                            var remoteIPString = new IPEndPoint(remoteIP, connection.RemotePort).ToString();
                            context.Request.Headers[XOriginalForName] = remoteIPString;
                        }
                        connection.RemoteIpAddress = endpoint.Address;
                        connection.RemotePort = endpoint.Port;
                    }
                }
            }
        }

        private void UpdateHost(HttpContext context)
        {
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
        }

        private void UpdateScheme(HttpContext context)
        {
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
        }
    }
}
