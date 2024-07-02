// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Encodings.Web;
using FormatterWebSite.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SystemTextJsonOutputFormatterTest : JsonOutputFormatterTestBase<FormatterWebSite.StartupWithJsonFormatter>
{
    [Fact]
    public override Task SerializableErrorIsReturnedInExpectedFormat() => base.SerializableErrorIsReturnedInExpectedFormat();

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

    [Fact]
    public async Task Formatting_PolymorphicModel_WithJsonPolymorphism()
    {
        // Arrange
        var expected = "{\"$type\":\"DerivedModel\",\"address\":\"Some address\",\"id\":10,\"name\":\"test\",\"streetName\":null}";

        // Act
        var response = await Client.GetAsync($"/SystemTextJsonOutputFormatter/{nameof(SystemTextJsonOutputFormatterController.PolymorphicResult)}");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Equal(expected, await response.Content.ReadAsStringAsync());
    }
}
