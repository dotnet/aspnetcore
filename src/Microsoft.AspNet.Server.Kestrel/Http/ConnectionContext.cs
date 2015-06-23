namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class ConnectionContext : ListenerContext
    {
        public ConnectionContext()
        {
        }

        public ConnectionContext(ListenerContext context) : base(context)
        {
        }

        public ConnectionContext(ConnectionContext context) : base(context)
        {
            SocketInput = context.SocketInput;
            SocketOutput = context.SocketOutput;
            ConnectionControl = context.ConnectionControl;
        }

        public SocketInput SocketInput { get; set; }
        public ISocketOutput SocketOutput { get; set; }

        public IConnectionControl ConnectionControl { get; set; }
    }
}