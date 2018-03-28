using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HubConnectionTests
    {
        private static HubConnection CreateHubConnection(TestConnection connection, IHubProtocol protocol = null)
        {
            return new HubConnection(() => connection, protocol ?? new JsonHubProtocol(), new LoggerFactory());
        }
    }
}
