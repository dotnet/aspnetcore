// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using BasicWebSite.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class NonNullableReferenceTypesTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>>
    {
        public NonNullableReferenceTypesTest(MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        private HttpClient Client { get; set; }

        [Fact]
        public async Task CanUseNonNullableReferenceType_WithController_OmitData_ValidationErrors()
        {
            // Arrange
            var parser = new HtmlParser();

            // Act 1
            var response = await Client.GetAsync("http://localhost/NonNullable");

            // Assert 1
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            var document = parser.Parse(content);
            var errors = document.QuerySelectorAll("#errors > ul > li");
            var li = Assert.Single(errors);
            Assert.Empty(li.TextContent);

            var cookieToken = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(response);
            var formToken = document.RetrieveAntiforgeryToken();

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/NonNullable");
            request.Headers.Add("Cookie", cookieToken.Key + "=" + cookieToken.Value);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", formToken),
            });

            // Act 2
            response = await Client.SendAsync(request);

            // Assert 2
            //
            // OK means there were validation errors.
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            content = await response.Content.ReadAsStringAsync();

            document = parser.Parse(content);
            errors = errors = document.QuerySelectorAll("#errors > ul > li");
            Assert.Equal(2, errors.Length); // Not validating BCL error messages
        }

        [Fact]
        public async Task CanUseNonNullableReferenceType_WithController_SubmitData_NoError()
        {
            // Arrange
            var parser = new HtmlParser();

            // Act 1
            var response = await Client.GetAsync("http://localhost/NonNullable");

            // Assert 1
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();

            var document = parser.Parse(content);
            var errors = document.QuerySelectorAll("#errors > ul > li");
            var li = Assert.Single(errors);
            Assert.Empty(li.TextContent);

            var cookieToken = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(response);
            var formToken = document.RetrieveAntiforgeryToken();

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/NonNullable");
            request.Headers.Add("Cookie", cookieToken.Key + "=" + cookieToken.Value);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string, string>("Name", "Pranav"),
                new KeyValuePair<string, string>("description", "Meme")
            });

            // Act 2
            response = await Client.SendAsync(request);

            // Assert 2
            //
            // Redirect means there were no validation errors.
            await response.AssertStatusCodeAsync(HttpStatusCode.Redirect);
        }
    }
}
