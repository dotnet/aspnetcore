// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
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
            var methodWithVoidReturnType = new MethodWithVoidReturnType(TestController.VoidAction);
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                        methodWithVoidReturnType.GetMethodInfo(),
                                                        null,
                                                        (IDictionary<string, object>)null);
            Assert.Same(null, result);
        }

        [Fact]
        public async Task AsyncAction_TaskReturnType()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskReturnType = new MethodWithTaskReturnType(_controller.TaskAction);
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                            methodWithTaskReturnType.GetMethodInfo(),
                                                            _controller,
                                                            actionParameters);
            Assert.Same(null, result);
        }

        [Fact]
        public async Task AsyncAction_TaskOfValueReturnType()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskValueTypeAction);
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                        methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                        _controller,
                                                        actionParameters);
            Assert.Equal(inputParam1, result);
        }

        [Fact]
        public async Task AsyncAction_TaskOfTaskOfValueReturnType()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfTaskOfIntReturnType = new MethodWithTaskOfTaskOfIntReturnType(_controller.TaskOfTaskAction);
            var result = await (Task<int>)(await ControllerActionExecutor.ExecuteAsync(
                                                                        methodWithTaskOfTaskOfIntReturnType.GetMethodInfo(),
                                                                        _controller,
                                                                        actionParameters));
            Assert.Equal(inputParam1, result);
        }

        [Fact]
        public async Task AsyncAction_WithAsyncKeywordThrows()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
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
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskActionWithExceptionWithoutAsync);
            await Assert.ThrowsAsync<NotImplementedException>(
                        () => ControllerActionExecutor.ExecuteAsync(methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                                   _controller,
                                                                   actionParameters));
        }

        [Fact]
        public async Task AsyncAction_WithExceptionsAfterAwait()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskActionThrowAfterAwait);
            await AssertThrowsAsync<ArgumentException>(
                                                async () =>
                                                    await ControllerActionExecutor.ExecuteAsync(
                                                                        methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                                        _controller,
                                                                        actionParameters),
                                                    "Argument Exception");
        }

        [Fact]
        public async Task SyncAction()
        {
            string inputString = "hello";
            var syncMethod = new SyncMethod(_controller.Echo);
            var result = await ControllerActionExecutor.ExecuteAsync(
                                                syncMethod.GetMethodInfo(),
                                                _controller,
                                                new Dictionary<string, object>() { { "input", inputString } });
            Assert.Equal(inputString, result);
        }

        [Fact]
        public async Task SyncAction_WithException()
        {
            string inputString = "hello";
            var syncMethod = new SyncMethod(_controller.EchoWithException);
            await Assert.ThrowsAsync<NotImplementedException>(
                        () => ControllerActionExecutor.ExecuteAsync(
                                                syncMethod.GetMethodInfo(),
                                                _controller,
                                                new Dictionary<string, object>() { { "input", inputString } }));
        }

        [Fact]
        public async Task AsyncAction_WithCustomTaskReturnTypeThrows()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            
            // If it is an unrecognized derived type we throw an InvalidOperationException.
            var methodWithCutomTaskReturnType = new MethodWithCustomTaskReturnType(_controller.TaskActionWithCustomTaskReturnType);

            string expectedException = string.Format(
                                                 CultureInfo.CurrentCulture,
                                                 "The method 'TaskActionWithCustomTaskReturnType' on type '{0}' returned a Task instance even though it is not an asynchronous method.",
                                                 typeof(TestController));
            await AssertThrowsAsync<InvalidOperationException>(
                                                async () => 
                                                    await ControllerActionExecutor.ExecuteAsync(
                                                                    methodWithCutomTaskReturnType.GetMethodInfo(), 
                                                                    _controller,
                                                                    actionParameters),
                                                 expectedException);
        }

        [Fact]
        public async Task AsyncAction_WithCustomTaskOfTReturnTypeThrows()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithCutomTaskOfTReturnType = new MethodWithCustomTaskOfTReturnType(_controller.TaskActionWithCustomTaskOfTReturnType);
            string expectedException = string.Format(
                                                   CultureInfo.CurrentCulture,
                                                   "The method 'TaskActionWithCustomTaskOfTReturnType' on type '{0}' returned a Task instance even though it is not an asynchronous method.",
                                                   typeof(TestController));

            await AssertThrowsAsync<InvalidOperationException>(
                                                async () =>
                                                    await ControllerActionExecutor.ExecuteAsync(
                                                                methodWithCutomTaskOfTReturnType.GetMethodInfo(),
                                                                _controller,
                                                                actionParameters),
                                               expectedException);
        }

        [Fact]
        public async Task AsyncAction_ReturningUnwrappedTaskThrows()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var methodWithUnwrappedTask = new MethodWithTaskReturnType(_controller.UnwrappedTask);
            await AssertThrowsAsync<InvalidOperationException>(
                                                async () => 
                                                    await ControllerActionExecutor.ExecuteAsync(
                                                                methodWithUnwrappedTask.GetMethodInfo(),
                                                                _controller,
                                                                actionParameters),
                                                                string.Format(CultureInfo.CurrentCulture,
                                                                             "The method 'UnwrappedTask' on type '{0}' returned an instance of '{1}'. Make sure to call Unwrap on the returned value to avoid unobserved faulted Task.",
                                                                             typeof(TestController),
                                                                             typeof(Task<Task>).FullName
                                                                             ));
        }

        [Fact]
        public async Task AsyncAction_WithDynamicReturnTypeThrows()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };

            var dynamicTaskMethod = new ReturnTaskAsDynamicValue(_controller.ReturnTaskAsDynamicValue);
            string expectedException = string.Format(
                                                  CultureInfo.CurrentCulture,
                                                  "The method 'ReturnTaskAsDynamicValue' on type '{0}' returned a Task instance even though it is not an asynchronous method.",
                                                  typeof(TestController));
            await AssertThrowsAsync<InvalidOperationException>(
                                                async () => 
                                                    await ControllerActionExecutor.ExecuteAsync(
                                                                dynamicTaskMethod.GetMethodInfo(),
                                                                _controller,
                                                                actionParameters),
                                                expectedException);
        }

        [Fact]
        public async Task ParametersInRandomOrder()
        {
            int inputParam1 = 1;
            string inputParam2 = "Second Parameter";

            // Note that the order of parameters is reversed
            var actionParameters = new Dictionary<string, object> { { "s", inputParam2 }, { "i", inputParam1 } };
            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskValueTypeAction);

            var result = await ControllerActionExecutor.ExecuteAsync(
                                                        methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                        _controller,
                                                        actionParameters);
            Assert.Equal(inputParam1, result);
        }

        [Fact]
        public async Task InvalidParameterValueThrows()
        {
            string inputParam2 = "Second Parameter";

            var actionParameters = new Dictionary<string, object> { { "i", "Some Invalid Value" }, { "s", inputParam2 } };
            var methodWithTaskOfIntReturnType = new MethodWithTaskOfIntReturnType(_controller.TaskValueTypeAction);
            var message = TestPlatformHelper.IsMono ? "Object type {0} cannot be converted to target type: {1}" :
                                                      "Object of type '{0}' cannot be converted to type '{1}'.";
            var expectedException = string.Format(
                                            CultureInfo.CurrentCulture,
                                            message,
                                            typeof (string),
                                            typeof (int));

            // If it is an unrecognized derived type we throw an InvalidOperationException.
            await AssertThrowsAsync<ArgumentException>(
                                                async () => 
                                                    await ControllerActionExecutor.ExecuteAsync(
                                                                                    methodWithTaskOfIntReturnType.GetMethodInfo(),
                                                                                    _controller, 
                                                                                    actionParameters),
                                                expectedException);
        }

        // TODO: XUnit Assert.Throw is not async-aware. Check if the latest version supports it.
        private static async Task AssertThrowsAsync<TException>(Func<Task<object>> func, string expectedExceptionMessage = "")
        {
            var expected = typeof(TException);
            Type actual = null;
            string actualExceptionMessage = string.Empty;
            try
            {
                var result = await func();
            }
            catch (Exception e)
            {
                actual = e.GetType();
                actualExceptionMessage = e.Message;
            }

            Assert.Equal(expected, actual);

            Assert.Equal(expectedExceptionMessage, actualExceptionMessage);
        }
    }
}