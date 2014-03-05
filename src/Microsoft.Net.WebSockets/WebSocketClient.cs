using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
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

        public async Task<WebSocket> ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            request.Headers[Constants.Headers.WebSocketVersion] = Constants.Headers.SupportedVersion;
            // TODO: Sub-protocols

            WebResponse response = await request.GetResponseAsync();
            // TODO: Validate handshake

            Stream stream = response.GetResponseStream();
            // Console.WriteLine(stream.CanWrite + " " + stream.CanRead);

            return new ClientWebSocket(stream, null, ReceiveBufferSize);
        }
    }
}
