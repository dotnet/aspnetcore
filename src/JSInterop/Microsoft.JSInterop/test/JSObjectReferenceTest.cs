// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop.Implementation;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop.Tests;

public class JSObjectReferenceTest
{
    [Fact]
    public void JSObjectReference_InvokeAsync_CallsUnderlyingJSRuntimeInvokeAsync()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var jsObject = new JSObjectReference(jsRuntime, 0);

        // Act
        _ = jsObject.InvokeAsync<object>("test", "arg1", "arg2");

        // Assert
        Assert.Equal(1, jsRuntime.BeginInvokeJSInvocationCount);
    }

    [Fact]
    public void JSInProcessObjectReference_Invoke_CallsUnderlyingJSRuntimeInvoke()
    {
        // Arrange
        var jsRuntime = new TestJSInProcessRuntime();
        var jsObject = new JSInProcessObjectReference(jsRuntime, 0);

        // Act
        jsObject.Invoke<object>("test", "arg1", "arg2");

        // Assert
        Assert.Equal(1, jsRuntime.InvokeJSInvocationCount);
    }

    [Fact]
    public async Task JSObjectReference_Dispose_DisallowsFurtherInteropCalls()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var jsObject = new JSObjectReference(jsRuntime, 0);

        // Act
        _ = jsObject.DisposeAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await jsObject.InvokeAsync<object>("test", "arg1", "arg2"));
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await jsObject.InvokeAsync<object>("test", CancellationToken.None, "arg1", "arg2"));
    }

    [Fact]
    public void JSInProcessObjectReference_Dispose_DisallowsFurtherInteropCalls()
    {
        // Arrange
        var jsRuntime = new TestJSInProcessRuntime();
        var jsObject = new JSInProcessObjectReference(jsRuntime, 0);

        // Act
        _ = jsObject.DisposeAsync();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => jsObject.Invoke<object>("test", "arg1", "arg2"));
    }

    class TestJSRuntime : JSRuntime
    {
        public int BeginInvokeJSInvocationCount { get; private set; }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            BeginInvokeJSInvocationCount++;
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
        }
    }

    class TestJSInProcessRuntime : JSInProcessRuntime
    {
        public int InvokeJSInvocationCount { get; private set; }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
        }

        protected override string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            InvokeJSInvocationCount++;

            return null;
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
        }
    }
}
