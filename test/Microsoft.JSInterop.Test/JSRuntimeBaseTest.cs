// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    }
}
