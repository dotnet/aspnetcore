using Microsoft.AspNet.Server.Kestrel;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class TestServiceContext : ServiceContext
    {
        public TestServiceContext()
        {
            AppLifetime = new LifetimeNotImplemented();
            Log = new TestKestrelTrace();
            DateHeaderValueManager = new TestDateHeaderValueManager();
        }
    }
}
