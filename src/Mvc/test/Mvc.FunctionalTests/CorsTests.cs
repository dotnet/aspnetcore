// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class CorsTests : CorsTestsBase<CorsWebSite.StartupWithoutEndpointRouting>
    {
        public CorsTests(MvcTestFixture<CorsWebSite.StartupWithoutEndpointRouting> fixture)
            : base(fixture)
        {
        }

        [Fact]
        public override async Task PreflightRequestOnNonCorsEnabledController_DoesNotMatchTheAction()
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), "http://localhost/NonCors/Post");
            request.Headers.Add(CorsConstants.Origin, "http://example.com");
            request.Headers.Add(CorsConstants.AccessControlRequestMethod, "POST");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
