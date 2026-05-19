// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using System.Text;
using FormatterWebSite.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// Functional tests for verifying the impact of using <see cref="MvcOptions.ValidateComplexTypesIfChildValidationFails"/>
/// </summary>
public class InputParentValidationTests
{
    public abstract class BaseTests<TStartup> : LoggedTest
        where TStartup : class
    {
        protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
        {
            base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
            Factory = new MvcTestFixture<TStartup>(LoggerFactory).WithWebHostBuilder(builder => builder.UseStartup<TStartup>());
            Client = Factory.CreateDefaultClient();
        }

        public override void Dispose()
        {
            Factory.Dispose();
            base.Dispose();
        }

        public WebApplicationFactory<TStartup> Factory { get; private set; }
        public HttpClient Client { get; private set; }

        protected abstract bool ShouldParentBeValidatedWhenChildIsInvalid { get; }

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
        protected override bool ShouldParentBeValidatedWhenChildIsInvalid => true;
    }

    /// <summary>
    /// Scenarios for verifying the impact of leaving <see cref="MvcOptions.ValidateComplexTypesIfChildValidationFails"/>
    /// to its default <see langword="false"/> value
    /// </summary>
    public class ParentNonValidationScenarios : BaseTests<FormatterWebSite.Startup>
    {
        protected override bool ShouldParentBeValidatedWhenChildIsInvalid => false;
    }
}
