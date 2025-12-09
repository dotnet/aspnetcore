// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using FormatterWebSite.Controllers;
using FormatterWebSite.Models;
using Microsoft.AspNetCore.InternalTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class InputFormatterTests : LoggedTest
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

    public MvcTestFixture<FormatterWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task CheckIfXmlInputFormatterIsBeingCalled()
    {
        // Arrange
        var sampleInputInt = 10;
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<DummyClass xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\"><SampleInt>"
            + sampleInputInt + "</SampleInt></DummyClass>";
        var content = new StringContent(input, Encoding.UTF8, "application/xml");

        // Act
        var response = await Client.PostAsync("http://localhost/Home/Index", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(sampleInputInt.ToString(CultureInfo.InvariantCulture), await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("utf-8")]
    [InlineData("unicode")]
    public async Task CustomFormatter_IsSelected_ForSupportedContentTypeAndEncoding(string encoding)
    {
        // Arrange
        var content = new StringContent("Test Content", Encoding.GetEncoding(encoding), "text/plain");

        // Act
        var response = await Client.PostAsync("http://localhost/InputFormatter/ReturnInput/", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Test Content", responseBody);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    public async Task CustomFormatter_NotSelected_ForUnsupportedContentType(string contentType)
    {
        // Arrange
        var content = new StringContent("Test Content", Encoding.UTF8, contentType);

        // Act
        var response = await Client.PostAsync("http://localhost/InputFormatter/ReturnInput/", content);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task BindingWorksForPolymorphicTypes()
    {
        // Act
        var response = await Client.GetAsync("PolymorphicBinding/ModelBound?DerivedProperty=Test");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var result = JsonConvert.DeserializeObject<DerivedModel>(await response.Content.ReadAsStringAsync());
        Assert.Equal("Test", result.DerivedProperty);
    }

    [Fact]
    public async Task ValidationUsesModelMetadataFromActualModelType_ForModelBoundParameters()
    {
        // Act
        var response = await Client.GetAsync("PolymorphicBinding/ModelBound");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var result = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Collection(
            result.Properties(),
            p =>
            {
                Assert.Equal("DerivedProperty", p.Name);
                var value = Assert.IsType<JArray>(p.Value);
                Assert.Equal("The DerivedProperty field is required.", value.First);
            });
    }

    [Fact]
    public async Task InputFormatterWorksForPolymorphicTypes()
    {
        // Act
        var input = "Test";
        var response = await Client.PostAsJsonAsync("PolymorphicBinding/InputFormatted", input);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var result = JsonConvert.DeserializeObject<DerivedModel>(await response.Content.ReadAsStringAsync());
        Assert.Equal(input, result.DerivedProperty);
    }

    [Fact]
    public async Task ValidationUsesModelMetadataFromActualModelType_ForInputFormattedParameters()
    {
        // Act
        var response = await Client.PostAsJsonAsync("PolymorphicBinding/InputFormatted", string.Empty);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var result = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Collection(
            result.Properties(),
            p =>
            {
                Assert.Equal("DerivedProperty", p.Name);
                var value = Assert.IsType<JArray>(p.Value);
                Assert.Equal("The DerivedProperty field is required.", value.First);
            });
    }

    [Fact]
    public async Task InputFormatterWorksForPolymorphicProperties()
    {
        // Act
        var input = "Test";
        var response = await Client.PostAsJsonAsync("PolymorphicPropertyBinding/Action", input);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var result = JsonConvert.DeserializeObject<DerivedModel>(await response.Content.ReadAsStringAsync());
        Assert.Equal(input, result.DerivedProperty);
    }

    [Fact]
    public async Task ValidationUsesModelMetadataFromActualModelType_ForInputFormattedProperties()
    {
        // Act
        var response = await Client.PostAsJsonAsync("PolymorphicPropertyBinding/Action", string.Empty);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var result = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Collection(
            result.Properties(),
            p =>
            {
                Assert.Equal("DerivedProperty", p.Name);
                var value = Assert.IsType<JArray>(p.Value);
                Assert.Equal("The DerivedProperty field is required.", value.First);
            });
    }

    [Fact]
    public async Task BodyIsRequiredByDefault()
    {
        // Act
        var response = await Client.PostAsJsonAsync<object>($"Home/{nameof(HomeController.DefaultBody)}", value: null);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Collection(
            problemDetails.Errors,
            kvp =>
            {
                Assert.Empty(kvp.Key);
                Assert.Equal("A non-empty request body is required.", Assert.Single(kvp.Value));
            });
    }

    [Fact]
    public async Task BodyIsRequiredByDefault_WhenNullableContextEnabled()
    {
        // Act
        var response = await Client.PostAsJsonAsync<object>($"Home/{nameof(HomeController.NonNullableBody)}", value: null);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Collection(
            problemDetails.Errors,
            kvp =>
            {
                Assert.Empty(kvp.Key);
                Assert.Equal("A non-empty request body is required.", Assert.Single(kvp.Value));
            },
            kvp =>
            {
                Assert.NotEmpty(kvp.Key);
                Assert.Equal("The dummy field is required.", Assert.Single(kvp.Value));
            });
    }

    [Fact]
    public async Task BodyIsRequiredByDefaultFailsWithContentLengthZero()
    {
        var content = new ByteArrayContent(Array.Empty<byte>());
        Assert.Null(content.Headers.ContentType);
        Assert.Equal(0, content.Headers.ContentLength);

        // Act
        var response = await Client.PostAsync($"Home/{nameof(HomeController.DefaultBody)}", content);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task OptionalFromBodyWorks()
    {
        // Act
        var response = await Client.PostAsJsonAsync<object>($"Home/{nameof(HomeController.OptionalBody)}", value: null);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OptionalFromBodyWorks_WithDefaultValue()
    {
        // Act
        var response = await Client.PostAsJsonAsync<object>($"Home/{nameof(HomeController.DefaultValueBody)}", value: null);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OptionalFromBodyWorks_WithNullable()
    {
        // Act
        var response = await Client.PostAsJsonAsync<object>($"Home/{nameof(HomeController.NullableBody)}", value: null);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OptionalFromBodyWorksWithEmptyRequest()
    {
        // Arrange
        var content = new ByteArrayContent(Array.Empty<byte>());
        Assert.Null(content.Headers.ContentType);
        Assert.Equal(0, content.Headers.ContentLength);

        // Act
        var response = await Client.PostAsync($"Home/{nameof(HomeController.OptionalBody)}", content);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OptionalFromBodyWorksWithEmptyRequest_WithDefaultValue()
    {
        // Arrange
        var content = new ByteArrayContent(Array.Empty<byte>());
        Assert.Null(content.Headers.ContentType);
        Assert.Equal(0, content.Headers.ContentLength);

        // Act
        var response = await Client.PostAsync($"Home/{nameof(HomeController.DefaultValueBody)}", content);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OptionalFromBodyWorksWithEmptyRequest_WithNullable()
    {
        // Arrange
        var content = new ByteArrayContent(Array.Empty<byte>());
        Assert.Null(content.Headers.ContentType);
        Assert.Equal(0, content.Headers.ContentLength);

        // Act
        var response = await Client.PostAsync($"Home/{nameof(HomeController.NullableBody)}", content);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }
}
