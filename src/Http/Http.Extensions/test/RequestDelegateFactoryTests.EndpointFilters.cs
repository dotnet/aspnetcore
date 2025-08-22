// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Routing.Internal;

public partial class RequestDelegateFactoryTests : LoggedTest
{
    public static object[][] ValueTypeReturningDelegates =>
    [
        [(Func<HttpContext, int>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, char>)((HttpContext httpContext) => 'b')],
        [(Func<HttpContext, bool>)((HttpContext httpContext) => true)],
        [(Func<HttpContext, float>)((HttpContext httpContext) => 4.2f)],
        [(Func<HttpContext, double>)((HttpContext httpContext) => 4.2)],
        [(Func<HttpContext, decimal>)((HttpContext httpContext) => 4.2m)],
        [(Func<HttpContext, long>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, short>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, byte>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, uint>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, ulong>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, ushort>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, sbyte>)((HttpContext httpContext) => 42)]
    ];

    [Theory]
    [MemberData(nameof(ValueTypeReturningDelegates))]
    public void Create_WithEndpointFilterOnBuiltInValueTypeReturningDelegate_Works(Delegate @delegate)
    {
        var invokeCount = 0;

        RequestDelegateFactoryOptions options = new()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(
            [
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
            ]),
        };

        var result = RequestDelegateFactory.Create(@delegate, options);
        Assert.Equal(2, invokeCount);
    }

    public static object[][] NullableValueTypeReturningDelegates =>
    [
        [(Func<HttpContext, int?>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, char?>)((HttpContext httpContext) => 'b')],
        [(Func<HttpContext, bool?>)((HttpContext httpContext) => true)],
        [(Func<HttpContext, float?>)((HttpContext httpContext) => 4.2f)],
        [(Func<HttpContext, double?>)((HttpContext httpContext) => 4.2)],
        [(Func<HttpContext, decimal?>)((HttpContext httpContext) => 4.2m)],
        [(Func<HttpContext, long?>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, short?>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, byte?>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, uint?>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, ulong?>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, ushort?>)((HttpContext httpContext) => 42)],
        [(Func<HttpContext, sbyte?>)((HttpContext httpContext) => 42)]
    ];

    [Theory]
    [MemberData(nameof(NullableValueTypeReturningDelegates))]
    public void Create_WithEndpointFilterOnNullableBuiltInValueTypeReturningDelegate_Works(Delegate @delegate)
    {
        var invokeCount = 0;

        RequestDelegateFactoryOptions options = new()
        {
            EndpointBuilder = CreateEndpointBuilderFromFilterFactories(
            [
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
            ]),
        };

        var result = RequestDelegateFactory.Create(@delegate, options);
        Assert.Equal(2, invokeCount);
    }
}
