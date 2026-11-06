// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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

    [Fact]
    public async Task JSObjectReference_DisposeAsync_IgnoresJSDisconnectedException()
    {
        // Arrange
        var jsRuntime = new TestJSRuntimeThatThrowsJSDisconnectedException();
        var jsObject = new JSObjectReference(jsRuntime, 0);

        // Act & Assert - Should not throw
        await jsObject.DisposeAsync();

        // Verify dispose was attempted
        Assert.Equal(1, jsRuntime.BeginInvokeJSInvocationCount);
    }

    [Fact]
    public async Task JSObjectReference_DisposeAsync_IgnoresJSDisconnectedException_OnMultipleCalls()
    {
        // Arrange
        var jsRuntime = new TestJSRuntimeThatThrowsJSDisconnectedException();
        var jsObject = new JSObjectReference(jsRuntime, 0);

        // Act & Assert - Should not throw on first call
        await jsObject.DisposeAsync();

        // Act & Assert - Should not throw on second call (no-op)
        await jsObject.DisposeAsync();

        // Verify dispose was only attempted once
        Assert.Equal(1, jsRuntime.BeginInvokeJSInvocationCount);
    }

    class TestJSRuntime : JSRuntime
    {
        public int BeginInvokeJSInvocationCount { get; private set; }

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            BeginInvokeJSInvocationCount++;
        }

        protected override void BeginInvokeJS(long taskId, string identifier, [StringSyntax("Json")] string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            throw new NotImplementedException();
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
        }
    }

    class TestJSRuntimeThatThrowsJSDisconnectedException : JSRuntime
    {
        public int BeginInvokeJSInvocationCount { get; private set; }

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            BeginInvokeJSInvocationCount++;
            throw new JSDisconnectedException("JavaScript interop calls cannot be issued at this time. This is because the circuit has disconnected and is being disposed.");
        }

        protected override void BeginInvokeJS(long taskId, string identifier, [StringSyntax("Json")] string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            throw new NotImplementedException();
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
        }
    }

    class TestJSInProcessRuntime : JSInProcessRuntime
    {
        public int InvokeJSInvocationCount { get; private set; }

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
        }

        protected override void BeginInvokeJS(long taskId, string identifier, [StringSyntax("Json")] string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            throw new NotImplementedException();
        }

        protected override string? InvokeJS(in JSInvocationInfo invocationInfo)
        {
            InvokeJSInvocationCount++;

            return null;
        }

        protected override string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            throw new NotImplementedException();
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
        }
    }
}
