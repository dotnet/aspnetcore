// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Tests;

// End-to-end coverage for the validation localization pipeline that is now emitted into the
// generated code and driven purely by ValidationOptions.LocalizerProvider / MessageKeyProvider and a
// registered IStringLocalizerFactory.
public class ValidationLocalizationIntegrationTests : ValidationTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_NoLocalizerFactory_UsesAttributeDefaults(bool useAsync)
    {
        var (provider, options) = CreateServices(translations: null);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedDefaultModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedDefaultModel(), context, useAsync, default);

        Assert.Equal("The Name field is required.", Single(context, "Name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_NoLocalizerFactory_LiteralDisplayNamePassesThrough(bool useAsync)
    {
        var (provider, options) = CreateServices(translations: null);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedLiteralDisplayModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedLiteralDisplayModel(), context, useAsync, default);

        Assert.Equal("The Customer Name field is required.", Single(context, "Name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_WithLocalizer_LocalizesDisplayNameAndErrorMessage(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["Customer Name"] = "Nom du client",
            ["RequiredKey"] = "Le {0} est requis.",
        };
        var (provider, options) = CreateServices(translations);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedKeyedModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedKeyedModel(), context, useAsync, default);

        // The localized display name is resolved first and then substituted into the localized template.
        Assert.Equal("Le Nom du client est requis.", Single(context, "Name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_LookupMiss_FallsBackToAttributeErrorMessage(bool useAsync)
    {
        // Factory is present but the key is not translated, so the message produced by the attribute
        // (here, the raw ErrorMessage used as the lookup key) is used as the fallback.
        var (provider, options) = CreateServices(new Dictionary<string, string>());
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedKeyedModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedKeyedModel(), context, useAsync, default);

        Assert.Equal("RequiredKey", Single(context, "Name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_RangeAttribute_UsesBuiltInFormatter(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["RangeKey"] = "{0} must be between {1} and {2}.",
        };
        var (provider, options) = CreateServices(translations);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedRangeModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedRangeModel { Age = 0 }, context, useAsync, default);

        Assert.Equal("Age must be between 1 and 100.", Single(context, "Age"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_SelfFormattingAttribute_UsesFormatMessageHook(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["SelfKey"] = "{0}: extra={1}",
        };
        var (provider, options) = CreateServices(translations);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedSelfFormattingModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedSelfFormattingModel(), context, useAsync, default);

        Assert.Equal("Field: extra=EXTRA", Single(context, "Value"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_MessageKeyProvider_ComputesLookupKey(bool useAsync)
    {
        var translations = new Dictionary<string, string>
        {
            ["RequiredAttribute"] = "{0} is mandatory.",
        };
        var (provider, options) = CreateServices(
            translations,
            o => o.MessageKeyProvider = ctx => ctx.ValidatorType.Name);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedDefaultModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedDefaultModel(), context, useAsync, default);

        Assert.Equal("Name is mandatory.", Single(context, "Name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_ExplicitErrorMessage_WinsOverProvider(bool useAsync)
    {
        // An explicit ErrorMessage on the validator is used as the key directly; the convention
        // provider is not consulted.
        var translations = new Dictionary<string, string>
        {
            ["RequiredKey"] = "Explicit {0}.",
            ["ConventionKey"] = "Convention {0}.",
        };
        var providerCalled = false;
        var (provider, options) = CreateServices(translations, o => o.MessageKeyProvider = _ =>
        {
            providerCalled = true;
            return "ConventionKey";
        });
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedKeyedModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedKeyedModel(), context, useAsync, default);

        Assert.Equal("Explicit Customer Name.", Single(context, "Name"));
        Assert.False(providerCalled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_LocalizerProvider_InvokedWithDeclaringTypeAndUsed(bool useAsync)
    {
        var translations = new Dictionary<string, string> { ["RequiredKey"] = "Le {0} est requis." };
        Type? seenType = null;
        var (provider, options) = CreateServices(translations, o => o.LocalizerProvider = (type, factory) =>
        {
            seenType = type;
            return factory.Create(type ?? typeof(object));
        });
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedKeyedModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedKeyedModel(), context, useAsync, default);

        Assert.Equal(typeof(LocalizedKeyedModel), seenType);
        Assert.Equal("Le Customer Name est requis.", Single(context, "Name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LocalizerProvider_ReturnsNull_Throws(bool useAsync)
    {
        // The LocalizerProvider contract requires a non-null localizer; returning null throws
        // consistently on both the display-name and error-message paths.
        var translations = new Dictionary<string, string> { ["RequiredKey"] = "unused" };
        var (provider, options) = CreateServices(translations, o => o.LocalizerProvider = (_, _) => null!);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedKeyedModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ValidateAsync(typeInfo, new LocalizedKeyedModel(), context, useAsync, default));
        Assert.Contains(nameof(ValidationOptions.LocalizerProvider), ex.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_ErrorMessageResourceType_BypassesLocalization(bool useAsync)
    {
        var translations = new Dictionary<string, string> { ["Customer Name"] = "Nom du client" };
        var (provider, options) = CreateServices(translations);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedResourceErrorModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedResourceErrorModel(), context, useAsync, default);

        Assert.Equal(LocalizedResources.RequiredError, Single(context, "Name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Property_ResourceDisplayName_BypassesDisplayNameLocalization(bool useAsync)
    {
        // A [Display(ResourceType=...)] name is the canonical localized source, so the localizer is
        // not consulted for the display name even when a translation would otherwise match.
        var translations = new Dictionary<string, string> { ["Resource-Resolved Name"] = "SHOULD NOT APPEAR" };
        var (provider, options) = CreateServices(translations);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedResourceDisplayModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedResourceDisplayModel(), context, useAsync, default);

        Assert.Equal("The Resource-Resolved Name field is required.", Single(context, "Name"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IValidatableObject_ResultsNotLocalized(bool useAsync)
    {
        var translations = new Dictionary<string, string> { ["Object error"] = "SHOULD NOT APPEAR" };
        var (provider, options) = CreateServices(translations);
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<LocalizedValidatableObjectModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(typeInfo, new LocalizedValidatableObjectModel(), context, useAsync, default);

        Assert.Equal("Object error", Single(context, "Value"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Parameter_LocalizerProvider_InvokedWithParameterType_AndUsed(bool useAsync)
    {
        // A top-level parameter has no declaring type, so the localizer is resolved from the
        // parameter's own type (mirrors MVC's ContainerType ?? ModelType) instead of falling back to
        // typeof(object).
        var translations = new Dictionary<string, string> { ["Parameter Name"] = "Nom du paramètre" };
        Type? seenType = null;
        var (provider, options) = CreateServices(translations, o => o.LocalizerProvider = (type, factory) =>
        {
            seenType = type;
            return factory.Create(type ?? typeof(object));
        });
        var parameterInfo = typeof(LocalizedParameterActions)
            .GetMethod(nameof(LocalizedParameterActions.Action))!
            .GetParameters()[0];
        Assert.True(options.TryGetValidatableParameterInfo(parameterInfo, out var paramInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await ValidateAsync(paramInfo, null, context, useAsync, default);

        Assert.Equal(typeof(string), seenType);
        Assert.Equal("The Nom du paramètre field is required.", Single(context, "value"));
    }

    private static string Single(ValidateContext context, string key)
        => Assert.Single(context.ValidationErrors![key].Select(e => e.ErrorMessage));

    private static (IServiceProvider Provider, ValidationOptions Options) CreateServices(
        IDictionary<string, string>? translations,
        Action<ValidationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        if (translations is not null)
        {
            services.AddSingleton<IStringLocalizerFactory>(new FakeStringLocalizerFactory(translations));
        }
        services.AddValidation(configureOptions);
        var provider = services.BuildServiceProvider();
        return (provider, provider.GetRequiredService<IOptions<ValidationOptions>>().Value);
    }

    private sealed class FakeStringLocalizerFactory(IDictionary<string, string> translations) : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => new FakeStringLocalizer(translations);
        public IStringLocalizer Create(string baseName, string location) => new FakeStringLocalizer(translations);
    }

    private sealed class FakeStringLocalizer(IDictionary<string, string> translations) : IStringLocalizer
    {
        public LocalizedString this[string name] => translations.TryGetValue(name, out var value)
            ? new LocalizedString(name, value, resourceNotFound: false)
            : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => throw new NotSupportedException();
    }
}

[ValidatableType]
public class LocalizedDefaultModel
{
    [Required]
    public string? Name { get; set; }
}

[ValidatableType]
public class LocalizedLiteralDisplayModel
{
    [Display(Name = "Customer Name")]
    [Required]
    public string? Name { get; set; }
}

[ValidatableType]
public class LocalizedKeyedModel
{
    [Display(Name = "Customer Name")]
    [Required(ErrorMessage = "RequiredKey")]
    public string? Name { get; set; }
}

[ValidatableType]
public class LocalizedRangeModel
{
    [Display(Name = "Age")]
    [Range(1, 100, ErrorMessage = "RangeKey")]
    public int Age { get; set; }
}

[ValidatableType]
public class LocalizedSelfFormattingModel
{
    [Display(Name = "Field")]
    [SelfFormatting(ErrorMessage = "SelfKey", Extra = "EXTRA")]
    public string? Value { get; set; }
}

[ValidatableType]
public class LocalizedResourceErrorModel
{
    [Display(Name = "Customer Name")]
    [Required(ErrorMessageResourceType = typeof(LocalizedResources), ErrorMessageResourceName = nameof(LocalizedResources.RequiredError))]
    public string? Name { get; set; }
}

[ValidatableType]
public class LocalizedResourceDisplayModel
{
    [Display(Name = nameof(LocalizedResources.DisplayName), ResourceType = typeof(LocalizedResources))]
    [Required]
    public string? Name { get; set; }
}

public static class LocalizedResources
{
    public static string RequiredError => "Resource required error";
    public static string DisplayName => "Resource-Resolved Name";
}

[ValidatableType]
public class LocalizedValidatableObjectModel : IValidatableObject
{
    public string? Value { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield return new ValidationResult("Object error", [nameof(Value)]);
    }
}

public static class LocalizedParameterActions
{
    public static void Action([Display(Name = "Parameter Name")][Required] string? value) { }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class SelfFormattingAttribute : ValidationAttribute, IValidationMessageFormatter
{
    public string Extra { get; set; } = string.Empty;

    public override bool IsValid(object? value) => value is not null;

    public string FormatMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, Extra);
}
