using Microsoft.Framework.Logging;

namespace MusicStore.Logging
{
    public class NullLoggerFactory : ILoggerFactory
    {
        public ILogger Create(string name)
        {
            return new NullLogger();
        }
    }
}