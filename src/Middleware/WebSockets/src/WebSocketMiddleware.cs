// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.WebSockets;

/// <summary>
/// Enables accepting WebSocket requests by adding a <see cref="IHttpWebSocketFeature"/>
/// to the <see cref="HttpContext"/> if the request is a valid WebSocket request.
/// </summary>
public partial class WebSocketMiddleware
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
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

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
        var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
        if ((upgradeFeature != null || connectFeature != null) && context.Features.Get<IHttpWebSocketFeature>() == null)
        {
            var webSocketFeature = new WebSocketHandshake(context, upgradeFeature, connectFeature, _options, _logger);
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
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug("Request origin {Origin} is not in the list of allowed origins.", originHeader.ToString());
                        }
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                }
            }
        }

        return _next(context);
    }

    private sealed class WebSocketHandshake : IHttpWebSocketFeature
    {
        private readonly HttpContext _context;
        private readonly IHttpUpgradeFeature? _upgradeFeature;
        private readonly IHttpExtendedConnectFeature? _connectFeature;
        private readonly WebSocketOptions _options;
        private readonly ILogger _logger;
        private bool? _isWebSocketRequest;
        private bool _isH2WebSocket;

        public WebSocketHandshake(HttpContext context, IHttpUpgradeFeature? upgradeFeature, IHttpExtendedConnectFeature? connectFeature, WebSocketOptions options, ILogger logger)
        {
            _context = context;
            _upgradeFeature = upgradeFeature;
            _connectFeature = connectFeature;
            _options = options;
            _logger = logger;
        }

        public bool IsWebSocketRequest
        {
            get
            {
                if (_isWebSocketRequest == null)
                {
                    if (_connectFeature?.IsExtendedConnect == true)
                    {
                        _isH2WebSocket = CheckSupportedWebSocketRequestH2(_context.Request.Method, _connectFeature.Protocol, _context.Request.Headers);
                        _isWebSocketRequest = _isH2WebSocket;
                    }
                    else if (_upgradeFeature?.IsUpgradableRequest == true)
                    {
                        _isWebSocketRequest = CheckSupportedWebSocketRequest(_context.Request.Method, _context.Request.Headers);
                    }
                    else
                    {
                        _isWebSocketRequest = false;
                    }
                }
                return _isWebSocketRequest.Value;
            }
        }

        public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext acceptContext)
        {
            if (!IsWebSocketRequest)
            {
                throw new InvalidOperationException("Not a WebSocket request.");
            }

            string? subProtocol = null;
            bool enableCompression = false;
            bool serverContextTakeover = true;
            int serverMaxWindowBits = 15;
            TimeSpan keepAliveInterval = _options.KeepAliveInterval;
            TimeSpan keepAliveTimeout = _options.KeepAliveTimeout;
            if (acceptContext != null)
            {
                subProtocol = acceptContext.SubProtocol;
                enableCompression = acceptContext.DangerousEnableCompression;
                serverContextTakeover = !acceptContext.DisableServerContextTakeover;
                serverMaxWindowBits = acceptContext.ServerMaxWindowBits;
                keepAliveInterval = acceptContext.KeepAliveInterval ?? keepAliveInterval;
                keepAliveTimeout = acceptContext.KeepAliveTimeout ?? keepAliveTimeout;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (acceptContext is ExtendedWebSocketAcceptContext advancedAcceptContext)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                if (advancedAcceptContext.KeepAliveInterval.HasValue)
                {
                    keepAliveInterval = advancedAcceptContext.KeepAliveInterval.Value;
                }
            }

            HandshakeHelpers.GenerateResponseHeaders(!_isH2WebSocket, _context.Request.Headers, subProtocol, _context.Response.Headers);

            WebSocketDeflateOptions? deflateOptions = null;
            if (enableCompression)
            {
                var ext = _context.Request.Headers.SecWebSocketExtensions;
                if (ext.Count != 0)
                {
                    // loop over each extension offer, extensions can have multiple offers, we can accept any
                    foreach (var extension in _context.Request.Headers.GetCommaSeparatedValues(HeaderNames.SecWebSocketExtensions))
                    {
                        if (extension.AsSpan().TrimStart().StartsWith("permessage-deflate", StringComparison.Ordinal))
                        {
                            if (HandshakeHelpers.ParseDeflateOptions(extension.AsSpan().TrimStart(), serverContextTakeover, serverMaxWindowBits, out var parsedOptions, out var response))
                            {
                                Log.CompressionAccepted(_logger, response);
                                deflateOptions = parsedOptions;
                                // If more extension types are added, this would need to be a header append
                                // and we wouldn't want to break out of the loop
                                _context.Response.Headers.SecWebSocketExtensions = response;
                                break;
                            }
                        }
                    }

                    if (deflateOptions is null)
                    {
                        Log.CompressionNotAccepted(_logger);
                    }
                }
            }

            Stream opaqueTransport;
            // HTTP/2
            if (_isH2WebSocket)
            {
                // Send the response headers
                opaqueTransport = await _connectFeature!.AcceptAsync();
            }
            // HTTP/1.1
            else
            {
                opaqueTransport = await _upgradeFeature!.UpgradeAsync(); // Sets status code to 101
            }

            // Disable request timeout, if there is one, after the websocket has been accepted
            _context.Features.Get<IHttpRequestTimeoutFeature>()?.DisableTimeout();

            var abortStream = new AbortStream(_context, opaqueTransport);
            var wrappedSocket = WebSocket.CreateFromStream(abortStream, new WebSocketCreationOptions()
            {
                IsServer = true,
                KeepAliveInterval = keepAliveInterval,
                KeepAliveTimeout = keepAliveTimeout,
                SubProtocol = subProtocol,
                DangerousDeflateOptions = deflateOptions
            });

            abortStream.WebSocket = wrappedSocket;
            return wrappedSocket;
        }

        public static bool CheckSupportedWebSocketRequest(string method, IHeaderDictionary requestHeaders)
        {
            if (!HttpMethods.IsGet(method))
            {
                return false;
            }

            if (!CheckWebSocketVersion(requestHeaders))
            {
                return false;
            }

            var foundHeader = false;

            var values = requestHeaders.GetCommaSeparatedValues(HeaderNames.Upgrade);
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

            return HandshakeHelpers.IsRequestKeyValid(requestHeaders.SecWebSocketKey.ToString());
        }

        // https://datatracker.ietf.org/doc/html/rfc8441
        // :method = CONNECT
        // :protocol = websocket
        // :scheme = https
        // :path = /chat
        // :authority = server.example.com
        // sec-websocket-protocol = chat, superchat
        // sec-websocket-extensions = permessage-deflate
        // sec-websocket-version = 13
        // origin = http://www.example.com
        public static bool CheckSupportedWebSocketRequestH2(string method, string? protocol, IHeaderDictionary requestHeaders)
        {
            return HttpMethods.IsConnect(method)
                && string.Equals(protocol, Constants.Headers.UpgradeWebSocket, StringComparison.OrdinalIgnoreCase)
                && CheckWebSocketVersion(requestHeaders);
        }

        public static bool CheckWebSocketVersion(IHeaderDictionary requestHeaders)
        {
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
                    return true;
                }
            }
            return false;
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "WebSocket compression negotiation accepted with values '{CompressionResponse}'.", EventName = "CompressionAccepted")]
        public static partial void CompressionAccepted(ILogger logger, string compressionResponse);

        [LoggerMessage(2, LogLevel.Debug, "Compression negotiation not accepted by server.", EventName = "CompressionNotAccepted")]
        public static partial void CompressionNotAccepted(ILogger logger);
    }
}
