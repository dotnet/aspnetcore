using Microsoft.AspNet.Server.Kestrel;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            AppLifetime = new ShutdownNotImplemented();
            Log = new TestKestrelTrace();
            DateHeaderValueManager = new TestDateHeaderValueManager();
        }
    }
}
