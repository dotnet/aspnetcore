// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.Extensions.DependencyInjection;

public class MvcRazorPagesMvcBuilderExtensionsTest
{
    [Fact]
    public void AddRazorPagesOptions_AddsConventions()
    {
        // Arrange
        var services = new ServiceCollection().AddOptions()
            .AddSingleton<IConfigureOptions<RazorPagesOptions>, RazorPagesOptionsSetup>();
        var applicationModelConvention = Mock.Of<IPageApplicationModelConvention>();
        var routeModelConvention = Mock.Of<IPageRouteModelConvention>();
        var builder = new MvcBuilder(services, new ApplicationPartManager());
        builder.AddRazorPagesOptions(options =>
        {
            options.Conventions.Add(applicationModelConvention);
            options.Conventions.Add(routeModelConvention);
        });
        var serviceProvider = services.BuildServiceProvider();
        var accessor = serviceProvider.GetRequiredService<IOptions<RazorPagesOptions>>();

        // Act & Assert
        var conventions = accessor.Value.Conventions;

        // Assert
        Assert.Collection(
            conventions,
            convention => Assert.Same(applicationModelConvention, convention),
            convention => Assert.Same(routeModelConvention, convention));
    }
}
