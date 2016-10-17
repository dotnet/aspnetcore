namespace Microsoft.Extensions.WebSockets.Internal
{
    public enum WebSocketConnectionState
    {
        Created,
        Connected,
        CloseSent,
        CloseReceived,
        Closed
    }
}
