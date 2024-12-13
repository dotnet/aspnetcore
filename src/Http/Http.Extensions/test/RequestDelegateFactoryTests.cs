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
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Generators.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Routing.Internal;

public partial class RequestDelegateFactoryTests : LoggedTest
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

        var factoryResult = RequestDelegateFactory.Create(@delegate, new RequestDelegateFactoryOptions()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) => async (context) =>
                {
                    var response = await next(context);
                    Assert.IsType<EmptyHttpResult>(response);
                    return response;
                }
            }),
        });
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

    private static void TestOptional(HttpContext httpContext, [FromRoute] int value = 42)
    {
        httpContext.Items.Add("input", value);
    }

    private static void TestOptionalNullable(HttpContext httpContext, int? value = 42)
    {
        httpContext.Items.Add("input", value);
    }

    private static void TestOptionalNullableNull(HttpContext httpContext, double? value = null)
    {
        httpContext.Items.Add("input", (object?)value ?? "Null");
    }

    private static void TestOptionalString(HttpContext httpContext, string value = "default")
    {
        httpContext.Items.Add("input", value);
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

    public static object?[][] TryParsableArrayParameters
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
                    new object[] { (Action<HttpContext, string[]>)Store, new[] { "plain string" }, new[] { "plain string" } },
                    new object[] { (Action<HttpContext, StringValues>)Store, new[] { "1", "2", "3" }, new StringValues(new[] { "1", "2", "3" }) },
                    new object[] { (Action<HttpContext, int[]>)Store, new[] { "-1", "2", "3" }, new[] { -1,2,3 } },
                    new object[] { (Action<HttpContext, uint[]>)Store, new[] { "1","42","32"}, new[] { 1U, 42U, 32U } },
                    new object[] { (Action<HttpContext, bool[]>)Store, new[] { "true", "false" }, new[] { true, false } },
                    new object[] { (Action<HttpContext, short[]>)Store, new[] { "-42" }, new[] { (short)-42 } },
                    new object[] { (Action<HttpContext, ushort[]>)Store, new[] { "42" }, new[] { (ushort)42 } },
                    new object[] { (Action<HttpContext, long[]>)Store, new[] { "-42" }, new[] { -42L } },
                    new object[] { (Action<HttpContext, ulong[]>)Store, new[] { "42" }, new[] { 42UL } },
                    new object[] { (Action<HttpContext, IntPtr[]>)Store, new[] { "-42" },new[] { new IntPtr(-42) } },
                    new object[] { (Action<HttpContext, char[]>)Store, new[] { "A" }, new[] { 'A' } },
                    new object[] { (Action<HttpContext, double[]>)Store, new[] { "0.5" },new[] { 0.5 } },
                    new object[] { (Action<HttpContext, float[]>)Store, new[] { "0.5" },new[] { 0.5f } },
                    new object[] { (Action<HttpContext, Half[]>)Store, new[] { "0.5" }, new[] { (Half)0.5f } },
                    new object[] { (Action<HttpContext, decimal[]>)Store, new[] { "0.5" },new[] { 0.5m } },
                    new object[] { (Action<HttpContext, Uri[]>)Store, new[] { "https://example.org" }, new[] { new Uri("https://example.org") } },
                    new object[] { (Action<HttpContext, DateTime[]>)Store, new[] { now.ToString("o") },new[] { now.ToUniversalTime() } },
                    new object[] { (Action<HttpContext, DateTimeOffset[]>)Store, new[] { "1970-01-01T00:00:00.0000000+00:00" },new[] { DateTimeOffset.UnixEpoch } },
                    new object[] { (Action<HttpContext, TimeSpan[]>)Store, new[] { "00:00:42" },new[] { TimeSpan.FromSeconds(42) } },
                    new object[] { (Action<HttpContext, Guid[]>)Store, new[] { "00000000-0000-0000-0000-000000000000" },new[] { Guid.Empty } },
                    new object[] { (Action<HttpContext, Version[]>)Store, new[] { "6.0.0.42" }, new[] { new Version("6.0.0.42") } },
                    new object[] { (Action<HttpContext, BigInteger[]>)Store, new[] { "-42" },new[]{ new BigInteger(-42) } },
                    new object[] { (Action<HttpContext, IPAddress[]>)Store, new[] { "127.0.0.1" }, new[] { IPAddress.Loopback } },
                    new object[] { (Action<HttpContext, IPEndPoint[]>)Store, new[] { "127.0.0.1:80" },new[] { new IPEndPoint(IPAddress.Loopback, 80) } },
                    new object[] { (Action<HttpContext, AddressFamily[]>)Store, new[] { "Unix" },new[] { AddressFamily.Unix } },
                    new object[] { (Action<HttpContext, ILOpCode[]>)Store, new[] { "Nop" }, new[] { ILOpCode.Nop } },
                    new object[] { (Action<HttpContext, AssemblyFlags[]>)Store, new[] { "PublicKey,Retargetable" },new[] { AssemblyFlags.PublicKey | AssemblyFlags.Retargetable } },
                    new object[] { (Action<HttpContext, int?[]>)Store, new[] { "42" }, new int?[] { 42 } },
                    new object[] { (Action<HttpContext, MyEnum[]>)Store, new[] { "ValueB" },new[] { MyEnum.ValueB } },
                    new object[] { (Action<HttpContext, MyTryParseRecord[]>)Store, new[] { "https://example.org" },new[] { new MyTryParseRecord(new Uri("https://example.org")) } },
                    new object?[] { (Action<HttpContext, int[]>)Store, new string[] {}, Array.Empty<int>() },
                    new object?[] { (Action<HttpContext, int?[]>)Store, new string?[] { "1", "2", null, "4" }, new int?[] { 1,2, null, 4 } },
                    new object?[] { (Action<HttpContext, int?[]>)Store, new string[] { "1", "2", "", "4" }, new int?[] { 1,2, null, 4 } },
                    new object[] { (Action<HttpContext, MyTryParseRecord?[]?>)Store, new[] { "" }, new MyTryParseRecord?[] { null } },
                };
        }
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
                    new object[] { (Action<HttpContext, Uri>)Store, "https://example.org", new Uri("https://example.org") },
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
    [MemberData(nameof(TryParsableArrayParameters))]
    public async Task RequestDelegateHandlesDoesNotHandleArraysFromQueryStringWhenBodyIsInferred(Delegate action, string[]? queryValues, object? expectedParameterValue)
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["tryParsable"] = queryValues
        });

        var factoryResult = RequestDelegateFactory.Create(action);

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        // Assert.NotEmpty(httpContext.Items);
        Assert.Null(httpContext.Items["tryParsable"]);

        // Ignore this parameter but we want to reuse the dataset
        GC.KeepAlive(expectedParameterValue);
    }

    [Fact]
    public async Task RequestDelegateHandlesOptionalArraysFromNullQueryString()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["tryParsable"] = (string?)null
        });

        static void StoreNullableIntArray(HttpContext httpContext, int?[]? tryParsable)
        {
            httpContext.Items["tryParsable"] = tryParsable;
        }

        var factoryResult = RequestDelegateFactory.Create(StoreNullableIntArray, new() { DisableInferBodyFromParameters = true });

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.NotEmpty(httpContext.Items);
        Assert.Null(httpContext.Items["tryParsable"]);
    }

    [Fact]
    public async Task RequestDelegateHandlesNullableStringValuesFromExplicitQueryStringSource()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["a"] = new(new[] { "1", "2", "3" })
        });

        httpContext.Request.Headers["Custom"] = new(new[] { "4", "5", "6" });

        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["form"] = new(new[] { "7", "8", "9" })
        });

        var factoryResult = RequestDelegateFactory.Create((HttpContext context,
            [FromHeader(Name = "Custom")] StringValues? headerValues,
            [FromQuery(Name = "a")] StringValues? queryValues,
            [FromForm(Name = "form")] StringValues? formValues) =>
        {
            context.Items["headers"] = headerValues;
            context.Items["query"] = queryValues;
            context.Items["form"] = formValues;
        });

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(new StringValues(new[] { "1", "2", "3" }), httpContext.Items["query"]);
        Assert.Equal(new StringValues(new[] { "4", "5", "6" }), httpContext.Items["headers"]);
        Assert.Equal(new StringValues(new[] { "7", "8", "9" }), httpContext.Items["form"]!);
    }

    [Fact]
    public async Task RequestDelegateHandlesNullableStringValuesFromExplicitQueryStringSourceForUnpresentedValues()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(null);

        var factoryResult = RequestDelegateFactory.Create((HttpContext context,
                [FromHeader(Name = "foo")] StringValues? headerValues,
                [FromQuery(Name = "bar")] StringValues? queryValues,
                [FromForm(Name = "form")] StringValues? formValues) =>
        {
            Assert.False(headerValues.HasValue);
            Assert.False(queryValues.HasValue);
            Assert.False(formValues.HasValue);
            context.Items["headers"] = headerValues;
            context.Items["query"] = queryValues;
            context.Items["form"] = formValues;
        });

        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Null(httpContext.Items["query"]);
        Assert.Null(httpContext.Items["headers"]);
        Assert.Null(httpContext.Items["form"]);
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
        Assert.Equal("notTryParsable must have a valid TryParse method to support converting from a string. No public static bool object.TryParse(string, out object) method found for notTryParsable.", ex.Message);
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
    public async Task RequestDelegateThrowsForTryParsableFailuresIfThrowOnBadRequestWithNonOptionalArrays()
    {
        var invoked = false;

        void StoreNullableIntArray(HttpContext httpContext, int?[] values)
        {
            invoked = true;
        }

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>()
        {
            ["values"] = (string?)null
        });

        var factoryResult = RequestDelegateFactory.Create(StoreNullableIntArray, new() { ThrowOnBadRequest = true, DisableInferBodyFromParameters = true });
        var requestDelegate = factoryResult.RequestDelegate;

        var badHttpRequestException = await Assert.ThrowsAsync<BadHttpRequestException>(() => requestDelegate(httpContext));

        Assert.False(invoked);

        // The httpContext should be untouched.
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.Response.HasStarted);

        // We don't log bad requests when we throw.
        Assert.Empty(TestSink.Writes);

        Assert.Equal(@"Failed to bind parameter ""Nullable<int>[] values"" from """".", badHttpRequestException.Message);
        Assert.Equal(400, badHttpRequestException.StatusCode);
    }

    private record ParametersListWithImplictFromBody(HttpContext HttpContext, TodoStruct Todo);

    private record ParametersListWithExplictFromBody(HttpContext HttpContext, [FromBody] Todo Todo);

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

            void TestImpliedFromBodyStruct_ParameterList([AsParameters] ParametersListWithImplictFromBody args)
            {
                args.HttpContext.Items.Add("body", args.Todo);
            }

            return new[]
            {
                    new[] { (Action<HttpContext, Todo>)TestImpliedFromBody },
                    new[] { (Action<HttpContext, ITodo>)TestImpliedFromBodyInterface },
                    new object[] { (Action<HttpContext, TodoStruct>)TestImpliedFromBodyStruct },
                    new object[] { (Action<ParametersListWithImplictFromBody>)TestImpliedFromBodyStruct_ParameterList },
                };
        }
    }

    public static object[][] ExplicitFromBodyActions
    {
        get
        {
            void TestExplicitFromBody_ParameterList([AsParameters] ParametersListWithExplictFromBody args)
            {
                args.HttpContext.Items.Add("body", args.Todo);
            }

            return new[]
            {
                new object[] { (Action<ParametersListWithExplictFromBody>)TestExplicitFromBody_ParameterList },
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

        var factoryResult = RequestDelegateFactory.Create(action, new RequestDelegateFactoryOptions() { ServiceProvider = mock.Object });
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var deserializedRequestBody = httpContext.Items["body"];
        Assert.NotNull(deserializedRequestBody);
        Assert.Equal(originalTodo.Name, ((ITodo)deserializedRequestBody!).Name);
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

    [Fact]
    public void RequestDelegateFactoryThrowsForByRefReturnTypes()
    {
        ReadOnlySpan<byte> Method1() => "hello world"u8;
        Span<byte> Method2() => "hello world"u8.ToArray();
        RefStruct Method3() => new("hello world"u8);

        var ex1 = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(Method1));
        var ex2 = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(Method2));
        var ex3 = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(Method3));

        Assert.Equal("Unsupported return type: System.ReadOnlySpan<byte>", ex1.Message);
        Assert.Equal("Unsupported return type: System.Span<byte>", ex2.Message);
        Assert.Equal($"Unsupported return type: {typeof(RefStruct).FullName}", ex3.Message);
    }

    ref struct RefStruct
    {
        public ReadOnlySpan<byte> Buffer { get; }

        public RefStruct(ReadOnlySpan<byte> buffer)
        {
            Buffer = buffer;
        }
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

    public static object[][] BadArgumentListActions
    {
        get
        {
            void TestParameterListRecord([AsParameters] BadArgumentListRecord req) { }
            void TestParameterListClass([AsParameters] BadArgumentListClass req) { }
            void TestParameterListClassWithMutipleConstructors([AsParameters] BadArgumentListClassMultipleCtors req) { }
            void TestParameterListAbstractClass([AsParameters] BadAbstractArgumentListClass req) { }
            void TestParameterListNoPulicConstructorClass([AsParameters] BadNoPublicConstructorArgumentListClass req) { }

            static string GetMultipleContructorsError(Type type)
                => $"Only a single public parameterized constructor is allowed for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'.";

            static string GetAbstractClassError(Type type)
                => $"The abstract type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}' is not supported.";

            static string GetNoContructorsError(Type type)
                => $"No public parameterless constructor found for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'.";

            static string GetInvalidConstructorError(Type type)
                => $"The public parameterized constructor must contain only parameters that match the declared public properties for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'.";

            return new object[][]
            {
                    new object[] { (Action<BadArgumentListRecord>)TestParameterListRecord, GetMultipleContructorsError(typeof(BadArgumentListRecord)) },
                    new object[] { (Action<BadArgumentListClass>)TestParameterListClass, GetInvalidConstructorError(typeof(BadArgumentListClass)) },
                    new object[] { (Action<BadArgumentListClassMultipleCtors>)TestParameterListClassWithMutipleConstructors, GetMultipleContructorsError(typeof(BadArgumentListClassMultipleCtors))  },
                    new object[] { (Action<BadAbstractArgumentListClass>)TestParameterListAbstractClass, GetAbstractClassError(typeof(BadAbstractArgumentListClass)) },
                    new object[] { (Action<BadNoPublicConstructorArgumentListClass>)TestParameterListNoPulicConstructorClass, GetNoContructorsError(typeof(BadNoPublicConstructorArgumentListClass)) },
            };
        }
    }

    [Theory]
    [MemberData(nameof(BadArgumentListActions))]
    public void BuildRequestDelegateThrowsInvalidOperationExceptionForInvalidParameterListConstructor(
        Delegate @delegate,
        string errorMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(@delegate));
        Assert.Equal(errorMessage, exception.Message);
    }

    [Fact]
    public void BuildRequestDelegateThrowsNotSupportedExceptionForNestedParametersList()
    {
        void TestNestedParameterListRecordOnType([AsParameters] NestedArgumentListRecord req) { }
        void TestNestedParameterListRecordOnArgument([AsParameters] ClassWithParametersConstructor req) { }

        Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(TestNestedParameterListRecordOnType));
        Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(TestNestedParameterListRecordOnArgument));
    }

    private record ParametersListWithImplictFromService(HttpContext HttpContext, IMyService MyService);

    private record ParametersListWithExplictFromService(HttpContext HttpContext, [FromService] MyService MyService);

    public static object[][] ExplicitFromServiceActions
    {
        get
        {
            void TestExplicitFromService(HttpContext httpContext, [FromService] MyService myService)
            {
                httpContext.Items.Add("service", myService);
            }

            void TestExplicitFromService_FromParameterList([AsParameters] ParametersListWithExplictFromService args)
            {
                args.HttpContext.Items.Add("service", args.MyService);
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
                    new object[] { (Action<ParametersListWithExplictFromService>)TestExplicitFromService_FromParameterList },
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

            void TestImpliedFromService_FromParameterList([AsParameters] ParametersListWithImplictFromService args)
            {
                args.HttpContext.Items.Add("service", args.MyService);
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
                    new object[] { (Action<ParametersListWithImplictFromService>)TestImpliedFromService_FromParameterList },
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

    [Fact]
    public void BuildRequestDelegateThrowsNotSupportedExceptionForByRefParameters()
    {
        void OutMethod(out string foo) { foo = ""; }
        void InMethod(in string foo) { }
        void RefMethod(ref string foo) { }

        var outParamException = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(OutMethod));
        var inParamException = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(InMethod));
        var refParamException = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(RefMethod));

        var typeName = typeof(string).MakeByRefType().Name;

        Assert.Equal($"The by reference parameter 'out {typeName} foo' is not supported.", outParamException.Message);
        Assert.Equal($"The by reference parameter 'in {typeName} foo' is not supported.", inParamException.Message);
        Assert.Equal($"The by reference parameter 'ref {typeName} foo' is not supported.", refParamException.Message);
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

    public static IEnumerable<object[]> PolymorphicResult
    {
        get
        {
            JsonTodoChild originalTodo = new()
            {
                Name = "Write even more tests!",
                Child = "With type hierarchies!",
            };

            JsonTodo TestAction() => originalTodo;

            Task<JsonTodo> TaskTestAction() => Task.FromResult<JsonTodo>(originalTodo);
            async Task<JsonTodo> TaskTestActionAwaited()
            {
                await Task.Yield();
                return originalTodo;
            }

            ValueTask<JsonTodo> ValueTaskTestAction() => ValueTask.FromResult<JsonTodo>(originalTodo);
            async ValueTask<JsonTodo> ValueTaskTestActionAwaited()
            {
                await Task.Yield();
                return originalTodo;
            }

            return new List<object[]>
                {
                    new object[] { (Func<JsonTodo>)TestAction },
                    new object[] { (Func<Task<JsonTodo>>)TaskTestAction},
                    new object[] { (Func<Task<JsonTodo>>)TaskTestActionAwaited},
                    new object[] { (Func<ValueTask<JsonTodo>>)ValueTaskTestAction},
                    new object[] { (Func<ValueTask<JsonTodo>>)ValueTaskTestActionAwaited},
                };
        }
    }

    // NOTE: This test needs to be retained here because it tests a specific capability to create a delegate
    //       from the request delegate factory without passing in an instance of a service provider which
    //       is not something we can get at with the shared compiler/runtime test harness.
    [Theory]
    [MemberData(nameof(PolymorphicResult))]
    public async Task RequestDelegateWritesMembersFromChildTypesToJsonResponseBody_WithJsonPolymorphicOptions(Delegate @delegate)
    {
        var httpContext = CreateHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(LoggerFactory)
            .AddSingleton(Options.Create(new JsonOptions()))
            .BuildServiceProvider();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var deserializedResponseBody = JsonSerializer.Deserialize<JsonTodoChild>(responseBodyStream.ToArray(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(deserializedResponseBody);
        Assert.Equal("Write even more tests!", deserializedResponseBody!.Name);
        Assert.Equal("With type hierarchies!", deserializedResponseBody!.Child);
    }

    // NOTE: This test needs to be retained here because it tests a specific capability to create a delegate
    //       from the request delegate factory without passing in an instance of a service provider which
    //       is not something we can get at with the shared compiler/runtime test harness. There is a variant
    //       of this test that has been added to the shared test harness to make sure that we do write the
    //       type discriminator.
    [Theory]
    [MemberData(nameof(PolymorphicResult))]
    public async Task RequestDelegateWritesJsonTypeDiscriminatorToJsonResponseBody_WithJsonPolymorphicOptions(Delegate @delegate)
    {
        var httpContext = CreateHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(LoggerFactory)
            .AddSingleton(Options.Create(new JsonOptions()))
            .BuildServiceProvider();

        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        var deserializedResponseBody = JsonNode.Parse(responseBodyStream.ToArray());

        Assert.NotNull(deserializedResponseBody);
        Assert.NotNull(deserializedResponseBody["$type"]);
        Assert.Equal(nameof(JsonTodoChild), deserializedResponseBody["$type"]!.GetValue<string>());
    }

    [JsonSerializable(typeof(Todo))]
    [JsonSerializable(typeof(TodoChild))]
    private partial class TestJsonContext : JsonSerializerContext
    { }

    [Fact]
    public void CreateDelegateThrows_WhenGetJsonTypeInfoFail()
    {
        var httpContext = CreateHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(LoggerFactory)
            .ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolver = TestJsonContext.Default)
            .BuildServiceProvider();

        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        TodoStruct TestAction() => new TodoStruct(42, "Bob", true);
        Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(TestAction, new() { ServiceProvider = httpContext.RequestServices }));
    }

    public static IEnumerable<object[]> CustomResults
    {
        get
        {
            var resultString = "Still not enough tests!";

            CustomResult TestAction() => new CustomResult(resultString);
            Task<CustomResult> TaskTestAction() => Task.FromResult(new CustomResult(resultString));
            ValueTask<CustomResult> ValueTaskTestAction() => ValueTask.FromResult(new CustomResult(resultString));
            FSharp.Control.FSharpAsync<CustomResult> FSharpAsyncTestAction() => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new CustomResult(resultString));

            static CustomResult StaticTestAction() => new CustomResult("Still not enough tests!");
            static Task<CustomResult> StaticTaskTestAction() => Task.FromResult(new CustomResult("Still not enough tests!"));
            static ValueTask<CustomResult> StaticValueTaskTestAction() => ValueTask.FromResult(new CustomResult("Still not enough tests!"));
            static FSharp.Control.FSharpAsync<CustomResult> StaticFSharpAsyncTestAction() => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new CustomResult("Still not enough tests!"));

            // Object return type where the object is IResult
            static object StaticResultAsObject() => new CustomResult("Still not enough tests!");

            // Task<object> return type
            static Task<object> StaticTaskOfIResultAsObject() => Task.FromResult<object>(new CustomResult("Still not enough tests!"));
            static ValueTask<object> StaticValueTaskOfIResultAsObject() => ValueTask.FromResult<object>(new CustomResult("Still not enough tests!"));
            static FSharp.Control.FSharpAsync<object> StaticFSharpAsyncOfIResultAsObject() => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return<object>(new CustomResult("Still not enough tests!"));

            StructResult TestStructAction() => new StructResult(resultString);
            Task<StructResult> TaskTestStructAction() => Task.FromResult(new StructResult(resultString));
            ValueTask<StructResult> ValueTaskTestStructAction() => ValueTask.FromResult(new StructResult(resultString));
            FSharp.Control.FSharpAsync<StructResult> FSharpAsyncTestStructAction() => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new StructResult(resultString));

            return new List<object[]>
                {
                    new object[] { (Func<CustomResult>)TestAction },
                    new object[] { (Func<Task<CustomResult>>)TaskTestAction},
                    new object[] { (Func<ValueTask<CustomResult>>)ValueTaskTestAction},
                    new object[] { (Func<FSharp.Control.FSharpAsync<CustomResult>>)FSharpAsyncTestAction },

                    new object[] { (Func<CustomResult>)StaticTestAction},
                    new object[] { (Func<Task<CustomResult>>)StaticTaskTestAction},
                    new object[] { (Func<ValueTask<CustomResult>>)StaticValueTaskTestAction},
                    new object[] { (Func<FSharp.Control.FSharpAsync<CustomResult>>)StaticFSharpAsyncTestAction },

                    new object[] { (Func<object>)StaticResultAsObject},

                    new object[] { (Func<Task<object>>)StaticTaskOfIResultAsObject},
                    new object[] { (Func<ValueTask<object>>)StaticValueTaskOfIResultAsObject},
                    new object[] { (Func<FSharp.Control.FSharpAsync<object>>)StaticFSharpAsyncOfIResultAsObject},

                    new object[] { (Func<StructResult>)TestStructAction },
                    new object[] { (Func<Task<StructResult>>)TaskTestStructAction },
                    new object[] { (Func<ValueTask<StructResult>>)ValueTaskTestStructAction },
                    new object[] { (Func<FSharp.Control.FSharpAsync<StructResult>>)FSharpAsyncTestStructAction },
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
            var expectedType = paramName == "age" ? "int age" : $"string name";
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

    public static IEnumerable<object?[]> UriDelegates
    {
        get
        {
            string uriParsing(Uri uri) => $"Uri: {uri.OriginalString}";

            return new List<object?[]>
                {
                    new object?[] { (Func<Uri, string>)uriParsing, "https://example.org", "Uri: https://example.org" },
                    new object?[] { (Func<Uri, string>)uriParsing, "https://example.org/path/to/file?name=value1&name=value2", "Uri: https://example.org/path/to/file?name=value1&name=value2" },
                    new object?[] { (Func<Uri, string>)uriParsing, "/path/to/file?name=value1&name=value2", "Uri: /path/to/file?name=value1&name=value2" },
                    new object?[] { (Func<Uri, string>)uriParsing, "?name=value1&name=value2", "Uri: ?name=value1&name=value2" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(UriDelegates))]
    public async Task RequestDelegateCanProcessUriValues(Delegate @delegate, string uri, string expectedResponse)
    {
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;

        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["uri"] = uri
        });

        var factoryResult = RequestDelegateFactory.Create(@delegate);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(expectedResponse, decodedResponseBody);
    }

    [Fact]
    public async Task RequestDelegateThrowsBadHttpRequestExceptionWhenReadingOversizeFormResultsIn413BadRequest()
    {
        var invoked = false;

        void TestAction(IFormFile file)
        {
            invoked = true;
        }

        var exception = new BadHttpRequestException("Request body too large. The max request body size is [who cares] bytes.", 413);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Content-Length"] = "1";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(exception);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
        Assert.Equal(@"Reading the request body failed with an IOException.", logMessage.Message);
        Assert.Same(exception, logMessage.Exception);
        Assert.Equal(413, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RequestDelegateThrowsBadHttpRequestExceptionWhenReadingOversizeJsonBodyResultsIn413BadRequest()
    {
        var invoked = false;

        void TestAction(Todo todo)
        {
            invoked = true;
        }

        var exception = new BadHttpRequestException("Request body too large. The max request body size is [who cares] bytes.", 413);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = "1000";
        httpContext.Request.Body = new ExceptionThrowingRequestBodyStream(exception);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.False(invoked);
        Assert.False(httpContext.RequestAborted.IsCancellationRequested);

        var logMessage = Assert.Single(TestSink.Writes);
        Assert.Equal(new EventId(1, "RequestBodyIOException"), logMessage.EventId);
        Assert.Equal(@"Reading the request body failed with an IOException.", logMessage.Message);
        Assert.Same(exception, logMessage.Exception);
        Assert.Equal(413, httpContext.Response.StatusCode);
    }

    [Fact]
    public void BuildRequestDelegateThrowsInvalidOperationExceptionBodyAndFormParameters()
    {
        void TestFormFileAndJson(IFormFile value1, Todo value2) { }
        void TestFormFilesAndJson(IFormFile value1, IFormFile value2, Todo value3) { }
        void TestFormFileCollectionAndJson(IFormFileCollection value1, Todo value2) { }
        void TestFormFileAndJsonWithAttribute(IFormFile value1, [FromBody] int value2) { }
        void TestFormCollectionAndJson(IFormCollection value1, Todo value2) { }
        void TestFormWithAttributeAndJson([FromForm] string value1, Todo value2) { }
        void TestJsonAndFormFile(Todo value1, IFormFile value2) { }
        void TestJsonAndFormFiles(Todo value1, IFormFile value2, IFormFile value3) { }
        void TestJsonAndFormFileCollection(Todo value1, IFormFileCollection value2) { }
        void TestJsonAndFormFileWithAttribute(Todo value1, [FromForm] IFormFile value2) { }
        void TestJsonAndFormCollection(Todo value1, IFormCollection value2) { }
        void TestJsonAndFormWithAttribute(Todo value1, [FromForm] string value2) { }

        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormFileAndJson));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormFilesAndJson));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormFileAndJsonWithAttribute));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormFileCollectionAndJson));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormCollectionAndJson));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestFormWithAttributeAndJson));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormFile));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormFiles));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormFileCollection));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormFileWithAttribute));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormCollection));
        Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestJsonAndFormWithAttribute));
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

    public static TheoryData<HttpContent, string> FormContent
    {
        get
        {
            var dataset = new TheoryData<HttpContent, string>();

            var multipartFormData = new MultipartFormDataContent("some-boundary");
            multipartFormData.Add(new StringContent("hello"), "message");
            multipartFormData.Add(new StringContent("foo"), "name");
            dataset.Add(multipartFormData, "multipart/form-data;boundary=some-boundary");

            var urlEncondedForm = new FormUrlEncodedContent(new Dictionary<string, string> { ["message"] = "hello", ["name"] = "foo" });
            dataset.Add(urlEncondedForm, "application/x-www-form-urlencoded");

            return dataset;
        }
    }

    [Fact]
    public void CreateThrowsNotSupportedExceptionIfIFormCollectionHasMetadataParameterName()
    {
        IFormCollection? formArgument = null;

        void TestAction([FromForm(Name = "foo")] IFormCollection formCollection)
        {
            formArgument = formCollection;
        }

        var nse = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(TestAction));
        Assert.Equal("Assigning a value to the IFromFormMetadata.Name property is not supported for parameters of type IFormCollection.", nse.Message);
    }

    public static object[][] NullableFromParameterListActions
    {
        get
        {
            void TestParameterListRecordStruct([AsParameters] ParameterListRecordStruct? args)
            { }

            void TestParameterListRecordClass([AsParameters] ParameterListRecordClass? args)
            { }

            void TestParameterListStruct([AsParameters] ParameterListStruct? args)
            { }

            void TestParameterListClass([AsParameters] ParameterListClass? args)
            { }

            return new[]
            {
                new object[] { (Action<ParameterListRecordStruct?>)TestParameterListRecordStruct },
                new object[] { (Action<ParameterListRecordClass?>)TestParameterListRecordClass },
                new object[] { (Action<ParameterListStruct?>)TestParameterListStruct },
                new object[] { (Action<ParameterListClass?>)TestParameterListClass },
            };
        }
    }

    [Theory]
    [MemberData(nameof(NullableFromParameterListActions))]
    public void RequestDelegateThrowsWhenNullableParameterList(Delegate action)
    {
        var parameter = action.Method.GetParameters()[0];
        var httpContext = CreateHttpContext();

        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(action));
        Assert.Contains($"The nullable type '{TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false)}' is not supported, mark the parameter as non-nullable.", exception.Message);
    }

    [Fact]
    public void RequestDelegateThrowsWhenParameterNameConflicts()
    {
        void TestAction(HttpContext context, [AsParameters] SampleParameterList args, [AsParameters] SampleParameterList args2)
        {
            context.Items.Add("foo", args.Foo);
        }
        var httpContext = CreateHttpContext();

        var exception = Assert.Throws<ArgumentException>(() => RequestDelegateFactory.Create(TestAction));
        Assert.Contains("An item with the same key has already been added. Key: Foo", exception.Message);
    }

    private class ParameterListWithReadOnlyProperties
    {
        public ParameterListWithReadOnlyProperties()
        {
            ReadOnlyValue = 1;
        }

        public int Value { get; set; }

        public int ConstantValue => 1;

        public int ReadOnlyValue { get; }
    }

    string GetString(string name)
    {
        return $"Hello, {name}!";
    }

    [Fact]
    public async Task RequestDelegateFactory_InvokesFilters_OnMethodInfoWithNullTargetFactory()
    {
        // Arrange
        var methodInfo = typeof(RequestDelegateFactoryTests).GetMethod(
            nameof(GetString),
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(string) });
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "TestName"
        });

        // Act
        var factoryResult = RequestDelegateFactory.Create(methodInfo!, null, new RequestDelegateFactoryOptions()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) => async (context) =>
                {
                    return await next(context);
                }
            }),
        });
        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal("Hello, TestName!", decodedResponseBody);
    }

    [Fact]
    public async Task RequestDelegateFactory_InvokesFilters_OnMethodInfoWithProvidedTargetFactory()
    {
        // Arrange
        var invoked = false;
        var methodInfo = typeof(RequestDelegateFactoryTests).GetMethod(
            nameof(GetString),
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(string) });
        var httpContext = CreateHttpContext();
        var responseBodyStream = new MemoryStream();
        httpContext.Response.Body = responseBodyStream;
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "TestName"
        });

        // Act
        Func<HttpContext, object> targetFactory = (context) =>
        {
            invoked = true;
            context.Items["invoked"] = true;
            return Activator.CreateInstance(methodInfo!.DeclaringType!)!;
        };
        var factoryResult = RequestDelegateFactory.Create(methodInfo!, targetFactory, new RequestDelegateFactoryOptions()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) => async (context) =>
                {
                    return await next(context);
                }
            }),
        });
        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.True(invoked);
        var invokedInContext = Assert.IsType<bool>(httpContext.Items["invoked"]);
        Assert.True(invokedInContext);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal("Hello, TestName!", decodedResponseBody);
    }

    public static object[][] FSharpAsyncOfTMethods
    {
        get
        {
            FSharp.Control.FSharpAsync<string> FSharpAsyncOfTMethod()
            {
                return FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return("foo");
            }

            FSharp.Control.FSharpAsync<string> FSharpAsyncOfTWithYieldMethod()
            {
                return FSharp.Control.FSharpAsync.AwaitTask(Yield());

                async Task<string> Yield()
                {
                    await Task.Yield();
                    return "foo";
                }
            }

            FSharp.Control.FSharpAsync<object> FSharpAsyncOfObjectWithYieldMethod()
            {
                return FSharp.Control.FSharpAsync.AwaitTask(Yield());

                async Task<object> Yield()
                {
                    await Task.Yield();
                    return "foo";
                }
            }

            return new object[][]
            {
                new object[] { (Func<FSharp.Control.FSharpAsync<string>>)FSharpAsyncOfTMethod },
                new object[] { (Func<FSharp.Control.FSharpAsync<string>>)FSharpAsyncOfTWithYieldMethod },
                new object[] { (Func<FSharp.Control.FSharpAsync<object>>)FSharpAsyncOfObjectWithYieldMethod }
            };
        }
    }

    [Theory]
    [MemberData(nameof(FSharpAsyncOfTMethods))]
    public async Task CanInvokeFilter_OnFSharpAsyncOfTReturningHandler(Delegate @delegate)
    {
        // Arrange
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext();
        httpContext.Response.Body = responseBodyStream;

        // Act
        var factoryResult = RequestDelegateFactory.Create(@delegate, new RequestDelegateFactoryOptions()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) => async (context) =>
                {
                    return await next(context);
                }
            }),
        });
        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal("foo", decodedResponseBody);
    }

    public static object[][] VoidReturningMethods
    {
        get
        {
            void VoidMethod() { }

            ValueTask ValueTaskMethod()
            {
                return ValueTask.CompletedTask;
            }

            ValueTask<FSharp.Core.Unit> ValueTaskOfUnitMethod()
            {
                return ValueTask.FromResult(default(FSharp.Core.Unit)!);
            }

            Task TaskMethod()
            {
                return Task.CompletedTask;
            }

            Task<FSharp.Core.Unit> TaskOfUnitMethod()
            {
                return Task.FromResult(default(FSharp.Core.Unit)!);
            }

            FSharp.Control.FSharpAsync<FSharp.Core.Unit> FSharpAsyncOfUnitMethod()
            {
                return FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(default(FSharp.Core.Unit)!);
            }

            async ValueTask ValueTaskWithYieldMethod()
            {
                await Task.Yield();
            }

            async Task TaskWithYieldMethod()
            {
                await Task.Yield();
            }

            FSharp.Control.FSharpAsync<FSharp.Core.Unit> FSharpAsyncOfUnitWithYieldMethod()
            {
                return FSharp.Control.FSharpAsync.AwaitTask(Yield());

                async Task<FSharp.Core.Unit> Yield()
                {
                    await Task.Yield();
                    return default!;
                }
            }

            return new object[][]
            {
                new object[] { (Action)VoidMethod },
                new object[] { (Func<ValueTask>)ValueTaskMethod },
                new object[] { (Func<ValueTask<FSharp.Core.Unit>>)ValueTaskOfUnitMethod },
                new object[] { (Func<Task>)TaskMethod },
                new object[] { (Func<Task<FSharp.Core.Unit>>)TaskOfUnitMethod },
                new object[] { (Func<FSharp.Control.FSharpAsync<FSharp.Core.Unit>>)FSharpAsyncOfUnitMethod },
                new object[] { (Func<ValueTask>)ValueTaskWithYieldMethod },
                new object[] { (Func<Task>)TaskWithYieldMethod},
                new object[] { (Func<FSharp.Control.FSharpAsync<FSharp.Core.Unit>>)FSharpAsyncOfUnitWithYieldMethod }
            };
        }
    }

    [Theory]
    [MemberData(nameof(VoidReturningMethods))]
    public async Task CanInvokeFilter_OnVoidReturningHandler(Delegate @delegate)
    {
        // Arrange
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext();
        httpContext.Response.Body = responseBodyStream;

        // Act
        var factoryResult = RequestDelegateFactory.Create(@delegate, new RequestDelegateFactoryOptions()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) => async (context) =>
                {
                    return await next(context);
                }
            }),
        });
        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(String.Empty, decodedResponseBody);
    }

    [Fact]
    public async Task CanInvokeFilter_OnTaskModifyingHttpContext()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        async Task HandlerWithTaskAwait(HttpContext c)
        {
            await tcs.Task;
            await Task.Yield();
            c.Response.StatusCode = 400;
        };
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext();
        httpContext.Response.Body = responseBodyStream;

        // Act
        var factoryResult = RequestDelegateFactory.Create(HandlerWithTaskAwait, new RequestDelegateFactoryOptions()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) => async (context) =>
                {
                    return await next(context);
                }
            }),
        });

        var requestDelegate = factoryResult.RequestDelegate;
        var request = requestDelegate(httpContext);
        tcs.TrySetResult();
        await request;

        Assert.Equal(400, httpContext.Response.StatusCode);
        var decodedResponseBody = Encoding.UTF8.GetString(responseBodyStream.ToArray());
        Assert.Equal(string.Empty, decodedResponseBody);
    }

    public static object[][] TasksOfTypesMethods
    {
        get
        {
            ValueTask<TodoStruct> ValueTaskOfStructMethod()
            {
                return ValueTask.FromResult(new TodoStruct { Name = "Test todo" });
            }

            async ValueTask<TodoStruct> ValueTaskOfStructWithYieldMethod()
            {
                await Task.Yield();
                return new TodoStruct { Name = "Test todo" };
            }

            Task<TodoStruct> TaskOfStructMethod()
            {
                return Task.FromResult(new TodoStruct { Name = "Test todo" });
            }

            async Task<TodoStruct> TaskOfStructWithYieldMethod()
            {
                await Task.Yield();
                return new TodoStruct { Name = "Test todo" };
            }

            FSharp.Control.FSharpAsync<TodoStruct> FSharpAsyncOfStructMethod()
            {
                return FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new TodoStruct { Name = "Test todo" });
            }

            FSharp.Control.FSharpAsync<TodoStruct> FSharpAsyncOfStructWithYieldMethod()
            {
                return FSharp.Control.FSharpAsync.AwaitTask(Yield());

                async Task<TodoStruct> Yield()
                {
                    await Task.Yield();
                    return new TodoStruct { Name = "Test todo" };
                }
            }

            return new object[][]
            {
                new object[] { (Func<ValueTask<TodoStruct>>)ValueTaskOfStructMethod },
                new object[] { (Func<ValueTask<TodoStruct>>)ValueTaskOfStructWithYieldMethod },
                new object[] { (Func<Task<TodoStruct>>)TaskOfStructMethod },
                new object[] { (Func<Task<TodoStruct>>)TaskOfStructWithYieldMethod },
                new object[] { (Func<FSharp.Control.FSharpAsync<TodoStruct>>)FSharpAsyncOfStructMethod },
                new object[] { (Func<FSharp.Control.FSharpAsync<TodoStruct>>)FSharpAsyncOfStructWithYieldMethod }
            };
        }
    }

    [Theory]
    [MemberData(nameof(TasksOfTypesMethods))]
    public async Task CanInvokeFilter_OnHandlerReturningTasksOfStruct(Delegate @delegate)
    {
        // Arrange
        var responseBodyStream = new MemoryStream();
        var httpContext = CreateHttpContext();
        httpContext.Response.Body = responseBodyStream;

        // Act
        var factoryResult = RequestDelegateFactory.Create(@delegate, new RequestDelegateFactoryOptions()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) => async (context) =>
                {
                    return await next(context);
                }
            }),
        });
        var requestDelegate = factoryResult.RequestDelegate;
        await requestDelegate(httpContext);

        Assert.Equal(200, httpContext.Response.StatusCode);
        var deserializedResponseBody = JsonSerializer.Deserialize<TodoStruct>(responseBodyStream.ToArray(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.Equal("Test todo", deserializedResponseBody.Name);
    }

    [Fact]
    public void Create_DoesNotAddDelegateMethodInfo_AsMetadata()
    {
        // Arrange
        var @delegate = () => { };

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        // RouteHandlerEndpointDataSource adds the MethodInfo as the first item in RouteHandlerOptions.EndointMetadata
        Assert.Empty(result.EndpointMetadata);
    }

    [Fact]
    public void Create_AddJsonResponseType_AsMetadata()
    {
        var @delegate = () => new object();
        var result = RequestDelegateFactory.Create(@delegate);

        var responseMetadata = Assert.IsAssignableFrom<IProducesResponseTypeMetadata>(Assert.Single(result.EndpointMetadata));

        Assert.Equal("application/json", Assert.Single(responseMetadata.ContentTypes));
        Assert.Equal(typeof(object), responseMetadata.Type);
    }

    [Fact]
    public void Create_AddPlaintextResponseType_AsMetadata()
    {
        var @delegate = () => "Hello";
        var result = RequestDelegateFactory.Create(@delegate);

        var responseMetadata = Assert.IsAssignableFrom<IProducesResponseTypeMetadata>(Assert.Single(result.EndpointMetadata));

        Assert.Equal("text/plain", Assert.Single(responseMetadata.ContentTypes));
        Assert.Equal(typeof(string), responseMetadata.Type);
    }

    [Fact]
    public void Create_DoesNotAddAnythingBefore_ThePassedInEndpointMetadata()
    {
        // Arrange
        var @delegate = (AddsCustomParameterMetadataBindable param1) => { };
        var customMetadata = new CustomEndpointMetadata();
        var options = new RequestDelegateFactoryOptions { EndpointBuilder = CreateEndpointBuilder(new List<object> { customMetadata }) };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        // RouteHandlerEndpointDataSource adds things like the MethodInfo, HttpMethodMetadata and attributes to RouteHandlerOptions.EndointMetadata,
        // but we just specified our CustomEndpointMetadata in this test.
        Assert.Collection(result.EndpointMetadata,
            m => Assert.Same(customMetadata, m),
            m => Assert.True(m is IParameterBindingMetadata { HasBindAsync : true }),
            m => Assert.True(m is ParameterNameMetadata { Name: "param1" }),
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Parameter }));
    }

    [Fact]
    public void Create_DoesNotAddDelegateAttributes_AsMetadata()
    {
        // Arrange
        var @delegate = [Attribute1, Attribute2] () => { };

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        // RouteHandlerEndpointDataSource adds the attributes to RouteHandlerOptions.EndointMetadata
        Assert.Empty(result.EndpointMetadata);
    }

    [Fact]
    public void Create_DiscoversMetadata_FromParametersImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var @delegate = (AddsCustomParameterMetadataBindable param1, AddsCustomParameterMetadata param2) => { };

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is ParameterNameMetadata { Name: "param1" });
        Assert.Contains(result.EndpointMetadata, m => m is ParameterNameMetadata { Name: "param2" });
    }

    [Fact]
    public void Create_DiscoversMetadata_FromParametersImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (AddsCustomParameterMetadata param1) => { };

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    }

    [Fact]
    public void Create_DiscoversEndpointMetadata_FromReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = () => new AddsCustomEndpointMetadataResult();

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public void Create_DiscoversEndpointMetadata_FromTaskWrappedReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = () => Task.FromResult(new AddsCustomEndpointMetadataResult());

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public void Create_DiscoversEndpointMetadata_FromValueTaskWrappedReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = () => ValueTask.FromResult(new AddsCustomEndpointMetadataResult());

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public void Create_DiscoversEndpointMetadata_FromFSharpAsyncWrappedReturnTypeImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = () => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new AddsCustomEndpointMetadataResult());

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.ReturnType });
    }

    [Fact]
    public void Create_CombinesDefaultMetadata_AndMetadataFromReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = () => new CountsDefaultEndpointMetadataResult();
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>
            {
                new CustomEndpointMetadata { Source = MetadataSource.Caller }
            }),
        };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        // Expecting '1' because only initial metadata will be in the metadata list when this metadata item is added
        Assert.Contains(result.EndpointMetadata, m => m is MetadataCountMetadata { Count: 1 });
    }

    [Fact]
    public void Create_CombinesDefaultMetadata_AndMetadataFromTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = () => Task.FromResult(new CountsDefaultEndpointMetadataResult());
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>
            {
                new CustomEndpointMetadata { Source = MetadataSource.Caller }
            }),
        };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        Assert.Contains(result.EndpointMetadata, m => m is ProducesResponseTypeMetadata { Type: { } type } && type == typeof(CountsDefaultEndpointMetadataResult));
        // Expecting the custom metadata and the implicit metadata associated with a Task-based return type to be inserted
        Assert.Contains(result.EndpointMetadata, m => m is MetadataCountMetadata { Count: 2 });
    }

    [Fact]
    public void Create_CombinesDefaultMetadata_AndMetadataFromValueTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = () => ValueTask.FromResult(new CountsDefaultEndpointMetadataResult());
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>
            {
                new CustomEndpointMetadata { Source = MetadataSource.Caller }
            }),
        };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        Assert.Contains(result.EndpointMetadata, m => m is ProducesResponseTypeMetadata { Type: { } type } && type == typeof(CountsDefaultEndpointMetadataResult));
        // Expecting the custom metadata nad hte implicit metadata associated with a Task-based return type to be inserted
        Assert.Contains(result.EndpointMetadata, m => m is MetadataCountMetadata { Count: 2 });
    }

    [Fact]
    public void Create_CombinesDefaultMetadata_AndMetadataFromFSharpAsyncWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = () => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new CountsDefaultEndpointMetadataResult());
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>
            {
                new CustomEndpointMetadata { Source = MetadataSource.Caller }
            }),
        };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        Assert.Contains(result.EndpointMetadata, m => m is IProducesResponseTypeMetadata { Type: { } type } && type == typeof(CountsDefaultEndpointMetadataResult));
        // Expecting '1' because only initial metadata will be in the metadata list when this metadata item is added
        Assert.Contains(result.EndpointMetadata, m => m is MetadataCountMetadata { Count: 2 });
    }

    [Fact]
    public void Create_CombinesDefaultMetadata_AndMetadataFromParameterTypesImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var @delegate = (AddsCustomParameterMetadata param1) => "Hello";
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>
            {
                new CustomEndpointMetadata { Source = MetadataSource.Caller }
            }),
        };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        Assert.Contains(result.EndpointMetadata, m => m is ParameterNameMetadata { Name: "param1" });
    }

    [Fact]
    public void Create_CombinesDefaultMetadata_AndMetadataFromParameterTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (AddsCustomParameterMetadata param1) => "Hello";
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>
            {
                new CustomEndpointMetadata { Source = MetadataSource.Caller }
            }),
        };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Caller });
        Assert.Contains(result.EndpointMetadata, m => m is CustomEndpointMetadata { Source: MetadataSource.Parameter });
    }

    [Fact]
    public void Create_CombinesAllMetadata_InCorrectOrder()
    {
        // Arrange
        var @delegate = [Attribute1, Attribute2] (AddsCustomParameterMetadata param1) => new CountsDefaultEndpointMetadataPoco();
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>
            {
                new CustomEndpointMetadata { Source = MetadataSource.Caller }
            }),
        };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.Collection(result.EndpointMetadata,
            // Initial metadata from RequestDelegateFactoryOptions.EndpointBuilder. If the caller want to override inferred metadata,
            // They need to call InferMetadata first, then add the overriding metadata, and then call Create with InferMetadata's result.
            // This is demonstrated in the following tests.
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Caller }),
            // Inferred AcceptsMetadata from RDF for complex type
            m => Assert.True(m is AcceptsMetadata am && am.RequestType == typeof(AddsCustomParameterMetadata)),
            // Inferred ParameterBinding metadata
            m => Assert.True(m is IParameterBindingMetadata { Name: "param1" }),
            // Inferred ProducesResopnseTypeMetadata from RDF for complex type
            m => Assert.Equal(typeof(CountsDefaultEndpointMetadataPoco), ((IProducesResponseTypeMetadata)m).Type),
            // Metadata provided by parameters implementing IEndpointParameterMetadataProvider
            m => Assert.True(m is ParameterNameMetadata { Name: "param1" }),
            // Metadata provided by parameters implementing IEndpointMetadataProvider
            m => Assert.True(m is CustomEndpointMetadata { Source: MetadataSource.Parameter }),
            // Metadata provided by return type implementing IEndpointMetadataProvider
            m => Assert.True(m is MetadataCountMetadata { Count: 6 }));
    }

    [Fact]
    public void Create_FlowsRoutePattern_ToMetadataProvider()
    {
        // Arrange
        var @delegate = (AddsRoutePatternMetadata param1) => { };
        var builder = new RouteEndpointBuilder(requestDelegate: null, RoutePatternFactory.Parse("/test/pattern"), order: 0);
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = builder,
        };

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is RoutePatternMetadata { RoutePattern: "/test/pattern" });
    }

    [Fact]
    public void Create_DoesNotInferMetadata_GivenManuallyConstructedMetadataResult()
    {
        var invokeCount = 0;

        // Arrange
        var @delegate = [Attribute1, Attribute2] (AddsCustomParameterMetadata param1) =>
        {
            invokeCount++;
            return new CountsDefaultEndpointMetadataResult();
        };

        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(),
        };
        var metadataResult = new RequestDelegateMetadataResult { EndpointMetadata = new List<object>() };
        var httpContext = CreateHttpContext();

        // An empty object should deserialize to AddsCustomParameterMetadata since it has no required properties.
        var requestBodyBytes = JsonSerializer.SerializeToUtf8Bytes(new object());
        var stream = new MemoryStream(requestBodyBytes);
        httpContext.Request.Body = stream;

        httpContext.Request.Headers["Content-Type"] = "application/json";
        httpContext.Request.Headers["Content-Length"] = stream.Length.ToString(CultureInfo.InvariantCulture);
        httpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(true));

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options, metadataResult);

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is IParameterBindingMetadata { Name: "param1" });
        Assert.Same(options.EndpointBuilder.Metadata, result.EndpointMetadata);

        // Make extra sure things are running as expected, as this non-InferMetadata path is no longer exercised by RouteEndpointDataSource,
        // and most of the other unit tests don't pass in a metadataResult without a cached factory context.
        Assert.True(result.RequestDelegate(httpContext).IsCompletedSuccessfully);
        Assert.Equal(1, invokeCount);
    }

    [Fact]
    public void InferMetadata_PopulatesAcceptsMetadata_WhenReadFromForm()
    {
        // Arrange
        var @delegate = void (IFormCollection formCollection) => { };
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(),
        };

        // Act
        var metadataResult = RequestDelegateFactory.InferMetadata(@delegate.Method, options);

        // Assert
        var allAcceptsMetadata = metadataResult.EndpointMetadata.OfType<IAcceptsMetadata>();
        var acceptsMetadata = Assert.Single(allAcceptsMetadata);

        Assert.NotNull(acceptsMetadata);
        Assert.Equal(new[] { "multipart/form-data", "application/x-www-form-urlencoded" }, acceptsMetadata.ContentTypes);
    }

    [Fact]
    public void InferMetadata_PopulatesCachedContext()
    {
        // Arrange
        var @delegate = void () => { };
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(),
        };

        // Act
        var metadataResult = RequestDelegateFactory.InferMetadata(@delegate.Method, options);

        // Assert
        Assert.NotNull(metadataResult.CachedFactoryContext);
    }

    [Fact]
    public void Create_AllowsRemovalOfDefaultMetadata_ByReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (Todo todo) => new RemovesAcceptsMetadataResult();

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.DoesNotContain(result.EndpointMetadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void Create_AllowsRemovalOfDefaultMetadata_ByTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (Todo todo) => Task.FromResult(new RemovesAcceptsMetadataResult());

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.DoesNotContain(result.EndpointMetadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void Create_AllowsRemovalOfDefaultMetadata_ByValueTaskWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (Todo todo) => ValueTask.FromResult(new RemovesAcceptsMetadataResult());

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.DoesNotContain(result.EndpointMetadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void Create_AllowsRemovalOfDefaultMetadata_ByFSharpAsyncWrappedReturnTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (Todo todo) => FSharp.Core.ExtraTopLevelOperators.DefaultAsyncBuilder.Return(new RemovesAcceptsMetadataResult());

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.DoesNotContain(result.EndpointMetadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void Create_AllowsRemovalOfDefaultMetadata_ByParameterTypesImplementingIEndpointParameterMetadataProvider()
    {
        // Arrange
        var @delegate = (RemovesAcceptsParameterMetadata param1) => "Hello";

        // Act
        var result = RequestDelegateFactory.Create(@delegate);

        // Assert
        Assert.DoesNotContain(result.EndpointMetadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void Create_AllowsRemovalOfDefaultMetadata_ByParameterTypesImplementingIEndpointMetadataProvider()
    {
        // Arrange
        var @delegate = (RemovesAcceptsParameterMetadata param1) => "Hello";
        var options = new RequestDelegateFactoryOptions();

        // Act
        var result = RequestDelegateFactory.Create(@delegate, options);

        // Assert
        Assert.DoesNotContain(result.EndpointMetadata, m => m is IAcceptsMetadata);
    }

    [Fact]
    public void Create_SetsApplicationServices_OnEndpointMetadataContext()
    {
        // Arrange
        var @delegate = (Todo todo) => new AccessesServicesMetadataResult();
        var metadataService = new MetadataService();
        var serviceProvider = new ServiceCollection().AddSingleton(metadataService).BuildServiceProvider();

        // Act
        var result = RequestDelegateFactory.Create(@delegate, new() { ServiceProvider = serviceProvider });

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is MetadataService);
    }

    [Fact]
    public void Create_SetsApplicationServices_OnEndpointParameterMetadataContext()
    {
        // Arrange
        var @delegate = (AccessesServicesMetadataBinder parameter1) => "Test";
        var metadataService = new MetadataService();
        var serviceProvider = new ServiceCollection().AddSingleton(metadataService).BuildServiceProvider();

        // Act
        var result = RequestDelegateFactory.Create(@delegate, new() { ServiceProvider = serviceProvider });

        // Assert
        Assert.Contains(result.EndpointMetadata, m => m is MetadataService);
    }

    [Fact]
    public void Create_ReturnsSameRequestDelegatePassedIn_IfNotModifiedByFilters()
    {
        RequestDelegate initialRequestDelegate = static (context) => Task.CompletedTask;
        var invokeCount = 0;

        RequestDelegateFactoryOptions options = new()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) =>
                {
                    invokeCount++;
                    return next;
                },
                (routeHandlerContext, next) =>
                {
                    invokeCount++;
                    return next;
                },
            }),
        };

        var result = RequestDelegateFactory.Create(initialRequestDelegate, options);
        Assert.Same(initialRequestDelegate, result.RequestDelegate);
        Assert.Equal(2, invokeCount);
    }

    [Fact]
    public void Create_Populates_EndpointBuilderWithRequestDelegateAndMetadata()
    {
        RequestDelegate requestDelegate = static context => Task.CompletedTask;

        RequestDelegateFactoryOptions options = new()
        {
            EndpointBuilder = new RouteEndpointBuilder(null, RoutePatternFactory.Parse("/"), order: 0),
        };

        var result = RequestDelegateFactory.Create(requestDelegate, options);

        Assert.Same(options.EndpointBuilder.RequestDelegate, result.RequestDelegate);
        Assert.Same(options.EndpointBuilder.Metadata, result.EndpointMetadata);
    }

    [Fact]
    public async Task RDF_CanAssertOnEmptyResult()
    {
        var @delegate = (string name, HttpContext context) => context.Items.Add("param", name);

        var result = RequestDelegateFactory.Create(@delegate, new RequestDelegateFactoryOptions()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(new List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>()
            {
                (routeHandlerContext, next) => async (context) =>
                {
                    var response = await next(context);
                    Assert.IsType<EmptyHttpResult>(response);
                    Assert.Same(Results.Empty, response);
                    return response;
                }
            }),
        });

        var httpContext = CreateHttpContext();
        httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "Tester"
        });

        await result.RequestDelegate(httpContext);
    }

    private class ParameterListRequiredNullableStringFromDifferentSources
    {
        public HttpContext? HttpContext { get; set; }

        [FromRoute]
        public required StringValues? RequiredRouteParam { get; set; }

        [FromQuery]
        public required StringValues? RequiredQueryParam { get; set; }

        [FromHeader]
        public required StringValues? RequiredHeaderParam { get; set; }
    }

    [Fact]
    public async Task RequestDelegateFactory_AsParameters_SupportsNullableRequiredMember()
    {
        // Arrange
        static void TestAction([AsParameters] ParameterListRequiredNullableStringFromDifferentSources args)
        {
            args.HttpContext!.Items.Add("RequiredRouteParam", args.RequiredRouteParam);
            args.HttpContext!.Items.Add("RequiredQueryParam", args.RequiredQueryParam);
            args.HttpContext!.Items.Add("RequiredHeaderParam", args.RequiredHeaderParam);
        }

        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        // Act
        await requestDelegate(httpContext);

        // Assert that when properties are required but nullable
        // we evaluate them as optional because required members
        // must be initialized but they can be initialized to null
        // when an NRT is required.
        Assert.Equal(200, httpContext.Response.StatusCode);

        Assert.Null(httpContext.Items["RequiredRouteParam"]);
        Assert.Null(httpContext.Items["RequiredQueryParam"]);
        Assert.Null(httpContext.Items["RequiredHeaderParam"]);
    }

#nullable disable
    private class ParameterListMixedRequiredStringsFromDifferentSources
    {
        public HttpContext HttpContext { get; set; }

        [FromRoute]
        public required string RequiredRouteParam { get; set; }

        [FromRoute]
        public string OptionalRouteParam { get; set; }

        [FromQuery]
        public required string RequiredQueryParam { get; set; }

        [FromQuery]
        public string OptionalQueryParam { get; set; }

        [FromHeader]
        public required string RequiredHeaderParam { get; set; }

        [FromHeader]
        public string OptionalHeaderParam { get; set; }
    }

    [Fact]
    public async Task RequestDelegateFactory_AsParameters_SupportsRequiredMember_NullabilityDisabled()
    {
        // Arange
        static void TestAction([AsParameters] ParameterListMixedRequiredStringsFromDifferentSources args) { }

        var httpContext = CreateHttpContext();

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        // Act
        await requestDelegate(httpContext);

        // Assert that we only execute required parameter
        // checks for members that have the required modifier
        Assert.Equal(400, httpContext.Response.StatusCode);

        var logs = TestSink.Writes.ToArray();
        Assert.Equal(3, logs.Length);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[0].EventId);
        Assert.Equal(LogLevel.Debug, logs[0].LogLevel);
        Assert.Equal(@"Required parameter ""string RequiredRouteParam"" was not provided from route.", logs[0].Message);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[1].EventId);
        Assert.Equal(LogLevel.Debug, logs[1].LogLevel);
        Assert.Equal(@"Required parameter ""string RequiredQueryParam"" was not provided from query string.", logs[1].Message);

        Assert.Equal(new EventId(4, "RequiredParameterNotProvided"), logs[2].EventId);
        Assert.Equal(LogLevel.Debug, logs[2].LogLevel);
        Assert.Equal(@"Required parameter ""string RequiredHeaderParam"" was not provided from header.", logs[2].Message);
    }
#nullable enable

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void RequestDelegateFactory_WhenJsonIsReflectionEnabledByDefaultFalse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var @delegate = (string task) => new Todo();

            // IsReflectionEnabledByDefault defaults to `false` when `PublishTrimmed=true`. For these scenarios, we
            // expect users to configure JSON source generation as instructed in the `NotSupportedException` message.
            var exception = Assert.Throws<NotSupportedException>(() => RequestDelegateFactory.Create(@delegate));
            Assert.Contains("Microsoft.AspNetCore.Routing.Internal.RequestDelegateFactoryTests+Todo", exception.Message);
            Assert.Contains("JsonSerializableAttribute", exception.Message);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void RequestDelegateFactory_WhenJsonIsReflectionEnabledByDefaultTrue()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var @delegate = (string task) => new Todo();

            // Assert
            var exception = Record.Exception(() => RequestDelegateFactory.Create(@delegate));
            Assert.Null(exception);
        }, options);
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

    private EndpointBuilder CreateEndpointBuilder(IEnumerable<object>? metadata = null)
    {
        var builder = new RouteEndpointBuilder(null, RoutePatternFactory.Parse("/"), 0);
        if (metadata is not null)
        {
            ((List<object>)builder.Metadata).AddRange(metadata);
        }
        return builder;
    }

    private EndpointBuilder CreateEndpointBuilderFromFilterFactories(IEnumerable<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>> filterFactories)
    {
        var builder = new RouteEndpointBuilder(null, RoutePatternFactory.Parse("/"), 0);
        ((List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>>)builder.FilterFactories).AddRange(filterFactories);
        return builder;
    }

    private record MetadataService;

    private class AccessesServicesMetadataResult : IResult, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            if (builder.ApplicationServices.GetRequiredService<MetadataService>() is { } metadataService)
            {
                builder.Metadata.Add(metadataService);
            }
        }

        public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
    }

    private class AccessesServicesMetadataBinder : IEndpointMetadataProvider
    {
        public static ValueTask<AccessesServicesMetadataBinder> BindAsync(HttpContext context, ParameterInfo parameter) =>
            new(new AccessesServicesMetadataBinder());

        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            if (builder.ApplicationServices.GetRequiredService<MetadataService>() is { } metadataService)
            {
                builder.Metadata.Add(metadataService);
            }
        }
    }

    private class Attribute1 : Attribute
    {
    }

    private class Attribute2 : Attribute
    {
    }

    private class AddsCustomEndpointMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.ReturnType });
        }

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }

    private class AddsNoEndpointMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {

        }

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }

    private class CountsDefaultEndpointMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            var currentMetadataCount = builder.Metadata.Count;
            builder.Metadata.Add(new MetadataCountMetadata { Count = currentMetadataCount });
        }

        public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
    }

    private class CountsDefaultEndpointMetadataPoco : IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            var currentMetadataCount = builder.Metadata.Count;
            builder.Metadata.Add(new MetadataCountMetadata { Count = currentMetadataCount });
        }
    }

    private class RemovesAcceptsParameterMetadata : IEndpointParameterMetadataProvider
    {
        public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
        {
            if (builder.Metadata is not null)
            {
                for (int i = builder.Metadata.Count - 1; i >= 0; i--)
                {
                    var metadata = builder.Metadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        builder.Metadata.RemoveAt(i);
                    }
                }
            }
        }
    }

    private class RemovesAcceptsMetadata : IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            if (builder.Metadata is not null)
            {
                for (int i = builder.Metadata.Count - 1; i >= 0; i--)
                {
                    var metadata = builder.Metadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        builder.Metadata.RemoveAt(i);
                    }
                }
            }
        }
    }

    private class RemovesAcceptsMetadataResult : IEndpointMetadataProvider, IResult
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            if (builder.Metadata is not null)
            {
                for (int i = builder.Metadata.Count - 1; i >= 0; i--)
                {
                    var metadata = builder.Metadata[i];
                    if (metadata is IAcceptsMetadata)
                    {
                        builder.Metadata.RemoveAt(i);
                    }
                }
            }
        }

        public Task ExecuteAsync(HttpContext httpContext) => throw new NotImplementedException();
    }

    private class AddsCustomParameterMetadataAsProperty : IEndpointParameterMetadataProvider, IEndpointMetadataProvider
    {
        public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
        {
            builder.Metadata.Add(new ParameterNameMetadata { Name = parameter.Name });
        }

        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Property });
        }
    }

    // TODO: Binding breaks if we explicitly implement IParsable. :(
    // We could special-case IParsable because we have a reference to it. The check for `!method.IsAbstract` in GetStaticMethodFromHierarchy
    // stops us from finding it now. But even if we did find it, we haven't implemented the correct code gen to call it for unreferenced interfaces.
    // We might have to use Type.GetInterfaceMap. See previous discussion: https://github.com/dotnet/aspnetcore/pull/40926#discussion_r837781209
    //
    // System.InvalidOperationException : TryParse method found on AddsCustomParameterMetadata with incorrect format. Must be a static method with format
    // bool TryParse(string, IFormatProvider, out AddsCustomParameterMetadata)
    // bool TryParse(string, out AddsCustomParameterMetadata)
    // but found
    // static Boolean TryParse(System.String, System.IFormatProvider, AddsCustomParameterMetadata ByRef)
    private class AddsCustomParameterMetadata : IEndpointParameterMetadataProvider, IEndpointMetadataProvider//, IParsable<AddsCustomParameterMetadata>
    {
        public AddsCustomParameterMetadataAsProperty? Data { get; set; }

        public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
        {
            builder.Metadata.Add(new ParameterNameMetadata { Name = parameter.Name });
        }

        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Parameter });
        }

        //static bool IParsable<AddsCustomParameterMetadata>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out AddsCustomParameterMetadata result)
        //{
        //    result = new();
        //    return true;
        //}

        //static AddsCustomParameterMetadata IParsable<AddsCustomParameterMetadata>.Parse(string s, IFormatProvider? provider) => throw new NotSupportedException();
    }

    private class AddsCustomParameterMetadataBindable : IEndpointParameterMetadataProvider, IEndpointMetadataProvider
    {
        public static ValueTask<AddsCustomParameterMetadataBindable> BindAsync(HttpContext context, ParameterInfo parameter) => default;

        public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
        {
            builder.Metadata.Add(new ParameterNameMetadata { Name = parameter.Name });
        }

        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new CustomEndpointMetadata { Source = MetadataSource.Parameter });
        }
    }

    private class AddsRoutePatternMetadata : IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            if (builder is not RouteEndpointBuilder reb)
            {
                return;
            }

            builder.Metadata.Add(new RoutePatternMetadata { RoutePattern = reb.RoutePattern?.RawText ?? string.Empty });
        }
    }

    private class MetadataCountMetadata
    {
        public int Count { get; init; }
    }

    private class ParameterNameMetadata
    {
        public string? Name { get; init; }
    }

    private class CustomEndpointMetadata
    {
        public string? Data { get; init; }

        public MetadataSource Source { get; init; }
    }

    private class RoutePatternMetadata
    {
        public string RoutePattern { get; init; } = String.Empty;
    }

    private enum MetadataSource
    {
        Caller,
        Parameter,
        ReturnType,
        Property
    }

    private class Todo : ITodo
    {
        public int Id { get; set; }
        public string? Name { get; set; } = "Todo";
        public bool IsComplete { get; set; }
    }

    private class JsonTodoChild : JsonTodo
    {
        public string? Child { get; set; }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(JsonTodoChild), nameof(JsonTodoChild))]
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
