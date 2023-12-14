// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Xunit;

namespace Microsoft.Extensions.Internal;

public class ObjectMethodExecutorTest
{
    private TestObject _targetObject = new TestObject();
    private TypeInfo targetTypeInfo = typeof(TestObject).GetTypeInfo();

    [Fact]
    public void ExecuteValueMethod()
    {
        var executor = GetExecutorForMethod("ValueMethod");
        var result = executor.Execute(
            _targetObject,
            new object[] { 10, 20 });
        Assert.False(executor.IsMethodAsync);
        Assert.Equal(30, (int)result);
    }

    [Fact]
    public void ExecuteVoidValueMethod()
    {
        var executor = GetExecutorForMethod("VoidValueMethod");
        var result = executor.Execute(
            _targetObject,
            new object[] { 10 });
        Assert.False(executor.IsMethodAsync);
        Assert.Null(result);
    }

    [Fact]
    public void ExecuteValueMethodWithReturnType()
    {
        var executor = GetExecutorForMethod("ValueMethodWithReturnType");
        var result = executor.Execute(
            _targetObject,
            new object[] { 10 });
        var resultObject = Assert.IsType<TestObject>(result);
        Assert.False(executor.IsMethodAsync);
        Assert.Equal("Hello", resultObject.value);
    }

    [Fact]
    public void ExecuteValueMethodUpdateValue()
    {
        var executor = GetExecutorForMethod("ValueMethodUpdateValue");
        var parameter = new TestObject();
        var result = executor.Execute(
            _targetObject,
            new object[] { parameter });
        var resultObject = Assert.IsType<TestObject>(result);
        Assert.False(executor.IsMethodAsync);
        Assert.Equal("HelloWorld", resultObject.value);
    }

    [Fact]
    public void ExecuteValueMethodWithReturnTypeThrowsException()
    {
        var executor = GetExecutorForMethod("ValueMethodWithReturnTypeThrowsException");
        var parameter = new TestObject();
        Assert.False(executor.IsMethodAsync);
        Assert.Throws<NotImplementedException>(
                    () => executor.Execute(
                        _targetObject,
                        new object[] { parameter }));
    }

    [Fact]
    public async Task ExecuteValueMethodAsync()
    {
        var executor = GetExecutorForMethod("ValueMethodAsync");
        var result = await executor.ExecuteAsync(
            _targetObject,
            new object[] { 10, 20 });
        Assert.True(executor.IsMethodAsync);
        Assert.Equal(30, (int)result);
    }

    [Fact]
    public async Task ExecuteValueMethodWithReturnTypeAsync()
    {
        var executor = GetExecutorForMethod("ValueMethodWithReturnTypeAsync");
        var result = await executor.ExecuteAsync(
            _targetObject,
            new object[] { 10 });
        var resultObject = Assert.IsType<TestObject>(result);
        Assert.True(executor.IsMethodAsync);
        Assert.Equal("Hello", resultObject.value);
    }

    [Fact]
    public async Task ExecuteValueMethodUpdateValueAsync()
    {
        var executor = GetExecutorForMethod("ValueMethodUpdateValueAsync");
        var parameter = new TestObject();
        var result = await executor.ExecuteAsync(
            _targetObject,
            new object[] { parameter });
        var resultObject = Assert.IsType<TestObject>(result);
        Assert.True(executor.IsMethodAsync);
        Assert.Equal("HelloWorld", resultObject.value);
    }

    [Fact]
    public async Task ExecuteValueMethodWithReturnTypeThrowsExceptionAsync()
    {
        var executor = GetExecutorForMethod("ValueMethodWithReturnTypeThrowsExceptionAsync");
        var parameter = new TestObject();
        Assert.True(executor.IsMethodAsync);
        await Assert.ThrowsAsync<NotImplementedException>(
                async () => await executor.ExecuteAsync(
                        _targetObject,
                        new object[] { parameter }));
    }

    [Theory]
    [InlineData(nameof(TestObject.ValueMethodWithReturnVoidThrowsExceptionAsync))]
    [InlineData(nameof(TestObject.ValueMethodWithReturnVoidValueTaskThrowsExceptionAsync))]
    [InlineData(nameof(TestObject.ValueMethodWithReturnUnitThrowsExceptionAsync))]
    [InlineData(nameof(TestObject.ValueMethodWithReturnValueTaskOfUnitThrowsExceptionAsync))]
    [InlineData(nameof(TestObject.ValueMethodWithReturnFSharpAsyncOfUnitThrowsExceptionAsync))]
    public async Task ExecuteValueMethodWithReturnVoidThrowsExceptionAsync(string method)
    {
        var executor = GetExecutorForMethod(method);
        var parameter = new TestObject();
        Assert.True(executor.IsMethodAsync);
        await Assert.ThrowsAsync<NotImplementedException>(
                async () => await executor.ExecuteAsync(
                        _targetObject,
                        new object[] { parameter }));
    }

    [Fact]
    public void GetDefaultValueForParameters_ReturnsSuppliedValues()
    {
        var suppliedDefaultValues = new object[] { 123, "test value" };
        var executor = GetExecutorForMethod("MethodWithMultipleParameters", suppliedDefaultValues);
        Assert.Equal(suppliedDefaultValues[0], executor.GetDefaultValueForParameter(0));
        Assert.Equal(suppliedDefaultValues[1], executor.GetDefaultValueForParameter(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => executor.GetDefaultValueForParameter(2));
    }

    [Fact]
    public void GetDefaultValueForParameters_ThrowsIfNoneWereSupplied()
    {
        var executor = GetExecutorForMethod("MethodWithMultipleParameters");
        Assert.Throws<InvalidOperationException>(() => executor.GetDefaultValueForParameter(0));
    }

    [Fact]
    public async Task TargetMethodReturningCustomAwaitableOfReferenceType_CanInvokeViaExecute()
    {
        // Arrange
        var executor = GetExecutorForMethod("CustomAwaitableOfReferenceTypeAsync");

        // Act
        var result = await (TestAwaitable<TestObject>)executor.Execute(_targetObject, new object[] { "Hello", 123 });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(TestObject), executor.AsyncResultType);
        Assert.NotNull(result);
        Assert.Equal("Hello 123", result.value);
    }

    [Fact]
    public async Task TargetMethodReturningCustomAwaitableOfValueType_CanInvokeViaExecute()
    {
        // Arrange
        var executor = GetExecutorForMethod("CustomAwaitableOfValueTypeAsync");

        // Act
        var result = await (TestAwaitable<int>)executor.Execute(_targetObject, new object[] { 123, 456 });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(int), executor.AsyncResultType);
        Assert.Equal(579, result);
    }

    [Fact]
    public async Task TargetMethodReturningCustomAwaitableOfReferenceType_CanInvokeViaExecuteAsync()
    {
        // Arrange
        var executor = GetExecutorForMethod("CustomAwaitableOfReferenceTypeAsync");

        // Act
        var result = await executor.ExecuteAsync(_targetObject, new object[] { "Hello", 123 });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(TestObject), executor.AsyncResultType);
        Assert.NotNull(result);
        Assert.IsType<TestObject>(result);
        Assert.Equal("Hello 123", ((TestObject)result).value);
    }

    [Fact]
    public async Task TargetMethodReturningCustomAwaitableOfValueType_CanInvokeViaExecuteAsync()
    {
        // Arrange
        var executor = GetExecutorForMethod("CustomAwaitableOfValueTypeAsync");

        // Act
        var result = await executor.ExecuteAsync(_targetObject, new object[] { 123, 456 });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(int), executor.AsyncResultType);
        Assert.NotNull(result);
        Assert.IsType<int>(result);
        Assert.Equal(579, (int)result);
    }

    [Theory]
    [InlineData(nameof(TestObject.VoidValueMethodAsync))]
    [InlineData(nameof(TestObject.VoidValueTaskMethodAsync))]
    [InlineData(nameof(TestObject.TaskOfUnitMethodAsync))]
    [InlineData(nameof(TestObject.ValueTaskOfUnitMethodAsync))]
    [InlineData(nameof(TestObject.FSharpAsyncOfUnitMethod))]
    public async Task TargetMethodReturningAwaitableOfVoidType_CanInvokeViaExecuteAsync(string method)
    {
        // Arrange
        var executor = GetExecutorForMethod(method);

        // Act
        var result = await executor.ExecuteAsync(_targetObject, new object[] { 123 });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(void), executor.AsyncResultType);
        Assert.Null(result);
    }

    [Fact]
    public async Task TargetMethodReturningAwaitableWithICriticalNotifyCompletion_UsesUnsafeOnCompleted()
    {
        // Arrange
        var executor = GetExecutorForMethod("CustomAwaitableWithICriticalNotifyCompletion");

        // Act
        var result = await executor.ExecuteAsync(_targetObject, new object[0]);

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(string), executor.AsyncResultType);
        Assert.Equal("Used UnsafeOnCompleted", (string)result);
    }

    [Fact]
    public async Task TargetMethodReturningAwaitableWithoutICriticalNotifyCompletion_UsesOnCompleted()
    {
        // Arrange
        var executor = GetExecutorForMethod("CustomAwaitableWithoutICriticalNotifyCompletion");

        // Act
        var result = await executor.ExecuteAsync(_targetObject, new object[0]);

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(string), executor.AsyncResultType);
        Assert.Equal("Used OnCompleted", (string)result);
    }

    [Fact]
    public async Task TargetMethodReturningValueTaskOfValueType_CanBeInvokedViaExecute()
    {
        // Arrange
        var executor = GetExecutorForMethod("ValueTaskOfValueType");

        // Act
        var result = await (ValueTask<int>)executor.Execute(_targetObject, new object[] { 123 });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(int), executor.AsyncResultType);
        Assert.Equal(123, result);
    }

    [Fact]
    public async Task TargetMethodReturningValueTaskOfReferenceType_CanBeInvokedViaExecute()
    {
        // Arrange
        var executor = GetExecutorForMethod("ValueTaskOfReferenceType");

        // Act
        var result = await (ValueTask<string>)executor.Execute(_targetObject, new object[] { "test result" });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(string), executor.AsyncResultType);
        Assert.Equal("test result", result);
    }

    [Fact]
    public async Task TargetMethodReturningValueTaskOfValueType_CanBeInvokedViaExecuteAsync()
    {
        // Arrange
        var executor = GetExecutorForMethod("ValueTaskOfValueType");

        // Act
        var result = await executor.ExecuteAsync(_targetObject, new object[] { 123 });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(int), executor.AsyncResultType);
        Assert.NotNull(result);
        Assert.Equal(123, (int)result);
    }

    [Fact]
    public async Task TargetMethodReturningValueTaskOfReferenceType_CanBeInvokedViaExecuteAsync()
    {
        // Arrange
        var executor = GetExecutorForMethod("ValueTaskOfReferenceType");

        // Act
        var result = await executor.ExecuteAsync(_targetObject, new object[] { "test result" });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(string), executor.AsyncResultType);
        Assert.Equal("test result", result);
    }

    [Fact]
    public async Task TargetMethodReturningFSharpAsync_CanBeInvokedViaExecute()
    {
        // Arrange
        var executor = GetExecutorForMethod("FSharpAsyncMethod");

        // Act
        var fsharpAsync = (FSharpAsync<string>)executor.Execute(_targetObject, new object[] { "test result" });
        var result = await FSharpAsync.StartAsTask(fsharpAsync,
            FSharpOption<TaskCreationOptions>.None,
            FSharpOption<CancellationToken>.None);

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(string), executor.AsyncResultType);
        Assert.Equal("test result", result);
    }

    [Fact]
    public async Task TargetMethodReturningFailingFSharpAsync_CanBeInvokedViaExecute()
    {
        // Arrange
        var executor = GetExecutorForMethod("FSharpAsyncFailureMethod");

        // Act
        var fsharpAsync = (FSharpAsync<string>)executor.Execute(_targetObject, new object[] { "test result" });
        var resultTask = FSharpAsync.StartAsTask(fsharpAsync,
            FSharpOption<TaskCreationOptions>.None,
            FSharpOption<CancellationToken>.None);

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(string), executor.AsyncResultType);

        var exception = await Assert.ThrowsAsync<AggregateException>(async () => await resultTask);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Test exception", exception.InnerException.Message);
    }

    [Fact]
    public async Task TargetMethodReturningFSharpAsync_CanBeInvokedViaExecuteAsync()
    {
        // Arrange
        var executor = GetExecutorForMethod("FSharpAsyncMethod");

        // Act
        var result = await executor.ExecuteAsync(_targetObject, new object[] { "test result" });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(string), executor.AsyncResultType);
        Assert.Equal("test result", result);
    }

    [Fact]
    public async Task TargetMethodReturningFailingFSharpAsync_CanBeInvokedViaExecuteAsync()
    {
        // Arrange
        var executor = GetExecutorForMethod("FSharpAsyncFailureMethod");

        // Act
        var resultTask = executor.ExecuteAsync(_targetObject, new object[] { "test result" });

        // Assert
        Assert.True(executor.IsMethodAsync);
        Assert.Same(typeof(string), executor.AsyncResultType);

        var exception = await Assert.ThrowsAsync<AggregateException>(async () => await resultTask);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Test exception", exception.InnerException.Message);
    }

    private ObjectMethodExecutor GetExecutorForMethod(string methodName)
    {
        var method = typeof(TestObject).GetMethod(methodName);
        return ObjectMethodExecutor.Create(method, targetTypeInfo);
    }

    private ObjectMethodExecutor GetExecutorForMethod(string methodName, object[] parameterDefaultValues)
    {
        var method = typeof(TestObject).GetMethod(methodName);
        return ObjectMethodExecutor.Create(method, targetTypeInfo, parameterDefaultValues);
    }

    public class TestObject
    {
        public string value;
        public int ValueMethod(int i, int j)
        {
            return i + j;
        }

        public void VoidValueMethod(int i)
        {

        }

        public TestObject ValueMethodWithReturnType(int i)
        {
            return new TestObject() { value = "Hello" };
        }

        public TestObject ValueMethodWithReturnTypeThrowsException(TestObject i)
        {
            throw new NotImplementedException("Not Implemented Exception");
        }

        public TestObject ValueMethodUpdateValue(TestObject parameter)
        {
            parameter.value = "HelloWorld";
            return parameter;
        }

        public Task<int> ValueMethodAsync(int i, int j)
        {
            return Task.FromResult<int>(i + j);
        }

        public async Task VoidValueMethodAsync(int i)
        {
            await ValueMethodAsync(3, 4);
        }

        public Task<Unit> TaskOfUnitMethodAsync(int i)
        {
            return Task.FromResult(default(Unit));
        }

        public async ValueTask VoidValueTaskMethodAsync(int i)
        {
            await ValueMethodAsync(3, 4);
        }

        public ValueTask<Unit> ValueTaskOfUnitMethodAsync(int i)
        {
            return ValueTask.FromResult(default(Unit));
        }

        public FSharpAsync<Unit> FSharpAsyncOfUnitMethod()
        {
            return ExtraTopLevelOperators.DefaultAsyncBuilder.Return(default(Unit));
        }

        public Task<TestObject> ValueMethodWithReturnTypeAsync(int i)
        {
            return Task.FromResult<TestObject>(new TestObject() { value = "Hello" });
        }

        public async Task ValueMethodWithReturnVoidThrowsExceptionAsync(TestObject i)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Not Implemented Exception");
        }

        public async Task<Unit> ValueMethodWithReturnUnitThrowsExceptionAsync(TestObject i)
        {
            await Task.FromResult(default(Unit));
            throw new NotImplementedException("Not Implemented Exception");
        }

        public async ValueTask ValueMethodWithReturnVoidValueTaskThrowsExceptionAsync(TestObject i)
        {
            await ValueTask.CompletedTask;
            throw new NotImplementedException("Not Implemented Exception");
        }

        public async ValueTask<Unit> ValueMethodWithReturnValueTaskOfUnitThrowsExceptionAsync(TestObject i)
        {
            await ValueTask.FromResult(default(Unit));
            throw new NotImplementedException("Not Implemented Exception");
        }

        public FSharpAsync<Unit> ValueMethodWithReturnFSharpAsyncOfUnitThrowsExceptionAsync(TestObject i)
        {
            return FSharpAsync.AwaitTask(Run());

            static Task Run()
            {
                Task.FromResult(default(Unit));
                throw new NotImplementedException("Not Implemented Exception");
            }
        }

        public async Task<TestObject> ValueMethodWithReturnTypeThrowsExceptionAsync(TestObject i)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Not Implemented Exception");
        }

        public Task<TestObject> ValueMethodUpdateValueAsync(TestObject parameter)
        {
            parameter.value = "HelloWorld";
            return Task.FromResult<TestObject>(parameter);
        }

        public TestAwaitable<TestObject> CustomAwaitableOfReferenceTypeAsync(
            string input1,
            int input2)
        {
            return new TestAwaitable<TestObject>(new TestObject
            {
                value = $"{input1} {input2}"
            });
        }

        public TestAwaitable<int> CustomAwaitableOfValueTypeAsync(
            int input1,
            int input2)
        {
            return new TestAwaitable<int>(input1 + input2);
        }

        public TestAwaitableWithICriticalNotifyCompletion CustomAwaitableWithICriticalNotifyCompletion()
        {
            return new TestAwaitableWithICriticalNotifyCompletion();
        }

        public TestAwaitableWithoutICriticalNotifyCompletion CustomAwaitableWithoutICriticalNotifyCompletion()
        {
            return new TestAwaitableWithoutICriticalNotifyCompletion();
        }

        public ValueTask<int> ValueTaskOfValueType(int result)
        {
            return new ValueTask<int>(result);
        }

        public ValueTask<string> ValueTaskOfReferenceType(string result)
        {
            return new ValueTask<string>(result);
        }

        public void MethodWithMultipleParameters(int valueTypeParam, string referenceTypeParam)
        {
        }

        public FSharpAsync<string> FSharpAsyncMethod(string parameter)
        {
            return FSharpAsync.AwaitTask(Task.FromResult(parameter));
        }

        public FSharpAsync<string> FSharpAsyncFailureMethod(string parameter)
        {
            return FSharpAsync.AwaitTask(
                Task.FromException<string>(new InvalidOperationException("Test exception")));
        }
    }

    public class TestAwaitable<T>
    {
        private T _result;
        private bool _isCompleted;
        private List<Action> _onCompletedCallbacks = new List<Action>();

        public TestAwaitable(T result)
        {
            _result = result;

            // Simulate a brief delay before completion
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(100);
                SetCompleted();
            });
        }

        private void SetCompleted()
        {
            _isCompleted = true;

            foreach (var callback in _onCompletedCallbacks)
            {
                callback();
            }
        }

        public TestAwaiter GetAwaiter()
        {
            return new TestAwaiter(this);
        }

        public struct TestAwaiter : INotifyCompletion
        {
            private TestAwaitable<T> _owner;

            public TestAwaiter(TestAwaitable<T> owner) : this()
            {
                _owner = owner;
            }

            public bool IsCompleted => _owner._isCompleted;

            public void OnCompleted(Action continuation)
            {
                if (_owner._isCompleted)
                {
                    continuation();
                }
                else
                {
                    _owner._onCompletedCallbacks.Add(continuation);
                }
            }

            public T GetResult()
            {
                return _owner._result;
            }
        }
    }

    public class TestAwaitableWithICriticalNotifyCompletion
    {
        public TestAwaiterWithICriticalNotifyCompletion GetAwaiter()
            => new TestAwaiterWithICriticalNotifyCompletion();
    }

    public class TestAwaitableWithoutICriticalNotifyCompletion
    {
        public TestAwaiterWithoutICriticalNotifyCompletion GetAwaiter()
            => new TestAwaiterWithoutICriticalNotifyCompletion();
    }

    public class TestAwaiterWithICriticalNotifyCompletion
        : CompletionTrackingAwaiterBase, ICriticalNotifyCompletion
    {
    }

    public class TestAwaiterWithoutICriticalNotifyCompletion
        : CompletionTrackingAwaiterBase, INotifyCompletion
    {
    }

    public class CompletionTrackingAwaiterBase
    {
        private string _result;

        public bool IsCompleted { get; private set; }

        public string GetResult() => _result;

        public void OnCompleted(Action continuation)
        {
            _result = "Used OnCompleted";
            IsCompleted = true;
            continuation();
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            _result = "Used UnsafeOnCompleted";
            IsCompleted = true;
            continuation();
        }
    }
}
