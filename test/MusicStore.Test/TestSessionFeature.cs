using System;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Session;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Logging.Testing;

namespace MusicStore.Controllers
{
    public class TestSessionFeature : ISessionFeature
    {
        public ISession Session
        {
            get
            {
                return new DistributedSession(
                    new LocalCache(new MemoryCache(new MemoryCacheOptions())),
                    "sessionId_A",
                    idleTimeout: TimeSpan.MaxValue,
                    tryEstablishSession: () => true,
                    loggerFactory: new NullLoggerFactory(),
                    isNewSessionKey: true);
            }
        }
    }
}
