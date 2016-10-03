using System.Collections.Generic;

namespace Microsoft.Extensions.WebSockets.Tests
{
    public class WebSocketConnectionSummary
    {
        public IList<WebSocketFrame> Received { get; }
        public WebSocketCloseResult CloseResult { get; }

        public WebSocketConnectionSummary(IList<WebSocketFrame> received, WebSocketCloseResult closeResult)
        {
            Received = received;
            CloseResult = closeResult;
        }
    }
}