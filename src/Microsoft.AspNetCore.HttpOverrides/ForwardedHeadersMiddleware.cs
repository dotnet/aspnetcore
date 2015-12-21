// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpOverrides
{
    public class ForwardedHeadersMiddleware
    {
        private const string XForwardedForHeaderName = "X-Forwarded-For";
        private const string XForwardedHostHeaderName = "X-Forwarded-Host";
        private const string XForwardedProtoHeaderName = "X-Forwarded-Proto";
        private const string XOriginalForName = "X-Original-For";
        private const string XOriginalHostName = "X-Original-Host";
        private const string XOriginalProtoName = "X-Original-Proto";

        private readonly ForwardedHeadersOptions _options;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ForwardedHeadersMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<ForwardedHeadersOptions> options)
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

            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ForwardedHeadersMiddleware>();
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            ApplyForwarders(context);
            return _next(context);
        }

        public void ApplyForwarders(HttpContext context)
        {
            // Gather expected headers. Enabled headers must have the same number of entries.
            string[] forwardedFor = null, forwardedProto = null, forwardedHost = null;
            bool checkFor = false, checkProto = false, checkHost = false;
            int entryCount = 0;

            if ((_options.ForwardedHeaders & ForwardedHeaders.XForwardedFor) == ForwardedHeaders.XForwardedFor)
            {
                checkFor = true;
                forwardedFor = context.Request.Headers.GetCommaSeparatedValues(XForwardedForHeaderName);
                if (StringValues.IsNullOrEmpty(forwardedFor))
                {
                    return;
                }
                entryCount = forwardedFor.Length;
            }

            if ((_options.ForwardedHeaders & ForwardedHeaders.XForwardedProto) == ForwardedHeaders.XForwardedProto)
            {
                checkProto = true;
                forwardedProto = context.Request.Headers.GetCommaSeparatedValues(XForwardedProtoHeaderName);
                if (StringValues.IsNullOrEmpty(forwardedProto))
                {
                    return;
                }
                if (checkFor && forwardedFor.Length != forwardedProto.Length)
                {
                    _logger.LogDebug(1, "Parameter count mismatch between X-Forwarded-For and X-Forwarded-Proto.");
                    return;
                }
                entryCount = forwardedProto.Length;
            }

            if ((_options.ForwardedHeaders & ForwardedHeaders.XForwardedHost) == ForwardedHeaders.XForwardedHost)
            {
                checkHost = true;
                forwardedHost = context.Request.Headers.GetCommaSeparatedValues(XForwardedHostHeaderName);
                if (StringValues.IsNullOrEmpty(forwardedHost))
                {
                    return;
                }
                if ((checkFor && forwardedFor.Length != forwardedHost.Length)
                    || (checkProto && forwardedProto.Length != forwardedHost.Length))
                {
                    _logger.LogDebug(1, "Parameter count mismatch between X-Forwarded-Host and X-Forwarded-For or X-Forwarded-Proto.");
                    return;
                }
                entryCount = forwardedHost.Length;
            }

            // Apply ForwardLimit, if any
            int offset = 0;
            if (_options.ForwardLimit.HasValue && entryCount > _options.ForwardLimit)
            {
                offset = entryCount - _options.ForwardLimit.Value;
                entryCount = _options.ForwardLimit.Value;
            }

            // Group the data together.
            var sets = new List<SetOfForwarders>(entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                var set = new SetOfForwarders();
                if (checkFor)
                {
                    set.IpAndPortText = forwardedFor[offset + i];
                }
                if (checkProto)
                {
                    set.Scheme = forwardedProto[offset + i];
                }
                if (checkHost)
                {
                    set.Host = forwardedHost[offset + i];
                }
                sets.Add(set);
            }
            // They get processed in reverse order, right to left.
            sets.Reverse();

            // Gather initial values
            var connection = context.Connection;
            var request = context.Request;
            var currentValues = new SetOfForwarders()
            {
                RemoteIpAndPort = connection.RemoteIpAddress != null ? new IPEndPoint(connection.RemoteIpAddress, connection.RemotePort) : null,
                // Host and Scheme initial values are never inspected, no need to set them here.
            };

            var checkKnownIps = _options.KnownNetworks.Count > 0 || _options.KnownProxies.Count > 0;
            bool applyChanges = false;
            int entriesConsumed = 0;

            foreach (var set in sets)
            {
                if (checkFor)
                {
                    // For the first instance, allow remoteIp to be null for servers that don't support it natively.
                    if (currentValues.RemoteIpAndPort != null && checkKnownIps && !CheckKnownAddress(currentValues.RemoteIpAndPort.Address))
                    {
                        // Stop at the first unknown remote IP, but still apply changes processed so far.
                        _logger.LogDebug(1, $"Unknown proxy: {currentValues.RemoteIpAndPort}");
                        break;
                    }
                    if (!IPEndPointParser.TryParse(set.IpAndPortText, out set.RemoteIpAndPort))
                    {
                        _logger.LogDebug(2, $"Failed to parse forwarded IPAddress: {currentValues.IpAndPortText}");
                        return;
                    }
                }

                if (checkProto)
                {
                    if (string.IsNullOrEmpty(set.Scheme))
                    {
                        _logger.LogDebug(3, $"Failed to parse forwarded scheme: {set.Scheme}");
                        return;
                    }
                }

                if (checkHost)
                {
                    if (string.IsNullOrEmpty(set.Host))
                    {
                        _logger.LogDebug(4, $"Failed to parse forwarded host: {set.Host}");
                        return;
                    }
                }

                applyChanges = true;
                currentValues = set;
                entriesConsumed++;
            }

            if (applyChanges)
            {
                if (checkFor)
                {
                    if (connection.RemoteIpAddress != null)
                    {
                        // Save the original
                        request.Headers[XOriginalForName] = new IPEndPoint(connection.RemoteIpAddress, connection.RemotePort).ToString();
                    }
                    if (forwardedFor.Length > entriesConsumed)
                    {
                        // Truncate the consumed header values
                        request.Headers[XForwardedForHeaderName] = forwardedFor.Take(forwardedFor.Length - entriesConsumed).ToArray();
                    }
                    else
                    {
                        // All values were consumed
                        request.Headers.Remove(XForwardedForHeaderName);
                    }
                    connection.RemoteIpAddress = currentValues.RemoteIpAndPort.Address;
                    connection.RemotePort = currentValues.RemoteIpAndPort.Port;
                }

                if (checkProto)
                {
                    // Save the original
                    request.Headers[XOriginalProtoName] = request.Scheme;
                    if (forwardedProto.Length > entriesConsumed)
                    {
                        // Truncate the consumed header values
                        request.Headers[XForwardedProtoHeaderName] = forwardedProto.Take(forwardedProto.Length - entriesConsumed).ToArray();
                    }
                    else
                    {
                        // All values were consumed
                        request.Headers.Remove(XForwardedProtoHeaderName);
                    }
                    request.Scheme = currentValues.Scheme;
                }

                if (checkHost)
                {
                    // Save the original
                    request.Headers[XOriginalHostName] = request.Host.ToString();
                    if (forwardedHost.Length > entriesConsumed)
                    {
                        // Truncate the consumed header values
                        request.Headers[XForwardedHostHeaderName] = forwardedHost.Take(forwardedHost.Length - entriesConsumed).ToArray();
                    }
                    else
                    {
                        // All values were consumed
                        request.Headers.Remove(XForwardedHostHeaderName);
                    }
                    request.Host = HostString.FromUriComponent(currentValues.Host);
                }
            }
        }

        private bool CheckKnownAddress(IPAddress address)
        {
            if (_options.KnownProxies.Contains(address))
            {
                return true;
            }
            foreach (var network in _options.KnownNetworks)
            {
                if (network.Contains(address))
                {
                    return true;
                }
            }
            return false;
        }

        private class SetOfForwarders
        {
            public string IpAndPortText;
            public IPEndPoint RemoteIpAndPort;
            public string Host;
            public string Scheme;
        }
    }
}
