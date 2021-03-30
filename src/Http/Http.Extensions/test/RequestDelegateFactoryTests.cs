// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Internal
{
    public class RequestDelegateFactoryTests
    {
        public static IEnumerable<object[]> NoResult
        {
            get
            {
                void TestAction(HttpContext httpContext)
                {
                    MarkAsInvoked(httpContext);
                }

                Task TaskTestAction(HttpContext httpContext)
                {
                    MarkAsInvoked(httpContext);
                    return Task.CompletedTask;
                }

                ValueTask ValueTaskTestAction(HttpContext httpContext)
                {
                    MarkAsInvoked(httpContext);
                    return ValueTask.CompletedTask;
                }

                void StaticTestAction(HttpContext httpContext)
                {
                    MarkAsInvoked(httpContext);
                }

                Task StaticTaskTestAction(HttpContext httpContext)
                {
                    MarkAsInvoked(httpContext);
                    return Task.CompletedTask;
                }

                ValueTask StaticValueTaskTestAction(HttpContext httpContext)
                {
                    MarkAsInvoked(httpContext);
                    return ValueTask.CompletedTask;
                }

                void MarkAsInvoked(HttpContext httpContext)
                {
                    httpContext.Items.Add("invoked", true);
                }

                return new List<object[]>
                {
                    new object[] { (Action<HttpContext>)TestAction },
                    new object[] { (Func<HttpContext, Task>)TaskTestAction },
                    new object[] { (Func<HttpContext, ValueTask>)ValueTaskTestAction },
                    new object[] { (Action<HttpContext>)StaticTestAction },
                    new object[] { (Func<HttpContext, Task>)StaticTaskTestAction },
                    new object[] { (Func<HttpContext, ValueTask>)StaticValueTaskTestAction },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NoResult))]
        public async Task RequestDelegateInvokesAction(Delegate @delegate)
        {
            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            Assert.True(httpContext.Items["invoked"] as bool?);
        }

        private static void StaticTestActionBasicReflection(HttpContext httpContext)
        {
            httpContext.Items.Add("invoked", true);
        }

        [Fact]
        public async Task StaticMethodInfoOverloadWorksWithBasicReflection()
        {
            var methodInfo = typeof(RequestDelegateFactoryTests).GetMethod(
                nameof(StaticTestActionBasicReflection),
                BindingFlags.NonPublic | BindingFlags.Static,
                new[] { typeof(HttpContext) });

            var requestDelegate = RequestDelegateFactory.Create(methodInfo!);

            var httpContext = new DefaultHttpContext();

            await requestDelegate(httpContext);

            Assert.True(httpContext.Items["invoked"] as bool?);
        }

        private class TestNonStaticActionClass
        {
            private readonly object _invokedValue;

            public TestNonStaticActionClass(object invokedValue)
            {
                _invokedValue = invokedValue;
            }

            private void NonStaticTestAction(HttpContext httpContext)
            {
                httpContext.Items.Add("invoked", _invokedValue);
            }
        }

        [Fact]
        public async Task NonStaticMethodInfoOverloadWorksWithBasicReflection()
        {
            var methodInfo = typeof(TestNonStaticActionClass).GetMethod(
                "NonStaticTestAction",
                BindingFlags.NonPublic | BindingFlags.Instance,
                new[] { typeof(HttpContext) });

            var invoked = false;

            object GetTarget()
            {
                if (!invoked)
                {
                    invoked = true;
                    return new TestNonStaticActionClass(1);
                }

                return new TestNonStaticActionClass(2);
            }

            var requestDelegate = RequestDelegateFactory.Create(methodInfo!, _ => GetTarget());

            var httpContext = new DefaultHttpContext();

            await requestDelegate(httpContext);

            Assert.Equal(1, httpContext.Items["invoked"]);

            httpContext = new DefaultHttpContext();

            await requestDelegate(httpContext);

            Assert.Equal(2, httpContext.Items["invoked"]);
        }

        [Fact]
        public void BuildRequestDelegateThrowsArgumentNullExceptions()
        {
            var methodInfo = typeof(RequestDelegateFactoryTests).GetMethod(
                nameof(StaticTestActionBasicReflection),
                BindingFlags.NonPublic | BindingFlags.Static,
                new[] { typeof(HttpContext) });

            var exNullAction = Assert.Throws<ArgumentNullException>(() => RequestDelegateFactory.Create(action: null!));
            var exNullMethodInfo1 = Assert.Throws<ArgumentNullException>(() => RequestDelegateFactory.Create(methodInfo: null!));
            var exNullMethodInfo2 = Assert.Throws<ArgumentNullException>(() => RequestDelegateFactory.Create(methodInfo: null!, _ => 0));
            var exNullTargetFactory = Assert.Throws<ArgumentNullException>(() => RequestDelegateFactory.Create(methodInfo!, targetFactory: null!));

            Assert.Equal("action", exNullAction.ParamName);
            Assert.Equal("methodInfo", exNullMethodInfo1.ParamName);
            Assert.Equal("methodInfo", exNullMethodInfo2.ParamName);
            Assert.Equal("targetFactory", exNullTargetFactory.ParamName);
        }

        public static IEnumerable<object[]> FromRouteResult
        {
            get
            {
                void TestAction(HttpContext httpContext, [FromRoute] int value)
                {
                    StoreInput(httpContext, value);
                };

                Task TaskTestAction(HttpContext httpContext, [FromRoute] int value)
                {
                    StoreInput(httpContext, value);
                    return Task.CompletedTask;
                }

                ValueTask ValueTaskTestAction(HttpContext httpContext, [FromRoute] int value)
                {
                    StoreInput(httpContext, value);
                    return ValueTask.CompletedTask;
                }

                return new List<object[]>
                {
                    new object[] { (Action<HttpContext, int>)TestAction },
                    new object[] { (Func<HttpContext, int, Task>)TaskTestAction },
                    new object[] { (Func<HttpContext, int, ValueTask>)ValueTaskTestAction },
                };
            }
        }
        private static void StoreInput(HttpContext httpContext, object value)
        {
            httpContext.Items.Add("input", value);
        }

        [Theory]
        [MemberData(nameof(FromRouteResult))]
        public async Task RequestDelegatePopulatesFromRouteParameterBasedOnParameterName(Delegate @delegate)
        {
            const string paramName = "value";
            const int originalRouteParam = 42;

            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            Assert.Equal(originalRouteParam, httpContext.Items["input"] as int?);
        }

        public static IEnumerable<object[]> FromRouteOptionalResult
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { (Action<HttpContext, int>)TestAction },
                    new object[] { (Func<HttpContext, int, Task>)TaskTestAction },
                    new object[] { (Func<HttpContext, int, ValueTask>)ValueTaskTestAction }
                };
            }
        }

        private static void TestAction(HttpContext httpContext, [FromRoute] int value = 42)
        {
            StoreInput(httpContext, value);
        }

        private static Task TaskTestAction(HttpContext httpContext, [FromRoute] int value = 42)
        {
            StoreInput(httpContext, value);
            return Task.CompletedTask;
        }

        private static ValueTask ValueTaskTestAction(HttpContext httpContext, [FromRoute] int value = 42)
        {
            StoreInput(httpContext, value);
            return ValueTask.CompletedTask;
        }

        [Theory]
        [MemberData(nameof(FromRouteOptionalResult))]
        public async Task RequestDelegatePopulatesFromRouteOptionalParameter(Delegate @delegate)
        {
            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            Assert.Equal(42, httpContext.Items["input"] as int?);
        }

        [Theory]
        [MemberData(nameof(FromRouteOptionalResult))]
        public async Task RequestDelegatePopulatesFromRouteOptionalParameterBasedOnParameterName(Delegate @delegate)
        {
            const string paramName = "value";
            const int originalRouteParam = 47;

            var httpContext = new DefaultHttpContext();

            httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            Assert.Equal(47, httpContext.Items["input"] as int?);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromRouteParameterBasedOnAttributeNameProperty()
        {
            const string specifiedName = "value";
            const int originalRouteParam = 42;

            int? deserializedRouteParam = null;

            void TestAction([FromRoute(Name = specifiedName)] int foo)
            {
                deserializedRouteParam = foo;
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues[specifiedName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

            var requestDelegate = RequestDelegateFactory.Create((Action<int>)TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(originalRouteParam, deserializedRouteParam);
        }

        [Fact]
        public async Task UsesDefaultValueIfNoMatchingRouteValue()
        {
            const string unmatchedName = "value";
            const int unmatchedRouteParam = 42;

            int? deserializedRouteParam = null;

            void TestAction([FromRoute] int foo)
            {
                deserializedRouteParam = foo;
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues[unmatchedName] = unmatchedRouteParam.ToString(NumberFormatInfo.InvariantInfo);

            var requestDelegate = RequestDelegateFactory.Create((Action<int>)TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(0, deserializedRouteParam);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromQueryParameterBasedOnParameterName()
        {
            const string paramName = "value";
            const int originalQueryParam = 42;

            int? deserializedRouteParam = null;

            void TestAction([FromQuery] int value)
            {
                deserializedRouteParam = value;
            }

            var query = new QueryCollection(new Dictionary<string, StringValues>()
            {
                [paramName] = originalQueryParam.ToString(NumberFormatInfo.InvariantInfo)
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Query = query;

            var requestDelegate = RequestDelegateFactory.Create((Action<int>)TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(originalQueryParam, deserializedRouteParam);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromHeaderParameterBasedOnParameterName()
        {
            const string customHeaderName = "X-Custom-Header";
            const int originalHeaderParam = 42;

            int? deserializedRouteParam = null;

            void TestAction([FromHeader(Name = customHeaderName)] int value)
            {
                deserializedRouteParam = value;
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[customHeaderName] = originalHeaderParam.ToString(NumberFormatInfo.InvariantInfo);

            var requestDelegate = RequestDelegateFactory.Create((Action<int>)TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(originalHeaderParam, deserializedRouteParam);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromBodyParameter()
        {
            Todo originalTodo = new()
            {
                Name = "Write more tests!"
            };

            Todo? deserializedRequestBody = null;

            void TestAction([FromBody] Todo todo)
            {
                deserializedRequestBody = todo;
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";

            var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
            httpContext.Request.Body = new MemoryStream(requestBodyBytes);

            var requestDelegate = RequestDelegateFactory.Create((Action<Todo>)TestAction);

            await requestDelegate(httpContext);

            Assert.NotNull(deserializedRequestBody);
            Assert.Equal(originalTodo.Name, deserializedRequestBody!.Name);
        }

        [Fact]
        public async Task RequestDelegateRejectsEmptyBodyGivenDefaultFromBodyParameter()
        {
            void TestAction([FromBody] Todo todo)
            {
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Headers["Content-Length"] = "0";

            var requestDelegate = RequestDelegateFactory.Create((Action<Todo>)TestAction);

            await Assert.ThrowsAsync<JsonException>(() => requestDelegate(httpContext));
        }

        [Fact]
        public async Task RequestDelegateAllowsEmptyBodyGivenCorrectyConfiguredFromBodyParameter()
        {
            var todoToBecomeNull = new Todo();

            void TestAction([FromBody(AllowEmpty = true)] Todo todo)
            {
                todoToBecomeNull = todo;
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Headers["Content-Length"] = "0";

            var requestDelegate = RequestDelegateFactory.Create((Action<Todo>)TestAction);

            await requestDelegate(httpContext);

            Assert.Null(todoToBecomeNull);
        }

        [Fact]
        public async Task RequestDelegateAllowsEmptyBodyStructGivenCorrectyConfiguredFromBodyParameter()
        {
            var structToBeZeroed = new BodyStruct
            {
                Id = 42
            };

            void TestAction([FromBody(AllowEmpty = true)] BodyStruct bodyStruct)
            {
                structToBeZeroed = bodyStruct;
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Headers["Content-Length"] = "0";

            var requestDelegate = RequestDelegateFactory.Create((Action<BodyStruct>)TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(default, structToBeZeroed);
        }

        [Fact]
        public async Task RequestDelegateLogsFromBodyIOExceptionsAsDebugAndAborts()
        {
            var invoked = false;

            var sink = new TestSink(context => context.LoggerName == "Microsoft.AspNetCore.Routing.MapAction");
            var testLoggerFactory = new TestLoggerFactory(sink, enabled: true);

            void TestAction([FromBody] Todo todo)
            {
                invoked = true;
            }

            var ioException = new IOException();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory>(testLoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Body = new IOExceptionThrowingRequestBodyStream(ioException);
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new TestHttpRequestLifetimeFeature());
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create((Action<Todo>)TestAction);

            await requestDelegate(httpContext);

            Assert.False(invoked);
            Assert.True(httpContext.RequestAborted.IsCancellationRequested);

            var logMessage = Assert.Single(sink.Writes);
            Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Same(ioException, logMessage.Exception);
        }

        [Fact]
        public async Task RequestDelegateLogsFromBodyInvalidDataExceptionsAsDebugAndSets400Response()
        {
            var invoked = false;

            var sink = new TestSink(context => context.LoggerName == "Microsoft.AspNetCore.Routing.MapAction");
            var testLoggerFactory = new TestLoggerFactory(sink, enabled: true);

            void TestAction([FromBody] Todo todo)
            {
                invoked = true;
            }

            var invalidDataException = new InvalidDataException();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory>(testLoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Body = new IOExceptionThrowingRequestBodyStream(invalidDataException);
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new TestHttpRequestLifetimeFeature());
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create((Action<Todo>)TestAction);

            await requestDelegate(httpContext);

            Assert.False(invoked);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            Assert.Equal(400, httpContext.Response.StatusCode);

            var logMessage = Assert.Single(sink.Writes);
            Assert.Equal(new EventId(2, "RequestBodyInvalidDataException"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Same(invalidDataException, logMessage.Exception);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromFormParameterBasedOnParameterName()
        {
            const string paramName = "value";
            const int originalQueryParam = 42;

            int? deserializedRouteParam = null;

            void TestAction([FromForm] int value)
            {
                deserializedRouteParam = value;
            }

            var form = new FormCollection(new Dictionary<string, StringValues>()
            {
                [paramName] = originalQueryParam.ToString(NumberFormatInfo.InvariantInfo)
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Form = form;

            var requestDelegate = RequestDelegateFactory.Create((Action<int>)TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(originalQueryParam, deserializedRouteParam);
        }

        [Fact]
        public async Task RequestDelegateLogsFromFormIOExceptionsAsDebugAndAborts()
        {
            var invoked = false;

            var sink = new TestSink(context => context.LoggerName == "Microsoft.AspNetCore.Routing.MapAction");
            var testLoggerFactory = new TestLoggerFactory(sink, enabled: true);

            void TestAction([FromForm] int value)
            {
                invoked = true;
            }

            var ioException = new IOException();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory>(testLoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            httpContext.Request.Body = new IOExceptionThrowingRequestBodyStream(ioException);
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new TestHttpRequestLifetimeFeature());
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create((Action<int>)TestAction);

            await requestDelegate(httpContext);

            Assert.False(invoked);
            Assert.True(httpContext.RequestAborted.IsCancellationRequested);

            var logMessage = Assert.Single(sink.Writes);
            Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Same(ioException, logMessage.Exception);
        }

        [Fact]
        public async Task RequestDelegateLogsFromFormInvalidDataExceptionsAsDebugAndSets400Response()
        {
            var invoked = false;

            var sink = new TestSink(context => context.LoggerName == "Microsoft.AspNetCore.Routing.MapAction");
            var testLoggerFactory = new TestLoggerFactory(sink, enabled: true);

            void TestAction([FromForm] int value)
            {
                invoked = true;
            }

            var invalidDataException = new InvalidDataException();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory>(testLoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            httpContext.Request.Body = new IOExceptionThrowingRequestBodyStream(invalidDataException);
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new TestHttpRequestLifetimeFeature());
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create((Action<int>)TestAction);

            await requestDelegate(httpContext);

            Assert.False(invoked);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            Assert.Equal(400, httpContext.Response.StatusCode);

            var logMessage = Assert.Single(sink.Writes);
            Assert.Equal(new EventId(2, "RequestBodyInvalidDataException"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Same(invalidDataException, logMessage.Exception);
        }

        [Fact]
        public void BuildRequestDelegateThrowsInvalidOperationExceptionGivenBothFromBodyAndFromFormOnDifferentParameters()
        {
            void TestAction([FromBody] int value1, [FromForm] int value2) { }
            void TestActionWithFlippedParams([FromForm] int value1, [FromBody] int value2) { }

            Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create((Action<int, int>)TestAction));
            Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create((Action<int, int>)TestActionWithFlippedParams));
        }

        [Fact]
        public void BuildRequestDelegateThrowsInvalidOperationExceptionGivenFromBodyOnMultipleParameters()
        {
            void TestAction([FromBody] int value1, [FromBody] int value2) { }

            Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create((Action<int, int>)TestAction));
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromServiceParameterBasedOnParameterType()
        {
            var myOriginalService = new MyService();
            MyService? injectedService = null;

            void TestAction([FromService] MyService myService)
            {
                injectedService = myService;
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(myOriginalService);

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create((Action<MyService>)TestAction);

            await requestDelegate(httpContext);

            Assert.Same(myOriginalService, injectedService);
        }

        [Fact]
        public async Task RequestDelegatePopulatesHttpContextParameterWithoutAttribute()
        {
            HttpContext? httpContextArgument = null;

            void TestAction(HttpContext httpContext)
            {
                httpContextArgument = httpContext;
            }

            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create((Action<HttpContext>)TestAction);

            await requestDelegate(httpContext);

            Assert.Same(httpContext, httpContextArgument);
        }

        [Fact]
        public async Task RequestDelegatePopulatesIFormCollectionParameterWithoutAttribute()
        {
            IFormCollection? formCollectionArgument = null;

            void TestAction(IFormCollection httpContext)
            {
                formCollectionArgument = httpContext;
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";

            var requestDelegate = RequestDelegateFactory.Create((Action<IFormCollection>)TestAction);

            await requestDelegate(httpContext);

            Assert.Same(httpContext.Request.Form, formCollectionArgument);
        }

        [Fact]
        public async Task RequestDelegatePassHttpContextRequestAbortedAsCancelationToken()
        {
            CancellationToken? cancellationTokenArgument = null;

            void TestAction(CancellationToken cancellationToken)
            {
                cancellationTokenArgument = cancellationToken;
            }

            using var cts = new CancellationTokenSource();
            var httpContext = new DefaultHttpContext
            {
                RequestAborted = cts.Token
            };

            var requestDelegate = RequestDelegateFactory.Create((Action<CancellationToken>)TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(httpContext.RequestAborted, cancellationTokenArgument);
        }

        public static IEnumerable<object[]> ComplexResult
        {
            get
            {
                Todo originalTodo = new()
                {
                    Name = "Write even more tests!"
                };

                Todo TestAction() => originalTodo;
                Task<Todo> TaskTestAction() => Task.FromResult(originalTodo);
                ValueTask<Todo> ValueTaskTestAction() => ValueTask.FromResult(originalTodo);

                static Todo StaticTestAction() => new Todo { Name = "Write even more tests!" };
                static Task<Todo> StaticTaskTestAction() => Task.FromResult(new Todo { Name = "Write even more tests!" });
                static ValueTask<Todo> StaticValueTaskTestAction() => ValueTask.FromResult(new Todo { Name = "Write even more tests!" });

                return new List<object[]>
                {
                    new object[] { (Func<Todo>)TestAction },
                    new object[] { (Func<Task<Todo>>)TaskTestAction},
                    new object[] { (Func<ValueTask<Todo>>)ValueTaskTestAction},
                    new object[] { (Func<Todo>)StaticTestAction},
                    new object[] { (Func<Task<Todo>>)StaticTaskTestAction},
                    new object[] { (Func<ValueTask<Todo>>)StaticValueTaskTestAction},
                };
            }
        }

        [Theory]
        [MemberData(nameof(ComplexResult))]
        public async Task RequestDelegateWritesComplexReturnValueAsJsonResponseBody(Delegate @delegate)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var deserializedResponseBody = JsonSerializer.Deserialize<Todo>(responseBodyStream.ToArray(), new JsonSerializerOptions
            {
                // TODO: the output is "{\"id\":0,\"name\":\"Write even more tests!\",\"isComplete\":false}"
                // Verify that the camelCased property names are consistent with MVC and if so whether we should keep the behavior.
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(deserializedResponseBody);
            Assert.Equal("Write even more tests!", deserializedResponseBody!.Name);
        }

        public static IEnumerable<object[]> CustomResults
        {
            get
            {
                var resultString = "Still not enough tests!";

                CustomResult TestAction() => new CustomResult(resultString);
                Task<CustomResult> TaskTestAction() => Task.FromResult(new CustomResult(resultString));
                ValueTask<CustomResult> ValueTaskTestAction() => ValueTask.FromResult(new CustomResult(resultString));

                static CustomResult StaticTestAction() => new CustomResult("Still not enough tests!");
                static Task<CustomResult> StaticTaskTestAction() => Task.FromResult(new CustomResult("Still not enough tests!"));
                static ValueTask<CustomResult> StaticValueTaskTestAction() => ValueTask.FromResult(new CustomResult("Still not enough tests!"));

                return new List<object[]>
                {
                    new object[] { (Func<CustomResult>)TestAction },
                    new object[] { (Func<Task<CustomResult>>)TaskTestAction},
                    new object[] { (Func<ValueTask<CustomResult>>)ValueTaskTestAction},
                    new object[] { (Func<CustomResult>)StaticTestAction},
                    new object[] { (Func<Task<CustomResult>>)StaticTaskTestAction},
                    new object[] { (Func<ValueTask<CustomResult>>)StaticValueTaskTestAction},
                };
            }
        }

        [Theory]
        [MemberData(nameof(CustomResults))]
        public async Task RequestDelegateUsesCustomIResult(Delegate @delegate)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

            Assert.Equal("Still not enough tests!", decodedResponseBody);
        }

        public static IEnumerable<object[]> StringResult
        {
            get
            {
                var test = "String Test";

                string TestAction() => test;
                Task<string> TaskTestAction() => Task.FromResult(test);
                ValueTask<string> ValueTaskTestAction() => ValueTask.FromResult(test);

                static string StaticTestAction() => "String Test";
                static Task<string> StaticTaskTestAction() => Task.FromResult("String Test");
                static ValueTask<string> StaticValueTaskTestAction() => ValueTask.FromResult("String Test");

                return new List<object[]>
                {
                    new object[] { (Func<string>)TestAction },
                    new object[] { (Func<Task<string>>)TaskTestAction },
                    new object[] { (Func<ValueTask<string>>)ValueTaskTestAction },
                    new object[] { (Func<string>)StaticTestAction },
                    new object[] { (Func<Task<string>>)StaticTaskTestAction },
                    new object[] { (Func<ValueTask<string>>)StaticValueTaskTestAction },
                };
            }
        }

        [Theory]
        [MemberData(nameof(StringResult))]
        public async Task RequestDelegateWritesStringReturnValueAsJsonResponseBody(Delegate @delegate)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

            Assert.Equal("String Test", responseBody);
        }

        public static IEnumerable<object[]> IntResult
        {
            get
            {
                int TestAction() => 42;
                Task<int> TaskTestAction() => Task.FromResult(42);
                ValueTask<int> ValueTaskTestAction() => ValueTask.FromResult(42);

                static int StaticTestAction() => 42;
                static Task<int> StaticTaskTestAction() => Task.FromResult(42);
                static ValueTask<int> StaticValueTaskTestAction() => ValueTask.FromResult(42);

                return new List<object[]>
                {
                    new object[] { (Func<int>)TestAction },
                    new object[] { (Func<Task<int>>)TaskTestAction },
                    new object[] { (Func<ValueTask<int>>)ValueTaskTestAction },
                    new object[] { (Func<int>)StaticTestAction },
                    new object[] { (Func<Task<int>>)StaticTaskTestAction },
                    new object[] { (Func<ValueTask<int>>)StaticValueTaskTestAction },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IntResult))]
        public async Task RequestDelegateWritesIntReturnValue(Delegate @delegate)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

            Assert.Equal("42", responseBody);
        }

        public static IEnumerable<object[]> BoolResult
        {
            get
            {
                bool TestAction() => true;
                Task<bool> TaskTestAction() => Task.FromResult(true);
                ValueTask<bool> ValueTaskTestAction() => ValueTask.FromResult(true);

                static bool StaticTestAction() => true;
                static Task<bool> StaticTaskTestAction() => Task.FromResult(true);
                static ValueTask<bool> StaticValueTaskTestAction() => ValueTask.FromResult(true);

                return new List<object[]>
                {
                    new object[] { (Func<bool>)TestAction },
                    new object[] { (Func<Task<bool>>)TaskTestAction },
                    new object[] { (Func<ValueTask<bool>>)ValueTaskTestAction },
                    new object[] { (Func<bool>)StaticTestAction },
                    new object[] { (Func<Task<bool>>)StaticTaskTestAction },
                    new object[] { (Func<ValueTask<bool>>)StaticValueTaskTestAction },
                };
            }
        }

        [Theory]
        [MemberData(nameof(BoolResult))]
        public async Task RequestDelegateWritesBoolReturnValue(Delegate @delegate)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

            Assert.Equal("true", responseBody);
        }

        private class Todo
        {
            public int Id { get; set; }
            public string? Name { get; set; } = "Todo";
            public bool IsComplete { get; set; }
        }

        private struct BodyStruct
        {
            public int Id { get; set; }
        }

        private class FromRouteAttribute : Attribute, IFromRouteMetadata
        {
            public string? Name { get; set; }
        }

        private class FromQueryAttribute : Attribute, IFromQueryMetadata
        {
            public string? Name { get; set; }
        }

        private class FromHeaderAttribute : Attribute, IFromHeaderMetadata
        {
            public string? Name { get; set; }
        }

        private class FromBodyAttribute : Attribute, IFromBodyMetadata
        {
            public bool AllowEmpty { get; set; }
        }

        private class FromFormAttribute : Attribute, IFromFormMetadata
        {
            public string? Name { get; set; }
        }

        private class FromServiceAttribute : Attribute, IFromServiceMetadata
        {
        }

        private class MyService
        {
        }

        private class CustomResult : IResult
        {
            private readonly string _resultString;

            public CustomResult(string resultString)
            {
                _resultString = resultString;
            }

            public Task ExecuteAsync(HttpContext httpContext)
            {
                return httpContext.Response.WriteAsync(_resultString);
            }
        }

        private class IOExceptionThrowingRequestBodyStream : Stream
        {
            private readonly Exception _exceptionToThrow;

            public IOExceptionThrowingRequestBodyStream(Exception exceptionToThrow)
            {
                _exceptionToThrow = exceptionToThrow;
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotImplementedException();

            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw _exceptionToThrow;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }

        private class TestHttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
        {
            private readonly CancellationTokenSource _requestAbortedCts = new CancellationTokenSource();

            public CancellationToken RequestAborted { get => _requestAbortedCts.Token; set => throw new NotImplementedException(); }

            public void Abort()
            {
                _requestAbortedCts.Cancel();
            }
        }
    }
}
