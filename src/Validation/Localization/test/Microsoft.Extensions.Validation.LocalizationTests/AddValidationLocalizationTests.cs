#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation.LocalizationTests;

public class AddValidationLocalizationTests
{
    [Fact]
    public void AddValidationLocalization_RegistersLocalizationServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IStringLocalizerFactory>();

        Assert.NotNull(factory);
    }

    [Fact]
    public void AddValidationLocalization_ConfiguresErrorMessageProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        Assert.NotNull(options.ErrorMessageProvider);
    }

    [Fact]
    public void AddValidationLocalization_ConfiguresDisplayNameProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        Assert.NotNull(options.DisplayNameProvider);
    }

    [Fact]
    public void AddValidationLocalization_WithErrorMessageKeySelector_UsesIt()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization(options =>
        {
            options.ErrorMessageKeyProvider = (in ctx) => $"{ctx.Attribute.GetType().Name}_Key";
        });

        var provider = services.BuildServiceProvider();
        var locOptions = provider.GetRequiredService<IOptions<ValidationLocalizationOptions>>().Value;

        Assert.NotNull(locOptions.ErrorMessageKeyProvider);

        var key = locOptions.ErrorMessageKeyProvider(new ErrorMessageProviderContext
        {
            Attribute = new System.ComponentModel.DataAnnotations.RequiredAttribute(),
            DisplayName = "Test",
            DeclaringType = null,
            Services = provider
        });

        Assert.Equal("RequiredAttribute_Key", key);
    }

    [Fact]
    public void AddValidationLocalization_DoesNotOverrideExistingProviders()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.Configure<ValidationOptions>(options =>
        {
            options.ErrorMessageProvider = (in _) => "Pre-existing";
            options.DisplayNameProvider = (in _) => "Pre-existing display";
        });
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        var errorResult = options.ErrorMessageProvider!(new ErrorMessageProviderContext
        {
            Attribute = new System.ComponentModel.DataAnnotations.RequiredAttribute(),
            DisplayName = "Test",
            DeclaringType = null,
            Services = provider
        });
        Assert.Equal("Pre-existing", errorResult);

        var displayResult = options.DisplayNameProvider!(new DisplayNameProviderContext
        {
            Name = "Test",
            Services = provider
        });
        Assert.Equal("Pre-existing display", displayResult);
    }

    [Fact]
    public void AddValidationLocalization_RegistersDefaultAttributeArgumentProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var argProvider = provider.GetService<IValidationAttributeFormatterProvider>();

        Assert.NotNull(argProvider);
        Assert.IsType<ValidationAttributeFormatterProvider>(argProvider);
    }
}
