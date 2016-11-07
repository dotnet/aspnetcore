using System;
using System.IO;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.SignalR.Redis
{
    public class RedisOptions
    {
        public ConfigurationOptions Options { get; set; } = new ConfigurationOptions();

        public Func<TextWriter, ConnectionMultiplexer> Factory { get; set; }

        // TODO: Async
        internal ConnectionMultiplexer Connect(TextWriter log)
        {
            if (Factory == null)
            {
                // REVIEW: Should we do this?
                if (Options.EndPoints.Count == 0)
                {
                    Options.EndPoints.Add("localhost");
                }
                return ConnectionMultiplexer.Connect(Options, log);
            }

            return Factory(log);
        }
    }
}
