// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.JSInterop.Tests
{
    public class DotNetObjectRefTest
    {
        [Fact]
        public void CanAccessValue()
        {
            var obj = new object();
            Assert.Same(obj, new DotNetObjectRef(obj).Value);
        }

        [Fact]
        public void CanAssociateWithSameRuntimeMultipleTimes()
        {
            var objRef = new DotNetObjectRef(new object());
            var jsRuntime = new TestJsRuntime();
            objRef.EnsureAttachedToJsRuntime(jsRuntime);
            objRef.EnsureAttachedToJsRuntime(jsRuntime);
        }

        [Fact]
        public void CannotAssociateWithDifferentRuntimes()
        {
            var objRef = new DotNetObjectRef(new object());
            var jsRuntime1 = new TestJsRuntime();
            var jsRuntime2 = new TestJsRuntime();
            objRef.EnsureAttachedToJsRuntime(jsRuntime1);

            var ex = Assert.Throws<InvalidOperationException>(
                () => objRef.EnsureAttachedToJsRuntime(jsRuntime2));
            Assert.Contains("Do not attempt to re-use", ex.Message);
        }

        [Fact]
        public void NotifiesAssociatedJsRuntimeOfDisposal()
        {
            // Arrange
            var objRef = new DotNetObjectRef(new object());
            var jsRuntime = new TestJsRuntime();
            objRef.EnsureAttachedToJsRuntime(jsRuntime);

            // Act
            objRef.Dispose();

            // Assert
            Assert.Equal(new[] { objRef }, jsRuntime.UntrackedRefs);
        }

        class TestJsRuntime : IJSRuntime
        {
            public List<DotNetObjectRef> UntrackedRefs = new List<DotNetObjectRef>();

            public Task<T> InvokeAsync<T>(string identifier, params object[] args)
                => throw new NotImplementedException();

            public void UntrackObjectRef(DotNetObjectRef dotNetObjectRef)
                => UntrackedRefs.Add(dotNetObjectRef);
        }
    }
}
