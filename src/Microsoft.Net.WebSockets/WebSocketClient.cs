using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Net.WebSockets.Client
{
    public class WebSocketClient
    {
        static WebSocketClient()
        {
            try
            {
                // Only call once
                WebSocket.RegisterPrefixes();
            }
            catch (Exception)
            {
                // Already registered
            }
        }

        public WebSocketClient()
        {
            ReceiveBufferSize = 1024 * 16;
            KeepAliveInterval = TimeSpan.FromMinutes(2);
            SubProtocols = new List<string>();
        }

        public IList<string> SubProtocols
        {
            get;
            private set;
        }

        public TimeSpan KeepAliveInterval
        {
            get;
            set;
        }

        public int ReceiveBufferSize
        {
            get;
            set;
        }

        public bool UseZeroMask
        {
            get;
            set;
        }

        public Action<HttpWebRequest> ConfigureRequest
        {
            get;
            set;
        }

        public Action<HttpWebResponse> InspectResponse
        {
            get;
            set;
        }

        public async Task<WebSocket> ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            CancellationTokenRegistration cancellation = cancellationToken.Register(() => request.Abort());

            request.Headers[Constants.Headers.SecWebSocketVersion] = Constants.Headers.SupportedVersion;
            if (SubProtocols.Count > 0)
            {
                request.Headers[Constants.Headers.SecWebSocketProtocol] = string.Join(", ", SubProtocols);
            }

            if (ConfigureRequest != null)
            {
                ConfigureRequest(request);
            }

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

            cancellation.Dispose();

            if (InspectResponse != null)
            {
                InspectResponse(response);
            }

            // TODO: Validate handshake
            if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                response.Dispose();
                throw new InvalidOperationException("Incomplete handshake, invalid status code: " + response.StatusCode);
            }
            // TODO: Validate Sec-WebSocket-Key/Sec-WebSocket-Accept

            string subProtocol = response.Headers[Constants.Headers.SecWebSocketProtocol];
            if (!string.IsNullOrEmpty(subProtocol) && !SubProtocols.Contains(subProtocol, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Incomplete handshake, the server specified an unknown sub-protocol: " + subProtocol);
            }

            Stream stream = response.GetResponseStream();

            return CommonWebSocket.CreateClientWebSocket(stream, subProtocol, KeepAliveInterval, ReceiveBufferSize, useZeroMask: UseZeroMask);
        }
    }
}
