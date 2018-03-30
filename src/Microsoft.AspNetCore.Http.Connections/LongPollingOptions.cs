using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class LongPollingOptions
    {
        public TimeSpan PollTimeout { get; set; } = TimeSpan.FromSeconds(90);
    }
}
