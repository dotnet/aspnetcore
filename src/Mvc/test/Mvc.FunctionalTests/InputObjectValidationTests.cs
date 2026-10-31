// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using FormatterWebSite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class InputObjectValidationTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<FormatterWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<FormatterWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

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
        Assert.Equal(
            "The field Id must be between 1 and 2000.," +
            "The field Name must be a string or array type with a minimum length of '5'.," +
            "The field Alias must be a string with a minimum length of 3 and a maximum length of 15.," +
            "The field Designation must match the regular expression " +
            "'[0-9a-zA-Z]*'.",
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
        var kvps = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Alias", "xyz"),
            };
        var content = new FormUrlEncodedContent(kvps);

        // Act
        var response = await Client.PostAsync("http://localhost/Validation/GetDeveloperAlias", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("xyz", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ValidationProviderAttribute_WillValidateObject()
    {
        // Arrange
        var invalidRequestData = "{\"FirstName\":\"TestName123\", \"LastName\": \"Test\"}";
        var content = new StringContent(invalidRequestData, Encoding.UTF8, "application/json");
        var expectedErrorMessage =
            "{\"FirstName\":[\"The field FirstName must match the regular expression '[A-Za-z]*'.\"," +
            "\"The field FirstName must be a string with a maximum length of 5.\"]}";

        // Act
        var response = await Client.PostAsync(
            "http://localhost/Validation/ValidationProviderAttribute", content);

        // Assert
        Assert.Equal(expected: StatusCodes.Status400BadRequest, actual: (int)response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedErrorMessage, actual: responseContent);
    }

    [Fact]
    public async Task ValidationProviderAttribute_DoesNotInterfere_WithOtherValidationAttributes()
    {
        // Arrange
        var invalidRequestData = "{\"FirstName\":\"Test\", \"LastName\": \"Testsson\"}";
        var content = new StringContent(invalidRequestData, Encoding.UTF8, "application/json");
        var expectedErrorMessage =
            "{\"LastName\":[\"The field LastName must be a string with a maximum length of 5.\"]}";

        // Act
        var response = await Client.PostAsync(
            "http://localhost/Validation/ValidationProviderAttribute", content);

        // Assert
        Assert.Equal(expected: StatusCodes.Status400BadRequest, actual: (int)response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedErrorMessage, actual: responseContent);
    }

    [Fact]
    public async Task ValidationProviderAttribute_RequiredAttributeErrorMessage_WillComeFirst()
    {
        // Arrange
        var invalidRequestData = "{\"FirstName\":\"Testname\", \"LastName\": \"\"}";
        var content = new StringContent(invalidRequestData, Encoding.UTF8, "application/json");
        var expectedError =
            "{\"LastName\":[\"The LastName field is required.\"]," +
            "\"FirstName\":[\"The field FirstName must be a string with a maximum length of 5.\"]}";

        // Act
        var response = await Client.PostAsync(
            "http://localhost/Validation/ValidationProviderAttribute", content);

        // Assert
        Assert.Equal(expected: StatusCodes.Status400BadRequest, actual: (int)response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedError, actual: responseContent);
    }

    // Test for https://github.com/aspnet/Mvc/issues/7357
    [Fact]
    public async Task ValidationThrowsError_WhenValidationExceedsMaxValidationDepth()
    {
        // Arrange
        var expected = $"ValidationVisitor exceeded the maximum configured validation depth '32' when validating property 'Value' on type '{typeof(RecursiveIdentifier)}'. " +
            "This may indicate a very deep or infinitely recursive object graph. Consider modifying 'MvcOptions.MaxValidationDepth' or suppressing validation on the model type.";
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "Validation/ValidationThrowsError_WhenValidationExceedsMaxValidationDepth")
        {
            Content = new StringContent(@"{ ""Id"": ""S-1-5-21-1004336348-1177238915-682003330-512"" }", Encoding.UTF8, "application/json"),
        };

        // Act
        var response = await Client.SendAsync(requestMessage);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.InternalServerError);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expected, content);
    }

    [Fact]
    public async Task ErrorsDeserializingMalformedJson_AreReportedForModelsWithoutAnyValidationAttributes()
    {
        // This test verifies that for a model with ModelMetadata.HasValidators = false, we continue to get an invalid ModelState + validation
        // errors from json serialization errors
        // Arrange
        var input = "{Id = \"This string is incomplete";
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "TestApi/PostBookWithNoValidation")
        {
            Content = new StringContent(input, Encoding.UTF8, "application/json"),
        };

        // Act
        var response = await Client.SendAsync(requestMessage);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        var validationProblemDetails = JsonConvert.DeserializeObject<ValidationProblemDetails>(responseContent);

        Assert.Collection(
            validationProblemDetails.Errors,
            error =>
            {
                Assert.Empty(error.Key);
                Assert.Equal(new[] { "Invalid character after parsing property name. Expected ':' but got: =. Path '', line 1, position 4." }, error.Value);
            });
    }

    [Fact]
    public async Task JsonValidationErrors_AreReportedForModelsWithoutAnyValidationAttributes()
    {
        // This test verifies that for a model with ModelMetadata.HasValidators = false, we continue to get an invalid ModelState + validation
        // errors from json serialization errors
        // Arrange
        var input = "{Id: \"0c92bb85-cfaf-4344-8a9d-f92e88716861\"}";
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "TestApi/PostBookWithNoValidation")
        {
            Content = new StringContent(input, Encoding.UTF8, "application/json"),
        };

        // Act
        var response = await Client.SendAsync(requestMessage);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        var validationProblemDetails = JsonConvert.DeserializeObject<ValidationProblemDetails>(responseContent);

        Assert.Collection(
            validationProblemDetails.Errors,
            error =>
            {
                Assert.Equal("isbn", error.Key);
                Assert.Equal(new[] { "Required property 'isbn' not found in JSON. Path '', line 1, position 44." }, error.Value);
            });
    }

    [Fact]
    public async Task ErrorsDeserializingMalformedXml_AreReportedForModelsWithoutAnyValidationAttributes()
    {
        // This test verifies that for a model with ModelMetadata.HasValidators = false, we continue to get an invalid ModelState + validation
        // errors from json serialization errors
        // Arrange
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<BookModelWithNoValidation xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite.Models\">" +
            "<Id>Incomplete element" +
            "</BookModelWithNoValidation>";
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "TestApi/PostBookWithNoValidation")
        {
            Content = new StringContent(input, Encoding.UTF8, "application/xml"),
        };

        // Act
        var response = await Client.SendAsync(requestMessage);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        var validationProblemDetails = JsonConvert.DeserializeObject<ValidationProblemDetails>(responseContent);

        Assert.Collection(
            validationProblemDetails.Errors,
            error =>
            {
                Assert.Empty(error.Key);
                Assert.Equal(new[] { "An error occurred while deserializing input data." }, error.Value);
            });
    }
}
