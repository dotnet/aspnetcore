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

            // Make sure required options is not null or whitespace
            EnsureOptionNotNullorWhitespace(options.Value.ForwardedForHeaderName, nameof(options.Value.ForwardedForHeaderName));
            EnsureOptionNotNullorWhitespace(options.Value.ForwardedHostHeaderName, nameof(options.Value.ForwardedHostHeaderName));
            EnsureOptionNotNullorWhitespace(options.Value.ForwardedProtoHeaderName, nameof(options.Value.ForwardedProtoHeaderName));
            EnsureOptionNotNullorWhitespace(options.Value.OriginalForHeaderName, nameof(options.Value.OriginalForHeaderName));
            EnsureOptionNotNullorWhitespace(options.Value.OriginalHostHeaderName, nameof(options.Value.OriginalHostHeaderName));
            EnsureOptionNotNullorWhitespace(options.Value.OriginalProtoHeaderName, nameof(options.Value.OriginalProtoHeaderName));

            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ForwardedHeadersMiddleware>();
            _next = next;
        }

        private static void EnsureOptionNotNullorWhitespace(string value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"options.{propertyName} is required", "options");
            }
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
                forwardedFor = context.Request.Headers.GetCommaSeparatedValues(_options.ForwardedForHeaderName);
                entryCount = Math.Max(forwardedFor.Length, entryCount);
            }

            if ((_options.ForwardedHeaders & ForwardedHeaders.XForwardedProto) == ForwardedHeaders.XForwardedProto)
            {
                checkProto = true;
                forwardedProto = context.Request.Headers.GetCommaSeparatedValues(_options.ForwardedProtoHeaderName);
                if (_options.RequireHeaderSymmetry && checkFor && forwardedFor.Length != forwardedProto.Length)
                {
                    _logger.LogWarning(1, "Parameter count mismatch between X-Forwarded-For and X-Forwarded-Proto.");
                    return;
                }
                entryCount = Math.Max(forwardedProto.Length, entryCount);
            }

            if ((_options.ForwardedHeaders & ForwardedHeaders.XForwardedHost) == ForwardedHeaders.XForwardedHost)
            {
                checkHost = true;
                forwardedHost = context.Request.Headers.GetCommaSeparatedValues(_options.ForwardedHostHeaderName);
                if (_options.RequireHeaderSymmetry
                    && ((checkFor && forwardedFor.Length != forwardedHost.Length)
                        || (checkProto && forwardedProto.Length != forwardedHost.Length)))
                {
                    _logger.LogWarning(1, "Parameter count mismatch between X-Forwarded-Host and X-Forwarded-For or X-Forwarded-Proto.");
                    return;
                }
                entryCount =  Math.Max(forwardedHost.Length, entryCount);
            }

            // Apply ForwardLimit, if any
            if (_options.ForwardLimit.HasValue && entryCount > _options.ForwardLimit)
            {
                entryCount = _options.ForwardLimit.Value;
            }

            // Group the data together.
            var sets = new SetOfForwarders[entryCount];
            for (int i = 0; i < sets.Length; i++)
            {
                // They get processed in reverse order, right to left.
                var set = new SetOfForwarders();
                if (checkFor && i < forwardedFor.Length)
                {
                    set.IpAndPortText = forwardedFor[forwardedFor.Length - i - 1];
                }
                if (checkProto && i < forwardedProto.Length)
                {
                    set.Scheme = forwardedProto[forwardedProto.Length - i - 1];
                }
                if (checkHost && i < forwardedHost.Length)
                {
                    set.Host = forwardedHost[forwardedHost.Length - i - 1];
                }
                sets[i] = set;
            }

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

            for ( ; entriesConsumed < sets.Length; entriesConsumed++)
            {
                var set = sets[entriesConsumed];
                if (checkFor)
                {
                    // For the first instance, allow remoteIp to be null for servers that don't support it natively.
                    if (currentValues.RemoteIpAndPort != null && checkKnownIps && !CheckKnownAddress(currentValues.RemoteIpAndPort.Address))
                    {
                        // Stop at the first unknown remote IP, but still apply changes processed so far.
                        _logger.LogDebug(1, $"Unknown proxy: {currentValues.RemoteIpAndPort}");
                        break;
                    }

                    IPEndPoint parsedEndPoint;
                    if (IPEndPointParser.TryParse(set.IpAndPortText, out parsedEndPoint))
                    {
                        applyChanges = true;
                        set.RemoteIpAndPort = parsedEndPoint;
                        currentValues.IpAndPortText = set.IpAndPortText;
                        currentValues.RemoteIpAndPort = set.RemoteIpAndPort;
                    }
                    else if (!string.IsNullOrEmpty(set.IpAndPortText))
                    {
                        // Stop at the first unparsable IP, but still apply changes processed so far.
                        _logger.LogDebug(1, $"Unparsable IP: {set.IpAndPortText}");
                        break;
                    }
                    else if (_options.RequireHeaderSymmetry)
                    {
                        _logger.LogWarning(2, $"Missing forwarded IPAddress.");
                        return;
                    }
                }

                if (checkProto)
                {
                    if (!string.IsNullOrEmpty(set.Scheme))
                    {
                        applyChanges = true;
                        currentValues.Scheme = set.Scheme;
                    }
                    else if (_options.RequireHeaderSymmetry)
                    {
                        _logger.LogWarning(3, $"Forwarded scheme is not present, this is required by {nameof(_options.RequireHeaderSymmetry)}");
                        return;
                    }
                }

                if (checkHost)
                {
                    if (!string.IsNullOrEmpty(set.Host))
                    {
                        applyChanges = true;
                        currentValues.Host = set.Host;
                    }
                    else if (_options.RequireHeaderSymmetry)
                    {
                        _logger.LogWarning(4, $"Incorrect number of x-forwarded-proto header values, see {nameof(_options.RequireHeaderSymmetry)}.");
                        return;
                    }
                }
            }

            if (applyChanges)
            {
                if (checkFor && currentValues.RemoteIpAndPort != null)
                {
                    if (connection.RemoteIpAddress != null)
                    {
                        // Save the original
                        request.Headers[_options.OriginalForHeaderName] = new IPEndPoint(connection.RemoteIpAddress, connection.RemotePort).ToString();
                    }
                    if (forwardedFor.Length > entriesConsumed)
                    {
                        // Truncate the consumed header values
                        request.Headers[_options.ForwardedForHeaderName] = forwardedFor.Take(forwardedFor.Length - entriesConsumed).ToArray();
                    }
                    else
                    {
                        // All values were consumed
                        request.Headers.Remove(_options.ForwardedForHeaderName);
                    }
                    connection.RemoteIpAddress = currentValues.RemoteIpAndPort.Address;
                    connection.RemotePort = currentValues.RemoteIpAndPort.Port;
                }

                if (checkProto && currentValues.Scheme != null)
                {
                    // Save the original
                    request.Headers[_options.OriginalProtoHeaderName] = request.Scheme;
                    if (forwardedProto.Length > entriesConsumed)
                    {
                        // Truncate the consumed header values
                        request.Headers[_options.ForwardedProtoHeaderName] = forwardedProto.Take(forwardedProto.Length - entriesConsumed).ToArray();
                    }
                    else
                    {
                        // All values were consumed
                        request.Headers.Remove(_options.ForwardedProtoHeaderName);
                    }
                    request.Scheme = currentValues.Scheme;
                }

                if (checkHost && currentValues.Host != null)
                {
                    // Save the original
                    request.Headers[_options.OriginalHostHeaderName] = request.Host.ToString();
                    if (forwardedHost.Length > entriesConsumed)
                    {
                        // Truncate the consumed header values
                        request.Headers[_options.ForwardedHostHeaderName] = forwardedHost.Take(forwardedHost.Length - entriesConsumed).ToArray();
                    }
                    else
                    {
                        // All values were consumed
                        request.Headers.Remove(_options.ForwardedHostHeaderName);
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

        private struct SetOfForwarders
        {
            public string IpAndPortText;
            public IPEndPoint RemoteIpAndPort;
            public string Host;
            public string Scheme;
        }
    }
}
