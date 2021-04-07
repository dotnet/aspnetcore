// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering
{
    public class RenderRegistryTest
    {
        [Fact]
        public void RendererRegistry_Find_ThrowsErrorOnNonWASM()
        {
            // Act
            Exception ex = Assert.Throws<ArgumentException>(() => RendererRegistry.Find(123));

            // Assert
            Assert.Equal("There is no renderer with ID 123.", ex.Message);
        }
        [Fact]
        public void RendererRegistry_Remove_DoesNothingOnNonWASM()
        {
            // Act
            var result = RendererRegistry.TryRemove(123);

            // Assert
            Assert.False(result);
        }
    }
}
