// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Microsoft.JSInterop;

public class JSInProcessRuntimeExtensionsTest
{
    [Fact]
    public void InvokeVoid_Works()
    {
        // Arrange
        var method = "someMethod";
        var args = new[] { "a", "b" };
        var jsRuntime = new Mock<IJSInProcessRuntime>(MockBehavior.Strict);
        jsRuntime.Setup(s => s.Invoke<IJSVoidResult>(method, args)).Returns(Mock.Of<IJSVoidResult>());

        // Act
        jsRuntime.Object.InvokeVoid(method, args);

        jsRuntime.Verify();
    }
}
