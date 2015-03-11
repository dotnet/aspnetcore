// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using ErrorPageMiddlewareWebSite;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    /// <summary>
    /// Functional test to verify the error reporting of Razor compilation by diagnostic middleware.
    /// </summary>
    public class ErrorPageTests
    {
        private const string SiteName = nameof(ErrorPageMiddlewareWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Assembly _resourcesAssembly = typeof(ErrorPageTests).GetTypeInfo().Assembly;

        [Theory]
        [InlineData("CompilationFailure", "/Views/ErrorPageMiddleware/CompilationFailure.cshtml(2,16): error CS0029:" +
                                          " Cannot implicitly convert type &#x27;int&#x27; to &#x27;string&#x27;")]
        [InlineData("ParserError", "The code block is missing a closing &quot;}&quot; character.  Make sure you " +
                                    "have a matching &quot;}&quot; character for all the &quot;{&quot; characters " +
                                    "within this block, and that none of the &quot;}&quot; characters are being " +
                                    "interpreted as markup.")]
        public async Task CompilationFailuresAreListedByErrorPageMiddleware(string action, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html");

            // Act
            var response = await client.GetAsync("http://localhost/" + action);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains(expected, content);
        }
    }
}