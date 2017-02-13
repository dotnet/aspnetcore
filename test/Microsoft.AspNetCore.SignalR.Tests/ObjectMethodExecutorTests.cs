// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
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
                new object[] { 10, 20 });
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
        public void ExecuteStaticValueMethodWithReturnType()
        {
            var executor = GetExecutorForMethod("StaticValueMethodWithReturnType");
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
            Assert.ThrowsAny<NotImplementedException>(
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
            Assert.Equal("HelloWorld", resultObject.value);
        }

        [Fact]
        public async Task ExecuteValueMethodWithReturnTypeThrowsExceptionAsync()
        {
            var executor = GetExecutorForMethod("ValueMethodWithReturnTypeThrowsExceptionAsync");
            var parameter = new TestObject();
            await Assert.ThrowsAsync<NotImplementedException>(
                    () => executor.ExecuteAsync(
                            _targetObject,
                            new object[] { parameter }));
        }

        [Theory]
        [InlineData("EchoWithDefaultAttributes", new object[] { "hello", true, 10 })]
        [InlineData("EchoWithDefaultValues", new object[] { "hello", true, 20 })]
        [InlineData("EchoWithDefaultValuesAndAttributes", new object[] { "hello", 20 })]
        [InlineData("EchoWithNoDefaultAttributesAndValues", new object[] { null, 0, false, null })]
        [InlineData("StaticEchoWithDefaultVaules", new object[] { "hello", true, 20 })]
        public void GetDefaultValueForParameters_ReturnsExpectedValues(string methodName, object[] expectedValues)
        {
            var executor = GetExecutorForMethod(methodName);
            var defaultValues = new object[expectedValues.Length];

            for (var index = 0; index < expectedValues.Length; index++)
            {
                defaultValues[index] = executor.GetDefaultValueForParameter(index);
            }

            Assert.Equal(expectedValues, defaultValues);
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

            public static TestObject StaticValueMethodWithReturnType(int i)
            {
                return new TestObject() { value = "Hello" };
            }

            public string EchoWithDefaultAttributes(
                [DefaultValue("hello")] string input1,
                [DefaultValue(true)] bool input2,
                [DefaultValue(10)] int input3)
            {
                return input1;
            }

            public string EchoWithDefaultValues(
                string input1 = "hello",
                bool input2 = true,
                int input3 = 20)
            {
                return input1;
            }

            public string EchoWithDefaultValuesAndAttributes(
                [DefaultValue("Hi")] string input1 = "hello",
                [DefaultValue(10)] int input3 = 20)
            {
                return input1;
            }

            public string EchoWithNoDefaultAttributesAndValues(
                string input1,
                int input2,
                bool input3,
                TestObject input4)
            {
                return input1;
            }

            public static string StaticEchoWithDefaultVaules(string input1 = "hello",
                bool input2 = true,
                int input3 = 20)
            {
                return input1;
            }
        }
    }
}
