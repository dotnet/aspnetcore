// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.JSInterop.Test
{
    public class JSRuntimeBaseTest
    {
        [Fact]
        public void DispatchesAsyncCallsWithDistinctAsyncHandles()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act
            runtime.InvokeAsync<object>("test identifier 1", "arg1", 123, true );
            runtime.InvokeAsync<object>("test identifier 2", "some other arg");

            // Assert
            Assert.Collection(runtime.BeginInvokeCalls,
                call =>
                {
                    Assert.Equal("test identifier 1", call.Identifier);
                    Assert.Equal("[\"arg1\",123,true]", call.ArgsJson);
                },
                call =>
                {
                    Assert.Equal("test identifier 2", call.Identifier);
                    Assert.Equal("[\"some other arg\"]", call.ArgsJson);
                    Assert.NotEqual(runtime.BeginInvokeCalls[0].AsyncHandle, call.AsyncHandle);
                });
        }

        [Fact]
        public void CanCompleteAsyncCallsAsSuccess()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act/Assert: Tasks not initially completed
            var unrelatedTask = runtime.InvokeAsync<string>("unrelated call", Array.Empty<object>());
            var task = runtime.InvokeAsync<string>("test identifier", Array.Empty<object>());
            Assert.False(unrelatedTask.IsCompleted);
            Assert.False(task.IsCompleted);

            // Act/Assert: Task can be completed
            runtime.OnEndInvoke(
                runtime.BeginInvokeCalls[1].AsyncHandle,
                /* succeeded: */ true,
                "my result");
            Assert.False(unrelatedTask.IsCompleted);
            Assert.True(task.IsCompleted);
            Assert.Equal("my result", task.Result);
        }

        [Fact]
        public void CanCompleteAsyncCallsAsFailure()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act/Assert: Tasks not initially completed
            var unrelatedTask = runtime.InvokeAsync<string>("unrelated call", Array.Empty<object>());
            var task = runtime.InvokeAsync<string>("test identifier", Array.Empty<object>());
            Assert.False(unrelatedTask.IsCompleted);
            Assert.False(task.IsCompleted);

            // Act/Assert: Task can be failed
            runtime.OnEndInvoke(
                runtime.BeginInvokeCalls[1].AsyncHandle,
                /* succeeded: */ false,
                "This is a test exception");
            Assert.False(unrelatedTask.IsCompleted);
            Assert.True(task.IsCompleted);

            Assert.IsType<AggregateException>(task.Exception);
            Assert.IsType<JSException>(task.Exception.InnerException);
            Assert.Equal("This is a test exception", ((JSException)task.Exception.InnerException).Message);
        }

        [Fact]
        public void CannotCompleteSameAsyncCallMoreThanOnce()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act/Assert
            runtime.InvokeAsync<string>("test identifier", Array.Empty<object>());
            var asyncHandle = runtime.BeginInvokeCalls[0].AsyncHandle;
            runtime.OnEndInvoke(asyncHandle, true, null);
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                // Second "end invoke" will fail
                runtime.OnEndInvoke(asyncHandle, true, null);
            });
            Assert.Equal($"There is no pending task with handle '{asyncHandle}'.", ex.Message);
        }

        [Fact]
        public void SerializesDotNetObjectWrappersInKnownFormat()
        {
            // Arrange
            var runtime = new TestJSRuntime();
            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();

            // Act
            // Showing we can pass the DotNetObject either as top-level args or nested
            var obj1Ref = new DotNetObjectRef(obj1);
            var obj1DifferentRef = new DotNetObjectRef(obj1);
            runtime.InvokeAsync<object>("test identifier",
                obj1Ref,
                new Dictionary<string, object>
                {
                    { "obj2", new DotNetObjectRef(obj2) },
                    { "obj3", new DotNetObjectRef(obj3) },
                    { "obj1SameRef", obj1Ref },
                    { "obj1DifferentRef", obj1DifferentRef },
                });

            // Assert: Serialized as expected
            var call = runtime.BeginInvokeCalls.Single();
            Assert.Equal("test identifier", call.Identifier);
            Assert.Equal("[\"__dotNetObject:1\",{\"obj2\":\"__dotNetObject:2\",\"obj3\":\"__dotNetObject:3\",\"obj1SameRef\":\"__dotNetObject:1\",\"obj1DifferentRef\":\"__dotNetObject:4\"}]", call.ArgsJson);

            // Assert: Objects were tracked
            Assert.Same(obj1, runtime.ArgSerializerStrategy.FindDotNetObject(1));
            Assert.Same(obj2, runtime.ArgSerializerStrategy.FindDotNetObject(2));
            Assert.Same(obj3, runtime.ArgSerializerStrategy.FindDotNetObject(3));
            Assert.Same(obj1, runtime.ArgSerializerStrategy.FindDotNetObject(4));
        }

        [Fact]
        public void SupportsCustomSerializationForArguments()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Arrange/Act
            runtime.InvokeAsync<object>("test identifier",
                new WithCustomArgSerializer());

            // Asssert
            var call = runtime.BeginInvokeCalls.Single();
            Assert.Equal("[{\"key1\":\"value1\",\"key2\":123}]", call.ArgsJson);
        }

        class TestJSRuntime : JSRuntimeBase
        {
            public List<BeginInvokeAsyncArgs> BeginInvokeCalls = new List<BeginInvokeAsyncArgs>();

            public class BeginInvokeAsyncArgs
            {
                public long AsyncHandle { get; set; }
                public string Identifier { get; set; }
                public string ArgsJson { get; set; }
            }

            protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
            {
                BeginInvokeCalls.Add(new BeginInvokeAsyncArgs
                {
                    AsyncHandle = asyncHandle,
                    Identifier = identifier,
                    ArgsJson = argsJson,
                });
            }

            public void OnEndInvoke(long asyncHandle, bool succeeded, object resultOrException)
                => EndInvokeJS(asyncHandle, succeeded, resultOrException);
        }

        class WithCustomArgSerializer : ICustomArgSerializer
        {
            public object ToJsonPrimitive()
            {
                return new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 },
                };
            }
        }
    }
}
