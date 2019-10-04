using System;
using System.Collections.Generic;

namespace Bedrock.Framework
{
    public class ServerBuilder
    {
        public ServerBuilder(IServiceProvider serviceProvider)
        {
            ApplicationServices = serviceProvider;
        }

        public IList<ServerBinding> Bindings { get; } = new List<ServerBinding>();

        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan HeartBeatInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IServiceProvider ApplicationServices { get; }

        public Server Build()
        {
            return new Server(this);
        }
    }
}
