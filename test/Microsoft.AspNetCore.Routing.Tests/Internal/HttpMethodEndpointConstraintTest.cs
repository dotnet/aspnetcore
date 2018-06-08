// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Internal
{
    public class HttpMethodEndpointConstraintTest
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
        public void HttpMethodEndpointConstraint_IgnoresPreflightRequests(IEnumerable<string> httpMethods, string accessControlMethod)
        {
            // Arrange
            var constraint = new HttpMethodEndpointConstraint(httpMethods);
            var context = CreateEndpointConstraintContext(constraint);
            context.HttpContext = CreateHttpContext("oPtIoNs", accessControlMethod);

            // Act
            var result = constraint.Accept(context);

            // Assert
            Assert.False(result, "Request should have been rejected.");
        }

        [Theory]
        [MemberData(nameof(AcceptCaseInsensitiveData))]
        public void HttpMethodEndpointConstraint_Accept_CaseInsensitive(IEnumerable<string> httpMethods, string expectedMethod)
        {
            // Arrange
            var constraint = new HttpMethodEndpointConstraint(httpMethods);
            var context = CreateEndpointConstraintContext(constraint);
            context.HttpContext = CreateHttpContext(expectedMethod);

            // Act
            var result = constraint.Accept(context);

            // Assert
            Assert.True(result, "Request should have been accepted.");
        }

        private static EndpointConstraintContext CreateEndpointConstraintContext(HttpMethodEndpointConstraint constraint)
        {
            var context = new EndpointConstraintContext();

            var endpointSelectorCandidate = new EndpointSelectorCandidate(
                new TestEndpoint(EndpointMetadataCollection.Empty, string.Empty, address: null),
                new List<IEndpointConstraint> { constraint });

            context.Candidates = new List<EndpointSelectorCandidate> { endpointSelectorCandidate };
            context.CurrentCandidate = context.Candidates[0];

            return context;
        }

        private static HttpContext CreateHttpContext(string requestedMethod, string accessControlMethod = null)
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Method = requestedMethod;

            if (accessControlMethod != null)
            {
                httpContext.Request.Headers.Add("Origin", StringValues.Empty);
                httpContext.Request.Headers.Add("Access-Control-Request-Method", accessControlMethod);
            }

            return httpContext;
        }
    }
}
