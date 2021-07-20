// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Internal
{
    public class RequestDelegateFactoryTests : LoggedTest
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

            public void NonStaticTestAction(HttpContext httpContext)
            {
                httpContext.Items.Add("invoked", _invokedValue);
            }
        }

        [Fact]
        public async Task NonStaticMethodInfoOverloadWorksWithBasicReflection()
        {
            var methodInfo = typeof(TestNonStaticActionClass).GetMethod(
                nameof(TestNonStaticActionClass.NonStaticTestAction),
                BindingFlags.Public | BindingFlags.Instance,
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

            var serviceProvider = new EmptyServiceProvider();

            var exNullAction = Assert.Throws<ArgumentNullException>(() => RequestDelegateFactory.Create(action: null!));
            var exNullMethodInfo1 = Assert.Throws<ArgumentNullException>(() => RequestDelegateFactory.Create(methodInfo: null!));

            Assert.Equal("action", exNullAction.ParamName);
            Assert.Equal("methodInfo", exNullMethodInfo1.ParamName);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromRouteParameterBasedOnParameterName()
        {
            const string paramName = "value";
            const int originalRouteParam = 42;

            static void TestAction(HttpContext httpContext, [FromRoute] int value)
            {
                httpContext.Items.Add("input", value);
            }

            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(originalRouteParam, httpContext.Items["input"]);
        }

        private static void TestOptional(HttpContext httpContext, [FromRoute] int value = 42)
        {
            httpContext.Items.Add("input", value);
        }

        private static void TestOptionalNullable(HttpContext httpContext, int? value = 42)
        {
            httpContext.Items.Add("input", value);
        }

        private static void TestOptionalString(HttpContext httpContext, string value = "default")
        {
            httpContext.Items.Add("input", value);
        }

        [Fact]
        public async Task SpecifiedRouteParametersDoNotFallbackToQueryString()
        {
            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create((int? id, HttpContext httpContext) =>
            {
                if (id is not null)
                {
                    httpContext.Items["input"] = id;
                }
            },
            new() { RouteParameterNames = new string[] { "id" } });

            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["id"] = "42"
            });

            await requestDelegate(httpContext);

            Assert.Null(httpContext.Items["input"]);
        }

        [Fact]
        public async Task CreatingDelegateWithInstanceMethodInfoCreatesInstancePerCall()
        {
            var methodInfo = typeof(HttpHandler).GetMethod(nameof(HttpHandler.Handle));

            Assert.NotNull(methodInfo);

            var requestDelegate = RequestDelegateFactory.Create(methodInfo!);
            var context = new DefaultHttpContext();

            await requestDelegate(context);

            Assert.Equal(1, context.Items["calls"]);

            await requestDelegate(context);

            Assert.Equal(1, context.Items["calls"]);
        }

        [Fact]
        public void SpecifiedEmptyRouteParametersThrowIfRouteParameterDoesNotExist()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                RequestDelegateFactory.Create(([FromRoute] int id) => { }, new() { RouteParameterNames = Array.Empty<string>() }));

            Assert.Equal("id is not a route paramter.", ex.Message);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromRouteOptionalParameter()
        {
            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create(TestOptional);

            await requestDelegate(httpContext);

            Assert.Equal(42, httpContext.Items["input"]);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromNullableOptionalParameter()
        {
            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create(TestOptional);

            await requestDelegate(httpContext);

            Assert.Equal(42, httpContext.Items["input"]);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromOptionalStringParameter()
        {
            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create(TestOptionalString);

            await requestDelegate(httpContext);

            Assert.Equal("default", httpContext.Items["input"]);
        }

        [Fact]
        public async Task RequestDelegatePopulatesFromRouteOptionalParameterBasedOnParameterName()
        {
            const string paramName = "value";
            const int originalRouteParam = 47;

            var httpContext = new DefaultHttpContext();

            httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

            var requestDelegate = RequestDelegateFactory.Create(TestOptional);

            await requestDelegate(httpContext);

            Assert.Equal(47, httpContext.Items["input"]);
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

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(originalRouteParam, deserializedRouteParam);
        }

        [Fact]
        public async Task Returns400IfNoMatchingRouteValueForRequiredParam()
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

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(400, httpContext.Response.StatusCode);
        }

        public static object?[][] TryParsableParameters
        {
            get
            {
                static void Store<T>(HttpContext httpContext, T tryParsable)
                {
                    httpContext.Items["tryParsable"] = tryParsable;
                }

                var now = DateTime.Now;

                return new[]
                {
                    // string is not technically "TryParsable", but it's the special case.
                    new object[] { (Action<HttpContext, string>)Store, "plain string", "plain string" },
                    new object[] { (Action<HttpContext, int>)Store, "-42", -42 },
                    new object[] { (Action<HttpContext, uint>)Store, "42", 42U },
                    new object[] { (Action<HttpContext, bool>)Store, "true", true },
                    new object[] { (Action<HttpContext, short>)Store, "-42", (short)-42 },
                    new object[] { (Action<HttpContext, ushort>)Store, "42", (ushort)42 },
                    new object[] { (Action<HttpContext, long>)Store, "-42", -42L },
                    new object[] { (Action<HttpContext, ulong>)Store, "42", 42UL },
                    new object[] { (Action<HttpContext, IntPtr>)Store, "-42", new IntPtr(-42) },
                    new object[] { (Action<HttpContext, char>)Store, "A", 'A' },
                    new object[] { (Action<HttpContext, double>)Store, "0.5", 0.5 },
                    new object[] { (Action<HttpContext, float>)Store, "0.5", 0.5f },
                    new object[] { (Action<HttpContext, Half>)Store, "0.5", (Half)0.5f },
                    new object[] { (Action<HttpContext, decimal>)Store, "0.5", 0.5m },
                    new object[] { (Action<HttpContext, DateTime>)Store, now.ToString("o"), now },
                    new object[] { (Action<HttpContext, DateTimeOffset>)Store, "1970-01-01T00:00:00.0000000+00:00", DateTimeOffset.UnixEpoch },
                    new object[] { (Action<HttpContext, TimeSpan>)Store, "00:00:42", TimeSpan.FromSeconds(42) },
                    new object[] { (Action<HttpContext, Guid>)Store, "00000000-0000-0000-0000-000000000000", Guid.Empty },
                    new object[] { (Action<HttpContext, Version>)Store, "6.0.0.42", new Version("6.0.0.42") },
                    new object[] { (Action<HttpContext, BigInteger>)Store, "-42", new BigInteger(-42) },
                    new object[] { (Action<HttpContext, IPAddress>)Store, "127.0.0.1", IPAddress.Loopback },
                    new object[] { (Action<HttpContext, IPEndPoint>)Store, "127.0.0.1:80", new IPEndPoint(IPAddress.Loopback, 80) },
                    new object[] { (Action<HttpContext, AddressFamily>)Store, "Unix", AddressFamily.Unix },
                    new object[] { (Action<HttpContext, ILOpCode>)Store, "Nop", ILOpCode.Nop },
                    new object[] { (Action<HttpContext, AssemblyFlags>)Store, "PublicKey,Retargetable", AssemblyFlags.PublicKey | AssemblyFlags.Retargetable },
                    new object[] { (Action<HttpContext, int?>)Store, "42", 42 },
                    new object[] { (Action<HttpContext, MyEnum>)Store, "ValueB", MyEnum.ValueB },
                    new object[] { (Action<HttpContext, MyTryParsableRecord>)Store, "https://example.org", new MyTryParsableRecord(new Uri("https://example.org")) },
                    new object?[] { (Action<HttpContext, int?>)Store, null, null },
                };
            }
        }

        private enum MyEnum { ValueA, ValueB, }

        private record MyTryParsableRecord(Uri Uri)
        {
            public static bool TryParse(string? value, out MyTryParsableRecord? result)
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                {
                    result = null;
                    return false;
                }

                result = new MyTryParsableRecord(uri);
                return true;
            }
        }

        [Theory]
        [MemberData(nameof(TryParsableParameters))]
        public async Task RequestDelegatePopulatesUnattributedTryParsableParametersFromRouteValue(Delegate action, string? routeValue, object? expectedParameterValue)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues["tryParsable"] = routeValue;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(action);

            await requestDelegate(httpContext);

            Assert.Equal(expectedParameterValue, httpContext.Items["tryParsable"]);
        }

        [Theory]
        [MemberData(nameof(TryParsableParameters))]
        public async Task RequestDelegatePopulatesUnattributedTryParsableParametersFromQueryString(Delegate action, string? routeValue, object? expectedParameterValue)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["tryParsable"] = routeValue
            });

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(action);

            await requestDelegate(httpContext);

            Assert.Equal(expectedParameterValue, httpContext.Items["tryParsable"]);
        }

        [Fact]
        public async Task RequestDelegatePopulatesUnattributedTryParsableParametersFromRouteValueBeforeQueryString()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.RouteValues["tryParsable"] = "42";

            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["tryParsable"] = "invalid!"
            });

            var requestDelegate = RequestDelegateFactory.Create((HttpContext httpContext, int tryParsable) =>
            {
                httpContext.Items["tryParsable"] = tryParsable;
            });

            await requestDelegate(httpContext);

            Assert.Equal(42, httpContext.Items["tryParsable"]);
        }

        public static object[][] DelegatesWithAttributesOnNotTryParsableParameters
        {
            get
            {
                void InvalidFromRoute([FromRoute] object notTryParsable) { }
                void InvalidFromQuery([FromQuery] object notTryParsable) { }
                void InvalidFromHeader([FromHeader] object notTryParsable) { }

                return new[]
                {
                    new object[] { (Action<object>)InvalidFromRoute },
                    new object[] { (Action<object>)InvalidFromQuery },
                    new object[] { (Action<object>)InvalidFromHeader },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DelegatesWithAttributesOnNotTryParsableParameters))]
        public void CreateThrowsInvalidOperationExceptionWhenAttributeRequiresTryParseMethodThatDoesNotExist(Delegate action)
        {
            var ex = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(action));
            Assert.Equal("No public static bool Object.TryParse(string, out Object) method found for notTryParsable.", ex.Message);
        }

        [Fact]
        public void CreateThrowsInvalidOperationExceptionGivenUnnamedArgument()
        {
            var unnamedParameter = Expression.Parameter(typeof(int));
            var lambda = Expression.Lambda(Expression.Block(), unnamedParameter);
            var ex = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(lambda.Compile()));
            Assert.Equal("A parameter does not have a name! Was it generated? All parameters must be named.", ex.Message);
        }

        [Fact]
        public async Task RequestDelegateLogsTryParsableFailuresAsDebugAndSets400Response()
        {
            var invoked = false;

            void TestAction([FromRoute] int tryParsable, [FromRoute] int tryParsable2)
            {
                invoked = true;
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.RouteValues["tryParsable"] = "invalid!";
            httpContext.Request.RouteValues["tryParsable2"] = "invalid again!";
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new TestHttpRequestLifetimeFeature());
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.False(invoked);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            Assert.Equal(400, httpContext.Response.StatusCode);

            var logs = TestSink.Writes.ToArray();

            Assert.Equal(2, logs.Length);

            Assert.Equal(new EventId(3, "ParamaterBindingFailed"), logs[0].EventId);
            Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
            Assert.Equal(@"Failed to bind parameter ""Int32 tryParsable"" from ""invalid!"".", logs[0].Message);

            Assert.Equal(new EventId(3, "ParamaterBindingFailed"), logs[0].EventId);
            Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
            Assert.Equal(@"Failed to bind parameter ""Int32 tryParsable2"" from ""invalid again!"".", logs[1].Message);
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

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

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

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(originalHeaderParam, deserializedRouteParam);
        }

        public static object[][] FromBodyActions
        {
            get
            {
                void TestExplicitFromBody(HttpContext httpContext, [FromBody] Todo todo)
                {
                    httpContext.Items.Add("body", todo);
                }

                void TestImpliedFromBody(HttpContext httpContext, Todo myService)
                {
                    httpContext.Items.Add("body", myService);
                }

                void TestImpliedFromBodyInterface(HttpContext httpContext, ITodo myService)
                {
                    httpContext.Items.Add("body", myService);
                }

                return new[]
                {
                    new[] { (Action<HttpContext, Todo>)TestExplicitFromBody },
                    new[] { (Action<HttpContext, Todo>)TestImpliedFromBody },
                    new[] { (Action<HttpContext, ITodo>)TestImpliedFromBodyInterface },
                };
            }
        }

        [Theory]
        [MemberData(nameof(FromBodyActions))]
        public async Task RequestDelegatePopulatesFromBodyParameter(Delegate action)
        {
            // while (!System.Diagnostics.Debugger.IsAttached)
            // {
            //     System.Console.WriteLine($"Waiting to attach on ${Environment.ProcessId}");
            //     System.Threading.Thread.Sleep(1000);
            // }
            Todo originalTodo = new()
            {
                Name = "Write more tests!"
            };

            var httpContext = new DefaultHttpContext();

            var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
            var stream = new MemoryStream(requestBodyBytes); ;
            httpContext.Request.Body = stream;

            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Headers["Content-Length"] = stream.Length.ToString();

            var jsonOptions = new JsonOptions();
            jsonOptions.SerializerOptions.Converters.Add(new TodoJsonConverter());

            var mock = new Mock<IServiceProvider>();
            mock.Setup(m => m.GetService(It.IsAny<Type>())).Returns<Type>(t =>
            {
                if (t == typeof(IOptions<JsonOptions>))
                {
                    return Options.Create(jsonOptions);
                }
                return null;
            });
            httpContext.RequestServices = mock.Object;

            var requestDelegate = RequestDelegateFactory.Create(action);

            await requestDelegate(httpContext);

            var deserializedRequestBody = httpContext.Items["body"];
            Assert.NotNull(deserializedRequestBody);
            Assert.Equal(originalTodo.Name, ((Todo)deserializedRequestBody!).Name);
        }

        [Theory]
        [MemberData(nameof(FromBodyActions))]
        public async Task RequestDelegateRejectsEmptyBodyGivenFromBodyParameter(Delegate action)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Headers["Content-Length"] = "0";

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(action);

            await requestDelegate(httpContext);

            Assert.Equal(400, httpContext.Response.StatusCode);
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

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

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

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(default, structToBeZeroed);
        }

        [Fact]
        public async Task RequestDelegateLogsFromBodyIOExceptionsAsDebugAndDoesNotAbort()
        {
            var invoked = false;

            void TestAction([FromBody] Todo todo)
            {
                invoked = true;
            }

            var ioException = new IOException();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Headers["Content-Length"] = "1";
            httpContext.Request.Body = new IOExceptionThrowingRequestBodyStream(ioException);
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new TestHttpRequestLifetimeFeature());
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.False(invoked);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);

            var logMessage = Assert.Single(TestSink.Writes);
            Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Same(ioException, logMessage.Exception);
        }

        [Fact]
        public async Task RequestDelegateLogsFromBodyInvalidDataExceptionsAsDebugAndSets400Response()
        {
            var invoked = false;

            void TestAction([FromBody] Todo todo)
            {
                invoked = true;
            }

            var invalidDataException = new InvalidDataException();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Content-Type"] = "application/json";
            httpContext.Request.Headers["Content-Length"] = "1";
            httpContext.Request.Body = new IOExceptionThrowingRequestBodyStream(invalidDataException);
            httpContext.Features.Set<IHttpRequestLifetimeFeature>(new TestHttpRequestLifetimeFeature());
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.False(invoked);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            Assert.Equal(400, httpContext.Response.StatusCode);

            var logMessage = Assert.Single(TestSink.Writes);
            Assert.Equal(new EventId(2, "RequestBodyInvalidDataException"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Same(invalidDataException, logMessage.Exception);
        }

        [Fact]
        public void BuildRequestDelegateThrowsInvalidOperationExceptionGivenFromBodyOnMultipleParameters()
        {
            void TestAttributedInvalidAction([FromBody] int value1, [FromBody] int value2) { }
            void TestInferredInvalidAction(Todo value1, Todo value2) { }
            void TestBothInvalidAction(Todo value1, [FromBody] int value2) { }

            Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestAttributedInvalidAction));
            Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestInferredInvalidAction));
            Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestBothInvalidAction));
        }

        public static object[][] FromServiceActions
        {
            get
            {
                void TestExplicitFromService(HttpContext httpContext, [FromService] MyService myService)
                {
                    httpContext.Items.Add("service", myService);
                }

                void TestExplicitFromIEnumerableService(HttpContext httpContext, [FromService] IEnumerable<MyService> myServices)
                {
                    httpContext.Items.Add("service", myServices.Single());
                }

                void TestImpliedFromService(HttpContext httpContext, IMyService myService)
                {
                    httpContext.Items.Add("service", myService);
                }

                void TestImpliedIEnumerableFromService(HttpContext httpContext, IEnumerable<MyService> myServices)
                {
                    httpContext.Items.Add("service", myServices.Single());
                }

                void TestImpliedFromServiceBasedOnContainer(HttpContext httpContext, MyService myService)
                {
                    httpContext.Items.Add("service", myService);
                }

                return new object[][]
                {
                    new[] { (Action<HttpContext, MyService>)TestExplicitFromService },
                    new[] { (Action<HttpContext, IEnumerable<MyService>>)TestExplicitFromIEnumerableService },
                    new[] { (Action<HttpContext, IMyService>)TestImpliedFromService },
                    new[] { (Action<HttpContext, IEnumerable<MyService>>)TestImpliedIEnumerableFromService },
                    new[] { (Action<HttpContext, MyService>)TestImpliedFromServiceBasedOnContainer },
                };
            }
        }

        [Theory]
        [MemberData(nameof(FromServiceActions))]
        public async Task RequestDelegatePopulatesParametersFromServiceWithAndWithoutAttribute(Delegate action)
        {
            var myOriginalService = new MyService();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(myOriginalService);
            serviceCollection.AddSingleton<IMyService>(myOriginalService);

            var services = serviceCollection.BuildServiceProvider();

            using var requestScoped = services.CreateScope();

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = requestScoped.ServiceProvider;

            var requestDelegate = RequestDelegateFactory.Create(action, options: new() { ServiceProvider = services });

            await requestDelegate(httpContext);

            Assert.Same(myOriginalService, httpContext.Items["service"]);
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

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Same(httpContext, httpContextArgument);
        }

        [Fact]
        public async Task RequestDelegatePassHttpContextRequestAbortedAsCancellationToken()
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

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(httpContext.RequestAborted, cancellationTokenArgument);
        }

        [Fact]
        public async Task RequestDelegatePassHttpContextUserAsClaimsPrincipal()
        {
            ClaimsPrincipal? userArgument = null;

            void TestAction(ClaimsPrincipal user)
            {
                userArgument = user;
            }

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal()
            };

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(httpContext.User, userArgument);
        }

        [Fact]
        public async Task RequestDelegatePassHttpContextRequestAsHttpRequest()
        {
            HttpRequest? httpRequestArgument = null;

            void TestAction(HttpRequest httpRequest)
            {
                httpRequestArgument = httpRequest;
            }

            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(httpContext.Request, httpRequestArgument);
        }

        [Fact]
        public async Task RequestDelegatePassesHttpContextRresponseAsHttpResponse()
        {
            HttpResponse? httpResponseArgument = null;

            void TestAction(HttpResponse httpResponse)
            {
                httpResponseArgument = httpResponse;
            }

            var httpContext = new DefaultHttpContext();

            var requestDelegate = RequestDelegateFactory.Create(TestAction);

            await requestDelegate(httpContext);

            Assert.Equal(httpContext.Response, httpResponseArgument);
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

                // Object return type where the object is IResult
                static object StaticResultAsObject() => new CustomResult("Still not enough tests!");
                static object StaticResultAsTaskObject() => Task.FromResult<object>(new CustomResult("Still not enough tests!"));
                static object StaticResultAsValueTaskObject() => ValueTask.FromResult<object>(new CustomResult("Still not enough tests!"));

                // Object return type where the object is Task<IResult>
                static object StaticResultAsTaskIResult() => Task.FromResult<IResult>(new CustomResult("Still not enough tests!"));

                // Object return type where the object is ValueTask<IResult>
                static object StaticResultAsValueTaskIResult() => ValueTask.FromResult<IResult>(new CustomResult("Still not enough tests!"));

                // Task<object> return type
                static Task<object> StaticTaskOfIResultAsObject() => Task.FromResult<object>(new CustomResult("Still not enough tests!"));
                static ValueTask<object> StaticValueTaskOfIResultAsObject() => ValueTask.FromResult<object>(new CustomResult("Still not enough tests!"));

                return new List<object[]>
                {
                    new object[] { (Func<CustomResult>)TestAction },
                    new object[] { (Func<Task<CustomResult>>)TaskTestAction},
                    new object[] { (Func<ValueTask<CustomResult>>)ValueTaskTestAction},
                    new object[] { (Func<CustomResult>)StaticTestAction},
                    new object[] { (Func<Task<CustomResult>>)StaticTaskTestAction},
                    new object[] { (Func<ValueTask<CustomResult>>)StaticValueTaskTestAction},

                    new object[] { (Func<object>)StaticResultAsObject},
                    new object[] { (Func<object>)StaticResultAsTaskObject},
                    new object[] { (Func<object>)StaticResultAsValueTaskObject},

                    new object[] { (Func<object>)StaticResultAsTaskIResult},
                    new object[] { (Func<object>)StaticResultAsValueTaskIResult},

                    new object[] { (Func<Task<object>>)StaticTaskOfIResultAsObject},
                    new object[] { (Func<ValueTask<object>>)StaticValueTaskOfIResultAsObject},
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

                // Dynamic via object
                static object StaticStringAsObjectTestAction() => "String Test";
                static object StaticTaskStringAsObjectTestAction() => Task.FromResult("String Test");
                static object StaticValueTaskStringAsObjectTestAction() => ValueTask.FromResult("String Test");

                // Dynamic via Task<object>
                static Task<object> StaticStringAsTaskObjectTestAction() => Task.FromResult<object>("String Test");

                // Dynamic via ValueTask<object>
                static ValueTask<object> StaticStringAsValueTaskObjectTestAction() => ValueTask.FromResult<object>("String Test");

                return new List<object[]>
                {
                    new object[] { (Func<string>)TestAction },
                    new object[] { (Func<Task<string>>)TaskTestAction },
                    new object[] { (Func<ValueTask<string>>)ValueTaskTestAction },
                    new object[] { (Func<string>)StaticTestAction },
                    new object[] { (Func<Task<string>>)StaticTaskTestAction },
                    new object[] { (Func<ValueTask<string>>)StaticValueTaskTestAction },

                    new object[] { (Func<object>)StaticStringAsObjectTestAction },
                    new object[] { (Func<object>)StaticTaskStringAsObjectTestAction },
                    new object[] { (Func<object>)StaticValueTaskStringAsObjectTestAction },

                    new object[] { (Func<Task<object>>)StaticStringAsTaskObjectTestAction },
                    new object[] { (Func<ValueTask<object>>)StaticStringAsValueTaskObjectTestAction },


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

        public static IEnumerable<object[]> NullResult
        {
            get
            {
                IResult? TestAction() => null;
                Task<bool?>? TaskBoolAction() => null;
                Task<IResult?>? TaskNullAction() => null;
                Task<IResult?> TaskTestAction() => Task.FromResult<IResult?>(null);
                ValueTask<IResult?> ValueTaskTestAction() => ValueTask.FromResult<IResult?>(null);

                return new List<object[]>
                {
                    new object[] { (Func<IResult?>)TestAction, "The IResult returned by the Delegate must not be null." },
                    new object[] { (Func<Task<IResult?>?>)TaskNullAction, "The IResult in Task<IResult> response must not be null." },
                    new object[] { (Func<Task<bool?>?>)TaskBoolAction, "The Task returned by the Delegate must not be null." },
                    new object[] { (Func<Task<IResult?>>)TaskTestAction, "The IResult returned by the Delegate must not be null." },
                    new object[] { (Func<ValueTask<IResult?>>)ValueTaskTestAction, "The IResult returned by the Delegate must not be null." },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NullResult))]
        public async Task RequestDelegateThrowsInvalidOperationExceptionOnNullDelegate(Delegate @delegate, string message)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => await requestDelegate(httpContext));
            Assert.Contains(message, exception.Message);
        }

        public static IEnumerable<object[]> NullContentResult
        {
            get
            {
                bool? TestBoolAction() => null;
                Task<bool?> TaskTestBoolAction() => Task.FromResult<bool?>(null);
                ValueTask<bool?> ValueTaskTestBoolAction() => ValueTask.FromResult<bool?>(null);

                int? TestIntAction() => null;
                Task<int?> TaskTestIntAction() => Task.FromResult<int?>(null);
                ValueTask<int?> ValueTaskTestIntAction() => ValueTask.FromResult<int?>(null);

                Todo? TestTodoAction() => null;
                Task<Todo?> TaskTestTodoAction() => Task.FromResult<Todo?>(null);
                ValueTask<Todo?> ValueTaskTestTodoAction() => ValueTask.FromResult<Todo?>(null);

                return new List<object[]>
                {
                    new object[] { (Func<bool?>)TestBoolAction },
                    new object[] { (Func<Task<bool?>>)TaskTestBoolAction },
                    new object[] { (Func<ValueTask<bool?>>)ValueTaskTestBoolAction },
                    new object[] { (Func<int?>)TestIntAction },
                    new object[] { (Func<Task<int?>>)TaskTestIntAction },
                    new object[] { (Func<ValueTask<int?>>)ValueTaskTestIntAction },
                    new object[] { (Func<Todo?>)TestTodoAction },
                    new object[] { (Func<Task<Todo?>>)TaskTestTodoAction },
                    new object[] { (Func<ValueTask<Todo?>>)ValueTaskTestTodoAction },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NullContentResult))]
        public async Task RequestDelegateWritesNullReturnNullValue(Delegate @delegate)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

            Assert.Equal("null", responseBody);
        }

        public static IEnumerable<object?[]> QueryParamOptionalityData
        {
            get
            {
                string requiredQueryParam(string name) => $"Hello {name}!";
                string defaultValueQueryParam(string name = "DefaultName") => $"Hello {name}!";
                string nullableQueryParam(string? name) => $"Hello {name}!";
                string requiredParseableQueryParam(int age) => $"Age: {age}";
                string defaultValueParseableQueryParam(int age = 12) => $"Age: {age}";
                string nullableQueryParseableParam(int? age) => $"Age: {age}";

                return new List<object?[]>
                {
                    new object?[] { (Func<string, string>)requiredQueryParam, "name", null, true, null},
                    new object?[] { (Func<string, string>)requiredQueryParam, "name", "TestName", false, "Hello TestName!" },
                    new object?[] { (Func<string, string>)defaultValueQueryParam, "name", null, false, "Hello DefaultName!" },
                    new object?[] { (Func<string, string>)defaultValueQueryParam, "name", "TestName", false, "Hello TestName!" },
                    new object?[] { (Func<string?, string>)nullableQueryParam, "name", null, false, "Hello !" },
                    new object?[] { (Func<string?, string>)nullableQueryParam, "name", "TestName", false, "Hello TestName!"},

                    new object?[] { (Func<int, string>)requiredParseableQueryParam, "age", null, true, null},
                    new object?[] { (Func<int, string>)requiredParseableQueryParam, "age", "42", false, "Age: 42" },
                    new object?[] { (Func<int, string>)defaultValueParseableQueryParam, "age", null, false, "Age: 12" },
                    new object?[] { (Func<int, string>)defaultValueParseableQueryParam, "age", "42", false, "Age: 42" },
                    new object?[] { (Func<int?, string>)nullableQueryParseableParam, "age", null, false, "Age: " },
                    new object?[] { (Func<int?, string>)nullableQueryParseableParam, "age", "42", false, "Age: 42"},
                };
            }
        }

        [Theory]
        [MemberData(nameof(QueryParamOptionalityData))]
        public async Task RequestDelegateHandlesQueryParamOptionality(Delegate @delegate, string paramName, string? queryParam, bool isInvalid, string? expectedResponse)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            if (queryParam is not null)
            {
                httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
                {
                    [paramName] = queryParam
                });
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var logs = TestSink.Writes.ToArray();

            if (isInvalid)
            {
                Assert.Equal(400, httpContext.Response.StatusCode);
                var log = Assert.Single(logs);
                Assert.Equal(LogLevel.Debug, log.LogLevel);
                Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
                var expectedType = paramName == "age" ? "Int32 age" : "String name";
                Assert.Equal($@"Required parameter ""{expectedType}"" was not provided.", log.Message);
            }
            else
            {
                Assert.Equal(200, httpContext.Response.StatusCode);
                Assert.False(httpContext.RequestAborted.IsCancellationRequested);
                var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
                Assert.Equal(expectedResponse, decodedResponseBody);
            }
        }

        public static IEnumerable<object?[]> RouteParamOptionalityData
        {
            get
            {
                string requiredRouteParam(string name) => $"Hello {name}!";
                string defaultValueRouteParam(string name = "DefaultName") => $"Hello {name}!";
                string nullableRouteParam(string? name) => $"Hello {name}!";
                string requiredParseableRouteParam(int age) => $"Age: {age}";
                string defaultValueParseableRouteParam(int age = 12) => $"Age: {age}";
                string nullableParseableRouteParam(int? age) => $"Age: {age}";

                return new List<object?[]>
                {
                    new object?[] { (Func<string, string>)requiredRouteParam, "name", null, true, null},
                    new object?[] { (Func<string, string>)requiredRouteParam, "name", "TestName", false, "Hello TestName!" },
                    new object?[] { (Func<string, string>)defaultValueRouteParam, "name", null, false, "Hello DefaultName!" },
                    new object?[] { (Func<string, string>)defaultValueRouteParam, "name", "TestName", false, "Hello TestName!" },
                    new object?[] { (Func<string?, string>)nullableRouteParam, "name", null, false, "Hello !" },
                    new object?[] { (Func<string?, string>)nullableRouteParam, "name", "TestName", false, "Hello TestName!" },

                    new object?[] { (Func<int, string>)requiredParseableRouteParam, "age", null, true, null},
                    new object?[] { (Func<int, string>)requiredParseableRouteParam, "age", "42", false, "Age: 42" },
                    new object?[] { (Func<int, string>)defaultValueParseableRouteParam, "age", null, false, "Age: 12" },
                    new object?[] { (Func<int, string>)defaultValueParseableRouteParam, "age", "42", false, "Age: 42" },
                    new object?[] { (Func<int?, string>)nullableParseableRouteParam, "age", null, false, "Age: " },
                    new object?[] { (Func<int?, string>)nullableParseableRouteParam, "age", "42", false, "Age: 42"},
                };
            }
        }

        [Theory]
        [MemberData(nameof(RouteParamOptionalityData))]
        public async Task RequestDelegateHandlesRouteParamOptionality(Delegate @delegate, string paramName, string? routeParam, bool isInvalid, string? expectedResponse)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            if (routeParam is not null)
            {
                httpContext.Request.RouteValues[paramName] = routeParam;
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var logs = TestSink.Writes.ToArray();

            if (isInvalid)
            {
                Assert.Equal(400, httpContext.Response.StatusCode);
                var log = Assert.Single(logs);
                Assert.Equal(LogLevel.Debug, log.LogLevel);
                Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
                var expectedType = paramName == "age" ? "Int32 age" : "String name";
                Assert.Equal($@"Required parameter ""{expectedType}"" was not provided.", log.Message);
            }
            else
            {
                Assert.Equal(200, httpContext.Response.StatusCode);
                Assert.False(httpContext.RequestAborted.IsCancellationRequested);
                var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
                Assert.Equal(expectedResponse, decodedResponseBody);
            }
        }

        public static IEnumerable<object?[]> BodyParamOptionalityData
        {
            get
            {
                string requiredBodyParam(Todo todo) => $"Todo: {todo.Name}";
                string defaultValueBodyParam(Todo? todo = null) => $"Todo: {todo?.Name}";
                string nullableBodyParam(Todo? todo) => $"Todo: {todo?.Name}";

                return new List<object?[]>
                {
                    new object?[] { (Func<Todo, string>)requiredBodyParam, false, true, null },
                    new object?[] { (Func<Todo, string>)requiredBodyParam, true, false, "Todo: Default Todo"},
                    new object?[] { (Func<Todo, string>)defaultValueBodyParam, false, false, "Todo: "},
                    new object?[] { (Func<Todo, string>)defaultValueBodyParam, true, false, "Todo: Default Todo"},
                    new object?[] { (Func<Todo?, string>)nullableBodyParam, false, false, "Todo: " },
                    new object?[] { (Func<Todo?, string>)nullableBodyParam, true, false, "Todo: Default Todo" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(BodyParamOptionalityData))]
        public async Task RequestDelegateHandlesBodyParamOptionality(Delegate @delegate, bool hasBody, bool isInvalid, string? expectedResponse)
        {
            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;
            
            if (hasBody)
            {
                var todo = new Todo() { Name = "Default Todo" };
                var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(todo);
                var stream = new MemoryStream(requestBodyBytes);
                httpContext.Request.Body = stream;
                httpContext.Request.Headers["Content-Type"] = "application/json";
                httpContext.Request.ContentLength = stream.Length;
            }

            var jsonOptions = new JsonOptions();
            jsonOptions.SerializerOptions.Converters.Add(new TodoJsonConverter());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            serviceCollection.AddSingleton(Options.Create(jsonOptions));
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var logs = TestSink.Writes.ToArray();

            if (isInvalid)
            {
                Assert.Equal(400, httpContext.Response.StatusCode);
                var log = Assert.Single(logs);
                Assert.Equal(LogLevel.Debug, log.LogLevel);
                Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
                Assert.Equal(@"Required parameter ""Todo todo"" was not provided.", log.Message);
            }
            else
            {
                Assert.Equal(200, httpContext.Response.StatusCode);
                Assert.False(httpContext.RequestAborted.IsCancellationRequested);
                var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
                Assert.Equal(expectedResponse, decodedResponseBody);
            }
        }

        public static IEnumerable<object?[]> ServiceParamOptionalityData
        {
            get
            {
                string requiredExplicitService([FromService] MyService service) => $"Service: {service}";
                string defaultValueExplicitServiceParam([FromService] MyService? service = null) => $"Service: {service}";
                string nullableExplicitServiceParam([FromService] MyService? service) => $"Service: {service}";

                return new List<object?[]>
                {
                    new object?[] { (Func<MyService, string>)requiredExplicitService, false, true},
                    new object?[] { (Func<MyService, string>)requiredExplicitService, true, false},

                    new object?[] { (Func<MyService, string>)defaultValueExplicitServiceParam, false, false},
                    new object?[] { (Func<MyService, string>)defaultValueExplicitServiceParam, true, false},

                    new object?[] { (Func<MyService?, string>)nullableExplicitServiceParam, false, false},
                    new object?[] { (Func<MyService?, string>)nullableExplicitServiceParam, true, false},
                };
            }
        }

        [Theory]
        [MemberData(nameof(ServiceParamOptionalityData))]
        public async Task RequestDelegateHandlesServiceParamOptionality(Delegate @delegate, bool hasService, bool isInvalid)
        {
            var httpContext = new DefaultHttpContext();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            if (hasService)
            {
                var service = new MyService();

                serviceCollection.AddSingleton(service);
            }
            var services = serviceCollection.BuildServiceProvider();
            httpContext.RequestServices = services;
            RequestDelegateFactoryOptions options = new() { ServiceProvider = services };

            var requestDelegate = RequestDelegateFactory.Create(@delegate, options);

            if (!isInvalid)
            {
                await requestDelegate(httpContext);
                Assert.Equal(200, httpContext.Response.StatusCode);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => requestDelegate(httpContext));
                Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            }
        }

        public static IEnumerable<object?[]> ImplicitServiceParamOptionalityData
        {
            get
            {
                string requiredImplicitService(MyService name) => $"Hello {name}!";
                string defaultValueImplicitServiceParam(MyService? name = null) => $"Hello {name}!";
                string nullableImplicitServiceParam(MyService? name) => $"Hello {name}!";

                return new List<object?[]>
                {
                    new object?[] { (Func<MyService, string>)requiredImplicitService, false, true},
                    new object?[] { (Func<MyService, string>)requiredImplicitService, true, false},

                    new object?[] { (Func<MyService, string>)defaultValueImplicitServiceParam, false, false},
                    new object?[] { (Func<MyService, string>)defaultValueImplicitServiceParam, true, false},

                    new object?[] { (Func<MyService?, string>)nullableImplicitServiceParam, false, false},
                    new object?[] { (Func<MyService?, string>)nullableImplicitServiceParam, true, false}
                };
            }
        }

        [Theory]
        [MemberData(nameof(ImplicitServiceParamOptionalityData))]
        public async Task RequestDelegateHandlesImplicitServiceParamOptionality(Delegate @delegate, bool hasService, bool isInvalid)
        {
            var httpContext = new DefaultHttpContext();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            if (hasService)
            {
                var service = new MyService();
                serviceCollection.AddSingleton(service);
            }
            var services = serviceCollection.BuildServiceProvider();
            httpContext.RequestServices = services;
            RequestDelegateFactoryOptions options = new() { ServiceProvider = services };

            var requestDelegate = RequestDelegateFactory.Create(@delegate, options);

            await requestDelegate(httpContext);
            Assert.Equal(isInvalid ? 400 : 200, httpContext.Response.StatusCode);
        }

        public static IEnumerable<object?[]> AllowEmptyData
        {
            get
            {
                string disallowEmptyAndNonOptional([FromBody(AllowEmpty = false)] Todo todo) => $"{todo}";
                string allowEmptyAndNonOptional([FromBody(AllowEmpty = true)] Todo todo) => $"{todo}";
                string allowEmptyAndOptional([FromBody(AllowEmpty = true)] Todo? todo = null) => $"{todo}";
                string disallowEmptyAndOptional([FromBody(AllowEmpty = false)] Todo? todo = null) => $"{todo}";

                return new List<object?[]>
                {
                    new object?[] { (Func<Todo, string>)disallowEmptyAndNonOptional, false },
                    new object?[] { (Func<Todo, string>)allowEmptyAndNonOptional, true },
                    new object?[] { (Func<Todo, string>)allowEmptyAndOptional, true },
                    new object?[] { (Func<Todo, string>)disallowEmptyAndOptional, true }
                };
            }
        }

        [Theory]
        [MemberData(nameof(AllowEmptyData))]
        public async Task AllowEmptyOverridesOptionality(Delegate @delegate, bool allowsEmptyRequest)
        {
            var httpContext = new DefaultHttpContext();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            var requestDelegate = RequestDelegateFactory.Create(@delegate);

            await requestDelegate(httpContext);

            var logs = TestSink.Writes.ToArray();
            
            if (!allowsEmptyRequest)
            {
                Assert.Equal(400, httpContext.Response.StatusCode);
                var log = Assert.Single(logs);
                Assert.Equal(LogLevel.Debug, log.LogLevel);
                Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
                Assert.Equal(@"Required parameter ""Todo todo"" was not provided.", log.Message);
            }
            else
            {
                Assert.Equal(200, httpContext.Response.StatusCode);
                Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            }           
        }

        [Fact]
        public async Task RequestDelegateHandlesRequiredAmbiguousValueFromBody()
        {
            var invoked = false;
            void TestAction(Todo todo)
            {
                invoked = true;
            }

            var httpContext = new DefaultHttpContext();

            httpContext.Request.Headers["Content-Type"] = "application/json";

            var todo = new Todo() { Name = "Default Todo" };
            var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(todo);
            var stream = new MemoryStream(requestBodyBytes);
            httpContext.Request.Body = stream;
            httpContext.Request.ContentLength = stream.Length;

            var jsonOptions = new JsonOptions();
            jsonOptions.SerializerOptions.Converters.Add(new TodoJsonConverter());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            serviceCollection.AddSingleton(Options.Create(jsonOptions));
            var services = serviceCollection.BuildServiceProvider();
            httpContext.RequestServices = services;

            var requestDelegate = RequestDelegateFactory.Create(TestAction, new() { ServiceProvider = services });

            await requestDelegate(httpContext);
            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.True(invoked);
        }

        [Fact]
        public async Task RequestDelegateHandlesRequiredAmbiguousValueFromService()
        {
            var invoked = false;
            void TestAction(Todo todo)
            {
                invoked = true;
            }

            var httpContext = new DefaultHttpContext();

            var todo = new Todo() { Name = "Default Todo" };

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(LoggerFactory);
            serviceCollection.AddSingleton(todo);
            var services = serviceCollection.BuildServiceProvider();
            httpContext.RequestServices = services;

            var requestDelegate = RequestDelegateFactory.Create(TestAction, new() { ServiceProvider = services });

            await requestDelegate(httpContext);
            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.True(invoked);
        }

#nullable disable

        [Theory]
        [InlineData(true, "Hello TestName!")]
        [InlineData(false, "Hello !")]
        public async Task CanSetStringParamAsOptionalWithNullabilityDisability(bool provideValue, string expectedResponse)
        {
            string optionalQueryParam(string name = null) => $"Hello {name}!";

            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            if (provideValue)
            {
                httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
                {
                    ["name"] = "TestName"
                });
            }

            var requestDelegate = RequestDelegateFactory.Create(optionalQueryParam);

            await requestDelegate(httpContext);

            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
            Assert.Equal(expectedResponse, decodedResponseBody);
        }

        [Theory]
        [InlineData(true, "Age: 42")]
        [InlineData(false, "Age: 0")]
        public async Task CanSetParseableStringParamAsOptionalWithNullabilityDisability(bool provideValue, string expectedResponse)
        {
            string optionalQueryParam(int age = default(int)) => $"Age: {age}";

            var httpContext = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            httpContext.Response.Body = responseBodyStream;

            if (provideValue)
            {
                httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
                {
                    ["age"] = "42"
                });
            }

            var requestDelegate = RequestDelegateFactory.Create(optionalQueryParam);

            await requestDelegate(httpContext);

            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
            Assert.Equal(expectedResponse, decodedResponseBody);
        }

#nullable enable

        private class Todo : ITodo
        {
            public int Id { get; set; }
            public string? Name { get; set; } = "Todo";
            public bool IsComplete { get; set; }
        }

        private interface ITodo
        {
            public int Id { get; }
            public string? Name { get; }
            public bool IsComplete { get; }
        }

        class TodoJsonConverter : JsonConverter<ITodo>
        {
            public override ITodo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var todo = new Todo();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    var property = reader.GetString()!;
                    reader.Read();

                    switch (property.ToLowerInvariant())
                    {
                        case "id":
                            todo.Id = reader.GetInt32();
                            break;
                        case "name":
                            todo.Name = reader.GetString();
                            break;
                        case "iscomplete":
                            todo.IsComplete = reader.GetBoolean();
                            break;
                        default:
                            break;
                    }
                }

                return todo;
            }

            public override void Write(Utf8JsonWriter writer, ITodo value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
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

        private class FromServiceAttribute : Attribute, IFromServiceMetadata
        {
        }

        class HttpHandler
        {
            private int _calls;

            public void Handle(HttpContext httpContext)
            {
                _calls++;
                httpContext.Items["calls"] = _calls;
            }
        }

        private interface IMyService
        {
        }

        private class MyService : IMyService
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

        private class EmptyServiceProvider : IServiceScope, IServiceProvider, IServiceScopeFactory
        {
            public IServiceProvider ServiceProvider => this;

            public IServiceScope CreateScope()
            {
                return new EmptyServiceProvider();
            }

            public void Dispose()
            {

            }

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IServiceScopeFactory))
                {
                    return this;
                }
                return null;
            }
        }

        private class TestHttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
        {
            private readonly CancellationTokenSource _requestAbortedCts = new();

            public CancellationToken RequestAborted { get => _requestAbortedCts.Token; set => throw new NotImplementedException(); }

            public void Abort()
            {
                _requestAbortedCts.Cancel();
            }
        }
    }
}
