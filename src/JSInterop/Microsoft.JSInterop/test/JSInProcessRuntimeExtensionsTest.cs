// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.JSInterop
{
    public class JSInProcessRuntimeExtensionsTest
    {
        [Fact]
        public void InvokeVoid_Works()
        {
            // Arrange
            var method = "someMethod";
            var args = new[] { "a", "b" };
            var jsRuntime = new Mock<IJSInProcessRuntime>(MockBehavior.Strict);
            jsRuntime.Setup(s => s.Invoke<object>(method, args)).Returns(new ValueTask<object>(new object()));

            // Act
            jsRuntime.Object.InvokeVoid(method, args);

            jsRuntime.Verify();
        }
    }
}
