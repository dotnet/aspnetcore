// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SecurityWebSite;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class CombineAuthorizeTests : IClassFixture<MvcTestFixture<StartupWithGlobalAuthorizeAndCombineAuthorizeFilters>>
    {
        public CombineAuthorizeTests(MvcTestFixture<StartupWithGlobalAuthorizeAndCombineAuthorizeFilters> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task CanAuthorizeWithOnlyCookie2()
        {
            // Arrange & Act
            var response = await Client.PostAsync("http://localhost/Administration/SignInCookie2", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.Contains("Set-Cookie"));

            var cookie2 = response.Headers.GetValues("Set-Cookie").SingleOrDefault();

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Administration/EitherCookie");
            request.Headers.Add("Cookie", cookie2);

            response = await Client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Administration.EitherCookie:AuthorizeCount=1", body);
         }
    }
}
