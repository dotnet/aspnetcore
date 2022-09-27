// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.HotReload;

namespace Microsoft.AspNetCore.Components.WebAssembly.HotReload;

public class WebAssemblyHotReloadTest
{
    [Fact]
    public void WebAssemblyHotReload_DiscoversMetadataHandlers_FromHot()
    {
        // Arrange
        var hotReloadManager = typeof(Renderer).Assembly.GetType("Microsoft.AspNetCore.Components.HotReload.HotReloadManager");
        Assert.NotNull(hotReloadManager);

        var handlerActions = new HotReloadAgent.UpdateHandlerActions();
        var logs = new List<string>();
        var hotReloadAgent = new HotReloadAgent(logs.Add);

        // Act
        hotReloadAgent.GetHandlerActions(handlerActions, hotReloadManager);

        // Assert
        Assert.Empty(handlerActions.ClearCache);
        Assert.Single(handlerActions.UpdateApplication);
    }
}
