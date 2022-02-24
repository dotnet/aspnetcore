// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text;
using FormatterWebSite.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// Functional tests for verifying the impact of using <see cref="MvcOptions.ValidateComplexTypesIfChildValidationFails"/>
/// </summary>
public class InputParentValidationTests
{
    public abstract class BaseTests<TStartup> : IClassFixture<MvcTestFixture<TStartup>>
        where TStartup : class
    {
        protected BaseTests(MvcTestFixture<TStartup> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(builder =>
                builder.UseStartup<TStartup>());

            Client = factory.CreateDefaultClient();
        }

        protected abstract bool ShouldParentBeValidatedWhenChildIsInvalid { get; }

        private HttpClient Client { get; }

        [Fact]
        public async Task ParentObjectValidation_RespectsMvcOptions_WhenChildIsInvalid()
        {
            // Arrange
            var content = CreateInvalidModel(false);
            var expectedErrors = this.GetExpectedErrors(this.ShouldParentBeValidatedWhenChildIsInvalid, true);

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/CreateInvalidModel", content);

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
            var content = CreateInvalidModel(true);
            var expectedErrors = this.GetExpectedErrors(true, false);

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/CreateInvalidModel", content);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var actualErrors = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(responseContent);

            Assert.Equal(expectedErrors, actualErrors);
        }

        private StringContent CreateInvalidModel(bool isChildValid)
        {
            var model = new InvalidModel()
            {
                Name = (isChildValid ? "Valid Name" : null)
            };

            return new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
        }

        private IDictionary<string, string[]> GetExpectedErrors(bool parentInvalid, bool childInvalid)
        {
            var result = new Dictionary<string, string[]>();

            if (parentInvalid)
            {
                result.Add(string.Empty, new string[] { "The model is not valid." });
            }

            if (childInvalid)
            {
                result.Add("Name", new string[] { "The Name field is required." });
            }

            return result;
        }
    }

    /// <summary>
    /// Scenarios for verifying the impact of setting <see cref="MvcOptions.ValidateComplexTypesIfChildValidationFails"/>
    /// to <see langword="true"/>
    /// </summary>
    public class ParentValidationScenarios : BaseTests<FormatterWebSite.StartupWithComplexParentValidation>
    {
        public ParentValidationScenarios(MvcTestFixture<FormatterWebSite.StartupWithComplexParentValidation> fixture)
            : base(fixture)
        {
        }

        protected override bool ShouldParentBeValidatedWhenChildIsInvalid => true;
    }

    /// <summary>
    /// Scenarios for verifying the impact of leaving <see cref="MvcOptions.ValidateComplexTypesIfChildValidationFails"/>
    /// to its default <see langword="false"/> value
    /// </summary>
    public class ParentNonValidationScenarios : BaseTests<FormatterWebSite.Startup>
    {
        public ParentNonValidationScenarios(MvcTestFixture<FormatterWebSite.Startup> fixture)
            : base(fixture)
        {
        }

        protected override bool ShouldParentBeValidatedWhenChildIsInvalid => false;
    }
}
