using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Components.TestServer
{
    public class InterruptibleWebSocketFeature : IHttpWebSocketFeature
    {
        public InterruptibleWebSocketFeature(
            HttpContext httpContext,
            string socketIdentifier,
            ConcurrentDictionary<string, InterruptibleWebSocket> registry)
        {
            HttpContext = httpContext;
            SocketIdentifier = socketIdentifier;
            OriginalFeature = new UpgradeHandshake(
                httpContext,
                httpContext.Features.Get<IHttpUpgradeFeature>(),
                httpContext.RequestServices.GetRequiredService<IOptions<WebSocketOptions>>().Value);
            Registry = registry;
        }

        public bool IsWebSocketRequest => OriginalFeature.IsWebSocketRequest;

        public HttpContext HttpContext { get; }
        public string SocketIdentifier { get; }

        private IHttpWebSocketFeature OriginalFeature { get; }
        public ConcurrentDictionary<string, InterruptibleWebSocket> Registry { get; }

        public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            var socket = new InterruptibleWebSocket(await OriginalFeature.AcceptAsync(context), SocketIdentifier);
            return Registry.AddOrUpdate(SocketIdentifier, socket, (k, e) =>
            {
                try
                {
                    e.Dispose();
                }
                catch (Exception)
                {
                }

                return socket;
            });
        }

        private class UpgradeHandshake : IHttpWebSocketFeature
        {
            public static readonly IEnumerable<string> NeededHeaders = new[]
            {
                HeaderNames.Upgrade,
                HeaderNames.Connection,
                HeaderNames.SecWebSocketKey,
                HeaderNames.SecWebSocketVersion
            };

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
                            var headers = new List<KeyValuePair<string, string>>();
                            foreach (string headerName in NeededHeaders)
                            {
                                foreach (var value in _context.Request.Headers.GetCommaSeparatedValues(headerName))
                                {
                                    headers.Add(new KeyValuePair<string, string>(headerName, value));
                                }
                            }
                            _isWebSocketRequest = CheckSupportedWebSocketRequest(_context.Request.Method, headers);
                        }
                    }
                    return _isWebSocketRequest.Value;
                }
            }

            public static bool CheckSupportedWebSocketRequest(string method, IEnumerable<KeyValuePair<string, string>> headers)
            {
                bool validUpgrade = false, validConnection = false, validKey = false, validVersion = false;

                if (!string.Equals("GET", method, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                foreach (var pair in headers)
                {
                    if (string.Equals(HeaderNames.Connection, pair.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(Constants.Headers.ConnectionUpgrade, pair.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            validConnection = true;
                        }
                    }
                    else if (string.Equals(HeaderNames.Upgrade, pair.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(Constants.Headers.UpgradeWebSocket, pair.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            validUpgrade = true;
                        }
                    }
                    else if (string.Equals(HeaderNames.SecWebSocketVersion, pair.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(Constants.Headers.SupportedVersion, pair.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            validVersion = true;
                        }
                    }
                    else if (string.Equals(HeaderNames.SecWebSocketKey, pair.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        validKey = IsRequestKeyValid(pair.Value);
                    }
                }

                return validConnection && validUpgrade && validVersion && validKey;
            }

            public static bool IsRequestKeyValid(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return false;
                }
                try
                {
                    byte[] data = Convert.FromBase64String(value);
                    return data.Length == 16;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public async Task<WebSocket> AcceptAsync(WebSocketAcceptContext acceptContext)
            {
                if (!IsWebSocketRequest)
                {
                    throw new InvalidOperationException("Not a WebSocket request."); // TODO: LOC
                }

                string subProtocol = null;
                if (acceptContext != null)
                {
                    subProtocol = acceptContext.SubProtocol;
                }

                TimeSpan keepAliveInterval = _options.KeepAliveInterval;
                int receiveBufferSize = _options.ReceiveBufferSize;
                var advancedAcceptContext = acceptContext as ExtendedWebSocketAcceptContext;
                if (advancedAcceptContext != null)
                {
                    if (advancedAcceptContext.ReceiveBufferSize.HasValue)
                    {
                        receiveBufferSize = advancedAcceptContext.ReceiveBufferSize.Value;
                    }
                    if (advancedAcceptContext.KeepAliveInterval.HasValue)
                    {
                        keepAliveInterval = advancedAcceptContext.KeepAliveInterval.Value;
                    }
                }

                string key = string.Join(", ", _context.Request.Headers[HeaderNames.SecWebSocketKey]);

                GenerateResponseHeaders(key, subProtocol, _context.Response.Headers);

                Stream opaqueTransport = await _upgradeFeature.UpgradeAsync(); // Sets status code to 101

                return WebSocket.CreateFromStream(opaqueTransport, isServer: true, subProtocol: subProtocol, keepAliveInterval: keepAliveInterval);
            }

            public static void GenerateResponseHeaders(string key, string subProtocol, IHeaderDictionary headers)
            {
                headers[HeaderNames.Connection] = Constants.Headers.ConnectionUpgrade;
                headers[HeaderNames.Upgrade] = Constants.Headers.UpgradeWebSocket;
                headers[HeaderNames.SecWebSocketAccept] = CreateResponseKey(key);
                if (!string.IsNullOrWhiteSpace(subProtocol))
                {
                    headers[HeaderNames.SecWebSocketProtocol] = subProtocol;
                }
            }

            public static string CreateResponseKey(string requestKey)
            {
                // "The value of this header field is constructed by concatenating /key/, defined above in step 4
                // in Section 4.2.2, with the string "258EAFA5- E914-47DA-95CA-C5AB0DC85B11", taking the SHA-1 hash of
                // this concatenated value to obtain a 20-byte value and base64-encoding"
                // https://tools.ietf.org/html/rfc6455#section-4.2.2

                if (requestKey == null)
                {
                    throw new ArgumentNullException(nameof(requestKey));
                }

                using (var algorithm = SHA1.Create())
                {
                    string merged = requestKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] mergedBytes = Encoding.UTF8.GetBytes(merged);
                    byte[] hashedBytes = algorithm.ComputeHash(mergedBytes);
                    return Convert.ToBase64String(hashedBytes);
                }
            }

            internal static class Constants
            {
                public static class Headers
                {
                    public const string UpgradeWebSocket = "websocket";
                    public const string ConnectionUpgrade = "Upgrade";
                    public const string SupportedVersion = "13";
                }
            }
        }
    }
}
