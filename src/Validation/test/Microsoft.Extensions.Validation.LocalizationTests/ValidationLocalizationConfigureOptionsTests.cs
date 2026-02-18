#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Localization;
using Microsoft.Extensions.Validation.Localization.Attributes;
using Microsoft.Extensions.Validation.LocalizationTests.Helpers;

namespace Microsoft.Extensions.Validation.LocalizationTests;

public class ValidationLocalizationConfigureOptionsTests
{
    [Fact]
    public void Configure_SetsDisplayNameProvider()
    {
        var translations = new Dictionary<string, string> { ["Customer Age"] = "Âge du client" };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();
        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        Assert.NotNull(validationOptions.DisplayNameProvider);

        var result = validationOptions.DisplayNameProvider(new DisplayNameLocalizationContext
        {
            DeclaringType = typeof(object),
            Name = "Customer Age",
            Services = new ServiceCollection().BuildServiceProvider()
        });

        Assert.Equal("Âge du client", result);
    }

    [Fact]
    public void Configure_DisplayNameProvider_ReturnsNullWhenNotFound()
    {
        var factory = new TestStringLocalizerFactory([]);
        var formatterProvider = new ValidationAttributeFormatterProvider();
        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var result = validationOptions.DisplayNameProvider!(new DisplayNameLocalizationContext
        {
            DeclaringType = typeof(object),
            Name = "NotTranslated",
            Services = new ServiceCollection().BuildServiceProvider()
        });

        Assert.Null(result);
    }

    [Fact]
    public void Configure_SetsErrorMessageProvider()
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredError"] = "Le champ {0} est obligatoire."
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();
        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        Assert.NotNull(validationOptions.ErrorMessageProvider);

        var result = validationOptions.ErrorMessageProvider(new ErrorMessageLocalizationContext
        {
            Attribute = new RequiredAttribute { ErrorMessage = "RequiredError" },
            DisplayName = "Name",
            MemberName = "Name",
            DeclaringType = typeof(object),
            Services = new ServiceCollection().BuildServiceProvider()
        });

        Assert.Equal("Le champ Name est obligatoire.", result);
    }

    [Fact]
    public void Configure_ErrorMessageProvider_ReturnsNullWhenNotFound()
    {
        var factory = new TestStringLocalizerFactory([]);
        var formatterProvider = new ValidationAttributeFormatterProvider();
        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var result = validationOptions.ErrorMessageProvider!(new ErrorMessageLocalizationContext
        {
            Attribute = new RequiredAttribute(),
            DisplayName = "Name",
            MemberName = "Name",
            DeclaringType = typeof(object),
            Services = new ServiceCollection().BuildServiceProvider()
        });

        Assert.Null(result);
    }

    [Fact]
    public void Configure_WithErrorMessageKeySelector_UsesIt()
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredAttribute_Error"] = "Custom key: {0} needed"
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();
        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions
        {
            ErrorMessageKeySelector = ctx => $"{ctx.Attribute.GetType().Name}_Error"
        });
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var result = validationOptions.ErrorMessageProvider!(new ErrorMessageLocalizationContext
        {
            Attribute = new RequiredAttribute(),
            DisplayName = "Name",
            MemberName = "Name",
            DeclaringType = typeof(object),
            Services = new ServiceCollection().BuildServiceProvider()
        });

        Assert.Equal("Custom key: Name needed", result);
    }

    [Fact]
    public void Configure_DoesNotOverrideExistingProviders()
    {
        var translations = new Dictionary<string, string>
        {
            ["The {0} field is required."] = "Translated: {0} required"
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();
        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions
        {
            ErrorMessageProvider = _ => "Pre-existing error",
            DisplayNameProvider = _ => "Pre-existing name"
        };
        configureOptions.Configure(validationOptions);

        var errorResult = validationOptions.ErrorMessageProvider(new ErrorMessageLocalizationContext
        {
            Attribute = new RequiredAttribute(),
            DisplayName = "Name",
            MemberName = "Name",
            Services = new ServiceCollection().BuildServiceProvider()
        });
        Assert.Equal("Pre-existing error", errorResult);

        var displayResult = validationOptions.DisplayNameProvider(new DisplayNameLocalizationContext
        {
            Name = "Test",
            Services = new ServiceCollection().BuildServiceProvider()
        });
        Assert.Equal("Pre-existing name", displayResult);
    }

    [Fact]
    public void Configure_ErrorMessageProvider_FormatsRangeAttributeArgs()
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeError"] = "{0} doit être entre {1} et {2}."
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();
        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions());
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var result = validationOptions.ErrorMessageProvider!(new ErrorMessageLocalizationContext
        {
            Attribute = new RangeAttribute(1, 100) { ErrorMessage = "RangeError" },
            DisplayName = "Age",
            MemberName = "Age",
            DeclaringType = typeof(object),
            Services = new ServiceCollection().BuildServiceProvider()
        });

        Assert.Equal("Age doit être entre 1 et 100.", result);
    }

    [Fact]
    public void Configure_ErrorMessageProvider_UsesKeySelectorNullToSkip()
    {
        var translations = new Dictionary<string, string>
        {
            ["The {0} field is required."] = "Should not appear"
        };
        var factory = new TestStringLocalizerFactory(translations);
        var formatterProvider = new ValidationAttributeFormatterProvider();
        var locOptions = new OptionsWrapper<ValidationLocalizationOptions>(new ValidationLocalizationOptions
        {
            ErrorMessageKeySelector = _ => null
        });
        var configureOptions = new ValidationLocalizationSetup(locOptions, factory, formatterProvider);

        var validationOptions = new ValidationOptions();
        configureOptions.Configure(validationOptions);

        var result = validationOptions.ErrorMessageProvider!(new ErrorMessageLocalizationContext
        {
            Attribute = new RequiredAttribute(),
            DisplayName = "Name",
            MemberName = "Name",
            Services = new ServiceCollection().BuildServiceProvider()
        });

        Assert.Null(result);
    }

    private class TestStringLocalizerFactory(Dictionary<string, string> translations) : IStringLocalizerFactory
    {
        private readonly Dictionary<string, string> _translations = translations;

        public IStringLocalizer Create(Type resourceSource)
            => new TestStringLocalizer(_translations);

        public IStringLocalizer Create(string baseName, string location)
            => new TestStringLocalizer(_translations);
    }
}
