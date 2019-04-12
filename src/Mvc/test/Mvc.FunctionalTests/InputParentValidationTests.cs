// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FormatterWebSite;
using FormatterWebSite.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class InputParentValidationTests : IClassFixture<MvcTestFixture<FormatterWebSite.StartupWithComplexParentValidation>>
    {
        public InputParentValidationTests(MvcTestFixture<FormatterWebSite.StartupWithComplexParentValidation> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(builder =>
                builder.UseStartup<FormatterWebSite.StartupWithComplexParentValidation>());

            Client = factory.CreateDefaultClient();
        }

        private HttpClient Client { get; }

        [Fact]
        public async Task ParentObjectIsValidated_WhenChildIsInvalid()
        {
            // Arrange
            var content = CreateManagerContent(12, "Too Short");

            var expectedErrors = new Dictionary<string, string[]>()
            {
                { string.Empty, new string[] { "A manager must have at least one direct report whose Id is greater than 20." } },
                { "DirectReports[0].Name", new string[] { "The field Name must be a string or array type with a minimum length of '15'." } }
            };

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/CreateManager", content);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var actualErrors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(responseContent);

            Assert.Equal(expectedErrors, actualErrors);
        }

        [Fact]
        public async Task ParentObjectIsValidated_WhenChildIsValid()
        {
            // Arrange
            var content = CreateManagerContent(12, "Long Enough To Be Valid");

            var expectedErrors = new Dictionary<string, string[]>()
            {
                { string.Empty, new string[] { "A manager must have at least one direct report whose Id is greater than 20." } }
            };

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/CreateManager", content);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var actualErrors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(responseContent);

            Assert.Equal(expectedErrors, actualErrors);
        }

        private StringContent CreateManagerContent(int reportId, string reportName)
        {
            var manager = new Manager()
            {
                Id = 11,
                Name = "A. Long Enough Name",
                DirectReports = new List<Employee>()
                {
                    new Employee() { Id = reportId, Name = reportName }
                }
            };

            return new StringContent(JsonConvert.SerializeObject(manager), Encoding.UTF8, "application/json");
        }
    }
}