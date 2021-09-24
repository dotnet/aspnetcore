// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
