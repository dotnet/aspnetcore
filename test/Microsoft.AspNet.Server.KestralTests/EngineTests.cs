using Microsoft.AspNet.Server.Kestrel;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.KestralTests
{
    /// <summary>
    /// Summary description for EngineTests
    /// </summary>
    public class EngineTests
    {
	    [Fact]
        public async Task EngineCanStartAndStop()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            engine.Stop();
        }
        [Fact]
        public async Task ListenerCanCreateAndDispose()
        {
            var engine = new KestrelEngine();
            engine.Start(1);
            var started = engine.CreateServer();
            started.Dispose();
            engine.Stop();
        }
    }
}