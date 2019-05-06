// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.ObjectPool
{
    public class DefaultObjectPoolProviderTest
    {
        [Fact]
        public void DefaultObjectPoolProvider_CreateForObject_DefaultObjectPoolReturned()
        {
            // Arrange
            var provider = new DefaultObjectPoolProvider();

            // Act
            var pool = provider.Create<object>();

            // Assert
            Assert.IsType<DefaultObjectPool<object>>(pool);
        }

        [Fact]
        public void DefaultObjectPoolProvider_CreateForIDisposable_DisposableObjectPoolReturned()
        {
            // Arrange
            var provider = new DefaultObjectPoolProvider();

            // Act
            var pool = provider.Create<DisposableObject>();

            // Assert
            Assert.IsType<DisposableObjectPool<DisposableObject>>(pool);
        }

        private class DisposableObject : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose() => IsDisposed = true;
        }
    }
}
