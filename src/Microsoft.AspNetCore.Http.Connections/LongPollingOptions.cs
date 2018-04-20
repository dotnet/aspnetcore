using System;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class LongPollingOptions
    {
        public TimeSpan PollTimeout { get; set; } = TimeSpan.FromSeconds(90);
    }
}
