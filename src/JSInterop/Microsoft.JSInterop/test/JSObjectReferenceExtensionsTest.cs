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
    public void AsFunction_ReturnsFunc_ThatInvokesInterop()
    {
        // Arrange
        var jsRuntime = new RecordingTestJSRuntime();
        var jsObjectReference = new JSObjectReference(jsRuntime, 1);

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(42));
        var reader = new Utf8JsonReader(bytes);

        // Act
        var func = jsObjectReference.AsFunction<int, int>();
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

    class RecordingTestJSRuntime : TestJSRuntime
    {
        public List<JSInvocationInfo> InvokeCalls { get; set; } = [];

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            InvokeCalls.Add(invocationInfo);
        }
    }
}
