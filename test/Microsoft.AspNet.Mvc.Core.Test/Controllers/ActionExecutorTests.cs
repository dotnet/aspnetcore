// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Controllers
{
    public class ActionExecutorTests
    {
        private TestController _controller = new TestController();

        private delegate void MethodWithVoidReturnType();

        private delegate string SyncMethod(string s);

        private delegate Task MethodWithTaskReturnType(int i, string s);

        private delegate Task<int> MethodWithTaskOfIntReturnType(int i, string s);

        private delegate Task<Task<int>> MethodWithTaskOfTaskOfIntReturnType(int i, string s);

        public delegate TestController.TaskDerivedType MethodWithCustomTaskReturnType(int i, string s);

        private delegate TestController.TaskOfTDerivedType<int> MethodWithCustomTaskOfTReturnType(int i, string s);

        private delegate dynamic ReturnTaskAsDynamicValue(int i, string s);

        [Fact]
        public async Task AsyncAction_WithVoidReturnType()
        {
            // Arrange
            var methodWithVoidReturnType = new MethodWithVoidReturnType(TestController.VoidAction);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                        methodWithVoidReturnType.GetMethodInfo(),
                                                        null,
                                                        (IDictionary<string, object>)null);

            // Assert
            Assert.Same(null, result);
        }

        [Fact]
        public async Task AsyncAction_TaskReturnType()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskReturnType = new MethodWithTaskReturnType(_controller.TaskAction);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                            methodWithTaskReturnType.GetMethodInfo(),
                                                            _controller,
                                                            actionParameters);

            // Assert
            Assert.Same(null, result);
        }

        [Fact]
        public async Task AsyncAction_TaskOfValueReturnType()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskValueTypeAction);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                        methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                        _controller,
                                                        actionParameters);

            // Assert
            Assert.Equal(inputParam1, result);
        }

        [Fact]
        public async Task AsyncAction_TaskOfTaskOfValueReturnType()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfTaskOfIntReturnType = new MethodWithTaskOfTaskOfIntReturnType(_controller.TaskOfTaskAction);

            // Act
            var result = await (Task<int>)(await ControllerActionExecutor.ExecuteAsync(
                                                                        methodWithTaskOfTaskOfIntReturnType.GetMethodInfo(),
                                                                        _controller,
                                                                        actionParameters));

            // Assert
            Assert.Equal(inputParam1, result);
        }

        [Fact]
        public async Task AsyncAction_WithAsyncKeywordThrows()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskActionWithException);

            // Act and Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                    () => ControllerActionExecutor.ExecuteAsync(methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                               _controller,
                                                               actionParameters));
        }

        [Fact]
        public async Task AsyncAction_WithoutAsyncThrows()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskActionWithExceptionWithoutAsync);

            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                        () => ControllerActionExecutor.ExecuteAsync(methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                                   _controller,
                                                                   actionParameters));
        }

        [Fact]
        public async Task AsyncAction_WithExceptionsAfterAwait()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskActionThrowAfterAwait);
            var expectedException = "Argument Exception";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => ControllerActionExecutor.ExecuteAsync(
                    methodWithTaskOfIntReturnType.GetMethodInfo(),
                    _controller,
                    actionParameters));
            Assert.Equal(expectedException, ex.Message);
        }

        [Fact]
        public async Task SyncAction()
        {
            // Arrange
            var inputString = "hello";
            var syncMethod = new SyncMethod(_controller.Echo);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                syncMethod.GetMethodInfo(),
                                                _controller,
                                                new Dictionary<string, object>() { { "input", inputString } });

            // Assert
            Assert.Equal(inputString, result);
        }

        [Fact]
        public async Task SyncAction_WithException()
        {
            // Arrange
            var inputString = "hello";
            var syncMethod = new SyncMethod(_controller.EchoWithException);

            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                        () => ControllerActionExecutor.ExecuteAsync(
                                                syncMethod.GetMethodInfo(),
                                                _controller,
                                                new Dictionary<string, object>() { { "input", inputString } }));
        }

        [Fact]
        public async Task ExecuteAsync_WithArgumentDictionary_DefaultValueAttributeUsed()
        {
            // Arrange
            var syncMethod = new SyncMethod(_controller.EchoWithDefaultValue);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                syncMethod.GetMethodInfo(),
                _controller,
                new Dictionary<string, object>());

            // Assert
            Assert.Equal("hello", result);
        }

        [Fact]
        public async Task ExecuteAsync_WithArgumentArray_DefaultValueAttributeIgnored()
        {
            // Arrange
            var syncMethod = new SyncMethod(_controller.EchoWithDefaultValue);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                syncMethod.GetMethodInfo(),
                _controller,
                new object[] { null, });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithArgumentDictionary_DefaultParameterValueUsed()
        {
            // Arrange
            var syncMethod = new SyncMethod(_controller.EchoWithDefaultValueAndAttribute);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                syncMethod.GetMethodInfo(),
                _controller,
                new Dictionary<string, object>());

            // Assert
            Assert.Equal("world", result);
        }

        [Fact]
        public async Task ExecuteAsync_WithArgumentDictionary_AnyValue_HasPrecedenceOverDefaults()
        {
            // Arrange
            var syncMethod = new SyncMethod(_controller.EchoWithDefaultValueAndAttribute);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                syncMethod.GetMethodInfo(),
                _controller,
                new Dictionary<string, object>() { { "input", null } });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AsyncAction_WithCustomTaskReturnTypeThrows()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            // If it is an unrecognized derived type we throw an InvalidOperationException.
            var methodWithCutomTaskReturnType = new MethodWithCustomTaskReturnType(_controller.TaskActionWithCustomTaskReturnType);

            var expectedException = string.Format(
                CultureInfo.CurrentCulture,
                "The method 'TaskActionWithCustomTaskReturnType' on type '{0}' returned a Task instance even though it is not an asynchronous method.",
                typeof(TestController));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ControllerActionExecutor.ExecuteAsync(
                    methodWithCutomTaskReturnType.GetMethodInfo(),
                    _controller,
                    actionParameters));
            Assert.Equal(expectedException, ex.Message);
        }

        [Fact]
        public async Task AsyncAction_WithCustomTaskOfTReturnTypeThrows()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithCutomTaskOfTReturnType = new MethodWithCustomTaskOfTReturnType(_controller.TaskActionWithCustomTaskOfTReturnType);
            var expectedException = string.Format(
                CultureInfo.CurrentCulture,
                "The method 'TaskActionWithCustomTaskOfTReturnType' on type '{0}' returned a Task instance even though it is not an asynchronous method.",
                typeof(TestController));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ControllerActionExecutor.ExecuteAsync(
                    methodWithCutomTaskOfTReturnType.GetMethodInfo(),
                    _controller,
                    actionParameters));
            Assert.Equal(expectedException, ex.Message);
        }

        [Fact]
        public async Task AsyncAction_ReturningUnwrappedTaskThrows()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithUnwrappedTask = new MethodWithTaskReturnType(_controller.UnwrappedTask);

            var expectedException = string.Format(
                CultureInfo.CurrentCulture,
                "The method 'UnwrappedTask' on type '{0}' returned an instance of '{1}'. " +
                "Make sure to call Unwrap on the returned value to avoid unobserved faulted Task.",
                typeof(TestController),
                typeof(Task<Task>).FullName);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ControllerActionExecutor.ExecuteAsync(
                    methodWithUnwrappedTask.GetMethodInfo(),
                    _controller,
                    actionParameters));
            Assert.Equal(expectedException, ex.Message);
        }

        [Fact]
        public async Task AsyncAction_WithDynamicReturnTypeThrows()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var dynamicTaskMethod = new ReturnTaskAsDynamicValue(_controller.ReturnTaskAsDynamicValue);
            var expectedException = string.Format(
                CultureInfo.CurrentCulture,
                "The method 'ReturnTaskAsDynamicValue' on type '{0}' returned a Task instance even though it is not an asynchronous method.",
                typeof(TestController));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
               () => ControllerActionExecutor.ExecuteAsync(
                    dynamicTaskMethod.GetMethodInfo(),
                    _controller,
                    actionParameters));
            Assert.Equal(expectedException, ex.Message);
        }

        [Fact]
        public async Task ParametersInRandomOrder()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";

            // Note that the order of parameters is reversed
            var actionParameters = new Dictionary<string, object> { { "s", inputParam2 }, { "i", inputParam1 } };
            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskValueTypeAction);

            // Act
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                        methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                        _controller,
                                                        actionParameters);

            // Assert
            Assert.Equal(inputParam1, result);
        }

        [Fact]
        public async Task InvalidParameterValueThrows()
        {
            // Arrange
            var inputParam2 = "Second Parameter";

            var actionParameters = new Dictionary<string, object> { { "i", "Some Invalid Value" }, { "s", inputParam2 } };
            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskValueTypeAction);
            var message = TestPlatformHelper.IsMono ? "Object type {0} cannot be converted to target type: {1}" :
                                                      "Object of type '{0}' cannot be converted to type '{1}'.";
            var expectedException = string.Format(
                CultureInfo.CurrentCulture,
                message,
                typeof(string),
                typeof(int));

            // Act & Assert
            // If it is an unrecognized derived type we throw an InvalidOperationException.
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => ControllerActionExecutor.ExecuteAsync(
                        methodWithTaskOfIntReturnType.GetMethodInfo(),
                        _controller,
                        actionParameters));

            Assert.Equal(expectedException, ex.Message);
        }
    }
}