// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Localization.Tests;

/// <summary>
/// DI integration tests for <see cref="ValidationLocalizationServiceCollectionExtensions"/>.
/// Verifies the bridge between <see cref="ValidationLocalizationOptions"/> and
/// <see cref="ValidationOptions.Localizer"/>.
/// </summary>
public class ValidationLocalizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddValidationLocalization_RegistersIStringLocalizerFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IStringLocalizerFactory>());
    }

    [Fact]
    public void AddValidationLocalization_SetsOptionsLocalizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ValidationOptions>>().Value;

        Assert.NotNull(options.Localizer);
    }

    [Fact]
    public void AddValidationLocalization_ConfigureOptionsIsApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization(options =>
        {
            options.ErrorMessageKeyProvider = _ => "configured";
        });

        var provider = services.BuildServiceProvider();
        var localizationOptions = provider.GetRequiredService<IOptions<ValidationLocalizationOptions>>().Value;

        Assert.NotNull(localizationOptions.ErrorMessageKeyProvider);
    }

    [Fact]
    public void AddValidationLocalization_UserExplicitLocalizerWins()
    {
        // When the user assigns options.Localizer in the AddValidation callback, the bridge's
        // ??= must not overwrite it.
        var customLocalizer = new NoOpValidationLocalizer();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation(options => options.Localizer = customLocalizer);
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var resolvedFromOptions = provider.GetRequiredService<IOptions<ValidationOptions>>().Value.Localizer;

        Assert.Same(customLocalizer, resolvedFromOptions);
    }

    [Fact]
    public void AddValidationLocalization_RegistrationOrderDoesNotMatter()
    {
        // Verifies AddValidationLocalization can be called before AddValidation.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidationLocalization();
        services.AddValidation();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IOptions<ValidationOptions>>().Value.Localizer);
    }

    [Fact]
    public void AddValidationLocalizationOfTResource_ConfiguresLocalizerProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization<SharedResources>();

        var provider = services.BuildServiceProvider();
        var localizationOptions = provider.GetRequiredService<IOptions<ValidationLocalizationOptions>>().Value;

        Assert.NotNull(localizationOptions.LocalizerProvider);
        // The configured provider must always create a localizer for typeof(SharedResources),
        // regardless of the type argument.
        var fakeFactory = new RecordingLocalizerFactory();
        localizationOptions.LocalizerProvider!(typeof(SomeType), fakeFactory);
        Assert.Equal(typeof(SharedResources), fakeFactory.LastResourceSource);
    }

    [Fact]
    public void AddValidationLocalizationOfTResource_UserConfigureCallbackAlsoApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization<SharedResources>(options =>
        {
            options.ErrorMessageKeyProvider = _ => "user";
        });

        var provider = services.BuildServiceProvider();
        var localizationOptions = provider.GetRequiredService<IOptions<ValidationLocalizationOptions>>().Value;

        Assert.NotNull(localizationOptions.LocalizerProvider);
        Assert.NotNull(localizationOptions.ErrorMessageKeyProvider);
    }

    [Fact]
    public void AddValidationLocalizationOfTResource_LocalizerProviderInvokesFactoryAtMostOnce()
    {
        // The shared-resource overload caches the resolved IStringLocalizer in a closure so
        // repeated calls reuse the same instance without going through the factory each time.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization<SharedResources>();

        var provider = services.BuildServiceProvider();
        var localizationOptions = provider.GetRequiredService<IOptions<ValidationLocalizationOptions>>().Value;
        var factory = new CountingLocalizerFactory();

        var first = localizationOptions.LocalizerProvider!(typeof(SomeType), factory);
        var second = localizationOptions.LocalizerProvider!(typeof(AnotherType), factory);
        var third = localizationOptions.LocalizerProvider!(typeof(SharedResources), factory);

        Assert.Equal(1, factory.CreateCallCount);
        Assert.Same(first, second);
        Assert.Same(second, third);
    }

    [Fact]
    public void AddValidationLocalization_TryAddIsIdempotent()
    {
        // Calling AddValidationLocalization twice must not register the IConfigureOptions bridge twice.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();
        services.AddValidationLocalization();

        var provider = services.BuildServiceProvider();
        var configurers = provider.GetServices<IConfigureOptions<ValidationOptions>>()
            .OfType<ValidationLocalizationSetup>()
            .ToList();

        Assert.Single(configurers);
    }

    private sealed class NoOpValidationLocalizer : IValidationLocalizer
    {
        public string? ResolveDisplayName(in DisplayNameLocalizationContext context) => null;
        public string? ResolveErrorMessage(in ErrorMessageLocalizationContext context) => null;
    }

    private sealed class SharedResources { }
    private sealed class SomeType { }
    private sealed class AnotherType { }

    private sealed class RecordingLocalizerFactory : IStringLocalizerFactory
    {
        public Type? LastResourceSource { get; private set; }

        public IStringLocalizer Create(Type resourceSource)
        {
            LastResourceSource = resourceSource;
            return new TestStringLocalizer([]);
        }

        public IStringLocalizer Create(string baseName, string location)
            => new TestStringLocalizer([]);
    }

    private sealed class CountingLocalizerFactory : IStringLocalizerFactory
    {
        public int CreateCallCount { get; private set; }

        public IStringLocalizer Create(Type resourceSource)
        {
            CreateCallCount++;
            return new TestStringLocalizer([]);
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            CreateCallCount++;
            return new TestStringLocalizer([]);
        }
    }
}
