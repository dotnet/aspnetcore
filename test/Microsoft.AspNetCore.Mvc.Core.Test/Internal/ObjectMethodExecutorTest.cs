// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
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
                new object[] { 10 , 20 });
            Assert.Equal(30, (int)result);
        }

        [Fact]
        public void ExecuteVoidValueMethod()
        {
            var executor = GetExecutorForMethod("VoidValueMethod");
            var result = executor.Execute(
                _targetObject,
                new object[] { 10 });
            Assert.Same(null, result);
        }

        [Fact]
        public void ExecuteValueMethodWithReturnType()
        {
            var executor = GetExecutorForMethod("ValueMethodWithReturnType");
            var result = executor.Execute(
                _targetObject,
                new object[] { 10 });
            var resultObject = Assert.IsType<TestObject>(result);
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
            Assert.Equal("HelloWorld", resultObject.value);
        }

        [Fact]
        public void ExecuteValueMethodWithReturnTypeThrowsException()
        {
            var executor = GetExecutorForMethod("ValueMethodWithReturnTypeThrowsException");
            var parameter = new TestObject();
            Assert.Throws<NotImplementedException>(
                        () => executor.Execute(
                            _targetObject,
                            new object[] { parameter }));
        }

        [Fact]
        public async void ExecuteValueMethodUsingAsyncMethod()
        {
            var executor = GetExecutorForMethod("ValueMethod");
            var result = await executor.ExecuteAsync(
                _targetObject,
                new object[] { 10, 20 });
            Assert.Equal(30, (int)result);
        }

        [Fact]
        public async void ExecuteVoidValueMethodUsingAsyncMethod()
        {
            var executor = GetExecutorForMethod("VoidValueMethod");
            var result = await executor.ExecuteAsync(
                _targetObject,
                new object[] { 10 });
            Assert.Same(null, result);
        }

        [Fact]
        public async void ExecuteValueMethodAsync()
        {
            var executor = GetExecutorForMethod("ValueMethodAsync");
            var result = await executor.ExecuteAsync(
                _targetObject,
                new object[] { 10, 20 });
            Assert.Equal(30, (int)result);
        }

        [Fact]
        public async void ExecuteVoidValueMethodAsync()
        {
            var executor = GetExecutorForMethod("VoidValueMethodAsync");
            var result = await executor.ExecuteAsync(
                _targetObject,
                new object[] { 10 });
            Assert.Same(null, result);
        }

        [Fact]
        public async void ExecuteValueMethodWithReturnTypeAsync()
        {
            var executor = GetExecutorForMethod("ValueMethodWithReturnTypeAsync");
            var result = await executor.ExecuteAsync(
                _targetObject,
                new object[] { 10 });
            var resultObject = Assert.IsType<TestObject>(result);
            Assert.Equal("Hello", resultObject.value);
        }

        [Fact]
        public async void ExecuteValueMethodUpdateValueAsync()
        {
            var executor = GetExecutorForMethod("ValueMethodUpdateValueAsync");
            var parameter = new TestObject();
            var result = await executor.ExecuteAsync(
                _targetObject,
                new object[] { parameter });
            var resultObject = Assert.IsType<TestObject>(result);
            Assert.Equal("HelloWorld", resultObject.value);
        }

        [Fact]
        public async void ExecuteValueMethodWithReturnTypeThrowsExceptionAsync()
        {
            var executor = GetExecutorForMethod("ValueMethodWithReturnTypeThrowsExceptionAsync");
            var parameter = new TestObject();
            await Assert.ThrowsAsync<NotImplementedException>(
                    () => executor.ExecuteAsync(
                            _targetObject,
                            new object[] { parameter }));
        }

        [Fact]
        public void ExecuteMethodOfTaskDerivedTypeReturnTypeThrowsException()
        {
            var expectedException = string.Format(
                CultureInfo.CurrentCulture,
                "The method 'TaskActionWithCustomTaskReturnType' on type '{0}' returned a Task instance even though it is not an asynchronous method.",
                typeof(TestObject));

            var ex = Assert.Throws<InvalidOperationException>(
                    () => GetExecutorForMethod("TaskActionWithCustomTaskReturnType"));
            Assert.Equal(expectedException, ex.Message);
        }

        [Fact]
        public void ExecuteMethodOfTaskDerivedTypeOfTReturnTypeThrowsException()
        {
            var expectedException = string.Format(
                CultureInfo.CurrentCulture,
                "The method 'TaskActionWithCustomTaskOfTReturnType' on type '{0}' returned a Task instance even though it is not an asynchronous method.",
                typeof(TestObject));

            var ex = Assert.Throws<InvalidOperationException>(
                    () => GetExecutorForMethod("TaskActionWithCustomTaskOfTReturnType"));

            Assert.Equal(expectedException, ex.Message);
        }

        private ObjectMethodExecutor GetExecutorForMethod(string methodName)
        {
            var method = typeof(TestObject).GetMethod(methodName);
            var executor = ObjectMethodExecutor.Create(method, targetTypeInfo);
            return executor;
        }

        public class TestObject
        {            
            public string value;
            public int ValueMethod(int i, int j)
            {
                return i+j;
            }

            public void VoidValueMethod(int i)
            {
                
            }
            public TestObject ValueMethodWithReturnType(int i)
            {
                return new TestObject() { value = "Hello" }; ;
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
            public Task<TestObject> ValueMethodWithReturnTypeAsync(int i)
            {
                return Task.FromResult<TestObject>(new TestObject() { value = "Hello" });
            }

            public Task<TestObject> ValueMethodWithReturnTypeThrowsExceptionAsync(TestObject i)
            {
                throw new NotImplementedException("Not Implemented Exception");
            }

            public Task<TestObject> ValueMethodUpdateValueAsync(TestObject parameter)
            {
                parameter.value = "HelloWorld";
                return Task.FromResult<TestObject>(parameter);
            }

            public TaskDerivedType TaskActionWithCustomTaskReturnType(int i, string s)
            {
                return new TaskDerivedType();
            }

            public TaskOfTDerivedType<int> TaskActionWithCustomTaskOfTReturnType(int i, string s)
            {
                return new TaskOfTDerivedType<int>(1);
            }

            public class TaskDerivedType : Task
            {
                public TaskDerivedType()
                    : base(() => Console.WriteLine("In The Constructor"))
                {
                }
            }

            public class TaskOfTDerivedType<T> : Task<T>
            {
                public TaskOfTDerivedType(T input)
                    : base(() => input)
                {
                }
            }
        }
    }
}
