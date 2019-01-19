// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.ObjectPool
{
    public class DisposableObjectPoolTest
    {
        [Fact]
        public void DisposableObjectPoolWithOneElement_Dispose_ObjectDisposed()
        {
            // Arrange
            var pool = new DisposableObjectPool<DisposableObject>(new DefaultPooledObjectPolicy<DisposableObject>());
            var obj = pool.Get();
            pool.Return(obj);

            // Act
            pool.Dispose();

            // Assert
            Assert.True(obj.IsDisposed);
        }

        [Fact]
        public void DisposableObjectPoolWithTwoElements_Dispose_ObjectsDisposed()
        {
            // Arrange
            var pool = new DisposableObjectPool<DisposableObject>(new DefaultPooledObjectPolicy<DisposableObject>());
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            pool.Return(obj1);
            pool.Return(obj2);

            // Act
            pool.Dispose();

            // Assert
            Assert.True(obj1.IsDisposed);
            Assert.True(obj2.IsDisposed);
        }

        [Fact]
        public void DisposableObjectPool_DisposeAndGet_ThrowsObjectDisposed()
        {
            // Arrange
            var pool = new DisposableObjectPool<DisposableObject>(new DefaultPooledObjectPolicy<DisposableObject>());
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            pool.Return(obj1);
            pool.Return(obj2);

            // Act
            pool.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
        }

        [Fact]
        public void DisposableObjectPool_DisposeAndReturn_DisposesObject()
        {
            // Arrange
            var pool = new DisposableObjectPool<DisposableObject>(new DefaultPooledObjectPolicy<DisposableObject>());
            var obj = pool.Get();

            // Act
            pool.Dispose();
            pool.Return(obj);

            // Assert
            Assert.True(obj.IsDisposed);
        }

        private class DisposableObject : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose() => IsDisposed = true;
        }
    }
}
