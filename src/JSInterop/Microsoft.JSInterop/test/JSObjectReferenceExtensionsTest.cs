// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop.Implementation;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop.Tests;

public class JSObjectReferenceExtensionsTest
{
    [Fact]
    public void AsAsyncFunction_WithVoidValueTaskFunc_ReturnsFunc()
    {
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        // Act
        var func = jsObjectReference.AsAsyncFunction<Func<int, ValueTask>>();

        // Assert
        Assert.NotNull(func);
        Assert.IsType<Func<int, ValueTask>>(func);
    }

    [Fact]
    public void AsAsyncFunction_WithVoidTaskFunc_ReturnsFunc()
    {
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        // Act
        var func = jsObjectReference.AsAsyncFunction<Func<int, Task>>();

        // Assert
        Assert.NotNull(func);
        Assert.IsType<Func<int, Task>>(func);
    }

    [Fact]
    public void AsAsyncFunction_WithValueTaskFunc_ReturnsFunc()
    {
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        // Act
        var func = jsObjectReference.AsAsyncFunction<Func<int, ValueTask<int>>>();

        // Assert
        Assert.NotNull(func);
        Assert.IsType<Func<int, ValueTask<int>>>(func);
    }

    [Fact]
    public void AsAsyncFunction_WithTaskFunc_ReturnsFunc()
    {
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        // Act
        var func = jsObjectReference.AsAsyncFunction<Func<int, Task<int>>>();

        // Assert
        Assert.NotNull(func);
        Assert.IsType<Func<int, Task<int>>>(func);
    }

    [Fact]
    public void AsAsyncFunction_WithValueTaskFunc_ReturnsFunc_ThatInvokesInterop()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(42));
        var reader = new Utf8JsonReader(bytes);

        // Act
        var func = jsObjectReference.AsAsyncFunction<Func<int, ValueTask<int>>>();
        ValueTask<int> task = func(1);

        jsRuntime.EndInvokeJS(
            jsRuntime.InvokeCalls[0].AsyncHandle,
            /* succeeded: */ true,
            ref reader);

        // Assert
        Assert.True(task.IsCompleted);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.Equal(42, task.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
    }

    [Fact]
    public void AsAsyncFunction_WithTaskFunc_ReturnsFunc_ThatInvokesInterop()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(42));
        var reader = new Utf8JsonReader(bytes);

        // Act
        var func = jsObjectReference.AsAsyncFunction<Func<int, Task<int>>>();
        Task<int> task = func(1);

        jsRuntime.EndInvokeJS(
            jsRuntime.InvokeCalls[0].AsyncHandle,
            /* succeeded: */ true,
            ref reader);

        // Assert
        Assert.True(task.IsCompleted);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.Equal(42, task.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
    }

    [Fact]
    public void AsAsyncFunction_WithEventHandlerDelegate_Throws()
    {
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        // Act/Assert
        Assert.Throws<InvalidOperationException>(jsObjectReference.AsAsyncFunction<EventHandler>);
    }

    [Fact]
    public void AsAsyncFunction_WithActionDelegate_Throws()
    {
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        // Act/Assert
        Assert.Throws<InvalidOperationException>(jsObjectReference.AsAsyncFunction<Action<int>>);
    }

    [Fact]
    public void AsAsyncFunction_WithFuncWithInvalidReturnType_Throws()
    {
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        // Act/Assert
        Assert.Throws<InvalidOperationException>(jsObjectReference.AsAsyncFunction<Func<int>>);
    }

    [Fact]
    public void AsAsyncFunction_WithFuncWithTooManyParams_Throws()
    {
        var jsRuntime = new TestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        // Act/Assert
        Assert.Throws<InvalidOperationException>(jsObjectReference.AsAsyncFunction<Func<int, int, int, int, int, int, int, int, int, Task>>);
    }

    class TestJSRuntime : JSInProcessRuntime
    {
        public List<JSInvocationInfo> InvokeCalls { get; set; } = [];

        public string? NextResultJson { get; set; }

        protected override string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            throw new NotImplementedException();
        }

        protected override string? InvokeJS(in JSInvocationInfo invocationInfo)
        {
            InvokeCalls.Add(invocationInfo);
            return NextResultJson;
        }

        protected override void BeginInvokeJS(long taskId, string identifier, [StringSyntax("Json")] string? argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotImplementedException("This test only covers sync calls");

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            InvokeCalls.Add(invocationInfo);
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => throw new NotImplementedException("This test only covers sync calls");
    }
}
