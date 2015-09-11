// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ModelBindingWebSite.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    /// <summary>
    /// Functional tests that verify if names returned by HtmlHelper.NameFor get model bound correctly.
    /// Each test works in three steps -
    /// 1) The result of an HtmlHelper.NameFor invocation for a specific expression is retrieved.
    /// 2) A form URL encoded value is posted for the retrieved name.
    /// 3) The server returns the bound object. We verify if the property specified by the expression in step 1
    /// has the expected value.
    /// </summary>
    public class RoundTripTests : IClassFixture<MvcTestFixture<ModelBindingWebSite.Startup>>
    {
        public RoundTripTests(MvcTestFixture<ModelBindingWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task RoundTrippedValues_GetsModelBound_ForSimpleExpressions()
        {
            // Arrange
            var expected = "test-name";

            // Act
            var expression = await Client.GetStringAsync("http://localhost/RoundTrip/GetPerson");
            var keyValuePairs = new[]
            {
                new KeyValuePair<string, string>(expression, expected)
            };
            var result = await GetPerson(Client, keyValuePairs);

            // Assert
            Assert.Equal("Name", expression);
            Assert.Equal(expected, result.Name);
        }

        // Uses the expression p => p.Parent.Age
        [Fact]
        public async Task RoundTrippedValues_GetsModelBound_ForSubPropertyExpressions()
        {
            // Arrange
            var expected = 40;

            // Act
            var expression = await Client.GetStringAsync("http://localhost/RoundTrip/GetPersonParentAge");
            var keyValuePairs = new[]
            {
                new KeyValuePair<string, string>(expression, expected.ToString())
            };
            var result = await GetPerson(Client, keyValuePairs);

            // Assert
            Assert.Equal("Parent.Age", expression);
            Assert.Equal(expected, result.Parent.Age);
        }

        // Uses the expression p => p.Dependents[0].Age
        [Fact]
        public async Task RoundTrippedValues_GetsModelBound_ForNumericIndexedProperties()
        {
            // Arrange
            var expected = 12;

            // Act
            var expression = await Client.GetStringAsync("http://localhost/RoundTrip/GetPersonDependentAge");
            var keyValuePairs = new[]
            {
                new KeyValuePair<string, string>(expression, expected.ToString())
            };
            var result = await GetPerson(Client, keyValuePairs);

            // Assert
            Assert.Equal("Dependents[0].Age", expression);
            Assert.Equal(expected, result.Dependents[0].Age);
        }

        // Uses the expression p => p.Parent.Attributes["height"]
        [Fact]
        public async Task RoundTrippedValues_GetsModelBound_ForStringIndexedProperties()
        {
            // Arrange
            var expected = "6 feet";

            // Act
            var expression = await Client.GetStringAsync("http://localhost/RoundTrip/GetPersonParentHeightAttribute");
            var keyValuePairs = new[]
            {
                new KeyValuePair<string, string>(expression, expected),
            };
            var result = await GetPerson(Client, keyValuePairs);

            // Assert
            Assert.Equal("Parent.Attributes[height]", expression);
            Assert.Equal(expected, result.Parent.Attributes["height"]);
        }

        // Uses the expression p => p.Dependents[0].Dependents[0].Name
        [Fact]
        public async Task RoundTrippedValues_GetsModelBound_ForNestedNumericIndexedProperties()
        {
            // Arrange
            var expected = "test-nested-name";

            // Act
            var expression = await Client.GetStringAsync("http://localhost/RoundTrip/GetDependentPersonName");
            var keyValuePairs = new[]
            {
                new KeyValuePair<string, string>(expression, expected.ToString())
            };
            var result = await GetPerson(Client, keyValuePairs);

            // Assert
            Assert.Equal("Dependents[0].Dependents[0].Name", expression);
            Assert.Equal(expected, result.Dependents[0].Dependents[0].Name);
        }

        private static async Task<Person> GetPerson(HttpClient client, KeyValuePair<string, string>[] keyValuePairs)
        {
            var content = new FormUrlEncodedContent(keyValuePairs);
            var response = await client.PostAsync("http://localhost/RoundTrip/Person", content);
            var result = JsonConvert.DeserializeObject<Person>(await response.Content.ReadAsStringAsync());
            return result;
        }
    }
}