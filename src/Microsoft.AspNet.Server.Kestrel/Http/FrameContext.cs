namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class FrameContext : ConnectionContext
    {
        public FrameContext()
        {

        }

        public FrameContext(ConnectionContext context) : base(context)
        {

        }

        public IFrameControl FrameControl { get; set; }
    }
}