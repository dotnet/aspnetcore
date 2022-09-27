// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Test;

public class MvcDataAnnotationsMvcOptionsSetupTests
{
    [Fact]
    public void MvcDataAnnotationsMvcOptionsSetup_ServiceConstructorWithoutIStringLocalizer()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton<IWebHostEnvironment>(Mock.Of<IWebHostEnvironment>());
        services.AddSingleton<IValidationAttributeAdapterProvider, ValidationAttributeAdapterProvider>();
        services.AddSingleton<IOptions<MvcDataAnnotationsLocalizationOptions>>(
            Options.Create(new MvcDataAnnotationsLocalizationOptions()));
        services.AddSingleton<IConfigureOptions<MvcOptions>, MvcDataAnnotationsMvcOptionsSetup>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var optionsSetup = serviceProvider.GetRequiredService<IConfigureOptions<MvcOptions>>();

        // Assert
        Assert.NotNull(optionsSetup);
    }
}
