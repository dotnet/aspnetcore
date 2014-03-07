using System;
using System.IO;
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
            ReceiveBufferSize = 1024;
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

            request.Headers[Constants.Headers.WebSocketVersion] = Constants.Headers.SupportedVersion;
            // TODO: Sub-protocols

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
                throw new InvalidOperationException("Incomplete handshake");
            }

            // TODO: Sub protocol

            Stream stream = response.GetResponseStream();

            return CommonWebSocket.CreateClientWebSocket(stream, null, ReceiveBufferSize, useZeroMask: UseZeroMask);
        }
    }
}
