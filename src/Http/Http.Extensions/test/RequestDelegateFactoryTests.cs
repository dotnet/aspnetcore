// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

namespace Microsoft.AspNetCore.Routing.Internal;

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
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var factoryResult = RequestDelegateFactory.Create(methodInfo!);
        var requestDelegate = factoryResult.RequestDelegate;

        var httpContext = CreateHttpContext();

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

        var factoryResult = RequestDelegateFactory.Create(methodInfo!, _ => GetTarget());
        var requestDelegate = factoryResult.RequestDelegate;

        var httpContext = CreateHttpContext();

        await requestDelegate(httpContext);

        Assert.Equal(1, httpContext.Items["invoked"]);

        httpContext = CreateHttpContext();

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

        var exNullAction = Assert.Throws<ArgumentNullException>(() => RequestDelegateFactory.Create(handler: null!));
        var exNullMethodInfo1 = Assert.Throws<ArgumentNullException>(() => RequestDelegateFactory.Create(methodInfo: null!));

        Assert.Equal("handler", exNullAction.ParamName);
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

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create((int? id, HttpContext httpContext) =>
        {
            if (id is not null)
            {
                httpContext.Items["input"] = id;
            }
        },
        new() { RouteParameterNames = new string[] { "id" } });

        var requestDelegate = factoryResult.RequestDelegate;

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["id"] = "42"
        });

        await requestDelegate(httpContext);

        Assert.Null(httpContext.Items["input"]);
    }

    [Fact]
    public async Task SpecifiedQueryParametersDoNotFallbackToRouteValues()
    {
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create((int? id, HttpContext httpContext) =>
        {
            if (id is not null)
            {
                httpContext.Items["input"] = id;
            }
        },
        new() { RouteParameterNames = new string[] { } });

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["id"] = "41"
        });
        httpContext.Request.RouteValues = new()
        {
            ["id"] = "42"
        };

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(41, httpContext.Items["input"]);
    }

    [Fact]
    public async Task NullRouteParametersPrefersRouteOverQueryString()
    {
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create((int? id, HttpContext httpContext) =>
        {
            if (id is not null)
            {
                httpContext.Items["input"] = id;
            }
        },
        new() { RouteParameterNames = null });

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["id"] = "41"
        });
        httpContext.Request.RouteValues = new()
        {
            ["id"] = "42"
        };

        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(42, httpContext.Items["input"]);
    }

    [Fact]
    public async Task CreatingDelegateWithInstanceMethodInfoCreatesInstancePerCall()
    {
        var methodInfo = typeof(HttpHandler).GetMethod(nameof(HttpHandler.Handle));

        Assert.NotNull(methodInfo);

        var factoryResult = RequestDelegateFactory.Create(methodInfo!);
        var requestDelegate = factoryResult.RequestDelegate;

        var context = CreateHttpContext();

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

        Assert.Equal("'id' is not a route parameter.", ex.Message);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromRouteOptionalParameter()
    {
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(TestOptional);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(42, httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromNullableOptionalParameter()
    {
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(TestOptional);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(42, httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromOptionalStringParameter()
    {
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(TestOptionalString);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal("default", httpContext.Items["input"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromRouteOptionalParameterBasedOnParameterName()
    {
        const string paramName = "value";
        const int originalRouteParam = 47;

        var httpContext = CreateHttpContext();

        httpContext.Request.RouteValues[paramName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        var factoryResult = RequestDelegateFactory.Create(TestOptional);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[specifiedName] = originalRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues[unmatchedName] = unmatchedRouteParam.ToString(NumberFormatInfo.InvariantInfo);

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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
                    new object[] { (Action<HttpContext, DateTime>)Store, now.ToString("o"), now.ToUniversalTime() },
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
                    new object[] { (Action<HttpContext, MyTryParseRecord>)Store, "https://example.org", new MyTryParseRecord(new Uri("https://example.org")) },
                    new object?[] { (Action<HttpContext, int?>)Store, null, null },
                };
        }
    }

    private enum MyEnum { ValueA, ValueB, }

    private record MyTryParseRecord(Uri Uri)
    {
        public static bool TryParse(string? value, out MyTryParseRecord? result)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                result = null;
                return false;
            }

            result = new MyTryParseRecord(uri);
            return true;
        }
    }

    private class MyBindAsyncTypeThatThrows
    {
        public static ValueTask<MyBindAsyncTypeThatThrows?> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new InvalidOperationException("BindAsync failed");
    }

    private record MyBindAsyncRecord(Uri Uri)
    {
        public static ValueTask<MyBindAsyncRecord?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.Equal(typeof(MyBindAsyncRecord), parameter.ParameterType);
            Assert.StartsWith("myBindAsyncRecord", parameter.Name);

            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                return new(result: null);
            }

            return new(result: new(uri));
        }

        // BindAsync(HttpContext, ParameterInfo) should be preferred over TryParse(string, ...) if there's
        // no [FromRoute] or [FromQuery] attributes.
        public static bool TryParse(string? value, out MyBindAsyncRecord? result) =>
            throw new NotImplementedException();
    }

    private record struct MyNullableBindAsyncStruct(Uri Uri)
    {
        public static ValueTask<MyNullableBindAsyncStruct?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.True(parameter.ParameterType == typeof(MyNullableBindAsyncStruct) || parameter.ParameterType == typeof(MyNullableBindAsyncStruct?));
            Assert.Equal("myNullableBindAsyncStruct", parameter.Name);

            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                return new(result: null);
            }

            return new(result: new(uri));
        }
    }

    private record struct MyBindAsyncStruct(Uri Uri)
    {
        public static ValueTask<MyBindAsyncStruct> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.True(parameter.ParameterType == typeof(MyBindAsyncStruct) || parameter.ParameterType == typeof(MyBindAsyncStruct?));
            Assert.Equal("myBindAsyncStruct", parameter.Name);

            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                throw new BadHttpRequestException("The request is missing the required Referer header.");
            }

            return new(result: new(uri));
        }

        // BindAsync(HttpContext, ParameterInfo) should be preferred over TryParse(string, ...) if there's
        // no [FromRoute] or [FromQuery] attributes.
        public static bool TryParse(string? value, out MyBindAsyncStruct result) =>
            throw new NotImplementedException();
    }

    private record MyAwaitedBindAsyncRecord(Uri Uri)
    {
        public static async ValueTask<MyAwaitedBindAsyncRecord?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.Equal(typeof(MyAwaitedBindAsyncRecord), parameter.ParameterType);
            Assert.StartsWith("myAwaitedBindAsyncRecord", parameter.Name);

            await Task.Yield();

            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                return null;
            }

            return new(uri);
        }
    }

    private record struct MyAwaitedBindAsyncStruct(Uri Uri)
    {
        public static async ValueTask<MyAwaitedBindAsyncStruct> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.Equal(typeof(MyAwaitedBindAsyncStruct), parameter.ParameterType);
            Assert.Equal("myAwaitedBindAsyncStruct", parameter.Name);

            await Task.Yield();

            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                throw new BadHttpRequestException("The request is missing the required Referer header.");
            }

            return new(uri);
        }
    }

    private record struct MyBothBindAsyncStruct(Uri Uri)
    {
        public static ValueTask<MyBothBindAsyncStruct> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.True(parameter.ParameterType == typeof(MyBothBindAsyncStruct) || parameter.ParameterType == typeof(MyBothBindAsyncStruct?));
            Assert.Equal("myBothBindAsyncStruct", parameter.Name);

            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                throw new BadHttpRequestException("The request is missing the required Referer header.");
            }

            return new(result: new(uri));
        }

        // BindAsync with ParameterInfo is preferred
        public static ValueTask<MyBothBindAsyncStruct> BindAsync(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }

    private record struct MySimpleBindAsyncStruct(Uri Uri)
    {
        public static ValueTask<MySimpleBindAsyncStruct> BindAsync(HttpContext context)
        {
            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                throw new BadHttpRequestException("The request is missing the required Referer header.");
            }

            return new(result: new(uri));
        }
    }

    private record MySimpleBindAsyncRecord(Uri Uri)
    {
        public static ValueTask<MySimpleBindAsyncRecord?> BindAsync(HttpContext context)
        {
            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                return new(result: null);
            }

            return new(result: new(uri));
        }
    }

    private interface IBindAsync<T>
    {
        static ValueTask<T?> BindAsync(HttpContext context)
        {
            if (typeof(T) != typeof(MyBindAsyncFromInterfaceRecord))
            {
                throw new InvalidOperationException();
            }

            if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
            {
                return new(default(T));
            }

            return new(result: (T)(object)new MyBindAsyncFromInterfaceRecord(uri));
        }
    }

    private record MyBindAsyncFromInterfaceRecord(Uri uri) : IBindAsync<MyBindAsyncFromInterfaceRecord>
    {
    }

    [Theory]
    [MemberData(nameof(TryParsableParameters))]
    public async Task RequestDelegatePopulatesUnattributedTryParsableParametersFromRouteValue(Delegate action, string? routeValue, object? expectedParameterValue)
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["tryParsable"] = routeValue;

        var factoryResult = RequestDelegateFactory.Create(action);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(expectedParameterValue, httpContext.Items["tryParsable"]);
    }

    [Theory]
    [MemberData(nameof(TryParsableParameters))]
    public async Task RequestDelegatePopulatesUnattributedTryParsableParametersFromQueryString(Delegate action, string? routeValue, object? expectedParameterValue)
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["tryParsable"] = routeValue
        });

        var factoryResult = RequestDelegateFactory.Create(action);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(expectedParameterValue, httpContext.Items["tryParsable"]);
    }

    [Fact]
    public async Task RequestDelegatePopulatesUnattributedTryParsableParametersFromRouteValueBeforeQueryString()
    {
        var httpContext = CreateHttpContext();

        httpContext.Request.RouteValues["tryParsable"] = "42";

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["tryParsable"] = "invalid!"
        });

        var factoryResult = RequestDelegateFactory.Create((HttpContext httpContext, int tryParsable) =>
        {
            httpContext.Items["tryParsable"] = tryParsable;
        });

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(42, httpContext.Items["tryParsable"]);
    }

    [Fact]
    public async Task RequestDelegatePrefersBindAsyncOverTryParse()
    {
        var httpContext = CreateHttpContext();

        httpContext.Request.Headers.Referer = "https://example.org";

        var resultFactory = RequestDelegateFactory.Create((HttpContext httpContext, MyBindAsyncRecord myBindAsyncRecord) =>
        {
            httpContext.Items["myBindAsyncRecord"] = myBindAsyncRecord;
        });

        var requestDelegate = resultFactory.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(new MyBindAsyncRecord(new Uri("https://example.org")), httpContext.Items["myBindAsyncRecord"]);
    }

    [Fact]
    public async Task RequestDelegatePrefersBindAsyncOverTryParseForNonNullableStruct()
    {
        var httpContext = CreateHttpContext();

        httpContext.Request.Headers.Referer = "https://example.org";

        var resultFactory = RequestDelegateFactory.Create((HttpContext httpContext, MyBindAsyncStruct myBindAsyncStruct) =>
        {
            httpContext.Items["myBindAsyncStruct"] = myBindAsyncStruct;
        });

        var requestDelegate = resultFactory.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(new MyBindAsyncStruct(new Uri("https://example.org")), httpContext.Items["myBindAsyncStruct"]);
    }

    [Fact]
    public async Task RequestDelegateUsesBindAsyncOverTryParseGivenNullableStruct()
    {
        var httpContext = CreateHttpContext();

        httpContext.Request.Headers.Referer = "https://example.org";

        var resultFactory = RequestDelegateFactory.Create((HttpContext httpContext, MyBindAsyncStruct? myBindAsyncStruct) =>
        {
            httpContext.Items["myBindAsyncStruct"] = myBindAsyncStruct;
        });

        var requestDelegate = resultFactory.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(new MyBindAsyncStruct(new Uri("https://example.org")), httpContext.Items["myBindAsyncStruct"]);
    }

    [Fact]
    public async Task RequestDelegateUsesParameterInfoBindAsyncOverOtherBindAsync()
    {
        var httpContext = CreateHttpContext();

        httpContext.Request.Headers.Referer = "https://example.org";

        var resultFactory = RequestDelegateFactory.Create((HttpContext httpContext, MyBothBindAsyncStruct? myBothBindAsyncStruct) =>
        {
            httpContext.Items["myBothBindAsyncStruct"] = myBothBindAsyncStruct;
        });

        var requestDelegate = resultFactory.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(new MyBothBindAsyncStruct(new Uri("https://example.org")), httpContext.Items["myBothBindAsyncStruct"]);
    }

    [Fact]
    public async Task RequestDelegateUsesTryParseOverBindAsyncGivenExplicitAttribute()
    {
        var fromRouteFactoryResult = RequestDelegateFactory.Create((HttpContext httpContext, [FromRoute] MyBindAsyncRecord myBindAsyncRecord) => { });
        var fromQueryFactoryResult = RequestDelegateFactory.Create((HttpContext httpContext, [FromQuery] MyBindAsyncRecord myBindAsyncRecord) => { });


        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["myBindAsyncRecord"] = "foo";
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["myBindAsyncRecord"] = "foo"
        });

        var fromRouteRequestDelegate = fromRouteFactoryResult.RequestDelegate;
        var fromQueryRequestDelegate = fromQueryFactoryResult.RequestDelegate;

        await Assert.ThrowsAsync<NotImplementedException>(() => fromRouteRequestDelegate(httpContext));
        await Assert.ThrowsAsync<NotImplementedException>(() => fromQueryRequestDelegate(httpContext));
    }

    [Fact]
    public async Task RequestDelegateCanAwaitValueTasksThatAreNotImmediatelyCompleted()
    {
        var httpContext = CreateHttpContext();

        httpContext.Request.Headers.Referer = "https://example.org";

        var resultFactory = RequestDelegateFactory.Create(
            (HttpContext httpContext, MyAwaitedBindAsyncRecord myAwaitedBindAsyncRecord, MyAwaitedBindAsyncStruct myAwaitedBindAsyncStruct) =>
            {
                httpContext.Items["myAwaitedBindAsyncRecord"] = myAwaitedBindAsyncRecord;
                httpContext.Items["myAwaitedBindAsyncStruct"] = myAwaitedBindAsyncStruct;
            });

        var requestDelegate = resultFactory.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(new MyAwaitedBindAsyncRecord(new Uri("https://example.org")), httpContext.Items["myAwaitedBindAsyncRecord"]);
        Assert.Equal(new MyAwaitedBindAsyncStruct(new Uri("https://example.org")), httpContext.Items["myAwaitedBindAsyncStruct"]);
    }

    [Fact]
    public async Task RequestDelegateUsesBindAsyncFromImplementedInterface()
    {
        var httpContext = CreateHttpContext();

        httpContext.Request.Headers.Referer = "https://example.org";

        var resultFactory = RequestDelegateFactory.Create((HttpContext httpContext, MyBindAsyncFromInterfaceRecord myBindAsyncRecord) =>
        {
            httpContext.Items["myBindAsyncFromInterfaceRecord"] = myBindAsyncRecord;
        });

        var requestDelegate = resultFactory.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(new MyBindAsyncFromInterfaceRecord(new Uri("https://example.org")), httpContext.Items["myBindAsyncFromInterfaceRecord"]);
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
        Assert.Equal("No public static bool object.TryParse(string, out object) method found for notTryParsable.", ex.Message);
    }

    [Fact]
    public void CreateThrowsInvalidOperationExceptionGivenUnnamedArgument()
    {
        var unnamedParameter = Expression.Parameter(typeof(int));
        var lambda = Expression.Lambda(Expression.Block(), unnamedParameter);
        var ex = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(lambda.Compile()));
        Assert.Equal("Encountered a parameter of type 'System.Runtime.CompilerServices.Closure' without a name. Parameters must have a name.", ex.Message);
    }

    [Fact]
    public async Task RequestDelegateLogsTryParsableFailuresAsDebugAndSets400Response()
    {
        var invoked = false;

        void TestAction([FromRoute] int tryParsable, [FromRoute] int tryParsable2)
        {
            invoked = true;
        }

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["tryParsable"] = "invalid!";
        httpContext.Request.RouteValues["tryParsable2"] = "invalid again!";

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logs = TestSink.Writes.ToArray();

        Assert.Equal(2, logs.Length);

        Assert.Equal(new EventId(3, "ParameterBindingFailed"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Failed to bind parameter ""int tryParsable"" from ""invalid!"".", logs[0].Message);

        Assert.Equal(new EventId(3, "ParameterBindingFailed"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Failed to bind parameter ""int tryParsable2"" from ""invalid again!"".", logs[1].Message);
    }

    [Fact]
    public async Task RequestDelegateThrowsForTryParsableFailuresIfThrowOnBadRequest()
    {
        var invoked = false;

        void TestAction([FromRoute] int tryParsable, [FromRoute] int tryParsable2)
        {
            invoked = true;
        }

        var httpContext = CreateHttpContext();
        httpContext.Request.RouteValues["tryParsable"] = "invalid!";
        httpContext.Request.RouteValues["tryParsable2"] = "invalid again!";

        var factoryResult = RequestDelegateFactory.Create(TestAction, new() { ThrowOnBadRequest = true });
        var requestDelegate = factoryResult.RequestDelegate;

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to bind parameter ""int tryParsable"" from ""invalid!"".", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    [Fact]
    public async Task RequestDelegateLogsBindAsyncFailuresAndSets400Response()
    {
        // Not supplying any headers will cause the HttpContext BindAsync overload to return null.
        var httpContext = CreateHttpContext();
        var invoked = false;

        var factoryResult = RequestDelegateFactory.Create((MyBindAsyncRecord myBindAsyncRecord1, MyBindAsyncRecord myBindAsyncRecord2) =>
        {
            invoked = true;
        });

        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);

        var logs = TestSink.Writes.ToArray();

        Assert.Equal(2, logs.Length);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Required parameter ""MyBindAsyncRecord myBindAsyncRecord1"" was not provided from MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo).", logs[0].Message);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Required parameter ""MyBindAsyncRecord myBindAsyncRecord2"" was not provided from MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo).", logs[1].Message);
    }

    [Fact]
    public async Task RequestDelegateThrowsForBindAsyncFailuresIfThrowOnBadRequest()
    {
        // Not supplying any headers will cause the HttpContext BindAsync overload to return null.
        var httpContext = CreateHttpContext();
        var invoked = false;

        var factoryResult = RequestDelegateFactory.Create((MyBindAsyncRecord myBindAsyncRecord1, MyBindAsyncRecord myBindAsyncRecord2) =>
        {
            invoked = true;
        }, new() { ThrowOnBadRequest = true });

        var requestDelegate = factoryResult.RequestDelegate;
        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Required parameter ""MyBindAsyncRecord myBindAsyncRecord1"" was not provided from MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo).", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    [Fact]
    public async Task RequestDelegateLogsSingleArgBindAsyncFailuresAndSets400Response()
    {
        // Not supplying any headers will cause the HttpContext BindAsync overload to return null.
        var httpContext = CreateHttpContext();
        var invoked = false;

        var factoryResult = RequestDelegateFactory.Create((MySimpleBindAsyncRecord mySimpleBindAsyncRecord1,
            MySimpleBindAsyncRecord mySimpleBindAsyncRecord2) =>
        {
            invoked = true;
        });

        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);

        var logs = TestSink.Writes.ToArray();

        Assert.Equal(2, logs.Length);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Required parameter ""MySimpleBindAsyncRecord mySimpleBindAsyncRecord1"" was not provided from MySimpleBindAsyncRecord.BindAsync(HttpContext).", logs[0].Message);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Required parameter ""MySimpleBindAsyncRecord mySimpleBindAsyncRecord2"" was not provided from MySimpleBindAsyncRecord.BindAsync(HttpContext).", logs[1].Message);
    }

    [Fact]
    public async Task RequestDelegateThrowsForSingleArgBindAsyncFailuresIfThrowOnBadRequest()
    {
        // Not supplying any headers will cause the HttpContext BindAsync overload to return null.
        var httpContext = CreateHttpContext();
        var invoked = false;

        var factoryResult = RequestDelegateFactory.Create((MySimpleBindAsyncRecord mySimpleBindAsyncRecord1,
            MySimpleBindAsyncRecord mySimpleBindAsyncRecord2) =>
        {
            invoked = true;
        }, new() { ThrowOnBadRequest = true });

        var requestDelegate = factoryResult.RequestDelegate;
        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Required parameter ""MySimpleBindAsyncRecord mySimpleBindAsyncRecord1"" was not provided from MySimpleBindAsyncRecord.BindAsync(HttpContext).", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    [Fact]
    public async Task BindAsyncExceptionsAreUncaught()
    {
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create((MyBindAsyncTypeThatThrows arg1) => { });

        var requestDelegate = factoryResult.RequestDelegate;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => requestDelegate(httpContext));
        Assert.Equal("BindAsync failed", ex.Message);
    }

    [Fact]
    public async Task BindAsyncWithBodyArgument()
    {
        Todo originalTodo = new()
        {
            Name = "Write more tests!"
        };

        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

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
        httpContext.Request.Headers.Referer = "https://example.org";

        var invoked = false;

        var factoryResult = RequestDelegateFactory.Create((HttpContext context, MyBindAsyncRecord myBindAsyncRecord, Todo todo) =>
        {
            invoked = true;
            context.Items[nameof(myBindAsyncRecord)] = myBindAsyncRecord;
            context.Items[nameof(todo)] = todo;
        });

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.True(invoked);
        var arg = httpContext.Items["myBindAsyncRecord"] as MyBindAsyncRecord;
        Assert.NotNull(arg);
        Assert.Equal("https://example.org/", arg!.Uri.ToString());
        var todo = httpContext.Items["todo"] as Todo;
        Assert.NotNull(todo);
        Assert.Equal("Write more tests!", todo!.Name);
    }

    [Fact]
    public async Task BindAsyncRunsBeforeBodyBinding()
    {
        Todo originalTodo = new()
        {
            Name = "Write more tests!"
        };

        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

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
        httpContext.Request.Headers.Referer = "https://example.org";

        var invoked = false;

        var factoryResult = RequestDelegateFactory.Create((HttpContext context, CustomTodo customTodo, Todo todo) =>
        {
            invoked = true;
            context.Items[nameof(customTodo)] = customTodo;
            context.Items[nameof(todo)] = todo;
        });

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.True(invoked);
        var todo0 = httpContext.Items["customTodo"] as Todo;
        Assert.NotNull(todo0);
        Assert.Equal("Write more tests!", todo0!.Name);
        var todo1 = httpContext.Items["todo"] as Todo;
        Assert.NotNull(todo1);
        Assert.Equal("Write more tests!", todo1!.Name);
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

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = query;

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[customHeaderName] = originalHeaderParam.ToString(NumberFormatInfo.InvariantInfo);

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(originalHeaderParam, deserializedRouteParam);
    }

    public static object[][] ImplicitFromBodyActions
    {
        get
        {
            void TestImpliedFromBody(HttpContext httpContext, Todo todo)
            {
                httpContext.Items.Add("body", todo);
            }

            void TestImpliedFromBodyInterface(HttpContext httpContext, ITodo todo)
            {
                httpContext.Items.Add("body", todo);
            }

            void TestImpliedFromBodyStruct(HttpContext httpContext, TodoStruct todo)
            {
                httpContext.Items.Add("body", todo);
            }

            return new[]
            {
                    new[] { (Action<HttpContext, Todo>)TestImpliedFromBody },
                    new[] { (Action<HttpContext, ITodo>)TestImpliedFromBodyInterface },
                    new object[] { (Action<HttpContext, TodoStruct>)TestImpliedFromBodyStruct },
                };
        }
    }

    public static object[][] ExplicitFromBodyActions
    {
        get
        {
            void TestExplicitFromBody(HttpContext httpContext, [FromBody] Todo todo)
            {
                httpContext.Items.Add("body", todo);
            }

            return new[]
            {
                    new[] { (Action<HttpContext, Todo>)TestExplicitFromBody },
                };
        }
    }

    public static object[][] FromBodyActions
    {
        get
        {
            return ExplicitFromBodyActions.Concat(ImplicitFromBodyActions).ToArray();
        }
    }

    [Theory]
    [MemberData(nameof(FromBodyActions))]
    public async Task RequestDelegatePopulatesFromBodyParameter(Delegate action)
    {
        Todo originalTodo = new()
        {
            Name = "Write more tests!"
        };

        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

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

        var factoryResult = RequestDelegateFactory.Create(action);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var deserializedRequestBody = httpContext.Items["body"];
        Assert.NotNull(deserializedRequestBody);
        Assert.Equal(originalTodo.Name, ((ITodo)deserializedRequestBody!).Name);
    }

    public static object[][] RawFromBodyActions
    {
        get
        {
            void TestStream(HttpContext httpContext, Stream stream)
            {
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                httpContext.Items.Add("body", ms.ToArray());
            }

            async Task TestPipeReader(HttpContext httpContext, PipeReader reader)
            {
                var ms = new MemoryStream();
                await reader.CopyToAsync(ms);
                httpContext.Items.Add("body", ms.ToArray());
            }

            return new[]
            {
                new object[] { (Action<HttpContext, Stream>)TestStream },
                new object[] { (Func<HttpContext, PipeReader, Task>)TestPipeReader }
            };
        }
    }

    [Theory]
    [MemberData(nameof(RawFromBodyActions))]
    public async Task RequestDelegatePopulatesFromRawBodyParameter(Delegate action)
    {
        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Name = "Write more tests!"
        });

        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var mock = new Mock<IServiceProvider>();
        httpContext.RequestServices = mock.Object;

        var factoryResult = RequestDelegateFactory.Create(action);

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Same(httpContext.Request.Body, stream);

        // Assert that we can read the body from both the pipe reader and Stream after executing
        httpContext.Request.Body.Position = 0;
        byte[] data = new byte[requestBodyBytes.Length];
        int read = await httpContext.Request.Body.ReadAsync(data.AsMemory());
        Assert.Equal(read, data.Length);
        Assert.Equal(requestBodyBytes, data);

        httpContext.Request.Body.Position = 0;
        var result = await httpContext.Request.BodyReader.ReadAsync();
        Assert.Equal(requestBodyBytes.Length, result.Buffer.Length);
        Assert.Equal(requestBodyBytes, result.Buffer.ToArray());
        httpContext.Request.BodyReader.AdvanceTo(result.Buffer.End);

        var rawRequestBody = httpContext.Items["body"];
        Assert.NotNull(rawRequestBody);
        Assert.Equal(requestBodyBytes, (byte[])rawRequestBody!);
    }

    [Theory]
    [MemberData(nameof(RawFromBodyActions))]
    public async Task RequestDelegatePopulatesFromRawBodyParameterPipeReader(Delegate action)
    {
        var httpContext = CreateHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Name = "Write more tests!"
        });

        var pipeReader = PipeReader.Create(new MemoryStream(requestBodyBytes));
        var stream = pipeReader.AsStream();
        httpContext.Features.Set<IRequestBodyPipeFeature>(new PipeRequestBodyFeature(pipeReader));
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Length"] = requestBodyBytes.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var mock = new Mock<IServiceProvider>();
        httpContext.RequestServices = mock.Object;

        var factoryResult = RequestDelegateFactory.Create(action);

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Same(httpContext.Request.Body, stream);
        Assert.Same(httpContext.Request.BodyReader, pipeReader);

        // Assert that we can read the body from both the pipe reader and Stream after executing and verify that they are empty (the pipe reader isn't seekable here)
        int read = await httpContext.Request.Body.ReadAsync(new byte[requestBodyBytes.Length].AsMemory());
        Assert.Equal(0, read);

        var result = await httpContext.Request.BodyReader.ReadAsync();
        Assert.Equal(0, result.Buffer.Length);
        Assert.True(result.IsCompleted);
        httpContext.Request.BodyReader.AdvanceTo(result.Buffer.End);

        var rawRequestBody = httpContext.Items["body"];
        Assert.NotNull(rawRequestBody);
        Assert.Equal(requestBodyBytes, (byte[])rawRequestBody!);
    }

    class PipeRequestBodyFeature : IRequestBodyPipeFeature
    {
        public PipeRequestBodyFeature(PipeReader pipeReader)
        {
            Reader = pipeReader;
        }
        public PipeReader Reader { get; set; }
    }

    [Theory]
    [MemberData(nameof(ExplicitFromBodyActions))]
    public async Task RequestDelegateRejectsEmptyBodyGivenExplicitFromBodyParameter(Delegate action)
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "0";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(false));

        var factoryResult = RequestDelegateFactory.Create(action);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ImplicitFromBodyActions))]
    public async Task RequestDelegateRejectsEmptyBodyGivenImplicitFromBodyParameter(Delegate action)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "0";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(false));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var factoryResult = RequestDelegateFactory.Create(action, new RequestDelegateFactoryOptions() { ThrowOnBadRequest = true });
        var requestDelegate = factoryResult.RequestDelegate;

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));
        Assert.StartsWith("Implicit body inferred for parameter", ex.Message);
        Assert.EndsWith("but no body was provided. Did you mean to use a Service instead?", ex.Message);
    }

    [Fact]
    public async Task RequestDelegateAllowsEmptyBodyGivenCorrectyConfiguredFromBodyParameter()
    {
        var todoToBecomeNull = new Todo();

        void TestAction([FromBody(AllowEmpty = true)] Todo todo)
        {
            todoToBecomeNull = todo;
        }

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "0";

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "0";

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(default, structToBeZeroed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateLogsIOExceptionsAsDebugDoesNotAbortAndNeverThrows(bool throwOnBadRequests)
    {
        var invoked = false;

        void TestAction([FromBody] Todo todo)
        {
            invoked = true;
        }

        var ioException = new IOException();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(ioException);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction, new() { ThrowOnBadRequest = throwOnBadRequests });
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal("Reading the request body failed with an IOException.", logMessage.Message);
        Assert.Same(ioException, logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateLogsJsonExceptionsAsDebugAndSets400Response()
    {
        var invoked = false;

        void TestAction([FromBody] Todo todo)
        {
            invoked = true;
        }

        var jsonException = new JsonException();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(jsonException);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(2, "InvalidJsonRequestBody"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal(@"Failed to read parameter ""Todo todo"" from the request body as JSON.", logMessage.Message);
        Assert.Same(jsonException, logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateThrowsForJsonExceptionsIfThrowOnBadRequest()
    {
        var invoked = false;

        void TestAction([FromBody] Todo todo)
        {
            invoked = true;
        }

        var jsonException = new JsonException();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(jsonException);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction, new() { ThrowOnBadRequest = true });
        var requestDelegate = factoryResult.RequestDelegate;

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to read parameter ""Todo todo"" from the request body as JSON.", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
        Assert.Same(jsonException, badHttpRequestException.InnerException);
    }

    [Fact]
    public async Task RequestDelegateLogsMalformedJsonAsDebugAndSets400Response()
    {
        var invoked = false;

        void TestAction([FromBody] Todo todo)
        {
            invoked = true;
        }

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{"));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(2, "InvalidJsonRequestBody"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal(@"Failed to read parameter ""Todo todo"" from the request body as JSON.", logMessage.Message);
        Assert.IsType<JsonException>(logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateThrowsForMalformedJsonIfThrowOnBadRequest()
    {
        var invoked = false;

        void TestAction([FromBody] Todo todo)
        {
            invoked = true;
        }

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{"));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction, new() { ThrowOnBadRequest = true });
        var requestDelegate = factoryResult.RequestDelegate;

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to read parameter ""Todo todo"" from the request body as JSON.", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
        Assert.IsType<JsonException>(badHttpRequestException.InnerException);
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

    [Fact]
    public void BuildRequestDelegateThrowsInvalidOperationExceptionForInvalidTryParse()
    {
        void TestTryParseStruct(BadTryParseStruct value1) { }
        void TestTryParseClass(BadTryParseClass value1) { }

        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestTryParseStruct));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestTryParseClass));
    }

    private struct BadTryParseStruct
    {
        public static void TryParse(string? value, out BadTryParseStruct result) { }
    }

    private class BadTryParseClass
    {
        public static void TryParse(string? value, out BadTryParseClass result)
        {
            result = new();
        }
    }

    [Fact]
    public void BuildRequestDelegateThrowsInvalidOperationExceptionForInvalidBindAsync()
    {
        void TestBindAsyncStruct(BadBindAsyncStruct value1) { }
        void TestBindAsyncClass(BadBindAsyncClass value1) { }

        var ex = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestBindAsyncStruct));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestBindAsyncClass));
    }

    private struct BadBindAsyncStruct
    {
        public static Task<BadBindAsyncStruct> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    private class BadBindAsyncClass
    {
        public static Task<BadBindAsyncClass> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    public static object[][] ExplicitFromServiceActions
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

            void TestExplicitMultipleFromService(HttpContext httpContext, [FromService] MyService myService, [FromService] IEnumerable<MyService> myServices)
            {
                httpContext.Items.Add("service", myService);
            }

            return new object[][]
            {
                    new[] { (Action<HttpContext, MyService>)TestExplicitFromService },
                    new[] { (Action<HttpContext, IEnumerable<MyService>>)TestExplicitFromIEnumerableService },
                    new[] { (Action<HttpContext, MyService, IEnumerable<MyService>>)TestExplicitMultipleFromService },
            };
        }
    }

    public static object[][] ImplicitFromServiceActions
    {
        get
        {
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
                    new[] { (Action<HttpContext, IMyService>)TestImpliedFromService },
                    new[] { (Action<HttpContext, IEnumerable<MyService>>)TestImpliedIEnumerableFromService },
                    new[] { (Action<HttpContext, MyService>)TestImpliedFromServiceBasedOnContainer },
            };
        }
    }

    public static object[][] FromServiceActions
    {
        get
        {
            return ImplicitFromServiceActions.Concat(ExplicitFromServiceActions).ToArray();
        }
    }

    [Theory]
    [MemberData(nameof(ImplicitFromServiceActions))]
    public async Task RequestDelegateRequiresServiceForAllImplicitFromServiceParameters(Delegate action)
    {
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(action);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var message = Assert.Single(TestSink.Writes).Message;
        Assert.StartsWith("Implicit body inferred for parameter", message);
        Assert.EndsWith("but no body was provided. Did you mean to use a Service instead?", message);
    }

    [Theory]
    [MemberData(nameof(ExplicitFromServiceActions))]
    public async Task RequestDelegateWithExplicitFromServiceParameters(Delegate action)
    {
        // IEnumerable<T> always resolves from DI but is empty and throws from test method
        if (action.Method.Name.Contains("TestExplicitFromIEnumerableService", StringComparison.Ordinal))
        {
            return;
        }

        var httpContext = CreateHttpContext();

        var requestDelegateResult = RequestDelegateFactory.Create(action);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => requestDelegateResult.RequestDelegate(httpContext));
        Assert.Equal("No service for type 'Microsoft.AspNetCore.Routing.Internal.RequestDelegateFactoryTests+MyService' has been registered.", ex.Message);
    }

    [Theory]
    [MemberData(nameof(FromServiceActions))]
    public async Task RequestDelegatePopulatesParametersFromServiceWithAndWithoutAttribute(Delegate action)
    {
        var myOriginalService = new MyService();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        serviceCollection.AddSingleton(myOriginalService);
        serviceCollection.AddSingleton<IMyService>(myOriginalService);

        var services = serviceCollection.BuildServiceProvider();

        using var requestScoped = services.CreateScope();

        var httpContext = CreateHttpContext();
        httpContext.RequestServices = requestScoped.ServiceProvider;

        var factoryResult = RequestDelegateFactory.Create(action, options: new() { ServiceProvider = services });
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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
        var httpContext = CreateHttpContext();
        // Reset back to default HttpRequestLifetimeFeature that implements a setter for RequestAborted.
        httpContext.Features.Set<IHttpRequestLifetimeFeature>(new HttpRequestLifetimeFeature());
        httpContext.RequestAborted = cts.Token;

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();
        httpContext.User = new ClaimsPrincipal();

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

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
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

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

            StructResult TestStructAction() => new StructResult(resultString);
            Task<StructResult> TaskTestStructAction() => Task.FromResult(new StructResult(resultString));
            ValueTask<StructResult> ValueTaskTestStructAction() => ValueTask.FromResult(new StructResult(resultString));

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

                    new object[] { (Func<StructResult>)TestStructAction },
                    new object[] { (Func<Task<StructResult>>)TaskTestStructAction },
                    new object[] { (Func<ValueTask<StructResult>>)ValueTaskTestStructAction },
                };
        }
    }

    [Theory]
    [MemberData(nameof(CustomResults))]
    public async Task RequestDelegateUsesCustomIResult(Delegate @delegate)
    {
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

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
    public async Task RequestDelegateWritesStringReturnValueAndSetContentTypeWhenNull(Delegate @delegate)
    {
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var responseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());

        Assert.Equal("String Test", responseBody);
        Assert.Equal("text/plain; charset=utf-8", httpContext.Response.ContentType);
    }

    [Theory]
    [MemberData(nameof(StringResult))]
    public async Task RequestDelegateWritesStringReturnDoNotChangeContentType(Delegate @delegate)
    {
        var httpContext = CreateHttpContext();
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
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
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

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
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

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
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

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
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

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
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        if (queryParam is not null)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                [paramName] = queryParam
            });
        }

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var logs = TestSink.Writes.ToArray();

        if (isInvalid)
        {
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(logs);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
            var expectedType = paramName == "age" ? "int age" : "string name";
            Assert.Equal($@"Required parameter ""{expectedType}"" was not provided from route or query string.", log.Message);
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
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        if (routeParam is not null)
        {
            httpContext.Request.RouteValues[paramName] = routeParam;
        }

        var factoryResult = RequestDelegateFactory.Create(@delegate, new()
        {
            RouteParameterNames = routeParam is not null ? new[] { paramName } : Array.Empty<string>()
        });

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var logs = TestSink.Writes.ToArray();

        if (isInvalid)
        {
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(logs);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
            var expectedType = paramName == "age" ? "int age" : "string name";
            Assert.Equal($@"Required parameter ""{expectedType}"" was not provided from query string.", log.Message);
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
        var httpContext = CreateHttpContext();
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
            httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        }

        var jsonOptions = new JsonOptions();
        jsonOptions.SerializerOptions.Converters.Add(new TodoJsonConverter());

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        serviceCollection.AddSingleton(Options.Create(jsonOptions));
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        var request = requestDelegate(httpContext);

        if (isInvalid)
        {
            var logs = TestSink.Writes.ToArray();
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(logs);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(5, "ImplicitBodyNotProvided"), log.EventId);
            Assert.Equal(@"Implicit body inferred for parameter ""todo"" but no body was provided. Did you mean to use a Service instead?", log.Message);
        }
        else
        {
            await request;
            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
            var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
            Assert.Equal(expectedResponse, decodedResponseBody);
        }
    }

    public static IEnumerable<object?[]> BindAsyncParamOptionalityData
    {
        get
        {
            void requiredReferenceType(HttpContext context, MyBindAsyncRecord myBindAsyncRecord)
            {
                context.Items["uri"] = myBindAsyncRecord.Uri;
            }
            void defaultReferenceType(HttpContext context, MyBindAsyncRecord? myBindAsyncRecord = null)
            {
                context.Items["uri"] = myBindAsyncRecord?.Uri;
            }
            void nullableReferenceType(HttpContext context, MyBindAsyncRecord? myBindAsyncRecord)
            {
                context.Items["uri"] = myBindAsyncRecord?.Uri;
            }
            void requiredReferenceTypeSimple(HttpContext context, MySimpleBindAsyncRecord mySimpleBindAsyncRecord)
            {
                context.Items["uri"] = mySimpleBindAsyncRecord.Uri;
            }


            void requiredValueType(HttpContext context, MyNullableBindAsyncStruct myNullableBindAsyncStruct)
            {
                context.Items["uri"] = myNullableBindAsyncStruct.Uri;
            }
            void defaultValueType(HttpContext context, MyNullableBindAsyncStruct? myNullableBindAsyncStruct = null)
            {
                context.Items["uri"] = myNullableBindAsyncStruct?.Uri;
            }
            void nullableValueType(HttpContext context, MyNullableBindAsyncStruct? myNullableBindAsyncStruct)
            {
                context.Items["uri"] = myNullableBindAsyncStruct?.Uri;
            }
            void requiredValueTypeSimple(HttpContext context, MySimpleBindAsyncStruct mySimpleBindAsyncStruct)
            {
                context.Items["uri"] = mySimpleBindAsyncStruct.Uri;
            }

            return new object?[][]
            {
                    new object?[] { (Action<HttpContext, MyBindAsyncRecord>)requiredReferenceType, false, true, false },
                    new object?[] { (Action<HttpContext, MyBindAsyncRecord>)requiredReferenceType, true, false, false, },
                    new object?[] { (Action<HttpContext, MySimpleBindAsyncRecord>)requiredReferenceTypeSimple, true, false, false },

                    new object?[] { (Action<HttpContext, MyBindAsyncRecord?>)defaultReferenceType, false, false, false, },
                    new object?[] { (Action<HttpContext, MyBindAsyncRecord?>)defaultReferenceType, true, false, false },

                    new object?[] { (Action<HttpContext, MyBindAsyncRecord?>)nullableReferenceType, false, false, false },
                    new object?[] { (Action<HttpContext, MyBindAsyncRecord?>)nullableReferenceType, true, false, false },

                    new object?[] { (Action<HttpContext, MyNullableBindAsyncStruct>)requiredValueType, false, true, true },
                    new object?[] { (Action<HttpContext, MyNullableBindAsyncStruct>)requiredValueType, true, false, true },
                    new object?[] { (Action<HttpContext, MySimpleBindAsyncStruct>)requiredValueTypeSimple, true, false, true },

                    new object?[] { (Action<HttpContext, MyNullableBindAsyncStruct?>)defaultValueType, false, false, true },
                    new object?[] { (Action<HttpContext, MyNullableBindAsyncStruct?>)defaultValueType, true, false, true },

                    new object?[] { (Action<HttpContext, MyNullableBindAsyncStruct?>)nullableValueType, false, false, true },
                    new object?[] { (Action<HttpContext, MyNullableBindAsyncStruct?>)nullableValueType, true, false, true },
            };
        }
    }

    [Theory]
    [MemberData(nameof(BindAsyncParamOptionalityData))]
    public async Task RequestDelegateHandlesBindAsyncOptionality(Delegate routeHandler, bool includeReferer, bool isInvalid, bool isStruct)
    {
        var httpContext = CreateHttpContext();

        if (includeReferer)
        {
            httpContext.Request.Headers.Referer = "https://example.org";
        }

        var factoryResult = RequestDelegateFactory.Create(routeHandler);

        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.False(httpContext.RequestAborted.IsCancellationRequested);

        if (isInvalid)
        {
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(TestSink.Writes);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);

            if (isStruct)
            {
                Assert.Equal(@"Required parameter ""MyNullableBindAsyncStruct myNullableBindAsyncStruct"" was not provided from MyNullableBindAsyncStruct.BindAsync(HttpContext, ParameterInfo).", log.Message);
            }
            else
            {
                Assert.Equal(@"Required parameter ""MyBindAsyncRecord myBindAsyncRecord"" was not provided from MyBindAsyncRecord.BindAsync(HttpContext, ParameterInfo).", log.Message);
            }
        }
        else
        {
            Assert.Equal(200, httpContext.Response.StatusCode);

            if (includeReferer)
            {
                Assert.Equal(new Uri("https://example.org"), httpContext.Items["uri"]);
            }
            else
            {
                Assert.Null(httpContext.Items["uri"]);
            }
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
        var httpContext = CreateHttpContext();

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

        var factoryResult = RequestDelegateFactory.Create(@delegate, options);
        var requestDelegate = factoryResult.RequestDelegate;

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
        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var logs = TestSink.Writes.ToArray();

        if (!allowsEmptyRequest)
        {
            Assert.Equal(400, httpContext.Response.StatusCode);
            var log = Assert.Single(logs);
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), log.EventId);
            Assert.Equal(@"Required parameter ""Todo todo"" was not provided from body.", log.Message);
        }
        else
        {
            Assert.Equal(200, httpContext.Response.StatusCode);
            Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        }
    }

#nullable disable

    [Theory]
    [InlineData(true, "Hello TestName!")]
    [InlineData(false, "Hello !")]
    public async Task CanSetStringParamAsOptionalWithNullabilityDisability(bool provideValue, string expectedResponse)
    {
        string optionalQueryParam(string name = null) => $"Hello {name}!";

        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        if (provideValue)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["name"] = "TestName"
            });
        }

        var factoryResult = RequestDelegateFactory.Create(optionalQueryParam);
        var requestDelegate = factoryResult.RequestDelegate;

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

        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        if (provideValue)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["age"] = "42"
            });
        }

        var factoryResult = RequestDelegateFactory.Create(optionalQueryParam);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(expectedResponse, decodedResponseBody);
    }

    [Theory]
    [InlineData(true, "Age: 42")]
    [InlineData(false, "Age: ")]
    public async Task TreatsUnknownNullabilityAsOptionalForReferenceType(bool provideValue, string expectedResponse)
    {
        string optionalQueryParam(string age) => $"Age: {age}";

        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        if (provideValue)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                ["age"] = "42"
            });
        }

        var factoryResult = RequestDelegateFactory.Create(optionalQueryParam);

        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(expectedResponse, decodedResponseBody);
    }

#nullable enable

    [Fact]
    public async Task CanExecuteRequestDelegateWithResultsExtension()
    {
        IResult actionWithExtensionsResult(string name) => Results.Extensions.TestResult(name);

        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "Tester"
        });

        var factoryResult = RequestDelegateFactory.Create(actionWithExtensionsResult);

        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(@"""Hello Tester. This is from an extension method.""", decodedResponseBody);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateRejectsNonJsonContent(bool shouldThrow)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/xml";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var factoryResult = RequestDelegateFactory.Create((HttpContext context, Todo todo) =>
        {
        }, new RequestDelegateFactoryOptions() { ThrowOnBadRequest = shouldThrow });
        var requestDelegate = factoryResult.RequestDelegate;

        var request = requestDelegate(httpContext);

        if (shouldThrow)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => request);
            Assert.Equal("Expected a supported JSON media type but got \"application/xml\".", ex.Message);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, ex.StatusCode);
        }
        else
        {
            await request;

            Assert.Equal(415, httpContext.Response.StatusCode);
            var logMessage = Assert.Single(TestSink.Writes);
            Assert.Equal(new EventId(6, "UnexpectedContentType"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Equal("Expected a supported JSON media type but got \"application/xml\".", logMessage.Message);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateWithBindAndImplicitBodyRejectsNonJsonContent(bool shouldThrow)
    {
        Todo originalTodo = new()
        {
            Name = "Write more tests!"
        };

        var httpContext = new DefaultHttpContext();

        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(originalTodo);
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "application/xml";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var factoryResult = RequestDelegateFactory.Create((HttpContext context, JsonTodo customTodo, Todo todo) =>
        {
        }, new RequestDelegateFactoryOptions() { ThrowOnBadRequest = shouldThrow });
        var requestDelegate = factoryResult.RequestDelegate;

        var request = requestDelegate(httpContext);

        if (shouldThrow)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => request);
            Assert.Equal("Expected a supported JSON media type but got \"application/xml\".", ex.Message);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, ex.StatusCode);
        }
        else
        {
            await request;

            Assert.Equal(415, httpContext.Response.StatusCode);
            var logMessage = Assert.Single(TestSink.Writes);
            Assert.Equal(new EventId(6, "UnexpectedContentType"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Equal("Expected a supported JSON media type but got \"application/xml\".", logMessage.Message);
        }
    }

    public static IEnumerable<object?[]> DateTimeDelegates
    {
        get
        {
            string dateTimeParsing(DateTime time) => $"Time: {time.ToString("O", CultureInfo.InvariantCulture)}, Kind: {time.Kind}";

            return new List<object?[]>
                {
                    new object?[] { (Func<DateTime, string>)dateTimeParsing, "9/20/2021 4:18:44 PM", "Time: 2021-09-20T16:18:44.0000000, Kind: Unspecified" },
                    new object?[] { (Func<DateTime, string>)dateTimeParsing, "2021-09-20 4:18:44", "Time: 2021-09-20T04:18:44.0000000, Kind: Unspecified" },
                    new object?[] { (Func<DateTime, string>)dateTimeParsing, "   9/20/2021    4:18:44 PM  ", "Time: 2021-09-20T16:18:44.0000000, Kind: Unspecified" },
                    new object?[] { (Func<DateTime, string>)dateTimeParsing, "2021-09-20T16:28:02.000-07:00", "Time: 2021-09-20T23:28:02.0000000Z, Kind: Utc" },
                    new object?[] { (Func<DateTime, string>)dateTimeParsing, "  2021-09-20T 16:28:02.000-07:00  ", "Time: 2021-09-20T23:28:02.0000000Z, Kind: Utc" },
                    new object?[] { (Func<DateTime, string>)dateTimeParsing, "2021-09-20T23:30:02.000+00:00", "Time: 2021-09-20T23:30:02.0000000Z, Kind: Utc" },
                    new object?[] { (Func<DateTime, string>)dateTimeParsing, "     2021-09-20T23:30: 02.000+00:00 ", "Time: 2021-09-20T23:30:02.0000000Z, Kind: Utc" },
                    new object?[] { (Func<DateTime, string>)dateTimeParsing, "2021-09-20 16:48:02-07:00", "Time: 2021-09-20T23:48:02.0000000Z, Kind: Utc" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DateTimeDelegates))]
    public async Task RequestDelegateCanProcessDateTimesToUtc(Delegate @delegate, string inputTime, string expectedResponse)
    {
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["time"] = inputTime
        });

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(expectedResponse, decodedResponseBody);
    }

    public static IEnumerable<object?[]> DateTimeOffsetDelegates
    {
        get
        {
            string dateTimeOffsetParsing(DateTimeOffset time) => $"Time: {time.ToString("O", CultureInfo.InvariantCulture)}, Offset: {time.Offset}";

            return new List<object?[]>
                {
                    new object?[] { (Func<DateTimeOffset, string>)dateTimeOffsetParsing, "09/20/2021 16:35:12 +00:00", "Time: 2021-09-20T16:35:12.0000000+00:00, Offset: 00:00:00" },
                    new object?[] { (Func<DateTimeOffset, string>)dateTimeOffsetParsing, "09/20/2021 11:35:12 +07:00", "Time: 2021-09-20T11:35:12.0000000+07:00, Offset: 07:00:00" },
                    new object?[] { (Func<DateTimeOffset, string>)dateTimeOffsetParsing, "09/20/2021 16:35:12", "Time: 2021-09-20T16:35:12.0000000+00:00, Offset: 00:00:00" },
                    new object?[] { (Func<DateTimeOffset, string>)dateTimeOffsetParsing, " 09/20/2021 16:35:12 ", "Time: 2021-09-20T16:35:12.0000000+00:00, Offset: 00:00:00" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DateTimeOffsetDelegates))]
    public async Task RequestDelegateCanProcessDateTimeOffsetsToUtc(Delegate @delegate, string inputTime, string expectedResponse)
    {
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["time"] = inputTime
        });

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(expectedResponse, decodedResponseBody);
    }

    public static IEnumerable<object?[]> DateOnlyDelegates
    {
        get
        {
            string dateOnlyParsing(DateOnly time) => $"Time: {time.ToString("O", CultureInfo.InvariantCulture)}";

            return new List<object?[]>
                {
                    new object?[] { (Func<DateOnly, string>)dateOnlyParsing, "9/20/2021", "Time: 2021-09-20" },
                    new object?[] { (Func<DateOnly, string>)dateOnlyParsing, "9 /20 /2021", "Time: 2021-09-20" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(DateOnlyDelegates))]
    public async Task RequestDelegateCanProcessDateOnlyValues(Delegate @delegate, string inputTime, string expectedResponse)
    {
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["time"] = inputTime
        });

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(expectedResponse, decodedResponseBody);
    }

    public static IEnumerable<object?[]> TimeOnlyDelegates
    {
        get
        {
            string timeOnlyParsing(TimeOnly time) => $"Time: {time.ToString("O", CultureInfo.InvariantCulture)}";

            return new List<object?[]>
                {
                    new object?[] { (Func<TimeOnly, string>)timeOnlyParsing, "4:34 PM", "Time: 16:34:00.0000000" },
                    new object?[] { (Func<TimeOnly, string>)timeOnlyParsing, "    4:34 PM   ", "Time: 16:34:00.0000000" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(TimeOnlyDelegates))]
    public async Task RequestDelegateCanProcessTimeOnlyValues(Delegate @delegate, string inputTime, string expectedResponse)
    {
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["time"] = inputTime
        });

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(expectedResponse, decodedResponseBody);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateLogsIOExceptionsForFormAsDebugDoesNotAbortAndNeverThrows(bool throwOnBadRequests)
    {
        var invoked = false;

        void TestAction(IFormFile file)
        {
            invoked = true;
        }

        var ioException = new IOException();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(ioException);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction, new() { ThrowOnBadRequest = throwOnBadRequests });
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal("Reading the request body failed with an IOException.", logMessage.Message);
        Assert.Same(ioException, logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateLogsMalformedFormAsDebugAndSets400Response()
    {
        var invoked = false;

        void TestAction(IFormFile file)
        {
            invoked = true;
        }

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "2049";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', 2049)));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(8, "InvalidFormRequestBody"), logMessage.EventId);
        Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        Assert.Equal(@"Failed to read parameter ""IFormFile file"" from the request body as form.", logMessage.Message);
        Assert.IsType<InvalidDataException>(logMessage.Exception);
    }

    [Fact]
    public async Task RequestDelegateThrowsForMalformedFormIfThrowOnBadRequest()
    {
        var invoked = false;

        void TestAction(IFormFile file)
        {
            invoked = true;
        }

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "2049";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', 2049)));
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction, new() { ThrowOnBadRequest = true });
        var requestDelegate = factoryResult.RequestDelegate;

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to read parameter ""IFormFile file"" from the request body as form.", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
        Assert.IsType<InvalidDataException>(badHttpRequestException.InnerException);
    }

    [Fact]
    public void BuildRequestDelegateThrowsInvalidOperationExceptionBodyAndFormFileParameters()
    {
        void TestFormFileAndJson(IFormFile value1, Todo value2) { }
        void TestFormFilesAndJson(IFormFile value1, IFormFile value2, Todo value3) { }
        void TestFormFileCollectionAndJson(IFormFileCollection value1, Todo value2) { }
        void TestFormFileAndJsonWithAttribute(IFormFile value1, [FromBody] int value2) { }
        void TestJsonAndFormFile(Todo value1, IFormFile value2) { }
        void TestJsonAndFormFiles(Todo value1, IFormFile value2, IFormFile value3) { }
        void TestJsonAndFormFileCollection(Todo value1, IFormFileCollection value2) { }
        void TestJsonAndFormFileWithAttribute(Todo value1, [FromForm] IFormFile value2) { }

        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormFileAndJson));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormFilesAndJson));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormFileAndJsonWithAttribute));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormFileCollectionAndJson));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormFile));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormFiles));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormFileCollection));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormFileWithAttribute));
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileCollectionParameter()
    {
        IFormFileCollection? formFilesArgument = null;

        void TestAction(IFormFileCollection formFiles)
        {
            formFilesArgument = formFiles;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files, formFilesArgument);
        Assert.NotNull(formFilesArgument!["file"]);

        var allAcceptsMetadata = factoryResult.EndpointMetadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data" }, acceptsMetadata.ContentTypes);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileCollectionParameterWithAttribute()
    {
        IFormFileCollection? formFilesArgument = null;

        void TestAction([FromForm] IFormFileCollection formFiles)
        {
            formFilesArgument = formFiles;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files, formFilesArgument);
        Assert.NotNull(formFilesArgument!["file"]);

        var allAcceptsMetadata = factoryResult.EndpointMetadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data" }, acceptsMetadata.ContentTypes);
    }

    [Fact]
    public void CreateThrowsNotSupportedExceptionIfIFormFileCollectionHasMetadataParameterName()
    {
        IFormFileCollection? formFilesArgument = null;

        void TestAction([FromForm(Name = "foo")] IFormFileCollection formFiles)
        {
            formFilesArgument = formFiles;
        }

        var nse = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(TestAction));
        Assert.Equal("Assigning a value to the IFromFormMetadata.Name property is not supported for parameters of type IFormFileCollection.", nse.Message);
    }

    [Fact]
    public void CreateThrowsNotSupportedExceptionIfFromFormParameterIsNotIFormFileCollectionOrIFormFile()
    {
        void TestActionBool([FromForm] bool value) { };
        void TestActionInt([FromForm] int value) { };
        void TestActionObject([FromForm] object value) { };
        void TestActionString([FromForm] string value) { };
        void TestActionCancellationToken([FromForm] CancellationToken value) { };
        void TestActionClaimsPrincipal([FromForm] ClaimsPrincipal value) { };
        void TestActionHttpContext([FromForm] HttpContext value) { };
        void TestActionIFormCollection([FromForm] IFormCollection value) { };

        AssertNotSupportedExceptionThrown(TestActionBool);
        AssertNotSupportedExceptionThrown(TestActionInt);
        AssertNotSupportedExceptionThrown(TestActionObject);
        AssertNotSupportedExceptionThrown(TestActionString);
        AssertNotSupportedExceptionThrown(TestActionCancellationToken);
        AssertNotSupportedExceptionThrown(TestActionClaimsPrincipal);
        AssertNotSupportedExceptionThrown(TestActionHttpContext);
        AssertNotSupportedExceptionThrown(TestActionIFormCollection);

        static void AssertNotSupportedExceptionThrown(Delegate handler)
        {
            var nse = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(handler));
            Assert.Equal("IFromFormMetadata is only supported for parameters of type IFormFileCollection and IFormFile.", nse.Message);
        }
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileParameter()
    {
        IFormFile? fileArgument = null;

        void TestAction(IFormFile file)
        {
            fileArgument = file;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file"], fileArgument);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromOptionalIFormFileParameter()
    {
        IFormFile? fileArgument = null;

        void TestAction(IFormFile? file)
        {
            fileArgument = file;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file"], fileArgument);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromMultipleRequiredIFormFileParameters()
    {
        IFormFile? file1Argument = null;
        IFormFile? file2Argument = null;

        void TestAction(IFormFile file1, IFormFile file2)
        {
            file1Argument = file1;
            file2Argument = file2;
        }

        var fileContent1 = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var fileContent2 = new StringContent("there", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent1, "file1", "file1.txt");
        form.Add(fileContent2, "file2", "file2.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file1"], file1Argument);
        Assert.Equal("file1.txt", file1Argument!.FileName);
        Assert.Equal("file1", file1Argument.Name);

        Assert.Equal(httpContext.Request.Form.Files["file2"], file2Argument);
        Assert.Equal("file2.txt", file2Argument!.FileName);
        Assert.Equal("file2", file2Argument.Name);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromOptionalMissingIFormFileParameter()
    {
        IFormFile? file1Argument = null;
        IFormFile? file2Argument = null;

        void TestAction(IFormFile? file1, IFormFile? file2)
        {
            file1Argument = file1;
            file2Argument = file2;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file1", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file1"], file1Argument);
        Assert.NotNull(file1Argument);

        Assert.Equal(httpContext.Request.Form.Files["file2"], file2Argument);
        Assert.Null(file2Argument);

        var allAcceptsMetadata = factoryResult.EndpointMetadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data" }, acceptsMetadata.ContentTypes);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileParameterWithMetadata()
    {
        IFormFile? fileArgument = null;

        void TestAction([FromForm(Name = "my_file")] IFormFile file)
        {
            fileArgument = file;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "my_file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["my_file"], fileArgument);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("my_file", fileArgument.Name);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromIFormFileAndBoundParameter()
    {
        IFormFile? fileArgument = null;
        TraceIdentifier traceIdArgument = default;

        void TestAction(IFormFile? file, TraceIdentifier traceId)
        {
            fileArgument = file;
            traceIdArgument = traceId;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));
        httpContext.TraceIdentifier = "my-trace-id";

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files["file"], fileArgument);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);

        Assert.Equal("my-trace-id", traceIdArgument.Id);
    }

    private readonly struct TraceIdentifier
    {
        private TraceIdentifier(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public static implicit operator string(TraceIdentifier value) => value.Id;

        public static ValueTask<TraceIdentifier> BindAsync(HttpContext context)
        {
            return ValueTask.FromResult(new TraceIdentifier(context.TraceIdentifier));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RequestDelegateRejectsNonFormContent(bool shouldThrow)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/xml";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(LoggerFactory);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var factoryResult = RequestDelegateFactory.Create((HttpContext context, IFormFile file) =>
        {
        }, new RequestDelegateFactoryOptions() { ThrowOnBadRequest = shouldThrow });
        var requestDelegate = factoryResult.RequestDelegate;

        var request = requestDelegate(httpContext);

        if (shouldThrow)
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => request);
            Assert.Equal("Expected a supported form media type but got \"application/xml\".", ex.Message);
            Assert.Equal(StatusCodes.Status415UnsupportedMediaType, ex.StatusCode);
        }
        else
        {
            await request;

            Assert.Equal(415, httpContext.Response.StatusCode);
            var logMessage = Assert.Single(TestSink.Writes);
            Assert.Equal(new EventId(7, "UnexpectedContentType"), logMessage.EventId);
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
            Assert.Equal("Expected a supported form media type but got \"application/xml\".", logMessage.Message);
        }
    }

    [Fact]
    public async Task RequestDelegateSets400ResponseIfRequiredFileNotSpecified()
    {
        var invoked = false;

        void TestAction(IFormFile file)
        {
            invoked = true;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "some-other-file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RequestDelegatePopulatesFromBothFormFileCollectionAndFormFileParameters()
    {
        IFormFileCollection? formFilesArgument = null;
        IFormFile? fileArgument = null;

        void TestAction(IFormFileCollection formFiles, IFormFile file)
        {
            formFilesArgument = formFiles;
            fileArgument = file;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(httpContext.Request.Form.Files, formFilesArgument);
        Assert.NotNull(formFilesArgument!["file"]);

        Assert.Equal(httpContext.Request.Form.Files["file"], fileArgument);
        Assert.Equal("file.txt", fileArgument!.FileName);
        Assert.Equal("file", fileArgument.Name);

        var allAcceptsMetadata = factoryResult.EndpointMetadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data" }, acceptsMetadata.ContentTypes);
    }

    [Theory]
    [InlineData("Authorization", "bearer my-token", "Support for binding parameters from an HTTP request's form is not currently supported if the request contains an \"Authorization\" HTTP request header. Use of an HTTP request form is not currently secure for HTTP requests in scenarios which require authentication.")]
    [InlineData("Cookie", ".AspNetCore.Auth=abc123", "Support for binding parameters from an HTTP request's form is not currently supported if the request contains a \"Cookie\" HTTP request header. Use of an HTTP request form is not currently secure for HTTP requests in scenarios which require authentication.")]
    public async Task RequestDelegateThrowsIfRequestUsingFormContainsSecureHeader(
        string headerName,
        string headerValue,
        string expectedMessage)
    {
        var invoked = false;

        void TestAction(IFormFile file)
        {
            invoked = true;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers[headerName] = headerValue;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(expectedMessage, badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    [Fact]
    public async Task RequestDelegateThrowsIfRequestUsingFormHasClientCertificate()
    {
        var invoked = false;

        void TestAction(IFormFile file)
        {
            invoked = true;
        }

        var fileContent = new StringContent("hello", Encoding.UTF8, "application/octet-stream");
        var form = new MultipartFormDataContent("some-boundary");
        form.Add(fileContent, "file", "file.txt");

        var stream = new MemoryStream();
        await form.CopyToAsync(stream);

        stream.Seek(0, SeekOrigin.Begin);

        var httpContext = CreateHttpContext();
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Content-Type"] = "multipart/form-data;boundary=some-boundary";
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

#pragma warning disable SYSLIB0026 // Type or member is obsolete
        var clientCertificate = new X509Certificate2();
#pragma warning restore SYSLIB0026 // Type or member is obsolete

        httpContext.Features.Set<ITlsConnectionFeature>(new TlsConnectionFeature(clientCertificate));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal("Support for binding parameters from an HTTP request's form is not currently supported if the request is associated with a client certificate. Use of an HTTP request form is not currently secure for HTTP requests in scenarios which require authentication.", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    private DefaultHttpContext CreateHttpContext()
    {
        var responseFeature = new TestHttpResponseFeature();

        return new()
        {
            RequestServices = new ServiceCollection().AddSingleton(LoggerFactory).BuildServiceProvider(),
            Features =
                {
                    [typeof(IHttpResponseFeature)] = responseFeature,
                    [typeof(IHttpResponseBodyFeature)] = responseFeature,
                    [typeof(IHttpRequestLifetimeFeature)] = new TestHttpRequestLifetimeFeature(),
                }
        };
    }

    private class Todo : ITodo
    {
        public int Id { get; set; }
        public string? Name { get; set; } = "Todo";
        public bool IsComplete { get; set; }
    }

    private class CustomTodo : Todo
    {
        public static async ValueTask<CustomTodo?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.Equal(typeof(CustomTodo), parameter.ParameterType);
            Assert.Equal("customTodo", parameter.Name);

            var body = await context.Request.ReadFromJsonAsync<CustomTodo>();
            context.Request.Body.Position = 0;
            return body;
        }
    }

    private class JsonTodo : Todo
    {
        public static async ValueTask<JsonTodo?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            // manually call deserialize so we don't check content type
            var body = await JsonSerializer.DeserializeAsync<JsonTodo>(context.Request.Body);
            context.Request.Body.Position = 0;
            return body;
        }
    }

    private record struct TodoStruct(int Id, string? Name, bool IsComplete) : ITodo;

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

    private class FromFormAttribute : Attribute, IFromFormMetadata
    {
        public string? Name { get; set; }
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

    private struct StructResult : IResult
    {
        private readonly string _resultString;

        public StructResult(string resultString)
        {
            _resultString = resultString;
        }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            return httpContext.Response.WriteAsync(_resultString);
        }
    }

    private class ExceptionThrowingRequestBodyStream : Stream
    {
        private readonly Exception _exceptionToThrow;

        public ExceptionThrowingRequestBodyStream(Exception exceptionToThrow)
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

    private class TestHttpResponseFeature : IHttpResponseFeature, IHttpResponseBodyFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public bool HasStarted { get; private set; }

        // Assume any access to the response Body/Stream/Writer is writing for test purposes.
        public Stream Body
        {
            get
            {
                HasStarted = true;
                return Stream.Null;
            }
            set
            {
            }
        }

        public Stream Stream
        {
            get
            {
                HasStarted = true;
                return Stream.Null;
            }
        }

        public PipeWriter Writer
        {
            get
            {
                HasStarted = true;
                return PipeWriter.Create(Stream.Null);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            HasStarted = true;
            return Task.CompletedTask;
        }

        public Task CompleteAsync()
        {
            HasStarted = true;
            return Task.CompletedTask;
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            HasStarted = true;
            return Task.CompletedTask;
        }

        public void DisableBuffering()
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }

    private class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
    {
        public RequestBodyDetectionFeature(bool canHaveBody)
        {
            CanHaveBody = canHaveBody;
        }

        public bool CanHaveBody { get; }
    }

    private class TlsConnectionFeature : ITlsConnectionFeature
    {
        public TlsConnectionFeature(X509Certificate2 clientCertificate)
        {
            ClientCertificate = clientCertificate;
        }

        public X509Certificate2? ClientCertificate { get; set; }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

internal static class TestExtensionResults
{
    public static IResult TestResult(this IResultExtensions resultExtensions, string name)
    {
        return Results.Ok(FormattableString.Invariant($"Hello {name}. This is from an extension method."));
    }
}
