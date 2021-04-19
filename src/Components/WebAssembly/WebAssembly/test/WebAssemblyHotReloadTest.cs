// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.HotReload
{
    public class WebAssemblyHotReloadTest
    {
        [Fact]
        public void WebAssemblyHotReload_DiscoversMetadataHandlers_FromComponentsAssembly()
        {
            // Arrange
            var assemblies = new[] { typeof(Renderer).Assembly, };

            // Act
            var (beforeUpdate, afterUpdate) = WebAssemblyHotReload.GetMetadataUpdateHandlerActions(assemblies);

            // Assert
            Assert.Empty(beforeUpdate);
            Assert.Single(afterUpdate);
        }
    }
}
