// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class InputObjectValidationTests : IClassFixture<MvcTestFixture<FormatterWebSite.Startup>>
    {
        public InputObjectValidationTests(MvcTestFixture<FormatterWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        // Parameters: Request Content, Expected status code, Expected model state error message
        public static IEnumerable<object[]> SimpleTypePropertiesModelRequestData
        {
            get
            {
                yield return new object[] {
                    "{\"ByteProperty\":1, \"NullableByteProperty\":5, \"ByteArrayProperty\":[1,2,3]}",
                    StatusCodes.Status400BadRequest,
                    "The field ByteProperty must be between 2 and 8."};

                yield return new object[] {
                    "{\"ByteProperty\":8, \"NullableByteProperty\":1, \"ByteArrayProperty\":[1,2,3]}",
                    StatusCodes.Status400BadRequest,
                    "The field NullableByteProperty must be between 2 and 8."};

                yield return new object[] {
                    "{\"ByteProperty\":8, \"NullableByteProperty\":2, \"ByteArrayProperty\":[1]}",
                    StatusCodes.Status400BadRequest,
                    "The field ByteArrayProperty must be a string or array type with a minimum length of '2'."};
            }
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task CheckIfObjectIsDeserializedWithoutErrors()
        {
            // Arrange
            var sampleId = 2;
            var sampleName = "SampleUser";
            var sampleAlias = "SampleAlias";
            var sampleDesignation = "HelloWorld";
            var sampleDescription = "sample user";
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<User xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\"><Id>" + sampleId +
                "</Id><Name>" + sampleName + "</Name><Alias>" + sampleAlias + "</Alias>" +
                "<Designation>" + sampleDesignation + "</Designation><description>" +
                sampleDescription + "</description></User>";
            var content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/Index", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("User has been registered : " + sampleName,
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CheckIfObjectIsDeserialized_WithErrors()
        {
            // Arrange
            var sampleId = 0;
            var sampleName = "user";
            var sampleAlias = "a";
            var sampleDesignation = "HelloWorld!";
            var sampleDescription = "sample user";
            var input = "{ Id:" + sampleId + ", Name:'" + sampleName + "', Alias:'" + sampleAlias +
                "' ,Designation:'" + sampleDesignation + "', description:'" + sampleDescription + "'}";
            var content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/Index", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            // Mono issue - https://github.com/aspnet/External/issues/29
            Assert.Equal(PlatformNormalizer.NormalizeContent(
                "The field Id must be between 1 and 2000.," +
                "The field Name must be a string or array type with a minimum length of '5'.," +
                "The field Alias must be a string with a minimum length of 3 and a maximum length of 15.," +
                "The field Designation must match the regular expression " +
                (TestPlatformHelper.IsMono ? "[0-9a-zA-Z]*." : "'[0-9a-zA-Z]*'.")),
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CheckIfExcludedFieldsAreNotValidated()
        {
            // Arrange
            var content = new StringContent("{\"Alias\":\"xyz\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/GetDeveloperName", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("No model validation for developer, even though developer.Name is empty.",
                         await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ShallowValidation_HappensOnExcluded_ComplexTypeProperties()
        {
            // Arrange
            var requestData = "{\"Name\":\"Library Manager\", \"Suppliers\": [{\"Name\":\"Contoso Corp\"}]}";
            var content = new StringContent(requestData, Encoding.UTF8, "application/json");
            var expectedModelStateErrorMessage
                                 = "The field Suppliers must be a string or array type with a minimum length of '2'.";
            var shouldNotContainMessage
                                 = "The field Name must be a string or array type with a maximum length of '5'.";

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/CreateProject", content);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(responseContent);
            var errorKeyValuePair = Assert.Single(responseObject, keyValuePair => keyValuePair.Value.Length > 0);
            var errorMessage = Assert.Single(errorKeyValuePair.Value);
            Assert.Equal(expectedModelStateErrorMessage, errorMessage);

            // verifies that the excluded type is not validated
            Assert.NotEqual(shouldNotContainMessage, errorMessage);
        }

        [Theory]
        [MemberData(nameof(SimpleTypePropertiesModelRequestData))]
        public async Task ShallowValidation_HappensOnExcluded_SimpleTypeProperties(
            string requestContent,
            int expectedStatusCode,
            string expectedModelStateErrorMessage)
        {
            // Arrange
            var content = new StringContent(requestContent, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync(
                "http://localhost/Validation/CreateSimpleTypePropertiesModel",
                content);

            // Assert
            Assert.Equal(expectedStatusCode, (int)response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(responseContent);
            var errorKeyValuePair = Assert.Single(responseObject, keyValuePair => keyValuePair.Value.Length > 0);
            var errorMessage = Assert.Single(errorKeyValuePair.Value);
            Assert.Equal(expectedModelStateErrorMessage, errorMessage);
        }

        [Fact]
        public async Task CheckIfExcludedField_IsNotValidatedForNonBodyBoundModels()
        {
            // Arrange
            var kvps = new List<KeyValuePair<string, string>>();
            kvps.Add(new KeyValuePair<string, string>("Alias", "xyz"));
            var content = new FormUrlEncodedContent(kvps);

            // Act
            var response = await Client.PostAsync("http://localhost/Validation/GetDeveloperAlias", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("xyz", await response.Content.ReadAsStringAsync());
        }
    }
}