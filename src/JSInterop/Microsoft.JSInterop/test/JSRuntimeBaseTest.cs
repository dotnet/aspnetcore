// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop.Internal;
using Xunit;

namespace Microsoft.JSInterop.Tests
{
    public class JSRuntimeBaseTest
    {
        [Fact]
        public void DispatchesAsyncCallsWithDistinctAsyncHandles()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act
            runtime.InvokeAsync<object>("test identifier 1", "arg1", 123, true);
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
            using var jsonDocument = JsonDocument.Parse("\"my result\"");

            // Act/Assert: Task can be completed
            runtime.OnEndInvoke(
                runtime.BeginInvokeCalls[1].AsyncHandle,
                /* succeeded: */ true,
                new JSAsyncCallResult(jsonDocument, jsonDocument.RootElement));
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
            using var jsonDocument = JsonDocument.Parse("\"This is a test exception\"");

            // Act/Assert: Task can be failed
            runtime.OnEndInvoke(
                runtime.BeginInvokeCalls[1].AsyncHandle,
                /* succeeded: */ false,
                new JSAsyncCallResult(jsonDocument, jsonDocument.RootElement));
            Assert.False(unrelatedTask.IsCompleted);
            Assert.True(task.IsCompleted);

            Assert.IsType<AggregateException>(task.Exception);
            Assert.IsType<JSException>(task.Exception.InnerException);
            Assert.Equal("This is a test exception", ((JSException)task.Exception.InnerException).Message);
        }

        [Fact]
        public async Task CanCompleteAsyncCallsWithErrorsDuringDeserialization()
        {
            // Arrange
            var runtime = new TestJSRuntime();

            // Act/Assert: Tasks not initially completed
            var unrelatedTask = runtime.InvokeAsync<string>("unrelated call", Array.Empty<object>());
            var task = runtime.InvokeAsync<int>("test identifier", Array.Empty<object>());
            Assert.False(unrelatedTask.IsCompleted);
            Assert.False(task.IsCompleted);
            using var jsonDocument = JsonDocument.Parse("\"Not a string\"");

            // Act/Assert: Task can be failed
            runtime.OnEndInvoke(
                runtime.BeginInvokeCalls[1].AsyncHandle,
                /* succeeded: */ true,
                new JSAsyncCallResult(jsonDocument, jsonDocument.RootElement));
            Assert.False(unrelatedTask.IsCompleted);

            var jsException = await Assert.ThrowsAsync<JSException>(() => task);
            Assert.IsType<JsonException>(jsException.InnerException);

            // Verify we've disposed the JsonDocument.
            Assert.Throws<ObjectDisposedException>(() => jsonDocument.RootElement.ValueKind);
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
            JSRuntime.SetCurrentJSRuntime(runtime);
            var obj1 = new object();
            var obj2 = new object();
            var obj3 = new object();

            // Act
            // Showing we can pass the DotNetObject either as top-level args or nested
            var obj1Ref = DotNetObjectRef.Create(obj1);
            var obj1DifferentRef = DotNetObjectRef.Create(obj1);
            runtime.InvokeAsync<object>("test identifier",
                obj1Ref,
                new Dictionary<string, object>
                {
                    { "obj2", DotNetObjectRef.Create(obj2) },
                    { "obj3", DotNetObjectRef.Create(obj3) },
                    { "obj1SameRef", obj1Ref },
                    { "obj1DifferentRef", obj1DifferentRef },
                });

            // Assert: Serialized as expected
            var call = runtime.BeginInvokeCalls.Single();
            Assert.Equal("test identifier", call.Identifier);
            Assert.Equal("[{\"__dotNetObject\":1},{\"obj2\":{\"__dotNetObject\":3},\"obj3\":{\"__dotNetObject\":4},\"obj1SameRef\":{\"__dotNetObject\":1},\"obj1DifferentRef\":{\"__dotNetObject\":2}}]", call.ArgsJson);

            // Assert: Objects were tracked
            Assert.Same(obj1, runtime.ObjectRefManager.FindDotNetObject(1));
            Assert.Same(obj1, runtime.ObjectRefManager.FindDotNetObject(2));
            Assert.Same(obj2, runtime.ObjectRefManager.FindDotNetObject(3));
            Assert.Same(obj3, runtime.ObjectRefManager.FindDotNetObject(4));
        }

        [Fact]
        public void CanSanitizeDotNetInteropExceptions()
        {
            // Arrange
            var expectedMessage = "An error ocurred while invoking '[Assembly]::Method'. Swapping to 'Development' environment will " +
                "display more detailed information about the error that occurred.";

            string GetMessage(string assembly, string method) => $"An error ocurred while invoking '[{assembly}]::{method}'. Swapping to 'Development' environment will " +
                "display more detailed information about the error that occurred.";

            var runtime = new TestJSRuntime()
            {
                OnDotNetException = (e, a, m) => new JSError { Message = GetMessage(a, m) }
            };

            var exception = new Exception("Some really sensitive data in here");

            // Act
            runtime.EndInvokeDotNet("0", false, exception, "Assembly", "Method");

            // Assert
            var call = runtime.BeginInvokeCalls.Single();
            Assert.Equal(0, call.AsyncHandle);
            Assert.Equal("DotNet.jsCallDispatcher.endInvokeDotNetFromJS", call.Identifier);
            Assert.Equal($"[\"0\",false,{{\"message\":\"{expectedMessage.Replace("'", "\\u0027")}\"}}]", call.ArgsJson);
        }

        private class JSError
        {
            public string Message { get; set; }
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

            public Func<Exception, string, string, object> OnDotNetException { get; set; }

            protected override object OnDotNetInvocationException(Exception exception, string assemblyName, string methodName)
            {
                if (OnDotNetException != null)
                {
                    return OnDotNetException(exception, assemblyName, methodName);
                }

                return base.OnDotNetInvocationException(exception, assemblyName, methodName);
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

            public void OnEndInvoke(long asyncHandle, bool succeeded, JSAsyncCallResult callResult)
                => EndInvokeJS(asyncHandle, succeeded, callResult);
        }
    }
}
