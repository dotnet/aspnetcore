// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

public class JSInProcessRuntimeBaseTest
{
    [Fact]
    public void DispatchesSyncCallsAndDeserializesResults()
    {
        // Arrange
        var runtime = new TestJSInProcessRuntime
        {
            NextResultJson = "{\"intValue\":123,\"stringValue\":\"Hello\"}"
        };

        // Act
        var syncResult = runtime.Invoke<TestDTO>("test identifier 1", "arg1", 123, true)!;
        var call = runtime.InvokeCalls.Single();

        // Assert
        Assert.Equal(123, syncResult.IntValue);
        Assert.Equal("Hello", syncResult.StringValue);
        Assert.Equal("test identifier 1", call.Identifier);
        Assert.Equal("[\"arg1\",123,true]", call.ArgsJson);
    }

    [Fact]
    public void SerializesDotNetObjectWrappersInKnownFormat()
    {
        // Arrange
        var runtime = new TestJSInProcessRuntime { NextResultJson = null };
        var obj1 = new object();
        var obj2 = new object();
        var obj3 = new object();

        // Act
        // Showing we can pass the DotNetObject either as top-level args or nested
        var syncResult = runtime.Invoke<DotNetObjectReference<object>>("test identifier",
            DotNetObjectReference.Create(obj1),
            new Dictionary<string, object>
            {
                    { "obj2",  DotNetObjectReference.Create(obj2) },
                    { "obj3",  DotNetObjectReference.Create(obj3) },
            });

        // Assert: Handles null result string
        Assert.Null(syncResult);

        // Assert: Serialized as expected
        var call = runtime.InvokeCalls.Single();
        Assert.Equal("test identifier", call.Identifier);
        Assert.Equal("[{\"__dotNetObject\":1},{\"obj2\":{\"__dotNetObject\":2},\"obj3\":{\"__dotNetObject\":3}}]", call.ArgsJson);

        // Assert: Objects were tracked
        Assert.Same(obj1, runtime.GetObjectReference(1).Value);
        Assert.Same(obj2, runtime.GetObjectReference(2).Value);
        Assert.Same(obj3, runtime.GetObjectReference(3).Value);
    }

    [Fact]
    public void SyncCallResultCanIncludeDotNetObjects()
    {
        // Arrange
        var runtime = new TestJSInProcessRuntime
        {
            NextResultJson = "[{\"__dotNetObject\":2},{\"__dotNetObject\":1}]"
        };
        var obj1 = new object();
        var obj2 = new object();

        // Act
        var syncResult = runtime.Invoke<DotNetObjectReference<object>[]>(
            "test identifier",
            DotNetObjectReference.Create(obj1),
            "some other arg",
            DotNetObjectReference.Create(obj2))!;
        var call = runtime.InvokeCalls.Single();

        // Assert
        Assert.Equal(new[] { obj2, obj1 }, syncResult.Select(r => r.Value));
    }

    class TestDTO
    {
        public int IntValue { get; set; }
        public string? StringValue { get; set; }
    }

    class TestJSInProcessRuntime : JSInProcessRuntime
    {
        public List<InvokeArgs> InvokeCalls { get; set; } = new List<InvokeArgs>();

        public string? NextResultJson { get; set; }

        protected override string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            InvokeCalls.Add(new InvokeArgs { Identifier = identifier, ArgsJson = argsJson });
            return NextResultJson;
        }

        public class InvokeArgs
        {
            public string? Identifier { get; set; }
            public string? ArgsJson { get; set; }
        }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotImplementedException("This test only covers sync calls");

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => throw new NotImplementedException("This test only covers sync calls");
    }
}
