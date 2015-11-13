// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ActionConstraints;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class HttpMethodConstraintTest
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
        public void HttpMethodConstraint_Accept_Preflight_CaseInsensitive(IEnumerable<string> httpMethods, string accessControlMethod)
        {
            // Arrange
            var constraint = new HttpMethodConstraint(httpMethods);
            var context = CreateActionConstraintContext(constraint);
            context.RouteContext = CreateRouteContext("oPtIoNs", accessControlMethod);

            // Act
            var result = constraint.Accept(context);

            // Assert
            Assert.True(result, "Request should have been accepted.");
        }

        [Theory]
        [MemberData(nameof(AcceptCaseInsensitiveData))]
        public void HttpMethodConstraint_Accept_CaseInsensitive(IEnumerable<string> httpMethods, string expectedMethod)
        {
            // Arrange
            var constraint = new HttpMethodConstraint(httpMethods);
            var context = CreateActionConstraintContext(constraint);
            context.RouteContext = CreateRouteContext(expectedMethod);

            // Act
            var result = constraint.Accept(context);

            // Assert
            Assert.True(result, "Request should have been accepted.");
        }

        private static ActionConstraintContext CreateActionConstraintContext(HttpMethodConstraint constraint)
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
}
