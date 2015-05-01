// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ScopedInstanceTest
    {
        [Fact]
        public void ScopedInstanceDisposesIDisposables()
        {
            var disposable = new Disposable();
			
            // Arrange
            var scopedInstance = new ScopedInstance<Disposable>
            {
                Value = disposable,
            };
			
            // Act
            scopedInstance.Dispose();
			
            // Assert
            Assert.True(disposable.IsDisposed);
        }
		
        [Fact]
        public void ScopedInstanceDoesNotThrowOnNonIDisposable()
        {
            // Arrange
            var scopedInstance = new ScopedInstance<object>()
            {
                Value = new object(),
            };
			
            // Act
            scopedInstance.Dispose();
        }
		
        [Fact]
        public void ScopedInstanceDoesNotThrowOnNull()
        {
            // Arrange
            var scopedInstance = new ScopedInstance<Disposable>()
            {
                Value = null, // just making it explicit that there is not value set yet.
            };		

            // Act
            scopedInstance.Dispose();
			
            // Assert
            Assert.Null(scopedInstance.Value);
        }
		
        private class Disposable : IDisposable
        {
            public bool IsDisposed { get; set; }
			
            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}