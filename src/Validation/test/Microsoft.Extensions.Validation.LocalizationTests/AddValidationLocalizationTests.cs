#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Localization;
using Microsoft.Extensions.Validation.Localization.Attributes;
using Microsoft.Extensions.Validation.LocalizationTests.Helpers;

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
    public void AddValidationLocalization_WithSharedResource_UsesSharedLocalizer()
    {
        Type? capturedType = null;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization<SharedResource>();
        services.AddSingleton<IStringLocalizerFactory>(new TrackingLocalizerFactory(t => capturedType = t));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        Assert.NotNull(options.ErrorMessageProvider);

        options.ErrorMessageProvider!(new ErrorMessageContext
        {
            Attribute = new System.ComponentModel.DataAnnotations.RequiredAttribute(),
            DisplayName = "Test",
            MemberName = "Test",
            Services = provider
        });

        Assert.Equal(typeof(SharedResource), capturedType);
    }

    [Fact]
    public void AddValidationLocalization_WithCustomLocalizerProvider_UsesIt()
    {
        Type? capturedType = null;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization(options =>
        {
            options.LocalizerProvider = (type, factory) =>
            {
                capturedType = type;
                return factory.Create(typeof(SharedResource));
            };
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        options.ErrorMessageProvider!(new ErrorMessageContext
        {
            Attribute = new System.ComponentModel.DataAnnotations.RequiredAttribute(),
            DisplayName = "Test",
            MemberName = "Test",
            DeclaringType = typeof(string),
            Services = provider
        });

        Assert.Equal(typeof(string), capturedType);
    }

    [Fact]
    public void AddValidationLocalization_WithErrorMessageKeySelector_UsesIt()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization(options =>
        {
            options.ErrorMessageKeySelector = ctx => $"{ctx.Attribute.GetType().Name}_Key";
        });

        var provider = services.BuildServiceProvider();
        var locOptions = provider.GetRequiredService<IOptions<ValidationLocalizationOptions>>().Value;

        Assert.NotNull(locOptions.ErrorMessageKeySelector);

        var key = locOptions.ErrorMessageKeySelector(new ErrorMessageContext
        {
            Attribute = new System.ComponentModel.DataAnnotations.RequiredAttribute(),
            DisplayName = "Test",
            MemberName = "Test",
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
            options.ErrorMessageProvider = _ => "Pre-existing";
            options.DisplayNameProvider = _ => "Pre-existing display";
        });
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        var errorResult = options.ErrorMessageProvider!(new ErrorMessageContext
        {
            Attribute = new System.ComponentModel.DataAnnotations.RequiredAttribute(),
            DisplayName = "Test",
            MemberName = "Test",
            Services = provider
        });
        Assert.Equal("Pre-existing", errorResult);

        var displayResult = options.DisplayNameProvider!(new DisplayNameContext
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

    private class SharedResource { }

    private class TrackingLocalizerFactory : IStringLocalizerFactory
    {
        private readonly Action<Type> _onCreateByType;

        public TrackingLocalizerFactory(Action<Type> onCreateByType) => _onCreateByType = onCreateByType;

        public IStringLocalizer Create(Type resourceSource)
        {
            _onCreateByType(resourceSource);
            return new TestStringLocalizer(new Dictionary<string, string>());
        }

        public IStringLocalizer Create(string baseName, string location)
            => new TestStringLocalizer(new Dictionary<string, string>());
    }
}
