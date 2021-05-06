// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets
{
    /// <summary>
    /// Enables accepting WebSocket requests by adding a <see cref="IHttpWebSocketFeature"/>
    /// to the <see cref="HttpContext"/> if the request is a valid WebSocket request.
    /// </summary>
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketOptions _options;
        private readonly ILogger _logger;
        private readonly bool _anyOriginAllowed;
        private readonly List<string> _allowedOrigins;

        /// <summary>
        /// Creates a new instance of the <see cref="WebSocketMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">The configuration options.</param>
        /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> instance used to create loggers.</param>
        public WebSocketMiddleware(RequestDelegate next, IOptions<WebSocketOptions> options, ILoggerFactory loggerFactory)
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
            _options = options.Value;
            _allowedOrigins = _options.AllowedOrigins.Select(o => o.ToLowerInvariant()).ToList();
            _anyOriginAllowed = _options.AllowedOrigins.Count == 0 || _options.AllowedOrigins.Contains("*", StringComparer.Ordinal);

            _logger = loggerFactory.CreateLogger<WebSocketMiddleware>();

            // TODO: validate options.
        }

        /// <summary>
        /// Processes a request to determine if it is a WebSocket request, and if so,
        /// sets the <see cref="IHttpWebSocketFeature"/> on the <see cref="HttpContext.Features"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> representing the request.</param>
        /// <returns>The <see cref="Task"/> that represents the completion of the middleware pipeline.</returns>
        public Task Invoke(HttpContext context)
        {
            // Detect if an opaque upgrade is available. If so, add a websocket upgrade.
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            if (upgradeFeature != null && context.Features.Get<IHttpWebSocketFeature>() == null)
            {
                var webSocketFeature = new UpgradeHandshake(context, upgradeFeature, _options);
                context.Features.Set<IHttpWebSocketFeature>(webSocketFeature);

                if (!_anyOriginAllowed)
                {
                    // Check for Origin header
                    var originHeader = context.Request.Headers.Origin;

                    if (!StringValues.IsNullOrEmpty(originHeader) && webSocketFeature.IsWebSocketRequest)
                    {
                        // Check allowed origins to see if request is allowed
                        if (!_allowedOrigins.Contains(originHeader.ToString(), StringComparer.Ordinal))
                        {
                            _logger.LogDebug("Request origin {Origin} is not in the list of allowed origins.", originHeader);
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }
                    }
                }
            }

            return _next(context);
        }

        private class UpgradeHandshake : IHttpWebSocketFeature
        {
            private readonly HttpContext _context;
            private readonly IHttpUpgradeFeature _upgradeFeature;
            private readonly WebSocketOptions _options;
            private bool? _isWebSocketRequest;

            public UpgradeHandshake(HttpContext context, IHttpUpgradeFeature upgradeFeature, WebSocketOptions options)
            {
                _context = context;
                _upgradeFeature = upgradeFeature;
                _options = options;
            }

            public bool IsWebSocketRequest
            {
                get
                {
                    if (_isWebSocketRequest == null)
                    {
                        if (!_upgradeFeature.IsUpgradableRequest)
                        {
                            _isWebSocketRequest = false;
                        }
                        else
                        {
                            _isWebSocketRequest = CheckSupportedWebSocketRequest(_context.Request.Method, _context.Request.Headers);
                        }
                    }
                    return _isWebSocketRequest.Value;
                }
            }

            public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext acceptContext)
            {
                if (!IsWebSocketRequest)
                {
                    throw new InvalidOperationException("Not a WebSocket request."); // TODO: LOC
                }

                string? subProtocol = null;
                if (acceptContext != null)
                {
                    subProtocol = acceptContext.SubProtocol;
                }

                TimeSpan keepAliveInterval = _options.KeepAliveInterval;
                if (acceptContext is ExtendedWebSocketAcceptContext advancedAcceptContext)
                {
                    if (advancedAcceptContext.KeepAliveInterval.HasValue)
                    {
                        keepAliveInterval = advancedAcceptContext.KeepAliveInterval.Value;
                    }
                }

                string key = _context.Request.Headers.SecWebSocketKey;

                HandshakeHelpers.GenerateResponseHeaders(key, subProtocol, _context.Response.Headers);

                // TODO: get from options
                WebSocketDeflateOptions? deflateOptions = null;
                var ext = _context.Request.Headers["Sec-WebSocket-Extensions"];
                if (ext.Count != 0)
                {
                    var decline = false;
                    foreach (var extension in ext)
                    {
                        if (extension.TrimStart().StartsWith(ClientWebSocketDeflateConstants.Extension, StringComparison.Ordinal))
                        {
                            deflateOptions = new();
                            if (ParseDeflateOptions(extension, deflateOptions, out var hasClientMaxWindowBits))
                            {
                                Resp(_context.Response.Headers, deflateOptions, hasClientMaxWindowBits);
                                decline = false;
                                break;
                            }
                            else
                            {
                                decline = true;
                            }
                        }
                    }
                    if (decline)
                    {
                        throw new InvalidOperationException("'permessage-deflate' extension not accepted.");
                    }
                }

                Stream opaqueTransport = await _upgradeFeature.UpgradeAsync(); // Sets status code to 101

                var options = new WebSocketCreationOptions()
                {
                    IsServer = true,
                    KeepAliveInterval = keepAliveInterval,
                    SubProtocol = subProtocol,
                    DangerousDeflateOptions = deflateOptions,
                };

                return WebSocket.CreateFromStream(opaqueTransport, options);
            }

            public static bool CheckSupportedWebSocketRequest(string method, IHeaderDictionary requestHeaders)
            {
                if (!HttpMethods.IsGet(method))
                {
                    return false;
                }

                var foundHeader = false;

                var values = requestHeaders.GetCommaSeparatedValues(HeaderNames.SecWebSocketVersion);
                foreach (var value in values)
                {
                    if (string.Equals(value, Constants.Headers.SupportedVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        // WebSockets are long lived; so if the header values are valid we switch them out for the interned versions.
                        if (values.Length == 1)
                        {
                            requestHeaders.SecWebSocketVersion = Constants.Headers.SupportedVersion;
                        }
                        foundHeader = true;
                        break;
                    }
                }
                if (!foundHeader)
                {
                    return false;
                }
                foundHeader = false;

                values = requestHeaders.GetCommaSeparatedValues(HeaderNames.Connection);
                foreach (var value in values)
                {
                    if (string.Equals(value, HeaderNames.Upgrade, StringComparison.OrdinalIgnoreCase))
                    {
                        // WebSockets are long lived; so if the header values are valid we switch them out for the interned versions.
                        if (values.Length == 1)
                        {
                            requestHeaders.Connection = HeaderNames.Upgrade;
                        }
                        foundHeader = true;
                        break;
                    }
                }
                if (!foundHeader)
                {
                    return false;
                }
                foundHeader = false;

                values = requestHeaders.GetCommaSeparatedValues(HeaderNames.Upgrade);
                foreach (var value in values)
                {
                    if (string.Equals(value, Constants.Headers.UpgradeWebSocket, StringComparison.OrdinalIgnoreCase))
                    {
                        // WebSockets are long lived; so if the header values are valid we switch them out for the interned versions.
                        if (values.Length == 1)
                        {
                            requestHeaders.Upgrade = Constants.Headers.UpgradeWebSocket;
                        }
                        foundHeader = true;
                        break;
                    }
                }
                if (!foundHeader)
                {
                    return false;
                }

                return HandshakeHelpers.IsRequestKeyValid(requestHeaders.SecWebSocketKey.ToString());
            }

            internal static class ClientWebSocketDeflateConstants
            {
                /// <summary>
                /// The maximum length that this extension can have, assuming that we're not abusing white space.
                /// <para />
                /// "permessage-deflate; client_max_window_bits=15; client_no_context_takeover; server_max_window_bits=15; server_no_context_takeover"
                /// </summary>
                public const int MaxExtensionLength = 128;

                public const string Extension = "permessage-deflate";

                public const string ClientMaxWindowBits = "client_max_window_bits";
                public const string ClientNoContextTakeover = "client_no_context_takeover";

                public const string ServerMaxWindowBits = "server_max_window_bits";
                public const string ServerNoContextTakeover = "server_no_context_takeover";
            }

            private static bool ParseDeflateOptions(ReadOnlySpan<char> extension, WebSocketDeflateOptions options, out bool hasClientMaxWindowBits)
            {
                hasClientMaxWindowBits = false;
                while (true)
                {
                    int end = extension.IndexOf(';');
                    ReadOnlySpan<char> value = (end >= 0 ? extension[..end] : extension).Trim();

                    if (value.Length > 0)
                    {
                        if (value.SequenceEqual(ClientWebSocketDeflateConstants.ClientNoContextTakeover))
                        {
                            options.ClientContextTakeover = false;
                        }
                        else if (value.SequenceEqual(ClientWebSocketDeflateConstants.ServerNoContextTakeover))
                        {
                            options.ServerContextTakeover = false;
                        }
                        else if (value.StartsWith(ClientWebSocketDeflateConstants.ClientMaxWindowBits))
                        {
                            hasClientMaxWindowBits = true;
                            var clientMaxWindowBits = ParseWindowBits(value);
                            if (clientMaxWindowBits > options.ClientMaxWindowBits)
                            {
                                return false;
                            }
                            options.ClientMaxWindowBits = clientMaxWindowBits;
                        }
                        else if (value.StartsWith(ClientWebSocketDeflateConstants.ServerMaxWindowBits))
                        {
                            var serverMaxWindowBits = ParseWindowBits(value);
                            if (serverMaxWindowBits > options.ServerMaxWindowBits)
                            {
                                return false;
                            }
                            options.ServerMaxWindowBits = serverMaxWindowBits;
                        }

                        static int ParseWindowBits(ReadOnlySpan<char> value)
                        {
                            // parameters can be sent without a value by the client
                            var startIndex = value.IndexOf('=');

                            if (startIndex < 0 ||
                                !int.TryParse(value[(startIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int windowBits) ||
                                windowBits < 9 ||
                                windowBits > 15)
                            {
                                throw new WebSocketException(WebSocketError.HeaderError, "");
                            }

                            return windowBits;
                        }
                    }

                    if (end < 0)
                    {
                        break;
                    }
                    extension = extension[(end + 1)..];
                }

                return true;
            }

            private static void Resp(IHeaderDictionary headers, WebSocketDeflateOptions options, bool hasClientMaxWindowBits)
            {
                headers.Add("Sec-WebSocket-Extensions", GetDeflateOptions(options, hasClientMaxWindowBits));

                static string GetDeflateOptions(WebSocketDeflateOptions options, bool hasClientMaxWindowBits)
                {
                    var builder = new StringBuilder(ClientWebSocketDeflateConstants.MaxExtensionLength);
                    builder.Append(ClientWebSocketDeflateConstants.Extension);

                    // If a received extension negotiation offer doesn't have the
                    // "client_max_window_bits" extension parameter, the corresponding
                    // extension negotiation response to the offer MUST NOT include the
                    // "client_max_window_bits" extension parameter.
                    // https://tools.ietf.org/html/rfc7692#section-7.1.2.2
                    if (hasClientMaxWindowBits)
                    {
                        if (options.ClientMaxWindowBits != 15)
                        {
                            builder.Append("; ").Append(ClientWebSocketDeflateConstants.ClientMaxWindowBits).Append('=')
                               .Append(options.ClientMaxWindowBits.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append("; ").Append(ClientWebSocketDeflateConstants.ClientMaxWindowBits);
                        }
                    }

                    if (!options.ClientContextTakeover)
                    {
                        builder.Append("; ").Append(ClientWebSocketDeflateConstants.ClientNoContextTakeover);
                    }

                    if (options.ServerMaxWindowBits != 15)
                    {
                        builder.Append("; ")
                               .Append(ClientWebSocketDeflateConstants.ServerMaxWindowBits).Append('=')
                               .Append(options.ServerMaxWindowBits.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append("; ").Append(ClientWebSocketDeflateConstants.ServerMaxWindowBits);
                    }

                    if (!options.ServerContextTakeover)
                    {
                        builder.Append("; ").Append(ClientWebSocketDeflateConstants.ServerNoContextTakeover);
                    }

                    Debug.Assert(builder.Length <= ClientWebSocketDeflateConstants.MaxExtensionLength);
                    return builder.ToString();
                }
            }
        }
    }
}
