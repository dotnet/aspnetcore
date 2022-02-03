// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using FormatterWebSite.Controllers;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public abstract class JsonInputFormatterTestBase<TStartup> : IClassFixture<MvcTestFixture<TStartup>> where TStartup : class
{
    protected JsonInputFormatterTestBase(MvcTestFixture<TStartup> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = factory.CreateDefaultClient();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<TStartup>();

    public HttpClient Client { get; }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/json")]
    public async Task JsonInputFormatter_IsSelectedForJsonRequest(string requestContentType)
    {
        // Arrange
        var sampleInputInt = 10;
        var input = "{\"sampleInt\":10}";
        var content = new StringContent(input, Encoding.UTF8, requestContentType);

        // Act
        var response = await Client.PostAsync("http://localhost/Home/Index", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(sampleInputInt.ToString(CultureInfo.InvariantCulture), await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("application/json", "{\"sampleInt\":10}", 10)]
    [InlineData("application/json", "{}", 0)]
    public async Task JsonInputFormatter_IsModelStateValid_ForValidContentType(
        string requestContentType,
        string jsonInput,
        int expectedSampleIntValue)
    {
        // Arrange
        var content = new StringContent(jsonInput, Encoding.UTF8, requestContentType);

        // Act
        var response = await Client.PostAsync("http://localhost/JsonFormatter/ReturnInput/", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedSampleIntValue.ToString(CultureInfo.InvariantCulture), responseBody);
    }

    [Theory]
    [InlineData("\"I'm a JSON string!\"")]
    [InlineData("true")]
    [InlineData("\"\"")] // Empty string
    public virtual async Task JsonInputFormatter_ReturnsDefaultValue_ForValueTypes(string input)
    {
        // Arrange
        var content = new StringContent(input, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("http://localhost/JsonFormatter/ValueTypeAsBody/", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("0", responseBody);
    }

    [Fact]
    public async Task JsonInputFormatter_ReadsPrimitiveTypes()
    {
        // Arrange
        var expected = "1773";
        var content = new StringContent(expected, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("http://localhost/JsonFormatter/ValueTypeAsBody/", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, responseBody);
    }

    [Fact]
    public async Task JsonInputFormatter_RoundtripsPocoModel()
    {
        // Arrange
        var expected = new JsonFormatterController.SimpleModel()
        {
            Id = 18,
            Name = "James",
            StreetName = "JnK",
        };

        // Act
        var response = await Client.PostAsJsonAsync("http://localhost/JsonFormatter/RoundtripSimpleModel/", expected);
        var actual = await response.Content.ReadAsAsync<JsonFormatterController.SimpleModel>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.StreetName, actual.StreetName);
    }

    [Fact]
    public virtual async Task JsonInputFormatter_RoundtripsRecordType()
    {
        // Arrange
        var expected = new JsonFormatterController.SimpleRecordModel(18, "James", "JnK");

        // Act
        var response = await Client.PostAsJsonAsync("http://localhost/JsonFormatter/RoundtripRecordType/", expected);
        var actual = await response.Content.ReadAsAsync<JsonFormatterController.SimpleRecordModel>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.StreetName, actual.StreetName);
    }

    [Fact]
    public virtual async Task JsonInputFormatter_ValidationWithRecordTypes_ValidationErrors()
    {
        // Arrange
        var expected = new JsonFormatterController.SimpleModelWithValidation(123, "This is a very long name", StreetName: null);

        // Act
        var response = await Client.PostAsJsonAsync($"JsonFormatter/{nameof(JsonFormatterController.RoundtripModelWithValidation)}", expected);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Collection(
            problem.Errors.OrderBy(e => e.Key),
            kvp =>
            {
                Assert.Equal("Id", kvp.Key);
                Assert.Equal("The field Id must be between 1 and 100.", Assert.Single(kvp.Value));
            },
            kvp =>
            {
                Assert.Equal("Name", kvp.Key);
                Assert.Equal("The field Name must be a string with a minimum length of 2 and a maximum length of 8.", Assert.Single(kvp.Value));
            },
            kvp =>
            {
                Assert.Equal("StreetName", kvp.Key);
                Assert.Equal("The StreetName field is required.", Assert.Single(kvp.Value));
            });
    }

    [Fact]
    public virtual async Task JsonInputFormatter_ValidationWithRecordTypes_NoValidationErrors()
    {
        // Arrange
        var expected = new JsonFormatterController.SimpleModelWithValidation(99, "TestName", "Some address");

        // Act
        var response = await Client.PostAsJsonAsync($"JsonFormatter/{nameof(JsonFormatterController.RoundtripModelWithValidation)}", expected);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var actual = await response.Content.ReadFromJsonAsync<JsonFormatterController.SimpleModel>();
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.StreetName, actual.StreetName);
    }

    [Fact]
    public async Task JsonInputFormatter_Returns415UnsupportedMediaType_ForEmptyContentType()
    {
        // Arrange
        var jsonInput = "{\"sampleInt\":10}";
        var content = new StringContent(jsonInput, Encoding.UTF8, "application/json");
        content.Headers.Clear();

        // Act
        var response = await Client.PostAsync("http://localhost/JsonFormatter/ReturnInput/", content);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Theory]
    [InlineData("application/json", "{\"sampleInt\":10}", 10)]
    [InlineData("application/json", "{}", 0)]
    public async Task JsonInputFormatter_IsModelStateValid_ForTransferEncodingChunk(
        string requestContentType,
        string jsonInput,
        int expectedSampleIntValue)
    {
        // Arrange
        var content = new StringContent(jsonInput, Encoding.UTF8, requestContentType);
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/JsonFormatter/ReturnInput/");
        request.Headers.TransferEncodingChunked = true;
        request.Content = content;

        // Act
        var response = await Client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedSampleIntValue.ToString(CultureInfo.InvariantCulture), responseBody);
    }
}
