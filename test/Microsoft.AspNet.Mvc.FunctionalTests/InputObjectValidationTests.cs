// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Microsoft.AspNet.WebUtilities;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class InputObjectValidationTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("FormatterWebSite");
        private readonly Action<IApplicationBuilder> _app = new FormatterWebSite.Startup().Configure;

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

        [Fact]
        public async Task CheckIfObjectIsDeserializedWithoutErrors()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
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
            var response = await client.PostAsync("http://localhost/Validation/Index", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("User has been registerd : " + sampleName,
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CheckIfObjectIsDeserialized_WithErrors()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var sampleId = 0;
            var sampleName = "user";
            var sampleAlias = "a";
            var sampleDesignation = "HelloWorld!";
            var sampleDescription = "sample user";
            var input = "{ Id:" + sampleId + ", Name:'" + sampleName + "', Alias:'" + sampleAlias +
                "' ,Designation:'" + sampleDesignation + "', description:'" + sampleDescription + "'}";
            var content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("http://localhost/Validation/Index", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("The field Id must be between 1 and 2000.," +
                "The field Name must be a string or array type with a minimum length of '5'.," +
                "The field Alias must be a string with a minimum length of 3 and a maximum length of 15.," +
                "The field Designation must match the regular expression '[0-9a-zA-Z]*'.",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CheckIfExcludedFieldsAreNotValidated()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var content = new StringContent("{\"Alias\":\"xyz\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("http://localhost/Validation/GetDeveloperName", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("No model validation for developer, even though developer.Name is empty.", 
                         await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ShallowValidation_HappensOnExcluded_ComplexTypeProperties()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var requestData = "{\"Name\":\"Library Manager\", \"Suppliers\": [{\"Name\":\"Contoso Corp\"}]}";
            var content = new StringContent(requestData, Encoding.UTF8, "application/json");
            var expectedModelStateErrorMessage 
                                 = "The field Suppliers must be a string or array type with a minimum length of '2'.";
            var shouldNotContainMessage 
                                 = "The field Name must be a string or array type with a maximum length of '5'.";

            // Act
            var response = await client.PostAsync("http://localhost/Validation/CreateProject", content);

            //Assert
            Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<Dictionary<string, ErrorCollection>>(responseContent);
            var errorCollection = Assert.Single(responseObject, modelState => modelState.Value.Errors.Any());
            var error = Assert.Single(errorCollection.Value.Errors);
            Assert.Equal(expectedModelStateErrorMessage, error.ErrorMessage);

            // verifies that the excluded type is not validated
            Assert.NotEqual(shouldNotContainMessage, error.ErrorMessage);
        }

        [Theory]
        [MemberData(nameof(SimpleTypePropertiesModelRequestData))]
        public async Task ShallowValidation_HappensOnExlcuded_SimpleTypeProperties(
                                                            string requestContent,
                                                            int expectedStatusCode,
                                                            string expectedModelStateErrorMessage)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var content = new StringContent(requestContent, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("http://localhost/Validation/CreateSimpleTypePropertiesModel",
                                                  content);

            //Assert
            Assert.Equal(expectedStatusCode, (int)response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<Dictionary<string, ErrorCollection>>(responseContent);
            var errorCollection = Assert.Single(responseObject, modelState => modelState.Value.Errors.Any());
            var error = Assert.Single(errorCollection.Value.Errors);
            Assert.Equal(expectedModelStateErrorMessage, error.ErrorMessage);
        }

        [Fact]
        public async Task CheckIfExcludedField_IsValidatedForNonBodyBoundModels()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var kvps = new List<KeyValuePair<string, string>>();
            kvps.Add(new KeyValuePair<string, string>("Alias", "xyz"));
            var content = new FormUrlEncodedContent(kvps);
            
            // Act
            var response = await client.PostAsync("http://localhost/Validation/GetDeveloperAlias", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("The Name field is required.", await response.Content.ReadAsStringAsync());
        }

        private class ErrorCollection
        {
            public IEnumerable<Error> Errors
            {
                get;
                set;
            }
        }

        private class Error
        {
            public string ErrorMessage
            {
                get;
                set;
            }
        }
    }
}