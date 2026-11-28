// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

public class DefaultWebSocketManagerTests
{
    [Fact]
    public async Task AcceptWithoutSubProtocol_UsesIndependentContexts()
    {
        var context = new DefaultHttpContext();
        var feature = new MutatingWebSocketFeature();
        context.Features.Set<IHttpWebSocketFeature>(feature);

        await context.WebSockets.AcceptWebSocketAsync();
        await context.WebSockets.AcceptWebSocketAsync();

        Assert.Equal(2, feature.Contexts.Count);
        Assert.NotSame(feature.Contexts[0], feature.Contexts[1]);
    }

    private sealed class MutatingWebSocketFeature : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => true;
        public List<WebSocketAcceptContext> Contexts { get; } = new();

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context)
        {
            Assert.False(context.DangerousEnableCompression);
            context.DangerousEnableCompression = true;
            Contexts.Add(context);
            return Task.FromResult<WebSocket>(null!);
        }
    }
}
