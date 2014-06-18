using Microsoft.AspNet.Server.Kestrel;
using Microsoft.AspNet.Server.Kestrel.Http;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.KestralTests
{
    /// <summary>
    /// Summary description for TestServer
    /// </summary>
    public class TestServer : IDisposable
    {
        private KestrelEngine _engine;
        private IDisposable _server;

        public TestServer(Func<Frame, Task> app)
        {
            Create(app);
        }

        public void Create(Func<Frame, Task> app)
        {
            _engine = new KestrelEngine();
            _engine.Start(1);
            _server = _engine.CreateServer(
                "http",
                "localhost",
                54321,
                app);

        }

        public void Dispose()
        {
            _server.Dispose();
            _engine.Dispose();
        }
    }
}