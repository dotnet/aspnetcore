// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

//[assembly: WebApplicationFactoryContentRoot("Options.Multitenant", "../../Samples/Options.MultiTenant", "Options.Multitenant.csproj", "-1")]
namespace AuthSamples.FunctionalTests
{
    public class OptionsMultitenantTests : IClassFixture<WebApplicationFactory<Options.MultiTenant.Startup>>
    {
        public OptionsMultitenantTests(WebApplicationFactory<Options.MultiTenant.Startup> fixture)
        {
            Client = fixture.CreateClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task DefaultReturns200()
        {
            // Arrange & Act
            var response = await Client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task VerifySchemes(HttpResponseMessage response, IEnumerable<string> expected, IEnumerable<string> notExpected)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            foreach (var scheme in expected)
            {
                Assert.Contains(scheme, content);
                Assert.Contains($"{scheme}.Id", content);
                Assert.Contains($"{scheme}.Secret", content);
            }
            foreach (var scheme in notExpected)
            {
                Assert.DoesNotContain(scheme, content);
                Assert.DoesNotContain($"{scheme}.Id", content);
                Assert.DoesNotContain($"{scheme}.Secret", content);
            }
        }

        [Fact]
        public async Task RemoveSchemeFromWrongTenantFails()
        {
            // Arrange & Act
            var response = await AddScheme("t1-1", "tenant1");
            await VerifySchemes(response, new string[] { "t1-1" }, new string[0]);

            response = await AddScheme("t2-1", "tenant2");
            await VerifySchemes(response, new string[] { "t2-1" }, new string[] { "t1-1" });

            response = await Client.GetAsync("/Auth/Remove?scheme=t1-one");
            await VerifySchemes(response, new string[0], new string[] { "t1-1", "t2-1" });

            response = await Client.GetAsync("/?tenant=tenant1");
            await VerifySchemes(response, new string[] { "t1-1" }, new string[] { "t2-1" });

            response = await Client.GetAsync("/?tenant=tenant2");
            await VerifySchemes(response, new string[] { "t2-1" }, new string[] { "t1-1" });
        }

        [Fact]
        public async Task CanAddRemoveSchemes()
        {
            // Arrange & Act
            var response = await AddScheme("t1-1", "tenant1");
            await VerifySchemes(response, new string[] { "t1-1" }, new string[0]);

            response = await AddScheme("t2-1", "tenant2");
            await VerifySchemes(response, new string[] { "t2-1" }, new string[] { "t1-1" });

            response = await AddScheme("t3-1", "tenant3");
            await VerifySchemes(response, new string[] { "t3-1" }, new string[] { "t1-1", "t2-1" });

            response = await AddScheme("t3-2", "tenant3");
            await VerifySchemes(response, new string[] { "t3-1", "t3-2" }, new string[] { "t1-1", "t2-1" });

            // Default should be empty
            response = await Client.GetAsync("/");
            await VerifySchemes(response, new string[0], new string[] { "t1-1", "t2-1", "t3-1", "t3-2" });

            // Now remove all the schemes one at a time
            response = await Client.GetAsync("/Auth/Remove?tenant=tenant1&scheme=t1-1");
            await VerifySchemes(response, new string[0], new string[] { "t1-1", "t2-1", "t3-1", "t3-2" });

            response = await Client.GetAsync("/Auth/Remove?tenant=tenant2&scheme=t2-1");
            await VerifySchemes(response, new string[0], new string[] { "t1-1", "t2-1", "t3-1", "t3-2" });

            response = await Client.GetAsync("/Auth/Remove?tenant=tenant3&scheme=t3-2");
            await VerifySchemes(response, new string[] { "t3-1" }, new string[] { "t1-1", "t2-1", "t3-2" });

            response = await Client.GetAsync("/Auth/Remove?tenant=tenant3&scheme=t3-1");
            await VerifySchemes(response, new string[0], new string[] { "t1-1", "t2-1", "t3-1", "t3-2" });

            response = await Client.GetAsync("/");
            await VerifySchemes(response, new string[0], new string[] { "t1-1", "t2-1", "t3-1", "t3-2" });
        }

        private async Task<HttpResponseMessage> AddScheme(string name, string tenant)
        {
            var goToSignIn = await Client.GetAsync($"/?tenant={tenant}");
            var signIn = await TestAssert.IsHtmlDocumentAsync(goToSignIn);

            var form = TestAssert.HasForm(signIn);
            return await Client.SendAsync(form, new Dictionary<string, string>()
            {
                ["scheme"] = name,
                ["ClientId"] = $"{name}.Id",
                ["ClientSecret"] = $"{name}.Secret",
            });

        }

    }
}
