using System;
using Microsoft.AspNetCore.Connections.Features;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks.Shared
{
    public class TestConnectionInherentKeepAliveFeature : IConnectionInherentKeepAliveFeature
    {
        public TimeSpan KeepAliveInterval { get; } = TimeSpan.Zero;
    }
}