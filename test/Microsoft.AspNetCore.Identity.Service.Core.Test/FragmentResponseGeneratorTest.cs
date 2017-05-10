// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class FragmentResponseGeneratorTest
    {
        [Fact]
        public void GenerateResponse_EncodesParameters_OnTheFragment()
        {
            // Arrange
            var expectedLocation = "http://www.example.com/callback#state=%23%3F%26%3D&code=serializedcode";
            var httpContext = new DefaultHttpContext();
            var generator = new FragmentResponseGenerator(UrlEncoder.Default);
            var parameters = new Dictionary<string, string[]>
            {
                ["state"] = new[] { "#?&=" },
                ["code"] = new[] { "serializedcode" }
            };

            var response = new OpenIdConnectMessage(parameters);
            response.RedirectUri = "http://www.example.com/callback";
            // Act
            generator.GenerateResponse(httpContext, response.RedirectUri, response.Parameters);

            // Assert
            Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
            Assert.Equal(expectedLocation, httpContext.Response.Headers[HeaderNames.Location]);
            var uri = new Uri(httpContext.Response.Headers[HeaderNames.Location]);

            Assert.False(string.IsNullOrEmpty(uri.Fragment));
            var fragmentParameters = QueryHelpers.ParseQuery(uri.Fragment.Substring(1));

            Assert.Equal(2, fragmentParameters.Count);
            var codeKvp = Assert.Single(fragmentParameters, kvp => kvp.Key == "code");
            Assert.Equal("serializedcode", codeKvp.Value);
            var stateKvp = Assert.Single(fragmentParameters, kvp => kvp.Key == "state");
            Assert.Equal("#?&=", stateKvp.Value);
        }
    }
}
