// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using RazorWebSite;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class DataAnnotationTests : IClassFixture<MvcTestFixture<StartupDataAnnotations>>
{
    private HttpClient Client { get; set; }

    public DataAnnotationTests(MvcTestFixture<StartupDataAnnotations> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(builder =>
        {
            builder.UseStartup<StartupDataAnnotations>();
        });
        Client = factory.CreateDefaultClient();
    }

    private const string EnumUrl = "http://localhost/Enum/Enum";

    [Fact]
    public async Task DataAnnotationLocalizationOfEnums_FromDataAnnotationLocalizerProvider()
    {
        // Arrange & Act
        var response = await Client.GetAsync(EnumUrl);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("FirstOptionDisplay from singletype", content);
    }
}
