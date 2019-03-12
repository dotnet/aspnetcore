// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class NewtonsoftJsonInputFormatterTest : JsonInputFormatterTestBase<FormatterWebSite.Startup>
    {
        public NewtonsoftJsonInputFormatterTest(MvcTestFixture<FormatterWebSite.Startup> fixture)
            : base(fixture)
        {
        }

        [Fact] // This test covers the 2.0 behavior. JSON.Net error messages are not preserved.
        public virtual async Task JsonInputFormatter_SuppliedJsonDeserializationErrorMessage()
        {
            // Arrange
            var content = new StringContent("{", Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("http://localhost/JsonFormatter/ReturnInput/", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("{\"\":[\"Unexpected end when reading JSON. Path '', line 1, position 1.\"]}", responseBody);
        }

        [Theory]
        [InlineData("application/json", "")]
        [InlineData("application/json", "    ")]
        public async Task JsonInputFormatter_ReturnsBadRequest_ForEmptyRequestBody(
            string requestContentType,
            string jsonInput)
        {
            // Arrange
            var content = new StringContent(jsonInput, Encoding.UTF8, requestContentType);

            // Act
            var response = await Client.PostAsync("http://localhost/JsonFormatter/ReturnInput/", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}