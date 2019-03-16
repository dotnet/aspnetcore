// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.JSInterop.Tests
{
    public class DotNetDispatcherTest
    {
        private readonly static string thisAssemblyName
            = typeof(DotNetDispatcherTest).Assembly.GetName().Name;
        private readonly TestJSRuntime jsRuntime
            = new TestJSRuntime();

        [Fact]
        public void CannotInvokeWithEmptyAssemblyName()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(" ", "SomeMethod", default, "[]");
            });

            Assert.StartsWith("Cannot be null, empty, or whitespace.", ex.Message);
            Assert.Equal("assemblyName", ex.ParamName);
        }

        [Fact]
        public void CannotInvokeWithEmptyMethodIdentifier()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke("SomeAssembly", " ", default, "[]");
            });

            Assert.StartsWith("Cannot be null, empty, or whitespace.", ex.Message);
            Assert.Equal("methodIdentifier", ex.ParamName);
        }

        [Fact]
        public void CannotInvokeMethodsOnUnloadedAssembly()
        {
            var assemblyName = "Some.Fake.Assembly";
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(assemblyName, "SomeMethod", default, null);
            });

            Assert.Equal($"There is no loaded assembly with the name '{assemblyName}'.", ex.Message);
        }

        // Note: Currently it's also not possible to invoke generic methods.
        // That's not something determined by DotNetDispatcher, but rather by the fact that we
        // don't close over the generics in the reflection code.
        // Not defining this behavior through unit tests because the default outcome is
        // fine (an exception stating what info is missing).

        [Theory]
        [InlineData("MethodOnInternalType")]
        [InlineData("PrivateMethod")]
        [InlineData("ProtectedMethod")]
        [InlineData("StaticMethodWithoutAttribute")] // That's not really its identifier; just making the point that there's no way to invoke it
        [InlineData("InstanceMethodWithoutAttribute")] // That's not really its identifier; just making the point that there's no way to invoke it
        public void CannotInvokeUnsuitableMethods(string methodIdentifier)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(thisAssemblyName, methodIdentifier, default, null);
            });

            Assert.Equal($"The assembly '{thisAssemblyName}' does not contain a public method with [JSInvokableAttribute(\"{methodIdentifier}\")].", ex.Message);
        }

        [Fact(Skip = "https://github.com/aspnet/AspNetCore-Internal/issues/1733")]
        public Task CanInvokeStaticVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange/Act
            SomePublicType.DidInvokeMyInvocableStaticVoid = false;
            var resultJson = DotNetDispatcher.Invoke(thisAssemblyName, "InvocableStaticVoid", default, null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(SomePublicType.DidInvokeMyInvocableStaticVoid);
        });

        [Fact]
        public Task CanInvokeStaticNonVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange/Act
            var resultJson = DotNetDispatcher.Invoke(thisAssemblyName, "InvocableStaticNonVoid", default, null);
            var result = Json.Deserialize<TestDTO>(resultJson);

            // Assert
            Assert.Equal("Test", result.StringVal);
            Assert.Equal(123, result.IntVal);
        });

        [Fact]
        public Task CanInvokeStaticNonVoidMethodWithoutCustomIdentifier() => WithJSRuntime(jsRuntime =>
        {
            // Arrange/Act
            var resultJson = DotNetDispatcher.Invoke(thisAssemblyName, nameof(SomePublicType.InvokableMethodWithoutCustomIdentifier), default, null);
            var result = Json.Deserialize<TestDTO>(resultJson);

            // Assert
            Assert.Equal("InvokableMethodWithoutCustomIdentifier", result.StringVal);
            Assert.Equal(456, result.IntVal);
        });

        [Fact(Skip = "https://github.com/aspnet/AspNetCore-Internal/issues/1733")]
        public Task CanInvokeStaticWithParams() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track a .NET object to use as an arg
            var arg3 = new TestDTO { IntVal = 999, StringVal = "My string" };
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(arg3));

            // Arrange: Remaining args
            var argsJson = Json.Serialize(new object[] {
                new TestDTO { StringVal = "Another string", IntVal = 456 },
                new[] { 100, 200 },
                "__dotNetObject:1"
            });

            // Act
            var resultJson = DotNetDispatcher.Invoke(thisAssemblyName, "InvocableStaticWithParams", default, argsJson);
            var result = Json.Deserialize<object[]>(resultJson);

            // Assert: First result value marshalled via JSON
            var resultDto1 = (TestDTO)jsRuntime.ArgSerializerStrategy.DeserializeObject(result[0], typeof(TestDTO));
            Assert.Equal("ANOTHER STRING", resultDto1.StringVal);
            Assert.Equal(756, resultDto1.IntVal);

            // Assert: Second result value marshalled by ref
            var resultDto2Ref = (string)result[1];
            Assert.Equal("__dotNetObject:2", resultDto2Ref);
            var resultDto2 = (TestDTO)jsRuntime.ArgSerializerStrategy.FindDotNetObject(2);
            Assert.Equal("MY STRING", resultDto2.StringVal);
            Assert.Equal(1299, resultDto2.IntVal);
        });

        [Fact(Skip = "https://github.com/aspnet/AspNetCore-Internal/issues/1733")]
        public Task CanInvokeInstanceVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance
            var targetInstance = new SomePublicType();
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance));

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, "InvokableInstanceVoid", 1, null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(targetInstance.DidInvokeMyInvocableInstanceVoid);
        });

        [Fact(Skip = "https://github.com/aspnet/AspNetCore-Internal/issues/1733")]
        public Task CanInvokeBaseInstanceVoidMethod() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance
            var targetInstance = new DerivedClass();
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance));

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, "BaseClassInvokableInstanceVoid", 1, null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(targetInstance.DidInvokeMyBaseClassInvocableInstanceVoid);
        });

        [Fact]
        public Task CannotUseDotNetObjectRefAfterDisposal() => WithJSRuntime(jsRuntime =>
        {
            // This test addresses the case where the developer calls objectRef.Dispose()
            // from .NET code, as opposed to .dispose() from JS code

            // Arrange: Track some instance, then dispose it
            var targetInstance = new SomePublicType();
            var objectRef = new DotNetObjectRef(targetInstance);
            jsRuntime.Invoke<object>("unimportant", objectRef);
            objectRef.Dispose();

            // Act/Assert
            var ex = Assert.Throws<ArgumentException>(
                () => DotNetDispatcher.Invoke(null, "InvokableInstanceVoid", 1, null));
            Assert.StartsWith("There is no tracked object with id '1'.", ex.Message);
        });

        [Fact]
        public Task CannotUseDotNetObjectRefAfterReleaseDotNetObject() => WithJSRuntime(jsRuntime =>
        {
            // This test addresses the case where the developer calls .dispose()
            // from JS code, as opposed to objectRef.Dispose() from .NET code

            // Arrange: Track some instance, then dispose it
            var targetInstance = new SomePublicType();
            var objectRef = new DotNetObjectRef(targetInstance);
            jsRuntime.Invoke<object>("unimportant", objectRef);
            DotNetDispatcher.ReleaseDotNetObject(1);

            // Act/Assert
            var ex = Assert.Throws<ArgumentException>(
                () => DotNetDispatcher.Invoke(null, "InvokableInstanceVoid", 1, null));
            Assert.StartsWith("There is no tracked object with id '1'.", ex.Message);
        });

        [Fact(Skip = "https://github.com/aspnet/AspNetCore-Internal/issues/1733")]
        public Task CanInvokeInstanceMethodWithParams() => WithJSRuntime(jsRuntime =>
        {
            // Arrange: Track some instance plus another object we'll pass as a param
            var targetInstance = new SomePublicType();
            var arg2 = new TestDTO { IntVal = 1234, StringVal = "My string" };
            jsRuntime.Invoke<object>("unimportant",
                new DotNetObjectRef(targetInstance),
                new DotNetObjectRef(arg2));
            var argsJson = "[\"myvalue\",\"__dotNetObject:2\"]";

            // Act
            var resultJson = DotNetDispatcher.Invoke(null, "InvokableInstanceMethod", 1, argsJson);

            // Assert
            Assert.Equal("[\"You passed myvalue\",\"__dotNetObject:3\"]", resultJson);
            var resultDto = (TestDTO)jsRuntime.ArgSerializerStrategy.FindDotNetObject(3);
            Assert.Equal(1235, resultDto.IntVal);
            Assert.Equal("MY STRING", resultDto.StringVal);
        });

        [Fact]
        public void CannotInvokeWithIncorrectNumberOfParams()
        {
            // Arrange
            var argsJson = Json.Serialize(new object[] { 1, 2, 3, 4 });

            // Act/Assert
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(thisAssemblyName, "InvocableStaticWithParams", default, argsJson);
            });

            Assert.Equal("In call to 'InvocableStaticWithParams', expected 3 parameters but received 4.", ex.Message);
        }

        [Fact(Skip = "https://github.com/aspnet/AspNetCore-Internal/issues/1733")]
        public Task CanInvokeAsyncMethod() => WithJSRuntime(async jsRuntime =>
        {
            // Arrange: Track some instance plus another object we'll pass as a param
            var targetInstance = new SomePublicType();
            var arg2 = new TestDTO { IntVal = 1234, StringVal = "My string" };
            jsRuntime.Invoke<object>("unimportant", new DotNetObjectRef(targetInstance), new DotNetObjectRef(arg2));

            // Arrange: all args
            var argsJson = Json.Serialize(new object[]
            {
                new TestDTO { IntVal = 1000, StringVal = "String via JSON" },
                "__dotNetObject:2"
            });

            // Act
            var callId = "123";
            var resultTask = jsRuntime.NextInvocationTask;
            DotNetDispatcher.BeginInvoke(callId, null, "InvokableAsyncMethod", 1, argsJson);
            await resultTask;
            var result = Json.Deserialize<SimpleJson.JsonArray>(jsRuntime.LastInvocationArgsJson);
            var resultValue = (SimpleJson.JsonArray)result[2];

            // Assert: Correct info to complete the async call
            Assert.Equal(0, jsRuntime.LastInvocationAsyncHandle); // 0 because it doesn't want a further callback from JS to .NET
            Assert.Equal("DotNet.jsCallDispatcher.endInvokeDotNetFromJS", jsRuntime.LastInvocationIdentifier);
            Assert.Equal(3, result.Count);
            Assert.Equal(callId, result[0]);
            Assert.True((bool)result[1]); // Success flag

            // Assert: First result value marshalled via JSON
            var resultDto1 = (TestDTO)jsRuntime.ArgSerializerStrategy.DeserializeObject(resultValue[0], typeof(TestDTO));
            Assert.Equal("STRING VIA JSON", resultDto1.StringVal);
            Assert.Equal(2000, resultDto1.IntVal);

            // Assert: Second result value marshalled by ref
            var resultDto2Ref = (string)resultValue[1];
            Assert.Equal("__dotNetObject:3", resultDto2Ref);
            var resultDto2 = (TestDTO)jsRuntime.ArgSerializerStrategy.FindDotNetObject(3);
            Assert.Equal("MY STRING", resultDto2.StringVal);
            Assert.Equal(2468, resultDto2.IntVal);
        });

        Task WithJSRuntime(Action<TestJSRuntime> testCode)
        {
            return WithJSRuntime(jsRuntime =>
            {
                testCode(jsRuntime);
                return Task.CompletedTask;
            });
        }

        async Task WithJSRuntime(Func<TestJSRuntime, Task> testCode)
        {
            // Since the tests rely on the asynclocal JSRuntime.Current, ensure we
            // are on a distinct async context with a non-null JSRuntime.Current
            await Task.Yield();

            var runtime = new TestJSRuntime();
            JSRuntime.SetCurrentJSRuntime(runtime);
            await testCode(runtime);
        }

        internal class SomeInteralType
        {
            [JSInvokable("MethodOnInternalType")] public void MyMethod() { }
        }

        public class SomePublicType
        {
            public static bool DidInvokeMyInvocableStaticVoid;
            public bool DidInvokeMyInvocableInstanceVoid;

            [JSInvokable("PrivateMethod")] private static void MyPrivateMethod() { }
            [JSInvokable("ProtectedMethod")] protected static void MyProtectedMethod() { }
            protected static void StaticMethodWithoutAttribute() { }
            protected static void InstanceMethodWithoutAttribute() { }

            [JSInvokable("InvocableStaticVoid")]
            public static void MyInvocableVoid()
            {
                DidInvokeMyInvocableStaticVoid = true;
            }

            [JSInvokable("InvocableStaticNonVoid")]
            public static object MyInvocableNonVoid()
                => new TestDTO { StringVal = "Test", IntVal = 123 };

            [JSInvokable("InvocableStaticWithParams")]
            public static object[] MyInvocableWithParams(TestDTO dtoViaJson, int[] incrementAmounts, TestDTO dtoByRef)
                => new object[]
                {
                    new TestDTO // Return via JSON marshalling
                    {
                        StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                        IntVal = dtoViaJson.IntVal + incrementAmounts.Sum()
                    },
                    new DotNetObjectRef(new TestDTO // Return by ref
                    {
                        StringVal = dtoByRef.StringVal.ToUpperInvariant(),
                        IntVal = dtoByRef.IntVal + incrementAmounts.Sum()
                    })
                };

            [JSInvokable]
            public static TestDTO InvokableMethodWithoutCustomIdentifier()
                => new TestDTO { StringVal = "InvokableMethodWithoutCustomIdentifier", IntVal = 456 };

            [JSInvokable]
            public void InvokableInstanceVoid()
            {
                DidInvokeMyInvocableInstanceVoid = true;
            }

            [JSInvokable]
            public object[] InvokableInstanceMethod(string someString, TestDTO someDTO)
            {
                // Returning an array to make the point that object references
                // can be embedded anywhere in the result
                return new object[]
                {
                    $"You passed {someString}",
                    new DotNetObjectRef(new TestDTO
                    {
                        IntVal = someDTO.IntVal + 1,
                        StringVal = someDTO.StringVal.ToUpperInvariant()
                    })
                };
            }

            [JSInvokable]
            public async Task<object[]> InvokableAsyncMethod(TestDTO dtoViaJson, TestDTO dtoByRef)
            {
                await Task.Delay(50);
                return new object[]
                {
                    new TestDTO // Return via JSON
                    {
                        StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                        IntVal = dtoViaJson.IntVal * 2,
                    },
                    new DotNetObjectRef(new TestDTO // Return by ref
                    {
                        StringVal = dtoByRef.StringVal.ToUpperInvariant(),
                        IntVal = dtoByRef.IntVal * 2,
                    })
                };
            }
        }

        public class BaseClass
        {
            public bool DidInvokeMyBaseClassInvocableInstanceVoid;

            [JSInvokable]
            public void BaseClassInvokableInstanceVoid()
            {
                DidInvokeMyBaseClassInvocableInstanceVoid = true;
            }
        }

        public class DerivedClass : BaseClass
        {
        }

        public class TestDTO
        {
            public string StringVal { get; set; }
            public int IntVal { get; set; }
        }

        public class TestJSRuntime : JSInProcessRuntimeBase
        {
            private TaskCompletionSource<object> _nextInvocationTcs = new TaskCompletionSource<object>();
            public Task NextInvocationTask => _nextInvocationTcs.Task;
            public long LastInvocationAsyncHandle { get; private set; }
            public string LastInvocationIdentifier { get; private set; }
            public string LastInvocationArgsJson { get; private set; }

            protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
            {
                LastInvocationAsyncHandle = asyncHandle;
                LastInvocationIdentifier = identifier;
                LastInvocationArgsJson = argsJson;
                _nextInvocationTcs.SetResult(null);
                _nextInvocationTcs = new TaskCompletionSource<object>();
            }

            protected override string InvokeJS(string identifier, string argsJson)
            {
                LastInvocationAsyncHandle = default;
                LastInvocationIdentifier = identifier;
                LastInvocationArgsJson = argsJson;
                _nextInvocationTcs.SetResult(null);
                _nextInvocationTcs = new TaskCompletionSource<object>();
                return null;
            }
        }
    }
}
