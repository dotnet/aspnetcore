// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.JSInterop.TestJSRuntime;

namespace Microsoft.JSInterop
{
    public class DotNetObjectRefTest
    {
        [Fact]
        public Task CanAccessValue() => WithJSRuntime(_ =>
        {
            var obj = new object();
            Assert.Same(obj, DotNetObjectRef.Create(obj).Value);
        });

        [Fact]
        public Task NotifiesAssociatedJsRuntimeOfDisposal() => WithJSRuntime(jsRuntime =>
        {
            // Arrange
            var objRef = DotNetObjectRef.Create(new object());

            // Act
            objRef.Dispose();

            // Assert
            var ex = Assert.Throws<ArgumentException>(() => jsRuntime.ObjectRefManager.FindDotNetObject(objRef.ObjectId));
            Assert.StartsWith("There is no tracked object with id '1'.", ex.Message);
        });
            }

            protected internal override void EndInvokeDotNet(string callId, bool success, object resultOrError, string assemblyName, string methodIdentifier, long dotNetObjectId)
            {
                throw new NotImplementedException();
            }
        }

        async Task WithJSRuntime(Action<JSRuntimeBase> testCode)
        {
            // Since the tests rely on the asynclocal JSRuntime.Current, ensure we
            // are on a distinct async context with a non-null JSRuntime.Current
            await Task.Yield();

            var runtime = new TestJSRuntime();
            JSRuntime.SetCurrentJSRuntime(runtime);
            testCode(runtime);
        }
    }
}
