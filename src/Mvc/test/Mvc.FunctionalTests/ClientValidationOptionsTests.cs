// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class ClientValidationOptionsTests : IClassFixture<MvcTestFixture<RazorPagesWebSite.Startup>>
{
    public ClientValidationOptionsTests(MvcTestFixture<RazorPagesWebSite.Startup> fixture) =>
        Fixture = fixture;

    public MvcTestFixture<RazorPagesWebSite.Startup> Fixture { get; }

    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/55926")]
    [Fact]
    public async Task DisablingClientValidation_DisablesItForPagesAndViews()
    {
        // Arrange
        var client = Fixture
            .WithWebHostBuilder(whb => whb.UseStartup<RazorPagesWebSite.StartupWithClientValidationDisabled>())
            .CreateClient();

        // Act
        var view = await client.GetStringAsync("Controller/ClientValidationDisabled");
        var page = await client.GetStringAsync("ClientvalidationDisabled");

        // Assert
        Assert.Equal("ClientValidationDisabled", view);
        Assert.Equal("ClientValidationDisabled", page);
    }
}
