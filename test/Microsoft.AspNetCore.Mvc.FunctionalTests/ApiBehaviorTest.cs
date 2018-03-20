// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BasicWebSite.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class ApiBehaviorTest : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public ApiBehaviorTest(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ActionsReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var contactModel = new Contact
            {
                Name = "Abc",
                City = "Redmond",
                State = "WA",
                Zip = "Invalid",
            };
            var contactString = JsonConvert.SerializeObject(contactModel);

            // Act
            var response = await Client.PostAsJsonAsync("/contact", contactModel);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            var actual = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(await response.Content.ReadAsStringAsync());
            Assert.Collection(
                actual.OrderBy(kvp => kvp.Key),
                kvp =>
                {
                    Assert.Equal("Name", kvp.Key);
                    var error = Assert.Single(kvp.Value);
                    Assert.Equal("The field Name must be a string with a minimum length of 5 and a maximum length of 30.", error);
                },
                kvp =>
                {
                    Assert.Equal("Zip", kvp.Key);
                    var error = Assert.Single(kvp.Value);
                    Assert.Equal("The field Zip must match the regular expression '\\d{5}'.", error);
                }
            );
        }

        [Fact]
        public async Task ActionsReturnBadRequest_UsesProblemDescriptionProviderAndApiConventionsToConfigureErrorResponse()
        {
            // Arrange
            var contactModel = new Contact
            {
                Name = "Abc",
                City = "Redmond",
                State = "WA",
                Zip = "Invalid",
            };
            var expected = new Dictionary<string, string[]>
            {
                {"Name", new string[] {"The field Name must be a string with a minimum length of 5 and a maximum length of 30."}},
                {"Zip", new string[]{ @"The field Zip must match the regular expression '\d{5}'."}}
            };
            var contactString = JsonConvert.SerializeObject(contactModel);

            // Act
            var response = await Client.PostAsJsonAsync("/contact/PostWithVnd", contactModel);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/vnd.error+json", response.Content.Headers.ContentType.MediaType);
            var content = await response.Content.ReadAsStringAsync();
            var actual = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(content);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task ActionsWithApiBehavior_InferFromBodyParameters()
        {
            // Arrange
            var input = new Contact
            {
                ContactId = 13,
                Name = "Test123",
            };

            // Act
            var response = await Client.PostAsJsonAsync("/contact/ActionWithInferredFromBodyParameter", input);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonConvert.DeserializeObject<Contact>(await response.Content.ReadAsStringAsync());
            Assert.Equal(input.ContactId, result.ContactId);
            Assert.Equal(input.Name, result.Name);
        }

        [Fact]
        public async Task ActionsWithApiBehavior_InferQueryAndRouteParameters()
        {
            // Arrange
            var id = 31;
            var name = "test";
            var email = "email@test.com";
            var url = $"/contact/ActionWithInferredRouteAndQueryParameters/{name}/{id}?email={email}";
            var response = await Client.PostAsync(url, new StringContent(string.Empty));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonConvert.DeserializeObject<Contact>(await response.Content.ReadAsStringAsync());
            Assert.Equal(id, result.ContactId);
            Assert.Equal(name, result.Name);
            Assert.Equal(email, result.Email);
        }

        [Fact]
        public async Task ActionsWithApiBehavior_InferEmptyPrefixForComplexValueProviderModel_Success()
        {
            // Arrange
            var id = 31;
            var name = "test_user";
            var email = "email@test.com";
            var url = $"/contact/ActionWithInferredEmptyPrefix?name={name}&contactid={id}&email={email}";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadAsAsync<Contact>();
            Assert.Equal(id, result.ContactId);
            Assert.Equal(name, result.Name);
            Assert.Equal(email, result.Email);
        }

        [Fact]
        public async Task ActionsWithApiBehavior_InferEmptyPrefixForComplexValueProviderModel_Ignored()
        {
            // Arrange
            var id = 31;
            var name = "test_user";
            var email = "email@test.com";
            var url = $"/contact/ActionWithInferredEmptyPrefix?contact.name={name}&contact.contactid={id}&contact.email={email}";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadAsAsync<Contact>();
            Assert.Equal(0, result.ContactId);
            Assert.Null(result.Name);
            Assert.Null(result.Email);
        }
    }
}
