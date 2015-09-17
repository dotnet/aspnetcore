using Microsoft.AspNet.Server.Kestrel;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            AppShutdown = new ShutdownNotImplemented();
            Log = new TestKestrelTrace();
        }
    }
}
