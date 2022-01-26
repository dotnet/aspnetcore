// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FormatterWebSite.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SystemTextJsonOutputFormatterTest : JsonOutputFormatterTestBase<FormatterWebSite.StartupWithJsonFormatter>
{
    public SystemTextJsonOutputFormatterTest(MvcTestFixture<FormatterWebSite.StartupWithJsonFormatter> fixture)
        : base(fixture)
    {
    }

    [Fact]
    public override async Task SerializableErrorIsReturnedInExpectedFormat()
    {
        // Arrange
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<Employee xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
            "<Id>2</Id><Name>foo</Name></Employee>";

        var expectedOutput = "{\"Id\":[\"The field Id must be between 10 and 100." +
                "\"],\"Name\":[\"The field Name must be a string or array type with" +
                " a minimum length of \\u002715\\u0027.\"]}";
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/SerializableError/CreateEmployee");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        request.Content = new StringContent(input, Encoding.UTF8, "application/xml");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var actualContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedOutput, actualContent);

        var modelStateErrors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(actualContent);
        Assert.Equal(2, modelStateErrors.Count);

        var errors = Assert.Single(modelStateErrors, kvp => kvp.Key == "Id").Value;

        var error = Assert.Single(errors);
        Assert.Equal("The field Id must be between 10 and 100.", error);

        errors = Assert.Single(modelStateErrors, kvp => kvp.Key == "Name").Value;
        error = Assert.Single(errors);
        Assert.Equal("The field Name must be a string or array type with a minimum length of '15'.", error);
    }

    [Fact]
    public override async Task Formatting_StringValueWithUnicodeContent()
    {
        // Act
        var response = await Client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringWithUnicodeResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("\"Hello Mr. \\uD83E\\uDD8A\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Formatting_WithCustomEncoder()
    {
        // Arrange
        static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.Encoder = JavaScriptEncoder.Default);
        }
        var client = Factory.WithWebHostBuilder(c => c.ConfigureServices(ConfigureServices)).CreateClient();

        // Act
        var response = await client.GetAsync($"/JsonOutputFormatter/{nameof(JsonOutputFormatterController.StringWithNonAsciiContent)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal("\"Une b\\u00EAte de cirque\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public override Task Formatting_DictionaryType() => base.Formatting_DictionaryType();

    [Fact]
    public override Task Formatting_ProblemDetails() => base.Formatting_ProblemDetails();

    [Fact]
    public override Task Formatting_PolymorphicModel() => base.Formatting_PolymorphicModel();
}
