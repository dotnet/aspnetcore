// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints;

public class HttpMethodActionConstraintTest
{
    public static TheoryData AcceptCaseInsensitiveData =
        new TheoryData<IEnumerable<string>, string>
        {
                { new string[] { "get", "Get", "GET", "GEt"}, "gEt" },
                { new string[] { "POST", "PoSt", "GEt"}, "GET" },
                { new string[] { "get" }, "get" },
                { new string[] { "post" }, "POST" },
                { new string[] { "gEt" }, "get" },
                { new string[] { "get", "PoST" }, "pOSt" }
        };

    [Theory]
    [MemberData(nameof(AcceptCaseInsensitiveData))]
    public void HttpMethodActionConstraint_IgnoresPreflightRequests(IEnumerable<string> httpMethods, string accessControlMethod)
    {
        // Arrange
        var constraint = new HttpMethodActionConstraint(httpMethods);
        var context = CreateActionConstraintContext(constraint);
        context.RouteContext = CreateRouteContext("oPtIoNs", accessControlMethod);

        // Act
        var result = constraint.Accept(context);

        // Assert
        Assert.False(result, "Request should have been rejected.");
    }

    [Theory]
    [MemberData(nameof(AcceptCaseInsensitiveData))]
    public void HttpMethodActionConstraint_Accept_CaseInsensitive(IEnumerable<string> httpMethods, string expectedMethod)
    {
        // Arrange
        var constraint = new HttpMethodActionConstraint(httpMethods);
        var context = CreateActionConstraintContext(constraint);
        context.RouteContext = CreateRouteContext(expectedMethod);

        // Act
        var result = constraint.Accept(context);

        // Assert
        Assert.True(result, "Request should have been accepted.");
    }

    private static ActionConstraintContext CreateActionConstraintContext(HttpMethodActionConstraint constraint)
    {
        var context = new ActionConstraintContext();

        var actionSelectorCandidate = new ActionSelectorCandidate(new ActionDescriptor(), new List<IActionConstraint> { constraint });

        context.Candidates = new List<ActionSelectorCandidate> { actionSelectorCandidate };
        context.CurrentCandidate = context.Candidates[0];

        return context;
    }

    private static RouteContext CreateRouteContext(string requestedMethod, string accessControlMethod = null)
    {
        var httpContext = new DefaultHttpContext();

        httpContext.Request.Method = requestedMethod;

        if (accessControlMethod != null)
        {
            httpContext.Request.Headers.Add("Origin", StringValues.Empty);
            httpContext.Request.Headers.Add("Access-Control-Request-Method", accessControlMethod);
        }

        var routeContext = new RouteContext(httpContext);
        routeContext.RouteData = new RouteData();

        return routeContext;
    }
}
