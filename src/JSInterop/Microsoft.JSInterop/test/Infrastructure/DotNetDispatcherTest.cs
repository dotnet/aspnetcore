// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.JSInterop.Infrastructure;

public class DotNetDispatcherTest
{
    private static readonly string thisAssemblyName = typeof(DotNetDispatcherTest).Assembly.GetName().Name;

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string or composed entirely of whitespace.")]
    public void CannotInvokeWithInvalidAssemblyName(string assemblyName, string expectedMessage)
    {
        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => DotNetDispatcher.Invoke(new TestJSRuntime(), new DotNetInvocationInfo(assemblyName, "SomeMethod", default, default), "[]"),
            "assemblyKey.AssemblyName",
            expectedMessage);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string or composed entirely of whitespace.")]
    public void CannotInvokeWithInvalidMethodIdentifier(string methodIdentifier, string expectedMessage)
    {
        // Act & Assert
        ExceptionAssert.ThrowsArgument(
            () => DotNetDispatcher.Invoke(new TestJSRuntime(), new DotNetInvocationInfo("SomeAssembly", methodIdentifier, default, default), "[]"),
            "methodIdentifier",
            expectedMessage);
    }

    [Fact]
    public void CannotInvokeMethodsOnUnloadedAssembly()
    {
        var assemblyName = "Some.Fake.Assembly";
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            DotNetDispatcher.Invoke(new TestJSRuntime(), new DotNetInvocationInfo(assemblyName, "SomeMethod", default, default), null);
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
            DotNetDispatcher.Invoke(new TestJSRuntime(), new DotNetInvocationInfo(thisAssemblyName, methodIdentifier, default, default), null);
        });

        Assert.Equal($"The assembly '{thisAssemblyName}' does not contain a public invokable method with [JSInvokableAttribute(\"{methodIdentifier}\")].", ex.Message);
    }

    [Fact]
    public void CanInvokeStaticVoidMethod()
    {
        // Arrange/Act
        var jsRuntime = new TestJSRuntime();
        SomePublicType.DidInvokeMyInvocableStaticVoid = false;
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, "InvocableStaticVoid", default, default), null);

        // Assert
        Assert.Null(resultJson);
        Assert.True(SomePublicType.DidInvokeMyInvocableStaticVoid);
    }

    [Fact]
    public void CanInvokeStaticNonVoidMethod()
    {
        // Arrange/Act
        var jsRuntime = new TestJSRuntime();
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, "InvocableStaticNonVoid", default, default), null);
        var result = JsonSerializer.Deserialize<TestDTO>(resultJson, jsRuntime.JsonSerializerOptions);

        // Assert
        Assert.Equal("Test", result.StringVal);
        Assert.Equal(123, result.IntVal);
    }

    [Fact]
    public void CanInvokeStaticNonVoidMethodWithoutCustomIdentifier()
    {
        // Arrange/Act
        var jsRuntime = new TestJSRuntime();
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, nameof(SomePublicType.InvokableMethodWithoutCustomIdentifier), default, default), null);
        var result = JsonSerializer.Deserialize<TestDTO>(resultJson, jsRuntime.JsonSerializerOptions);

        // Assert
        Assert.Equal("InvokableMethodWithoutCustomIdentifier", result.StringVal);
        Assert.Equal(456, result.IntVal);
    }

    [Fact]
    public void CanInvokeStaticWithParams()
    {
        // Arrange: Track a .NET object to use as an arg
        var jsRuntime = new TestJSRuntime();
        var arg3 = new TestDTO { IntVal = 999, StringVal = "My string" };
        var objectRef = DotNetObjectReference.Create(arg3);
        jsRuntime.Invoke<object>("unimportant", objectRef);

        // Arrange: Remaining args
        var argsJson = JsonSerializer.Serialize(new object[]
        {
                new TestDTO { StringVal = "Another string", IntVal = 456 },
                new[] { 100, 200 },
                objectRef
        }, jsRuntime.JsonSerializerOptions);

        // Act
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, "InvocableStaticWithParams", default, default), argsJson);
        var result = JsonDocument.Parse(resultJson);
        var root = result.RootElement;

        // Assert: First result value marshalled via JSON
        var resultDto1 = JsonSerializer.Deserialize<TestDTO>(root[0].GetRawText(), jsRuntime.JsonSerializerOptions);

        Assert.Equal("ANOTHER STRING", resultDto1.StringVal);
        Assert.Equal(756, resultDto1.IntVal);

        // Assert: Second result value marshalled by ref
        var resultDto2Ref = root[1];
        Assert.False(resultDto2Ref.TryGetProperty(nameof(TestDTO.StringVal), out _));
        Assert.False(resultDto2Ref.TryGetProperty(nameof(TestDTO.IntVal), out _));

        Assert.True(resultDto2Ref.TryGetProperty(DotNetDispatcher.DotNetObjectRefKey.EncodedUtf8Bytes, out var property));
        var resultDto2 = Assert.IsType<DotNetObjectReference<TestDTO>>(jsRuntime.GetObjectReference(property.GetInt64())).Value;
        Assert.Equal("MY STRING", resultDto2.StringVal);
        Assert.Equal(1299, resultDto2.IntVal);
    }

    [Fact]
    public void InvokingWithIncorrectUseOfDotNetObjectRefThrows()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var method = nameof(SomePublicType.IncorrectDotNetObjectRefUsage);
        var arg3 = new TestDTO { IntVal = 999, StringVal = "My string" };
        var objectRef = DotNetObjectReference.Create(arg3);
        jsRuntime.Invoke<object>("unimportant", objectRef);

        // Arrange: Remaining args
        var argsJson = JsonSerializer.Serialize(new object[]
        {
                new TestDTO { StringVal = "Another string", IntVal = 456 },
                new[] { 100, 200 },
                objectRef
        }, jsRuntime.JsonSerializerOptions);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, method, default, default), argsJson));
        Assert.Equal($"In call to '{method}', parameter of type '{nameof(TestDTO)}' at index 3 must be declared as type 'DotNetObjectRef<TestDTO>' to receive the incoming value.", ex.Message);
    }

    [Fact]
    public void CanInvokeInstanceVoidMethod()
    {
        // Arrange: Track some instance
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new SomePublicType();
        var objectRef = DotNetObjectReference.Create(targetInstance);
        jsRuntime.Invoke<object>("unimportant", objectRef);

        // Act
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, "InvokableInstanceVoid", 1, default), null);

        // Assert
        Assert.Null(resultJson);
        Assert.True(targetInstance.DidInvokeMyInvocableInstanceVoid);
    }

    [Fact]
    public void CanInvokeBaseInstanceVoidMethod()
    {
        // Arrange: Track some instance
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new DerivedClass();
        var objectRef = DotNetObjectReference.Create(targetInstance);
        jsRuntime.Invoke<object>("unimportant", objectRef);

        // Act
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, "BaseClassInvokableInstanceVoid", 1, default), null);

        // Assert
        Assert.Null(resultJson);
        Assert.True(targetInstance.DidInvokeMyBaseClassInvocableInstanceVoid);
    }

    [Fact]
    public void DotNetObjectReferencesCanBeDisposed()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new SomePublicType();
        var objectRef = DotNetObjectReference.Create(targetInstance);
        jsRuntime.Invoke<object>("unimportant", objectRef);

        // Act
        DotNetDispatcher.BeginInvokeDotNet(jsRuntime, new DotNetInvocationInfo(null, "__Dispose", objectRef.ObjectId, default), null);

        // Assert
        Assert.True(objectRef.Disposed);
    }

    [Fact]
    public void CannotUseDotNetObjectRefAfterDisposal()
    {
        // This test addresses the case where the developer calls objectRef.Dispose()
        // from .NET code, as opposed to .dispose() from JS code

        // Arrange: Track some instance, then dispose it
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new SomePublicType();
        var objectRef = DotNetObjectReference.Create(targetInstance);
        jsRuntime.Invoke<object>("unimportant", objectRef);
        objectRef.Dispose();

        // Act/Assert
        var ex = Assert.Throws<ArgumentException>(
            () => DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, "InvokableInstanceVoid", 1, default), null));
        Assert.StartsWith("There is no tracked object with id '1'.", ex.Message);
    }

    [Fact]
    public void CannotUseDotNetObjectRefAfterReleaseDotNetObject()
    {
        // This test addresses the case where the developer calls .dispose()
        // from JS code, as opposed to objectRef.Dispose() from .NET code

        // Arrange: Track some instance, then dispose it
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new SomePublicType();
        var objectRef = DotNetObjectReference.Create(targetInstance);
        jsRuntime.Invoke<object>("unimportant", objectRef);
        objectRef.Dispose();

        // Act/Assert
        var ex = Assert.Throws<ArgumentException>(
            () => DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, "InvokableInstanceVoid", 1, default), null));
        Assert.StartsWith("There is no tracked object with id '1'.", ex.Message);
    }

    [Fact]
    public void EndInvokeJS_WithSuccessValue()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var testDTO = new TestDTO { StringVal = "Hello", IntVal = 4 };
        var task = jsRuntime.InvokeAsync<TestDTO>("unimportant");
        var argsJson = JsonSerializer.Serialize(new object[] { jsRuntime.LastInvocationAsyncHandle, true, testDTO }, jsRuntime.JsonSerializerOptions);

        // Act
        DotNetDispatcher.EndInvokeJS(jsRuntime, argsJson);

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        var result = task.Result;
        Assert.Equal(testDTO.StringVal, result.StringVal);
        Assert.Equal(testDTO.IntVal, result.IntVal);
    }

    [Fact]
    public async Task EndInvokeJS_WithErrorString()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var expected = "Some error";
        var task = jsRuntime.InvokeAsync<TestDTO>("unimportant");
        var argsJson = JsonSerializer.Serialize(new object[] { jsRuntime.LastInvocationAsyncHandle, false, expected }, jsRuntime.JsonSerializerOptions);

        // Act
        DotNetDispatcher.EndInvokeJS(jsRuntime, argsJson);

        // Assert
        var ex = await Assert.ThrowsAsync<JSException>(async () => await task);
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public async Task EndInvokeJS_WithNullError()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var task = jsRuntime.InvokeAsync<TestDTO>("unimportant");
        var argsJson = JsonSerializer.Serialize(new object[] { jsRuntime.LastInvocationAsyncHandle, false, null }, jsRuntime.JsonSerializerOptions);

        // Act
        DotNetDispatcher.EndInvokeJS(jsRuntime, argsJson);

        // Assert
        var ex = await Assert.ThrowsAsync<JSException>(async () => await task);
        Assert.Empty(ex.Message);
    }

    [Fact]
    public void EndInvokeJS_DoesNotThrowJSONExceptionIfTaskCancelled()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var testDTO = new TestDTO { StringVal = "Hello", IntVal = 4 };
        var cts = new CancellationTokenSource();
        var argsJson = JsonSerializer.Serialize(new object[] { jsRuntime.LastInvocationAsyncHandle, true, testDTO }, jsRuntime.JsonSerializerOptions);

        // Act
        var task = jsRuntime.InvokeAsync<TestDTO>("unimportant", cts.Token);

        cts.Cancel();

        DotNetDispatcher.EndInvokeJS(jsRuntime, argsJson);

        // Assert
        Assert.False(task.IsCompletedSuccessfully);
        Assert.True(task.IsCanceled);
    }

    [Fact]
    public void EndInvokeJS_ThrowsIfJsonIsEmptyString()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var task = jsRuntime.InvokeAsync<TestDTO>("somemethod");

        // Act & Assert
        Assert.ThrowsAny<JsonException>(() => DotNetDispatcher.EndInvokeJS(jsRuntime, ""));
    }

    [Fact]
    public void EndInvokeJS_ThrowsIfJsonIsNotArray()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var task = jsRuntime.InvokeAsync<TestDTO>("somemethod");

        // Act & Assert
        Assert.ThrowsAny<JsonException>(() => DotNetDispatcher.EndInvokeJS(jsRuntime, $"{{\"key\": \"{jsRuntime.LastInvocationAsyncHandle}\"}}"));
    }

    [Fact]
    public void EndInvokeJS_ThrowsIfJsonArrayIsInComplete()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var task = jsRuntime.InvokeAsync<TestDTO>("somemethod");

        // Act & Assert
        Assert.ThrowsAny<JsonException>(() => DotNetDispatcher.EndInvokeJS(jsRuntime, $"[{jsRuntime.LastInvocationAsyncHandle}, false"));
    }

    [Fact]
    public void EndInvokeJS_ThrowsIfJsonArrayHasMoreThan3Arguments()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var task = jsRuntime.InvokeAsync<TestDTO>("somemethod");

        // Act & Assert
        Assert.ThrowsAny<JsonException>(() => DotNetDispatcher.EndInvokeJS(jsRuntime, $"[{jsRuntime.LastInvocationAsyncHandle}, false, \"Hello\", 5]"));
    }

    [Fact]
    public void EndInvokeJS_DoesNotThrowJSONExceptionIfTaskCancelled_WithMoreThan3Arguments()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var cts = new CancellationTokenSource();

        // Act
        var task = jsRuntime.InvokeAsync<TestDTO>("unimportant", cts.Token);

        cts.Cancel();

        DotNetDispatcher.EndInvokeJS(jsRuntime, $"[{jsRuntime.LastInvocationAsyncHandle}, false, \"Hello\", 5]");

        // Assert
        Assert.False(task.IsCompletedSuccessfully);
        Assert.True(task.IsCanceled);
    }

    [Fact]
    public void EndInvokeJS_Works()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var task = jsRuntime.InvokeAsync<TestDTO>("somemethod");

        // Act
        DotNetDispatcher.EndInvokeJS(jsRuntime, $"[{jsRuntime.LastInvocationAsyncHandle}, true, {{\"intVal\": 7}}]");

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(7, task.Result.IntVal);
    }

    [Fact]
    public void EndInvokeJS_WithArrayValue()
    {
        var jsRuntime = new TestJSRuntime();
        var task = jsRuntime.InvokeAsync<int[]>("somemethod");

        DotNetDispatcher.EndInvokeJS(jsRuntime, $"[{jsRuntime.LastInvocationAsyncHandle}, true, [1, 2, 3]]");

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Equal(new[] { 1, 2, 3 }, task.Result);
    }

    [Fact]
    public void EndInvokeJS_WithNullValue()
    {
        var jsRuntime = new TestJSRuntime();
        var task = jsRuntime.InvokeAsync<TestDTO>("somemethod");

        DotNetDispatcher.EndInvokeJS(jsRuntime, $"[{jsRuntime.LastInvocationAsyncHandle}, true, null]");

        Assert.True(task.IsCompletedSuccessfully);
        Assert.Null(task.Result);
    }

    [Fact]
    public void CanInvokeInstanceMethodWithParams()
    {
        // Arrange: Track some instance plus another object we'll pass as a param
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new SomePublicType();
        var arg2 = new TestDTO { IntVal = 1234, StringVal = "My string" };
        jsRuntime.Invoke<object>("unimportant",
            DotNetObjectReference.Create(targetInstance),
            DotNetObjectReference.Create(arg2));
        var argsJson = "[\"myvalue\",{\"__dotNetObject\":2}]";

        // Act
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, "InvokableInstanceMethod", 1, default), argsJson);

        // Assert
        Assert.Equal("[\"You passed myvalue\",{\"__dotNetObject\":3}]", resultJson);
        var resultDto = ((DotNetObjectReference<TestDTO>)jsRuntime.GetObjectReference(3)).Value;
        Assert.Equal(1235, resultDto.IntVal);
        Assert.Equal("MY STRING", resultDto.StringVal);
    }

    [Fact]
    public void CanInvokeNonGenericInstanceMethodOnGenericType()
    {
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new GenericType<int>();
        jsRuntime.Invoke<object>("_setup",
            DotNetObjectReference.Create(targetInstance));
        var argsJson = "[\"hello world\"]";

        // Act
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, nameof(GenericType<int>.EchoStringParameter), 1, default), argsJson);

        // Assert
        Assert.Equal("\"hello world\"", resultJson);
    }

    [Fact]
    public void CanInvokeMethodsThatAcceptGenericParametersOnGenericTypes()
    {
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new GenericType<string>();
        jsRuntime.Invoke<object>("_setup",
            DotNetObjectReference.Create(targetInstance));
        var argsJson = "[\"hello world\"]";

        // Act
        var resultJson = DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, nameof(GenericType<string>.EchoParameter), 1, default), argsJson);

        // Assert
        Assert.Equal("\"hello world\"", resultJson);
    }

    [Fact]
    public void CannotInvokeStaticOpenGenericMethods()
    {
        var methodIdentifier = "StaticGenericMethod";
        var jsRuntime = new TestJSRuntime();

        // Act
        var ex = Assert.Throws<ArgumentException>(() => DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, methodIdentifier, 0, default), "[7]"));
        Assert.Contains($"The assembly '{thisAssemblyName}' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].", ex.Message);
    }

    [Fact]
    public void CannotInvokeInstanceOpenGenericMethods()
    {
        var methodIdentifier = "InstanceGenericMethod";
        var targetInstance = new GenericType<int>();
        var jsRuntime = new TestJSRuntime();
        jsRuntime.Invoke<object>("_setup",
            DotNetObjectReference.Create(targetInstance));
        var argsJson = "[\"hello world\"]";

        // Act
        var ex = Assert.Throws<ArgumentException>(() => DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, methodIdentifier, 1, default), argsJson));
        Assert.Contains($"The type 'GenericType`1' does not contain a public invokable method with [{nameof(JSInvokableAttribute)}(\"{methodIdentifier}\")].", ex.Message);
    }

    [Fact]
    public void CannotInvokeMethodsWithGenericParameters_IfTypesDoNotMatch()
    {
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new GenericType<int>();
        jsRuntime.Invoke<object>("_setup",
            DotNetObjectReference.Create(targetInstance));
        var argsJson = "[\"hello world\"]";

        // Act & Assert
        Assert.Throws<JsonException>(() =>
            DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(null, nameof(GenericType<int>.EchoParameter), 1, default), argsJson));
    }

    [Fact]
    public void CannotInvokeWithFewerNumberOfParameters()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var argsJson = JsonSerializer.Serialize(new object[]
        {
                new TestDTO { StringVal = "Another string", IntVal = 456 },
                new[] { 100, 200 },
        }, jsRuntime.JsonSerializerOptions);

        // Act/Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, "InvocableStaticWithParams", default, default), argsJson);
        });

        Assert.Equal("The call to 'InvocableStaticWithParams' expects '3' parameters, but received '2'.", ex.Message);
    }

    [Fact]
    public void CannotInvokeWithMoreParameters()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var objectRef = DotNetObjectReference.Create(new TestDTO { IntVal = 4 });
        var argsJson = JsonSerializer.Serialize(new object[]
        {
                new TestDTO { StringVal = "Another string", IntVal = 456 },
                new[] { 100, 200 },
                objectRef,
                7,
        }, jsRuntime.JsonSerializerOptions);

        // Act/Assert
        var ex = Assert.Throws<JsonException>(() =>
        {
            DotNetDispatcher.Invoke(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, "InvocableStaticWithParams", default, default), argsJson);
        });

        Assert.Equal("Unexpected JSON token Number. Ensure that the call to `InvocableStaticWithParams' is supplied with exactly '3' parameters.", ex.Message);
    }

    [Fact]
    public async Task CanInvokeAsyncMethod()
    {
        // Arrange: Track some instance plus another object we'll pass as a param
        var jsRuntime = new TestJSRuntime();
        var targetInstance = new SomePublicType();
        var arg2 = new TestDTO { IntVal = 1234, StringVal = "My string" };
        var arg1Ref = DotNetObjectReference.Create(targetInstance);
        var arg2Ref = DotNetObjectReference.Create(arg2);
        jsRuntime.Invoke<object>("unimportant", arg1Ref, arg2Ref);

        // Arrange: all args
        var argsJson = JsonSerializer.Serialize(new object[]
        {
                new TestDTO { IntVal = 1000, StringVal = "String via JSON" },
                arg2Ref,
        }, jsRuntime.JsonSerializerOptions);

        // Act
        var callId = "123";
        var resultTask = jsRuntime.NextInvocationTask;
        DotNetDispatcher.BeginInvokeDotNet(jsRuntime, new DotNetInvocationInfo(null, "InvokableAsyncMethod", 1, callId), argsJson);
        await resultTask;

        // Assert: Correct completion information
        Assert.Equal(callId, jsRuntime.LastCompletionCallId);
        Assert.True(jsRuntime.LastCompletionResult.Success);
        var resultJson = Assert.IsType<string>(jsRuntime.LastCompletionResult.ResultJson);
        var result = JsonSerializer.Deserialize<SomePublicType.InvokableAsyncMethodResult>(resultJson, jsRuntime.JsonSerializerOptions);

        Assert.Equal("STRING VIA JSON", result.SomeDTO.StringVal);
        Assert.Equal(2000, result.SomeDTO.IntVal);

        // Assert: Second result value marshalled by ref
        var resultDto2 = result.SomeDTORef.Value;
        Assert.Equal("MY STRING", resultDto2.StringVal);
        Assert.Equal(2468, resultDto2.IntVal);
    }

        [Fact]
        public async Task CanInvokeAsyncMethodReturningValueTask()
        {
            // Arrange: Track some instance plus another object we'll pass as a param
            var jsRuntime = new TestJSRuntime();
            var targetInstance = new SomePublicType();
            var arg2 = new TestDTO { IntVal = 1234, StringVal = "My string" };
            var arg1Ref = DotNetObjectReference.Create(targetInstance);
            var arg2Ref = DotNetObjectReference.Create(arg2);
            jsRuntime.Invoke<object>("unimportant", arg1Ref, arg2Ref);

            // Arrange: all args
            var argsJson = JsonSerializer.Serialize(new object[]
            {
                new TestDTO { IntVal = 1000, StringVal = "String via JSON" },
                arg2Ref,
            }, jsRuntime.JsonSerializerOptions);

            // Act
            var callId = "123";
            var resultTask = jsRuntime.NextInvocationTask;
            DotNetDispatcher.BeginInvokeDotNet(jsRuntime, new DotNetInvocationInfo(null,
                nameof(SomePublicType.InvokableAsyncMethodReturningValueTask), 1, callId), argsJson);
            await resultTask;

            // Assert: Correct completion information
            Assert.Equal(callId, jsRuntime.LastCompletionCallId);
            Assert.True(jsRuntime.LastCompletionResult.Success);
        }

        [Fact]
        public async Task CanInvokeAsyncMethodReturningNonGenericValueTask()
        {
            // Arrange: Track some instance plus another object we'll pass as a param
            var jsRuntime = new TestJSRuntime();
            var targetInstance = new SomePublicType();
            var arg1Ref = DotNetObjectReference.Create(targetInstance);
            jsRuntime.Invoke<object>("unimportant", arg1Ref);

            // Arrange: all args
            var argsJson = JsonSerializer.Serialize(new object[] { }, jsRuntime.JsonSerializerOptions);

            // Act
            var callId = "123";
            var resultTask = jsRuntime.NextInvocationTask;
            DotNetDispatcher.BeginInvokeDotNet(jsRuntime, new DotNetInvocationInfo(null,
                nameof(SomePublicType.InvokableAsyncMethodReturningValueTaskNonGeneric), 1, callId), argsJson);
            await resultTask;

            // Assert: Correct completion information
            Assert.Equal(callId, jsRuntime.LastCompletionCallId);
            Assert.True(jsRuntime.LastCompletionResult.Success);
        }

    [Fact]
    public async Task CanInvokeSyncThrowingMethod()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();

        // Act
        var callId = "123";
        var resultTask = jsRuntime.NextInvocationTask;
        DotNetDispatcher.BeginInvokeDotNet(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, nameof(ThrowingClass.ThrowingMethod), default, callId), default);

        await resultTask; // This won't throw, it sets properties on the jsRuntime.

        // Assert
        Assert.Equal(callId, jsRuntime.LastCompletionCallId);
        Assert.False(jsRuntime.LastCompletionResult.Success); // Fails

        // Make sure the method that threw the exception shows up in the call stack
        // https://github.com/dotnet/aspnetcore/issues/8612
        Assert.Contains(nameof(ThrowingClass.ThrowingMethod), jsRuntime.LastCompletionResult.Exception.ToString());
    }

    [Fact]
    public async Task CanInvokeAsyncThrowingMethod()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();

        // Act
        var callId = "123";
        var resultTask = jsRuntime.NextInvocationTask;
        DotNetDispatcher.BeginInvokeDotNet(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, nameof(ThrowingClass.AsyncThrowingMethod), default, callId), default);

        await resultTask; // This won't throw, it sets properties on the jsRuntime.

        // Assert
        Assert.Equal(callId, jsRuntime.LastCompletionCallId);
        Assert.False(jsRuntime.LastCompletionResult.Success); // Fails

        // Make sure the method that threw the exception shows up in the call stack
        // https://github.com/dotnet/aspnetcore/issues/8612
        Assert.Contains(nameof(ThrowingClass.AsyncThrowingMethod), jsRuntime.LastCompletionResult.Exception.ToString());
    }

    [Fact]
    public async Task BeginInvoke_ThrowsWithInvalidArgsJson_WithCallId()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var callId = "123";
        var resultTask = jsRuntime.NextInvocationTask;
        DotNetDispatcher.BeginInvokeDotNet(jsRuntime, new DotNetInvocationInfo(thisAssemblyName, "InvocableStaticWithParams", default, callId), "<xml>not json</xml>");

        await resultTask; // This won't throw, it sets properties on the jsRuntime.

        // Assert
        Assert.Equal(callId, jsRuntime.LastCompletionCallId);
        Assert.False(jsRuntime.LastCompletionResult.Success); // Fails
        var exception = jsRuntime.LastCompletionResult.Exception;
        Assert.Contains("JsonReaderException: '<' is an invalid start of a value.", exception.ToString());
    }

    [Fact]
    public void BeginInvoke_ThrowsWithInvalid_DotNetObjectRef()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var callId = "123";
        var resultTask = jsRuntime.NextInvocationTask;
        DotNetDispatcher.BeginInvokeDotNet(jsRuntime, new DotNetInvocationInfo(null, "InvokableInstanceVoid", 1, callId), null);

        // Assert
        Assert.Equal(callId, jsRuntime.LastCompletionCallId);
        Assert.False(jsRuntime.LastCompletionResult.Success); // Fails
        var exception = jsRuntime.LastCompletionResult.Exception;
        Assert.StartsWith("System.ArgumentException: There is no tracked object with id '1'. Perhaps the DotNetObjectReference instance was already disposed.", exception.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("<xml>")]
    public void ParseArguments_ThrowsIfJsonIsInvalid(string arguments)
    {
        Assert.ThrowsAny<JsonException>(() => DotNetDispatcher.ParseArguments(new TestJSRuntime(), "SomeMethod", arguments, new[] { typeof(string) }));
    }

    [Theory]
    [InlineData("{\"key\":\"value\"}")]
    [InlineData("\"Test\"")]
    public void ParseArguments_ThrowsIfTheArgsJsonIsNotArray(string arguments)
    {
        // Act & Assert
        Assert.ThrowsAny<JsonException>(() => DotNetDispatcher.ParseArguments(new TestJSRuntime(), "SomeMethod", arguments, new[] { typeof(string) }));
    }

    [Theory]
    [InlineData("[\"hello\"")]
    [InlineData("[\"hello\",")]
    public void ParseArguments_ThrowsIfTheArgsJsonIsInvalidArray(string arguments)
    {
        // Act & Assert
        Assert.ThrowsAny<JsonException>(() => DotNetDispatcher.ParseArguments(new TestJSRuntime(), "SomeMethod", arguments, new[] { typeof(string) }));
    }

    [Fact]
    public void ParseArguments_Works()
    {
        // Arrange
        var arguments = "[\"Hello\", 2]";

        // Act
        var result = DotNetDispatcher.ParseArguments(new TestJSRuntime(), "SomeMethod", arguments, new[] { typeof(string), typeof(int), });

        // Assert
        Assert.Equal(new object[] { "Hello", 2 }, result);
    }

    [Fact]
    public void ParseArguments_SingleArgument()
    {
        // Arrange
        var arguments = "[{\"IntVal\": 7}]";

        // Act
        var result = DotNetDispatcher.ParseArguments(new TestJSRuntime(), "SomeMethod", arguments, new[] { typeof(TestDTO), });

        // Assert
        var value = Assert.IsType<TestDTO>(Assert.Single(result));
        Assert.Equal(7, value.IntVal);
        Assert.Null(value.StringVal);
    }

    [Fact]
    public void ParseArguments_NullArgument()
    {
        // Arrange
        var arguments = "[4, null]";

        // Act
        var result = DotNetDispatcher.ParseArguments(new TestJSRuntime(), "SomeMethod", arguments, new[] { typeof(int), typeof(TestDTO), });

        // Assert
        Assert.Collection(
            result,
            v => Assert.Equal(4, v),
            v => Assert.Null(v));
    }

    [Fact]
    public void ParseArguments_Throws_WithIncorrectDotNetObjectRefUsage()
    {
        // Arrange
        var method = "SomeMethod";
        var arguments = "[4, {\"__dotNetObject\": 7}]";

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => DotNetDispatcher.ParseArguments(new TestJSRuntime(), method, arguments, new[] { typeof(int), typeof(TestDTO), }));

        // Assert
        Assert.Equal($"In call to '{method}', parameter of type '{nameof(TestDTO)}' at index 2 must be declared as type 'DotNetObjectRef<TestDTO>' to receive the incoming value.", ex.Message);
    }

    [Fact]
    public void ReceiveByteArray_Works()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var byteArray = new byte[] { 1, 5, 7 };

        // Act
        DotNetDispatcher.ReceiveByteArray(jsRuntime, 0, byteArray);

        // Assert
        Assert.Equal(1, jsRuntime.ByteArraysToBeRevived.Count);
        Assert.Equal(byteArray, jsRuntime.ByteArraysToBeRevived.Buffer[0]);
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
        public static object[] MyInvocableWithParams(TestDTO dtoViaJson, int[] incrementAmounts, DotNetObjectReference<TestDTO> dtoByRef)
            => new object[]
            {
                    new TestDTO // Return via JSON marshalling
                    {
                        StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                        IntVal = dtoViaJson.IntVal + incrementAmounts.Sum()
                    },
                    DotNetObjectReference.Create(new TestDTO // Return by ref
                    {
                        StringVal = dtoByRef.Value.StringVal.ToUpperInvariant(),
                        IntVal = dtoByRef.Value.IntVal + incrementAmounts.Sum()
                    })
            };

        [JSInvokable(nameof(IncorrectDotNetObjectRefUsage))]
        public static object[] IncorrectDotNetObjectRefUsage(TestDTO dtoViaJson, int[] incrementAmounts, TestDTO dtoByRef)
            => throw new InvalidOperationException("Shouldn't be called");

        [JSInvokable]
        public static TestDTO InvokableMethodWithoutCustomIdentifier()
            => new TestDTO { StringVal = "InvokableMethodWithoutCustomIdentifier", IntVal = 456 };

        [JSInvokable]
        public void InvokableInstanceVoid()
        {
            DidInvokeMyInvocableInstanceVoid = true;
        }

        [JSInvokable]
        public object[] InvokableInstanceMethod(string someString, DotNetObjectReference<TestDTO> someDTORef)
        {
            var someDTO = someDTORef.Value;
            // Returning an array to make the point that object references
            // can be embedded anywhere in the result
            return new object[]
            {
                    $"You passed {someString}",
                    DotNetObjectReference.Create(new TestDTO
                    {
                        IntVal = someDTO.IntVal + 1,
                        StringVal = someDTO.StringVal.ToUpperInvariant()
                    })
            };
        }

        [JSInvokable]
        public async Task<InvokableAsyncMethodResult> InvokableAsyncMethod(TestDTO dtoViaJson, DotNetObjectReference<TestDTO> dtoByRefWrapper)
        {
            await Task.Delay(50);
            var dtoByRef = dtoByRefWrapper.Value;
            return new InvokableAsyncMethodResult
            {
                SomeDTO = new TestDTO // Return via JSON
                {
                    StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                    IntVal = dtoViaJson.IntVal * 2,
                },
                SomeDTORef = DotNetObjectReference.Create(new TestDTO // Return by ref
                {
                    StringVal = dtoByRef.StringVal.ToUpperInvariant(),
                    IntVal = dtoByRef.IntVal * 2,
                })
            };
        }

        [JSInvokable]
        public async ValueTask<InvokableAsyncMethodResult> InvokableAsyncMethodReturningValueTask(TestDTO dtoViaJson, DotNetObjectReference<TestDTO> dtoByRefWrapper)
        {
            var dtoByRef = dtoByRefWrapper.Value;
            return await new ValueTask<InvokableAsyncMethodResult>( new InvokableAsyncMethodResult()
            {
                SomeDTO = new TestDTO // Return via JSON
                {
                    StringVal = dtoViaJson.StringVal.ToUpperInvariant(),
                    IntVal = dtoViaJson.IntVal * 2,
                },
                SomeDTORef = DotNetObjectReference.Create(new TestDTO // Return by ref
                {
                    StringVal = dtoByRef.StringVal.ToUpperInvariant(),
                    IntVal = dtoByRef.IntVal * 2,
                })
            });
        }

        [JSInvokable]
        public async ValueTask InvokableAsyncMethodReturningValueTaskNonGeneric()
        {
            await Task.CompletedTask;
            return;
        }

        public class InvokableAsyncMethodResult
        {
            public TestDTO SomeDTO { get; set; }
            public DotNetObjectReference<TestDTO> SomeDTORef { get; set; }
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

    public class ThrowingClass
    {
        [JSInvokable]
        public static string ThrowingMethod()
        {
            throw new InvalidTimeZoneException();
        }

        [JSInvokable]
        public static async Task<string> AsyncThrowingMethod()
        {
            await Task.Yield();
            throw new InvalidTimeZoneException();
        }
    }

    public class GenericType<TValue>
    {
        [JSInvokable] public string EchoStringParameter(string input) => input;
        [JSInvokable] public TValue EchoParameter(TValue input) => input;
    }

    public class GenericMethodClass
    {
        [JSInvokable("StaticGenericMethod")] public static string StaticGenericMethod<TValue>(TValue input) => input.ToString();
        [JSInvokable("InstanceGenericMethod")] public string GenericMethod<TValue>(TValue input) => input.ToString();
    }

    public class TestJSRuntime : JSInProcessRuntime
    {
        private TaskCompletionSource _nextInvocationTcs = new TaskCompletionSource();
        public Task NextInvocationTask => _nextInvocationTcs.Task;
        public long LastInvocationAsyncHandle { get; private set; }
        public string LastInvocationIdentifier { get; private set; }
        public string LastInvocationArgsJson { get; private set; }

        public string LastCompletionCallId { get; private set; }
        public DotNetInvocationResult LastCompletionResult { get; private set; }

        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            LastInvocationAsyncHandle = asyncHandle;
            LastInvocationIdentifier = identifier;
            LastInvocationArgsJson = argsJson;
            _nextInvocationTcs.SetResult();
            _nextInvocationTcs = new TaskCompletionSource();
        }

        protected override string InvokeJS(string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            LastInvocationAsyncHandle = default;
            LastInvocationIdentifier = identifier;
            LastInvocationArgsJson = argsJson;
            _nextInvocationTcs.SetResult();
            _nextInvocationTcs = new TaskCompletionSource();
            return null;
        }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            LastCompletionCallId = invocationInfo.CallId;
            LastCompletionResult = invocationResult;
            _nextInvocationTcs.SetResult();
            _nextInvocationTcs = new TaskCompletionSource();
        }
    }
}
