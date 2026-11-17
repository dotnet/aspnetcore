// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Extensions.Validation.Tests;

internal static class GeneratedValidationTestHelpers
{
    public static (IServiceProvider Provider, ValidationOptions Options) CreateValidationServices(Action<ValidationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddValidation(configureOptions);
        var provider = services.BuildServiceProvider();
        return (provider, provider.GetRequiredService<IOptions<ValidationOptions>>().Value);
    }

    public static (IServiceProvider Provider, ValidationOptions Options) CreateValidationServices(
        IDictionary<string, string> translations,
        Action<ValidationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IStringLocalizerFactory>(new TestStringLocalizerFactory(translations));
        services.AddValidation(configureOptions);
        var provider = services.BuildServiceProvider();
        return (provider, provider.GetRequiredService<IOptions<ValidationOptions>>().Value);
    }

    private sealed class TestStringLocalizerFactory(IDictionary<string, string> translations) : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new TestStringLocalizer(translations);
        public IStringLocalizer Create(string baseName, string location) => new TestStringLocalizer(translations);
    }

    private sealed class TestStringLocalizer(IDictionary<string, string> translations) : IStringLocalizer
    {
        public LocalizedString this[string name] => translations.TryGetValue(name, out var value)
            ? new LocalizedString(name, value, resourceNotFound: false)
            : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] => translations.TryGetValue(name, out var value)
            ? new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), resourceNotFound: false)
            : new LocalizedString(name, name, resourceNotFound: true);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => throw new NotSupportedException();
    }

    public static IValidatableTypeInfo GetTypeInfo<T>(ValidationOptions options)
    {
        Assert.True(options.TryGetValidatableTypeInfo(typeof(T), out var typeInfo));
        return typeInfo;
    }

    public static ValidateContext CreateContext(IServiceProvider provider, ValidationOptions options)
        => new()
        {
            ServiceProvider = provider,
            ValidationOptions = options,
        };
}

internal sealed class CannedValidatableTypeInfo : IValidatableTypeInfo
{
    public void Validate(object? value, ValidateContext context) { }
    public Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    public bool TryFindProperty(string propertyName, ValidationOptions validationOptions, [NotNullWhen(true)] out IValidatablePropertyInfo? validatablePropertyInfo)
    {
        validatablePropertyInfo = null;
        return false;
    }
}
