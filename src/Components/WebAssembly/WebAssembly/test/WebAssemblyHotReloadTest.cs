// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.HotReload;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.HotReload
{
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
}
